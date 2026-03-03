using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumHatchItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Hatch");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumHatchClosed>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 2, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe(3).
                AddIngredient<WulfrumHullItem>(5).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumHatchClosed : ModTile, ICustomPaintable, ICustomDoor
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public int NewType => TileType<WulfrumHatchOpen>();
        public SoundStyle InteractSound => WulfrumDoorItem.OpenSound;

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileWaterDeath[Type] = false;
            TileID.Sets.NotReallySolid[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
            TileObjectData.newTile.AnchorLeft = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.newTile.AnchorRight = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 0;

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Hatch");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AdjTiles = new int[] { TileID.TrapdoorClosed };

            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomDoor[Type] = true;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override bool Slope(int i, int j) => false;
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemType<WulfrumHatchItem>();
        }

        public override bool RightClick(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (CustomDoorHandler.InteractWithCustomDoor(tile, i, j))
                NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, i, j, 1);

            return true;
        }

        public override void HitWire(int i, int j)
        {
            if (CustomDoorHandler.InteractWithCustomDoor(Main.tile[i, j], i, j))
                NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, i, j, 1);
        }
    }

    //Lacks painted sprites
    public class WulfrumHatchOpen : ModTile, ICustomDoor, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public int NewType => TileType<WulfrumHatchClosed>();
        public SoundStyle InteractSound => WulfrumDoorItem.CloseSound;

        public bool CountsAsSolid(int frameX, int frameY) => true;

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            RegisterItemDrop(ItemType<WulfrumHatchItem>());

            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileSolid[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileWaterDeath[Type] = false;
            TileID.Sets.NotReallySolid[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
            TileObjectData.newTile.AnchorLeft = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.newTile.AnchorRight = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 0;

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Hatch");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AdjTiles = new int[] { TileID.TrapdoorOpen };

            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomDoor[Type] = true;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override bool Slope(int i, int j) => false;
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemType<WulfrumHatchItem>();
        }

        public override bool RightClick(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (CustomDoorHandler.InteractWithCustomDoor(tile, i, j))
                NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 1, i, j, 1);

            return true;
        }

        public override void HitWire(int i, int j)
        {
            if (CustomDoorHandler.InteractWithCustomDoor(Main.tile[i, j], i, j))
                NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 1, i, j, 1);
        }
    }
}
