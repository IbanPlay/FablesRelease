namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public class NautilusTrident : ModProjectile
    {
        public bool TileHoming => Projectile.ai[1] != 0;
        public bool PlayerHoming => !TileHoming && Projectile.ai[0] > -1 && Projectile.ai[0] < 255;
        public Vector2 TileTarget => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        public Player PlayerTarget => Main.player[(int)Projectile.ai[0]];

        public static int MaxTime = 100;
        public float Completion => (MaxTime - Projectile.timeLeft) / (float)MaxTime;

        public override string Texture => AssetDirectory.SirNautilus + Name;
        public bool FadingAway {
            get => Projectile.localAI[0] > 0;
            set => Projectile.localAI[0] = value ? 1f : 0;
        }

        public ref float LastRecordedVelocity => ref Projectile.localAI[1];


        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> trailCache;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Trident");
        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 100;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
        }

        public void StartDissapearing()
        {
            Projectile.tileCollide = false;
            FadingAway = true;
            Projectile.velocity *= 0.4f;
            Projectile.timeLeft = 30;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            if (Collision.SolidCollision(Projectile.Center, 1, 1, false))
                OnTileCollide(Projectile.velocity);

            return false;
        }


        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float halfLength = 36f * Projectile.scale;
            Vector2 projectileDirection = Projectile.rotation.ToRotationVector2();

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - projectileDirection * halfLength, Projectile.Center + projectileDirection * halfLength, 24, ref collisionPoint);
        }

        public override bool? CanDamage() => !FadingAway;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!FadingAway)
            {
                if (Completion < 0.7f)
                    Projectile.velocity *= 1.04f;
                else
                    Projectile.velocity *= 0.98f;

                if (!Main.rand.NextBool(3))
                {
                    Dust zust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), 43, Projectile.velocity * 0.25f, 100, Color.DeepSkyBlue, Main.rand.NextFloat(0.7f, 1f));
                    zust.noGravity = true;
                }

                if (Main.rand.NextBool(3))
                {
                    Dust zust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), 43, -Projectile.velocity * 0.25f, 200, Color.Turquoise, Main.rand.NextFloat(0.9f, 1.2f));
                    zust.noGravity = true;
                }

                LastRecordedVelocity = Projectile.velocity.Length();


                if (TileHoming && Projectile.Distance(TileTarget) < 10f + Projectile.velocity.Length())
                {
                    StartDissapearing();
                    Projectile.velocity *= 0.2f;
                    SoundEngine.PlaySound(SirNautilus.TridentHit, Projectile.Center);

                    if (!Main.dedServ && Main.LocalPlayer.Distance(Projectile.Center) < 500)
                        CameraManager.Shake += 3f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_Death(), TileTarget, Vector2.UnitY * 4f, ModContent.ProjectileType<SandstoneBoulder>(), SirNautilus.TridentThrow_BoulderDamage / 2, 2);
                    }
                }

                else if (PlayerHoming)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(PlayerTarget.Center) * Projectile.velocity.Length(), 0.08f);
                    if (PlayerTarget.Distance(Projectile.Center) < PlayerTarget.Distance(Projectile.Center + Projectile.velocity))
                        Projectile.tileCollide = true;

                    if (Projectile.velocity.Length() < 3f)
                        StartDissapearing();
                }

                else if (!TileHoming && !Projectile.tileCollide)
                    Projectile.tileCollide = Vector2.Dot(Projectile.velocity.SafeNormalize(Vector2.Zero), Projectile.DirectionTo(Projectile.GetNearestPlayer().Center)) < 0.1f;


                if (Projectile.timeLeft == 1)
                    StartDissapearing();
            }

            else
                Projectile.velocity *= 0.85f;

            if (!Main.dedServ)
            {
                ManageCaches();
                ManageTrail();
            }
        }

        private void ManageCaches()
        {
            if (trailCache == null)
            {
                trailCache = new List<Vector2>();

                for (int i = 0; i < 30; i++)
                {
                    trailCache.Add(Projectile.Center + Projectile.rotation.ToRotationVector2() * 30f);
                }
            }

            trailCache.Add(Projectile.Center + Projectile.rotation.ToRotationVector2() * 30f);

            while (trailCache.Count > 30)
            {
                trailCache.RemoveAt(0);
            }
        }

        private void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, factor =>
            {
                float baseWidth = factor * 13;
                if (factor > 0.97f)
                    baseWidth *= 0.5f + 0.5f * ((factor - 0.97f) / 0.03f);

                baseWidth *= 1f - 0.5f * Utils.GetLerpValue(LastRecordedVelocity, 3f, 10f, true);
                return baseWidth;

            }, factor =>
            {
                Color baseColor = Color.DodgerBlue;

                if (FadingAway)
                    baseColor *= Projectile.timeLeft / 30f;

                return baseColor * 0.5f * (float)Math.Pow(factor, 2f);
            }, new TriangularTip(8f));

            TrailDrawer.SetPositions(trailCache);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(4f);

            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
            effect.Parameters["streakScale"].SetValue(1f);
            TrailDrawer?.Render(effect, -Main.screenPosition);

            Texture2D tex = TextureAssets.Projectile[Type].Value;

            if (!FadingAway)
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);

            else
            {
                Effect reapparification = Scene["NautilusTridentApparification"].GetShader().Shader;
                reapparification.Parameters["completion"].SetValue(Projectile.timeLeft / 30f);
                reapparification.Parameters["sourceFrame"].SetValue(new Vector4(0, 0, tex.Width, tex.Height));
                reapparification.Parameters["texSize"].SetValue(tex.Size());
                reapparification.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
                reapparification.Parameters["lightColor"].SetValue(Color.Lerp(lightColor, Color.White, 1 - Projectile.timeLeft / 30f));
                reapparification.Parameters["sidewaysGradient"].SetValue(true);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, reapparification, Main.GameViewMatrix.TransformationMatrix);

                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SirNautilus.TridentHit, Projectile.Center);
            Projectile.velocity = oldVelocity * 0.4f;
            StartDissapearing();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            SoundEngine.PlaySound(SirNautilus.TridentHit, target.Center);
            Projectile.penetrate--;
            if (Projectile.penetrate <= 0)
                StartDissapearing();
        }
    }
}
