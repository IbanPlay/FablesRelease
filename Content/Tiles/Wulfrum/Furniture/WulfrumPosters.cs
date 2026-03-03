using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public abstract class BaseWulfrumPosterItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public abstract string PosterDisplayName { get; }
        public abstract string PosterTooltip { get; }
        public abstract int PosterTileType { get; }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Poster - " + PosterDisplayName);
            Tooltip.SetDefault(PosterTooltip);
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(PosterTileType);
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 0, 25);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(1).
                AddIngredient(ItemID.Wood).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public abstract class BaseWulfrumPosterTile : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public abstract int PosterItemType { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public virtual int OriginX => Width / 2;
        public virtual int OriginY => Height / 2;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.DynastyWall;
            HitSound = SoundID.Dig;

            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(OriginX, OriginY);
            TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, Height).ToArray();
            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Poster");
            AddMapEntry(CommonColors.WulfrumLeatherRed, name);
            TileID.Sets.FramesOnKillWall[Type] = true;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    #region Washing machine
    public class WulfrumWashingMachinePosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Washing Machine";
        public override string PosterTooltip => "'Guaranteed, the fastest, most thorough wash in the industry, or your money back!\n" +
                    "[c/EED202:WARNING!] Do not cycle until you have made sure the included restraints have been securely fastened.'";
        public override int PosterTileType => TileType<WulfrumWashingMachinePoster>();
    }

    public class WulfrumWashingMachinePoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumWashingMachinePosterItem>();
        public override int Width => 3;
        public override int Height => 2;
    }
    #endregion

    #region Acrobatics Pack Poster
    public class WulfrumAcrobaticsPackPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Acrobatics Pack";
        public override string PosterTooltip => "'You ever tried to use wings in a cave? Awful!\n" +
                    "For the explorers out there, the Acrobatics Pack™ is the way to go!'";
        public override int PosterTileType => TileType<WulfrumAcrobaticsPackPoster>();
    }

    public class WulfrumAcrobaticsPackPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumAcrobaticsPackPosterItem>();
        public override int Width => 2;
        public override int Height => 3;
    }
    #endregion

    #region Digging Turtle Poster
    public class WulfrumDiggingTurtlePosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Digging Turtle";
        public override string PosterTooltip => "'The long awaited sequel to the Digging Mole™, their turtle-y nature allows them to last a whopping THREE seconds longer before exploding!'";
        public override int PosterTileType => TileType<WulfrumDiggingTurtlePoster>();
    }

    public class WulfrumDiggingTurtlePoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumDiggingTurtlePosterItem>();
        public override int Width => 2;
        public override int Height => 3;
    }
    #endregion

    #region Grappler Poster
    public class WulfrumGrapplerPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Grappler";
        public override string PosterTooltip => "'Hang in there!'";
        public override int PosterTileType => TileType<WulfrumGrapplerPoster>();
    }

    public class WulfrumGrapplerPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumGrapplerPosterItem>();
        public override int Width => 3;
        public override int Height => 3;
    }
    #endregion

    #region Rover Poster
    public class WulfrumRoverPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Rover";
        public override string PosterTooltip => "'Keep your friends close, but keep your Rover™ even closer!'";
        public override int PosterTileType => TileType<WulfrumRoverPoster>();
    }

    public class WulfrumRoverPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumRoverPosterItem>();
        public override int Width => 3;
        public override int Height => 2;
    }
    #endregion

    #region Gyrator Poster
    public class WulfrumGyratorPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Gyrator";
        public override string PosterTooltip => "'Old is new! Return to the humble beginnings of Wulfrum Co. with this timeless classic!\n" +
                    "Comes free with the purchase of any other Wulfrum Co. product.'";
        public override int PosterTileType => TileType<WulfrumGyratorPoster>();
    }

    public class WulfrumGyratorPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumGyratorPosterItem>();
        public override int Width => 2;
        public override int Height => 2;
    }
    #endregion

    #region Fidget Poster
    public class WulfrumFidgetPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Fidget Toys";
        public override string PosterTooltip => "'Our incredible fidgets are included with every Wulfrum Co. purchase. Collect all One-Hundred and Fifty-One!'";
        public override int PosterTileType => TileType<WulfrumFidgetPoster>();
    }

    public class WulfrumFidgetPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumFidgetPosterItem>();
        public override int Width => 3;
        public override int Height => 3;
    }
    #endregion

    #region Power Armor Poster
    public class WulfrumPowerArmorPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Power Armor";
        public override string PosterTooltip => "'Tired of being an unpopular, forgettable good-for-nothing?\n" +
                    "With this suit, you can finally become as loved and appreciated as our very own John Wulfrum™!'";
        public override int PosterTileType => TileType<WulfrumPowerArmorPoster>();
    }

    public class WulfrumPowerArmorPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumPowerArmorPosterItem>();
        public override int Width => 3;
        public override int Height => 3;
    }
    #endregion

    #region Mortar Poster
    public class WulfrumMortarPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Mortar";
        public override string PosterTooltip => "'Can't be bothered to deal with trespassers yourself?\n" +
                    "With only a handful of these defensive units, your property will be kept safe while suffering from minimal collateral damage!'";
        public override int PosterTileType => TileType<WulfrumMortarPoster>();
    }

    public class WulfrumMortarPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumMortarPosterItem>();
        public override int Width => 3;
        public override int Height => 2;
    }
    #endregion

    #region Roller Poster
    public class WulfrumRollerPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Roller";
        public override string PosterTooltip => "'You won’t grind your gears with this cog on your side!'";
        public override int PosterTileType => TileType<WulfrumRollerPoster>();
    }

    public class WulfrumRollerPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumRollerPosterItem>();
        public override int Width => 2;
        public override int Height => 2;
    }
    #endregion

    #region Magnetizer Poster
    public class WulfrumMagnetizerPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Magnetizer";
        public override string PosterTooltip => "'Due to popular demand, Wulfrum Co.'s own delivery robots have finally hit the shelves!\n" +
                    "[c/EED202:WARNING!] Do not attempt to transport iron-based products.'";
        public override int PosterTileType => TileType<WulfrumMagnetizerPoster>();
    }

    public class WulfrumMagnetizerPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumMagnetizerPosterItem>();
        public override int Width => 3;
        public override int Height => 3;
    }
    #endregion

    #region Computer Poster
    public class WulfrumComputerPosterItem : BaseWulfrumPosterItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override string PosterDisplayName => "Computer";
        public override string PosterTooltip => "'One whole terabyte solely dedicated to the Wulfrum Co. merchandise search engine!\n" +
                    "Comes pre-installed with the latest version of Wulfrum OS DX.'";
        public override int PosterTileType => TileType<WulfrumComputerPoster>();
    }

    public class WulfrumComputerPoster : BaseWulfrumPosterTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override int PosterItemType => ItemType<WulfrumComputerPosterItem>();
        public override int Width => 3;
        public override int Height => 2;
    }
    #endregion
}
