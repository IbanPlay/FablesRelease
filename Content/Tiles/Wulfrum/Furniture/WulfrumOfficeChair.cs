using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumOfficeChairItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Office Chair");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumOfficeChair>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(4).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumOfficeChair : ModTile, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 18 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft; // Player faces to the left

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; // Player faces to the right
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Office Chair");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
            TileID.Sets.CanBeSatOnForNPCs[Type] = true;
            TileID.Sets.CanBeSatOnForPlayers[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            FablesSets.CustomPaintedSprites[Type] = true;
            AdjTiles = new int[] { TileID.Chairs };
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;


        //Peeposit
        #region Sitting
        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info)
        {
            info.DirectionOffset = 0;
            info.VisualOffset = new Vector2(-8f, 0f);


            Tile tile = Framing.GetTileSafely(i, j);

            info.TargetDirection = -1;
            if (Main.tile[i, j].TileFrameX >= 36)
                info.TargetDirection = 1;

            int xPos = tile.TileFrameX / 18;
            if (xPos == 1)
                i--;
            if (xPos == 2)
                i++;

            j += 2 - (tile.TileFrameY / 18);

            info.AnchorTilePosition.X = i;
            info.AnchorTilePosition.Y = j;
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Tile t = Main.tile[i, j];
            i -= (t.TileFrameX / 18) % 2;
            i++;

            if (player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance + 8))
            {
                player.GamepadEnableGrappleCooldown();
                player.sitting.SitDown(player, i, j);
            }
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Tile t = Main.tile[i, j];
            i -= (t.TileFrameX / 18) % 2;
            i++;

            if (!player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance + 8))
                return;

            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemType<WulfrumOfficeChairItem>();

            //Flip the item
            if (Main.tile[i, j].TileFrameX >= 36)
                player.cursorItemIconReversed = true;
        }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            Tile t = Main.tile[i, j];
            i -= (t.TileFrameX / 18) % 2;
            i++;
            return settings.player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance);
        }
        #endregion
    }
}
