using CalamityFables.Core;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class RockySandstoneWallItem : ModItem
    {
        public static ushort[] wallTypes = new ushort[4];
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void Load()
        {
            MergingWallLoader.LoadMergingWall(AssetDirectory.BurntDesert, "RockySandstoneWall", Type, ref wallTypes, [WallID.Sandstone, WallID.SandstoneEcho], new Color(104, 52, 28), DustID.Sand);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
            DisplayName.SetDefault("Rocky Sandstone Wall");
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 7;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createWall = wallTypes[0];
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SandstoneWall).
                AddTile(TileID.HeavyWorkBench).
                Register();
        }
    }
    public class HallowedRockySandstoneWallItem : ModItem
    {
        public static ushort[] wallTypes = new ushort[4];
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void Load()
        {
            MergingWallLoader.LoadMergingWall(AssetDirectory.BurntDesert, "HallowedRockySandstoneWall", Type, ref wallTypes, [WallID.HallowSandstone, WallID.HallowSandstoneEcho], new Color(74, 41, 88), DustID.Sand);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
            DisplayName.SetDefault("Rocky Pearlsandstone Wall");
            for (int i = 0; i < 4; i++)
                WallID.Sets.Hallow[wallTypes[i]] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 7;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createWall = wallTypes[0];
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.HallowSandstoneWall).
                AddTile(TileID.HeavyWorkBench).
                Register();
        }
    }
    public class CorruptRockySandstoneWallItem : ModItem
    {
        public static ushort[] wallTypes = new ushort[4];
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void Load()
        {
            MergingWallLoader.LoadMergingWall(AssetDirectory.BurntDesert, "CorruptRockySandstoneWall", Type, ref wallTypes, [WallID.CorruptSandstone, WallID.CorruptSandstoneEcho], new Color(53, 35, 75), DustID.Corruption);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
            DisplayName.SetDefault("Rocky Ebonsandstone Wall"); 
            for (int i = 0; i < 4; i++)
                WallID.Sets.Corrupt[wallTypes[i]] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 7;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createWall = wallTypes[0];
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.CorruptSandstoneWall).
                AddTile(TileID.HeavyWorkBench).
                Register();
        }
    }
    public class CrimsonRockySandstoneWallItem : ModItem
    {
        public static ushort[] wallTypes = new ushort[4];
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void Load()
        {
            MergingWallLoader.LoadMergingWall(AssetDirectory.BurntDesert, "CrimsonRockySandstoneWall", Type, ref wallTypes, [WallID.CrimsonSandstone, WallID.CrimsonSandstoneEcho], new Color(50, 19, 21), DustID.Crimstone);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
            DisplayName.SetDefault("Rocky Crimsandstone Wall");
            for (int i = 0; i < 4; i++)
                WallID.Sets.Crimson[wallTypes[i]] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 7;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createWall = wallTypes[0];
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.CrimsonSandstoneWall).
                AddTile(TileID.HeavyWorkBench).
                Register();
        }
    }
}