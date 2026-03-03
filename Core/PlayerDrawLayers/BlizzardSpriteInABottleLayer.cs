using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Items.EarlyGameMisc;
using Terraria.DataStructures;
using static CalamityFables.Content.Items.Snow.BlizzardSpriteInABottle;

namespace CalamityFables.Core.DrawLayers
{
    public class BlizzardSpriteInABottleLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.NeckAcc);

        public static Asset<Texture2D> EyeAsset;
        public static Asset<Texture2D> EyeMirageAsset;
        public static Asset<Texture2D> BottleAsset;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetPlayerFlag("BlizzardSpriteInABottleVanity");
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (!drawInfo.drawPlayer.GetPlayerData(out BlizzardSpriteInABottleData bottleData))
            {
                bottleData = new BlizzardSpriteInABottleData();
                bottleData.player = drawInfo.drawPlayer;
                drawInfo.drawPlayer.SetPlayerData(bottleData);
            }
            if (bottleData.RenderTargetsDisposed)
                bottleData.LoadRenderTargets();

            //Draw a bottle to replace the player's head
            BottleAsset ??= ModContent.Request<Texture2D>(AssetDirectory.SnowItems + "BlizzardSpriteInABottle_Bottle");
            Texture2D bottle = BottleAsset.Value;
            Rectangle frame = bottle.Frame(1, 20, 0, drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height);
            DrawData item = new DrawData(bottle, drawInfo.BodyPosition(), frame, drawInfo.colorArmorHead * 0.4f, 0f,  drawInfo.bodyVect, 1f, drawInfo.playerEffect, 0);
            item.shader = drawInfo.drawPlayer.cHead;
            drawInfo.DrawDataCache.Add(item);

            if (bottleData.heatHaze > 0)
                DrawSun(ref drawInfo, bottleData.heatHaze);

            if (drawInfo.shadow == 0 && bottleData.processedCloudRT != null)
            {
                bottleData.processedCloudRT.Request();
                if (bottleData.processedCloudRT.IsReady)
                {
                    RenderTarget2D rt = bottleData.processedCloudRT.GetTarget();
                    DrawData cloudLayer = new DrawData(rt, bottleData.cloudCanvasPosition - Main.screenPosition, null, Color.White, -drawInfo.rotation, rt.Size() / 2f, 1f, 0, 0);
                    cloudLayer.shader = drawInfo.drawPlayer.cHead;
                    drawInfo.DrawDataCache.Add(cloudLayer);
                }
            }

            //Draw the eye
            EyeAsset ??= ModContent.Request<Texture2D>(AssetDirectory.SnowItems + "BlizzardSpriteInABottle_Eye");
            EyeMirageAsset ??= ModContent.Request<Texture2D>(AssetDirectory.SnowItems + "BlizzardSpriteInABottle_EyeMirage");

            Texture2D eye = EyeAsset.Value;
            frame = eye.Frame(1, 21, 0, drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height);
            Vector2 eyePosition = drawInfo.HeadPosition(false, true);
            float eyeRotation = drawInfo.drawPlayer.headRotation;
            Vector2 eyeOrigin = drawInfo.headVect;
            float hurtTimer = drawInfo.drawPlayer.GetModPlayer<FablesPlayer>().JustHurtTimer;
            if (hurtTimer > 0f)
            {
                eyePosition += Main.rand.NextVector2Circular(4f, 4f) * hurtTimer;
                frame.Y = 1120;
            }
            Color eyeColor = drawInfo.colorArmorHead;
            eyeColor *= (1 - bottleData.heatHaze);

            item = new DrawData(eye, eyePosition, frame, eyeColor, eyeRotation, eyeOrigin, 1f, drawInfo.playerEffect, 0);
            item.shader = drawInfo.drawPlayer.cHead;
            drawInfo.DrawDataCache.Add(item);

            //Mirage eye
            if (bottleData.heatHaze == 0)
                return;
            eyeColor = drawInfo.colorArmorHead * bottleData.heatHaze;
            eye = EyeMirageAsset.Value;
            item = new DrawData(eye, eyePosition, frame, eyeColor, eyeRotation, eyeOrigin, 1f, drawInfo.playerEffect, 0);
            item.shader = drawInfo.drawPlayer.cHead;
            drawInfo.DrawDataCache.Add(item);
        }

        public void DrawSun(ref PlayerDrawSet drawInfo, float opacity)
        {
            CloudSpriteInABottleSunLayer.SunAsset = CloudSpriteInABottleSunLayer.SunAsset ?? ModContent.Request<Texture2D>(AssetDirectory.SkyItems + "CloudSpriteInABottle_Sun");
            Texture2D sunTex = CloudSpriteInABottleSunLayer.SunAsset.Value;
            Texture2D sunBloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            float wobble = MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.2f;

            //Copied from vanilla finch nest code, dont wanna deal w alignment
            Vector2 drawPosition = drawInfo.HeadPosition() - Vector2.UnitX * 4f * drawInfo.drawPlayer.direction - Vector2.UnitY * 5f * drawInfo.drawPlayer.gravDir;

            DrawData item = new DrawData(sunTex, drawPosition, null, Color.White * opacity, 0f, sunTex.Size() / 2f, 1f * 0.6f, drawInfo.playerEffect, 0);
            drawInfo.DrawDataCache.Add(item);

            drawInfo.DrawDataCache.Add(new DrawData(sunBloom, drawPosition, null, Color.White with { A = 0 } * 0.2f * opacity, 0f, sunBloom.Size() / 2f, 0.4f * (0.77f + wobble) * 0.6f, 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, Color.White with { A = 0 } * 0.6f * opacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(0.4f, 1.7f + wobble) * 0.4f, 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, Color.Orange with { A = 0 } * 0.3f * opacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 1.7f + wobble) * 0.4f, 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, Color.Orange with { A = 0 } * 0.1f * opacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 1.7f + wobble) * 0.3f, 0));
        }
    }
}
