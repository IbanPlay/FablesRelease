namespace CalamityFables.Content.Items.Cursed
{
    public class CursedRarity : ModRarity
    {
        public override Color RarityColor => Color.Lerp(new Color(98, 25, 53), new Color(160, 10, 20), (float)Math.Pow(0.5f + 0.5f * Math.Sin(Main.GlobalTimeWrappedHourly * 3f), 3f));
    }
}
