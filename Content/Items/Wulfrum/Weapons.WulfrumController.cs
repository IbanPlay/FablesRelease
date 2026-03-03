using CalamityFables.Content.Buffs.Summon;
using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumController")]
    public class WulfrumController : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Controller");
            Tooltip.SetDefault("Summons a wulfrum droid to fight for you\n" +
                "Hold right click while holding the remote to switch all of your drones into supercharge mode\n" +
                "Supercharged droids will stop attacking and focus wulfrum energy onto you\n" +
                "The beam provides extra regeneration and defense\n" +
                "Can also be used to heal other players by keeping your mouse cursor close enough to them");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.mana = 10;
            Item.width = 28;
            Item.height = 20;
            Item.useTime = Item.useAnimation = 34;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.knockBack = 0.5f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item15; //phaseblade sound effect
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WulfrumDroid>();
            Item.buffType = ModContent.BuffType<WulfrumDroidBuff>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Summon;
        }

        public override void HoldItem(Player player)
        {
            player.SyncRightClick();
            player.SyncMousePosition();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                player.AddBuff(Item.buffType, 2);

                position = Main.MouseWorld;
                velocity.X = 0;
                velocity.Y = 0;
                int droid = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (Main.projectile.IndexInRange(droid))
                    Main.projectile[droid].originalDamage = Item.damage;
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>(9).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class WulfrumControllerPlayer : ModPlayer
    {
        public int buffingDrones = 0;

        public override void UpdateDead()
        {
            buffingDrones = 0;
        }

        public override void UpdateLifeRegen()
        {
            if (buffingDrones > 0)
            {
                Player.lifeRegen += buffingDrones * 3;
                Player.statDefense += buffingDrones * 3;

                buffingDrones = 0;
            }
        }

        public override void PostUpdateMiscEffects()
        {
            if (buffingDrones > 0 && Main.rand.NextBool(3))
            {
                Vector2 dustPos = Player.position + (Player.height * Main.rand.NextFloat(0.7f, 1f) + Player.gfxOffY) * Vector2.UnitY + Vector2.UnitX * Main.rand.NextFloat() * Player.width;

                Dust chust = Dust.NewDustPerfect(dustPos, 274, -Vector2.UnitY * Main.rand.NextFloat(1.4f, 7f) + Player.velocity, Alpha: 100, Scale: Main.rand.NextFloat(1.2f, 1.8f));
                chust.noGravity = true;
                chust.noLight = true;
            }
        }
    }


    public class WulfrumDroid : ModProjectile
    {
        public static readonly SoundStyle HelloSound = new(SoundDirectory.Wulfrum + "WulfrumDroidSpawnBeep") { PitchVariance = 0.4f };
        public static readonly SoundStyle PewSound = new(SoundDirectory.Wulfrum + "WulfrumDroidFire") { PitchVariance = 0.4f, Volume = 0.6f, MaxInstances = 0 };
        public static readonly SoundStyle RandomChirpSound = new(SoundDirectory.Wulfrum + "WulfrumDroidChirp", 4) { PitchVariance = 0.3f };
        public static readonly SoundStyle HurrySound = new(SoundDirectory.Wulfrum + "WulfrumDroidHurry", 2) { PitchVariance = 0.3f };
        public static readonly SoundStyle RepairSound = new(SoundDirectory.Wulfrum + "WulfrumDroidRepair") { Volume = 0.8f, PitchVariance = 0.3f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };
        public int NewSoundDelay {
            get {
                int minionCount = 1;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == Owner.whoAmI && proj.type == Type && proj.whoAmI != Projectile.whoAmI)
                    {
                        minionCount++;
                    }
                }

                return (int)(Main.rand.Next(340, 1460) * minionCount);
            }
        }

        public static Asset<Texture2D> MuzzleFlashAsset;
        public static Asset<Texture2D> GunBarrelAsset;
        public static Asset<Texture2D> PropellerAsset;

        internal PrimitiveTrail TrailDrawer;
        internal Color PrimColorMult;
        public Player healedPlayer;

        public float Initialized = 0f;
        public static float AggroRange = 450f;
        public static float ShootDelay = 110f; //This is the max delay, but it can b e shorter
        public enum BehaviorState { Aggressive, Idle };
        public BehaviorState State {
            get => (BehaviorState)(int)Projectile.ai[0];

            set => Projectile.ai[0] = (float)value;
        }

        public ref float ShootTimer => ref Projectile.ai[1];
        public Player HealTarget
        {
            get
            {
                if (Projectile.ai[2] <= 0)
                    return null;
                return Main.player[(int)Projectile.ai[2] - 1];
            }
            set
            {
                if (value == null)
                    Projectile.ai[2] = 0;
                else
                    Projectile.ai[2] = value.whoAmI + 1;
            }
        }

        public ref float AyeAyeCaptainCooldown => ref Projectile.localAI[0];
        public ref float NuzzleFlashTime => ref Projectile.localAI[1];

        public float BuffModeBuffer = 0f;

        public NPC Target {
            get {
                NPC target = null;

                if (Owner.HasMinionAttackTargetNPC)
                    target = CheckNPCTargetValidity(Main.npc[Owner.MinionAttackTargetNPC]);

                if (target != null)
                    return target;

                else
                {
                    for (int npcIndex = 0; npcIndex < Main.npc.Length; npcIndex++)
                    {
                        target = CheckNPCTargetValidity(Main.npc[npcIndex]);
                        if (target != null)
                            return target;
                    }
                }

                return null;
            }
        }

        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Droid");
            Main.projFrames[Projectile.type] = 24;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 32;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Initialized = 0f;
        }

        //Returns the npc if targetable, returns null if not
        public NPC CheckNPCTargetValidity(NPC potentialTarget)
        {
            if (potentialTarget.CanBeChasedBy(this, false))
            {
                float targetDist = Vector2.Distance(potentialTarget.Center, Projectile.Center);

                if ((targetDist < AggroRange) && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, potentialTarget.position, potentialTarget.width, potentialTarget.height))
                {
                    return potentialTarget;
                }
            }

            return null;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            #region initialization
            //Spawn dust
            if (Initialized == 0f)
            {
                int dustAmt = Main.rand.Next(10, 16);
                for (int dustIndex = 0; dustIndex < dustAmt; dustIndex++)
                {
                    Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);

                    Dust wishyDust = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(1f, 8f), 229, Alpha: 100, Scale: Main.rand.NextFloat(1f, 1.4f));
                    wishyDust.noGravity = true;
                    wishyDust.noLight = true;
                    wishyDust.velocity = direction * Main.rand.NextFloat(2f, 4);
                }

                SoundEngine.PlaySound(HelloSound, Projectile.Center);
                Projectile.soundDelay = NewSoundDelay;
                ShootTimer = ShootDelay;

                Initialized++;
            }
            #endregion
            #region buff handling
            if (Owner.RightClicking() && Owner.HeldItem.type == ModContent.ItemType<WulfrumController>())
            {
                if (BuffModeBuffer > 0)
                {
                    BuffModeBuffer--;
                    Projectile.netUpdate = true;
                }
            }

            else if (BuffModeBuffer < 15)
                BuffModeBuffer = 15;
            bool buffMode = BuffModeBuffer <= 0;
            #endregion

            #region active check
            if (player.dead || !player.active) // This is the "active check", makes sure the minion is alive while the Player is alive, and despawns if not
                player.ClearBuff(ModContent.BuffType<WulfrumDroidBuff>());
            if (player.HasBuff(ModContent.BuffType<WulfrumDroidBuff>()))
                Projectile.timeLeft = 2;
            #endregion

            Projectile.MinionAntiClump();

            //Lets only recalculate our target once per frame aight
            NPC targetCache = buffMode ? null : Target;
            float separationAnxietyDist = buffMode ? 1200 : targetCache != null ? 1000f : 500f; //Have more lenience on how far away from the player should the drone return if theyre attacking an enemy

            //Makes a sound when switching to buff mode (with a cooldown to avoid the sound being spammed
            if (AyeAyeCaptainCooldown > 0)
                AyeAyeCaptainCooldown--;
            else if (buffMode)
            {
                SoundEngine.PlaySound(RepairSound, Projectile.Center);
                AyeAyeCaptainCooldown = 100;
            }

            //Random chirping sounds
            if (Projectile.soundDelay > 0)
                Projectile.soundDelay--;
            else
            {
                SoundEngine.PlaySound(RandomChirpSound with { Volume = RandomChirpSound.Volume * Main.rand.NextFloat(0.5f, 1f) }, Projectile.Center);
                Projectile.soundDelay = NewSoundDelay;

                if (targetCache == null)
                {
                    Vector2 emoteDirection = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 0.7f);

                    Particle emote = new WulfrumDroidEmote(Projectile.Center + emoteDirection * 10f, emoteDirection * Main.rand.NextFloat(3f, 5f), Main.rand.Next(30, 65), Main.rand.NextFloat(1.4f, 2f));
                    ParticleHandler.SpawnParticle(emote);
                }
            }


            //Return to the player if too far away from them. Hurry up droids!
            if (Vector2.Distance(Owner.Center, Projectile.Center) > separationAnxietyDist)
            {
                State = BehaviorState.Idle;
                Projectile.netUpdate = true;

                //play the hurry sound once and never again until it is reset
                if (Projectile.soundDelay < 10000)
                    SoundEngine.PlaySound(HurrySound, Projectile.Center);

                Projectile.soundDelay = 10010;

                if (Main.rand.NextBool(7))
                {
                    Vector2 emoteDirection = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 0.7f);
                    emoteDirection.X -= Math.Sign(Projectile.velocity.X) * 1f;

                    Particle emote = new WulfrumDroidSweatEmote(Projectile.Center + emoteDirection * 10f, emoteDirection * Main.rand.NextFloat(3f, 5f), Main.rand.Next(20, 35), Main.rand.NextFloat(1.4f, 2f));
                    ParticleHandler.SpawnParticle(emote);
                }
            }
            //If not hurrying up, reset the sound delay.
            else if (Projectile.soundDelay >= 10000)
                Projectile.soundDelay = NewSoundDelay;


            //Target the enemy
            if (targetCache != null && State == BehaviorState.Aggressive)
            {
                Vector2 vectorToTarget = targetCache.Center - Projectile.Center;
                float distanceToTarget = vectorToTarget.Length();
                vectorToTarget = vectorToTarget.SafeNormalize(Vector2.Zero);

                //Accelerate towards target if far away
                if (distanceToTarget > 200f)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, vectorToTarget * 6f, 1 / 41f);

                //"Float" the minion
                else if (Projectile.velocity.Y > -1f)
                    Projectile.velocity.Y -= 0.1f;

                //Small tweak that makes the minion hop away from the targets center if its too aligned with it vertically
                if (Math.Abs(Projectile.Center.X - targetCache.Center.X) < 10f)
                    Projectile.velocity.X += 4f * Math.Sign(Projectile.Center.X - targetCache.Center.X);
            }

            else
            {
                //Become passive if theres obstruction between it and the player
                if (!Collision.CanHitLine(Projectile.Center, 1, 1, Owner.Center, 1, 1))
                    State = BehaviorState.Idle;

                float returnSpeed = State == BehaviorState.Idle ? 15f : 6f;

                //Accelerate towards the player if too far & not fast enough
                Vector2 playerVec = Owner.Center - Projectile.Center - Vector2.UnitY * 60f;
                float playerDist = playerVec.Length();
                if (playerDist > 200f && returnSpeed < 9f)
                    returnSpeed = 9f;

                if (playerDist < 100f && State == BehaviorState.Idle && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    State = BehaviorState.Aggressive;
                    Projectile.netUpdate = true;
                }

                //Teleport to player if too far away.
                if (playerDist > 2000f)
                {
                    Projectile.Center = player.Center;
                    HealTarget = null;
                    Projectile.netUpdate = true;
                }

                else if (buffMode)
                {
                    AyeAyeCaptainCooldown = 50f;

                    Player playerToBuff = Owner;
                    float shortestMouseDistance = (Owner.MountedCenter - Owner.MouseWorld()).Length();

                    if (HealTarget == null)
                    {
                        if (Main.netMode == NetmodeID.SinglePlayer)
                            HealTarget = Main.LocalPlayer;

                        else
                        {
                            for (int i = 0; i < Main.maxPlayers; i++)
                            {
                                if (Main.player[i].active && !Main.player[i].dead && (Main.player[i].team == Owner.team || Main.player[i].team == 0))
                                {
                                    float mouseDistanceToPotentialTarget = (Main.player[i].MountedCenter - Owner.MouseWorld()).Length();
                                    float ownerDistanceToPotentialTarget = (Main.player[i].MountedCenter - Owner.Center).Length();

                                    if (ownerDistanceToPotentialTarget <= 1100 && shortestMouseDistance > mouseDistanceToPotentialTarget)
                                    {
                                        playerToBuff = Main.player[i];
                                        shortestMouseDistance = mouseDistanceToPotentialTarget;
                                    }
                                }
                            }

                            HealTarget = playerToBuff;

                            /*
                            if (playerToBuff != Owner && Main.rand.NextBool(4))
                            {
                                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);

                                Dust wishyDust = Dust.NewDustPerfect(playerToBuff.Center + direction * Main.rand.NextFloat(4f, 9f), 229, Alpha: 100, Scale: Main.rand.NextFloat(1f, 1.4f));
                                wishyDust.noGravity = true;
                                wishyDust.noLight = true;
                                wishyDust.velocity = direction * Main.rand.NextFloat(2f, 4);
                            }
                            */

                        }
                    }

                    healedPlayer = HealTarget;
                    float distanceToHealed = (healedPlayer.Center - Projectile.Center).Length();
                    float distanceToOwner = (Owner.Center - Projectile.Center).Length();

                    Vector2 aimPosition = healedPlayer.MountedCenter - Vector2.UnitY.RotatedBy((float)Math.Sin(Main.GlobalTimeWrappedHourly + Projectile.whoAmI) * MathHelper.PiOver2 * 0.9f) * 60f - Vector2.UnitY * 20f;
                    float distanceToAim = (aimPosition - Projectile.Center).Length();

                    if (distanceToAim > 50)
                    {
                        float speed = MathHelper.Lerp(10f, 30f, Math.Clamp((distanceToAim - 110f) / 400f, 0f, 1f));
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, (aimPosition - Projectile.Center).SafeNormalize(Vector2.Zero) * speed, 0.05f);
                    }

                    else
                    {
                        Projectile.velocity *= 0.98f;
                        if (Projectile.velocity == Vector2.Zero)
                            Projectile.velocity = (aimPosition - Projectile.Center).SafeNormalize(Vector2.Zero) * 5f;
                    }

                    if (distanceToHealed < 200f)
                    {
                        //Heal player
                        healedPlayer.GetModPlayer<WulfrumControllerPlayer>().buffingDrones++;

                        ShootTimer--;
                        if (ShootTimer <= 0)
                        {
                            ShootTimer = ShootDelay;

                            if (Main.rand.NextBool(3))
                            {
                                RoverDrivePlayer shieldMan = healedPlayer.GetModPlayer<RoverDrivePlayer>();
                                if (shieldMan.ProtectionMatrixDurability > 0 && shieldMan.ProtectionMatrixDurability < RoverDrive.ProtectionMatrixDurabilityMax)
                                {
                                    shieldMan.ProtectionMatrixDurability++;
                                    if (healedPlayer.FindCooldown(Cooldowns.WulfrumRoverDriveDurability.ID, out var cd))
                                        cd.timeLeft = shieldMan.ProtectionMatrixDurability;
                                }
                            }
                        }
                    }
                    //Detach from target if too far
                    else if (distanceToOwner > 1200 && HealTarget != Owner)
                    {
                        HealTarget = null;
                        Projectile.netUpdate = true;
                    }
                }

                else
                {
                    HealTarget = null;

                    //Aim towards the top of the player
                    if (playerDist > 70f)
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, playerVec.SafeNormalize(Vector2.Zero) * returnSpeed, 1 / 21f);

                    else
                    {
                        //Get unstuck if your movespeed is zero
                        if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
                            Projectile.velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * -0.15f;

                        //Accelerate more
                        Projectile.velocity *= 1.01f;
                    }
                }
            }



            if (buffMode && HealTarget != null)
            {
                Projectile.spriteDirection = (HealTarget.Center.X - Projectile.Center.X).NonZeroSign();

                Vector2 toHealTarget = (Projectile.Center - HealTarget.Center);
                toHealTarget.Y *= 0.4f;

                float targetRotation = toHealTarget.ToRotation();
                if (Projectile.spriteDirection == 1)
                    targetRotation += MathHelper.Pi;

                targetRotation += Projectile.velocity.X * 0.04f;

                Projectile.rotation = Utils.AngleLerp(Projectile.rotation, targetRotation, 0.2f);
            }
            else
            {
                if (targetCache == null)
                    Projectile.spriteDirection = Projectile.direction = Math.Sign(Projectile.velocity.X);
                else
                    Projectile.spriteDirection = (targetCache.Center.X - Projectile.Center.X).NonZeroSign();

                    Projectile.rotation = Projectile.velocity.X * 0.05f;
            }

            UpdateFrame(buffMode, targetCache);

            if (!buffMode)
                ChargeUpAndFire(targetCache);

            if (!Main.dedServ)
                ManageTrail();
        }


        private bool shortShoot = false;
        public void ChargeUpAndFire(NPC targetCache)
        {
            //Decrease the timer by a random number
            ShootTimer -= Main.rand.Next(1, 4);


            if (ShootTimer <= 0)
            {
                ShootTimer = ShootDelay;
                shortShoot = !shortShoot;
                if (shortShoot)
                    ShootTimer = ShootTimer * 0.1f;
                else 
                    ShootTimer = ShootTimer * 1.9f;


                Projectile.netUpdate = true;

                //Don't shoot if no target.
                if (targetCache == null)
                    return;

                NuzzleFlashTime = 1f;
                SoundEngine.PlaySound(PewSound, Projectile.Center);
                //Recoil
                Vector2 velocity = targetCache.Center - Projectile.Center;
                velocity.Normalize();
                velocity *= 10f;

                Projectile.velocity -= velocity * (shortShoot ? 0.3f : 0.1f);


                if (Main.myPlayer == Projectile.owner)
                {
                    int bolt = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<WulfrumEnergyBurst>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    Main.projectile[bolt].originalDamage = Projectile.originalDamage;
                    Main.projectile[bolt].netUpdate = true;
                    Projectile.netUpdate = true;
                }
            }
        }
        public void ManageTrail()
        {
            if (healedPlayer == null)
                healedPlayer = Owner;
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);

        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = 0.4f + 0.4f * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + completionRatio * 12f) * 0.5f + 0.5f);

            fadeOpacity *= (1 - MathHelper.Clamp(((healedPlayer.Center - Projectile.Center).Length() - 170f) / 70f, 0f, 1f));

            return (Color.CornflowerBlue.MultiplyRGB(PrimColorMult) * fadeOpacity) with { A = 0 };
        }

        internal float WidthFunction(float completionRatio)
        {
            return (3.4f + 4f * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + completionRatio * 12f) * 0.5f + 0.5f)) * 2f;
        }


        private int propellerFrameCounter;
        private int propellerFrame;
        private float gunBarrelRotation;

        public void UpdateFrame(bool buffMode, NPC targetCache)
        {
            int frameTime = 8;
            if (buffMode && Projectile.frame < 16 || Projectile.frame >= 20)
                frameTime = 3;
            if (!buffMode && Projectile.frame < 4 || Projectile.frame >= 8)
                frameTime = 3;


            if (++Projectile.frameCounter > frameTime)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;
            }

            if (buffMode)
            {
                //Start of the transition between gun deployed and healing syringe deployed
                if (Projectile.frame < 8)
                    Projectile.frame = 8;

                //Loop the main animation of the syringe being out
                if (Projectile.frame >= 20)
                    Projectile.frame = 16;
            }

            else
            {
                //Start of the transition between syringe and gun
                if (Projectile.frame > 9 && Projectile.frame < 20)
                    Projectile.frame = 20;

                //Loop idle anim
                if (Projectile.frame == 8 || Projectile.frame == 9)
                    Projectile.frame = 4;
            }

            if (NuzzleFlashTime > 0)
                NuzzleFlashTime -= 1 / 4f;

            //Track the gun rotation
            if (Projectile.frame >= 4 && Projectile.frame < 8)
            {
                float targetRotation = Projectile.rotation * Projectile.spriteDirection;

                if (targetCache != null)
                {
                    Vector2 towardsTarget = (targetCache.Center - Projectile.Center);
                    towardsTarget.Y *= 0.3f;
                    towardsTarget.X = Math.Abs(towardsTarget.X);
                    targetRotation = towardsTarget.ToRotation();
                }

                if (NuzzleFlashTime <= 0f)
                {
                    gunBarrelRotation = Utils.AngleTowards(gunBarrelRotation, targetRotation, 0.1f);
                    gunBarrelRotation = Utils.AngleLerp(gunBarrelRotation, targetRotation, 0.1f);
                }
            }
            else
                gunBarrelRotation = Projectile.rotation;

            if (propellerFrameCounter++ > 4)
            {
                propellerFrameCounter = 0;
                propellerFrame = (propellerFrame + 1) % 4;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (healedPlayer == null)
                healedPlayer = Owner;

            bool healing = BuffModeBuffer <= 0;
            GunBarrelAsset ??= ModContent.Request<Texture2D>(Texture + "_Barrel");
            PropellerAsset ??= ModContent.Request<Texture2D>(Texture + "_Propeller");

            if (healing && Projectile.frame >= 15 && Projectile.frame <= 20 && (healedPlayer.Center - Projectile.Center).Length() < 240f && TrailDrawer != null)
            {
                Vector2 healBeamStart = Projectile.Center + Vector2.UnitY.RotatedBy(Projectile.rotation) * 9f * Projectile.scale;

                Vector2[] drawPos = new Vector2[] { 
                    healedPlayer.Center, 
                    healedPlayer.Center, 
                    (Projectile.Center + healedPlayer.Center) * 0.5f + Vector2.UnitY * 40f,
                    healBeamStart,
                    healBeamStart};

                TrailDrawer?.SetPositions(drawPos, FablesUtils.SmoothBezierPointRetreivalFunction);
                TrailDrawer.NextPosition = healedPlayer.Center;

                Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
                effect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly);
                effect.Parameters["fadeDistance"].SetValue(0.3f);
                effect.Parameters["fadePower"].SetValue(1 / 6f);
                effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "ZapTrail").Value);

                FablesUtils.DrawChromaticAberration(Vector2.UnitX, 1.8f, delegate (Vector2 offset, Color colorMod) {
                    PrimColorMult = colorMod;
                    TrailDrawer?.Render(effect, -Main.screenPosition + offset);
                });
            }

            SpriteEffects flip = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Texture2D bodyTexture = TextureAssets.Projectile[Type].Value;
            Rectangle bodyFrame = bodyTexture.Frame(2, 24, 0, Projectile.frame, -2, -2);
            Vector2 bodyOrigin = new Vector2(bodyFrame.Width / 2, 16);
            Vector2 bodyPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bodyTexture, bodyPosition, bodyFrame, lightColor, Projectile.rotation, bodyOrigin, Projectile.scale, flip);

            bodyFrame.X += bodyTexture.Width / 2;
            Main.EntitySpriteDraw(bodyTexture, bodyPosition, bodyFrame, Color.White, Projectile.rotation, bodyOrigin, Projectile.scale, flip);

            Texture2D propeller = PropellerAsset.Value;
            Rectangle propFrame = propeller.Frame(1, 4, 0, propellerFrame, 0, -2);
            Vector2 propellerPosition = bodyPosition - Vector2.UnitY.RotatedBy(Projectile.rotation) * 14 * Projectile.scale;
            Main.EntitySpriteDraw(propeller, propellerPosition, propFrame, lightColor, Projectile.rotation, new Vector2(propFrame.Width / 2f, propFrame.Height), Projectile.scale, SpriteEffects.None);
            
            //Gun barrel
            if (Projectile.frame >= 4 && Projectile.frame < 8)
            {
                Texture2D gun = GunBarrelAsset.Value;
                Vector2 gunOrigin = new Vector2(0, gun.Height / 2);

                float gunDrop = (Projectile.frame > 4 && Projectile.frame < 7) ? 9 : 7;
                Vector2 gunPosition = bodyPosition + Vector2.UnitY.RotatedBy(Projectile.rotation) * gunDrop * Projectile.scale;

                float gunRotation = gunBarrelRotation;
                if (Projectile.spriteDirection == -1)
                {
                    Vector2 rotVector = gunRotation.ToRotationVector2();
                    rotVector.X *= -1;
                    gunRotation = rotVector.ToRotation();
                }

                Main.EntitySpriteDraw(gun, gunPosition, null, lightColor, gunRotation, gunOrigin, Projectile.scale, SpriteEffects.None);


                if (NuzzleFlashTime > 0)
                {
                    float opacity = 0.8f + 0.2f * (float)Math.Pow(NuzzleFlashTime, 1.7f);
                    MuzzleFlashAsset ??= ModContent.Request<Texture2D>(Texture + "_MuzzleFlash");
                    Texture2D shineTex = MuzzleFlashAsset.Value;
                    Color sheenColor = Color.White * opacity;
                    Vector2 muzzleFlashOrigin = new Vector2(2, shineTex.Height / 2);
                    Vector2 muzzleFlashPosition = gunPosition + Vector2.UnitX.RotatedBy(gunRotation) * 20 * Projectile.scale;

                    Vector2 flashScale = new Vector2(1f + MathF.Pow(1 - NuzzleFlashTime, 0.6f) * 0.4f, 1 - MathF.Pow(1 - NuzzleFlashTime, 1.5f));

                    Main.EntitySpriteDraw(shineTex, muzzleFlashPosition, null, sheenColor with { A = 0 }, gunRotation, muzzleFlashOrigin, flashScale * Projectile.scale, SpriteEffects.None);
                    //Main.EntitySpriteDraw(shineTex, muzzleFlashPosition, null, (sheenColor * 0.4f) with { A = 0 }, gunRotation, muzzleFlashOrigin, flashScale * 1.2f * Projectile.scale, SpriteEffects.None);
                }
            }

            return false;
        }

        public override bool? CanDamage() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(shortShoot);
            writer.Write(BuffModeBuffer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            shortShoot = reader.ReadBoolean();
            BuffModeBuffer = reader.ReadSingle();
        }
    }


    public class WulfrumEnergyBurst : ModProjectile
    {
        public ref float OriginalRotation => ref Projectile.ai[0];
        public NPC Target {
            get {
                if (Projectile.ai[1] < 0 || Projectile.ai[1] > Main.maxNPCs)
                    return null;

                return Main.npc[(int)Projectile.ai[1]];
            }
            set {
                if (value == null)
                    Projectile.ai[1] = -1;
                else
                    Projectile.ai[1] = value.whoAmI;
            }
        }

        public static float MaxDeviationAngle = MathHelper.PiOver4;
        public static float HomingRange = 350;
        public static float HomingAngle = MathHelper.PiOver4 * 1f;

        internal PrimitiveTrail TrailDrawer;
        internal Color PrimColorMult = Color.White;
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bolt");
            ProjectileID.Sets.MinionShot[Projectile.type] = true;

            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Summon;

            Projectile.timeLeft = 140;
            Projectile.extraUpdates = 2;


            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public NPC FindTarget()
        {
            float bestScore = 0;
            NPC bestTarget = null;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC potentialTarget = Main.npc[i];

                if (!potentialTarget.CanBeChasedBy(null, false))
                    continue;

                float distance = potentialTarget.Distance(Projectile.Center);
                float angle = Projectile.velocity.AngleBetween((potentialTarget.Center - Projectile.Center));

                float extraDistance = potentialTarget.width / 2 + potentialTarget.height / 2;

                if (distance - extraDistance < HomingRange && angle < HomingAngle / 2f)
                {
                    if (!Collision.CanHit(Projectile.Center, 1, 1, potentialTarget.Center, 1, 1) && extraDistance < distance)
                        continue;

                    float attemptedScore = EvaluatePotentialTarget(distance - extraDistance, angle / 2f);
                    if (attemptedScore > bestScore)
                    {
                        bestTarget = potentialTarget;
                        bestScore = attemptedScore;
                    }
                }
            }
            return bestTarget;

        }

        public float EvaluatePotentialTarget(float distance, float angle)
        {
            float score = 1 - distance / HomingRange * 0.5f;

            score += (1 - Math.Abs(angle) / (HomingAngle / 2f)) * 0.5f;

            return score;
        }

        public override void AI()
        {
            if (Projectile.timeLeft == 140)
            {
                if (OriginalRotation == 0)
                {
                    OriginalRotation = Projectile.velocity.ToRotation();
                    Projectile.rotation = OriginalRotation;
                }
                Target = null;
            }
            else
            {
                Target = FindTarget();
            }

            Lighting.AddLight(Projectile.Center, (Color.GreenYellow * 0.8f).ToVector3() * 0.5f);

            if (Target != null)
            {
                float distanceFromTarget = (Target.Center - Projectile.Center).Length();

                Projectile.rotation = Projectile.rotation.AngleTowards((Target.Center - Projectile.Center).ToRotation(), 0.07f * (float)Math.Pow((1 - distanceFromTarget / HomingRange), 2));
            }

            Projectile.velocity *= 1.01f;
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * Projectile.velocity.Length();

            //Blast off.
            if (Projectile.timeLeft == 140)
            {
                Vector2 dustCenter = Projectile.Center + Projectile.velocity * 1f;

                for (int i = 0; i < 5; i++)
                {
                    Dust chust = Dust.NewDustPerfect(dustCenter, 178, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(1f, 1.2f));
                    chust.noGravity = true;
                }
            }

            if (Projectile.timeLeft <= 137)
            {
                if (Main.rand.NextBool(4))
                {
                    Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Dust chust = Dust.NewDustPerfect(dustCenter, 178, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(0.6f, 1.15f));
                    chust.noGravity = true;
                }

                if (Main.rand.NextBool(4))
                {
                    Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Dust largeDust = Dust.NewDustPerfect(dustCenter, 178, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.4f), Scale: Main.rand.NextFloat(0.4f, 1f));
                    largeDust.noGravity = true;
                    largeDust.noLight = true;
                }
            }

            if (!Main.dedServ)
                ManageTrail();
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction, new TriangularTip(8f));

            TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, FablesUtils.SmoothBezierPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Sqrt(completionRatio);
            return (Color.Chartreuse.MultiplyRGB(PrimColorMult) * fadeOpacity) with { A = 0 };
        }

        internal float WidthFunction(float completionRatio)
        {
            return 9.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 6f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "ZapTrail").Value);

            FablesUtils.DrawChromaticAberration(Vector2.UnitX, 1.5f, delegate (Vector2 offset, Color colorMod) {
                PrimColorMult = colorMod;
                TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition + offset);
            });

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            float stretchy = MathHelper.Clamp((Projectile.velocity.Length() - 6f) / 16f, 0f, 1f);
            Vector2 scale = new Vector2(1f + stretchy * -0.2f, stretchy * 0.5f + 1f);
            Main.EntitySpriteDraw(tex, Projectile.Center - Projectile.velocity - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation + MathHelper.PiOver2, tex.Size() / 2f, Projectile.scale * scale, SpriteEffects.None, 0);

            return false;
        }
    }

    #region particles
    public class WulfrumDroidEmote : Particle
    {
        public override string Texture => AssetDirectory.WulfrumItems + "WulfrumDroidEmotes";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public Rectangle Frame;

        public WulfrumDroidEmote(Vector2 position, Vector2 velocity, int lifeTime, float scale = 1f, int variant = -1)
        {
            Position = position;
            Velocity = velocity;
            Color = Color.White;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = velocity.ToRotation() + MathHelper.PiOver2;

            if (variant == -1)
                variant = Main.rand.Next(15);

            Frame = new Rectangle(16 * (variant % 8), 16 * (variant / 8), 16, 16);
        }

        public override void Update()
        {
            Velocity *= 0.96f;
            Scale *= 0.97f;

            Color lightColor = Frame.Y > 0 ? new Color(112, 244, 244) : new Color(194, 255, 62);
            Lighting.AddLight(Position, lightColor.ToVector3());
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D emoteTexture = ParticleTexture;
            Vector2 origin = new Vector2(Frame.Width / 2f, Frame.Height);
            float opacity = 1 - (float)Math.Pow(LifetimeCompletion, 4f);

            spriteBatch.Draw(emoteTexture, Position - basePosition, Frame, Color * opacity, Rotation, origin, Scale, SpriteEffects.None, 0);
        }
    }


    public class WulfrumDroidSweatEmote : Particle
    {
        public override string Texture => AssetDirectory.WulfrumItems + "WulfrumDroidSweatEmote";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public WulfrumDroidSweatEmote(Vector2 position, Vector2 velocity, int lifeTime, float scale = 1f)
        {
            Position = position;
            Velocity = velocity;
            Color = Color.White;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = velocity.ToRotation() + MathHelper.PiOver2;

        }

        public override void Update()
        {
            Velocity *= 0.96f;
            Scale *= 0.97f;
            Velocity.Y += 0.06f;

            Lighting.AddLight(Position, new Color(194, 255, 62).ToVector3());
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D emoteTexture = ParticleTexture;
            Vector2 origin = new Vector2(emoteTexture.Width / 2f, emoteTexture.Height / 2f);
            float opacity = 1 - (float)Math.Pow(LifetimeCompletion, 4f);

            SpriteEffects effect = Velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(emoteTexture, Position - basePosition, null, Color * opacity, Rotation, origin, Scale, effect, 0);
        }
    }
    #endregion
}
