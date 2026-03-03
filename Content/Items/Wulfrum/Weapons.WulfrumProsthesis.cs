using CalamityFables.Particles;
using ReLogic.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumProsthesis")]
    public class WulfrumProsthesis : ModItem, IHideFrontArm
    {
        public static readonly SoundStyle ShootSound = new(SoundDirectory.Wulfrum + "WulfrumProsthesisShoot") { PitchVariance = 0.1f, Volume = 0.55f };
        public static readonly SoundStyle HitSound = new(SoundDirectory.Wulfrum + "WulfrumProsthesisHit") { PitchVariance = 0.1f, Volume = 0.75f, MaxInstances = 3 };
        public static readonly SoundStyle SuckSound = new(SoundDirectory.Wulfrum + "WulfrumProsthesisSucc") { Volume = 0.5f };
        public static readonly SoundStyle SuckStopSound = new(SoundDirectory.Wulfrum + "WulfrumProsthesisSuccStop") { Volume = 0.5f };

        public static float MANA_SUCK_RANGE = 300f;
        public static float MANA_SICKNESS_MIN_TIME = 2f;
        public static float MANA_SICKNESS_MAX_TIME = 10f;
        public static float MANA_PER_SECOND = 30f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Prosthesis");
            Tooltip.SetDefault("Casts a wulfrum bolt\n" +
                               "Right click to drain mana from creatures in front of you\n" +
                               "[c/83B87E:Technology and magic have been forever locked in an arms race of imitation and retaliation]\n" +
                               "[c/83B87E:At times they work in tandem, as seen in certain prosthetic limbs]");
            //Lore about how magic is not always a given for everyone, and how some unlucky people sometimes resort to voluntarily cutting their limbs to use magic augmented prosthesis
            //1 : Informs about magic as a narrative thing, 2 : Informs about wulfrum energy being partly magical.
            Item.ResearchUnlockCount = 1;
        }

        internal static Asset<Texture2D> RealSprite;
        public override string Texture => AssetDirectory.WulfrumItems + Name + "_Arm";

        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 6;
            Item.width = 34;
            Item.height = 42;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = ShootSound;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WulfrumBolt>();
            Item.shootSpeed = 18f;
            Item.holdStyle = 16; //Custom hold style
            Item.ChangePlayerDirectionOnShoot = false;
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();
            player.SyncRightClick();
        }

        public override bool CanUseItem(Player player) => !Main.projectile.Any(n => n.active && n.owner == player.whoAmI && n.type == ModContent.ProjectileType<WulfrumManaDrain>());

        public override bool AltFunctionUse(Player player) => true;

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.altFunctionUse == 2)
                type = ModContent.ProjectileType<WulfrumManaDrain>();
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            if (player.altFunctionUse == 2)
                mult = 0f;
        }

        public override void UseAnimation(Player player)
        {
            Item.UseSound = ShootSound;
            if (player.altFunctionUse == 2)
                Item.UseSound = null;
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

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;

            //Default
            Vector2 itemPosition = player.MountedCenter + new Vector2(-2f * player.direction, -1f * player.gravDir);
            float itemRotation = (player.MouseWorld() - itemPosition).ToRotation();

            //Adjust for animation

            if (animProgress < 0.7f)
                itemPosition -= itemRotation.ToRotationVector2() * (1 - (float)Math.Pow(1 - (0.7f - animProgress) / 0.7f, 4)) * 4f;

            if (animProgress < 0.4f)
                itemRotation += -0.45f * (float)Math.Pow((0.4f - animProgress) / 0.4f, 2) * player.direction * player.gravDir;

            //Shakezzz
            if (player.itemTime == 1 && Main.projectile.Any(n => n.active && n.owner == player.whoAmI && n.type == ModContent.ProjectileType<WulfrumManaDrain>()))
            {
                itemPosition += Main.rand.NextVector2Circular(2f, 2f);
            }


            Vector2 itemSize = new Vector2(28, 14);
            Vector2 itemOrigin = new Vector2(-8, 0);
            CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true);
        }

        public override void HoldStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player, heldItemFrame);
        public override void UseStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player, heldItemFrame);

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (RealSprite == null)
                RealSprite = ModContent.Request<Texture2D>(AssetDirectory.WulfrumItems + Name);

            Texture2D properSprite = RealSprite.Value;

            spriteBatch.DrawNewInventorySprite(properSprite, new Vector2(28f, 14f), position, drawColor, origin, scale, new Vector2(0, -6f));

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (RealSprite == null)
                RealSprite = ModContent.Request<Texture2D>(AssetDirectory.WulfrumItems + Name);

            Texture2D properSprite = RealSprite.Value;

            spriteBatch.Draw(properSprite, Item.position - Main.screenPosition, null, lightColor, rotation, properSprite.Size() / 2f, scale, 0, 0);

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>(10).
                AddIngredient<EnergyCore>().
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class WulfrumProsthesisPlayer : ModPlayer
    {
        public bool ManaDrainActive = false;

        public override void UpdateDead()
        {
            ManaDrainActive = false;
        }

        public override void PostUpdateMiscEffects()
        {
            if (ManaDrainActive)
            {
                //+1 mana regen count means 0.5 mana per second

                Player.manaRegenCount += (int)(WulfrumProsthesis.MANA_PER_SECOND * 2);

                /*
                int buffIndex = Player.FindBuffIndex(BuffID.ManaSickness);

                //Give the player the minimum infliction time
                if (buffIndex == -1)
                    Player.AddBuff(BuffID.ManaSickness, (int)(WulfrumProsthesis.MANA_SICKNESS_MIN_TIME * 60));
                //Increase to reach the max infliction time
                else if (Player.buffTime[buffIndex] < WulfrumProsthesis.MANA_SICKNESS_MAX_TIME * 60)
                {
                    Player.buffTime[buffIndex] += 4;
                }
                */
            }

            ManaDrainActive = false;
        }
    }

    public class WulfrumBolt : ModProjectile
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

        internal PrimitiveTrail TrailDrawer;
        //internal PrimitiveTrail TrailDrawer2;
        internal Color PrimColorMult = Color.White;

        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bolt");

            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 90;
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
            if (Projectile.timeLeft == 90)
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

            Projectile.velocity *= 0.973f;
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * Projectile.velocity.Length();

            //Blast off.
            if (Projectile.timeLeft == 90)
            {
                Vector2 dustCenter = Projectile.Center + Projectile.velocity * 1f;

                for (int i = 0; i < 5; i++)
                {
                    Dust chust = Dust.NewDustPerfect(dustCenter, 15, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(1.2f, 1.8f));
                    chust.noGravity = true;
                }
            }

            if (Projectile.timeLeft <= 87)
            {
                if (Main.rand.NextBool())
                {
                    Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Dust chust = Dust.NewDustPerfect(dustCenter, 15, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(1.2f, 1.8f));
                    chust.noGravity = true;
                }

                if (Main.rand.NextBool(4))
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
            
            TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, SmoothBezierPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;

            //TrailDrawer2 = TrailDrawer2 ?? new AffinePrimitiveTrail(30, WidthFunction, ColorFunction);
            //TrailDrawer2.Positions = TrailDrawer.Positions;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Sqrt(completionRatio);
            return (Color.DeepSkyBlue.MultiplyRGB(PrimColorMult) * fadeOpacity) with { A = 0 };
        }

        internal float WidthFunction(float completionRatio)
        {
            return 9.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
                Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
                effect.Parameters["time"].SetValue(0f);
                effect.Parameters["fadeDistance"].SetValue(0.3f);
                effect.Parameters["fadePower"].SetValue(1 / 6f);
                effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            

            DrawChromaticAberration(Vector2.UnitX, 3.5f, delegate (Vector2 offset, Color colorMod) {
                    PrimColorMult = colorMod ;
                    TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition + offset);
           });


            //PrimColorMult = Color.White;
            //TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition);
            //TrailDrawer2?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition - Vector2.UnitY * 200f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(WulfrumProsthesis.HitSound, Projectile.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            return base.OnTileCollide(oldVelocity);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int numParticles = Main.rand.Next(4, 7);
            for (int i = 0; i < numParticles; i++)
            {
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.Pi / 6f) * Main.rand.NextFloat(3, 14);
                ParticleHandler.SpawnParticle(new TechyHoloysquareParticle(target.Center, velocity, Main.rand.NextFloat(2.5f, 3f), Main.rand.NextBool() ? new Color(99, 255, 229) : new Color(25, 132, 247), 25));
            }
        }
    }


    public class WulfrumManaDrain : ModProjectile
    {
        private SlotId SuccSoundSlot;
        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => AssetDirectory.Invisible;

        public ref float Timer => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mana Drain");
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }
        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;



        public override void AI()
        {
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;

            if (!Owner.RightClicking() || Owner.dead || Owner.frozen || !Owner.active || Owner.statMana == Owner.statManaMax2)
                return;

            //Once again the sound volume scaling does NOT work for whatever reason? Help.

            if (!SoundEngine.TryGetActiveSound(SuccSoundSlot, out var idleSoundOut) || !idleSoundOut.IsPlaying)
            {
                SuccSoundSlot = SoundEngine.PlaySound(WulfrumProsthesis.SuckSound with { Volume = WulfrumProsthesis.SuckSound.Volume * 0.01f, IsLooped = true }, Owner.Center);

            }

            else if (idleSoundOut != null)
            {
                idleSoundOut.Position = Owner.Center;
                idleSoundOut.Volume = Math.Clamp((Timer / 30f) + 0.001f, 0f, 1f) * 100f;
            }


            Projectile.timeLeft = 2;
            Projectile.Center = Owner.MountedCenter;
            Projectile.velocity = (Owner.MouseWorld() - Owner.MountedCenter).SafeNormalize(Vector2.One);

            if (Main.rand.NextBool(6))
            {
                Particle streak = new ManaDrainStreak(Owner, Main.rand.NextFloat(0.2f, 0.5f), Projectile.velocity.RotatedByRandom(MathHelper.PiOver2 * 0.6f) * Main.rand.NextFloat(70f, 150f), Main.rand.NextFloat(30f, 44f), Color.GreenYellow, Color.DeepSkyBlue, Main.rand.Next(13, 20));
                ParticleHandler.SpawnParticle(streak);
            }

            NPC target = GetSuccTarget();

            if (target != null)
            {
                Owner.GetModPlayer<WulfrumProsthesisPlayer>().ManaDrainActive = true;

                if (!Main.rand.NextBool(3))
                {
                    Vector2 center = target.Center;
                    center.X += (float)Main.rand.Next(-100, 100) * 0.1f;
                    center.Y += (float)Main.rand.Next(-100, 100) * 0.1f;

                    center += target.velocity;
                    //Drain ganggg

                    Particle bloom = new ManaDrainBlob(Owner, center, Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(0.7f, 0.9f), Color.DeepSkyBlue);
                    ParticleHandler.SpawnParticle(bloom);
                }
            }

            Timer++;
        }

        public NPC GetSuccTarget()
        {
            float collisionPoint = 0f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC struckNPC = Main.npc[i];

                if (!struckNPC.active || struckNPC.townNPC || struckNPC.friendly)
                    continue;

                float distance = struckNPC.Distance(Projectile.Center);
                float extraDistance = struckNPC.width / 2 + struckNPC.height / 2;

                if (distance - extraDistance < WulfrumProsthesis.MANA_SUCK_RANGE)
                {
                    if (!Collision.CheckAABBvLineCollision(struckNPC.Hitbox.TopLeft(), struckNPC.Hitbox.Size(), Owner.MountedCenter, Owner.MountedCenter + Projectile.velocity * WulfrumProsthesis.MANA_SUCK_RANGE, 110f, ref collisionPoint))
                        continue;

                    if (!Collision.CanHit(Projectile.Center, 1, 1, struckNPC.Center, 1, 1) && extraDistance < distance)
                        continue;

                    return struckNPC;
                }
            }

            return null;
        }

        public override void OnKill(int timeLeft)
        {
            if (Timer > 2)
            {
                NPC target = GetSuccTarget();

                if (target != null)
                {
                    int particlesCount = Main.rand.Next(5, 10);
                    for (int i = 0; i < particlesCount; i++)
                    {
                        Vector2 center = target.Center;
                        center.X += (float)Main.rand.Next(-100, 100) * 0.1f;
                        center.Y += (float)Main.rand.Next(-100, 100) * 0.1f;

                        center += target.velocity;
                        //Drain ganggg

                        Particle bloom = new ManaDrainBlob(Owner, center, Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(0.76f, 1f), Color.DeepSkyBlue);
                        ParticleHandler.SpawnParticle(bloom);
                    }
                }
            }

            if (SoundEngine.TryGetActiveSound(SuccSoundSlot, out var soundOut))
            {
                soundOut.Stop();
                SoundEngine.PlaySound(WulfrumProsthesis.SuckStopSound with { Volume = WulfrumProsthesis.SuckStopSound.Volume * Timer / 30f }, Projectile.Center);

            }
        }
    }
}
