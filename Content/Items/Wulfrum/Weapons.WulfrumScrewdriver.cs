using Terraria.DataStructures;
using static CalamityFables.Helpers.FablesUtils;
using CalamityFables.Content.Items.EarlyGameMisc;
using Terraria.Enums;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumScrewdriver")]
    public class WulfrumScrewdriver : ModItem
    {
        public static int DefaultTime = 10;
        public static readonly SoundStyle ThrustSound = new(SoundDirectory.Wulfrum + "WulfrumScrewdriverThrust") { PitchVariance = 0.4f };
        public static readonly SoundStyle ThudSound = new(SoundDirectory.Wulfrum + "WulfrumScrewdriverThud") { PitchVariance = 0.2f, Volume = 0.7f };
        public static readonly SoundStyle ScrewGetSound = new(SoundDirectory.Wulfrum + "WulfrumScrewdriverScrewGet") { PitchVariance = 0.1f };
        public static readonly SoundStyle ScrewHitSound = new(SoundDirectory.Wulfrum + "WulfrumScrewdriverScrewHit") { Volume = 0.7f };
        public static readonly SoundStyle FunnyUltrablingSound = new("CalamityFables/Sounds/UltrablingHit");

        public static bool ScrewQeuedForStorage = false;
        public bool ScrewStored = false;
        public bool ScrewAvailable => ScrewStored && ScrewTimer == 0;
        public static Vector3 ScrewStart = new Vector3(0);
        public static Vector3 ScrewPosition;
        public static Vector2 PrevOffset;
        public static float ScrewTimer;
        public static float ScrewTime = 40;
        public static Asset<Texture2D> ScrewTex;
        public static Asset<Texture2D> ScrewOutlineTex;
        public static float ScrewBaseDamageMult = 1.4f;
        public static float ScrewBazingaModeDamageMult = 6.5f;
        public static float ScrewBazingaAimAssistAngle = 0.52f; //This may look high but remember this is the FULL angle, so it actually checks for half that angle deviation
        public static float ScrewBazingaAimAssistReach = 600f;

        public override ModItem Clone(Item item)
        {
            return base.Clone(item); /*

            ModItem clone = base.Clone(item);
            if (clone is WulfrumScrewdriver a && item.ModItem is WulfrumScrewdriver a2 && a2.ScrewStored)
            {
                a.ScrewStored = a2.ScrewStored;
            }
            return clone;*/
        }

        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Screwdriver");
            Tooltip.SetDefault("Can be used to rapidly and royally screw over any foe, provided they're weak enough\n" +
                               "Striking an enemy has a chance to give you a wulfrum screw, that you can throw back at them by right clicking\n" +
                               "Hitting a flung screw with the screwdriver will boost its damage and speed, sending it hurling forwards at high speeds\n" +
                               "[c/83B87E:\"Who makes flatheads this large?? The hell am I supposed to use it for, the thing could take an eye out!\"]\n" +
                               "[c/83B87E:\"…Ah.\"]");
            Item.ResearchUnlockCount = 1;

            if (Main.dedServ)
                return;
            ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumScrewGore").Type] = true;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            //Super speedy
            if (player.altFunctionUse == 2)
                return 2f;

            return base.UseSpeedMultiplier(player);
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 50;
            Item.damage = 7;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = DefaultTime + WulfrumScrewdriverProj.MaxTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = DefaultTime + WulfrumScrewdriverProj.MaxTime;
            Item.useTurn = true;
            Item.knockBack = 3.75f;
            Item.UseSound = ThrustSound;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<WulfrumScrewdriverProj>();
            Item.shootSpeed = 1f;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>(12).
                AddIngredient<EnergyCore>().
                AddTile(TileID.Anvils).
                Register();
        }

        public override void Update(ref float gravity, ref float maxFallSpeed) => ScrewStored = false;
        public override void UpdateInventory(Player player)
        {
            if (player.HeldItem != Item)
                ScrewStored = false;
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();

            if (Main.myPlayer == player.whoAmI)
            {
                if (ScrewQeuedForStorage)
                {
                    ScrewStored = true;
                    ScrewQeuedForStorage = false;
                }

                if (ScrewTimer > 0)
                    ScrewTimer--;

                if (ScrewTimer == 1)
                {
                    Vector2 dustPos = new Vector2(ScrewPosition.X, ScrewPosition.Y) + Main.screenPosition;
                    int numDust = Main.rand.Next(5, 15);
                    for (int i = 0; i < numDust; i++)
                    {
                        Dust.NewDustPerfect(dustPos, Main.rand.NextBool() ? 246 : 247, Main.rand.NextVector2Circular(1f, 1f), Scale: Main.rand.NextFloat(0.9f, 1.4f));
                    }

                    SoundEngine.PlaySound(ScrewGetSound);
                }
            }

            base.HoldItem(player);
        }

        public override bool AltFunctionUse(Player player) => ScrewAvailable;
        public override bool CanUseItem(Player player) => player.altFunctionUse == 2 ? !Main.projectile.Any(n => n.active && n.owner == player.whoAmI && n.type == ModContent.ProjectileType<WulfrumScrew>()) : true;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.altFunctionUse == 2) damage = (int)(damage * ScrewBaseDamageMult);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Vector2 chuckSpeed = new Vector2(Math.Sign(velocity.X) * 0.4f, -1.24f) + player.velocity / 4f;

                //Prevent it from being thrown at any less than -1f
                chuckSpeed.Y = Math.Clamp(chuckSpeed.Y, -1f, 3f);

                //Opposite velocities, add more of the players own velocity to make it easier to aim
                if (velocity.X * player.velocity.X < 0)
                    chuckSpeed.X += player.velocity.X * 0.07f;

                int p = Projectile.NewProjectile(source, position, chuckSpeed, ModContent.ProjectileType<WulfrumScrew>(), damage, knockback, player.whoAmI);

                ScrewStored = false;
                return false;
            }

            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        public CurveSegment InitialAway = new CurveSegment(SineOutEasing, 0f, 0f, -0.2f, 3);
        public CurveSegment AccelerateTowards = new CurveSegment(PolyInEasing, 0.3f, -0.2f, 1.2f, 3);
        public CurveSegment Bump1Segment = new CurveSegment(SineBumpEasing, 0.5f, 1f, 0.24f);
        public CurveSegment Bump2Segment = new CurveSegment(SineBumpEasing, 0.8f, 1f, -0.1f);
        internal float ProgressionOfScrew => PiecewiseAnimation(ScrewTimer / ScrewTime, new CurveSegment[] { InitialAway, AccelerateTowards, Bump1Segment, Bump2Segment });

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (!ScrewStored)
                return;

            Player myPlayer = Main.LocalPlayer;

            if (myPlayer.HeldItem != Item || !myPlayer.active || myPlayer.dead)
                return;

            spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            if (ScrewTex == null)
                ScrewTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumItems + "WulfrumScrew");
            if (ScrewOutlineTex == null)
                ScrewOutlineTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumItems + "WulfrumScrewOutline");

            Texture2D screwTex = ScrewTex.Value;
            Texture2D screwOutlineTex = ScrewOutlineTex.Value;

            Vector2 realIdealSpot = myPlayer.MountedCenter + myPlayer.gfxOffY * Vector2.UnitY - Main.screenPosition - Vector2.UnitY * 50f * myPlayer.gravDir - Vector2.Lerp(myPlayer.velocity, PrevOffset, 0.5f);
            realIdealSpot.Y += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 5f;
            realIdealSpot.X += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1f) * 7.8f;

            ScrewPosition = new Vector3(realIdealSpot, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f) * MathHelper.PiOver4 * 0.34f + (myPlayer.gravDir == -1 ? MathHelper.Pi : 0));

            position = Vector2.Lerp(new Vector2(ScrewPosition.X, ScrewPosition.Y), new Vector2(ScrewStart.X, ScrewStart.Y), ProgressionOfScrew);
            float rotation = ScrewPosition.Z.AngleLerp(ScrewStart.Z, ProgressionOfScrew);
            float outlineOpacity = (float)Math.Pow(1 - ScrewTimer / ScrewTime, 2);
            scale = 1.05f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f);

            Main.spriteBatch.Draw(screwOutlineTex, position, null, Color.Lerp(Color.GreenYellow, Color.White, (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f + 0.5f) * outlineOpacity, rotation, screwOutlineTex.Size() / 2f, scale, 0, 0);
            Main.spriteBatch.Draw(screwTex, position, null, Color.White, rotation, screwTex.Size() / 2f, scale, 0, 0);

            spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);


            PrevOffset = myPlayer.velocity;
            base.PostDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
    }


    public class WulfrumScrewdriverProj : ModProjectile
    {
        public float Timer => MaxTime - Projectile.timeLeft;
        public float LifetimeCompletion => Timer / (float)MaxTime;

        public static int MaxTime = 14;
        public ref float EndLag => ref Projectile.ai[0];
        public ref float TrueDirection => ref Projectile.ai[1];
        public Player Owner => Main.player[Projectile.owner];

        public static Asset<Texture2D> SmearTex;
        public override string Texture => AssetDirectory.WulfrumItems + "WulfrumScrewdriver";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Screwdriver");
            ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 14;
            Projectile.height = 50;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.ownerHitCheck = true;
            Projectile.timeLeft = MaxTime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override bool? CanDamage()
        {
            return Projectile.timeLeft <= (MaxTime - (int)MathF.Ceiling(5f / Owner.GetTotalAttackSpeed(Projectile.DamageType)));
        }

        public override bool ShouldUpdatePosition() => false;
        public CurveSegment ThrustSegment = new CurveSegment(LinearEasing, 0f, 0f, 1f, 3);
        public CurveSegment HoldSegment = new CurveSegment(SineBumpEasing, 0.2f, 1f, 0.2f);
        public CurveSegment RetractSegment = new CurveSegment(PolyOutEasing, 0.76f, 1f, -0.8f, 3);
        public CurveSegment BumpSegment = new CurveSegment(SineBumpEasing, 0.9f, 0.2f, 0.15f);
        internal float DistanceFromPlayer => PiecewiseAnimation(LifetimeCompletion, new CurveSegment[] { ThrustSegment, HoldSegment, RetractSegment, BumpSegment });
        public Vector2 OffsetFromPlayer => Projectile.velocity * DistanceFromPlayer * 12f;


        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //The hitbox is simplified into a line collision.
            float collisionPoint = 0f;
            float bladeLenght = 78f * Projectile.scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Owner.MountedCenter + OffsetFromPlayer, Owner.MountedCenter + OffsetFromPlayer + (Projectile.velocity * bladeLenght), 24, ref collisionPoint);
        }

        public override void AI()
        {
            if (EndLag == 0) //Initialization
            {
                EndLag = (float)Math.Max(Owner.itemTime - MaxTime, 1);
                TrueDirection = (Owner.MouseWorld() - Owner.MountedCenter).SafeNormalize(Vector2.Zero).ToRotation(); //Store this for the screw hit
                Projectile.velocity = (Owner.MouseWorld() - Owner.MountedCenter).SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4 * 0.15f);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }

            //Manage position and rotation
            Projectile.Center = Owner.MountedCenter + OffsetFromPlayer;
            Projectile.scale = 1f + (float)Math.Sin(LifetimeCompletion * MathHelper.Pi) * 0.2f; //SWAGGER

            //Make the owner look like theyre holding the sword bla bla
            Owner.heldProj = Projectile.whoAmI;
            Owner.direction = Math.Sign(Projectile.velocity.X);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() * Owner.gravDir - MathHelper.PiOver2);
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;


            //Check for launchable screws.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.ModProjectile != null && proj.owner == Projectile.owner && proj.ModProjectile is WulfrumScrew screw && screw.BazingaTime == 0)
                {
                    float collisionPoint = 0f;
                    float bladeLenght = 86f * Projectile.scale;
                    if (Collision.CheckAABBvLineCollision(proj.Hitbox.TopLeft(), proj.Hitbox.Size(), Owner.Center + OffsetFromPlayer, Owner.Center + OffsetFromPlayer + (Projectile.velocity * bladeLenght), 34, ref collisionPoint))
                    {
                        Vector2 thudVelocity = TrueDirection.ToRotationVector2() * 6f;
                        NPC potentialAimAssist = FindTarget();
                        if (potentialAimAssist != null)
                            thudVelocity = (potentialAimAssist.Center - proj.Center).SafeNormalize(Vector2.Zero) * 6f;

                        screw.BazingaTime = WulfrumScrew.BazingaTimeMax;
                        proj.velocity = thudVelocity;
                        if (screw.AlreadyBazinged == 0)
                            proj.damage = (int)(proj.damage * WulfrumScrewdriver.ScrewBazingaModeDamageMult);
                        proj.timeLeft = WulfrumScrew.Lifetime;
                        proj.knockBack *= 2.5f;
                        screw.AlreadyBazinged++;

                        SoundEngine.PlaySound(WulfrumScrewdriver.ScrewHitSound, Projectile.Center);

                        if (screw.AlreadyBazinged > 2)
                            SoundEngine.PlaySound(WulfrumScrewdriver.FunnyUltrablingSound, Projectile.Center);


                        if (Main.myPlayer == proj.owner)
                        {
                            CameraManager.Shake = 6f;
                        }

                        return;
                    }
                }
            }
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
                float angle = TrueDirection.ToRotationVector2().AngleBetween((potentialTarget.Center - Projectile.Center));

                float extraDistance = potentialTarget.width / 2 + potentialTarget.height / 2;

                if (distance - extraDistance < WulfrumScrewdriver.ScrewBazingaAimAssistReach && angle < WulfrumScrewdriver.ScrewBazingaAimAssistAngle / 2f)
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
            float score = 1 - distance / WulfrumScrewdriver.ScrewBazingaAimAssistReach * 0.2f;
            //Prioritize angle over distance
            score += (1 - Math.Abs(angle) / (WulfrumScrewdriver.ScrewBazingaAimAssistAngle / 2f)) * 0.8f;

            return score;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(WulfrumScrewdriver.ThudSound, target.Center);
            Projectile.timeLeft = 0;

            //Chance to gain a screw
            if (Main.rand.NextBool(5) && Main.myPlayer == Owner.whoAmI && !target.CountsAsACritter)
            {
                if (Owner.HeldItem.ModItem is WulfrumScrewdriver screwdriver && !screwdriver.ScrewStored)
                {
                    WulfrumScrewdriver.ScrewStart = new Vector3(target.Center + Projectile.velocity * 14f * Main.rand.NextFloat() - Main.screenPosition, Main.rand.NextFloat(MathHelper.PiOver2 - MathHelper.PiOver4));
                    WulfrumScrewdriver.ScrewTimer = WulfrumScrewdriver.ScrewTime;
                    WulfrumScrewdriver.ScrewQeuedForStorage = true;

                    SoundEngine.PlaySound(SoundID.Item156);
                }
            }

            //Dust
            for (int k = 0; k < 4; k++)
            {
                Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 70f, 16, Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(6), 0, default, Main.rand.NextFloat(0.7f, 1f));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.numHits == 0)
            {
                Owner.itemTime = (int)EndLag;
                Owner.itemAnimation = (int)EndLag;
            }

            //Go into jojo spam mode if you hit an enemy
            else
            {
                Owner.itemTime = (int)(5f / Owner.GetTotalAttackSpeed(Projectile.DamageType));
                Owner.itemAnimation = (int)(5f / Owner.GetTotalAttackSpeed(Projectile.DamageType));
            }
        }

        public override void CutTiles()
        {
            // tilecut_0 is an unnamed decompiled variable which tells CutTiles how the tiles are being cut (in this case, via a Projectile).
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.TileActionAttempt cut = new Utils.TileActionAttempt(DelegateMethods.CutTiles);

            float bladeLength = 78f * Projectile.scale;
            Utils.PlotTileLine(Owner.MountedCenter + OffsetFromPlayer, Owner.MountedCenter + OffsetFromPlayer + (Projectile.velocity * bladeLength), 24, cut);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;

            if (SmearTex == null)
                SmearTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumItems + "WulfrumScrewdriver_Thrust");
            Texture2D smearTex = SmearTex.Value;

            Vector2 drawOrigin = new Vector2(tex.Width / 2f, tex.Height);
            Vector2 scale = new Vector2(Math.Abs((float)Math.Sin(LifetimeCompletion * MathHelper.TwoPi * 0.5f)), 1f);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, drawOrigin, scale * Projectile.scale, 0f, 0);

            if (LifetimeCompletion < 0.6f)
            {
                int frameCount = (int)Math.Floor((LifetimeCompletion / 0.6f) * 3f);
                Rectangle frame = new Rectangle(0, (smearTex.Height / 3) * frameCount, smearTex.Width, smearTex.Height / 3);
                float opacity = 1 - (float)Math.Pow(LifetimeCompletion / 0.6f, 0.5f);

                Main.spriteBatch.Draw(smearTex, Projectile.Center + Projectile.velocity * 67f - Main.screenPosition, frame, Color.White * opacity, Projectile.rotation, frame.Size() / 2f, 0.9f, 0, 0);
            }
            return false;
        }
    }


    public class WulfrumScrew : ModProjectile
    {
        internal PrimitiveTrail TrailDrawer;
        internal Color PrimColorMult = Color.White;


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Screw");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public static int Lifetime = 950;
        public float LifetimeCompletion => MathHelper.Clamp((Lifetime - Projectile.timeLeft) / (float)Lifetime, 0f, 1f);
        public ref float BazingaTime => ref Projectile.ai[0];
        public ref float AlreadyBazinged => ref Projectile.ai[1];
        public static float BazingaTimeMax = 120f;
        public float BazingaTimeCompletion => (BazingaTimeMax - BazingaTime) / BazingaTimeMax;
        public float FadePercent => Math.Clamp(Projectile.timeLeft / FadeTime, 0f, 1f);
        public static float FadeTime => 30f;
        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Melee;

            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.scale = 1.2f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 0.998f;

            if (Projectile.timeLeft < Lifetime - 100 && BazingaTime == 0)
                Projectile.velocity.Y += 0.01f;


            if (BazingaTime > 0)
            {
                BazingaTime--;

                if (BazingaTime == BazingaTimeMax - 2)
                    CameraManager.Shake = 0;

                Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                Dust chust = Dust.NewDustPerfect(dustCenter, 15, -Projectile.velocity * Main.rand.NextFloat(0.6f, 1.5f), Scale: Main.rand.NextFloat(1f, 1.4f));
                chust.noGravity = true;

                if (!Main.rand.NextBool(5))
                    chust.noLightEmittence = true;
            }

            if (Projectile.Center.Distance(Owner.MountedCenter) > 1300 && Projectile.timeLeft > FadeTime)
                Projectile.timeLeft = (int)FadeTime;

            ManageTrail();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DisableCrit();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(ObsidianThrowingDagger.TileHitSound, Projectile.Center);


            bool screwRegained = false;

            //50% chance to gain back the screw on kill.
            if (target.life - hit.Damage <= 0)
            {
                if (Main.rand.NextBool() && Main.myPlayer == Owner.whoAmI)
                {
                    if (Owner.HeldItem.ModItem is WulfrumScrewdriver screwdriver && !screwdriver.ScrewStored)
                    {
                        WulfrumScrewdriver.ScrewStart = new Vector3(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 4f * Main.rand.NextFloat() - Main.screenPosition, Projectile.rotation);
                        WulfrumScrewdriver.ScrewTimer = WulfrumScrewdriver.ScrewTime;
                        screwdriver.ScrewStored = true;

                        SoundEngine.PlaySound(SoundID.Item156);

                        screwRegained = true;
                    }
                }
            }

            if (!screwRegained && Main.netMode != NetmodeID.Server)
            {
                Gore screwGore = Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.position, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(4f, 6f) + Projectile.velocity * 0.7f, Mod.Find<ModGore>("WulfrumScrewGore").Type);
                screwGore.timeLeft = 20;
            }

            //Screenstun
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            if (BazingaTime > 0)
                SoundEngine.PlaySound(SoundDirectory.CommonSounds.WulfrumNPCDeathSound, Projectile.Center);

            SoundEngine.PlaySound(ObsidianThrowingDagger.TileHitSound, Projectile.Center);
            if (Main.netMode != NetmodeID.Server)
            {
                Gore screwGore = Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.position, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(1f, 3f) + Projectile.velocity * 0.7f, Mod.Find<ModGore>("WulfrumScrewGore").Type);
                screwGore.timeLeft = 20;
            }

            return base.OnTileCollide(oldVelocity);
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction, new TriangularTip(8f));

            TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, FablesUtils.SmoothBezierPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity;
        }


        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Pow(completionRatio, 2) * (float)Math.Pow(1 - BazingaTimeCompletion, 0.4f);
            return (Color.GreenYellow.MultiplyRGB(PrimColorMult) * fadeOpacity) with { A = 0 };
        }

        internal float WidthFunction(float completionRatio)
        {
            return 9.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;

            float distanceFromAim = Projectile.Center.ShortestDistanceToLine(Owner.MountedCenter, Main.MouseWorld);
            float distanceFromPlayerAcrossSightLine = (Owner.MountedCenter - Projectile.Center.ClosestPointOnLine(Owner.MountedCenter, Main.MouseWorld)).Length();

            float opacity = MathHelper.Clamp(1f - distanceFromAim / 90f, 0f, 1f) * (1f - Math.Clamp((float)Math.Pow(distanceFromPlayerAcrossSightLine / 300f, 9f), 0f, 1f));

            //Draw a sightline before the player hits it.
            if (Owner.whoAmI == Main.myPlayer && BazingaTime == 0 && opacity > 0)
            {
                Texture2D empty = ModContent.Request<Texture2D>(AssetDirectory.Invisible).Value;

                //Setup the laser sights effect.
                Effect laserScopeEffect = Scene["PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);

                laserScopeEffect.Parameters["mainOpacity"].SetValue((float)Math.Pow(opacity, 0.5f)); //Opacity increases as the screw gets close to the cursor

                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(700f * 0.2f));
                laserScopeEffect.Parameters["laserAngle"].SetValue((Main.MouseWorld - Owner.MountedCenter).ToRotation() * -1);

                laserScopeEffect.Parameters["laserWidth"].SetValue(0.0025f + (float)Math.Pow(opacity, 5) * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.002f + 0.002f));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(3f);

                laserScopeEffect.Parameters["color"].SetValue(Color.GreenYellow.ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Black.ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f + (1 - opacity) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);

                Main.EntitySpriteDraw(empty, Projectile.Center - Main.screenPosition, null, Color.White, 0, empty.Size() / 2f, 350f, 0, 0);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            opacity = BazingaTimeCompletion * FadePercent;

            if (BazingaTime > 0)
            {
                Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
                effect.Parameters["time"].SetValue(0f);
                effect.Parameters["fadeDistance"].SetValue(0.3f);
                effect.Parameters["fadePower"].SetValue(1 / 6f);
                effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);

                FablesUtils.DrawChromaticAberration(Vector2.UnitX, 1f, delegate (Vector2 offset, Color colorMod) {
                    PrimColorMult = colorMod;
                    TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition + offset);
                });

                //Draw the screw with chroma abberation
                FablesUtils.DrawChromaticAberration(Vector2.UnitX, 3f, delegate (Vector2 offset, Color colorMod) {
                    Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + offset, null, (Color.GreenYellow.MultiplyRGB(colorMod) * opacity) with { A = 0 }, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
                });


                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor) * (1 - opacity), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);

                //draw a sheen texture like zenith.
                Texture2D shineTex = AssetDirectory.CommonTextures.BloomStreak.Value;
                Vector2 shineScale = new Vector2(0.5f, 1.5f - opacity);
                Color shineColor = Color.GreenYellow * (1 - opacity) * 0.7f;

                Main.EntitySpriteDraw(shineTex, Projectile.Center - Main.screenPosition, null, shineColor with { A = 0 }, MathHelper.PiOver2, shineTex.Size() / 2f, shineScale * Projectile.scale, SpriteEffects.None, 0);

            }

            else
            {
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor) * FadePercent, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }

    }
}
