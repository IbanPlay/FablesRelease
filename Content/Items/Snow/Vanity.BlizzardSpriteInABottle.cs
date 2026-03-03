using CalamityFables.Content.NPCs.Desert;
using CalamityFables.Content.NPCs.Sky;
using CalamityFables.Content.NPCs.Snow;
using System.Diagnostics.Metrics;
using Terraria.DataStructures;
using static CalamityFables.Content.NPCs.Snow.BlizzardSprite;

namespace CalamityFables.Content.Items.Snow
{
    public class BlizzardSpriteInABottle : ModItem
    {
        public class BlizzardSpriteInABottleData : CustomGlobalData, ITemporaryRenderTargetHolder
        {
            public static Asset<Texture2D> SmallCloudTexture;
            public Player player;

            public BlizzardCloudRenderTarget cloudRT;
            public DrawActionTextureContent processedCloudRT;
            public NavierStrokeCanvas mistCanvas;

            public Vector2 cloudCanvasPosition;
            public Vector2 mistCanvasPosition;

            public float heatHaze;

            public int TicksSinceLastUsedRenderTargets { get; set; }
            public int AutoDisposeTime => 120;
            public bool RenderTargetsDisposed { get; set; } = true;

            public override void Reset()
            {

            }

            public void LoadRenderTargets()
            {
                processedCloudRT = new DrawActionTextureContent(PostProcessClouds, 600, 600, startSpritebatch: false);
                Main.ContentThatNeedsRenderTargets.Add(processedCloudRT);
                RenderTargetsManager.AddTemporaryTarget(this);
            }

            public void DisposeOfRenderTargets()
            {
                processedCloudRT?.Reset();
                Main.ContentThatNeedsRenderTargets.Remove(processedCloudRT);
                processedCloudRT.GetTarget()?.Dispose();
            }

            public void PostProcessClouds(SpriteBatch spriteBatch)
            {
                if (player == null)
                    return;

                cloudRT.DrawRenderTargetForPlayerLayer(spriteBatch, heatHaze, player.whoAmI);

                if (FablesConfig.Instance.FluidSimVFXEnabled && (mistCanvas == null || mistCanvas.Disposed))
                    InitializeMistCanvas();

                if (mistCanvas != null && mistCanvas.Canvas != null)
                {
                    mistCanvas.addedVelocity = Vector2.UnitX * Main.WindForVisuals * 0.1f;
                    Effect blizzMistEffect = Scene["BlizzardMist"].GetShader().Shader;
                    blizzMistEffect.Parameters["textureResolution"].SetValue(mistCanvas.Size * 2f);
                    blizzMistEffect.Parameters["worldPos"].SetValue(mistCanvasPosition * 0.5f);
                    Vector4 baseColor = new Vector4(0.9f, 0.97f, 1.2f, 0.9f);
                    float opacity = 0.6f;
                    if (heatHaze > 0f)
                    {
                        opacity = MathHelper.Lerp(opacity, 0.3f, heatHaze);
                        baseColor = Vector4.Lerp(baseColor, new Vector4(0.7f, 0.46f, 0.1f, 0.2f), heatHaze);
                    }

                    blizzMistEffect.Parameters["baseColor"].SetValue(baseColor);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, blizzMistEffect, Matrix.Identity);
                    Main.spriteBatch.Draw(mistCanvas.Canvas, Vector2.Zero, null, Color.White * opacity, 0f, Vector2.Zero, processedCloudRT.size.ToVector2() / mistCanvas.Size, 0, 0f);
                    Main.spriteBatch.End();
                }
            }

            public void InitializeMistCanvas()
            {
                mistCanvas = new NavierStrokeCanvas(new Point(150, 150), new Point(30, 30))
                {
                    displacementMultiplier = 0.25f,
                    densityDiffusion = 0.02f,
                    velocityDiffusion = 0.02f,
                    densityDissipation = 0.94f, //98
                    velocityDissipation = 0.99f,
                    projectionIterations = 25,
                    advectionStrength = 0.9f
                };
                mistCanvas.DrawDirectionalSourcesEvent += DrawMistSources;
                mistCanvas.DrawOmnidirectionalSourcesEvent += DrawOmniMistSources;
            }

