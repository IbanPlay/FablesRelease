using CalamityFables.Content.Items.BaseTypes;
using CalamityFables.Cooldowns;
using CalamityFables.Particles;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumFusionCannon")]
    public class WulfrumFusionCannon : HeldOnlyItem, IHideFrontArm
    {
        public static readonly SoundStyle ShootSound = new(SoundDirectory.Wulfrum + "WulfrumProsthesisShoot") { PitchVariance = 0.1f, Volume = 0.4f };

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Experimental Wulfrum Fusion Array");
            Tooltip.SetDefault("Fires quick bursts of medium-range pellets\n" +
                "[c/878787:\"Who needs whips when you can simply become the summon yourself?\"]");
            Item.ResearchUnlockCount = 0;
            //Imaging hiding actually important/interesting/funny lore in there... :drool: that would be so dark souls
        }
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public bool noAnimation = false;

        public override void SetDefaults()
        {
            Item.damage = 6;
            Item.ArmorPenetration = 10;
            Item.DamageType = DamageClass.Summon;
            Item.width = 34;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = ShootSound;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WulfrumFusionBolt>();
            Item.shootSpeed = 18f;
            Item.holdStyle = 16; //Custom hold style

            Item.useAnimation = 10;
            Item.useTime = 4;
            Item.reuseDelay = 17;
            Item.noUseGraphic = false;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var name = tooltips.FirstOrDefault(x => x.Name == "ItemName" && x.Mod == "Terraria");
            name.OverrideColor = Color.Lerp(new Color(194, 255, 67), new Color(112, 244, 244), 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f));
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();

            if (player.whoAmI == Main.myPlayer)
            {
                if (!WulfrumHat.HasArmorSet(player))
                {
                    Item.type = 0;
                    Item.SetDefaults(0);
                    Item.stack = 0;

                    Main.mouseItem = new Item();
                }
            }

            Item.noUseGraphic = false;
            if (!player.FindCooldown(WulfrumBastionCooldown.ID, out var cd) || cd.timeLeft > WulfrumHat.BastionCooldown + WulfrumHat.BastionTime - WulfrumHat.BastionBuildTime)
                Item.noUseGraphic = true;

        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity = velocity.RotatedByRandom(MathHelper.PiOver4 * 0.1f);
        }

        public override bool CanUseItem(Player player)
        {
            return player.FindCooldown(WulfrumBastionCooldown.ID, out var cd) && cd.timeLeft < WulfrumHat.BastionCooldown + WulfrumHat.BastionTime - WulfrumHat.BastionBuildTime;
        }

        public void SetItemInHand(Player player, Rectangle heldItemFrame)
        {
            //Make the player face where they're aiming.
            if (player.MouseWorld().X > player.Center.X)
            {
                player.ChangeDir(1);
            }
            else
            {
                player.ChangeDir(-1);
            }

            //Hide the cannon till its visible on the player
            if (!player.FindCooldown(WulfrumBastionCooldown.ID, out var cd) || cd.timeLeft > WulfrumHat.BastionCooldown + WulfrumHat.BastionTime - WulfrumHat.BastionBuildTime)
                return;

            if (player.ItemTimeIsZero)
                noAnimation = false;
            if (player.itemAnimation > Item.useAnimation)
                noAnimation = true;

            float animProgress = 1 - player.itemAnimation / (float)player.itemAnimationMax;
            //It beecomes nan if the player loads into a world with the set bonus already active / without shooting any weapons before using it.
            //this is because itemAnimationMax isnt set before the item gets used once.
            if (noAnimation || animProgress is float.NaN)
                animProgress = 1;

            //Default
            Vector2 itemPosition = player.MountedCenter + new Vector2(-2f * player.direction, -1f * player.gravDir);
            float itemRotation = (player.MouseWorld() - itemPosition).ToRotation();

            //Adjust for animation

            if (animProgress < 0.9f)
                itemPosition -= itemRotation.ToRotationVector2() * (1 - (float)Math.Pow(1 - (0.9f - animProgress) / 0.9f, 4)) * 3f;

            if (animProgress < 0.6f)
                itemRotation += -0.3f * (float)Math.Pow((0.6f - animProgress) / 0.6f, 2) * player.direction * player.gravDir;

            //Shakezzz
            itemPosition += Main.rand.NextVector2Circular(2f, 2f) * (1 - animProgress);


            Vector2 itemSize = new Vector2(38, 18);
            Vector2 itemOrigin = new Vector2(-12, 0);
            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true);
        }

        public override void HoldStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player, heldItemFrame);
        public override void UseStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player, heldItemFrame);
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) => false;
    }


    public class WulfrumFusionBolt : ModProjectile
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
        public static float HomingRange = 250;
        public static float HomingAngle = MathHelper.PiOver4 * 1.65f;
        public static float HomingStrength = 0.05f;

        internal PrimitiveTrail TrailDrawer;
        internal Color PrimColorMult = Color.White;

        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fusion Bolt");

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
                Projectile.rotation = Projectile.velocity.ToRotation();

                Target = null;
            }
            else
            {
                Target = FindTarget();
            }

            Lighting.AddLight(Projectile.Center, (Color.DeepSkyBlue).ToVector3() * 0.5f);

            if (Target != null)
            {
                float distanceFromTarget = (Target.Center - Projectile.Center).Length();
                Projectile.rotation = Projectile.rotation.AngleTowards((Target.Center - Projectile.Center).ToRotation(), HomingStrength * (float)Math.Pow((1 - distanceFromTarget / HomingRange), 2));
            }

            Projectile.velocity *= 0.983f;
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * Projectile.velocity.Length();

            //Blast off.
            if (Projectile.timeLeft == 140)
            {
                Vector2 dustCenter = Projectile.Center + Projectile.velocity * 1f;

                for (int i = 0; i < 5; i++)
                {
                    Dust chust = Dust.NewDustPerfect(dustCenter, 15, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(1.2f, 1.8f));
                    chust.noGravity = true;
                }
            }

            if (Projectile.timeLeft <= 137)
            {
                if (Main.rand.NextBool(5))
                {
                    Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);
                    Dust chust = Dust.NewDustPerfect(dustCenter, 15, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(1.2f, 1.8f));
                    chust.noGravity = true;
                }

                if (Main.rand.NextBool(8))
                {
                    Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Dust largeDust = Dust.NewDustPerfect(dustCenter, 257, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.4f), Scale: Main.rand.NextFloat(0.4f, 1f));
                    largeDust.noGravity = true;
                    largeDust.noLight = true;
                }

                if (Main.rand.NextBool(5))
                {
                    Vector2 center = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.Pi / 6f) * Main.rand.NextFloat(4, 10);
                    ParticleHandler.SpawnParticle(new TechyHoloysquareParticle(center, velocity, Main.rand.NextFloat(1f, 2f), Main.rand.NextBool() ? new Color(99, 255, 229) : new Color(25, 132, 247), 25));

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
            return (Color.Lerp(Color.DeepSkyBlue, Color.YellowGreen, Projectile.timeLeft / 140f).MultiplyRGB(PrimColorMult) * fadeOpacity) with { A = 0};
        }

        internal float WidthFunction(float completionRatio)
        {
            return 6.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;

            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 6f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);

            FablesUtils.DrawChromaticAberration(Vector2.UnitX, 0.5f, delegate (Vector2 offset, Color colorMod) {
                PrimColorMult = colorMod;
                TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition + offset);
            });
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(WulfrumProsthesis.HitSound with { Volume = WulfrumProsthesis.HitSound.Volume * 0.6f }, Projectile.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            return base.OnTileCollide(oldVelocity);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI; //There exists a "whoAmIToTargettingIndex but idk if thats needed question mark

            int numParticles = Main.rand.Next(1, 3);
            for (int i = 0; i < numParticles; i++)
            {
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.Pi / 6f) * Main.rand.NextFloat(3, 14);
                ParticleHandler.SpawnParticle(new TechyHoloysquareParticle(target.Center, velocity, Main.rand.NextFloat(2.5f, 3f), Main.rand.NextBool() ? new Color(99, 255, 229) : new Color(25, 132, 247), 25));
            }
        }
    }
}
