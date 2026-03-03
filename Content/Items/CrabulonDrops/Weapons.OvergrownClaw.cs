using CalamityFables.Content.Items.EarlyGameMisc;
using CalamityFables.Particles;
using ReLogic.Utilities;
using System.IO;
using static CalamityFables.Content.Items.CrabulonDrops.OvergrownClaw;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    [ReplacingCalamity("MycelialClaws")]
    public class OvergrownClaw : ModItem, IHideFrontArm
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        #region Reflection Fields
        public static Vector2 SLAM_DAMAGE_MULT_RANGE = new(1f, 2f);
        public static Vector2 SLAM_DAMAGE_VELOCITY_RANGE = new(6f, 22f);
        public static int MAX_GRAB_DISTANCE = 350;
        public static int MIN_GRAB_DISTANCE = 180;
        public static int MAX_GRAB_TIME = 360;
        public static Vector2 SLAM_AOE_DAMAGE_MULT_RANGE = new(0.75f, 1.5f);
        public readonly static float SLAM_AOE_RADIUS = 120;
        public static float RIP_OUT_DAMAGE_MULT = 1.8f;
        public static int RIP_OUT_TIME = 45;
        public static Vector2 SWINGING_DAMAGE_MULT_RANGE = new(1f, 2f);
        public static Point FALL_DAMAGE_RANGE = new(50, 90);
        public static int CRUSH_DPS = 20;

        #endregion

        public bool ShouldHideArm(Player player) => player.ownedProjectileCounts[Item.shoot] >= 1;

        public static readonly SoundStyle ClawSlamSound = new SoundStyle("CalamityFables/Sounds/CrabulonDrops/OvergrownClawSlam", 2);
        public static readonly SoundStyle ClawReleaseSound = new SoundStyle("CalamityFables/Sounds/CrabulonDrops/OvergrownClawRelease", 2);
        public static readonly SoundStyle ClawRipSound = new SoundStyle("CalamityFables/Sounds/CrabulonDrops/OvergrownClawRip", 2);
        public static readonly SoundStyle ClawGrabSound = new SoundStyle("CalamityFables/Sounds/CrabulonDrops/OvergrownClawGrab");
        public static readonly SoundStyle ClawSwingLoop = new SoundStyle("CalamityFables/Sounds/CrabulonDrops/OvergrownClawDD2Modification");
        public static readonly SoundStyle ClawVineStrainLoop = new SoundStyle("CalamityFables/Sounds/CrabulonDrops/OvergrownClawVineStrainLoop");

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Overgrown Claw");
            Tooltip.SetDefault("Launches a fungal claw to grab your foes\n" +
                "Small enemies can be moved and thrown, or slammed into terrain\n" +
                "Pull your cursor away from a large target to tear the claw out, dealing high damage");
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ToolTipDamageMultiplier[Type] = SLAM_DAMAGE_MULT_RANGE.Y;
        }

        public override void SetDefaults()
        {
            Item.damage = 65;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.width = 23;
            Item.height = 8;

            Item.useAnimation = 45;
            Item.useTime = 45;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 8f;
            Item.value = Item.buyPrice(0, 7, 50, 0);
            Item.rare = ItemRarityID.Green;
            Item.autoReuse = false;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<OvergrownClawProjectile>();
            Item.shootSpeed = 24f;
            Item.noUseGraphic = true;
            Item.UseSound = SerratedFiberCloak.ThrowSound with { Pitch = -0.5f };
        }

        public override void HoldItem(Player player) => player.SyncMousePosition();

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.FirstOrDefault(n => n.Name == "CritChance").Text = SpikedBall.CantCritText.Value;
        }
    }

    public class OvergrownClawProjectile : ModProjectile, IDrawPixelated
    {
        public static Asset<Texture2D> ClawMouthAsset;
        public static Asset<Texture2D> ChainAsset;
        public static Asset<Texture2D> ArmGraftAsset;
        public static Asset<Texture2D> CrackTexture;

        private PrimitiveSliceTrail swingSmearTrailDrawer;
        private List<Vector2> smearSliceOuterTrailCache;
        private List<Vector2> smearSliceInnerTrailCache;

        private readonly static int SmearTrailLength = 20;

        internal PrimitiveTrail VineTrailDrawer;
        public List<Vector2> vineTrailCache;

        public Player Owner => Main.player[Projectile.owner];

        public NPC grabbedNPC;
        public Vector2 grabOffset;
        public Vector2 grabOffsetRotated;
        public float grabRotation;
        public float maxGrabDistance;
        public int initialFlip;
        public Vector2 groundCrackPosition;
        public SlotId wooshSoundLoopSlotID;
        public SlotId strainSoundLoopSlotID;

        public BezierCurve vineBezier;
        public Vector2[] vineBezierControls;

        public enum AIState
        {
            Flying,
            GrabLight,
            GrabHeavy,
            PostGrabStun,
            Retracting
        }

        public AIState State
        {
            get { return (AIState)Projectile.ai[1]; }
            set { Projectile.ai[1] = (int)value; }
        }

        public enum GrabEndType
        {
            Slam,
            ReleaseLight,
            ReleaseHeavy,
            Rip
        }

        public Vector2 VisualCenter
        {
            get
            {
                if (State != AIState.GrabLight)
                    return Projectile.Center;

                return Projectile.Center + grabOffset + grabOffsetRotated.RotatedBy(Projectile.rotation - grabRotation);
            }
        }

        public GrabEndType grabEndType;

        public int flyTimer;
        public int tearOffTimer;
        public int grabTimer;
        public int retractTrailIntensityCountDown;

        public float vineStateLerp = 0;

        public bool CanBeUsed => Owner.active && !Owner.dead && !Owner.noItems && !Owner.CCed && Owner.channel;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Claw");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 4000;
        }

        public override string Texture => AssetDirectory.CrabulonDrops + "OvergrownClawClaw";

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.friendly = true;
            Projectile.timeLeft = 400;
            Projectile.penetrate = -1;
            Projectile.hide = true;

            Projectile.extraUpdates = 1;
            Projectile.CritChance = 0;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float clawLenght = 35 * Projectile.scale;
            float clawWidth = 15 * Projectile.scale;
            Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center + normal * 10f, Projectile.Center + normal * clawLenght, clawWidth, ref collisionPoint);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(flyTimer);
            writer.Write(grabbedNPC?.whoAmI ?? -1);
            writer.WriteVector2(grabOffset);
            writer.Write(grabRotation);
            writer.Write(maxGrabDistance);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            flyTimer = reader.ReadInt32();

            int npcIndex = reader.ReadInt32();
            grabbedNPC = npcIndex >= 0 ? Main.npc[npcIndex] : null;

            grabOffset = reader.ReadVector2();
            grabRotation = reader.ReadSingle();
            maxGrabDistance = reader.ReadSingle();
        }

        void FlyingAI()
        {
            if (!Projectile.WithinRange(Owner.Center, 1200) || flyTimer > 40)
            {
                State = AIState.Retracting;
                Projectile.velocity *= 0.6f;
            }
            else
            {
                Projectile.velocity *= 0.94f;

                float projectileVelocityAngle = Projectile.velocity.ToRotation();
                float velocityLenght = Projectile.velocity.Length();
                float redirectionPower = Utils.GetLerpValue(4f, 27f, velocityLenght, true);

                if (redirectionPower > 0)
                {
                    float pointDirection = Owner.MountedCenter.AngleTo(Owner.MouseWorld());
                    float newVelocityAngle = projectileVelocityAngle.AngleTowards(pointDirection, 0.1f * redirectionPower);

                    Projectile.velocity = newVelocityAngle.ToRotationVector2() * velocityLenght;
                }
            }

            Projectile.rotation = Utils.AngleLerp(Projectile.velocity.ToRotation(), Owner.MountedCenter.SafeDirectionTo(Projectile.Center).ToRotation(), (float)flyTimer / 40f);

            flyTimer++;
            Owner.direction = FablesUtils.NonZeroSign(Projectile.Center.X - Owner.MountedCenter.X);
        }

        void GrabLightAI()
        {
            Projectile.netUpdate = true; //todo find out if theres a better place to put this
            bool canStrikeEnemy = grabbedNPC != null && grabbedNPC.active && !grabbedNPC.dontTakeDamage;
            bool hookTooFar = grabbedNPC != null && !grabbedNPC.WithinRange(Owner.Center, 1200);

            float rot = Utils.AngleTo(Owner.MountedCenter, VisualCenter + Projectile.velocity).AngleTowards(vineBezier.Evaluate(0.99f).SafeDirectionTo(vineBezier.Evaluate(1f)).ToRotation(), MathHelper.Lerp(0.06f, MathHelper.Pi, grabbedNPC.knockBackResist));
            if (float.IsNaN(rot)) rot = 0;
            Projectile.rotation = rot;

            if (!CanBeUsed || !canStrikeEnemy || hookTooFar)
            {
                ThrowEnemy();
                return;
            }
            else
            {
                Vector2 aimDir = Owner.MountedCenter.SafeDirectionTo(Owner.MouseWorld());
                Vector2 dirToClaw = Owner.MountedCenter.SafeDirectionTo(VisualCenter);

                float rotDiff; //= -dirToClaw.ToRotation().AngleBetween(aimDir.ToRotation());

                if (dirToClaw.ToRotation() - aimDir.ToRotation() > MathHelper.Pi)
                {
                    rotDiff = MathHelper.TwoPi - (aimDir.ToRotation() - dirToClaw.ToRotation());
                }
                else if (dirToClaw.ToRotation() - aimDir.ToRotation() < -MathHelper.Pi)
                {
                    rotDiff = (aimDir.ToRotation() - dirToClaw.ToRotation()) - MathHelper.TwoPi;
                }
                else
                {
                    rotDiff = aimDir.ToRotation() - dirToClaw.ToRotation();
                }

                Vector2 moveVelocity = dirToClaw.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.One) * (Math.Min(dirToClaw.AngleBetween(aimDir), 0.4f) / 0.4f) * MathF.Sign(rotDiff);

                float moveAdjustment = MathHelper.Lerp(0.33f * grabbedNPC.knockBackResist + 0.07f, 0.8f, (Vector2.Dot(Vector2.UnitY, moveVelocity.SafeNormalize(Vector2.Zero)) + 1) / 2);
                moveAdjustment *= 0.5f + Utils.GetLerpValue(0f, 12f, Projectile.velocity.Length());

                if (maxGrabDistance > MAX_GRAB_DISTANCE)
                    maxGrabDistance -= 5;

                Vector2 toClaw = VisualCenter - Owner.MountedCenter;

                Projectile.velocity *= 0.98f;
                Projectile.velocity += moveVelocity * moveAdjustment * MathHelper.Lerp(1f, 0.8f, Utils.GetLerpValue(MIN_GRAB_DISTANCE, MAX_GRAB_DISTANCE, toClaw.Length()));

                //let go if enemy teleports away
                if (grabbedNPC.Distance(Projectile.Center) > 40 + Projectile.velocity.Length())
                {
                    ThrowEnemy();
                    return;
                }

                if (toClaw.Length() >= maxGrabDistance)
                {
                    Vector2 targetPos = Owner.MountedCenter + toClaw.SafeNormalize(Vector2.One) * maxGrabDistance;
                    
                    for (int i = 0; i < 80; i++)
                    {
                        Vector2 distClampDir = VisualCenter.SafeDirectionTo(targetPos);

                        if (VisualCenter.Distance(Owner.MountedCenter) <= maxGrabDistance || Collision.SolidCollision(Projectile.position + distClampDir, Projectile.width, Projectile.height, false))
                            break;

                        Projectile.Center += distClampDir;
                    }

                    Projectile.velocity -= toClaw.SafeNormalize(Vector2.One);

                    if (toClaw.Length() >= maxGrabDistance + 60)
                    {
                        RipOut();
                        Projectile.tileCollide = false;
                        //ThrowEnemy();
                        return;
                    }
                }
                else
                {
                    Projectile.velocity += dirToClaw * 0.3f;
                }

                grabbedNPC.Center = Projectile.Center + Projectile.velocity;

                if (!Main.dedServ)
                {
                    if (!SoundEngine.TryGetActiveSound(wooshSoundLoopSlotID, out var activeWooshSound))
                    {
                        wooshSoundLoopSlotID = SoundEngine.PlaySound(ClawSwingLoop, VisualCenter);
                        SoundEngine.TryGetActiveSound(wooshSoundLoopSlotID, out activeWooshSound);
                    }

                    if (activeWooshSound is not null)
                    {
                        activeWooshSound.Position = VisualCenter;
                        activeWooshSound.Pitch = -0.6f + MathHelper.Clamp((Projectile.velocity.Length() - 6f) / 10f, 0f, 1f);
                        activeWooshSound.Volume = MathHelper.Clamp((Projectile.velocity.Length() - 6f) / 10f, 0f, 1f);

                        activeWooshSound.Update();

                        SoundHandler.TrackSoundWithFade(wooshSoundLoopSlotID);
                    }

                    if (!SoundEngine.TryGetActiveSound(strainSoundLoopSlotID, out var activeStrainSound))
                    {
                        strainSoundLoopSlotID = SoundEngine.PlaySound(ClawVineStrainLoop, VisualCenter);
                        SoundEngine.TryGetActiveSound(strainSoundLoopSlotID, out activeStrainSound);
                    }

                    if (activeStrainSound is not null)
                    {
                        activeStrainSound.Position = VisualCenter;
                        //activeStrainSound.Pitch = MathHelper.Clamp(Projectile.velocity.Length() / 3f, 0f, 1f) - 0.5f;

                        //activeStrainSound.Volume = MathHelper.Clamp((Projectile.velocity.Length() - 2f) / 4f, 0f, 1f) - (float)Math.Pow(Projectile.velocity.Length() / 10f, 2);

                        activeStrainSound.Volume = (MathHelper.Clamp(1f - Vector2.Dot(Projectile.velocity, moveVelocity), 0f, 1f) + (1f - (Projectile.velocity.Length() / 12f))) * MathHelper.Clamp((Projectile.velocity.Length() - 1f) / 2f, 0.3f, 1f);
                        activeStrainSound.Pitch = MathHelper.Clamp(Projectile.velocity.Length() / 32f, -0.5f, 0.3f);

                        activeStrainSound.Update();

                        SoundHandler.TrackSoundWithFade(strainSoundLoopSlotID);
                    }
                }

                grabTimer++;

                if (grabTimer >= MAX_GRAB_TIME)
                {
                    ThrowEnemy();
                    return;
                }

                Owner.direction = FablesUtils.NonZeroSign(Owner.MouseWorld().X - Owner.MountedCenter.X);
            }
        }

        void GrabHeavyAI()
        {
            bool canStrikeEnemy = grabbedNPC != null && grabbedNPC.active && !grabbedNPC.dontTakeDamage;
            bool hookTooFar = grabbedNPC != null && !grabbedNPC.WithinRange(Owner.Center, 1200);

            if (!CanBeUsed || !canStrikeEnemy || hookTooFar)
            {
                LetGoHeavy();
                return;
            }
            else
            {
                grabTimer += 1;// + (int)(MathHelper.Max(Owner.MountedCenter.Distance(Projectile.Center) - MAX_GRAB_DISTANCE, 0) / 10); ;

                Projectile.velocity *= 0f;

                if (grabbedNPC.Distance(Projectile.Center - grabOffset) > 40 + grabbedNPC.velocity.Length())
                {
                    LetGoHeavy();
                    return;
                }

                Projectile.Center = grabbedNPC.Center + grabOffset;

                if (Vector2.Dot(Owner.MountedCenter.SafeDirectionTo(Owner.MouseWorld()), Owner.MountedCenter.SafeDirectionTo(Projectile.Center)) < 0.7f
                    || Owner.MountedCenter.Distance(Projectile.Center) > MAX_GRAB_DISTANCE)
                {
                    //rip timer goes faster when claw is out of range
                    tearOffTimer += 1 + (int)(MathHelper.Max(Owner.MountedCenter.Distance(Projectile.Center) - MAX_GRAB_DISTANCE, 0) / 10);

                    if (tearOffTimer >= RIP_OUT_TIME)
                    {
                        RipOut();
                    }
                }
                else if (tearOffTimer > 0)
                {
                    tearOffTimer--;
                }

                if (!Main.dedServ)
                {
                    if (!SoundEngine.TryGetActiveSound(strainSoundLoopSlotID, out var activeStrainSound))
                    {
                        strainSoundLoopSlotID = SoundEngine.PlaySound(ClawVineStrainLoop, Projectile.Center);
                        SoundEngine.TryGetActiveSound(strainSoundLoopSlotID, out activeStrainSound);
                    }

                    if (activeStrainSound is not null)
                    {
                        activeStrainSound.Position = Projectile.Center;

                        activeStrainSound.Volume = (float)tearOffTimer / RIP_OUT_TIME;
                        activeStrainSound.Pitch = MathHelper.Lerp(0f, 0.3f, (float)tearOffTimer / RIP_OUT_TIME);

                        activeStrainSound.Update();

                        SoundHandler.TrackSoundWithFade(strainSoundLoopSlotID);
                    }
                }

                if (grabTimer >= MAX_GRAB_TIME)
                {
                    LetGoHeavy();
                    return;
                }

                float rot = grabRotation.AngleTowards(vineBezier.Evaluate(0.95f).SafeDirectionTo(vineBezier.Evaluate(0.96f)).ToRotation(), 0.1f);
                if (float.IsNaN(rot)) rot = 0;
                Projectile.rotation = rot;
                Owner.direction = FablesUtils.NonZeroSign(Owner.MouseWorld().X - Owner.MountedCenter.X);
            }
        }

        void StunAI()
        {
            Vector2 toClaw = Projectile.Center - Owner.MountedCenter;

            Projectile.velocity *= 0.96f;

            if (grabTimer == 0)
                maxGrabDistance = toClaw.Length() + 50f;

            else if (toClaw.Length() > maxGrabDistance)
            {
                Projectile.Center = Owner.MountedCenter + toClaw.SafeNormalize(Vector2.One) * maxGrabDistance;
                Projectile.velocity -= toClaw.SafeNormalize(Vector2.One);
            }

            grabTimer++;

            if (grabTimer > 10)
            {
                State = AIState.Retracting;
            }

            retractTrailIntensityCountDown--;

            Projectile.rotation = vineBezier.Evaluate(0.99f).SafeDirectionTo(vineBezier.Evaluate(1f)).ToRotation();
            Owner.direction = FablesUtils.NonZeroSign(Owner.MouseWorld().X - Owner.MountedCenter.X);
        }

        void RetractingAI()
        {
            Projectile.tileCollide = false;

            float distanceToPlayer = (Owner.Center - Projectile.Center).Length();
            //Retracts instantly if either too far or too close
            if (distanceToPlayer > 3000f || distanceToPlayer <= 30)
            {
                Projectile.netUpdate = true;
                Projectile.Kill();
                return;
            }

            Vector2 towardsOwner = Projectile.SafeDirectionTo(Owner.Center);
            float speed = Projectile.velocity.Length();
            float dot = Vector2.Dot(towardsOwner, Projectile.velocity.SafeNormalize(Vector2.UnitY));
            Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(Projectile.AngleTo(Owner.Center), 0.13f).ToRotationVector2() * speed;

            //Accelerate back towards the player
            Projectile.velocity += towardsOwner;
            if (dot <= 0)
                Projectile.velocity += towardsOwner;

            //Clamp the max magnitude of the velocity
            float maxVelocity = 18f;
            if (Projectile.velocity.Length() > maxVelocity && distanceToPlayer < 1000f)
                Projectile.velocity = Projectile.velocity.ClampMagnitude(maxVelocity);

            retractTrailIntensityCountDown--;

            Projectile.rotation = Owner.MountedCenter.SafeDirectionTo(Projectile.Center).ToRotation();

            Owner.direction = FablesUtils.NonZeroSign(Owner.MouseWorld().X - Owner.MountedCenter.X);
        }

        void LetGoHeavy()
        {
            Projectile.netUpdate = true;
            EndGrab(GrabEndType.ReleaseHeavy);
        }

        void ThrowEnemy()
        {
            Projectile.netUpdate = true;
            new KnockbackNPCPacket(grabbedNPC.whoAmI, new Vector2(Projectile.velocity.X, Projectile.velocity.Y * 1.5f) - grabbedNPC.velocity).Send();

            grabbedNPC.GetGlobalNPC<FallDamageNPC>().ApplyFallDamage(grabbedNPC, FALL_DAMAGE_RANGE.X, FALL_DAMAGE_RANGE.Y);

            EndGrab(GrabEndType.ReleaseLight);
        }

        void SlamEnemy(Vector2 oldVelocity)
        {
            Vector2 groundEffectPosition = grabbedNPC.Center + Projectile.oldVelocity;

            Collision.HitTiles(groundEffectPosition - new Vector2(Projectile.width * 2, Projectile.width), Vector2.Zero, Projectile.width * 4, Projectile.height * 2);

            Vector2 groundSmokeNormal = Vector2.UnitY;
            if (Projectile.velocity.X != oldVelocity.X)
                groundSmokeNormal = Vector2.UnitX * Math.Sign(oldVelocity.X);
            else if (Projectile.velocity.Y != oldVelocity.Y)
                groundSmokeNormal = Vector2.UnitY * Math.Sign(oldVelocity.Y);

            for (int i = 0; i < 10; i++)
            {
                Vector2 smokeVelocity = groundSmokeNormal.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f);

                ParticleHandler.SpawnParticle
                (
                    new SporeGas(groundEffectPosition, smokeVelocity, groundEffectPosition, 50f, 2f)
                );
            }

            if (Main.myPlayer == Projectile.owner)
            {
                //Deals more damage when going fast
                float damageMult = Utils.Remap(oldVelocity.Length(), SLAM_DAMAGE_VELOCITY_RANGE.X, SLAM_DAMAGE_VELOCITY_RANGE.Y, SLAM_DAMAGE_MULT_RANGE.X, SLAM_DAMAGE_MULT_RANGE.Y);
                int damage = (int)(Projectile.damage * damageMult);
                Owner.ApplyDamageToNPC(grabbedNPC, damage, 0f, 0, false, Projectile.DamageType, true);

                float slamDamageMult = Utils.Remap(oldVelocity.Length(), SLAM_DAMAGE_VELOCITY_RANGE.X, SLAM_DAMAGE_VELOCITY_RANGE.Y, SLAM_AOE_DAMAGE_MULT_RANGE.X, SLAM_AOE_DAMAGE_MULT_RANGE.Y);
                int slamDamage = (int)(Projectile.damage * slamDamageMult);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.MoveTowards(grabbedNPC.Center, 20f), Vector2.Zero, ModContent.ProjectileType<OvergrownClawSlamHitbox>(), slamDamage, 0, Main.myPlayer, grabbedNPC.whoAmI);
            }

            new KnockbackNPCPacket(grabbedNPC.whoAmI, new Vector2(0f, -4f - grabbedNPC.velocity.Y)).Send();

            Projectile.velocity *= 0f;

            Projectile.velocity -= groundSmokeNormal * 3f;

            EndGrab(GrabEndType.Slam);

            SoundEngine.PlaySound(ClawSlamSound, Projectile.Center);

            groundCrackPosition = Projectile.Center;
        }

        void RipOut()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                int damage = (int)(Projectile.damage * RIP_OUT_DAMAGE_MULT);
                Owner.ApplyDamageToNPC(grabbedNPC, damage, 0f, 0, false, Projectile.DamageType, true);
            }

            //todo make the direction better (???)
            Projectile.velocity += Projectile.Center.SafeDirectionTo(Owner.MouseWorld()) * 5f;

            Vector2 ripPosition = Projectile.Center.MoveTowards(grabbedNPC.Center, 32);

            EndGrab(GrabEndType.Rip);

            Projectile.velocity += Projectile.Center.SafeDirectionTo(Owner.MouseWorld()) * 6f;

            SoundEngine.PlaySound(ClawRipSound, Projectile.Center);

            for (int i = 0; i < 18; i++)
            {
                Dust d = Dust.NewDustDirect(ripPosition, 4, 4, DustID.Blood, Scale: Main.rand.NextFloat(1.2f, 1.7f));
                d.velocity = Projectile.velocity.RotatedByRandom(1f) * Main.rand.NextFloat(0.1f, 1f);
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 smokeVelocity = Projectile.velocity.RotatedByRandom(1f) * Main.rand.NextFloat(0.1f, 0.3f);

                ParticleHandler.SpawnParticle
                (
                    new SporeGas(ripPosition, smokeVelocity, ripPosition, 50f, 2f) { FrontLayer = false }
                );
            }
        }

        public override void AI()
        {
            vineBezier ??= new();

            Projectile.netUpdate = true; //DEBUG REMOVE LATER

            if (initialFlip == 0)
                initialFlip = -Projectile.velocity.X.NonZeroSign();

            if (State != AIState.GrabLight && State != AIState.PostGrabStun && State != AIState.GrabHeavy)
                Projectile.rotation = Owner.MountedCenter.SafeDirectionTo(Projectile.Center).ToRotation();

            Owner.UpdateBasicHoldoutVariables(Projectile, 20);

            vineStateLerp = MathHelper.Clamp(vineStateLerp + ((State == AIState.GrabLight || State == AIState.GrabHeavy) ? 0.05f : -0.05f), 0f, 1f);

            if (State == AIState.Flying)
                FlyingAI();

            if (State == AIState.GrabLight)
                GrabLightAI();

            if (State == AIState.GrabHeavy)
                GrabHeavyAI();

            if (State == AIState.PostGrabStun)
                StunAI();

            if (State == AIState.Retracting)
                RetractingAI();

            if (State == AIState.Flying || State == AIState.PostGrabStun || State == AIState.Retracting)
            {
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (vineBezier.Evaluate(0f).SafeDirectionTo(vineBezier.Evaluate(0.05f)).ToRotation() - MathHelper.PiOver2) * Owner.gravDir);

            }
            else if (State == AIState.GrabLight || State == AIState.GrabHeavy)
            {
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Owner.MountedCenter.SafeDirectionTo(Owner.MouseWorld()).ToRotation() - MathHelper.PiOver2) * Owner.gravDir);

            }

            //Vector2 rotationVelocity = Projectile.velocity;
            //if (State == AIState.Retracting)
            //    rotationVelocity *= -1;

            ManageVineBezier();

            if (!Main.dedServ)
            {
                ManageCache();
                ManageTrail();
                UpdateMouthRotation();
            }
        }

        public void EndGrab(GrabEndType grabEnd)
        {
            if ((grabEnd == GrabEndType.Slam || grabEnd == GrabEndType.ReleaseLight) && !grabbedNPC.noTileCollide)
            {
                //attempt to move npc out of terrain if it was let go while in it
                //Vector2 shiftBack = grabbedNPC.Center.SafeDirectionTo(Projectile.Center) * (grabbedNPC.Center.Distance(Projectile.Center));

                //Point? moveNpcOutOfGroundRaycaseHit = grabbedNPC.Center.RaytraceToFirstSolid(Projectile.Center);

                //if (moveNpcOutOfGroundRaycaseHit is not null)
                //{
                //    grabbedNPC.Center = new Vector2(moveNpcOutOfGroundRaycaseHit.Value.X * 16, moveNpcOutOfGroundRaycaseHit.Value.Y * 16) + shiftBack;
                //}

                for (int i = 0; i < (int)grabbedNPC.Size.Length(); i++)
                {
                    if (!Collision.SolidCollision(grabbedNPC.TopLeft, grabbedNPC.width, grabbedNPC.height))
                        break;

                    grabbedNPC.Center += new Vector2(0, Math.Sign(Projectile.Center.Y - grabbedNPC.Center.Y));
                }

                if (Collision.SolidCollision(grabbedNPC.TopLeft, grabbedNPC.width, grabbedNPC.height))
                {
                    //grabbedNPC.Center += new Vector2(0, Math.Sign(Projectile.Center.Y - grabbedNPC.Center.Y) * (int)grabbedNPC.Size.Length());

                    for (int i = 0; i < (int)grabbedNPC.Size.Length(); i++)
                    {
                        if (!Collision.SolidCollision(grabbedNPC.TopLeft, grabbedNPC.width, grabbedNPC.height))
                            break;

                        grabbedNPC.Center += new Vector2(Math.Sign(Projectile.Center.X - grabbedNPC.Center.X), 0);
                    }
                }
            }

            SoundEngine.PlaySound(ClawReleaseSound, grabbedNPC.Center);

            if (State == AIState.GrabLight)
            {
                grabbedNPC.Center = Projectile.Center;
            }

            Projectile.Center = VisualCenter;
            grabbedNPC = null;
            grabTimer = 0;
            tearOffTimer = 0;
            State = AIState.PostGrabStun;
            grabEndType = grabEnd;
            retractTrailIntensityCountDown = grabEnd != GrabEndType.ReleaseLight ? 0 : (int)(MathHelper.Clamp(Projectile.velocity.Length() / 10f * 5f + 30f, 0f, 1f) * 90f);
        }

        #region Collision stuff
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.netUpdate = true;

            float impactVelChange = (oldVelocity.Length() - Projectile.velocity.Length()) * (2.5f - (grabbedNPC?.knockBackResist ?? 1f) * 1.5f);

            //Retract if hitting the floor too hard
            if (State == AIState.Flying)
            {
                if (impactVelChange > 2f)
                {
                    Projectile.velocity = oldVelocity.SafeNormalize(Vector2.Zero) * 0.4f;
                    State = AIState.PostGrabStun;
                    grabTimer = 0;
                    SoundEngine.PlaySound(ClawGrabSound, Projectile.Center);
                }
                //Skid along the floor otherwise
                else
                    Projectile.velocity *= 0.95f;
            }

            if (State == AIState.GrabLight && grabbedNPC is not null)
            {
                Projectile.netUpdate = true;

                impactVelChange *= 1f - Math.Max(Vector2.Dot(Owner.MountedCenter.SafeDirectionTo(VisualCenter), Projectile.oldVelocity.SafeNormalize(Vector2.Zero)), 0);

                if (impactVelChange > 6f)
                {
                    SlamEnemy(oldVelocity);
                }
            }

            return false;
        }
        #endregion

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Very low damage to grab
            if (State == AIState.Flying)
                modifiers.SourceDamage *= 0.2f;

            // Direct hits when swinging an enemy
            else if (State == AIState.GrabLight)
            {
                float damageMult = Utils.Remap(Projectile.velocity.Length(), SLAM_DAMAGE_VELOCITY_RANGE.X, SLAM_DAMAGE_VELOCITY_RANGE.Y, SWINGING_DAMAGE_MULT_RANGE.X, SWINGING_DAMAGE_MULT_RANGE.Y);
                modifiers.SourceDamage *= damageMult;
            }

            modifiers.DisableCrit();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != AIState.Flying)
                return;

            // loop through all projectiles to check if the target is already grabbed by another claw
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].ModProjectile is not null
                    && Main.projectile[i].active
                    && Main.projectile[i].ModProjectile is OvergrownClawProjectile projectile
                    && projectile.grabbedNPC?.whoAmI == target.whoAmI)
                {
                    return;
                }
            }

            Projectile.netUpdate = true;

            if (target.life <= 0) 
                return;

            Projectile.velocity *= 0.2f;
            grabbedNPC = target;
            grabOffset = (Projectile.Center - grabbedNPC.Center);
            grabRotation = Projectile.rotation;
            if (CanGrabLight(grabbedNPC))
            {
                grabOffsetRotated = new Vector2(-30, 0).RotatedBy(Projectile.rotation);
                grabOffset -= grabOffsetRotated;
                //grabOffset = grabOffset.RotatedBy(-Projectile.rotation);
                Projectile.width = grabbedNPC.width;
                Projectile.height = grabbedNPC.height;
                Projectile.position = grabbedNPC.position;
            }

            maxGrabDistance = MathF.Max(MIN_GRAB_DISTANCE, Owner.MountedCenter.Distance(VisualCenter) + 20f);
            State = CanGrabLight(grabbedNPC) ? AIState.GrabLight : AIState.GrabHeavy;

            //Attached to a baloon
            if (target.type == NPCID.BlueSlime && target.ai[0] == -999f)
            {
                target.ai[0] = 0;
                target.netUpdate = true;
            }

            SoundEngine.PlaySound(ClawGrabSound, VisualCenter);
        }

        public override bool? CanDamage() => (State == AIState.Flying || (State == AIState.GrabLight && Projectile.velocity.Length() >= SLAM_DAMAGE_VELOCITY_RANGE.X)) ? null : false;  // Allow damage while swinging
        public override bool? CanHitNPC(NPC target) => target == grabbedNPC ? false : null; // Prevent hitting grabbed NPC

        #region Prim stuff
        public void ManageTrail()
        {
            VineTrailDrawer = VineTrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);
            VineTrailDrawer.SetPositionsSmart(vineTrailCache, Projectile.position, FablesUtils.RigidPointRetreivalFunction);
            swingSmearTrailDrawer ??= new PrimitiveSliceTrail(20, SwingSmearColorFunc);

            swingSmearTrailDrawer.SetPositions(smearSliceOuterTrailCache, smearSliceInnerTrailCache);
        }

        public static Color SwingSmearColorFunc(float progress) => Color.Lerp(Color.DarkGray, Color.SlateGray, progress);

        private void ManageVineBezier()
        {
            Vector2 midControlDir = Owner.MountedCenter.SafeDirectionTo(Owner.MouseWorld());

            Vector2 projCenterAfterVelocity = VisualCenter + Projectile.velocity;

            if (State != AIState.GrabHeavy && State != AIState.Flying)
            {
                vineBezierControls =
                [
                    Owner.MountedCenter,
                    Vector2.Lerp(Owner.MountedCenter + midControlDir * Owner.MountedCenter.Distance(projCenterAfterVelocity) / 1.5f, Owner.MountedCenter, 1f - vineStateLerp),
                    projCenterAfterVelocity
                ];
            }
            else if (State == AIState.Flying)
            {
                vineBezierControls =
                [
                    Owner.MountedCenter,
                    projCenterAfterVelocity + new Vector2(-Owner.MountedCenter.Distance(projCenterAfterVelocity) / 1.5f, 0).RotatedBy(Projectile.rotation),
                    projCenterAfterVelocity
                ];
            }
            else
            {
                vineBezierControls =
                [
                    Owner.MountedCenter,
                    Owner.MountedCenter + midControlDir * Owner.MountedCenter.Distance(projCenterAfterVelocity) / 1.5f,
                    projCenterAfterVelocity + new Vector2(-48, 0).RotatedBy(grabRotation + Main.rand.NextFloat(-0.2f, 0.2f) * ((float)tearOffTimer / RIP_OUT_TIME)),
                    projCenterAfterVelocity
                ];
            }

            vineBezier = new BezierCurve(vineBezierControls);
        }

        private void ManageCache()
        {
            if (vineTrailCache == null)
            {
                vineTrailCache = new();
                for (int i = 0; i < 20; i++)
                    vineTrailCache.Add(Vector2.Zero);
            }

            Vector2 projCenterAfterVelocity = VisualCenter + Projectile.velocity;

            Vector2 sSliceOuter = projCenterAfterVelocity + Vector2.UnitX.RotatedBy(Projectile.rotation) * 50;
            Vector2 sSliceInner = projCenterAfterVelocity - Vector2.UnitX.RotatedBy(Projectile.rotation) * 10;

            smearSliceOuterTrailCache ??= Enumerable.Repeat(sSliceOuter, SmearTrailLength).ToList();
            smearSliceInnerTrailCache ??= Enumerable.Repeat(sSliceInner, SmearTrailLength).ToList();

            smearSliceOuterTrailCache.Add(sSliceOuter);
            while (smearSliceOuterTrailCache.Count > SmearTrailLength)
                smearSliceOuterTrailCache.RemoveAt(0);

            smearSliceInnerTrailCache.Add(sSliceInner);
            while (smearSliceInnerTrailCache.Count > SmearTrailLength)
                smearSliceInnerTrailCache.RemoveAt(0);

            Vector2 perpendicular = Owner.MountedCenter.SafeDirectionTo(projCenterAfterVelocity).RotatedBy(MathHelper.PiOver2);
            float oscillations = 1f;
            float wobbleStrenght = 0;

            for (int i = 0; i < 20; i++)
            {
                Vector2 vineCurvePos = vineBezier.Evaluate(i / 19f);
                vineTrailCache[19 - i] = vineCurvePos + perpendicular * MathF.Sin(oscillations * i / 19f) * wobbleStrenght;
            }
        }

        public static Color ChainColor;
        internal Color ColorFunction(float completionRatio) => ChainColor;

        internal float WidthFunction(float completionRatio)
        {
            float stretch = 1f + 0.2f * Utils.GetLerpValue(400f, 0f, Owner.Center.Distance(VisualCenter), true);
            return ChainAsset.Height() / 2f * stretch;
        }

        public override void OnKill(int timeLeft)
        {
        }
        #endregion

        public float mouthRotation;

        public bool CanGrabLight(NPC npc) => npc.knockBackResist > 0f && !npc.boss;

        public void UpdateMouthRotation()
        {
            if (State == AIState.GrabLight || State == AIState.GrabHeavy || grabEndType == GrabEndType.Rip)
            {
                float mouthShutProgress = FablesUtils.ExpOutEasing(MathHelper.Clamp((float)grabTimer / 10f, 0, 1));
                mouthRotation = MathHelper.Lerp(-0.2f, 0.4f, mouthShutProgress) * initialFlip;
            }

            //progressively opens when flying
            if (State == AIState.Flying)
                mouthRotation = Utils.GetLerpValue(40f, 0f, flyTimer, true) * -0.5f * initialFlip;
            //Close when retracting
            else if (State == AIState.Retracting)
                mouthRotation = MathHelper.Lerp(mouthRotation, 0.5f * initialFlip, 0.15f);


            if (grabTimer > MAX_GRAB_TIME - 120)
                mouthRotation += Main.rand.NextFloat(-0.1f, 0.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ClawMouthAsset = ClawMouthAsset ?? ModContent.Request<Texture2D>(Texture + "Mouth");
            ChainAsset = ChainAsset ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "OvergrownClawChain");
            ArmGraftAsset ??= ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "OvergrownClaw_Arm");

            ChainColor = lightColor;

            Effect effect = AssetDirectory.PrimShaders.TextureMap;
            effect.Parameters["scroll"].SetValue(0);
            effect.Parameters["repeats"].SetValue(Owner.MountedCenter.Distance(VisualCenter) / (float)ChainAsset.Width());
            effect.Parameters["sampleTexture"].SetValue(ChainAsset.Value);
            VineTrailDrawer?.Render(effect, -Main.screenPosition);
            Texture2D tex = TextureAssets.Projectile[Type].Value;

            Vector2 shakeOffset = Main.rand.NextVector2Circular(4, 4) * ((float)tearOffTimer / RIP_OUT_TIME);
            float shakeRotOffset = Main.rand.NextFloat(-0.2f, 0.2f) * ((float)tearOffTimer / RIP_OUT_TIME);

            Vector2 mouthOrigin = new(2, 2);
            Vector2 mouthOffset = new(8, 10);

            if (initialFlip > 0)
            {
                mouthOffset.Y *= -1;
                mouthOrigin.Y = ClawMouthAsset.Value.Height - mouthOrigin.Y;
            }

            Vector2 mouthPosition = VisualCenter + mouthOffset.RotatedBy(Projectile.rotation) * Projectile.scale;
            Main.EntitySpriteDraw(ClawMouthAsset.Value, mouthPosition + shakeOffset - Main.screenPosition, null, lightColor, Projectile.rotation + mouthRotation + shakeRotOffset, mouthOrigin, Projectile.scale, initialFlip > 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);

            Vector2 clawOrigin = new Vector2(13, 15);
            SpriteEffects effects = SpriteEffects.None;

            float clawRotation = -0.4f + shakeRotOffset;

            if (initialFlip > 0)
            {
                clawOrigin.X = tex.Width - clawOrigin.X;
                effects = SpriteEffects.FlipHorizontally;
                clawRotation *= -1;
                clawRotation += MathHelper.Pi;
            }

            Main.EntitySpriteDraw(tex, VisualCenter + shakeOffset - Main.screenPosition, null, lightColor, Projectile.rotation + clawRotation, clawOrigin, Projectile.scale, effects);

            Vector2 graftPosition = Owner.MountedCenter + new Vector2(-2f * Owner.direction, -1f * Owner.gravDir);
            Vector2 graftOrigin = new((ArmGraftAsset.Width() / 2) + 1, ArmGraftAsset.Height() - 6);

            SpriteEffects graftEffects = SpriteEffects.None;

            if (Owner.MouseWorld().X > Owner.MountedCenter.X)
            {
                graftEffects = SpriteEffects.FlipHorizontally;
                graftOrigin.X -= 2;
            }
            float graftRotation = Owner.compositeFrontArm.rotation + MathHelper.Pi;

            Main.EntitySpriteDraw(ArmGraftAsset.Value, new Vector2((int)graftPosition.X, (int)graftPosition.Y) - Main.screenPosition, null, lightColor, graftRotation, graftOrigin, 1f, graftEffects);
            return false;
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect smearEffect = Scene["OvergrownClawSmearPrimitive"].GetShader().Shader;
            smearEffect.Parameters["time"].SetValue(0.8f + Main.GameUpdateCount * 0.1f);
            smearEffect.Parameters["sampleTexture"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);
            smearEffect.Parameters["edgeSliceColor"].SetValue(new Color(20, 30, 34).ToVector4());
            smearEffect.Parameters["edgeFadeColor"].SetValue(new Color(67, 67, 78).ToVector4());
            smearEffect.Parameters["flecksColor"].SetValue(new Color(90, 50, 255).ToVector4());
            smearEffect.Parameters["flecksHorizontalFade"].SetValue(new Color(30, 170, 255).ToVector4());

            float intensity = 0;

            if (State == AIState.GrabLight)
                intensity = MathHelper.Clamp(Projectile.velocity.Length() / 10f, 0f, 1f);
            if (State == AIState.Retracting || State == AIState.PostGrabStun)
                intensity = MathHelper.Clamp(retractTrailIntensityCountDown / 90f, 0, 1);

            if (intensity <= 0)
                return;

            smearEffect.Parameters["intensity"].SetValue(intensity);
            swingSmearTrailDrawer?.Render(smearEffect, -Main.screenPosition);
        }
    }

    public class OvergrownClawSlamHitbox : ModProjectile, IDrawOverTileMask
    {
        public override string Texture => AssetDirectory.Noise + "RadialMudCrackConcentric";


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seismic Shock");
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 40;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 50;
            Projectile.hide = true;
            Projectile.CritChance = 0;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (FablesUtils.AABBvCircle(targetHitbox, Projectile.Center, SLAM_AOE_RADIUS))
                return true;
            return false;
        }

        //We don't want the aoe to double hit the slammed npc lol
        public override bool? CanHitNPC(NPC target) => target.whoAmI != (int)Projectile.ai[0] ? null : false;

        public override bool? CanDamage() => Projectile.timeLeft > 38;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DisableCrit();

        public override void AI()
        {

        }

        public bool MaskDrawActive => true;
        public bool UsesNonsolidMask => false;

        public void DrawOverMask(SpriteBatch spriteBatch, bool solidLayer)
        {
            Texture2D cracks = TextureAssets.Projectile[Type].Value;
            //Color crackColor = new Color(30, 40, 255, 0);
            //crackColor *= MathF.Pow(Projectile.timeLeft / 40f, 2f);
            //spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, crackColor, 0, tex.Size() / 2f, 1.5f, 0, 0);

            float animationTimer = 1 - Projectile.timeLeft / 40f;

            Texture2D woosh = ModContent.Request<Texture2D>(AssetDirectory.Noise + "ChromaBurst").Value;
            float wooshProgress = (float)Math.Pow(Math.Min(animationTimer / 0.2f, 1f), 0.5f);
            Color wooshColor = new Color(30, 40, 255, 0) * (1 - wooshProgress);
            spriteBatch.Draw(woosh, Projectile.Center - Main.screenPosition, null, wooshColor, 0, woosh.Size() / 2f, 0.4f * wooshProgress, 0, 0);

            Color crackColor = new Color(30, 40, 255, 0);
            crackColor *= (float)Math.Pow(1 - animationTimer, 3f);
            for (int i = 0; i < 3; i++)
                spriteBatch.Draw(cracks, Projectile.Center - Main.screenPosition, null, crackColor, 0, cracks.Size() / 2f, 1.5f, 0, 0);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
             if (target.velocity.Y == 0)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    new KnockbackNPCPacket(target.whoAmI, new Vector2(0f, -9f * target.knockBackResist)).Send(-1, -1, false);
                else
                    target.velocity.Y -= 9f * target.knockBackResist;
            }
        }
    }
}
