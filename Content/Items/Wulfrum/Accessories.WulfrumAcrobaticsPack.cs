using MonoMod.Cil;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Localization;
using static CalamityFables.Helpers.FablesUtils;
using static Mono.Cecil.Cil.OpCodes;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Items.Wulfrum
{
    [AutoloadEquip(EquipType.Back)]
    [ReplacingCalamity("WulfrumAcrobaticsPack")]
    public class WulfrumAcrobaticsPack : ModItem
    {
        public static readonly SoundStyle ShootSound = new(SoundDirectory.Wulfrum + "WulfrumHookShoot") { Volume = 0.7f, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };
        public static readonly SoundStyle GrabSound = new(SoundDirectory.Wulfrum + "WulfrumHookGrapple") { Volume = 0.7f, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };
        public static readonly SoundStyle ReleaseSound = new(SoundDirectory.Wulfrum + "WulfrumHookDisengage") { Volume = 0.7f, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };
        public static readonly SoundStyle SwingWooshSound = new("CalamityFables/Sounds/LoudSwingWoosh");

        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public const float MOVESPEED_INCREASE = 0.08f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(MOVESPEED_INCREASE * 100));

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Wulfrum Acrobatics Pack");
            //Tooltip.SetDefault("Retools hooks into a wulfrum slingshot with swinging physics\n" +
            //    "Hold UP to retract the hook\n" +
            //    "Automatically attempts to grapple a nearby tiles when about to take fall damage, unless DOWN is held\n" +
            //    "8% increased movement speed");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 22;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.accessory = true;
            Item.rare = ItemRarityID.Blue;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.moveSpeed += MOVESPEED_INCREASE;
            player.GetModPlayer<WulfrumPackPlayer>().wulfrumPackEquipped = true;
            player.GetModPlayer<WulfrumPackPlayer>().PackItem = Item;

            Lighting.AddLight(player.Center, Color.Lerp(Color.DeepSkyBlue, Color.GreenYellow, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.5f + 0.5f).ToVector3());
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Chain, 2).
                AddIngredient<WulfrumMetalScrap>(6).
                AddIngredient<EnergyCore>(1).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class WulfrumPackPlayer : ModPlayer
    {
        public override void Load()
        {
            FablesPlayer.OverrideGrappleEvent += OverrideGrapple;
            On_Player.GrappleMovement += CustomGrappleMovementCheck;
            On_Player.PlayerFrame += EnableWalkAnimWhileGrappled;
            FablesProjectile.CanUseGrappleEvent += FablesProjectile_CanUseGrappleEvent;

            IL_Player.Update += ConserveRegularMovementWhileGrappled;
            IL_Player.Update += LetPlayerWalkUpSlopesWhileGrappled;
        }

        private bool? FablesProjectile_CanUseGrappleEvent(int type, Player player)
        {
            //Player can shoot up to 2 wulfrum hooks, but only 2 is allowed to stay grappled.
            if (player.GetModPlayer<WulfrumPackPlayer>().wulfrumPackEquipped)
            {
                if (Main.projectile.Count(n => n.active && n.owner == player.whoAmI && n.type == ProjectileType<WulfrumHook>()) > 1)
                    return false;
            }

            return null;
        }


        #region Detours and IL

        private bool OverrideGrapple(Player player)
        {
            WulfrumPackPlayer mp = player.GetModPlayer<WulfrumPackPlayer>();
            if (!mp.wulfrumPackEquipped)
                return false;

            if (mp.hookCooldown > 0)
                return true;

            if (!FablesPlayer.BasicGrappleChecks(player, ProjectileType<WulfrumHook>(), true))
                return true;

            new SyncWulfrumHookSounds(true, Main.LocalPlayer.Center).Send(); //plays the sound cuz it runs locally as well

            //Shoot projectile
            Vector2 velocity = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.One) * GrappleVelocity;
            Projectile.NewProjectile(player.GetSource_ItemUse(mp.PackItem), player.Center, velocity, ProjectileType<WulfrumHook>(), 0, 0, player.whoAmI);

            //Puts a cooldown on hooking straight downwards to avoid infinite height gain / "hover"
            float angleToRightBelow = Vector2.Dot(velocity.SafeNormalize(Vector2.UnitY), Vector2.UnitY);
            if (angleToRightBelow > 0) 
            {
                int extraCooldown = (int)(angleToRightBelow * 15); //Get more cooldown the straightest down youre aiming
                mp.hookCooldown = 15 + extraCooldown;
            }

            return true;
        }

        /// <summary>
        /// Determines if the custom grapple movement should take place or not. Useful for hooks that only do movement tricks in some cases
        /// </summary>
        private static void CustomGrappleMovementCheck(On_Player.orig_GrappleMovement orig, Player self)
        {
            WulfrumPackPlayer mp = self.GetModPlayer<WulfrumPackPlayer>();

            if (mp.GrappleMovementDisabled)
                return;
            orig(self);
        }

        private void ConserveRegularMovementWhileGrappled(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<PlayerEyeHelper>("Update")))
            {
                LogILEpicFail("Prevent custom grapples from disabling movement", "Could not locate function call for UpdatePettingAnimal.");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdarg(0), i => i.MatchLdfld<Player>("tongued"), i => i.MatchBrtrue(out _)))
            {
                LogILEpicFail("Prevent custom grapples from disabling movement", "Could not locate field tongued.");
                return;
            }

            ILLabel labelStartTonguedCheck = il.DefineLabel();
            labelStartTonguedCheck.Target = cursor.Next;

            if (!cursor.TryGotoPrev(MoveType.AfterLabel, i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Player>("grappling"),
                    i => i.MatchLdcI4(0),
                    i => i.MatchLdelemI4(),
                    i => i.MatchLdcI4(-1),
                    i => i.MatchBneUn(out _)))
            {
                LogILEpicFail("Prevent custom grapples from disabling movement", "Could not locate grappling[0] == -1 check");
                return;
            }

            //Cache all the labels pointing to the grappling check
            /*
            List<ILLabel> labels = cursor.IncomingLabels.ToList();
            int editFirstInstruction = cursor.Index;
            */

            cursor.Emit(Ldarg_0);
            cursor.EmitDelegate(PlayerCountsAsUngrappled);
            cursor.Emit(Brtrue_S, labelStartTonguedCheck);

            /*
            //Change all labels that were pointing to the grappling check directly to point to our new check instead
            Instruction newTarget = cursor.Instrs[editFirstInstruction];
            foreach (ILLabel lbl in labels)
                lbl.Target = newTarget;
            */
        }

        private void LetPlayerWalkUpSlopesWhileGrappled(ILContext il)
        {

            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<Player>("SlopeDownMovement")))
            {
                LogILEpicFail("Prevent custom grapples from disabling slope walking", "Could not locate function call for SlopeDownMovement");
                return;
            }

            ILLabel startOfIfElse = null;

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdarg(0), i => i.MatchLdfld<Player>("gravDir"), i => i.MatchLdcR4(-1), i => i.MatchBneUn(out startOfIfElse)))
            {
                LogILEpicFail("Prevent custom grapples from disabling slope walking", "Could not locate the gravDir == -1 check");
                return;
            }

            cursor.GotoLabel(startOfIfElse);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Player>("grappling"),
                    i => i.MatchLdcI4(0),
                    i => i.MatchLdelemI4(),
                    i => i.MatchLdcI4(-1),
                    i => i.MatchBneUn(out _)))
            {
                LogILEpicFail("Prevent custom grapples from disabling slope walking", "Could not locate grappling[0] == -1 check.");
                return;
            }

            ILLabel ifElseBody = il.DefineLabel(cursor.Next);

            if (!cursor.TryGotoPrev(MoveType.AfterLabel, i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Player>("grappling"),
                    i => i.MatchLdcI4(0),
                    i => i.MatchLdelemI4(),
                    i => i.MatchLdcI4(-1),
                    i => i.MatchBneUn(out _)))
            {
                LogILEpicFail("Prevent custom grapples from disabling slope walking", "Somehow couldnt backtrack to before the grappling[0] check");
                return;
            }

            cursor.Emit(Ldarg_0);
            cursor.EmitDelegate(PlayerCountsAsUngrappled);
            cursor.Emit(Brtrue_S, ifElseBody);
        }

        //Lets the player have an animated walk cycle if the grapples movement is disabled
        private static void EnableWalkAnimWhileGrappled(On_Player.orig_PlayerFrame orig, Player self)
        {
            WulfrumPackPlayer mp = self.GetModPlayer<WulfrumPackPlayer>();
            int cache = -1;
            //Cache the hook if it should be ignored for framing
            if (self.grappling[0] >= 0 && mp.GrappleMovementDisabled && Main.projectile[self.grappling[0]].type == ProjectileType<WulfrumHook>())
            {
                cache = self.grappling[0];
                self.grappling[0] = -1;
            }

            orig(self);

            //Load back the cached hook
            if (cache > -1)
                self.grappling[0] = cache;
        }

        #endregion

        public bool wulfrumPackEquipped = false;
        public Item PackItem = null;
        /// <summary>
        /// The index of the grapple projectile currently grappled.
        /// </summary>
        public int grappleProjectileIndex = 0;
        /// <summary>
        /// The lenght of the current rope. Determined when the grapple lands.
        /// </summary>
        public float SwingLenght = 0f;
        /// <summary>
        /// Used when we need to store the hook between instructions.
        /// </summary>
        public int hookCache = -1;
        /// <summary>
        /// The cooldown is only set when firing a hook straight downwards
        /// </summary>
        public int hookCooldown = 0;

        #region Properties
        /// <summary>
        /// Is the player grappled?
        /// </summary>
        public bool Grappled {
            get {
                if (!wulfrumPackEquipped)
                    return false;
                if (grappleProjectileIndex < 0)
                    return false;

                Projectile proj = Main.projectile[grappleProjectileIndex];
                if (proj.type != ModContent.ProjectileType<WulfrumHook>())
                {
                    grappleProjectileIndex = -1;
                    return false;
                }

                return proj.owner == Player.whoAmI && proj.active && proj.ai[0] == 3;
            }
        }

        /// <summary>
        /// Can the player move around as if unhooked?
        /// This applies only if the player is hooked and on the floor.
        /// The grapple movement resumes if the player tries to further away from the lenght of the hook
        /// </summary>
        public bool GrappleMovementDisabled {
            get {
                if (!Grappled || !PlayerOnGround)
                    return false;
                return (Player.Center - Main.projectile[grappleProjectileIndex].Center).Length() < SwingLenght;
            }
        }

        public bool AutoGrappleActivated {
            get {
                if (!wulfrumPackEquipped || Grappled || //Ignore if player isnt wearing the grapple pack, or is already grappled.
                    Player.noFallDmg || Player.equippedWings != null || //Ignore if player can't take fall damage
                    Player.controlDown || //Ignore if player disables the auto grapple by holding down
                    Player.velocity.Y * Player.gravDir < 0 || //Ignore if not falling *down*
                    (Player.fallStart >= (int)(Player.position.Y / 16f) && Player.gravDir > 0) || (!(Player.fallStart <= (int)(Player.position.Y / 16f)) && Player.gravDir < 0) || //Ignore if the player is not falling below their last fall point
                    Player.mount.Active || //ignore if player is on a mount
                    Player.webbed || Player.stoned || Player.frozen || Player.vortexDebuff //Ignore if players movement is compromised
                    )
                    return false;

                return true;
            }
        }

        public int PlayerCountsAsUngrappled(Player player)
        {
            if (player.grappling[0] == -1)
                return 0;
            return (player.GetModPlayer<WulfrumPackPlayer>().GrappleMovementDisabled && Main.projectile[player.grappling[0]].type == ProjectileType<WulfrumHook>()) ? 1 : 0;
        }

        #endregion

        public Vector2 CurrentPosition;
        public Vector2 OldPosition;
        public List<VerletPoint> Segments;

        public static int SimulationResolution = 3;
        public static int HookUpdates = 3;
        public static float GrappleVelocity = 17f;
        public static float GrappleReelSpeed = 2.4f;
        public static float ReturnVelocity = 5f;
        public static float MaxHopVelocity = 4f; //The maximum velocity at which the player gets any amount of vertical boost from hopping out of the hook
        public static int SafetySteps = 3;
        public static float SafetyHookAngle = MathHelper.PiOver2 * 1.2f;
        public static float SafetyHookAngleResolution = 50f;

        public bool PlayerOnGround => Collision.SolidCollision(Player.position + Vector2.UnitY * 2f * Player.gravDir, Player.width, Player.height, false);

        public override void ResetEffects()
        {
            wulfrumPackEquipped = false;
            PackItem = null;

            if (hookCooldown > 0)
                hookCooldown--;
        }

        //Initialize the segments between the player and the hook's end point.
        public void SetSegments(Vector2 endPoint)
        {
            if (Segments == null)
                Segments = new List<VerletPoint>();

            Segments.Clear();

            for (int i = 0; i <= SimulationResolution; i++)
            {
                float progress = i / (float)SimulationResolution;
                VerletPoint segment = new VerletPoint(Vector2.Lerp(endPoint, Player.Center, progress));
                if (i == 0)
                    segment.locked = true;

                if (i == SimulationResolution)
                    segment.oldPosition = Player.oldPosition + new Vector2(Player.width, Player.height) * 0.5f;

                Segments.Add(segment);
            }
        }

        public override void PreUpdateMovement()
        {
            if (Grappled)
            {
                //Break the grapple if the player is too far from the hook origin
                if ((Main.projectile[grappleProjectileIndex].Center - Player.Center).Length() > SwingLenght + 80f && Main.myPlayer == Player.whoAmI)
                {
                    new SyncWulfrumHookSounds(false, Main.projectile[grappleProjectileIndex].Center).Send();
                    Main.projectile[grappleProjectileIndex].Kill();
                }
                else
                    SimulateMovement(Main.projectile[grappleProjectileIndex]);
            }
        }

        public void SimulateMovement(Projectile grapple)
        {
            if (Segments is null)
            {
                //Failsafe that requests the control points
                if (Main.myPlayer != Player.whoAmI)
                    new RequestWulfrumHookSync(Player.whoAmI);
                return;
            }

            Segments = VerletPoint.SimpleSimulation(Segments, SwingLenght / SimulationResolution, 50, 0.3f * Player.gravDir);

            Vector2 CurrentPosition;

            foreach (VerletPoint position in Segments)
            {
                CurrentPosition = position.position;
            }


            if (!GrappleMovementDisabled)
            {
                CurrentPosition = Segments[SimulationResolution].position;
                Player.velocity = CurrentPosition - Player.Center;

                //let the player swing themselves around if they are under the hook.
                if (Player.gravDir * (Player.Center.Y - Segments[0].position.Y) > 0)
                {
                    float swing = 0;

                    if (Math.Sign(Player.velocity.X) < 0)
                    {
                        if (Player.controlLeft)
                            swing -= 0.1f;

                        else if (Player.controlRight)
                            swing += 0.1f;
                    }

                    else if (Math.Sign(Player.velocity.X) > 0)
                    {
                        if (Player.controlRight)
                            swing += 0.1f;

                        else if (Player.controlLeft)
                            swing -= 0.1f;
                    }

                    Player.velocity.X += swing;
                }

                //Pushes the player sideways if they're above the hook and almost above it, to prevent them from staying up without falling
                else if (Math.Abs(Player.Center.X - Segments[0].position.X) < 30f && Math.Abs(Player.velocity.X) < 1)
                {
                    Player.velocity.X = Player.velocity.X == 0 ? 1.5f : 1.5f * Math.Sign(Player.velocity.X);
                }

                if (Main.myPlayer == Player.whoAmI)
                    new SyncWulfrumHookSimulation().Send(-1, -1, false);
            }

            if (Grappled)
            {
                for (int i = 1; i < Segments.Count; i++)
                {
                    Lighting.AddLight(Segments[i].position, Color.Lerp(Color.DeepSkyBlue, Color.GreenYellow, i / (float)SimulationResolution).ToVector3());
                }
            }

            //Set the old position of the simulation's segments to be the players current center (before the velocity gets applied
            //We can't set the new position here by simply adding the velocity to the players current position because it leads to.. funny bugs if you collide with tiles.
            Segments[SimulationResolution].oldPosition = Player.Center;
        }

        public override void PostUpdate()
        {
            //After the player's movements are finished being calculated, set the current position of the hook chain to be at their new center.
            if (Grappled)
            {
                if (Segments == null)
                {
                    if (Main.myPlayer != Player.whoAmI)
                        new RequestWulfrumHookSync(Player.whoAmI);
                    return;
                }

                Segments[SimulationResolution].position = Player.Center;


                if (!GrappleMovementDisabled)
                {
                    //Play a swoosh sound if the player changed sides and moved fast
                    bool playerCrossedSides = Math.Sign(Segments[SimulationResolution].oldPosition.X - Segments[0].position.X) != Math.Sign(Segments[SimulationResolution].position.X - Segments[0].position.X);
                    float swingSpeed = (Segments[SimulationResolution].oldPosition - Segments[SimulationResolution].position).Length();
                    if (swingSpeed > 6f && playerCrossedSides)
                        SoundEngine.PlaySound(WulfrumAcrobaticsPack.SwingWooshSound with { Volume = WulfrumAcrobaticsPack.SwingWooshSound.Volume * (Math.Clamp((swingSpeed - 6f) / 12f, 0, 1)) }, Player.Center);
                }
            }

            else if (AutoGrappleActivated)
            {
                Vector2 checkedPlayerPosition = Player.position;
                bool imminentDanger = false;

                for (int i = 0; i < SafetySteps; i++)
                {
                    Vector2 collisionVector = Collision.TileCollision(checkedPlayerPosition, Player.velocity, Player.width, Player.height, gravDir: (int)Player.gravDir);
                    if (collisionVector.Y < Player.velocity.Y)
                    {
                        imminentDanger = true;
                        checkedPlayerPosition += collisionVector;
                        break;
                    }

                    checkedPlayerPosition += collisionVector;
                }

                if (!imminentDanger)
                    return;

                int fallDistance = (int)(checkedPlayerPosition.Y / 16f) - Player.fallStart;
                int fallDmgThreshold = 25 + Player.extraFall;

                //Technically doesn't ignore clouds but oh well.
                if (fallDistance * Player.gravDir > fallDmgThreshold)
                {
                    float halfSpread = SafetyHookAngle / 2f;
                    Point bestGrapplePos = Point.Zero;
                    float bestGrappleScore = 0;

                    for (float angle = -halfSpread; angle < halfSpread; angle += SafetyHookAngle / SafetyHookAngleResolution)
                    {
                        for (int i = 0; i < (int)(WulfrumHook.MaxReach / 16f); i++)
                        {
                            Vector2 checkSpot = Player.Center + (-Vector2.UnitY * Player.gravDir * i * 16f).RotatedBy(angle);
                            Point tilePos = checkSpot.ToSafeTileCoordinates();
                            Tile tile = Main.tile[tilePos];
                            if (tile.HasUnactuatedTile && tile.CanTileBeLatchedOnTo() && !Player.IsBlacklistedForGrappling(tilePos))
                            {
                                if (bestGrappleScore < EvaluatePotentialSafetyHookPos((checkSpot - Player.Center).Length(), angle))
                                {
                                    bestGrapplePos = tilePos;
                                    bestGrappleScore = EvaluatePotentialSafetyHookPos((checkSpot - Player.Center).Length(), angle);
                                }
                                break;
                            }
                        }
                    }

                    if (bestGrapplePos != Point.Zero)
                    {
                        //Clear any hooks that might have been flying before then.
                        for (int i = 0; i < Main.maxProjectiles; ++i)
                        {
                            Projectile p = Main.projectile[i];
                            if (!p.active || p.owner != Player.whoAmI || p.type != ProjectileType<WulfrumHook>())
                                continue;

                            if (p.ModProjectile is WulfrumHook)
                            {
                                SoundEngine.PlaySound(WulfrumAcrobaticsPack.ReleaseSound, p.Center);
                                p.Kill();

                            }
                        }

                        //Reset the players fall height, because if they take fall dmg in teh frame right after this one it may have a chance of still killing the player due to the
                        //code where the grapple resets the players fall speed hasnt been called yet
                        Player.fallStart = (int)(Player.position.Y / 16);

                        if (Player.whoAmI == Main.myPlayer)
                        {
                            Projectile.NewProjectile(Player.GetSource_ItemUse(PackItem), bestGrapplePos.ToWorldCoordinates(), Vector2.Zero, ProjectileType<WulfrumHook>(), 3, 0, Player.whoAmI);
                        }
                    }
                }
            }
        }

        public float EvaluatePotentialSafetyHookPos(float distance, float angle)
        {
            float score = 0.0001f;

            if (distance < 2 * WulfrumHook.MaxReach / 3f)
                score += distance / (2 * WulfrumHook.MaxReach / 3f);

            else
                score += 1 - (distance - (2 * WulfrumHook.MaxReach / 3f)) / (WulfrumHook.MaxReach / 3f);

            score += (1 - Math.Abs(angle) / (SafetyHookAngle / 2f)) * 0.5f;

            return score;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (!wulfrumPackEquipped)
                return;

            if (triggersSet.Up && SwingLenght > 3f && Grappled)
            {
                SoundEngine.PlaySound(WulfrumAcrobaticsPack.ReleaseSound with { Pitch = -0.7f, Volume = 0.3f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, MaxInstances = 1 });
                SwingLenght -= GrappleReelSpeed;
                new SyncWulfrumHookLenght().Send(-1, -1, false);
            }

            //Jumping out of the hook
            if (triggersSet.Jump && Player.releaseJump)
            {
                for (int i = 0; i < Main.maxProjectiles; ++i)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.owner != Player.whoAmI || p.type != ProjectileType<WulfrumHook>())
                        continue;

                    //Only clear hooks that are attached to stuff
                    if (p.ModProjectile is WulfrumHook claw && claw.State == WulfrumHook.HookState.Grappling)
                    {

                        float angleToUpright = (Player.Center - p.Center).AngleBetween(-Vector2.UnitY);
                        float jumpForceOffHook = Player.Distance(p.Center) < 38 ? 1f : Utils.GetLerpValue(MathHelper.PiOver4, MathHelper.PiOver2, angleToUpright, true);

                        if (jumpForceOffHook > 0f)
                        {
                            Vector2 velocityBoost = Vector2.Zero;

                            //Additionally, accelerate the player a lil' if they were holding down the buttons in the direction of their swing.
                            if ((Math.Sign(Player.velocity.X) < 0 && Player.controlLeft) || (Math.Sign(Player.velocity.X) > 0 && Player.controlRight))
                            {
                                velocityBoost += Player.velocity * 0.15f;
                            }
                            //Additionally^2, if the player isnt moving very fast, make them do a straight up hop.
                            //Don't do the hop if the player isnt moving at all though because thats handled by vanilla.
                            if (Player.velocity.Length() < MaxHopVelocity && Player.velocity.Length() > 0.0001f || PlayerOnGround)
                            {
                                velocityBoost -= Vector2.UnitY * Player.jumpSpeed * (1 - (float)Math.Pow(Player.velocity.Length() / MaxHopVelocity, 5f));
                            }

                            Player.velocity += velocityBoost * jumpForceOffHook;
                            Player.jump = Player.jumpHeight / 2;

                        }
                        else
                        {
                            //Prevents double jumps from getting activated
                            Player.releaseJump = false;
                        }

                        new SyncWulfrumHookJumpoff(jumpForceOffHook > 0, Player.velocity, Player.jump).Send(runLocally:false);
                        new SyncWulfrumHookSounds(false, p.Center).Send();
                        p.Kill();
                        Player.ClearGrapplingBlacklist();
                    }
                }

            }
        }
    }

    #region Packets
    [Serializable]
    public class SyncWulfrumHookSounds : Module
    {
        int whoAmI;
        bool shoot;
        Vector2 position;

        public SyncWulfrumHookSounds(bool shootSound, Vector2 position)
        {
            whoAmI = Main.myPlayer;
            shoot = shootSound;
            this.position = position;
        }

        protected override void Receive()
        {
            if (Main.dedServ)
                Send(-1, whoAmI, false);
            else
                SoundEngine.PlaySound(shoot ? WulfrumAcrobaticsPack.ShootSound : WulfrumAcrobaticsPack.ReleaseSound, position);
        }
    }

    [Serializable]
    public class SyncWulfrumHookSimulation : Module
    {
        int whoAmI;
        float hookLenght;
        Vector2[] controlPoints;
        Vector2[] controlPointsOldPos;
        Vector2 playerVelocity;

        public SyncWulfrumHookSimulation()
        {
            whoAmI = Main.myPlayer;
            WulfrumPackPlayer mp = Main.LocalPlayer.GetModPlayer<WulfrumPackPlayer>();
            hookLenght = mp.SwingLenght;
            playerVelocity = Main.LocalPlayer.velocity;
            controlPoints = mp.Segments.Select(p => p.position).ToArray();
            controlPointsOldPos = mp.Segments.Select(p => p.oldPosition).ToArray();
        }

        protected override void Receive()
        {
            Player player = Main.player[whoAmI];
            WulfrumPackPlayer mp = player.GetModPlayer<WulfrumPackPlayer>();
            mp.SwingLenght = hookLenght;

            player.velocity = playerVelocity;

            if (mp.Segments == null)
                mp.Segments = new List<VerletPoint>();
            mp.Segments.Clear();

            for (int i = 0; i <= WulfrumPackPlayer.SimulationResolution; i++)
            {
                VerletPoint segment = new VerletPoint(controlPoints[i]);
                if (i == 0)
                    segment.locked = true;
                segment.oldPosition = controlPointsOldPos[i];

                mp.Segments.Add(segment);
            }

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }

    [Serializable]
    public class RequestWulfrumHookSync : Module
    {
        int target;
        public RequestWulfrumHookSync(int who)
        {
            target = who;
        }

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
                Send(target, -1, false);
            else if (target == Main.myPlayer)
                new SyncWulfrumHookSimulation().Send(-1, -1, false);
        }
    }

    [Serializable]
    public class SyncWulfrumHookLenght : Module
    {
        int whoAmI;
        float hookLenght;

        public SyncWulfrumHookLenght()
        {
            whoAmI = Main.myPlayer;
            WulfrumPackPlayer mp = Main.LocalPlayer.GetModPlayer<WulfrumPackPlayer>();
            hookLenght = mp.SwingLenght;
        }

        protected override void Receive()
        {
            Player player = Main.player[whoAmI];
            WulfrumPackPlayer mp = player.GetModPlayer<WulfrumPackPlayer>();
            mp.SwingLenght = hookLenght;
            SoundEngine.PlaySound(WulfrumAcrobaticsPack.ReleaseSound with { Pitch = -0.7f, Volume = 0.3f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, MaxInstances = 1 }, player.Center);

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }

    [Serializable]
    public class SyncWulfrumHookJumpoff : Module
    {
        int whoAmI;
        bool couldJumpOff;
        Vector2 velocity;
        int jump;

        public SyncWulfrumHookJumpoff(bool couldJumpOff, Vector2 velocity, int jump = 0)
        {
            whoAmI = Main.myPlayer;
            this.couldJumpOff = couldJumpOff;
            this.jump = jump;
            this.velocity = velocity;
        }

        protected override void Receive()
        {
            Player player = Main.player[whoAmI];
            if (!couldJumpOff)
                player.releaseJump = false;
            else
            {
                player.jump = jump;
                player.velocity = velocity;
            }

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }
    #endregion

    public class WulfrumHook : ModProjectile, IDrawPixelated
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public Player Owner => Main.player[Projectile.owner];
        internal PrimitiveTrail TrailRenderer;

        public HookState State {
            get => (HookState)(int)Projectile.ai[0];
            set { Projectile.ai[0] = (int)value; }
        }

        public ref float Timer => ref Projectile.ai[1];

        public DrawhookLayer layer => DrawhookLayer.AboveTiles;

        public enum HookState
        {
            Thrown,
            Retracting,
            Grappling = 3 //Making this value "3" is important here, as it makes it so that i can put this projectile in the player grapple list while also never having it considered as "grappling" (aka ai[0] = 2)
        }

        public static float MaxReach = 600;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Slingshot");
            //Expand the draw distance. Should never happen really , but just in case the player basically walks away from the hook.
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 3000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 3;
            Projectile.height = 3;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = WulfrumPackPlayer.HookUpdates;
            Projectile.netImportant = true;
            Projectile.aiStyle = ProjAIStyleID.Hook; //The projectile uses entirely custom AI, but for some reason terraria's only way to distinguish what is and isnt a hook is its ai style.
        }

        public override bool? CanDamage() => false;

        public override bool PreAI()
        {
            if (!Main.dedServ)
            {
                TrailRenderer = TrailRenderer ?? new PrimitiveTrail(30, PrimWidthFunction, PrimColorFunction);
                Vector2[] segmentPositions = new Vector2[] { Projectile.Center, Owner.Center };
                if (State == HookState.Grappling && Owner.GetModPlayer<WulfrumPackPlayer>().Segments != null)
                    segmentPositions = Owner.GetModPlayer<WulfrumPackPlayer>().Segments.Select(x => x.position).ToArray();

                TrailRenderer.SetPositions(segmentPositions, SmoothBezierPointRetreivalFunction);
                TrailRenderer.NextPosition = Projectile.Center + (Projectile.Center - Owner.Center).SafeNormalize(Vector2.Zero);
            }

            return false;
        }

        public override void PostAI()
        {
            Lighting.AddLight(Projectile.Center, Color.DeepSkyBlue.ToVector3());
            Vector2 BetweenOwner = Owner.Center - Projectile.Center;

            if (Owner.dead || Owner.stoned || Owner.webbed || Owner.frozen || BetweenOwner.Length() > 1500)
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation = BetweenOwner.ToRotation() - MathHelper.PiOver2;

            if (Owner.GetModPlayer<WulfrumPackPlayer>().wulfrumPackEquipped)
            {
                Projectile.timeLeft = 2;
            }

            if (State == HookState.Thrown)
            {
                //Retract if too far.
                if (MaxReach < BetweenOwner.Length())
                    State = HookState.Retracting;

                float fallSpeed = Projectile.velocity.Y;

                if (Timer > 15 * Projectile.extraUpdates)
                    Projectile.velocity += Vector2.UnitY * 0.5f * (1 - Math.Clamp((Timer - 15) / 35f, 0f, 1f)) / Projectile.extraUpdates;

                Projectile.velocity *= 0.98f;

                if (Projectile.velocity.Y + 0.001 > 0)
                    Projectile.velocity.Y = Math.Clamp(Projectile.velocity.Y, 0, Math.Max(18f, fallSpeed));

                if (Projectile.velocity.Length() < 1f)
                    State = HookState.Retracting;

                CheckForGrapplableTiles();

            }

            else if (State == HookState.Retracting)
            {
                 Projectile.velocity = BetweenOwner.SafeNormalize(Vector2.One) * WulfrumPackPlayer.ReturnVelocity;
                Projectile.Center += Vector2.UnitY * 0.5f;

                //if (Main.dedServ)
                //    NetMessage.SendData(MessageID.ShimmerActions, Projectile.owner, -1, null, 0, Projectile.Center.X, Projectile.Center.Y);

                if (BetweenOwner.Length() < 25f)
                    Projectile.Kill();
            }

            else
            {
                float lenghtToOwner = BetweenOwner.Length();

                if (lenghtToOwner > Owner.GetModPlayer<WulfrumPackPlayer>().SwingLenght + 60f && Main.myPlayer == Projectile.whoAmI)
                {
                    State = HookState.Retracting;
                    Projectile.netUpdate = true;
                    Projectile.netSpam = 0;
                }

                Point tilePos = Projectile.Center.ToTileCoordinates();
                Tile tile = Main.tile[tilePos];
                if (!tile.HasUnactuatedTile || !tile.CanTileBeLatchedOnTo() || Owner.IsBlacklistedForGrappling(tilePos))
                    State = HookState.Retracting;

                Projectile.velocity = Vector2.Zero;

                if (Owner.grapCount < 10)
                {
                    Owner.grappling[Owner.grapCount] = Projectile.whoAmI;
                    Owner.grapCount++;
                }

                Owner.GetModPlayer<WulfrumPackPlayer>().grappleProjectileIndex = Projectile.whoAmI;
            }

            Timer++;
        }

        public void CheckForGrapplableTiles()
        {
            Vector2 hitboxStart = Projectile.Center - new Vector2(5f);
            Vector2 hitboxEnd = Projectile.Center + new Vector2(5f);

            Point topLeftTile = (hitboxStart - new Vector2(16f)).ToTileCoordinates();
            Point bottomRightTile = (hitboxEnd + new Vector2(32f)).ToTileCoordinates();

            for (int x = topLeftTile.X; x < bottomRightTile.X; x++)
            {
                for (int y = topLeftTile.Y; y < bottomRightTile.Y; y++)
                {
                    Vector2 worldPos = new Vector2(x * 16f, y * 16f);
                    Point tilePos = new Point(x, y);

                    //Ignore tiles that arent being collided with
                    if (!(hitboxStart.X + 10f > worldPos.X) || !(hitboxStart.X < worldPos.X + 16f) || !(hitboxStart.Y + 10f > worldPos.Y) || !(hitboxStart.Y < worldPos.Y + 16f))
                        continue;

                    Tile tile = Main.tile[tilePos];

                    if (!tile.HasUnactuatedTile || !tile.CanTileBeLatchedOnTo() || Owner.IsBlacklistedForGrappling(tilePos))
                        continue;

                    OnGrapple(worldPos, x, y);

                    break;
                }

                if (State == HookState.Grappling)
                    break;
            }
        }

        public void OnGrapple(Vector2 grapplePos, int x, int y)
        {
            WulfrumPackPlayer mp = Owner.GetModPlayer<WulfrumPackPlayer>();

            //Clear previous grapples
            if (mp.grappleProjectileIndex >= 0 && Main.projectile[mp.grappleProjectileIndex].active && 
                Main.projectile[mp.grappleProjectileIndex].owner == Owner.whoAmI && 
                Main.projectile[mp.grappleProjectileIndex].ModProjectile is WulfrumHook hook && hook.State == HookState.Grappling)
                Main.projectile[mp.grappleProjectileIndex].Kill();

            //Hook onto the tile
            Projectile.velocity = Vector2.Zero;
            State = HookState.Grappling;
            Projectile.Center = grapplePos + Vector2.One * 8f;
            //effects
            WorldGen.KillTile(x, y, fail: true, effectOnly: true);
            SoundEngine.PlaySound(SoundID.Dig, grapplePos);
            SoundEngine.PlaySound(WulfrumAcrobaticsPack.GrabSound, grapplePos);

            if (Owner.grapCount < 10)
            {
                Owner.grappling[Owner.grapCount] = Projectile.whoAmI;
                Owner.grapCount++;
            }

            mp.SwingLenght = (Owner.Center - Projectile.Center).Length();
            mp.OldPosition = Owner.Center - Owner.velocity;
            mp.SetSegments(Projectile.Center);
            mp.grappleProjectileIndex = Projectile.whoAmI;

            Rectangle? tileVisualHitbox = WorldGen.GetTileVisualHitbox(x, y);
            if (tileVisualHitbox.HasValue)
                Projectile.Center = tileVisualHitbox.Value.Center.ToVector2();

            if (Main.myPlayer == Owner.whoAmI)
            {
                Projectile.netUpdate = true;
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Owner.whoAmI);
                new SyncWulfrumHookSimulation().Send(-1, -1, false);
            }
        }


        public override void OnKill(int timeLeft)
        {
            if (Owner.grappling[0] == Projectile.whoAmI)
            {
                Owner.grappling[0] = -1;
                Owner.grapCount = 0;
            }
        }

        public float PrimWidthFunction(float completionRatio)
        {
            return 1.6f;
        }
        public Color PrimColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.DeepSkyBlue, Color.GreenYellow, (float)Math.Pow(completionRatio, 1.5D));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0, 0);
            return false;
        }

        public override bool PreDrawExtras() => false; //Prevents vanilla chain drawing from taking place

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            TrailRenderer?.Render();
        }
    }
}
