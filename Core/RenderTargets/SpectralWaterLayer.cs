using CalamityFables.Content.Boss.SeaKnightMiniboss;

namespace CalamityFables.Core
{
    public class SpectralPuddleDrawingSystem : ModSystem
    {
        public static RenderTarget2D spectralWaterRenderTarget;
        public static Vector3 UsedColor;
        public static int coloredDustCount;
        public static Vector3 averageDustColor = Vector3.Zero;
        public static List<int> spectralDust = new List<int>();
        public static int dustType;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTarget;
            RenderTargetsManager.DrawToRenderTargetsEvent += DrawToRenderTarget;
            FablesDrawLayers.DrawBehindDustEvent += DrawBatchedSpectralPools;
            RenderTargetsManager.ClearDustCachesEvent += ClearSpectralDust;

            ResizeRenderTarget();
        }

        private void ClearSpectralDust()
        {
            spectralDust.Clear();
            coloredDustCount = 0;
            averageDustColor = Vector3.Zero;
        }

        private void DrawToRenderTarget()
        {
            List<int> ghostlyNautiluses = new List<int>();
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC NPC = Main.npc[i];

                if (NPC.active && NPC.hide && NPC.type == ModContent.NPCType<SirNautilus>())
                {
                    ghostlyNautiluses.Add(i);
                }
            }

            TurnOnThePuddleEffects();

            if (spectralDust.Count == 0 && ghostlyNautiluses.Count == 0)
            {
                Main.graphics.GraphicsDevice.SetRenderTargets(null);
                return;
            }

            RenderTargetsManager.NoViewMatrixPrims = true;

            if (coloredDustCount == 0)
                UsedColor = Vector3.Lerp(UsedColor, new Vector3(60, 255, 195) / 255f, 0.3f);
            else
                UsedColor = Vector3.Lerp(UsedColor, averageDustColor / ((float)coloredDustCount * 255f), 0.3f);

            if (spectralDust.Count > 0)
            {
                Effect effect = Scene["SpectralWaterShape"].GetShader().Shader;
                effect.Parameters["noiseSize"].SetValue(new Vector2(0.1f, 0.1f));

                Texture2D milkyNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "MilkyBlobNoise").Value;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect);

                foreach (int index in spectralDust)
                {
                    Dust dust = Main.dust[index];
                    float completion = Math.Clamp(1 - (dust.scale - 0.1f) / 0.9f, 0f, 1f);
                    Color dustColor = new Color(completion, (dust.dustIndex % 255) / 255f, (dust.rotation + (float)Main.time * 0.02f) % 1f);
                    Main.spriteBatch.Draw(milkyNoise, (dust.position - Main.screenPosition) / 2, null, dustColor, 0f, milkyNoise.Size() / 2f, dust.rotation / 64f, SpriteEffects.None, 0);
                }
                Main.spriteBatch.End();
            }

            if (ghostlyNautiluses.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null);

                foreach (int index in ghostlyNautiluses)
                {
                    NPC npc = Main.npc[index];
                    ModNPC modNPC = npc.ModNPC;

                    if (modNPC.PreDraw(Main.spriteBatch, Main.screenPosition, npc.GetAlpha(Color.White)))
                        Main.instance.DrawNPC(index, false);

                    modNPC.PostDraw(Main.spriteBatch, Main.screenPosition, npc.GetAlpha(Color.White));
                }

                Main.spriteBatch.End();
            }

            RenderTargetsManager.NoViewMatrixPrims = false;
            Main.graphics.GraphicsDevice.SetRenderTargets(null);
        }

        private void ResizeRenderTarget()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (spectralWaterRenderTarget != null && !spectralWaterRenderTarget.IsDisposed)
                    spectralWaterRenderTarget.Dispose();

                spectralWaterRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            });
        }

        private void DrawBatchedSpectralPools()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Effect waterEffect = Scene["SpectralWaterLayer"].GetShader().Shader;
            waterEffect.Parameters["startingColor"].SetValue(new Vector4(UsedColor, 1f));
            waterEffect.Parameters["endingColor"].SetValue(new Color(50, 210, 210).ToVector4() * 0.6f);
            waterEffect.Parameters["outlineColor"].SetValue(new Color(125, 255, 255).ToVector4());

            waterEffect.Parameters["textureSize"].SetValue(spectralWaterRenderTarget.Size());
            waterEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.06f);
            waterEffect.Parameters["noiseTex"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "ManifoldNoise").Value);

            waterEffect.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.Draw(spectralWaterRenderTarget, Vector2.Zero, null, Color.White, 0, new Vector2(0, 0), 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        public static bool TurnOnThePuddleEffects()
        {
            GraphicsDevice gD = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            if (Main.gameMenu || Main.dedServ || spriteBatch is null || spectralWaterRenderTarget is null || gD is null)
                return false;

            gD.SetRenderTarget(spectralWaterRenderTarget);
            gD.Clear(Color.Transparent);
            return true;
        }
    }


    public class SpectralWaterDustNoisy : ModDust
    {
        public override void SetStaticDefaults()
        {
            SpectralPuddleDrawingSystem.dustType = Type;
        }

        public override string Texture => AssetDirectory.Assets + "Invisible";
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.rotation = 1f;
            dust.noLightEmittence = false;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;

            if (!dust.noGravity)
            {
                dust.velocity.Y += 0.03f;
            }

            //dust.rotation = dust.velocity.ToRotation();
            dust.scale *= 0.96f;

            if (dust.noGravity)
                dust.scale *= 0.96f;

            if (dust.scale < 0.1f)
                dust.active = false;

            if (!dust.noLightEmittence)
            {
                Vector3 lightColor = 255f * Color.Turquoise.ToVector3();
                if (dust.customData != null)
                    lightColor = (Vector3)dust.customData;

                Lighting.AddLight(dust.position / 16, lightColor);
            }


            if (dust.active)
            {
                SpectralPuddleDrawingSystem.spectralDust.Add(dust.dustIndex);
                if (dust.customData != null)
                {
                    SpectralPuddleDrawingSystem.coloredDustCount++;
                    SpectralPuddleDrawingSystem.averageDustColor += (Vector3)dust.customData;
                }
            }

            return false;
        }
    }


    public class SpectralWaterDustEmbers : ModDust
    {
        public override string Texture => AssetDirectory.SirNautilus + "SpectralWaterDust";
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.noLight = true;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return dust.color;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;

            if (!dust.noGravity)
            {
                dust.velocity.Y += 0.03f;
            }

            //dust.rotation = dust.velocity.ToRotation();
            dust.scale *= 0.96f;

            if (dust.noGravity)
                dust.scale *= 0.96f;

            if (dust.scale < 0.1f)
                dust.active = false;

            if (!dust.noLightEmittence)
            {
                Lighting.AddLight(dust.position / 16, 255f * Color.Turquoise.ToVector3());
            }

            return false;
        }
    }
}