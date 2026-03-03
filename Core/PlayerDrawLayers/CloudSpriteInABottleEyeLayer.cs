using CalamityFables.Content.Items.EarlyGameMisc;
using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    public class CloudSpriteInABottleEyeLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.NeckAcc);

        public static Asset<Texture2D> EyeAsset;
        public static Asset<Texture2D> HurtEyeAsset;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return !drawInfo.drawPlayer.dead && drawInfo.shadow == 0 && drawInfo.drawPlayer.GetPlayerFlag("CloudSpriteInABottleVanity");
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            EyeAsset = EyeAsset ?? ModContent.Request<Texture2D>(AssetDirectory.SkyItems + "CloudSpriteInABottle_Eye");
            HurtEyeAsset = HurtEyeAsset ?? ModContent.Request<Texture2D>(AssetDirectory.SkyItems + "CloudSpriteInABottle_EyeHurt");

            float hurtTime = drawInfo.drawPlayer.GetModPlayer<FablesPlayer>().JustHurtTimer;

            Texture2D eyeTex = EyeAsset.Value;
            Rectangle bodyFrame = drawInfo.drawPlayer.bodyFrame;

            Vector2 drawPosition = drawInfo.HeadPosition(false, true);
            float usedRotation = drawInfo.drawPlayer.headRotation;
            Vector2 origin = drawInfo.headVect;

            if (hurtTime > 0.3f)
            {
                eyeTex = HurtEyeAsset.Value;
                drawPosition += Main.rand.NextVector2Circular(4f, 4f) * (float)Math.Pow(hurtTime, 3f);
                bodyFrame.Y = bodyFrame.Height * 5;
            }

            DrawData item = new DrawData(eyeTex, drawPosition, bodyFrame, Color.Lerp(drawInfo.colorArmorHead, Color.White, 0.7f), usedRotation, origin, 1f, drawInfo.playerEffect, 0);
            item.shader = drawInfo.drawPlayer.cHead;

            //Cache the layer's index so we can grab it back afterwards
            CloudMetaballLayer.CloudSpriteEyeDrawIndex = drawInfo.DrawDataCache.Count;
            drawInfo.DrawDataCache.Add(item);
        }
    }
}
