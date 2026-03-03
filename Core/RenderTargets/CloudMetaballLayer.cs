using CalamityFables.Content.NPCs.Sky;
using CalamityFables.Content.Items.Sky;
using Terraria.DataStructures;

namespace CalamityFables.Core
{
    public class CloudMetaballLayer : ModSystem
    {
        public static int cloudSpriteType;

        /// <summary>
        /// The render target that contains the RGB orbs making up the cloud sprite clouds
        /// </summary>
        public static RenderTarget2D cloudSpriteRenderTarget;
        /// <summary>
        /// The fullscreen target that contains the cloud sprite clouds with their final coloring and distortion
        /// </summary>
        public static RenderTarget2D postProcessedCloudSpriteRenderTarget;
        /// <summary>
        /// The fullscreen target that contains the cloud sprite clouds outline with their final coloring and distortion
        /// </summary>
        public static RenderTarget2D postProcessedCloudSpriteOutlineRenderTarget;
        /// <summary>
        /// The render target that contains the RGB orbs making up the player's cloud sprite clouds
        /// </summary>
        public static RenderTarget2D playerCloudsRenderTarget;
        /// <summary>
        /// The fullscreen target that contains the player's cloud sprite clouds with their final coloring and distortion
        /// </summary>
        public static RenderTarget2D postProcessedPlayerCloudsRenderTarget;
        /// <summary>
        /// The fullscreen target that contains the player's cloud sprite clouds outline with their final coloring and distortion
        /// </summary>
        public static RenderTarget2D postProcessedPlayerCloudsOutlineRenderTarget;

        public static List<int> NPCDustToDraw = new List<int>();
        public static List<int> playerDustToDraw = new List<int>();
        public static Dictionary<int, DrawData> playerDrawDataCache = new Dictionary<int, DrawData>();

        public static List<int> sunsToDraw = new List<int>();
        public static Asset<Texture2D> DustAsset;
        public static Asset<Texture2D> SunAsset;
        public static Asset<Texture2D> CloudNoiseAsset;
        public static Asset<Texture2D> DisplacementMapAsset;

        public static bool drewNPCDusts = false;
        public static bool drewFriendlyDusts = false;

        public int framesSinceLastDrawnCloudSpriteDust;
        public int framesSinceLastDrawnPlayerDust;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            FablesDrawLayers.DrawThingsAboveSolidTilesEvent += DrawCloudSpriteSunAndTarget;
            FablesDrawLayers.DrawThingsAbovePlayersEvent += DrawFriendlyCloudsAndEyes;

            FablesDrawLayers.PreDrawPlayersEvent += ClearDrawInfoCache;
            FablesDrawLayers.ModifyDrawLayersAfterTransformsEvent += InterceptDrawInfoIntoCache;

            RenderTargetsManager.ClearDustCachesEvent += ClearCloudDustCache;
            RenderTargetsManager.SortDustCachesEvent += SortCloudDustCache;
            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTargets;
            RenderTargetsManager.DrawToRenderTargetsEvent += DrawToRenderTargets;
            ResizeRenderTargets();
        }


        private void ClearDrawInfoCache(bool afterProjectiles)
        {
            if (!afterProjectiles)
                playerDrawDataCache.Clear();
        }

        public static int CloudSpriteEyeDrawIndex;
        private void InterceptDrawInfoIntoCache(ref PlayerDrawSet drawinfo)
        {
            if (CloudSpriteEyeDrawIndex > 0)
            {
                playerDrawDataCache[drawinfo.drawPlayer.whoAmI] = drawinfo.DrawDataCache[CloudSpriteEyeDrawIndex];
                CloudSpriteEyeDrawIndex = -1;
            }
        }

        private void ClearCloudDustCache()
        {
            NPCDustToDraw.Clear();
            playerDustToDraw.Clear();
        }

        private void SortCloudDustCache()
        {
            NPCDustToDraw.OrderBy(i => Main.dust[i].position.X - Main.screenPosition.X + (Main.dust[i].position.Y - Main.screenPosition.Y) * Main.screenWidth);
            playerDustToDraw.OrderBy(i => Main.dust[i].position.X - Main.screenPosition.X + (Main.dust[i].position.Y - Main.screenPosition.Y) * Main.screenWidth);
        }

