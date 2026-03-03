using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class MakeshiftStool : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Makeshift Stool");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<MakeshiftStoolTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.PalmWood, 15).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }

    public class MakeshiftStoolTile : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleWrapLimit = 2;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop , TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft; 

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); 
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; 
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);
            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.BorealWood;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Stool");
            AddMapEntry(new Color(117, 92, 69), name);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
            TileID.Sets.CanBeSatOnForNPCs[Type] = true;
            TileID.Sets.CanBeSatOnForPlayers[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            AdjTiles = new int[] { TileID.Chairs };
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = 2;
        }



        //Peeposit
        #region Sitting
        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info)
        {
            info.VisualOffset = new Vector2(-8f, -2f);
            info.DirectionOffset = 2;

            Tile tile = Framing.GetTileSafely(i, j);

            info.TargetDirection = -1;
            if (Main.tile[i, j].TileFrameX >= 36)
                info.TargetDirection = 1;

            int xPos = tile.TileFrameX / 18;
            if (xPos == 1)
                i--;
            if (xPos == 2)
                i++;

            j += 1 - (tile.TileFrameY / 18);

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
            player.cursorItemIconID = ItemType<MakeshiftStool>();

            //Flip the item
            if (Main.tile[i, j].TileFrameX >= 36)
                player.cursorItemIconReversed = true;
        }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            Tile t = Main.tile[i, j];
            i -= (t.TileFrameX / 18) % 2;
            i++;
            return settings.player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance + 8);
        }
        #endregion
    }
}
