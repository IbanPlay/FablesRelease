using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Items.EarlyGameMisc;
using Terraria.DataStructures;
using static CalamityFables.Content.Items.BurntDesert.SandstormSpriteInABottle;

namespace CalamityFables.Core.DrawLayers
{
    public class SandstormSpriteInABottleLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.NeckAcc);

        public static Asset<Texture2D> EyeAsset;
        public static Asset<Texture2D> BottleAsset;


        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetPlayerFlag("SandstormSpriteInABottleVanity");
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {

            if (!drawInfo.drawPlayer.GetPlayerData(out SandstormSpriteInABottleData bottleData))
            {
                bottleData = new SandstormSpriteInABottleData();
                bottleData.player = drawInfo.drawPlayer;
                drawInfo.drawPlayer.SetPlayerData(bottleData);
            }

            if (!bottleData.loadedRenderTargets)
                bottleData.LoadRenderTargets();


            Vector2 eyeRotationVector = bottleData.eyeRotation.ToRotationVector2();
            if (eyeRotationVector.Y < 0)
                DrawEye(ref drawInfo, bottleData);

            DrawCloudLayer(ref drawInfo, bottleData.backCloudRT);

            //Draw a bottle to replace the player's head
            BottleAsset = BottleAsset ?? ModContent.Request<Texture2D>(AssetDirectory.DesertItems + "SandstormSpriteInABottle_Head");
            Texture2D bottle = BottleAsset.Value;
            Rectangle frame = bottle.Frame(1, 20, 0, drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height);
            DrawData item = new DrawData(bottle, drawInfo.BodyPosition(), frame, drawInfo.colorArmorHead * 0.4f, 0f,  drawInfo.bodyVect, 1f, drawInfo.playerEffect, 0);
            item.shader = drawInfo.drawPlayer.cHead;
            drawInfo.DrawDataCache.Add(item);


            DrawCloudLayer(ref drawInfo, bottleData.frontCloudRT);

            if (eyeRotationVector.Y >= 0)
                DrawEye(ref drawInfo, bottleData);
        }

        public void DrawCloudLayer(ref PlayerDrawSet drawInfo, MergeBlendTextureContent renderTarget)
        {
            if (drawInfo.shadow != 0)
                return;

            if (renderTarget == null)
                return;
            renderTarget.Request();
            if (!renderTarget.IsReady)
                return;

            RenderTarget2D rt = renderTarget.GetTarget();
            DrawData item = new DrawData(rt, drawInfo.drawPlayer.Center - Main.screenPosition, null, drawInfo.colorArmorHead * 0.7f, 0f, rt.Size() / 2f, 1f, 0, 0);
            item.shader = drawInfo.drawPlayer.cHead;
            drawInfo.DrawDataCache.Add(item);
        }

        public void DrawEye(ref PlayerDrawSet drawInfo, SandstormSpriteInABottleData bottleData)
        {
            if (drawInfo.shadow != 0)
                return;

            EyeAsset = EyeAsset ?? ModContent.Request<Texture2D>(AssetDirectory.DesertItems + "SandstormSpriteInABottleEye");

            Texture2D eyeTex = EyeAsset.Value;
            Vector2 drawPosition = drawInfo.HeadPosition(true, false);
            drawPosition.Y -= 6f * drawInfo.drawPlayer.gravDir;

            drawPosition.X += 2f * drawInfo.drawPlayer.direction;

            //Small lag behind
            Vector2 dampedVelocity = drawInfo.drawPlayer.GetModPlayer<FablesPlayer>().springyVelocityTracker.value;

            Vector2 lagOffset = dampedVelocity;
            lagOffset.X *= 0.3f; //Less lag horizontally cuz it looks a bit less good
            if (drawInfo.drawPlayer.gravDir == 1 && lagOffset.Y < -4)
                lagOffset.Y = -4;
            else if (drawInfo.drawPlayer.gravDir == -1 && lagOffset.Y > 4)
                lagOffset.Y = 4;
            drawPosition -= lagOffset;

            Vector2 eyeRotationVector = bottleData.eyeRotation.ToRotationVector2();
            eyeRotationVector.Y = 0;
            float eyeTilt = dampedVelocity.X * -0.03f;
            eyeRotationVector = eyeRotationVector.RotatedBy(eyeTilt);
            
            Vector2 eyePosition = drawPosition + eyeRotationVector * 12f;


            //Get the eye's frame based on the rotation cycle
            float wrappedRotation = (bottleData.eyeRotation.Modulo(MathHelper.TwoPi) / MathHelper.TwoPi);
            int rotationFrame = (int)(wrappedRotation * 12);
            rotationFrame = rotationFrame.Modulo(12);
            Rectangle eyeFrame = eyeTex.Frame(4, 12, 0, rotationFrame, -2, -2);


            Color eyeColor = Color.Lerp(drawInfo.colorArmorHead, Color.White, 0.7f);
            SpriteEffects flip = drawInfo.drawPlayer.gravDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            DrawData item = new DrawData(eyeTex, eyePosition, eyeFrame, eyeColor, eyeTilt, eyeFrame.Size() / 2f, 1f, flip, 0);
            item.shader = drawInfo.drawPlayer.cHead;
            drawInfo.DrawDataCache.Add(item);

            //Tints the eye based on the biome
            FablesUtils.GetBiomeInfluences(out float corroInfluence, out float crimInfluence, out float hallowInfluence);
            if (hallowInfluence > 0)
            {
                Rectangle hallowEyeFrame = eyeFrame;
                hallowEyeFrame.X += 16;
                item = new DrawData(eyeTex, eyePosition, hallowEyeFrame, eyeColor * hallowInfluence, eyeTilt, eyeFrame.Size() / 2f, 1f, flip, 0);
                item.shader = drawInfo.drawPlayer.cHead;
                drawInfo.DrawDataCache.Add(item);
            }
            if (corroInfluence > 0)
            {
                Rectangle corroEyeFrame = eyeFrame;
                corroEyeFrame.X += 16 * 2;
                item = new DrawData(eyeTex, eyePosition, corroEyeFrame, eyeColor * corroInfluence, eyeTilt, eyeFrame.Size() / 2f, 1f, flip, 0);
                item.shader = drawInfo.drawPlayer.cHead;
                drawInfo.DrawDataCache.Add(item);
            }
            if (crimInfluence > 0)
            {
                Rectangle crimEyeFrame = eyeFrame;
                crimEyeFrame.X += 16 * 3;
                item = new DrawData(eyeTex, eyePosition, crimEyeFrame, eyeColor * crimInfluence, eyeTilt, eyeFrame.Size() / 2f, 1f, flip, 0);
                item.shader = drawInfo.drawPlayer.cHead;
                drawInfo.DrawDataCache.Add(item);
            }
        }
    }
}
