using CalamityFables.Content.Items.EarlyGameMisc;
using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    public class CloudSpriteInABottleSunLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Backpacks);
        public static Asset<Texture2D> SunAsset;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return !drawInfo.drawPlayer.dead && drawInfo.shadow == 0 && drawInfo.drawPlayer.GetPlayerFlag("CloudSpriteInABottleVanity");
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            SunAsset = SunAsset ?? ModContent.Request<Texture2D>(AssetDirectory.SkyItems + "CloudSpriteInABottle_Sun");
            Texture2D sunTex = SunAsset.Value;
            Texture2D sunBloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            float wobble = MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.2f;

            //Copied from vanilla finch nest code, dont wanna deal w alignment
            Vector2 drawPosition = drawInfo.HeadPosition() - Vector2.UnitX * 10f * drawInfo.drawPlayer.direction - Vector2.UnitY * 5f * drawInfo.drawPlayer.gravDir;

            DrawData item = new DrawData(sunTex, drawPosition, null, Color.White, 0f, sunTex.Size() / 2f, 1f, drawInfo.playerEffect, 0);
            drawInfo.DrawDataCache.Add(item);

            drawInfo.DrawDataCache.Add(new DrawData(sunBloom, drawPosition, null, Color.White with { A = 0 } * 0.2f, 0f, sunBloom.Size() / 2f, 0.4f * (0.77f + wobble), 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, Color.White with { A = 0 } * 0.7f, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(0.4f, 1.7f + wobble) * 0.6f, 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, Color.Orange with { A = 0 } * 0.4f, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 1.7f + wobble) * 0.6f, 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, Color.Orange with { A = 0 } * 0.2f, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 1.7f + wobble) * 0.8f, 0));
        }
    }
}
