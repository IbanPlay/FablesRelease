using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class DesertScourgeLightningStrike : ModProjectile
    {
        public ref float TelegraphTime => ref Projectile.ai[0];
        public ref float Height => ref Projectile.ai[1];
        public override string Texture => AssetDirectory.Invisible;

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;
        private List<Vector2> zappyCache;
        private float whiteFlashTimer;

        public bool IsJustLingering => Projectile.penetrate <= -1;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Thunder Strike");
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 80;
            Projectile.alpha = 255;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;

            Projectile.extraUpdates = 0;
            whiteFlashTimer = 10;
        }

        public override bool ShouldUpdatePosition() => TelegraphTime <= 0 && Projectile.penetrate != -1;
        public override bool? CanDamage() => TelegraphTime <= 0 && Projectile.penetrate != -1 ? null : false;

        public override void AI()
        {
            if (TelegraphTime > 0)
            {
                TelegraphTime--;
                Projectile.timeLeft++;

                if (TelegraphTime <= 0)
                {
                    Projectile.tileCollide = true;
                    Projectile.Center -= Vector2.UnitY * Height;
                    Projectile.velocity = Vector2.UnitY * 8f;

                    Projectile.extraUpdates = 13;
                    SoundEngine.PlaySound(SoundID.Thunder with { MaxInstances = 20 }, Projectile.Center);
                }

                return;
            }

            if (IsJustLingering || Projectile.timeLeft < 77)
            {
                ManageCache();
                ManageTrail();
            }

            if (IsJustLingering)
                return;

            if (Projectile.timeLeft < 77)
            {
                if (zappyCache != null)
                {
                    //Make the dust appear inbetween the recenter points
                    int previousPointIndex = zappyCache.Count - Main.rand.Next(1, 4);
                    Vector2 previousPosition = zappyCache[previousPointIndex];
                    Vector2 nextPosition = zappyCache[previousPointIndex - 1];

                    Vector2 position = Vector2.Lerp(previousPosition, nextPosition, Main.rand.NextFloat()) - Projectile.Size / 2;

                    int core = Dust.NewDust(position, Projectile.width, Projectile.height, 205, 0f, 0f, 150);    //bolt core (venomy)
                    Main.dust[core].noGravity = true;
                    //Main.dust[core].velocity = (Projectile.rotation + (Main.rand.NextFloatDirection() * MathHelper.PiOver2)).ToRotationVector2() * Main.dust[core].velocity.Length();
                    Main.dust[core].velocity = previousPosition.DirectionTo(nextPosition) * 4f;
                    Main.dust[core].scale = 1.5f;

                    int sparks = Dust.NewDust(position, Projectile.width, Projectile.height, 27, 0f, 0f, 150);    //bolt "spray"
                    Main.dust[sparks].noGravity = true;
                    Main.dust[sparks].velocity = previousPosition.DirectionTo(nextPosition) * 4f;
                    Main.dust[sparks].scale = 1.25f;
                }
            }

            //if you didnt hit anything, go "ghost" aka linger with no damage
            if (Projectile.timeLeft <= 1)
            {
                GoGhost();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity;
            GoGhost();
            return false;
        }

        public void GoGhost()
        {
            //if (Main.netMode != NetmodeID.MultiplayerClient)
            //Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PlasmaRodBoltExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

            Projectile.extraUpdates = 0;
            Projectile.timeLeft = 30;
            Projectile.penetrate = -1;
            Projectile.netUpdate = true;
            Projectile.netSpam = 0;
            Projectile.tileCollide = false;

            if (cache != null)
                cache.Add(Projectile.Center + Projectile.velocity * 2f);

            Projectile.velocity = Vector2.Zero;

            if (cache == null)
                return;

            //Remove all duplicates from the end of the trail
            zappyCache = null;
            while (cache.Count > 3 && cache[0] == cache[1])
            {
                cache.RemoveAt(0);
            }
        }

        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();

                for (int i = 0; i < 140; i++)
                {
                    cache.Add(Projectile.Center);
                }
            }

            //Add each new position to the trail (if moving)
            if (!IsJustLingering)
                cache.Add(Projectile.Center);

            while (cache.Count > 140)
            {
                cache.RemoveAt(0);
            }

            //Every 5 frames, the zappy cache gets "reset".
            if (zappyCache == null || Projectile.timeLeft % 5 == 0)
            {
                zappyCache = new List<Vector2>();

                //The zappy cache takes every fourth part of the main position cache, and wiggles them a bit randomly for the lightning look
                for (int i = 0; i < cache.Count; i += 4)
                {
                    float lerper = i / (float)cache.Count;

                    Vector2 point = cache[i];
                    Vector2 nextPoint = i == cache.Count - 1 ? cache[i - 1] : cache[i + 1];
                    Vector2 dir = Vector2.Normalize(nextPoint - point).RotatedBy(Main.rand.NextBool() ? MathHelper.PiOver2 : -MathHelper.PiOver2);

                    //if were at the tip of the trail or the direction points nowhere, add the trail point
                    if (i > cache.Count - 3 || dir == Vector2.Zero || float.IsNaN(dir.X))
                        zappyCache.Add(point);

                    //Add a point thats wiggly. The furthest from the tip, the wiggliest
                    else
                    {
                        float wiggliness = 25 - lerper * 15f;
                        if (IsJustLingering)
                            wiggliness *= (float)Math.Pow(Projectile.timeLeft / 30f, 0.2); //Wiggles less and less as the projectile fades

                        zappyCache.Add(point + (dir * Main.rand.NextFloat(wiggliness)));
                    }
                }
            }

            //If we are not on a fifth frame, we add a random offset to all the zap trail points, and then also make them move AWAY (lerp negative) from their original position
            else
            {
                for (int i = 0; i < zappyCache.Count; i++)
                {
                    float lerper = i / (float)zappyCache.Count;
                    zappyCache[i] = zappyCache[i] + Main.rand.NextVector2Circular(8f, 8f) * (1 - (float)Math.Pow(lerper, 3f));

                    if (i * 4 < cache.Count)
                        zappyCache[i] = Vector2.Lerp(zappyCache[i], cache[i * 4], -0.1f);
                }
            }
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(40, f =>
            {
                float baseWidth = 70f;
                if (!IsJustLingering)
                    return baseWidth * 0.8f * (float)Math.Pow(f, 0.5f);

                return baseWidth * (float)Math.Pow(f, 0.5f) * (float)Math.Pow(Projectile.timeLeft / 30f, 1.2f);

            },
            factor =>
            {
                float trailOpacity = 0.75f;
                float colorLerper = 1f;
                if (IsJustLingering)
                {
                    colorLerper = Projectile.timeLeft / 30f;
                    trailOpacity *= (float)Math.Pow(Projectile.timeLeft / 30f, 0.1f);
                }

                if (factor > 0.99f)
                    return Color.Transparent;

                Color trailColor;

                Color baseColor = Color.Lerp(new Color(255, 96, 255), Color.White, whiteFlashTimer / 10f);
                Color endColor = Color.Lerp(new Color(78, 5, 177), Color.RoyalBlue, colorLerper);

                trailColor = Color.Lerp(endColor, baseColor, factor);

                return trailColor * trailOpacity;
            });
            if (zappyCache.Count <= 2)
                return;
            TrailDrawer.SetPositionsSmart(zappyCache, Projectile.Center, RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = zappyCache[zappyCache.Count - 2] - zappyCache[zappyCache.Count - 1];
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TelegraphTime > 0)
            {
                Texture2D tex = AssetDirectory.CommonTextures.BloomCircle.Value;
                Vector2 scale = new Vector2(0.3f, Height / tex.Height);
                Rectangle rect = new Rectangle(0, 0, tex.Width, tex.Height / 2);

                float timer = 1 - (float)Math.Pow(TelegraphTime / 120f, 0.4f);
                Color alphaPurple = Color.RoyalBlue with { A = 0 };

                scale.X *= 1f - 0.3f * timer;
                scale.Y *= 1f + 0.5f * timer;

                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, rect, alphaPurple * 0.4f * timer, 0, tex.Size() / 2f, scale * 1.65f, SpriteEffects.None, 0);
                return false;
            }

            if (!IsJustLingering)
                return false;

            if (whiteFlashTimer > 0)
                whiteFlashTimer--;

            //Draw the trail
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 16f);
            Main.graphics.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value;

            TrailDrawer?.Render(effect, -Main.screenPosition);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);


            if (IsJustLingering)
            {
                Texture2D bloom = AssetDirectory.CommonTextures.PixelBloomCircle.Value;

                Vector2 bloomPosition = Projectile.Center - Main.screenPosition - Projectile.velocity * 0.4f;
                if (cache != null)
                    bloomPosition = cache[cache.Count - 3] - Main.screenPosition;

                Color alphaPurple = MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.8f + Projectile.whoAmI * 0.1f) % 1, Color.MediumOrchid, Color.DeepPink, Color.RoyalBlue) with { A = 0 };
                Color alphaWhite = Color.White with { A = 0 };

                float bloomOpacity = (float)Math.Pow(Projectile.timeLeft / 30f, 1.2f);
                float bloomSize = (float)Math.Pow(Projectile.timeLeft / 30f, 0.4f);

                //Draws 2 layers of circular bloom on the tip of the bolt
                Main.EntitySpriteDraw(bloom, bloomPosition, null, alphaPurple * 0.4f * bloomOpacity, 0, bloom.Size() / 2f, bloomSize * Projectile.scale * 1.65f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(bloom, bloomPosition, null, alphaWhite * bloomOpacity, 0, bloom.Size() / 2f, bloomSize * Projectile.scale * 0.85f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(bloom, bloomPosition, null, alphaWhite * bloomOpacity, 0, bloom.Size() / 2f, bloomSize * Projectile.scale * 0.55f, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}