            private void DrawMistSources(NavierStrokeCanvas canvas)
            {
                Vector2 center = mistCanvas.Size / 2f - Vector2.UnitY * 4.5f;
                Vector2 baseVel = Vector2.Zero;
                canvas.DrawOnCanvas(0.2f, baseVel + Vector2.UnitY.RotatedBy(Main.GlobalTimeWrappedHourly) * 0.1f, center, 13, true, 0f, Vector2.One * 0.5f);
                canvas.DrawOnCanvas(0.1f, baseVel - Vector2.UnitY.RotatedBy(Main.GlobalTimeWrappedHourly) * 0.056f, center, 11, true, 0f, Vector2.One * 0.5f);
                canvas.DrawOnCanvas(0.2f, baseVel, center + Vector2.UnitY * 1.5f, 13, false, 0f, Vector2.One * 0.5f);

                if (Main.rand.NextBool(9))
                {
                    float rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 position = center + Vector2.UnitY.RotatedBy(rotation) * Main.rand.NextFloat(1.5f, 7f);
                    Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 2f);
                    canvas.DrawOnCanvas(1f, velocity * 0.5f, position, 6, true, 0f, Vector2.One * 0.5f);
                }
            }
            private void DrawOmniMistSources(NavierStrokeCanvas canvas)
            {
                Vector2 center = mistCanvas.Size / 2f - Vector2.UnitY * 4f;
                canvas.DrawOnCanvas(0.2f, 0.05f, 0.2f, center, 4f);
            }
        }

        public override string Texture => AssetDirectory.SnowItems + Name;

        public override void Load()
        {
            FablesPlayer.HideDrawLayersEvent += Decapitate;
        }

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.BlizzardinaBottle;
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
            Item.vanity = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                DoVisuals(player);
        }
        public override void UpdateVanity(Player player)
        {
            DoVisuals(player);
        }

        public override void UpdateItemDye(Player player, int dye, bool hideVisual)
        {
            if (!hideVisual && dye != 0)
                player.cHead = dye;
        }

        public void DoVisuals(Player player)
        {
            player.SetPlayerFlag(Name + "Vanity");
            player.SetCustomHurtSound(BlizzardSprite.HurtSound, 10, 0.9f);

            if (!player.GetPlayerData(out BlizzardSpriteInABottleData bottleData))
            {
                bottleData = new BlizzardSpriteInABottleData();
                bottleData.player = player;
                player.SetPlayerData(bottleData);
            }

            bottleData.mistCanvas?.KeepCanvasActive();
            bottleData.TicksSinceLastUsedRenderTargets = 0;

            if (!Main.dedServ)
                UpdateAndSpawnClouds(player, bottleData);

            bottleData.cloudCanvasPosition = player.MountedCenter;
            bottleData.cloudCanvasPosition.X = (int)(bottleData.cloudCanvasPosition.X / 2) * 2;
            bottleData.cloudCanvasPosition.Y = (int)(bottleData.cloudCanvasPosition.Y / 2) * 2;

            if (bottleData.mistCanvas != null)
            {
                Vector2 mistCanvasPosition = player.MountedCenter;
                mistCanvasPosition.X = (int)(mistCanvasPosition.X / 4) * 4;
                mistCanvasPosition.Y = (int)(mistCanvasPosition.Y / 4) * 4;
                bottleData.mistCanvas.position = mistCanvasPosition;
            }
            if (bottleData.cloudRT != null)
                bottleData.cloudRT.Position = bottleData.cloudCanvasPosition;

            bool heatHazing = false;
            if (CalamityFables.SpiritEnabled && CalamityFables.SpiritReforged.TryFind("SaltBiome", out ModBiome saltFlats))
                heatHazing = player.InModBiome(saltFlats);

            if (heatHazing && bottleData.heatHaze < 1)
            {
                bottleData.heatHaze += 1 / 40f;
                if (bottleData.heatHaze > 1)
                    bottleData.heatHaze = 1f;
            }
            if (!heatHazing && bottleData.heatHaze > 0)
            {
                bottleData.heatHaze -= 1 / 40f;
                if (bottleData.heatHaze < 0)
                    bottleData.heatHaze = 0f;
            }
        }

        public void UpdateAndSpawnClouds(Player player, BlizzardSpriteInABottleData bottleData)
        {
            if (bottleData.cloudRT == null)
            {
                bottleData.cloudRT = new BlizzardCloudRenderTarget(300);
                bottleData.cloudRT.PreUpdateParticlesEvent += (RTParticle particle) => {
                    ((BlizzardCloudParticle)particle).impartedRotation = player.direction * -0.02f;

                    Vector2 drawPosition = (particle.Position - bottleData.cloudCanvasPosition) / 2f + new Vector2(150f, 150f);
                    if (drawPosition.X < 0 || drawPosition.X > 300 || drawPosition.Y < 0 || drawPosition.Y > 300)
                        particle.Kill();
                };
            }

            int dustAmount = Main.rand.NextBool(2) ? 4 : 3;
            for (int i = 0; i < dustAmount; i++)
            {
                Vector2 cloudVelocity = Main.rand.NextVector2Circular(1f, 1f) - Vector2.UnitY * 2f;
                cloudVelocity *= Main.rand.NextFloat(0.1f, 0.3f);

                cloudVelocity.Y *= player.gravDir;

                Vector2 cloudPosition = player.MountedCenter - Vector2.UnitY * 15f * player.gravDir;
                cloudPosition += Main.rand.NextVector2Circular(6f, 10);

                BlizzardCloudParticle newCloud = new BlizzardCloudParticle(cloudPosition, cloudVelocity + player.velocity * 0.3f);
                bottleData.cloudRT.SpawnParticle(newCloud);
            }
        }

        private void Decapitate(Player player, PlayerDrawSet drawInfo)
        {
            if (player.GetPlayerFlag(Name + "Vanity") && !drawInfo.headOnlyRender)
                PlayerDrawLayers.Head.Hide();
        }
    }
}
