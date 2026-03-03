using CalamityFables.Content.NPCs.Desert;
using CalamityFables.Content.NPCs.Sky;
using System.Diagnostics.Metrics;
using Terraria.DataStructures;
using static CalamityFables.Content.NPCs.Desert.SandstormSprite;

namespace CalamityFables.Content.Items.BurntDesert
{
    public class SandstormSpriteInABottle : ModItem
    {
        public class SandstormSpriteInABottleData : CustomGlobalData
        {
            public static Asset<Texture2D> SmallCloudTexture;

            public float eyeRotation = 0;
            public float eyeRotationDirection = 0;
            public Player player;

            public List<SandSpriteCloud> clouds = new List<SandSpriteCloud>();
            public MergeBlendTextureContent frontCloudRT;
            public MergeBlendTextureContent backCloudRT;

            public int lastInUseTimer = 0;
            public bool loadedRenderTargets = false;

            public override void Reset()
            {
                lastInUseTimer++;
                if (loadedRenderTargets && lastInUseTimer > 120)
                    UnloadRenderTargets();
            }

            public void LoadRenderTargets()
            {
                loadedRenderTargets = true;
                frontCloudRT = new MergeBlendTextureContent(DrawCloudsFront, 600, 600);
                Main.ContentThatNeedsRenderTargets.Add(frontCloudRT);
                backCloudRT = new MergeBlendTextureContent(DrawCloudsBack, 600, 600);
                Main.ContentThatNeedsRenderTargets.Add(backCloudRT);
            }

            public void UnloadRenderTargets()
            {
                loadedRenderTargets = false;
                frontCloudRT?.Reset();
                Main.ContentThatNeedsRenderTargets.Remove(frontCloudRT);
                frontCloudRT.GetTarget()?.Dispose();
                backCloudRT?.Reset();
                Main.ContentThatNeedsRenderTargets.Remove(backCloudRT);
                backCloudRT.GetTarget()?.Dispose();
            }

            public void DrawCloudsFront(SpriteBatch spriteBatch, bool backgroundPass) => DrawClouds(spriteBatch, backgroundPass, true);
            public void DrawCloudsBack(SpriteBatch spriteBatch, bool backgroundPass) => DrawClouds(spriteBatch, backgroundPass, false);

            public void DrawClouds(SpriteBatch spriteBatch, bool backgroundPass, bool frontClouds)
            {
                if (player == null)
                    return;

                SmallCloudTexture ??= ModContent.Request<Texture2D>(AssetDirectory.DesertItems + "SandstormSpriteBottleClouds");
                Texture2D tex = SmallCloudTexture.Value;
                Vector2 textureOrigin = new Vector2(300f, 300f);
                Color color = backgroundPass ? Color.Black : frontClouds ? Color.White : CloudBackColor;

                for (int i = clouds.Count - 1; i >= 0; i--)
                {
                    SandSpriteCloud cloud = clouds[i];
                    if (frontClouds && cloud.radialPosition.X >= 1f)
                        continue;
                    else if (!frontClouds && cloud.radialPosition.X < 1)
                        continue;

                    Rectangle frame = tex.Frame(7, 3 * 4, cloud.frame, cloud.variant);
                    Vector2 offset = cloud.radialPosition;

                    float ringWidth = 0.3f + 0.5f * MathF.Pow(Utils.GetLerpValue(10f, -15f, offset.Y, true), 0.6f);

                    ringWidth -= Utils.GetLerpValue(-10f, -50f, offset.Y, true) * 0.5f;

                    offset.X -= frontClouds ? 0.5f : 1.5f;
                    offset.X *= 23f * ringWidth;
                    //Back clouds go in the opposite direction to make them appear as if they are rotating backwards
                    if (!frontClouds)
                        offset.X *= -1;

                    Color usedColor = color;
                    //Front clouds turn towards darkness at the edges
                    if (!backgroundPass && frontClouds)
                        usedColor = Color.Lerp(color, CloudBackColor, Utils.GetLerpValue(0.2f, 0f, cloud.radialPosition.X, true) + Utils.GetLerpValue(0.8f, 1f, cloud.radialPosition.X, true));

                    offset += cloud.worldPosition - player.Center;

                    float cloudSize = 1f;
                    //Shrink particles when next to the border of the area
                    cloudSize *= Utils.GetLerpValue(300f, 280f, Math.Abs(offset.X), true);
                    cloudSize *= Utils.GetLerpValue(300f, 280f, Math.Abs(offset.Y), true);

                    FablesUtils.GetBiomeInfluences(out float corroInfluence, out float crimInfluence, out float hallowInfluence);
                    float baseInfluence = MathF.Pow(Math.Max(0f, 1 - hallowInfluence - corroInfluence - crimInfluence), 0.1f);
                    if (backgroundPass)
                        baseInfluence = 1f;

                    spriteBatch.Draw(tex, textureOrigin + offset, frame, usedColor * baseInfluence, cloud.rotation, frame.Size() / 2f, cloudSize, 0, 0);


                    if (backgroundPass)
                        continue;

                    if (hallowInfluence > 0f)
                    {
                        Rectangle hallowFrame = frame;
                        hallowFrame.Y += 102;
                        spriteBatch.Draw(tex, textureOrigin + offset, hallowFrame, usedColor * hallowInfluence, cloud.rotation, frame.Size() / 2f, cloudSize, 0, 0);
                    }
                    if (corroInfluence > 0f)
                    {
                        Rectangle corroFrame = frame;
                        corroFrame.Y += 102 * 2;
                        spriteBatch.Draw(tex, textureOrigin + offset, corroFrame, usedColor * corroInfluence, cloud.rotation, frame.Size() / 2f, cloudSize, 0, 0);
                    }
                    if (crimInfluence > 0f)
                    {
                        Rectangle crimFrame = frame;
                        crimFrame.Y += 102 * 3;
                        spriteBatch.Draw(tex, textureOrigin + offset, crimFrame, usedColor * crimInfluence, cloud.rotation, frame.Size() / 2f, cloudSize, 0, 0);
                    }
                }
            }
        }

