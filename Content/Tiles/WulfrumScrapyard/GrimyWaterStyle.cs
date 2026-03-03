namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class GrimyWaterStyle : ModWaterStyle
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        public override string BlockTexture => AssetDirectory.WulfrumScrapyard + Name + "_Block";
        public override string SlopeTexture => AssetDirectory.WulfrumScrapyard + Name + "_Slope";

        public override int ChooseWaterfallStyle() => ModContent.GetInstance<GrimyWaterfallStyle>().Slot;
        public override int GetSplashDust() => DustID.Water_Cavern;
        public override int GetDropletGore() => GoreID.WaterDripCavern;
        public override Color BiomeHairColor() => new(91, 99, 63);
    }
}