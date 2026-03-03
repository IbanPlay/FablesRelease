using CalamityFables.Content.Tiles.Graves;
using CalamityFables.Particles;
using Microsoft.Build.Tasks;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class MirageDust : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Mirage Dust");
            Tooltip.SetDefault("Can be powdered down on the floor to create a column of ghostly floating dust");
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.rare = ItemRarityID.Blue;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = false;
            Item.UseSound = SoundID.Item1;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.DesertFossil, 10).
                AddCondition(SealedChamber.InNautilusChamber).
                Register();
        }


        public override bool CanUseItem(Player player)
        {
            //Reach check
            float distanceToPlayer = (new Vector2(Player.tileTargetX, Player.tileTargetY) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer > Player.tileRangeX)
                return false;

            return DustFallManager.PositionValidForDustFall(new Point(Player.tileTargetX, Player.tileTargetY), -1);
        }

        public override bool? UseItem(Player player)
        {
            return DustFallManager.TryPlaceDustfall(new Point(Player.tileTargetX, Player.tileTargetY), true);
        }
    }
}