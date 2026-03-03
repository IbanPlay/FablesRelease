using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Dusts;
using ReLogic.Utilities;
using System.IO;
using Terraria.Localization;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    [ReplacingCalamity("SeaboundStaff")]
    public class StormlionWhip : ModItem
    {
        public static readonly SoundStyle HookSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "StormlionWhipHook");
        public static readonly SoundStyle YankSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "StormlionWhipYank");

        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        public static float MAX_TETHER_RANGE = 16 * 40;
        public static float DAMAGE_TO_FULLY_CHARGE = 75;
        public static float SECONDS_TO_FULLY_CHARGE = 8;
        public static int RETURN_TIME = 8;

        public static int TAG_DAMAGE = 6;

        public static int TICK_DAMAGE = 5;
        public static int TICK_FREQUENCY = 30;

        public static float MIN_BLAST_MULT = 2;
        public static float MAX_BLAST_MULT = 4;
        public static int BLAST_RADIUS = 115;

        public static LocalizedText ElectroBlastDamageText;
        public static LocalizedText TickDamageText;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion Whip");
            Tooltip.SetDefault("{0} summon tag damage while hooked\n" +
                               "Your summons will focus struck enemies\n" +
                               "Hold left click to hook the whip into enemies\n" +
                               "Hooked enemies build up static electricity, minion damage expedites this\n" +
                               "Release left click to detonate static electricity");
            Item.ResearchUnlockCount = 1;

            ElectroBlastDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.DetonationDamage");
            TickDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.StormlionWhipTickDamage");
        }

        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<StormlionWhipProjectile>(), 15, 1.2f, 5f, 34);

            Item.autoReuse = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 40);
            Item.channel = true;
        }

        public override bool CanUseItem(Player player)
        {
            //itll return false in multiplayer, instantly killing the whip because the itemanimation is 0
            if (Main.myPlayer != player.whoAmI)
                return true;

            return player.ownedProjectileCounts[Item.shoot] == 0;
        }

        public override bool MeleePrefix() => true;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TAG_DAMAGE);

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");
            int baseDamage = Main.LocalPlayer.GetWeaponDamage(Item, true);

            TooltipLine ElectroBlastDamage = new TooltipLine(Mod, "CalamityFables:BlastDamage", ElectroBlastDamageText.Format((int)(baseDamage * MIN_BLAST_MULT), (int)(baseDamage * MAX_BLAST_MULT)));
            TooltipLine TickDamage = new TooltipLine(Mod, "CalamityFables:TickDamage", TickDamageText.Format(TICK_DAMAGE));

            ElectroBlastDamage.OverrideColor = Color.Lerp(Color.White, Color.Turquoise, MathF.Pow(0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2), 4));
            TickDamage.OverrideColor = Color.Lerp(Color.White, Color.Turquoise, MathF.Pow(0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2 + MathHelper.Pi), 4));

            tooltips.Insert(damageIndex + 1, ElectroBlastDamage);
            tooltips.Insert(damageIndex + 2, TickDamage);
        }
    }

    public class StormlionWhipProjectile : BaseWhip
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;
        public static Asset<Texture2D> OutlineTexture;
        public static Asset<Texture2D> WhiteTexture;

        #region Fields and properties
        public enum AiState
        {
            Whipping,
            Hooked,
            ReelingBack
        }
        public AiState AIState {
            get {
                if (Projectile.ai[1] < 0)
                    return AiState.ReelingBack;

                if (hookNPC != default(NPC) && hookNPC != null)
                    return AiState.Hooked;

                return AiState.Whipping;
            }

            set {
                if (value == AiState.ReelingBack)
                    Projectile.ai[1] = -1;
                if (value == AiState.Hooked)
                    Projectile.ai[1] = 1;
            }
        }

        public float Charge {
            get => Math.Clamp(Projectile.ai[1] - 1, 0, 1);
            set => Projectile.ai[1] = value + 1;
        }

        public Vector2 npcHookOffset = Vector2.Zero; //Used to determine the offset from the hooked npc's center
        public float npcHookRotation; //Stores the projectile's rotation when hitting an npc
        public NPC hookNPC; //The npc the projectile is hooked into
        public int tickDamageTimer = 0;

        internal List<Vector2> cache;
        internal PrimitiveTrail TrailDrawer = null;
        internal SlotId electroLoopSlot;
        public bool hasBlinked = false;
        public float blinkTimer;
        public bool hasDoneHitSound = false;
        #endregion

        public bool CanBeUsed => Owner.active && !Owner.dead && !Owner.noItems && !Owner.CCed && Owner.channel;


        #region Loading and defaults
        public override void Load()
        {
            base.Load();
            FablesNPC.OnHitByProjectileEvent += RegisterElectricCharge;
            FablesNPC.ModifyHitByProjectileEvent += TagDamageWhenHooked;

            if (Main.dedServ)
                return;
            OutlineTexture = ModContent.Request<Texture2D>(Texture + "Outline");
            WhiteTexture = ModContent.Request<Texture2D>(Texture + "White");
        }

        public StormlionWhipProjectile() : base("Stormlion Whip", 20, 0.85f, Color.Black, 2) { }

        public override void SafeSetDefaults()
        {
            Projectile.localNPCHitCooldown = 20 * Projectile.MaxUpdates;
        }

        public override bool? CanDamage()
        {
            //Can damage as normal when whipping
            if (AIState == AiState.Whipping)
                return null;

            return false;
        }

        public override bool ShouldUpdatePosition() => false;
        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            if (hookNPC == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(hookNPC.whoAmI);
            writer.Write(npcHookRotation);
            writer.WriteVector2(npcHookOffset);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int npcID = reader.ReadInt32();
            if (npcID == -1)
                return;

            hookNPC = Main.npc[npcID];
            npcHookRotation = reader.ReadSingle();
            npcHookOffset = reader.ReadVector2();
        }

        public override bool PreAI()
        {
            blinkTimer -= 1 / (120f * 0.3f);

            if (AIState == AiState.Hooked)
            {
                //Stay stuck
                Projectile.timeLeft = 2;
                Owner.heldProj = Projectile.whoAmI;
                Owner.itemTime = Owner.itemAnimation = Owner.itemAnimationMax;
                AnimationLength = Owner.itemAnimationMax * Projectile.MaxUpdates;

                StuckAI();
                return false;
            }

            if (AIState == AiState.ReelingBack)
            {
                blinkTimer -= 1 / (120f * 0.4f); //Blink goes off even faster

                Owner.heldProj = Projectile.whoAmI;
                Owner.itemTime = Owner.itemAnimation = 2;
                GoBackAI();
                return false;
            }

            return base.PreAI();
        }

        public override void ArcAI()
        {
            if (Projectile.ai[0] > 0 && Projectile.ai[0] > MiddleOfArc - 5 && Projectile.ai[0] < MiddleOfArc + 10)
            {
                float dustScale = 0.3f + 0.7f * (float)Math.Sin((Projectile.ai[0] - (MiddleOfArc - 5)) / 15f * MathHelper.Pi);
                int dustType = Main.rand.NextBool() ? DustID.Electric : ModContent.DustType<ElectroDust>();

                Dust d = Dust.NewDustPerfect(EndPoint, dustType, Vector2.Zero, 0, Color.GhostWhite, dustScale * 0.5f);
                d.noGravity = true;
                d.position += Main.rand.NextVector2Circular(1, 8).RotatedBy(Projectile.rotation);
                d.velocity += new Vector2(0f, -Main.rand.Next(1, 3)).RotatedBy(Projectile.rotation).RotatedByRandom(0.5f);
                if (dustType != DustID.Electric)
                    d.scale *= 0.4f;
            }

            if ((HitboxActive || Projectile.ai[0] > MiddleOfArc) && !Main.dedServ)
            {
                ManageCache();
                ManageTrail();
            }
        }

        #region Hooked in
        public void StuckAI()
        {
            //StormlionWhip.DAMAGE_TO_FULLY_CHARGE = 10;

            bool canStrikeEnemy = hookNPC != null && hookNPC.active && !hookNPC.dontTakeDamage;
            bool hookTooFar = hookNPC != null && Owner.Distance(hookNPC.Center) > StormlionWhip.MAX_TETHER_RANGE;

            //Return to player if player stops holding attack button, way too much distance is between the player and hooked npc, or if the hooked npc dies/becomes invulnerable
            if (!CanBeUsed || hookTooFar || !canStrikeEnemy)
            {
                if (hookTooFar)
                    Charge += 0.15f;

                BlastOff();
                cache = null;
                AIState = AiState.ReelingBack;
                Projectile.timeLeft = Projectile.MaxUpdates * StormlionWhip.RETURN_TIME;
                Projectile.velocity = Projectile.Center - Owner.MountedCenter;
                Projectile.netUpdate = true;
                return;
            }

            Charge += 1 / (120f * StormlionWhip.SECONDS_TO_FULLY_CHARGE);

            if (Charge == 1 && !hasBlinked)
            {
                hasBlinked = true;
                blinkTimer = 1f;
                SoundEngine.PlaySound(TornElectrosac.ChargeSound, Owner.Center);
            }


            Projectile.hide = true;
            Projectile.Center = hookNPC.Center + npcHookOffset;
            Projectile.rotation = npcHookRotation;
            Projectile.velocity = Vector2.Zero;
            Owner.ChangeDir((Projectile.Center.X - Owner.Center.X).NonZeroSign());
            Projectile.spriteDirection = Owner.direction;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Owner.MountedCenter.AngleTo(Projectile.Center) - MathHelper.PiOver2);

            Projectile.WhipPointsForCollision.Clear();
            Projectile.FillWhipControlPoints(Projectile, Projectile.WhipPointsForCollision);

            //Tick electrification
            tickDamageTimer++;
            if (Main.myPlayer == Projectile.owner && tickDamageTimer % (StormlionWhip.TICK_FREQUENCY * Projectile.MaxUpdates) == 0)
            {
                var modifier = hookNPC.GetIncomingStrikeModifiers(null, Math.Sign(Owner.DirectionTo(Projectile.Center).X));
                modifier.ScalingArmorPenetration += 1;

                hookNPC.ModifiableStrikeNPC(modifier, StormlionWhip.TICK_DAMAGE);
            }

            if (Main.rand.NextBool(2 + (int)((1 - Charge) * 10)))
            {
                int dustSegment = Main.rand.Next(Segments);

                float dustScale = 0.3f + 0.7f * (float)Math.Sin((Projectile.ai[0] - (MiddleOfArc - 5)) / 15f * MathHelper.Pi);
                int dustType = Main.rand.NextBool() ? DustID.Electric : ModContent.DustType<ElectroDust>();

                Dust d = Dust.NewDustPerfect(Projectile.WhipPointsForCollision[dustSegment], dustType, Vector2.Zero, 0, Color.GhostWhite, dustScale * 0.5f);
                d.noGravity = true;
                d.position += Main.rand.NextVector2Circular(1, 8).RotatedBy(Projectile.rotation);
                d.velocity += new Vector2(0f, -Main.rand.Next(1, 3)).RotatedBy(Projectile.rotation).RotatedByRandom(0.5f);
                if (dustType != DustID.Electric)
                    d.scale *= 0.4f;
            }

            if (!hasDoneHitSound)
            {
                hasDoneHitSound = true;
                SoundEngine.PlaySound(StormlionWhip.HookSound, Projectile.Center);
            }

            //Electric sound loop
            if (Main.myPlayer == Projectile.owner)
            {
                if (SoundEngine.TryGetActiveSound(electroLoopSlot, out var sound))
                {
                    sound.Position = Projectile.Center;
                    sound.Volume = Charge;
                    sound.Update();
                }
                else
                    electroLoopSlot = SoundEngine.PlaySound(DesertScourge.ElectroLoopSound, Projectile.Center);

                SoundHandler.TrackSound(electroLoopSlot);
            }
        }

        public void BlastOff()
        {
            //Applies knockback (packet is executed on client which applies the knockback as well
            if (hookNPC != null && hookNPC.knockBackResist != 0)
                new KnockbackNPCPacket(hookNPC.whoAmI, hookNPC.SafeDirectionTo(Owner.MountedCenter) * 9f * (float)Math.Pow(hookNPC.knockBackResist, 0.3f)).Send();

            SoundEngine.PlaySound(StormlionWhip.YankSound, Projectile.Center);

            for (int direction = -1; direction <= 1; direction += 2)
            {
                for (int i = 0; i < 18; i++)
                {
                    Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                    float dustAngle = (float)Math.Pow(Main.rand.NextFloat(), 3f);
                    if (Main.rand.NextBool())
                        dustAngle *= -1;

                    Vector2 dustSpeed = Projectile.SafeDirectionFrom(Owner.Center).RotatedBy(dustAngle * MathHelper.PiOver4) * Main.rand.NextFloat(1f, 1.6f) * direction;
                    dustSpeed *= 1f + (1 - Math.Abs(dustAngle)) * 4f;

                    if (direction == -1)
                        dustSpeed *= 2;

                    int dustType = Main.rand.NextBool(4) ? DustID.Electric : DustID.Blood;

                    Dust bust = Dust.NewDustPerfect(dustPosition, dustType, dustSpeed, Scale: Main.rand.NextFloat(0.8f, 2.2f));
                    if (dustType == DustID.Electric)
                    {
                        bust.noGravity = true;
                        bust.scale *= 0.6f;
                    }
                }
            }

            if (Main.myPlayer == Projectile.owner)
            {
                if (Charge > 0.5f)
                {
                    int blast_damage = (int)(MathHelper.Lerp(StormlionWhip.MIN_BLAST_MULT, StormlionWhip.MAX_BLAST_MULT, Utils.GetLerpValue(0.5f, 1f, Charge, true)) * Projectile.damage);
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MiniElectroblast>(), blast_damage, 0, Main.myPlayer, StormlionWhip.BLAST_RADIUS);
                }

                float yankStrenght = 8f + 10f * Charge;
                yankStrenght += Utils.GetLerpValue(StormlionWhip.MAX_TETHER_RANGE * 0.5f, StormlionWhip.MAX_TETHER_RANGE, Owner.Distance(Projectile.Center), true) * 10f;

                CameraManager.AddCameraEffect(new DirectionalCameraTug(Owner.SafeDirectionFrom(EndPoint) * -yankStrenght, 3f, 25, easingDegree: 4));
            }
        }
        #endregion

        #region Returning
        public void GoBackAI()
        {
            Projectile.rotation = Projectile.AngleFrom(Owner.Center) + MathHelper.PiOver2;
            Projectile.direction = Projectile.spriteDirection = Owner.direction;

            float progress = Projectile.timeLeft / (float)(StormlionWhip.RETURN_TIME * Projectile.MaxUpdates);
            Projectile.Center = Owner.MountedCenter + Projectile.velocity * PolyInEasing(progress);

            if (Main.dedServ)
                return;
            ManageCache();
            ManageTrail();
        }
        #endregion

        public override void CustomizeWhipControlPoints(List<Vector2> controlPoints)
        {
            if (AIState != AiState.Whipping)
            {
                controlPoints.Clear();
                Vector2 playerArmPosition = Owner.MountedCenter + Owner.SafeDirectionTo(Projectile.Center) * 14f;

                for (int i = 0; i < Segments + 1; i++)
                {
                    float lerper = i / (float)Segments;
                    controlPoints.Add(Vector2.Lerp(playerArmPosition, Projectile.Center, lerper));
                }
            }
        }

        #region Hit stuff
        public static bool FindPlayerHook(Player player, out Projectile projectile, out StormlionWhipProjectile stormWhip)
        {
            projectile = null;
            stormWhip = null;
            int stormWhipType = ModContent.ProjectileType<StormlionWhipProjectile>();

            //If the player has no stormlion whips, return false
            if (player.ownedProjectileCounts[stormWhipType] == 0)
                return false;

            //If the player has a held projectile (which should be the whip if it was spawned before the projectile checking, since itd be earlier in the update cycle)
            if (player.heldProj >= 0 && player.heldProj < Main.maxProjectiles)
            {
                projectile = Main.projectile[player.heldProj];
                if (projectile.type != stormWhipType) //If the projctile somehow isnt the stormlion whip, give up
                    return false;

                //Else, simply return the held projectile
                stormWhip = projectile.ModProjectile as StormlionWhipProjectile;
                return true;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                projectile = Main.projectile[i];
                if (projectile.owner == player.whoAmI && projectile.type == stormWhipType)
                {
                    player.heldProj = projectile.whoAmI; //Set the held projectile properly early to cut down on further loops.
                    stormWhip = projectile.ModProjectile as StormlionWhipProjectile;
                    return true;
                }
            }

            return false;
        }

        private void RegisterElectricCharge(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[projectile.owner];
            if (!FindPlayerHook(owner, out Projectile heldProj, out StormlionWhipProjectile stormWhip))
                return;

            if (heldProj.ai[1] >= 1 && stormWhip.hookNPC.whoAmI == npc.whoAmI)
            {
                if (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type] || projectile.sentry)
                {
                    heldProj.ai[1] += hit.SourceDamage / StormlionWhip.DAMAGE_TO_FULLY_CHARGE;
                    heldProj.netUpdate = true;
                }
            }
        }

        private void TagDamageWhenHooked(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[projectile.owner];
            if (!FindPlayerHook(owner, out Projectile heldProj, out StormlionWhipProjectile stormWhip))
                return;

            if (heldProj.ai[1] >= 1 && stormWhip.hookNPC.whoAmI == npc.whoAmI)
            {
                modifiers.FlatBonusDamage += StormlionWhip.TAG_DAMAGE;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            //Hook into npc on hit
            if (AIState == AiState.Whipping && Owner.channel)
            {
                Projectile.Center = Utils.ClosestPointInRect(target.Hitbox, Projectile.Center);

                hookNPC = target;
                npcHookRotation = Projectile.rotation;
                npcHookOffset = (Projectile.Center - target.Center) * 0.4f; //Decrease offset from what it normally would be due to big hitbox
                AIState = AiState.Hooked;
                Projectile.netUpdate = true;
                Owner.MinionAttackTargetNPC = target.whoAmI;
            }
        }
        #endregion

        #region Prims
        public void ManageCache()
        {
            Vector2 desiredPosition = Projectile.Center;
            if (AIState == AiState.Whipping)
                desiredPosition = EndPoint;

            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();
                for (int i = 0; i < 20; i++)
                {
                    cache.Add(desiredPosition);
                }
            }

            cache.Add(desiredPosition);

            while (cache.Count > 20)
            {
                cache.RemoveAt(0);
            }
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WhipWidthFunction, WhipColorFunction);
            TrailDrawer.SetPositionsSmart(cache, EndPoint, RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;
        }


        internal static float WhipWidthFunction(float completionRatio)
        {
            float baseWidth = (10f * (float)Math.Pow(completionRatio, 0.3f));  //Width tapers off at the end
            baseWidth *= (1 + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7) * 0.3f); //Width oscillates
            return baseWidth;
        }

        internal static Color WhipColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(new Color(66, 144, 212) * 0.05f, new Color(55, 176, 251), completionRatio);
            color *= (float)Math.Pow(completionRatio, 1.2f);
            return color;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || cache == null)
                return;

            GhostTrail clone = new GhostTrail(cache, TrailDrawer, 0.3f, Owner, "Primitive_IntensifiedTextureMap", delegate (Effect effect, float fading) {
                effect.Parameters["repeats"].SetValue(4);
                effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "DoubleTrail").Value);
            });
            GhostTrailsHandler.LogNewTrail(clone);
        }
        #endregion

        #region Whip Drawing
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Projectile.hide)
                behindNPCs.Add(index);
        }

        public override bool ShouldDrawSegment(int segment) => true;
        public override Vector2 SegmentScale(int segment, Vector2 difference, Rectangle frame)
        {
            if (AIState == AiState.Whipping)
                return Vector2.One * 0.8f; //Smaller segments when swung

            //Stretching segments when stuck
            Vector2 scale = Vector2.One;
            scale.Y = difference.Length() / frame.Height * 4f;
            scale.Y = Math.Min(scale.Y, 1.3f);

            if (scale.Y > 1)
                scale.X -= scale.Y - 1;

            return scale;
        }

        public override void DrawBehindWhip(ref Color lightColor)
        {
            if (AIState == AiState.Hooked && !DrawStateTrackerSystem.drawingCachedProjectiles)
                return;

            if (AIState != AiState.Hooked)
            {
                Effect effect = AssetDirectory.PrimShaders.IntensifiedTextureMap;
                effect.Parameters["repeats"].SetValue(4);
                effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "DoubleTrail").Value);
                TrailDrawer?.Render(effect, -Main.screenPosition);
            }

            if (blinkTimer <= 0)
                return;

            Texture2D texture = OutlineTexture.Value;
            Rectangle whipFrame = texture.Frame(xFrames, yFrames, xFrame, 0);
            int height = whipFrame.Height;
            Vector2 firstPoint = PointsForDrawing[0];
            Color color = Color.Lerp(Color.White, Color.DodgerBlue, (float)Math.Pow(1 - blinkTimer, 0.5f));

            for (int i = 0; i < PointsForDrawing.Count - 1; i++)
            {
                Vector2 origin = whipFrame.Size() * 0.5f;
                bool draw = true;

                if (i == 0)
                {
                    origin.Y += HandleOffset;
                }
                else if (i == PointsForDrawing.Count - 2)
                {
                    whipFrame.Y = height * (yFrames - 1);
                }
                else
                {
                    whipFrame.Y = height * MidSegmentFrame(i);
                    draw = ShouldDrawSegment(i);
                }

                Vector2 difference = PointsForDrawing[i + 1] - PointsForDrawing[i];

                if (draw)
                {
                    float rotation = difference.ToRotation() - MathHelper.PiOver2;
                    SpriteEffects effect = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 scale = Vector2.One;
                    if (i > 0 && i < PointsForDrawing.Count - 2)
                        scale = SegmentScale(i, difference, whipFrame);

                    scale.X *= 1.2f;
                    Main.EntitySpriteDraw(texture, PointsForDrawing[i] - Main.screenPosition, whipFrame, color * blinkTimer, rotation, origin, scale * Projectile.scale, effect, 0);
                }

                firstPoint += difference;
            }

            base.DrawBehindWhip(ref lightColor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (AIState == AiState.Hooked && !DrawStateTrackerSystem.drawingCachedProjectiles)
                return false;
            base.PreDraw(ref lightColor);
            if (blinkTimer <= 0f)
                return false;

            Texture2D texture = WhiteTexture.Value;
            Rectangle whipFrame = texture.Frame(xFrames, yFrames, xFrame, 0);
            int height = whipFrame.Height;
            Vector2 firstPoint = PointsForDrawing[0];
            Color color = Color.Lerp(Color.White, Color.DodgerBlue, (float)Math.Pow(1 - blinkTimer, 2.5f));
            color.A = 0;

            for (int i = 0; i < PointsForDrawing.Count - 1; i++)
            {
                Vector2 origin = whipFrame.Size() * 0.5f;
                bool draw = true;

                if (i == 0)
                {
                    origin.Y += HandleOffset;
                }
                else if (i == PointsForDrawing.Count - 2)
                {
                    whipFrame.Y = height * (yFrames - 1);
                }
                else
                {
                    whipFrame.Y = height * MidSegmentFrame(i);
                    draw = ShouldDrawSegment(i);
                }

                Vector2 difference = PointsForDrawing[i + 1] - PointsForDrawing[i];

                if (draw)
                {
                    float rotation = difference.ToRotation() - MathHelper.PiOver2;
                    SpriteEffects effect = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 scale = Vector2.One;
                    if (i > 0 && i < PointsForDrawing.Count - 2)
                        scale = SegmentScale(i, difference, whipFrame);

                    scale.X *= 1.2f;
                    Main.EntitySpriteDraw(texture, PointsForDrawing[i] - Main.screenPosition, whipFrame, color * blinkTimer, rotation, origin, scale * Projectile.scale, effect, 0);
                }

                firstPoint += difference;
            }

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color? defaultAlpha = base.GetAlpha(lightColor);

            if (AIState == AiState.Hooked)
            {
                float lenght = PointsForDrawing[0].Distance(PointsForDrawing[PointsForDrawing.Count - 1]);
                float stretchCritical = Utils.GetLerpValue(StormlionWhip.MAX_TETHER_RANGE * 0.8f, StormlionWhip.MAX_TETHER_RANGE, lenght, true);
                if (stretchCritical > 0)
                {
                    if (!defaultAlpha.HasValue)
                        defaultAlpha = lightColor;

                    defaultAlpha = Color.Lerp(defaultAlpha.Value, Color.OrangeRed, stretchCritical * 0.8f * (float)(0.5 + 0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 8f)));
                }
            }

            return defaultAlpha;
        }
        #endregion
    }
}
