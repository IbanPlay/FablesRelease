using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Localization;
using static CalamityFables.Helpers.FablesUtils;
using static CalamityFables.Content.Items.DesertScourgeDrops.CarrionDetonatorStaff;

namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    [ReplacingCalamity("StormSpray")]
    public class CarrionDetonatorStaff : ModItem
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        #region Sounds
        public static readonly SoundStyle UseSound = new(SoundDirectory.DesertScourgeDrops + "CarrionDetonatorFire");
        public static readonly SoundStyle SliceSound = new(SoundDirectory.DesertScourgeDrops + "CarrionDetonatorSlice", 3) { PitchVariance = 0.05f };
        public static readonly SoundStyle BlastSound = new(SoundDirectory.DesertScourgeDrops + "CarrionDetonatorBlast") { };
        public static readonly SoundStyle SmallBlastSound = new(SoundDirectory.DesertScourgeDrops + "CarrionDetonatorSmallBlast");
        public static readonly SoundStyle BigBlastSound = new(SoundDirectory.DesertScourgeDrops + "CarrionDetonatorLargeBlast");

        #endregion

        #region Reflection fields
        public static float WIND_CUT_PROBABILITY = 0.5f;
        public static int WIND_CUT_MAX_MEATCHUNKS = 3;
        public static float CRIT_WIND_CUT_PROBABILITY = 1f;
        public static int CRIT_WIND_CUT_MAX_MEATCHUNKS = 3;

        public static float MEATCHUNK_LIFETIME = 10;
        public static float MEATCHUNK_SHRINK_TIME = 0.5f;
        public static int MEATCHUNKS_MAX_PER_PLAYER = 25;
        public static float MEATCHUNK_MAX_DISTANCE_TO_PLAYER = 16f * 50f;

        public static float MEATCHUNK_SIZE_GAINED_PER_MERGE = 0.2f;
        public static float MEATCHUNK_ATTRACTION_DISTANCE = 400f;
        public static float MEATCHUNK_ATTRACTION_SPEED = 0.2f;
        public static float MEATCHUNK_MAX_SIZE = 3f;

        public static float MEATBLAST_MIN_RADIUS = 80f;
        public static float MEATBLAST_MAX_RADIUS = 200f;

        public static float MEATBLAST_MIN_DAMAGE_MULT = 0.5f;
        public static float MEATBLAST_MAX_DAMAGE_MULT = 1f;

        #endregion

        internal static int MeatChunkID;
        internal static int MeatBlastID;

        public override void SetStaticDefaults()
        {
            DetonationDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.DetonationDamage");
            Item.staff[Type] = true;
        }

        public static LocalizedText DetonationDamageText;

        public override void SetDefaults()
        {
            Item.damage = 16;
            Item.crit = 6;
            Item.width = 36;
            Item.height = 48;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.DamageType = DamageClass.Magic;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0.2f;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Green;
            Item.shoot = ModContent.ProjectileType<CarrionDetonatorWindWave>();
            Item.autoReuse = true;
            Item.shootSpeed = 18f;
            Item.mana = 8;
        }

        public override void HoldItem(Player player) => player.SyncMousePosition();

        public override bool AltFunctionUse(Player player) => true;

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            if (player.altFunctionUse == 2)
                mult *= 0;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            // Slower use speed on alt use
            if (player.altFunctionUse == 2)
                return 0.4f;

            return 1f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                SoundEngine.PlaySound(SoundID.Item40 with { MaxInstances = 0, Pitch = 0.4f });

                bool spawnedBlast = false;
                foreach (var projectile in Main.ActiveProjectiles)
                {
                    if (projectile.owner == player.whoAmI && projectile.type == MeatChunkID)
                    {
                        float meatScale = Utils.GetLerpValue(1f, MEATCHUNK_MAX_SIZE, projectile.scale, true);
                        float explosionRadius = MathHelper.Lerp(MEATBLAST_MIN_RADIUS, MEATBLAST_MAX_RADIUS, meatScale);
                        int explosionDamage = GetMeatblastDamage(damage, meatScale);
                        float explosionKnockback = Math.Max(0, (meatScale - 0.4f) * 20f * knockback);

                        Projectile.NewProjectile(source, projectile.Center, Vector2.Zero, MeatBlastID, explosionDamage, explosionKnockback, Main.myPlayer, explosionRadius);
                        spawnedBlast = true;
                        projectile.Kill();
                    }
                }

                if (spawnedBlast && CameraManager.Quake < 50)
                    CameraManager.Quake += 12;

                return false;
            }

            return true;
        }

        public static int GetMeatblastDamage(int damage, float scale)
        {
            float baseMult = 1f + scale * (MEATCHUNK_MAX_SIZE - 1) / MEATCHUNK_SIZE_GAINED_PER_MERGE;
            float scalingMult = MathHelper.Lerp(MEATBLAST_MIN_DAMAGE_MULT, MEATBLAST_MAX_DAMAGE_MULT, scale);

            return (int)(damage * baseMult * scalingMult);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");

            if (damageIndex == -1)
                return;

            int baseDamage = Main.LocalPlayer.GetWeaponDamage(Item, true);
            TooltipLine fieldDot = new TooltipLine(Mod, "CalamityFables:BlastDamage", DetonationDamageText.Format(GetMeatblastDamage(baseDamage, 0f), GetMeatblastDamage(baseDamage, 1f)))
            {
                OverrideColor = Color.Lerp(Color.White, Color.Coral, MathF.Pow(0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2f), 4f))
            };
            tooltips.Insert(damageIndex + 1, fieldDot);
        }
    }

    public class CarrionDetonatorWindWave : ModProjectile
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        internal List<Vector2> cache;

        public Player Owner => Main.player[Projectile.owner];
        public ref float ScaleUp => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wind Slice");

            if (Main.dedServ)
                return;

            //Register gores as safe
            for (int i = 1; i < 4; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("DSStaff_Gore" + i.ToString()).Type] = true;

            for (int i = 1; i < 6; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("GroundBeefGore" + i.ToString()).Type] = true;
            
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 120;
            Projectile.scale = 1f;
        }

        public override bool? CanDamage() => Projectile.penetrate > -1; //When its got -1 penetrate it means it's already hit an enemy

        public bool playedSound;

        public override void AI()
        {
            if (!playedSound)
            {
                SoundEngine.PlaySound(UseSound with { MaxInstances = 0, PitchVariance = 0.4f }, Projectile.Center);
                playedSound = true;
            }
            
            float projectileVelocityAngle = Projectile.velocity.ToRotation();
            //if (Projectile.velocity != Vector2.Zero)
            //    Projectile.rotation = projectileVelocityAngle + MathHelper.PiOver2; <- Old code for the wind wave type look

            Projectile.velocity *= 0.97f;
            if (Projectile.penetrate == -2)
                Projectile.velocity *= 0.97f;

            float velocityLenght = Projectile.velocity.Length();
            float redirectionPower = Utils.GetLerpValue(16f, 7f, velocityLenght, true);
            if (redirectionPower > 0)
            {
                float pointDirection = Owner.MountedCenter.AngleTo(Owner.MouseWorld());
                float newVelocityAngle = projectileVelocityAngle.AngleTowards(pointDirection, 0.03f * redirectionPower);

                Projectile.velocity = newVelocityAngle.ToRotationVector2() * velocityLenght;
            }
            if (velocityLenght < 3f)
                Projectile.timeLeft--;
            Projectile.rotation -= 0.2f * (0.6f + 0.4f * Utils.GetLerpValue(0f, 5f, velocityLenght, true));
            if (Projectile.penetrate > -1)
            {
                Projectile.scale = 1.1f + (float)Math.Pow(redirectionPower * 0.5f, 2f);
                ScaleUp = 1f + Utils.GetLerpValue(12f, 6f, velocityLenght, true) * 0.7f;
            }
            else
                Projectile.scale *= 0.99f;

            //Straight dust
            if (Main.rand.NextBool(3))
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f) + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(-35f, 35f) * Projectile.scale * ScaleUp;
                int dustType = Main.rand.NextBool(5) ? DustID.Sandnado : DustID.SandstormInABottle;
                Dust d = Dust.NewDustPerfect(dustPos, dustType, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4 * 0.2f) * -3f);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.8f, 1f);
                d.velocity *= 2.4f;
            }

            //Centrifugal dust
            if (Main.rand.NextBool(4))
            {
                float dustAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 dustPosition = Projectile.Center + dustAngle.ToRotationVector2() * Main.rand.NextFloat(30f, 40f) * Projectile.scale * ScaleUp;
                Vector2 dustVelocity = (dustAngle + MathHelper.PiOver2).ToRotationVector2() * -Main.rand.NextFloat(5f, 9f);

                int dustType = Main.rand.NextBool(5) ? DustID.Sandnado : DustID.SandstormInABottle;
                Dust d = Dust.NewDustPerfect(dustPosition, dustType, dustVelocity);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.8f, 1.3f);
            }

            ManageCache();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => AABBvCircle(targetHitbox, Projectile.Center, ScaleUp * Projectile.scale * 30f) && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1);

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity * 0.85f; //Slows down on tile hits
            Projectile.penetrate = -2;
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.velocity *= 0.6f;
            Projectile.penetrate = -1; //Setting penetrate to -1 makes it not hit any other enemies
            Projectile.netUpdate = true;

            bool crit = hit.Crit;

            if (Main.rand.NextFloat() > (crit ? CRIT_WIND_CUT_PROBABILITY : WIND_CUT_PROBABILITY))
                return;

            int meatChunkCount = Main.rand.Next((crit ? CRIT_WIND_CUT_MAX_MEATCHUNKS : WIND_CUT_MAX_MEATCHUNKS) + 1);
            float slice = MathHelper.TwoPi + Projectile.velocity.ToRotation();

            for (int i = 0; i < meatChunkCount; i++)
            {
                if (Owner.ownedProjectileCounts[MeatChunkID] + 1 + i > MEATCHUNKS_MAX_PER_PLAYER)
                    break;

                Vector2 meatChunkPos = Projectile.Center.MoveTowards(target.Center, 30f);
                Vector2 meatChunkSpeed = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 5f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), meatChunkPos, meatChunkSpeed, MeatChunkID, 20, 0, Main.myPlayer, slice);
                slice = 0;
            }
        }

        #region Drawing stuff (I'm stuff)
        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();
                for (int i = 0; i < 40; i++)
                {
                    cache.Add(Projectile.Center + Projectile.velocity);
                }
            }

            cache.Add(Projectile.Center + Projectile.velocity);

            while (cache.Count > 40)
            {
                cache.RemoveAt(0);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new Vector2(tex.Width / 2, tex.Height);
            Color sliceColor = Color.SandyBrown with { A = 0 };
            float opacityMult = Math.Clamp(Projectile.timeLeft / 30f, 0f, 1f) * 0.5f;

            cache.Reverse();
            List<float> arcLenghtParametrization = cache.ArcLenghtParametrize(out float trailLenght);
            int lowerBoundIndex = 0;
            int higherBoundIndex = 1;
            float distanceBetweenPoints = 8 * Projectile.scale;
            int trailCounts = 40;

            for (int i = 1; i < trailCounts; i++)
            {
                float distanceAlongTrail = distanceBetweenPoints * i;

                while (higherBoundIndex < arcLenghtParametrization.Count && arcLenghtParametrization[higherBoundIndex] < distanceAlongTrail)
                {
                    higherBoundIndex++;
                    lowerBoundIndex++;
                }
                if (higherBoundIndex == arcLenghtParametrization.Count)
                    break;

                float interpolant = Utils.GetLerpValue(arcLenghtParametrization[lowerBoundIndex], arcLenghtParametrization[higherBoundIndex], distanceAlongTrail, true);
                Vector2 drawPosition = Vector2.Lerp(cache[lowerBoundIndex], cache[higherBoundIndex], interpolant);

                float progress = 1 - distanceAlongTrail / Math.Min(trailLenght, trailCounts * distanceBetweenPoints);
                float opacity = (float)Math.Pow(progress, 5f);
                Color color = Color.Tan with { A = 100 };
                float rotationOffset = -progress * 14.5f; //This is where the magic happens
                Main.EntitySpriteDraw(tex, drawPosition - Main.screenPosition, null, color * opacity * opacityMult, Projectile.rotation + rotationOffset, origin, ScaleUp * Projectile.scale, 0, 0);
            }
            cache.Reverse();

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, sliceColor * opacityMult, Projectile.rotation, origin, ScaleUp * Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, sliceColor * opacityMult * 0.1f, Projectile.rotation, origin, ScaleUp * Projectile.scale * 1.1f, 0, 0);

            if (Projectile.penetrate > -1)
                DrawLensFlare(Projectile.Center + (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 35f * Projectile.scale);
            return false;
        }

        public void DrawLensFlare(Vector2 position)
        {
            position -= Main.screenPosition;

            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            Color alphaPurple = MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.8f + Projectile.whoAmI * 0.1f) % 1, Color.Tan, Color.Goldenrod, Color.SandyBrown) with { A = 0 };

            alphaPurple *= Main.rand.NextFloat(0.8f, 3f);

            //Draws 2 layers of circular bloom on the tip of the slice
            Main.EntitySpriteDraw(bloom, position, null, alphaPurple * 0.2f, 0, bloom.Size() / 2f, Projectile.scale * 0.45f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, position, null, alphaPurple * 0.6f, 0, bloom.Size() / 2f, Projectile.scale * 0.15f, SpriteEffects.None, 0);

            //Draws 2 layer of lens flare ontop of the slice, scaling with velocity
            Vector2 squishy = new Vector2(0.46f, 1.8f);
            Main.EntitySpriteDraw(lensFlare, position, null, alphaPurple * 0.3f, MathHelper.PiOver2, lensFlare.Size() / 2f, squishy * Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(lensFlare, position, null, alphaPurple * 0.4f, MathHelper.PiOver2, lensFlare.Size() / 2f, squishy * Projectile.scale * 0.5f, SpriteEffects.None, 0);
        }
        #endregion
    }

    public class CarrionDetonatorMeatChunk : ModProjectile
    {
        internal List<Projectile> gravitationPoints;
        internal float variant;

        public override string Texture => AssetDirectory.DesertScourgeDrops + "CarrionMeat";

        public override void SetStaticDefaults() => MeatChunkID = Type;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = ProjAIStyleID.MoveShort;
            Projectile.timeLeft = (int)(60 * MEATCHUNK_LIFETIME);
            variant = Main.rand.NextFloat();
        }

        public override bool? CanDamage() => false;
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.X != Projectile.velocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (oldVelocity.Y != Projectile.velocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;
            Projectile.velocity *= 0.7f;
            return false;
        }

        public override bool PreAI()
        {
            //Apparition efffects
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                SoundEngine.PlaySound(SliceSound with { Volume = 0.5f }, Projectile.Center);

                if (Projectile.ai[0] != 0 && !Main.dedServ)
                {
                    Particle flasj = new HitStreakParticle(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextFloat(MathHelper.TwoPi), Color.White with { A = 100 }, Color.Tomato with { A = 100 } * 0.4f, (Color.Tomato * 0.2f) with { A = 0 }, Main.rand.NextFloat(0.2f, 0.7f), 10);
                    ParticleHandler.SpawnParticle(flasj);

                    int goreCount = Main.rand.Next(1, 4);
                    for (int i = 0; i < goreCount; i++)
                    {
                        int goreType = Mod.Find<ModGore>("DSStaff_Gore" + Main.rand.Next(1, 4).ToString()).Type;
                        Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Projectile.velocity.RotatedByRandom(MathHelper.PiOver2 * 0.6f) * 1.4f, goreType);
                        gore.timeLeft = 5;
                        gore.sticky = true;
                    }


                    for (int i = 0; i < 26; i++)
                    {
                        Vector2 speed = (Projectile.ai[0] + Main.rand.NextFloat(-0.2f, 0.2f)).ToRotationVector2() * Main.rand.NextFloat(1f, 7f);
                        int dustType = Main.rand.NextBool() ? DustID.Blood : Main.rand.NextBool() ? DustID.RedStarfish : DustID.SomethingRed;
                        Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, speed);
                        d.scale = Main.rand.NextFloat(1f, 1.3f);
                    }

                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 speed = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f);
                        int dustType = Main.rand.NextBool() ? DustID.Blood : DustID.RedStarfish;
                        Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, speed);
                        d.scale = Main.rand.NextFloat(1f, 1.3f);
                    }
                }
            }

            Projectile.velocity *= 0.97f;
            Projectile.rotation += Projectile.velocity.X * 0.06f;
            //Keep rotating slowly
            if (Math.Abs(Projectile.velocity.X) < 0.1f)
                Projectile.rotation += Math.Sign(Projectile.rotation) * 0.01f;

            if (Projectile.timeLeft > 60 && Projectile.DistanceSQ(Main.player[Projectile.owner].Center) > MEATCHUNK_MAX_DISTANCE_TO_PLAYER * MEATCHUNK_MAX_DISTANCE_TO_PLAYER)
                Projectile.timeLeft = 60;

            if (Projectile.timeLeft < MEATCHUNK_SHRINK_TIME * 60)
                Projectile.scale *= 0.98f;

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            //Ring of small dust
            for (int i = 0; i < 8; i++)
            {
                Vector2 dustOffset = Vector2.UnitY.RotatedBy(i / 8f * MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center + dustOffset * 10f, DustID.Blood, dustOffset * Main.rand.NextFloat(0.2f, 0.7f), 100, default(Color), 1f);
                d.scale = Main.rand.NextFloat(1f, 2f);
            }

            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f) * 10f, DustID.Blood, Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), 100, default(Color), 1f);
            }

            if (timeLeft > 0 || Main.dedServ)
                return;

            int goreCount = Main.rand.Next(0, 4);
            for (int i = 0; i < goreCount; i++)
            {
                int goreType = Mod.Find<ModGore>("GroundBeefGore" + Main.rand.Next(1, 6).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f) * Projectile.scale, Vector2.UnitY, goreType);
                gore.timeLeft = 4;
                gore.alpha = 20;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (gravitationPoints != null && gravitationPoints.Count > 0 && Projectile.scale < MEATCHUNK_MAX_SIZE)
            {
                Texture2D connection = AssetDirectory.CommonTextures.BloomStreak.Value;
                Rectangle crop = new(0, 15, connection.Width, connection.Height / 2 - 15);
                Vector2 origin = new(connection.Width / 2, crop.Height);

                foreach (Projectile otherConnection in gravitationPoints)
                {
                    float distance = otherConnection.Distance(Projectile.Center);
                    float opacity = (float)Math.Pow(1f - distance / MEATCHUNK_ATTRACTION_DISTANCE, 3f) * 0.3f + 0.3f;
                    Vector2 stretch = new Vector2(0.6f, distance / (float)crop.Height * 0.5f);

                    Main.EntitySpriteDraw(connection, Projectile.Center - Main.screenPosition, crop, Color.IndianRed with { A = 100 } * opacity, Projectile.AngleTo(otherConnection.Center) + MathHelper.PiOver2, origin, stretch, 0, 0);
                }
            }



            Vector2 scale = new Vector2(1f - (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.06f, 1f);
            scale.Y = 2 - scale.X;

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame;
            float scaledWidth = Projectile.scale * tex.Width;

            if (scaledWidth >= 46)
            {
                tex = ModContent.Request<Texture2D>(Texture + "_Big").Value;
                frame = new(0, 0, tex.Width, tex.Height);
            }
            else if (scaledWidth >= 34)
            {
                tex = ModContent.Request<Texture2D>(Texture + "_Medium").Value;
                frame = new(0, (tex.Height / 3) * (int)Math.Floor(variant * 3), tex.Width, tex.Height / 3 - 2);
            }
            else if (scaledWidth >= 28)
            {
                tex = ModContent.Request<Texture2D>(Texture + "_Small").Value;
                frame = new(0, (tex.Height / 3) * (int)Math.Floor(variant * 3), tex.Width, tex.Height / 3 - 2);
            }
            else
                frame = new(0, (tex.Height / 4) * (int)Math.Floor(variant * 4), tex.Width, tex.Height / 4 - 2);

            float drawScale = scaledWidth / (float)tex.Width;

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2f, scale * drawScale, 0, 0);
            return false;
        }
    }

    public class CarrionDetonatorMeatBlast : ModProjectile, IDrawPixelated
    {
        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;

        public override string Texture => AssetDirectory.Invisible;

        internal PrimitiveClosedLoop BlastLoop;
        internal bool darkMode = false;
        public ref float Radius => ref Projectile.ai[0];
        public int Lifetime = 20;
        public float Completion => Projectile.timeLeft / (float)Lifetime;

        public float HowBig => Utils.GetLerpValue(MEATBLAST_MIN_RADIUS, MEATBLAST_MAX_RADIUS, Radius, true);

        public override void SetStaticDefaults() => MeatBlastID = Type;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }

        public override bool? CanDamage() => Projectile.timeLeft > Lifetime - 10;

        public override void AI()
        {

            Projectile.velocity = Vector2.Zero;

            if (!Main.dedServ)
            {
                if (Projectile.localAI[0] == 0)
                {
                    Projectile.localAI[0] = 1;


                    SoundStyle pickedSound = HowBig < 0.33f ? SmallBlastSound : HowBig > 0.8f ? BigBlastSound : BlastSound;
                    SoundEngine.PlaySound(pickedSound with { MaxInstances = 1, Identifier = "CarrionBlast", SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, Projectile.Center);


                    //Small blood dust
                    for (int i = 0; i < 40 + HowBig * 40f; i++)
                    {
                        Vector2 dustOffset = Vector2.UnitY.RotatedByRandom(MathHelper.Pi);
                        Vector2 dustPosition = Projectile.Center + dustOffset * Main.rand.NextFloat(0.4f, 0.9f) * Radius;

                        Dust d = Dust.NewDustPerfect(dustPosition, DustID.Blood, dustOffset * Main.rand.NextFloat(0.2f, 2.7f) * (1 + HowBig * 2f), 100, default(Color), 1f);
                        d.scale = Main.rand.NextFloat(1f, 2f);
                    }

                    for (int i = 0; i < 40 + HowBig * 40f; i++)
                    {
                        int dustType = Main.rand.NextBool(3) ? DustID.RedStarfish : DustID.PinkStarfish;

                        Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(10f, 10f) * (1 + HowBig), 0, default(Color), 1f);
                        d.scale = Main.rand.NextFloat(0.4f, 1.3f);
                        d.noGravity = false;
                        d.alpha = 0;
                    }

                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 smokePosition = Projectile.Center + Main.rand.NextVector2Circular(60f, 60f) * (1f + HowBig);
                        Vector2 smokeVelocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 2f) + Main.rand.NextVector2Circular(2.6f, 2.6f);
                        Color smokeFireColor = Main.rand.NextBool(3) ? Color.Crimson : Color.OrangeRed;
                        Particle fleshSmoke = new ExplosionSmoke(smokePosition, smokeVelocity, smokeFireColor, Color.DarkRed * 0.3f, Color.Coral * 0.3f, Main.rand.NextFloat(1.4f, 3f), 0.03f);
                        ParticleHandler.SpawnParticle(fleshSmoke);
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 direction = Main.rand.NextVector2Circular(5f, 5f);
                        Particle streak = new BlastStreak(Projectile.Center, direction, 5f, Color.PeachPuff, Color.Tomato, Color.Tomato * 0.3f, 0.4f + HowBig, 12, 3f);
                        ParticleHandler.SpawnParticle(streak);
                    }


                    for (int i = 0; i < 6 + HowBig * 10; i++)
                    {
                        int goreType = Mod.Find<ModGore>("CarrionBlastMeatChunk" + Main.rand.Next(1, 6).ToString()).Type;
                        Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2Circular(6f + 14f * HowBig, 6f + 14f * HowBig), goreType);
                        gore.timeLeft = 35;
                    }
                }

                BlastLoop = BlastLoop ?? new PrimitiveClosedLoop(50, BlastLoopWidth, BlastLoopColor);
                BlastLoop.SetPositionsCircle(Projectile.Center, (1f - 0.9f * (float)Math.Pow(Completion, 4f)) * Radius);
            }
        }

        internal float BlastLoopWidth(float completionRatio)
        {
            float baseWidth = (15f + HowBig * 7f) * (float)Math.Pow(Completion, 0.7f);  //Width tapers off at the end
            baseWidth *= (1 + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7) * 0.3f); //Width oscillates
            return baseWidth;
        }

        internal Color BlastLoopColor(float completionRatio)
        {
            Color color = Color.Lerp(Color.Pink, Color.Firebrick, 1 - Completion);
            if (Completion > 0.9f)
                color = Color.White;
            if (darkMode)
                color = Color.Crimson with { A = 200 } * 0.4f * Completion;

            return color;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => AABBvCircle(targetHitbox, Projectile.Center, Radius);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Projectile.damage = (int)(Projectile.damage * 0.8f);

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = Scene["GlorpBlast"].GetShader().Shader;
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 4.2f + Projectile.whoAmI * 0.3f);
            effect.Parameters["repeats"].SetValue(5f);
            effect.Parameters["voronoi"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "Voronoi").Value);
            effect.Parameters["noiseOverlay"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "PrettyManifoldNoise").Value);

            darkMode = true;
            for (int i = 0; i < 4; i++)
            {
                BlastLoop?.Render(effect, -Main.screenPosition + (i / 4f * MathHelper.TwoPi).ToRotationVector2() * 2f);
            }
            darkMode = false;
            BlastLoop?.Render(effect, -Main.screenPosition);
        }
    }


    public class CarrionGravitySystem : ModSystem
    {
        public override void PreUpdateProjectiles()
        {
            List<Projectile> meatChunks = new();
            int meatType = ModContent.ProjectileType<CarrionDetonatorMeatChunk>();
            int maxLifetime = (int)(60f * MEATCHUNK_LIFETIME);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != meatType || p.scale >= MEATCHUNK_MAX_SIZE || (p.scale == 1 && p.timeLeft > maxLifetime - 30))
                    continue;

                meatChunks.Add(p);
            }

            float maxAttractionDistance = MEATCHUNK_ATTRACTION_DISTANCE;
            float fuseDistance = 16f;

            maxAttractionDistance *= maxAttractionDistance; //Squared
            fuseDistance *= fuseDistance;

            foreach (Projectile meat in meatChunks)
            {
                CarrionDetonatorMeatChunk modProj = meat.ModProjectile as CarrionDetonatorMeatChunk;
                if (modProj.gravitationPoints == null)
                    modProj.gravitationPoints = new List<Projectile>();
                modProj.gravitationPoints.Clear();

                foreach (Projectile otherMeat in meatChunks)
                {
                    if (otherMeat == meat)
                        continue;
                    if (otherMeat.owner != meat.owner)
                        continue;
                    if (!meat.active || !otherMeat.active)
                        continue;

                    float distance = meat.DistanceSQ(otherMeat.Center);

                    if (distance < fuseDistance)
                    {
                        meat.velocity += otherMeat.velocity;
                        meat.velocity *= 0.5f;
                        meat.rotation = Main.rand.NextFloat(MathHelper.TwoPi) * meat.rotation.NonZeroSign();
                        meat.scale = meat.scale + (otherMeat.scale - 1) + MEATCHUNK_SIZE_GAINED_PER_MERGE; //Merge the 2 sizes with an added extra
                        meat.scale = Math.Min(meat.scale, MEATCHUNK_MAX_SIZE);
                        meat.timeLeft = maxLifetime;
                        modProj.variant = Main.rand.NextFloat();

                        SoundEngine.PlaySound(SoundID.Item167 with { Pitch = 0.4f }, meat.Center);

                        otherMeat.ai[0] = -1;
                        otherMeat.Kill();
                    }

                    else if (distance < maxAttractionDistance)
                    {
                        modProj.gravitationPoints.Add(otherMeat);

                        meat.velocity += meat.DirectionTo(otherMeat.Center) * (1 - distance / maxAttractionDistance) * MEATCHUNK_ATTRACTION_SPEED;
                    }
                }
            }
        }
    }

    public abstract class CarrionBlastMeatChunk : ModGore
    {
        public override string Texture => AssetDirectory.Assets + "Gores/GroundBeefGore" + Number.ToString();
        public abstract int Number { get; }

        public override void SetStaticDefaults()
        {
            GoreID.Sets.DrawBehind[Type] = true;
            ChildSafety.SafeGore[Type] = true;
        }

        public override void OnSpawn(Gore gore, IEntitySource source)
        {
            gore.behindTiles = true;
        }

        public override bool Update(Gore gore)
        {
            gore.velocity *= 0.96f;
            gore.velocity.Y += 0.04f;

            gore.timeLeft--;
            gore.position += gore.velocity;
            gore.rotation += gore.velocity.X * 0.05f;

            if (gore.timeLeft < 10)
            {
                gore.alpha = (int)(255 * (1 - gore.timeLeft / 10f));
                gore.scale *= 0.95f;
            }

            if (gore.timeLeft < 0)
                gore.active = false;

            return false;
        }
    }

    public class CarrionBlastMeatChunk1 : CarrionBlastMeatChunk
    {
        public override int Number => 1;
    }

    public class CarrionBlastMeatChunk2 : CarrionBlastMeatChunk
    {
        public override int Number => 2;
    }
    public class CarrionBlastMeatChunk3 : CarrionBlastMeatChunk
    {
        public override int Number => 3;
    }
    public class CarrionBlastMeatChunk4 : CarrionBlastMeatChunk
    {
        public override int Number => 4;
    }
    public class CarrionBlastMeatChunk5 : CarrionBlastMeatChunk
    {
        public override int Number => 5;
    }
}
