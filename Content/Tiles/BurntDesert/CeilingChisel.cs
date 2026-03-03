using CalamityFables.Content.Tiles.Graves;
using CalamityFables.Particles;
using Microsoft.Build.Tasks;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class CeilingChisel : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Ceiling Chisel");
            Tooltip.SetDefault("Carves out cracks in ceilings, letting a stream of dust flow down");
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
                AddRecipeGroup(RecipeGroupID.IronBar).
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

            return DustFallManager.PositionValidForDustFall(new Point(Player.tileTargetX, Player.tileTargetY));
        }


        public override bool? UseItem(Player player)
        {
            return DustFallManager.TryPlaceDustfall(new Point(Player.tileTargetX, Player.tileTargetY));
        }
    }
}