        public override string Texture => AssetDirectory.DesertItems + Name;

        public override void Load()
        {
            FablesPlayer.HideDrawLayersEvent += Decapitate;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.SandstorminaBottle;
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
            player.SetCustomHurtSound(SandstormSprite.HitSound, 10, 0.9f);

            if (!player.GetPlayerData(out SandstormSpriteInABottleData bottleData))
            {
                bottleData = new SandstormSpriteInABottleData();
                bottleData.player = player;
                player.SetPlayerData(bottleData);
            }

            bottleData.lastInUseTimer = 0;

            bottleData.eyeRotationDirection = MathHelper.Lerp(bottleData.eyeRotationDirection, -player.direction, 0.02f);
            if (Math.Abs(bottleData.eyeRotationDirection + player.direction) < 0.05f)
                bottleData.eyeRotationDirection = -player.direction;

            //Spin faster when hit
            float spinValue = (0.2f + player.GetModPlayer<FablesPlayer>().JustHurtTimer * 0.4f);
            bottleData.eyeRotation += bottleData.eyeRotationDirection * spinValue;

            UpdateAndSpawnClouds(player, bottleData);
        }


        public void UpdateAndSpawnClouds(Player player, SandstormSpriteInABottleData bottleData)
        {
            List<SandSpriteCloud> clouds = bottleData.clouds;

            int dustAmount = Main.rand.NextBool(2) ? 2 : 1;
            for (int i = 0; i < dustAmount; i++)
            {
                SandSpriteCloud newCloud = new SandSpriteCloud(player.MountedCenter - Vector2.UnitY * 10f * player.gravDir, player.velocity);
                if (Main.rand.NextBool(3))
                    newCloud.frameSpeed++;
                newCloud.worldPosition.Y += Main.rand.NextFloat(-10f, 5f) * player.gravDir;
                newCloud.radialPosition.Y += Main.rand.NextFloat(-10f, 15f) ;
                clouds.Add(newCloud);
            }
                
            //SandSpriteCloud smallCloud = new SandSpriteCloud(NPC.Bottom + Vector2.UnitY * 10f, NPC.velocity, true);
            //smallCloud.worldPosition.Y += Main.rand.NextFloat(-30f, 10f);
            // clouds.Add(smallCloud);
            
            //Update clouds
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                SandSpriteCloud cloud = clouds[i];

                //Animate clouds and despawn the old ones
                cloud.frameCount++;
                if (cloud.frameCount > cloud.frameSpeed)
                {
                    cloud.frameCount = 0;
                    cloud.frame++;
                    if (cloud.frame >= 7)
                    {
                        clouds.RemoveAt(i);
                        continue;
                    }
                }

                //Accelerate
                if (cloud.frame < 4)
                {
                    cloud.radialVelocity.X = MathHelper.Lerp(cloud.radialVelocity.X, 0.06f * player.direction, 0.03f);
                    cloud.radialVelocity.Y = MathHelper.Lerp(cloud.radialVelocity.Y, -0.8f * player.gravDir, 0.05f);
                }
                //Slown down
                else
                    cloud.radialVelocity = Vector2.Lerp(cloud.radialVelocity, Vector2.Zero, 0.04f);

                cloud.radialPosition += cloud.radialVelocity;

                float cloudLifeProgress = cloud.frame / 7f;
                cloudLifeProgress += (cloud.frameCount / cloud.frameSpeed) / 7f;
                cloudLifeProgress = 1 - cloudLifeProgress;

                if (cloudLifeProgress > 0.6f)
                {
                    if (cloudLifeProgress > 0.8f)
                        cloud.worldPosition.X = MathHelper.Lerp(cloud.worldPosition.X, player.Center.X, cloudLifeProgress * 0.8f);

                    cloud.velocity = Vector2.Lerp(cloud.velocity, player.velocity, cloudLifeProgress * 0.03f);
                    cloud.worldPosition.X = MathHelper.Lerp(cloud.worldPosition.X, player.Center.X, cloudLifeProgress * 0.3f);
                }

                if (Math.Abs(player.velocity.X) < 0.2f)
                    cloud.velocity.X += -player.direction * 0.1f;

                cloud.worldPosition += cloud.velocity;
                cloud.velocity *= 0.81f;

                //Warp around
                while (cloud.radialPosition.X > 2)
                    cloud.radialPosition.X -= 2;
                while (cloud.radialPosition.X < 0)
                    cloud.radialPosition.X += 2;

                cloud.rotation -= player.direction * 0.012f;
            }
        }

        private void Decapitate(Player player, PlayerDrawSet drawInfo)
        {
            if (player.GetPlayerFlag(Name + "Vanity") && !drawInfo.headOnlyRender)
                PlayerDrawLayers.Head.Hide();
        }
    }
}
