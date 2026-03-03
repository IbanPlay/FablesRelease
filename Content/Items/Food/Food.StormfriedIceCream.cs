

using CalamityFables.Content.Boss.MushroomCrabBoss;

namespace CalamityFables.Content.Items.Food
{
    public class StormfriedIceCream : BaseFoodItem
    {
        public override int FoodBuff => BuffID.WellFed3;

        public override int BuffTime => 60 * 60 * 10;

        public override Point SpriteSize => new(28, 28);

        public override Color[] CrumbColors => [ new Color(237, 225, 199), new Color(210, 191, 148), new Color(165, 201, 233), new Color(233, 204, 138)];


        public override void OnConsumeItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<ElectrifiedBloodBuff>(), 60 * 60 * 5);
        }
    }

    public class ElectrifiedBloodBuff : ModBuff
    {
        public override string Texture => AssetDirectory.Food + Name;
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
    }
}
