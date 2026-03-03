namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public class SignathionSpectralBolt : ModProjectile
    {
        public override string Texture => AssetDirectory.Tiles + "PointOfInterest2xGlow";

        public bool Teal => Projectile.ai[0] == 1;
        public static readonly Vector3 BlueColor = new Vector3(0, 191, 255);
        public static readonly Vector3 TealColor = new Vector3(0, 250, 154);

        public Vector3 UsedColor => Teal ? TealColor : BlueColor;


        public ref float SpecialState => ref Projectile.ai[1];
        public bool TurnsIntoPuddle => Projectile.ai[1] > 0;
        public bool PuddleMode => Projectile.ai[1] > 1.5f;
        public bool DeathStuck => Projectile.ai[1] < 0f;

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> trailCache;


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spectral Water");

            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
        }

        public override bool ShouldUpdatePosition() => !PuddleMode && !DeathStuck;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Turquoise.ToVector3() * 0.4f);


            if (!PuddleMode && !DeathStuck)
            {
                Projectile.velocity.Y += 0.22f;
                Projectile.rotation = Projectile.velocity.ToRotation();

                if (Projectile.localAI[0] == 0f)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        Dust cust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 1.8f);

                        cust.noGravity = true;
                        cust.velocity = Projectile.velocity * 0.6f + Main.rand.NextVector2Circular(2f, 2f);
                        cust.rotation = Main.rand.NextFloat(1f, 1.3f);

                        cust.customData = UsedColor;
                    }
                    Projectile.localAI[0] = 1f;
                }

                if (!Main.rand.NextBool(4))
                {
                    Dust cust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<SpectralWaterDustNoisy>(), 0f, 0f, 100, default, 1f);

                    cust.noGravity = true;
                    cust.velocity *= 0.5f;
                    cust.velocity -= Projectile.velocity * 0.15f;

                    cust.rotation = Main.rand.NextFloat(0.5f, 1f);

                    cust.customData = UsedColor;
                }

                if (!Main.rand.NextBool(3))
                {
                    Dust cust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<SpectralWaterDustNoisy>(), 0f, 0f, 100, default, 1.6f);

                    cust.noGravity = true;
                    cust.velocity *= 0.5f;
                    cust.velocity = Projectile.velocity * 0.15f;

                    cust.rotation = Main.rand.NextFloat(1.2f, 1.7f);

                    cust.customData = UsedColor;
                }
            }

            else if (PuddleMode)
            {
                Projectile.velocity = Vector2.UnitY * 5f;

                if (Main.rand.NextBool(5))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust cust = Dust.NewDustDirect(Projectile.BottomLeft, Projectile.width, 3, ModContent.DustType<SpectralWaterDustNoisy>(), 0f, 0f, 100, default, 1.2f);
                        cust.noGravity = true;
                        cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);

                        cust.customData = UsedColor;
                    }
                }

                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dust cust = Dust.NewDustDirect(Projectile.BottomLeft, Projectile.width, 1, ModContent.DustType<SpectralWaterDustNoisy>(), 0f, 0f, 100, default, 1.2f);
                        cust.noGravity = true;
                        cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.4f);

                        cust.customData = UsedColor;
                    }

                }

                if (Main.rand.NextBool(3))
                {
                    Dust cust = Dust.NewDustDirect(Projectile.BottomLeft, Projectile.width, 1, ModContent.DustType<SpectralWaterDustEmbers>(), 0f, 0f, 255, Color.White, 1.2f);
                    cust.noGravity = true;
                    cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.4f);
                }


                Lighting.AddLight(Projectile.Bottom / 16, 2f * 255f * Color.Turquoise.ToVector3() * (0.8f + 0.2f * (float)Math.Sin(Projectile.whoAmI + Main.GlobalTimeWrappedHourly * 0.4f)));

            }

            if (!Main.dedServ)
            {
                ManageCache();
                ManageTrail();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (TurnsIntoPuddle)
            {
                if (!PuddleMode && Math.Abs(Projectile.velocity.X - oldVelocity.X) <= 0.1 && oldVelocity.Y >= 0)
                {
                    Projectile.timeLeft = SpecialState >= 1.5f ? SirNautilus.SpecterBolts_ShotgunPuddleTime : SirNautilus.SpecterBolts_PuddleTime;

                    SpecialState = 2;

                    Projectile.velocity.X = 0;

                    Projectile.tileCollide = false;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.position = Projectile.Bottom;
                    Projectile.width = 70;
                    Projectile.height = 14;
                    Projectile.Bottom = Projectile.position;

                    for (int i = 0; i < 26; i++)
                    {
                        Projectile.position.Y += 1;
                        if (Collision.SolidCollision(Projectile.BottomLeft - Vector2.UnitY * 3, Projectile.width, 2))
                            break;
                    }
                }

                else
                    StartDying(oldVelocity);

                return false;
            }

            else
            {
                StartDying(oldVelocity);
                return false;
            }
        }

        public void StartDying(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Item21, Projectile.position);

            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;

            Projectile.timeLeft = 16;
            Projectile.ai[1] = -0.5f;

            for (int i = 0; i < 33; i++)
            {
                Dust cust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f), ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 1.2f);
                cust.noGravity = false;
                cust.velocity = oldVelocity * Main.rand.NextFloat(0.1f, 0.3f) + Main.rand.NextVector2Circular(0.3f, 0.3f);
                cust.rotation = 1f;

                cust.customData = UsedColor;
            }
        }

        private void ManageCache()
        {
            if (trailCache == null)
            {
                trailCache = new List<Vector2>();

                for (int i = 0; i < 30; i++)
                {
                    trailCache.Add(Projectile.Center);
                }
            }

            if (!PuddleMode && trailCache.Count > 0 && Projectile.Center != trailCache[trailCache.Count - 1])
                trailCache.Add(Projectile.Center);

            if (trailCache.Count >= 2)
                trailCache.RemoveAt(0);
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, factor =>
            {
                float baseWidth = factor * 16;
                if (factor > 0.97f)
                    baseWidth *= 0.5f + 0.5f * ((factor - 0.97f) / 0.03f);
                return baseWidth;

            }, factor =>
            {
                Color baseColor = Teal ? Color.MediumSpringGreen : Color.DodgerBlue;

                if (DeathStuck)
                    baseColor *= Projectile.timeLeft / 16f;

                float multiplier = 0.5f;
                if (Teal)
                    multiplier *= 0.34f;

                return baseColor * multiplier * (float)Math.Pow(factor, 2f);
            }, new TriangularTip(8f));

            if (trailCache.Count > 1)
            {
                TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, FablesUtils.SmoothBezierPointRetreivalFunction);
                TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (PuddleMode)
            {
                Texture2D tex = TextureAssets.Projectile[Type].Value;

                Vector2 scale = new Vector2(Projectile.width / (float)tex.Width, Projectile.height * 5.5f / (float)tex.Height);
                scale.Y *= (0.8f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.6f));

                Color color = new Color(UsedColor.X, UsedColor.Y, UsedColor.Z) * 0.3f;
                color.A = 0;

                if (Projectile.timeLeft < 15)
                    color *= Projectile.timeLeft / 15f;

                Main.EntitySpriteDraw(tex, Projectile.Bottom - Vector2.UnitY * 3f - Main.screenPosition, null, color, 0f, new Vector2(tex.Width / 2f, tex.Height - 16), scale, 0, 0);
            }

            if (trailCache.Count <= 1)
                return false;

            Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(4f);

            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);

            TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if (PuddleMode)
                modifiers.FinalDamage *= SirNautilus.SpecterBolts_PuddleDamageReduction;
        }

        public override void OnKill(int timeLeft)
        {
            if (PuddleMode)
            {
                SoundEngine.PlaySound(SirNautilus.SpectralWaterSizzle, Projectile.position);

                for (int i = 0; i < 43; i++)
                {
                    Dust cust = Dust.NewDustDirect(Projectile.BottomLeft, Projectile.width, 3, ModContent.DustType<SpectralWaterDustNoisy>(), 0f, 0f, 100, default, 0.8f);
                    cust.noGravity = true;
                    cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 5f);
                    cust.rotation = 1.3f;

                    cust.customData = UsedColor;
                }
            }

        }
    }
}
