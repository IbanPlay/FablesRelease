using CalamityFables.Particles;
using Terraria.WorldBuilding;
using CalamityFables.Content.Boss.DesertWormBoss;
using Terraria.DataStructures;
using System.IO;
using Terraria.Graphics.Effects;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    [ReplacingCalamity("PuffShroom")]
    public class InkyBluePendant : ModItem
    {
        public static readonly SoundStyle BounceSound = new SoundStyle(SoundDirectory.CrabulonDrops + "InkCapGuardianBounce");
        public static readonly SoundStyle SummonSound = new SoundStyle(SoundDirectory.CrabulonDrops + "InkCapGuardianSummon");
        public static readonly SoundStyle ShootSound = new SoundStyle(SoundDirectory.CrabulonDrops + "InkCapMortarShoot") {  Volume = 0.6f };
        public static readonly SoundStyle ExplosionSound = new SoundStyle(SoundDirectory.CrabulonDrops + "InkCapMortarExplosion");
        public static readonly SoundStyle EnrootSound = new SoundStyle(SoundDirectory.CrabulonDrops + "InkCapGuardianRoot");

        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Inky Blue Pendant");
            Tooltip.SetDefault("Summons an Inky Cap Guardian to fight for you\n" +
                "Right click to enroot the Guardian, creating a healing aura for you and other players\n" +
                "The enrooted Guardian can also be used as a trampoline\n" +
                "You can only have one guardian summoned at once\n" +
                "Requires two minion slots");

            ItemID.Sets.StaffMinionSlotsRequired[Type] = 2;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 22;
            Item.damage = 40;
            Item.knockBack = 3f;
            Item.mana = 10;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.value = Item.buyPrice(0, 7, 50, 0);
            Item.rare = ItemRarityID.Green;

            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<InkyCapGuardianBuff>();
            Item.shoot = ModContent.ProjectileType<InkyCapGuardian>();
        }

        public override bool CanShoot(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return true;

            if (player.ownedProjectileCounts[Item.shoot] >= 1)
                return false;
            return true;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position = Main.MouseWorld;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 2);
            return true;
        }

        public override bool AltFunctionUse(Player player) => false;
        public override bool CanUseItem(Player player)
        {
            return player.altFunctionUse == 2 || (player.ownedProjectileCounts[Item.shoot] == 0 && player.slotsMinions <= player.maxMinions - 2);
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2 && player.ownedProjectileCounts[Item.shoot] >= 1)
            {
                if (Main.myPlayer == player.whoAmI)
                {

                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.active && p.owner == player.whoAmI && p.type == Item.shoot)
                        {
                            //Un-root if rooted
                            if (p.ai[0] == 4)
                            {
                                p.ai[0] = 1;
                                return true;
                            }

                            //Ignore if jumping to root position or too far away
                            if (p.ai[0] == 3 || !player.WithinRange(Main.MouseWorld, 700))
                                return false;

                            Point tilePosition = Main.MouseWorld.ToSafeTileCoordinates();
                            bool clearAbove = !Main.tile[tilePosition - new Point(0, 1)].IsTileSolidOrPlatform();
                            bool foundPosition = false;
                            bool goUp = false;

                            for (int x = 0; x < 20; x++)
                            {
                                bool clear = !Main.tile[tilePosition].IsTileSolidOrPlatform();
                                if ((!goUp && !clear && clearAbove) || (goUp && clear && !clearAbove))
                                {
                                    foundPosition = true;
                                    break;
                                }
                                if (x == 0 && !clear)
                                    goUp = true;

                                if (!goUp)
                                    tilePosition.Y++;
                                else
                                    tilePosition.Y--;

                                clearAbove = clear;
                            }

                            if (!foundPosition)
                                return false;

                            if (goUp)
                                tilePosition.Y++;

                            (p.ModProjectile as InkyCapGuardian).RootPosition = tilePosition.ToWorldCoordinates();
                            p.ai[0] = 3;
                            p.ai[1] = 0;

                            //Setting p.netupdate does nothing because it gets cleared at the start of NPC.ai oml fuck my stupid chungus life
                            //p.netUpdate = true;
                            //p.netSpam = 0;
                            break;
                        }
                    }

                }

                return true;
            }

            return base.UseItem(player);
        }
    }

    public class InkyCapGuardianBuff : ModBuff
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            DisplayName.SetDefault("Inky Cap Guardian");
            Description.SetDefault("Just a small little mushrum");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<InkyCapGuardian>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }

    public class InkyCapGuardian : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public List<Vector2> positionCache;
        public List<float> rotationCache;

        public enum Action
        {
            Fighting,
            CatchupFlight,
            KnockedOut,
            JumpingToRootPoint,
            Enrooted
        }

        public Action CurrentAction {
            get => (Action)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        private Vector2 _rootPosition;
        private bool _queuedNetUpdate = false;
        public Vector2 RootPosition
        {
            get => _rootPosition;
            set
            {
                if (_rootPosition != value && Main.myPlayer == Projectile.owner)
                    _queuedNetUpdate = true;
                _rootPosition = value;
            }
        }

        public ref float ConsecutiveEnemyBounceCount => ref Projectile.ai[1];

        public static float healAuraSize = 160f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Inky Cap Guardian");
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.VampireFrog);
            Projectile.aiStyle = -1;
            Projectile.width = 42;
            Projectile.height = 50;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 2f;

            /*
            Projectile.usesIDStaticNPCImmunity = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            */
        }

        public bool playedSpawnSound = false;
        public override void AI()
        {
            if (!Owner.dead && Owner.HasBuff<InkyCapGuardianBuff>())
                Projectile.timeLeft = 2;

            if (_queuedNetUpdate)
                Projectile.netUpdate = true;

            Projectile.hide = false;
            if (!playedSpawnSound)
            {
                playedSpawnSound = true;
                SoundEngine.PlaySound(InkyBluePendant.SummonSound, Projectile.Center);
            }

            float distanceToPlayer = Projectile.Distance(Owner.Center);
            float horizontalDistanceToPlayer = Math.Abs(Owner.Center.X - Projectile.Center.X);
            float verticalDistanceToPlayer = Math.Abs(Owner.Center.Y - Projectile.Center.Y);

            Vector2 idealPosition = Owner.Center - Vector2.UnitX * Owner.Directions * (40 + Owner.width + 40 * Projectile.minionPos); 

            //Vanilla uses AI_067_CustomEliminationCheck_Pirates as the elimination check, but it always returns true. How weird
            int target = -1;
            float targettingRange = 800;
            Projectile.Minion_FindTargetInRange((int)targettingRange, ref target, true);


            if (CurrentAction == Action.CatchupFlight)
                FlyBackToPlayer();
            else if (CurrentAction == Action.KnockedOut)
                FallWhileStunned(idealPosition);
            else if (CurrentAction == Action.JumpingToRootPoint)
                JumpToRootPoint();
            else if (CurrentAction == Action.Enrooted)
                SentryBehavior(target);
            else
            {
                Projectile.tileCollide = true;
                Projectile.shouldFallThrough = Owner.Bottom.Y - 12f > Projectile.Bottom.Y;
                float distanceToIdealRestPosition = Math.Abs(Projectile.Center.X - idealPosition.X);

                //Edge case mostly for when its spawned
                if (FablesUtils.FullSolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    CurrentAction = Action.CatchupFlight;
                    return;
                }

                NPC targetNPC = null;
                //If not targetting anything, its allowed to go into flight mode to catch up
                if (target == -1)
                {
                    //Teleport to the player if way too far
                    if (distanceToPlayer > 2000)
                        Projectile.Center = Owner.Center;
                    //Catch up to the player whle flying if too far
                    else if (distanceToPlayer > 500 || verticalDistanceToPlayer > 300 || Owner.rocketDelay2 > 0)
                    {
                        CurrentAction = Action.CatchupFlight;
                        Projectile.netUpdate = true;

                        //Abruptly change velocity to go to the player
                        if (Math.Sign(Projectile.velocity.Y) != Math.Sign((Owner.Center - Projectile.Center).Y))
                            Projectile.velocity.Y = 0;
                    }
                }

                else
                {
                    targetNPC = Main.npc[target];
                    idealPosition = targetNPC.Center;
                    Projectile.shouldFallThrough = targetNPC.Center.Y > Projectile.Bottom.Y;


                    if (Projectile.IsInRangeOfMeOrMyOwner(targetNPC, targettingRange, out var _, out var _, out var _))
                    {
                        if (Vector2.Distance(Projectile.Center, targetNPC.Center) < 50)
                        {
                            Projectile.velocity = Projectile.velocity.ClampMagnitude(10);
                            Projectile.netUpdate = true;
                        }

                        //Bounce on the enemies if they're in the air
                        Rectangle shortenedHitbox = targetNPC.Hitbox;
                        shortenedHitbox.Inflate(0, -8);
                        if (Projectile.Hitbox.Intersects(shortenedHitbox) && Projectile.velocity.Y >= 0f)
                        {
                            Projectile.velocity.Y = -10f;
                            Projectile.velocity.X += Math.Max(Math.Abs(Projectile.velocity.X * 0.2f), 5) * Projectile.velocity.X.NonZeroSign();
                            ConsecutiveEnemyBounceCount++;

                            targetNPC.velocity.Y += 4f * targetNPC.knockBackResist;

                            ParticleHandler.SpawnParticle(new SplatRippleParticle(Projectile.Bottom, Color.DodgerBlue * 1.7f, Color.RoyalBlue * 0.8f) { Lifetime = 14 });
                            SoundEngine.PlaySound(InkyBluePendant.BounceSound with { Volume = 0.3f }, Projectile.Center);

                            //Get knocked out
                            if (ConsecutiveEnemyBounceCount >= 5)
                            {
                                CurrentAction = Action.KnockedOut;
                                Projectile.velocity.X *= 0.7f;
                                Projectile.velocity.Y *= 1.4f;
                                KnockedOutTimer = 0;

                                SoundEngine.PlaySound(InkyBluePendant.ShootSound, Projectile.Center);
                                
                                int bolt = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Top, Projectile.SafeDirectionTo(targetNPC.Center) * 10f, ModContent.ProjectileType<InkyCapSporeBomb>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                                Main.projectile[bolt].originalDamage = Projectile.originalDamage;
                                Main.projectile[bolt].netUpdate = true;
                            }

                        }
                    }

                    idealPosition.X += 20 * (targetNPC.Center.X - Projectile.Center.X).NonZeroSign();
                }

                //Bounce
                bool canWaterBounce = !Projectile.shouldFallThrough && Projectile.velocity.Y >= 0 && Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height + 10);
                if (Projectile.velocity.Y == 0 || canWaterBounce)
                {
                    //Reset the count of bounces
                    ConsecutiveEnemyBounceCount = 0;

                    float bounceStrenght = 8 + MathF.Sin(Projectile.minionPos * 0.3f);
                    bounceStrenght += Utils.GetLerpValue(140, 300, distanceToIdealRestPosition, true) * 4f;

                    //Check ahead for obstacles and increase the jump strenght if so
                    if (bounceStrenght < 11)
                    {
                        //Jump over obstacles
                        Point tilePosition = Projectile.Center.ToTileCoordinates();
                        tilePosition.X += Projectile.spriteDirection * 2;
                        tilePosition.X += (int)Projectile.velocity.X;

                        tilePosition.Y -= (int)(Utils.GetLerpValue(4, 7, bounceStrenght, true) * 2.9f);

                        for (int i = 0; i < 4; i++)
                        {
                            if (!WorldGen.InWorld(tilePosition.X, tilePosition.Y) || bounceStrenght > 11)
                                break;

                            Tile t = Main.tile[tilePosition];
                            if (WorldGen.SolidTile(t))
                                bounceStrenght += 2.5f;

                            tilePosition.Y--;
                        }
                    }

                    //Bounce less when the ideal position has been reached
                    if (targetNPC == null && Owner.velocity.X == 0 && distanceToIdealRestPosition < 30)
                        bounceStrenght *= 0.7f + 0.3f * Utils.GetLerpValue(0f, 30f, distanceToIdealRestPosition, true);

                    //Bounce higher to reach the target
                    if (targetNPC != null)
                    {
                        float enemyBounceStrenght = (float)Math.Sqrt(Math.Abs(Projectile.Center.Y - targetNPC.Center.Y) * 2f * 0.52f);
                        if (enemyBounceStrenght > 25)
                            enemyBounceStrenght = 25;
                        bounceStrenght = Math.Max(enemyBounceStrenght, bounceStrenght);
                    }

                    Projectile.velocity.Y = -bounceStrenght;
                }

                if (Projectile.velocity.Y < 10)
                    Projectile.velocity.Y += 0.52f;

                //Go towards the target
                if (Math.Abs(Projectile.Center.X - idealPosition.X) > 10)
                {
                    int direction = (idealPosition.X - Projectile.Center.X).NonZeroSign();
                    float acceleration = 0.04f;

                    if (Projectile.velocity.X.NonZeroSign() != direction)
                        acceleration *= 2f;

                    float speed = Math.Max(Math.Abs(Owner.velocity.X), 6);
                    if (targetNPC != null)
                        speed = 12;

                    Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, speed * direction, acceleration);
                }

                //Keep on bouncing
                else
                {
                    Projectile.velocity.X *= 0.98f;
                    if (Math.Abs(Projectile.velocity.X) < 0.1f)
                        Projectile.velocity.X = 0;
                }

                Projectile.rotation = Math.Clamp(Projectile.velocity.X * 0.04f * Utils.GetLerpValue(0, 5f, Projectile.velocity.Y), -0.3f, 0.3f);
            }

            if (!Main.dedServ && healPulseProgress > 0)
            {
                healPulseProgress -= 1 / (60f * 0.5f);
            }
        }

        public void FlyBackToPlayer()
        {
            //Reset the count of bounces and other variables
            ConsecutiveEnemyBounceCount = 0;
            Projectile.tileCollide = false;
            Projectile.shouldFallThrough = true;

            //Ideal position is above the player
            Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * 30f - Vector2.UnitX * Owner.direction * 30;
            idealPosition += Vector2.UnitX.RotatedBy(Projectile.minionPos * 0.4f) * 30f;
            float distanceToIdealPosition = (Projectile.Center - idealPosition).Length();

            Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.045f;

            //Max velocity is either 4 or the speed of the player, so it never has trouble catching up
            float maxVelocity = Math.Max(Owner.velocity.Length(), 7);
            if (goalVelocity.Length() > maxVelocity)
                goalVelocity = goalVelocity.ClampMagnitude(maxVelocity);

            if (distanceToIdealPosition > 60f)
            {
                Projectile.velocity.X += goalVelocity.X * 0.1f;
                Projectile.velocity.Y += goalVelocity.Y * 0.06f;

                //A low lerp leads to curvier movement , which is nice when its hovering around its destination
                float lerpStrenght = 0.01f + Utils.GetLerpValue(100, 300, distanceToIdealPosition, true) * 0.07f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, lerpStrenght);
            }

            FlyingVisuals();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            //Stop flying
            if (Projectile.WithinRange(Owner.Center, 200) && Owner.velocity.Y == 0f && Projectile.Bottom.Y <= Owner.Bottom.Y && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                CurrentAction = Action.Fighting;

            //Teleport to the player if way too far
            if (distanceToIdealPosition > 2000)
                Projectile.Center = Owner.Center;
        }
        public void FlyingVisuals()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextFloat() < Utils.GetLerpValue(4, 10, Projectile.velocity.Length(), true))
            {
                float smokeSize = Main.rand.NextFloat(1f, 2.6f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 smokeCenter = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.4f, 1.3f) - Projectile.velocity * 0.6f;
                Particle smoke = new SporeGas(smokeCenter, velocity, Projectile.Center, 12f, smokeSize, 0.01f);
                smoke.FrontLayer = false;
                ParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 gushDirection = Main.rand.NextFloat(-MathHelper.Pi, 0).ToRotationVector2();
                Vector2 dustPosition = Projectile.Center + gushDirection * 22f * Main.rand.NextFloat(0.1f, 0.6f);
                Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * 0.3f - Projectile.velocity;
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.GlowingMushroom, velocity);
                d.noLightEmittence = true;
            }
        }

        public ref float KnockedOutTimer => ref Projectile.ai[1];
        public void FallWhileStunned(Vector2 idealPosition)
        {
            Projectile.tileCollide = true;
            Projectile.shouldFallThrough = Owner.Bottom.Y - 12f > Projectile.Bottom.Y;

            Projectile.rotation += Projectile.velocity.X * 0.04f;

            //Land back on the floor
            if (Projectile.velocity.Y == 0)
            {
                CurrentAction = Action.Fighting;
                ConsecutiveEnemyBounceCount = 0;
            }

            //Go towards ideal position
            if (KnockedOutTimer > 35 && Math.Abs(Projectile.Center.X - idealPosition.X) > 10)
            {
                int direction = (idealPosition.X - Projectile.Center.X).NonZeroSign();
                float acceleration = 0.04f;
                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, 3 * direction, acceleration);
            }

            //After 1.5 seconds, if were too far, fly back to the player
            if ((Projectile.Distance(Owner.Center) > 500 || Math.Abs(Owner.Center.Y - Projectile.Center.Y) > 300) && KnockedOutTimer > 90)
            {
                CurrentAction = Action.CatchupFlight;
                ConsecutiveEnemyBounceCount = 0;
            }

            KnockedOutTimer++;
            Projectile.velocity.Y += 0.53f;
        }


        public void JumpToRootPoint()
        {
            if (Projectile.ai[1] == 0)
            {
                Projectile.velocity = FablesUtils.GetArcVel(Projectile.Center, RootPosition, 0.5f, 30f, heightAboveTarget: 200f);
                Projectile.ai[1] = 1;
            }

            Projectile.tileCollide = false;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y += 0.5f;
            if (Projectile.WithinRange(RootPosition, 10 + Projectile.velocity.Length()))
            {
                CurrentAction = Action.Enrooted;
                enrootTimer = 1f;
                ConsecutiveEnemyBounceCount = 0;
                HealPulseTimer = 0;
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = RootPosition - Vector2.UnitY * 22f;

                ParticleHandler.SpawnParticle(new CircularPulseShine(Projectile.Center, Color.Blue, 2f));
                Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
                SoundEngine.PlaySound(InkyBluePendant.EnrootSound, Projectile.Center);
            }
        }

        public float enrootTimer = 0f;
        public float bounceTimer = 0f;
        public int[] consecutiveBounceCounter = new int[Main.maxPlayers];
        public ref float HealPulseTimer => ref Projectile.ai[2];
        public float healPulseProgress;

        public void SentryBehavior(int target)
        {
            Projectile.rotation = 0;
            Projectile.tileCollide = true;
            Projectile.hide = true;

            Rectangle bounceHitbox = Projectile.Hitbox;
            bounceHitbox.Y += 14;
            bounceHitbox.Inflate(20, 0);

            bool healPulseActive = false;
            HealPulseTimer++;
            if (HealPulseTimer % 60 == 0)
            {
                healPulseActive = true;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead )
                {
                    if (p.WithinRange(Projectile.Center, healAuraSize))
                    {
                        p.AddBuff(ModContent.BuffType<InkyCapSpores>(), 5 * 60);
                        if (bounceHitbox.Intersects(p.Hitbox) && p.velocity.Y >= 0 && p.GetModPlayer<FablesPlayer>().previousGrapCount == 0)
                            BouncePlayerUp(p);

                        if (healPulseActive && Main.myPlayer == p.whoAmI)
                            p.Heal(4);
                    }

                    if (p.velocity.Y == 0)
                        consecutiveBounceCounter[i] = 0;
                }
            }

            HealingAuraVisuals(healPulseActive);

            if (HealPulseTimer % (60 * 2) == 0)
                MortarFire(target);

            //Automatically un-root if not close enough
            if (!Projectile.WithinRange(Owner.Center, 1000))
                CurrentAction = Action.CatchupFlight;
        }

        public void BouncePlayerUp(Player player)
        {
            float jumpHeight = 12 + consecutiveBounceCounter[player.whoAmI] * 2f;
            jumpHeight = Math.Min(jumpHeight, 17);

            player.velocity.Y = -jumpHeight;
            player.velocity.X *= 1.3f;

            player.RefreshMovementAbilities();

            bounceTimer = 1f;
            SoundEngine.PlaySound(InkyBluePendant.BounceSound with { Pitch = -0.4f + Utils.GetLerpValue(12, 17, jumpHeight, true) * 0.7f }, Projectile.Center);
            ParticleHandler.SpawnParticle(new SplatRippleParticle(Projectile.Center + Vector2.UnitY * 8f, Color.White, Color.DodgerBlue * 0.6f) { Scale = 1.5f, Lifetime = 18 });

            for (int i = 0; i < 10; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                direction.Y *= 0.2f;

                if (direction.Y < 0)
                    direction.Y *= -1;

                Dust d = Dust.NewDustPerfect(Projectile.Center + direction * 30f, DustID.MushroomSpray, direction * 3f - Vector2.UnitY * 0.2f, Scale: Main.rand.NextFloat(1f, 2f));
                d.noGravity = true;
            }


            float spinTime = 20 + (jumpHeight / 9f) * 22f;
            float spinCount = jumpHeight / 9f;
            float spinPower = 0.6f - Utils.GetLerpValue(10f, 17f, jumpHeight, true) * 0.3f;

            player.GetModPlayer<FablesPlayer>().AddSpinEffect(new BasicPirouetteEffect((int)spinTime, spinCount, spinPower));

            consecutiveBounceCounter[player.whoAmI]++;
        }

        public void HealingAuraVisuals(bool healPulse)
        {
            //Tick counters down
            bounceTimer -= 1 / (60f * 0.3f);
            enrootTimer -= 1 / (60f * 0.3f);

            //Dust
            if (Main.rand.NextBool(8))
            {
                for (int i = 0; i < 2; i++)
                {
                    int dustType = DustID.MushroomSpray;
                    if (Main.rand.NextBool())
                        dustType = ModContent.DustType<SpectralWaterDustEmbers>();

                    Vector2 offset = Main.rand.NextVector2Circular(healAuraSize, 20f);
                    offset.Y *= 6f;
                    if (offset.Y > 0)
                        offset.Y *= -1;

                    Dust d = Dust.NewDustPerfect(Projectile.Center + offset, dustType, -Vector2.UnitY * Main.rand.NextFloat(1f, 3f), 0);
                    d.noGravity = !Main.rand.NextBool(6);
                    d.fadeIn = 0f;
                    d.noLight = true;
                    d.color = Color.White;
                    d.position.Y += 16;

                    if (dustType != DustID.MushroomSpray)
                        d.color *= Main.rand.NextFloat(0.5f, 1f);
                }
            }

            //Health crosses
            if (Main.rand.NextBool(16))
            {
                Vector2 positionOffset = Main.rand.NextVector2Circular(healAuraSize, healAuraSize);
                if (positionOffset.Y > 0)
                    positionOffset.Y *= -1;
                positionOffset.ClampMagnitude(healAuraSize, 80f);
                float scale = Main.rand.NextFloat(0.5f, 1f);
                Color baseColor = Color.Lerp(Color.White, Color.DodgerBlue, Main.rand.NextFloat(0f, 0.5f));

                ParticleHandler.SpawnParticle(new HealingCrossParticle(Projectile.Center + positionOffset, -Vector2.UnitY * 0.2f, baseColor, Color.DodgerBlue * 0.5f, scale, Main.rand.Next(30, 60)));
            }

            //Spores

            if (Main.rand.NextBool(2) && false)
            {
                Vector2 position = Projectile.Bottom + Main.rand.NextVector2Circular(healAuraSize, 2f);
                position.Y -= Main.rand.NextFloat(-6f, 10f);
                SporeGas gas = new SporeGas(position, -Vector2.UnitY, position, 10f, Main.rand.NextFloat(1f, 2f));
                gas.forceNoDust = true;
                gas.FrontLayer = false;

                ParticleHandler.SpawnParticle(gas);
            }

            if (healPulse)
                healPulseProgress = 1;
        }
        public void MortarFire(int target)
        {
            HealPulseTimer = 0;
            //Don't shoot if no target.
            if (target < 0)
                return;

            SoundEngine.PlaySound(InkyBluePendant.ShootSound, Projectile.Center);
            if (Main.myPlayer == Projectile.owner)
            {
                NPC targetNPC = Main.npc[target];
                Vector2 velocity = FablesUtils.GetArcVel(Projectile.Top, targetNPC.Center + targetNPC.velocity * 8f, 0.4f, 100, heightAboveTarget: 100f, maxXvel:14);
                int damage = (int)(Projectile.damage * 2f);
                int knockback = (int)(Projectile.knockBack * 2f);

                int bolt = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Top, velocity, ModContent.ProjectileType<InkyCapSporeBomb>(), damage, knockback, Projectile.owner);
                Main.projectile[bolt].originalDamage = Projectile.originalDamage * 2;
                Main.projectile[bolt].netUpdate = true;
            }

        }


        public override bool MinionContactDamage() => CurrentAction != Action.Enrooted && CurrentAction != Action.JumpingToRootPoint && CurrentAction != Action.KnockedOut;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //Draw behind tiles when enrooted
            if (CurrentAction == Action.Enrooted)
                behindNPCsAndTiles.Add(index);
        }


        public override bool PreDraw(ref Color lightColor)
        {
            //Update the position and fade of the aura
            if (CurrentAction == Action.Enrooted)
            {
                enrootedAuraPosition = Projectile.Center;
                enrootedAuraOpacity = MathHelper.Lerp(enrootedAuraOpacity, 1f, 0.02f);
            }
            else
            {
                enrootedAuraOpacity = MathHelper.Lerp(enrootedAuraOpacity, 0f, 0.04f);
                if (enrootedAuraOpacity < 0.03f)
                    enrootedAuraOpacity = 0;
            }

            if (enrootedAuraOpacity > 0)
                DrawBloomAuraBack();

            //if (healPulseProgress > 0)
            //    DrawCirclePulse();

            Texture2D projectileTex = TextureAssets.Projectile[Type].Value;
            int frameCount = Projectile.frame;
            Rectangle frame = projectileTex.Frame();
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawRotation = Projectile.rotation;

            Vector2 squish = Vector2.One * Projectile.scale;
            if (CurrentAction != Action.CatchupFlight && CurrentAction != Action.KnockedOut && CurrentAction != Action.JumpingToRootPoint)
            {
                drawPosition.Y += 10;

                squish.Y += Utils.GetLerpValue(0, 6, Projectile.velocity.Y) * 0.2f;
                if (squish.Y > Projectile.scale * 1.3f)
                    squish.Y = Projectile.scale * 1.3f;
                if (squish.Y < Projectile.scale * 0.7f)
                    squish.Y = Projectile.scale * 0.7f;

                squish.X = 2 * Projectile.scale  - squish.Y;
            }
            else
            {
                //Stretch based on flight speed
                squish.Y += Utils.GetLerpValue(2, 8, Projectile.velocity.Length(), true) * 0.2f * (0.75f + 0.25f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.2f));
                squish.X = 2 * Projectile.scale  - squish.Y;
            }

            if (CurrentAction == Action.Enrooted)
            {
                drawPosition.Y -= 3;

                if (bounceTimer > 0)
                {
                    squish.Y -= bounceTimer * MathF.Pow(bounceTimer, 2f) * 0.8f;
                    squish.X += bounceTimer * FablesUtils.PolyInOutEasing(bounceTimer, 1.2f) * 0.4f;
                }
                else
                {
                    squish.Y += MathF.Sin(bounceTimer) * 0.06f;
                    squish.X += (0.5f + 0.5f * MathF.Sin(bounceTimer + MathHelper.PiOver2 * 1.5f)) * 0.1f;
                }

                if (enrootTimer > 0)
                    squish *= MathF.Pow(enrootTimer, 2f) * 0.5f + 1f;

                Texture2D outline = ModContent.Request<Texture2D>(Texture + "Outline").Value;
                Color outlineColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly % 1, Color.Teal, Color.MediumTurquoise, Color.RoyalBlue);
                Main.EntitySpriteDraw(outline, drawPosition, null, outlineColor * enrootedAuraOpacity, drawRotation, outline.Size() * 0.5f, squish, effects, 0);
            }


            // Draw the head.
            Main.EntitySpriteDraw(projectileTex, drawPosition, frame, lightColor, drawRotation, frame.Size() * 0.5f, squish, effects, 0);

            projectileTex = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            Main.EntitySpriteDraw(projectileTex, drawPosition, frame, Color.White * 0.6f, drawRotation, frame.Size() * 0.5f, squish, effects, 0);


            if (CurrentAction == Action.Enrooted && bounceTimer > 0)
            {
                squish.Y *= 1f + bounceTimer * 0.3f;
                Color glowColor = Color.Lerp(Color.Blue, Color.Turquoise, bounceTimer) with { A = 0 } * (float)Math.Pow(bounceTimer, 0.5f);

                for (int i = 0; i < 5; i++)
                    Main.EntitySpriteDraw(projectileTex, drawPosition - Vector2.UnitY * MathF.Pow(i / 4f, 1.6f) * (10f + 40 * MathF.Pow(bounceTimer, 0.4f)), frame, glowColor * (1 - i / 4f), drawRotation, frame.Size() * 0.5f, squish, effects, 0);
            }

            if (enrootedAuraOpacity > 0)
                DrawBloomAura();

            //if (healPulseProgress > 0)
            //    DrawWavePulse();

            return false;
        }
        

        #region Primitives
        /*
        public PrimitiveClosedLoop healEffect1;
        public PrimitiveClosedLoop healEffect2;
        public PrimitiveClosedLoop healEffect3;


        public float healPulseProgress;

        public void ManagePrimitivesAndCaches()
        {
            healEffect1 ??= new PrimitiveClosedLoop(30, HealWaveWidthFunction, HealWaveColorFunction);
            healEffect2 ??= new PrimitiveClosedLoop(30, HealWaveWidthFunction, HealWaveColorFunction);
            healEffect3 ??= new PrimitiveClosedLoop(30, HealRingWidthFunction, HealRingColorFunction);

            float progress = 1 - healPulseProgress;
            float pulseSize = 20f + healAuraSize * (float)Math.Pow(progress, 0.5f);

            healEffect1.SetPositionsCircle(enrootedAuraPosition, pulseSize, progress * 0.6f);
            healEffect2.SetPositionsEllipse(enrootedAuraPosition, pulseSize * 0.9f, progress * -0.18f, 0.2f, axisRotation: -progress);
            healEffect3.SetPositionsCircle(enrootedAuraPosition, pulseSize);
        }

        public float HealWaveWidthFunction(float completion)
        {
            return 6f;
        }

        public Color HealWaveColorFunction(float completion)
        {
            Color baseColor = new Color(100, 190, 255);
            return baseColor with { A = 0 };
        }


        public float HealRingWidthFunction(float completion)
        {
            return 10f;
        }

        public Color HealRingColorFunction(float completion)
        {
            Color baseColor = new Color(100, 120, 255);
            return baseColor * 0.3f;
        }

        public void DrawCirclePulse()
        {
            //healEffect3?.Render();
        }

        public void DrawWavePulse()
        {
            Effect effect = Scene["InkyCapHealWave"].GetShader().Shader;

            //effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.05f);
            effect.Parameters["repeats"].SetValue(2f);
            effect.Parameters["waveLenght"].SetValue(0.4f);

            effect.Parameters["trailStretch"].SetValue(new Vector2(1f, 0.3f));
            effect.Parameters["trailScroll"].SetValue(Main.GameUpdateCount * 0.01f);
            effect.Parameters["colorMultiplier"].SetValue(new Vector3(1.3f));
            effect.Parameters["verticalFadeStart"].SetValue(0.2f);
            effect.Parameters["noiseOverlay"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "ZapTrail").Value);


            //healEffect1?.Render(effect);
            //healEffect2?.Render(effect);
        }
        */
        #endregion

        #region Aura drawing
        public Vector2 enrootedAuraPosition;
        public float enrootedAuraOpacity;
        public void DrawBloomAura()
        {
            Texture2D auraTex = AssetDirectory.CommonTextures.BloomDiamondColumn.Value;
            Vector2 stretch = new Vector2(2.4f, 1f) * (healAuraSize / 160f);
            Vector2 origin = new Vector2(auraTex.Width / 2f, auraTex.Height * 0.7f);

            for (int i = 0; i < 4; i++)
            {
                float opacity = MathF.Pow((1 + i) / 5f, 1.4f) * 0.3f;
                float scaleMult = MathF.Pow(1 - i / 5f, 0.2f);

                Color startColor = FablesUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f) % 1, Color.Aqua, Color.Cyan * 0.6f, Color.Blue, Color.Teal, Color.MediumTurquoise);
                Color baseColor = Color.Lerp(startColor, Color.Teal, 1 - i / 4f);
                baseColor *= opacity * enrootedAuraOpacity;

                Main.EntitySpriteDraw(auraTex, enrootedAuraPosition - Main.screenPosition, null, baseColor with { A = 0 }, 0, origin, stretch * 2.4f * scaleMult, 0, 0);
            }

        }

        public float previousLightStrenght = -1;
        public float previousHealTintStrenght = 0;

        public void DrawBloomAuraBack()
        {
            Texture2D auraTex = ModContent.Request<Texture2D>(AssetDirectory.Tiles + "PointOfInterest2xGlow").Value;
            Vector2 size = new Vector2(healAuraSize / (auraTex.Width / 2f), 1.4f);
            Vector2 origin = new Vector2(auraTex.Width / 2f, auraTex.Height - 8);
            float opacity = 0.1f;
            Vector2 position = enrootedAuraPosition + Vector2.UnitY * Projectile.height * 0.5f - Vector2.UnitY * 6f;

            /*
            float lightStrenght = Lighting.GetColor(enrootedAuraPosition.ToTileCoordinates()).GetBrightness();
            float lightPower = Utils.GetLerpValue(0.7f, 1f, lightStrenght, true);
            if (lightPower > 0)
            {
                Texture2D premultAuraTex = AssetDirectory.CommonTextures.BigBloomCircle.Value;
                Rectangle frame = new Rectangle(0, 0, premultAuraTex.Width, premultAuraTex.Height / 2);
                Vector2 darkSize = new Vector2(healAuraSize / (premultAuraTex.Width / 2f));

                Main.EntitySpriteDraw(premultAuraTex, position - Main.screenPosition, frame, Color.Black * lightPower * enrootedAuraOpacity * 0.5f, 0, premultAuraTex.Size() / 2f, darkSize * 1.8f, 0, 0);
            }
            */

            float lightStrenght = Lighting.GetColor(enrootedAuraPosition.ToTileCoordinates()).GetBrightness();
            if (previousLightStrenght < 0)
                previousLightStrenght = lightStrenght;
            previousLightStrenght = MathHelper.Lerp(previousLightStrenght, lightStrenght, 0.05f);
            opacity *= 1 + 2f * Utils.GetLerpValue(0.5f, 1f, previousLightStrenght, true);

            bool healActive = Main.LocalPlayer.WithinRange(enrootedAuraPosition, healAuraSize);
            if (healActive)
                previousHealTintStrenght = MathHelper.Lerp(previousHealTintStrenght, 1f, 0.1f);
            else
            {
                previousHealTintStrenght = MathHelper.Lerp(previousHealTintStrenght, 0, 0.04f);
                if (previousHealTintStrenght <= 0.02f)
                    previousHealTintStrenght = 0;
            }

            Color baseColor = Color.Lerp(Color.Teal, new Color(0, 180, 108), previousHealTintStrenght);

            if (previousHealTintStrenght > 0 && healPulseProgress > 0)
            {
                float visualPulseProgress = MathF.Pow(healPulseProgress, 1.6f) * previousHealTintStrenght;
                baseColor = Color.Lerp(baseColor, new Color(100, 255, 170), visualPulseProgress);

                size.Y *= 1 + 0.1f * visualPulseProgress;
                size.X *= 1f - 0.05f * visualPulseProgress;
            }

            //Size of the aura part that extends below
            float auraDownSize = 0.05f;
            Point floorPosition = RootPosition.ToTileCoordinates();

            for (int i = -9; i <= 9; i++)
            {
                Point checkPos = floorPosition + new Point(i, 0);
                Tile t = Main.tile[checkPos];
                if (!t.HasUnactuatedTile ||
                    !Main.tileSolid[t.TileType] ||
                    t.Slope != SlopeType.Solid ||
                    t.IsHalfBlock
                    )
                {
                    //If theres missing floor we gotta extend the aura down
                    auraDownSize = 0.35f;
                    break;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Main.EntitySpriteDraw(auraTex, position - Main.screenPosition, null, baseColor with { A = 0 } * opacity * enrootedAuraOpacity, 0, origin, size, 0, 0);
                
                Main.EntitySpriteDraw(auraTex, position - Main.screenPosition, null, baseColor with { A = 0 } * opacity * enrootedAuraOpacity, MathHelper.Pi, origin, size with { Y = size.Y * auraDownSize }, 0, 0);


                size.X *= 0.95f;
                size.Y *= 1.1f;
                opacity *= 1.2f;
            }
        }
        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(RootPosition);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            RootPosition = reader.ReadVector2();
        }
    }

    public class InkyCapSporeBomb : ModProjectile
    {
        internal PrimitiveTrail TrailDrawer;
        internal List<Vector2> cache;

        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seed");
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 60 * 6;
            Projectile.extraUpdates = 1;
        }

        public bool didExplosiveVisuals = false;

        //Increased collision range so it can detonate from afar like a flak cannon
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = Projectile.timeLeft <= 3 ? Projectile.width / 2f : 40;
            return FablesUtils.AABBvCircle(targetHitbox, Projectile.Center, radius);
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (Color.DodgerBlue * 0.8f).ToVector3() * 0.5f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y += 0.4f;

            if (!Main.dedServ)
                ManageTrail();

            if (Projectile.timeLeft <= 3)
            {
                Projectile.tileCollide = false;
                Projectile.velocity *= 0;
                Projectile.Resize(140, 140);

                if (!didExplosiveVisuals)
                {
                    SoundEngine.PlaySound(InkyBluePendant.ExplosionSound, Projectile.Center);
                    didExplosiveVisuals = true;
                    for (int i = 0; i < 50; i++)
                    {
                        float smokeSize = Main.rand.NextFloat(1.7f, 3f);
                        float distance = Main.rand.NextFloat();

                        Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                        Vector2 smokeCenter = Projectile.Center + gushDirection * (distance * 35f + 15f);
                        Vector2 velocity = gushDirection * (Main.rand.NextFloat(0f, 0.6f) + distance * 2f);
                        Particle smoke = new SporeGas(smokeCenter, velocity, Projectile.Center, 22f, smokeSize, 0.01f) { forceNoDust = true };
                        smoke.FrontLayer = false;
                        ParticleHandler.SpawnParticle(smoke);
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                        Vector2 dustPosition = Projectile.Center + gushDirection * 22f * Main.rand.NextFloat(0.1f, 0.6f);
                        Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 7f);
                        Dust d = Dust.NewDustPerfect(dustPosition, DustID.MushroomSpray, velocity, 0, default, Main.rand.NextFloat(1f, 2f));
                        d.noLightEmittence = true;
                        d.noGravity = true;
                    }

                    //ParticleHandler.SpawnParticle(new ShineFlashParticle(Projectile.Center, Color.DodgerBlue, 1f, 18) { Rotation = Main.rand.NextFloat(-0.2f, 0.2f) }) ;
                }
                return;
            }

            if (!Main.rand.NextBool(4))
            {
                bool bright = Main.rand.NextBool(9);
                Color blotColor = bright ? Color.Blue : Main.rand.NextBool(9) ? Color.MidnightBlue : Color.Black;
                float blotSize = Main.rand.NextFloat(0.7f, 2f);
                Vector2 blotVelocity = -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.3f);
                Particle p = new GoopBlotParticle(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), blotVelocity, blotColor, blotSize, Main.rand.Next(10, 30));
                p.FrontLayer = bright;

                ParticleHandler.SpawnParticle(p);
            }


            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20F), DustID.MushroomSpray, -Projectile.velocity * 0.1f);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.5f, 1.2f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft > 3)
                Projectile.timeLeft = 3;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft > 3)
                Projectile.timeLeft = 3;
            return false;
        }

        #region Primitives
        public void ManageTrail()
        {
            cache ??= new List<Vector2>();
            if (cache.Count == 0)
                for (int i = 0; i < 20; i++)
                    cache.Add(Projectile.Center);

            cache.Add(Projectile.Center);
            while (cache.Count > 20)
                cache.RemoveAt(0);

            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(20, WidthFunction, ColorFunction);
            TrailDrawer.SetPositionsSmart(cache, Projectile.Center, FablesUtils.RigidPointRetreivalFunction);
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Sqrt(completionRatio);
            Color baseColor = Color.MidnightBlue;

            return baseColor * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio)
        {
            float width = 14.4f * Utils.GetLerpValue(0f, 0.8f, completionRatio, true);

            return width;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ && cache != null)
            {
                GhostTrail clone = new GhostTrail(cache, TrailDrawer, 0.15f, null, "TarTrail", delegate (Effect effect, float fading) {
                    effect.Parameters["repeats"].SetValue(2);
                    effect.Parameters["outlineColor"].SetValue(new Color(12, 0, 0).ToVector4());
                    effect.Parameters["fadeInColor"].SetValue(new Color(78, 50, 43).ToVector4());
                    effect.Parameters["scroll"].SetValue(Main.GameUpdateCount * 0.05f);
                    effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "FluidTrail").Value);
                });

                clone.ShrinkTrailLenght = true;
                GhostTrailsHandler.LogNewTrail(clone);
            }
        }
        #endregion

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft <= 3)
                return false;

            Effect effect = Scene["TarTrail"].GetShader().Shader;
            effect.Parameters["repeats"].SetValue(2);
            effect.Parameters["outlineColor"].SetValue(new Color(12, 0, 0).ToVector4());
            effect.Parameters["fadeInColor"].SetValue(new Color(78, 50, 43).ToVector4());
            effect.Parameters["scroll"].SetValue(Main.GameUpdateCount * 0.05f);
            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "FluidTrail").Value);
            TrailDrawer?.Render(effect, - Main.screenPosition);

            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.DodgerBlue with { A = 0 } * 0.3f, 0f, bloom.Size() / 2f, Projectile.scale * 0.2f, SpriteEffects.None, 0);


            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center- Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.DodgerBlue with { A = 0 } * 0.5f, Projectile.rotation, tex.Size() / 2f, Projectile.scale * 1.2f, SpriteEffects.None, 0);

            Texture2D shine = AssetDirectory.CommonTextures.BloomStreak.Value;
            Vector2 scale = new Vector2(1.2f, 1.8f);
            float shineOpacity = Utils.GetLerpValue(6f, 0f, Math.Abs(Projectile.velocity.Y), true);

            Main.EntitySpriteDraw(shine, Projectile.Center - Main.screenPosition, null, Color.DodgerBlue with { A = 0 } * 0.6f * shineOpacity, MathHelper.PiOver2, shine.Size() / 2f, scale * Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(shine, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 } * shineOpacity, MathHelper.PiOver2, shine.Size() / 2f, scale * 0.6f * Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public void ShineBlastVisual()
        {
            Texture2D shine = AssetDirectory.CommonTextures.BloomStreak.Value;
            Vector2 scale = new Vector2(1.2f, 1.8f) * Projectile.scale * (2 - (Projectile.timeLeft / 3f));
            Vector2 origin = shine.Size() / 2f;

            for (int i = 0; i < 2; i++)
            {
                float rotation = i * MathHelper.PiOver2;
                Main.EntitySpriteDraw(shine, Projectile.Center - Main.screenPosition, null, Color.DodgerBlue with { A = 0 } * 0.6f, rotation, origin, scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(shine, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 }, rotation, origin, scale * 0.6f, SpriteEffects.None, 0);
            }
        }
    }


    public class InkyCapSpores : ModBuff
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            DisplayName.SetDefault("Inky Cap Spores");
            Description.SetDefault("Increased regeneration");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen += 10;

            if (Main.rand.NextBool(3))
            {
                int dustType = DustID.MushroomSpray;
                if (Main.rand.NextBool())
                    dustType = ModContent.DustType<SpectralWaterDustEmbers>();

                Vector2 offset = Main.rand.NextVector2Circular(20f, 30f);

                Dust d = Dust.NewDustPerfect(player.Center + offset, dustType, -Vector2.UnitY * Main.rand.NextFloat(1f, 3f), 0);
                d.noGravity = true;
                d.noLight = true;
            }

            if (Main.rand.NextBool(16))
            {
                Rectangle spawnPosition = player.Hitbox;
                spawnPosition.Inflate(20, 20);

                Vector2 position = Main.rand.NextVector2FromRectangle(spawnPosition);
                float scale = Main.rand.NextFloat(0.5f, 1f);
                Color baseColor = Color.Lerp(Color.White, Color.DodgerBlue, Main.rand.NextFloat(0f, 0.5f));
                ParticleHandler.SpawnParticle(new HealingCrossParticle(position, -Vector2.UnitY * 0.1f, baseColor, Color.DodgerBlue * 0.5f, scale, Main.rand.Next(30, 60)));

            }
        }
    }

}
