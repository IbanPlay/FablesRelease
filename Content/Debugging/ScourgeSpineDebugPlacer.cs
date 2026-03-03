using CalamityFables.Content.Tiles.BurntDesert;
using Terraria.DataStructures;

namespace CalamityFables.Content.Debug
{
    public class ScourgeSpineDebugPlacer : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetDefaults()
        {
            Item.useTime = Item.useAnimation = 8;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.shootSpeed = 20f;
            Item.width = 30;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(0, 0, 10, 0);
            Item.rare = ItemRarityID.Blue;
        }


        public override bool? UseItem(Player player)
        {
            foreach (var item in TileEntity.ByID)
            {
                if (item.Value is ScourgeSpineDecor decor)
                {
                    decor.Kill(decor.Position.X, decor.Position.Y);
                }
            }

            FablesWorld.FillDesertWithScourgeSkeletons();
            return true;
        }
    }

}