        private void DrawToRenderTargets()
        {
            if (Main.gameMenu || Main.dedServ || Main.spriteBatch is null || 
                cloudSpriteRenderTarget is null || 
                playerCloudsRenderTarget is null || 
                postProcessedCloudSpriteRenderTarget is null ||
                postProcessedPlayerCloudsRenderTarget is null ||
                postProcessedCloudSpriteOutlineRenderTarget is null ||
                postProcessedPlayerCloudsOutlineRenderTarget is null ||
                Main.graphics.GraphicsDevice is null)
                return;

            sunsToDraw.Clear();
            bool playerWithHead = false;

            //Find all cloud sprites so we can keep track of their suns to draw em later
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC NPC = Main.npc[i];
                if (NPC.active && NPC.type == cloudSpriteType)
                    sunsToDraw.Add(NPC.whoAmI);
            }

            //Similarly, find all players with the cloud sprite in a bottle equipped so we can keep track of both who they are, and all the suns that have to get drawn behind em
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                bool inRange = i == Main.myPlayer || !player.outOfRange;

                if (player.active && !player.dead && !player.ShouldNotDraw && inRange && player.GetPlayerFlag("CloudSpriteInABottleVanity"))
                {
                    sunsToDraw.Add(-player.whoAmI - 1);
                    playerWithHead = true;
                }
            }

            //Keep track of if there were any dusts drawn for players and npcs
            drewNPCDusts = false;
            drewFriendlyDusts = false;

            if (NPCDustToDraw.Count > 0)
            {
                InitializeDisposedTargets(ref cloudSpriteRenderTarget, ref postProcessedCloudSpriteRenderTarget, ref postProcessedCloudSpriteOutlineRenderTarget);

                drewNPCDusts = true;
                framesSinceLastDrawnCloudSpriteDust = 0;
                DrawDustsOnLayer(NPCDustToDraw, cloudSpriteRenderTarget);
                DrawCloudTargetWithEffects(cloudSpriteRenderTarget, postProcessedCloudSpriteRenderTarget, postProcessedCloudSpriteOutlineRenderTarget);
            }
            else
            {
                if (framesSinceLastDrawnCloudSpriteDust < 600)
                    framesSinceLastDrawnCloudSpriteDust++;
                else if (
                    !cloudSpriteRenderTarget.IsDisposed ||
                    !postProcessedCloudSpriteRenderTarget.IsDisposed ||
                    !postProcessedCloudSpriteOutlineRenderTarget.IsDisposed 
                    )
                {
                    cloudSpriteRenderTarget?.Dispose();
                    postProcessedCloudSpriteRenderTarget?.Dispose();
                    postProcessedCloudSpriteOutlineRenderTarget?.Dispose();
                }
            }


            if (playerDustToDraw.Count > 0 || playerWithHead)
            {
                InitializeDisposedTargets(ref playerCloudsRenderTarget, ref postProcessedPlayerCloudsRenderTarget, ref postProcessedPlayerCloudsOutlineRenderTarget);

                drewFriendlyDusts = true;
                framesSinceLastDrawnPlayerDust = 0;
                DrawDustsOnLayer(playerDustToDraw, playerCloudsRenderTarget, true);
                DrawCloudTargetWithEffects(playerCloudsRenderTarget, postProcessedPlayerCloudsRenderTarget, postProcessedPlayerCloudsOutlineRenderTarget);
            }
            else
            {
                if (framesSinceLastDrawnPlayerDust < 600)
                    framesSinceLastDrawnPlayerDust++;
                else if (
                    !playerCloudsRenderTarget.IsDisposed ||
                    !postProcessedPlayerCloudsRenderTarget.IsDisposed ||
                    !postProcessedPlayerCloudsOutlineRenderTarget.IsDisposed
                    )
                {
                    playerCloudsRenderTarget?.Dispose();
                    postProcessedPlayerCloudsRenderTarget?.Dispose();
                    postProcessedPlayerCloudsOutlineRenderTarget?.Dispose();
                }
            }

            RenderTargetsManager.NoViewMatrixPrims = false;
            Main.graphics.GraphicsDevice.SetRenderTargets(null);
        }
        
        /// <summary>
        /// Draws all the cached dust RGB balls on the given rendertarget
        /// </summary>
        /// <param name="dustsToDraw">Dust indices to draw on the target</param>
        /// <param name="layer">Target to draw the dust on</param>
        /// <param name="drawHeadCoverExtra">Used for players, to make the dust render</param>
        public static void DrawDustsOnLayer(List<int> dustsToDraw, RenderTarget2D layer, bool drawHeadCoverExtra = false)
        {
            DustAsset = DustAsset ?? ModContent.Request<Texture2D>(AssetDirectory.SkyNPCs + "CloudSprite_Dust");
            Texture2D cloudTex = DustAsset.Value;
            Vector2 origin = cloudTex.Size() / 2f;

            RenderTargetsManager.NoViewMatrixPrims = true;
            Main.graphics.GraphicsDevice.SetRenderTarget(layer);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null);

            //Draws covers below the head so that theyre always at least covered a bit
            if (drawHeadCoverExtra)
            {
                foreach (int playerIndex in sunsToDraw)
                {
                    if (playerIndex >= 0)
                        continue;
                    Player player = Main.player[(playerIndex + 1) * -1];
                    Vector2 coverPosition = -(Vector2.UnitY * player.gravDir * 13f);
                    player.sitting.GetSittingOffsetInfo(player, out Vector2 posOffset, out float seatYOffset);
                    coverPosition.Y += seatYOffset;

                    if (player.direction == -1)
                        coverPosition.X -= 4;

                    Main.spriteBatch.Draw(cloudTex, (player.MountedCenter + coverPosition.RotatedBy(player.fullRotation) - Main.screenPosition) / 2, null, Color.White, 0f, origin, 0.45f, SpriteEffects.None, 0);
                }
            }

            //Draw all the dusts
            foreach (int index in dustsToDraw)
            {
                Dust dust = Main.dust[index];
                Main.spriteBatch.Draw(cloudTex, (dust.position - Main.screenPosition) / 2, null, Color.White, 0f, origin, dust.scale * 0.45f, SpriteEffects.None, 0);
            }

            //Draw the bloom overlay for all the suns
            DrawSunBloomMetaballoverlay();
            Main.spriteBatch.End();
        }

        /// <summary>
        /// Draws bloom circles where cloud sprite/player vanity suns are located, so that the cloud shader can use it to illuminate through the clouds
        /// </summary>
        public static void DrawSunBloomMetaballoverlay()
        {
            if (sunsToDraw.Count == 0)
                return;

            Texture2D sunBloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Vector2 origin = sunBloom.Size() / 2f;

            foreach (int index in sunsToDraw)
            {
                Vector2 sunPos = Vector2.Zero;
                float sunScale = 1f;

                //Index above 0 means its a NPC index, for cloud sprites
                if (index >= 0 && Main.npc[index].ModNPC is CloudSprite sprite)
                {
                    sunPos = sprite.SunPosition;
                    sunScale = Main.npc[index].scale;
                }
                //Index below zero means its a player index, for cloud sprites in a bottle
                else if (index < 0)
                {
                    Player player = Main.player[(index + 1) * -1];
                    sunPos = player.MountedCenter - (Vector2.UnitY * player.gravDir * 16f + Vector2.UnitX * player.direction * 12f).RotatedBy(player.bodyRotation);
                    sunScale = 0.8f;
                }
                Main.spriteBatch.Draw(sunBloom, (sunPos - Main.screenPosition) / 2, null, Color.Red with { A = 0 }, 0f, origin, sunScale * 0.2f, SpriteEffects.None, 0);
            }
        }

        public static void GetCloudPalette(out Vector3 cloudColorBright, out Vector3 cloudColorDark, out Vector3 skyColorMult, out Vector3 shadowColorMult, out Vector3 outlineColor)
        {
            cloudColorBright = new Color(213, 234, 231).ToVector3();
            cloudColorDark = new Color(163, 180, 191).ToVector3();

            //Skycolor is a global tint, shadowcolor is for the underside of the clouds
            skyColorMult = FablesGeneralSystemHooks.AtmospherelessColorOfTheSkies.ToVector3();
            shadowColorMult = new Color(138, 177, 198).ToVector3();
            float cloudBrightness = skyColorMult.Length();

            //In dark settings, change the shadows on the clouds to a glowing underglow
            if (cloudBrightness < 0.35f)
            {
                Vector3 underlight = new Vector3(8.5f, 3.4f, 1.8f);
                if (Main.LocalPlayer.ZoneGlowshroom)
                    underlight = new Vector3(4.5f, 4.4f, 15.8f);

                shadowColorMult = Vector3.Lerp(shadowColorMult, underlight, 1 - cloudBrightness / 0.35f);

                skyColorMult *= 1 + (1 - cloudBrightness / 0.35f);
                if (cloudBrightness == 0)
                    skyColorMult = new Vector3(0.06f, 0.06f, 0.2f);
            }

            outlineColor = GetCloudOutlineColorMultiplier();
        }

        public static Vector3 GetCloudOutlineColorMultiplier() =>  new Vector3(0.4f, 0.6f, 0.8f);

        /// <summary>
        /// Renders the suns for NPC cloud sprites, alongside the cloudy dust on their layer
        /// </summary>
        private void DrawCloudSpriteSunAndTarget()
        {
            if (cloudSpriteRenderTarget == null)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Draw all the suns behind the clouds
            foreach (int index in sunsToDraw)
            {
                //Stop if the indices start getting negative (those are player suns and draw separately)
                if (index < 0)
                    break;

                NPC cloudSpr = Main.npc[index];
                if (cloudSpr.ModNPC != null && cloudSpr.ModNPC is CloudSprite sprite)
                    CloudSprite.DrawCloudSpriteSun(Main.screenPosition, sprite.SunPosition, cloudSpr.scale);
            }

            Main.spriteBatch.End();
            if (!drewNPCDusts)
                return;

            //Draw the cloudy dust ontop
            DrawCloudsWithOutline(postProcessedCloudSpriteRenderTarget, postProcessedCloudSpriteOutlineRenderTarget);
        }

        /// <summary>
        /// Renders the cloud dust from player vanity, alongside the eyes above the player's faces 
        /// </summary>
        /// <param name="aboveProjectiles"></param>
        private void DrawFriendlyCloudsAndEyes(bool aboveProjectiles)
        {
            if (playerCloudsRenderTarget == null || !aboveProjectiles)
                return;

            if (drewFriendlyDusts)
                DrawCloudsWithOutline(postProcessedPlayerCloudsRenderTarget, postProcessedPlayerCloudsOutlineRenderTarget);

            bool spriteBatchRestarted = false;

            foreach (int index in sunsToDraw)
            {
                //Positive indices mean its NPC indexes
                if (index >= 0 || !playerDrawDataCache.TryGetValue(-(index+ 1), out DrawData drawData))
                    continue;

                if (!spriteBatchRestarted)
                {
                    spriteBatchRestarted = true;
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                }

                Main.EntitySpriteDraw(drawData);
            }

            if (spriteBatchRestarted)
                Main.spriteBatch.End();
        }

        /// <summary>
        /// Renders the post processed target 5x with an offset to create an outline around the clouds
        /// </summary>
        /// <param name="postProcessedTarget"></param>
        public static void DrawCloudsWithOutline(Texture2D postProcessedTarget, Texture2D postProcessedOutlineTarget)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Vector2.One;
                if (i % 2 == 0)
                    offset.X = 0;
                else
                    offset.Y = 0;
                if (i < 2)
                    offset *= -1;

                Main.spriteBatch.Draw(postProcessedOutlineTarget, offset * 2f, null, Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.Draw(postProcessedTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// Renders the cloud target with the cloud shader onto another target that holds the post processed result
        /// </summary>
        /// <param name="cloudTarget"></param>
        /// <param name="postProcessedTarget"></param>
        public static void DrawCloudTargetWithEffects(Texture2D cloudTarget, RenderTarget2D postProcessedTarget, RenderTarget2D postProcessedOutlineTarget)
        {
            Effect metaBawlsEffect = Scene["CloudMetaball"].GetShader().Shader;
            CloudNoiseAsset = CloudNoiseAsset ?? ModContent.Request<Texture2D>(AssetDirectory.Noise + "GradientNoise");
            DisplacementMapAsset = DisplacementMapAsset ?? ModContent.Request<Texture2D>(AssetDirectory.Noise + "ManifoldDisplaceNoise");
            Texture2D noise = CloudNoiseAsset.Value;
            Texture2D displace = DisplacementMapAsset.Value;

            GetCloudPalette(out Vector3 cloudColorBright,
                out Vector3 cloudColorDark,
                out Vector3 skyColorMult,
                out Vector3 shadowColorMult,
                out Vector3 outlineColor);

            Vector2 noiseScalar = new Vector2(4) / cloudTarget.Size();
            Vector2 baseNoiseOffset = noiseScalar * Main.screenPosition * 0.5f;
            baseNoiseOffset.X %= 1;
            baseNoiseOffset.Y %= 1;
            noiseScalar *= noise.Size();

            //Color palette
            metaBawlsEffect.Parameters["cloudColor1"].SetValue(new Vector4(cloudColorBright * skyColorMult, 1f));
            metaBawlsEffect.Parameters["cloudColor2"].SetValue(new Vector4(cloudColorDark * skyColorMult, 1f));
            metaBawlsEffect.Parameters["shadowsColorMultiply"].SetValue(shadowColorMult);
            metaBawlsEffect.Parameters["sunGlowColor"].SetValue(new Vector3(0.7f, 0.3f, 0f));
            metaBawlsEffect.Parameters["illuminatedOutlineColorMult"].SetValue(new Vector3(0.7f, 0.5f, 0.4f));

            metaBawlsEffect.Parameters["textureSize"].SetValue(cloudTarget.Size());
            metaBawlsEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.06f);

            metaBawlsEffect.Parameters["noiseTex"].SetValue(noise);
            metaBawlsEffect.Parameters["displacementMap"].SetValue(displace);
            //Ratios by which the time is multiplied for the displace matrix
            metaBawlsEffect.Parameters["displaceFactors"].SetValue(new Vector4(2f, 0.3f, -1.5f, 0.72f));
            metaBawlsEffect.Parameters["displaceMapStrenght"].SetValue(new Vector2(0.0075f) / Main.GameViewMatrix.Zoom);

            //Precomputed values to avoid doing it for every pixel
            metaBawlsEffect.Parameters["noiseBaseDisplacement"].SetValue(baseNoiseOffset);
            metaBawlsEffect.Parameters["noiseScalar"].SetValue(noiseScalar);
            metaBawlsEffect.Parameters["screenRatio"].SetValue(cloudTarget.Width / (float)cloudTarget.Height);

            Main.graphics.GraphicsDevice.SetRenderTarget(postProcessedTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);

            metaBawlsEffect.Parameters["outlineColorMult"].SetValue(new Vector3(1f, 1f, 1f));
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, metaBawlsEffect, Matrix.Identity);
            Main.spriteBatch.Draw(cloudTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            Main.spriteBatch.End();

            Main.graphics.GraphicsDevice.SetRenderTargets(postProcessedOutlineTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);

            metaBawlsEffect.Parameters["outlineColorMult"].SetValue(outlineColor);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, metaBawlsEffect, Matrix.Identity);
            Main.spriteBatch.Draw(cloudTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            Main.spriteBatch.End();

            Main.graphics.GraphicsDevice.SetRenderTargets(null);
        }

        private void InitializeDisposedTargets(ref RenderTarget2D mainRt, ref RenderTarget2D postProcessedTarget, ref RenderTarget2D postProcessedOutlineTarget)
        {
            if (mainRt == null || mainRt.IsDisposed)
                mainRt = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            if (postProcessedTarget == null || postProcessedTarget.IsDisposed)
                postProcessedTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            if (postProcessedOutlineTarget == null || postProcessedOutlineTarget.IsDisposed)
                postProcessedOutlineTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
        }

        private void ResizeRenderTargets()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (cloudSpriteRenderTarget != null && !cloudSpriteRenderTarget.IsDisposed)
                    cloudSpriteRenderTarget.Dispose();
                cloudSpriteRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);

                if (postProcessedCloudSpriteRenderTarget != null && !postProcessedCloudSpriteRenderTarget.IsDisposed)
                    postProcessedCloudSpriteRenderTarget.Dispose();
                postProcessedCloudSpriteRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);

                if (postProcessedCloudSpriteOutlineRenderTarget != null && !postProcessedCloudSpriteOutlineRenderTarget.IsDisposed)
                    postProcessedCloudSpriteOutlineRenderTarget.Dispose();
                postProcessedCloudSpriteOutlineRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);



                if (playerCloudsRenderTarget != null && !playerCloudsRenderTarget.IsDisposed)
                    playerCloudsRenderTarget.Dispose();
                playerCloudsRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);

                if (postProcessedPlayerCloudsRenderTarget != null && !postProcessedPlayerCloudsRenderTarget.IsDisposed)
                    postProcessedPlayerCloudsRenderTarget.Dispose();
                postProcessedPlayerCloudsRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);

                if (postProcessedPlayerCloudsOutlineRenderTarget != null && !postProcessedPlayerCloudsOutlineRenderTarget.IsDisposed)
                    postProcessedPlayerCloudsOutlineRenderTarget.Dispose();
                postProcessedPlayerCloudsOutlineRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            });
        }
    }



    public class CloudMetaball : ModDust
    {
        public override void SetStaticDefaults()
        {
            CloudSprite.cloudDustType = Type;
        }

        public override string Texture => AssetDirectory.Invisible;

        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.noLight = true;
        }

        public override bool Update(Dust dust)
        {
            dust.scale *= 0.98f;
            if (dust.scale < 0.6f)
                dust.scale *= 0.96f;

            dust.rotation += 0.06f;
            dust.position += dust.velocity;

            dust.velocity.Y -= 0.02f;
            dust.velocity.X += Main.WindForVisuals * 0.034f;

            dust.color.A = 255;
            if (dust.scale < 0.26f)
                dust.active = false;

            if (dust.active)
                CloudMetaballLayer.NPCDustToDraw.Add(dust.dustIndex);

            return false;
        }
    }

    public class CloudMetaballFast : ModDust
    {
        public override void SetStaticDefaults()
        {
            CloudSprite.fastCloudDustType = Type;
        }

        public override string Texture => AssetDirectory.Invisible;

        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.noLight = true;
            dust.alpha = 30;
        }

        public override bool Update(Dust dust)
        {
            dust.scale *= 0.97f;

            dust.rotation += 0.06f;
            dust.position += dust.velocity;

            dust.velocity.Y *= 0.97f;
            dust.velocity.X += Main.WindForVisuals * 0.014f;

            if (dust.alpha < 10)
                dust.scale *= 0.97f;

            dust.alpha--;
            if (dust.alpha <= 0)
                dust.active = false;

            if (dust.active)
                CloudMetaballLayer.NPCDustToDraw.Add(dust.dustIndex);

            return false;
        }
    }

    public class CloudMetaballPlayer : CloudMetaball
    {
        public override void SetStaticDefaults()
        {
            CloudSpriteInABottle.dustType = Type;
        }

        public override bool Update(Dust dust)
        {
            dust.scale *= 0.98f;
            if (dust.scale < 0.6f)
                dust.scale *= 0.96f;

            dust.rotation += 0.06f;
            dust.position += dust.velocity;

            dust.velocity.Y -= 0.02f;
            dust.velocity.X += Main.WindForVisuals * 0.034f;

            dust.color.A = 255;
            if (dust.scale < 0.26f)
                dust.active = false;

            if (dust.active)
                CloudMetaballLayer.playerDustToDraw.Add(dust.dustIndex);

            return false;
        }
    }

}