using CalamityFables.Content.Items.BurntDesert;
using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    public class DesertProwlerBackBloomLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.FirstVanillaLayer);
        public static Asset<Texture2D> SunAsset;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return !drawInfo.drawPlayer.dead && drawInfo.shadow == 0 && drawInfo.drawPlayer.GetModPlayer<DesertProwlerPlayer>().jumpBuffVisualOpacity > 0f;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Texture2D sunBloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            float wobble = MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.2f;

            float opacity = drawInfo.drawPlayer.GetModPlayer<DesertProwlerPlayer>().jumpBuffVisualOpacity;

            Color glowColor = Main.dayTime ? Color.Orange : Color.CadetBlue;
            Color strongGlowColor = Main.dayTime ? Color.DarkOliveGreen : Color.CornflowerBlue;

            //Copied from vanilla finch nest code, dont wanna deal w alignment
            Vector2 drawPosition = drawInfo.BodyPosition() - Vector2.UnitY * drawInfo.drawPlayer.gravDir;

            drawInfo.DrawDataCache.Add(new DrawData(sunBloom, drawPosition, null, Color.White with { A = 0 } * 0.16f * opacity, 0f, sunBloom.Size() / 2f, 2f * (0.77f + wobble), 0));
            drawInfo.DrawDataCache.Add(new DrawData(sunBloom, drawPosition, null, glowColor with { A = 0 } * 0.2f * opacity, 0f, sunBloom.Size() / 2f, 1f * (0.77f + wobble), 0));
            drawInfo.DrawDataCache.Add(new DrawData(sunBloom, drawPosition, null, strongGlowColor with { A = 0 } * 0.5f * opacity, 0f, sunBloom.Size() / 2f, 0.5f * (0.77f + wobble), 0));


            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, Color.White with { A = 0 } * 0.7f * opacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(0.4f, 2.4f + wobble) * 0.6f, 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, glowColor with { A = 0 } * 0.4f * opacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 2f + wobble) * 0.6f, 0));
            drawInfo.DrawDataCache.Add(new DrawData(lensFlare, drawPosition, null, glowColor with { A = 0 } * 0.2f * opacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 2f + wobble) * 0.8f, 0));
        }
    }
}
