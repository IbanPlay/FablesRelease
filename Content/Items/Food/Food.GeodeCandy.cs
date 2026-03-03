

namespace CalamityFables.Content.Items.Food
{
    public class GeodeCandy : BaseFoodItem
    {
        public override int FoodBuff => BuffID.WellFed3;

        public override int BuffTime => 60 * 60 * 10;

        public override Point SpriteSize => new(20, 36);

        public override Color[] CrumbColors => [ new Color(55, 214, 233), new Color(231, 112, 255), new Color(206, 74, 88)];

        public override void OnConsumeItem(Player player)
        {
            player.AddBuff(BuffID.Lucky, 60 * 60 * 7);
        }
    }
}
