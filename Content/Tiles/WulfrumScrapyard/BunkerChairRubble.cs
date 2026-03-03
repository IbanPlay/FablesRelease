using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class BunkerChairRubble : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileNoAttach[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(1, 2);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft; // Player faces to the left

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; // Player faces to the right
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            TileID.Sets.CanBeSatOnForNPCs[Type] = true;
            TileID.Sets.CanBeSatOnForPlayers[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;

            DustType = DustID.Iron;
            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(114, 110, 101), name);
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
            info.VisualOffset.X = -4f;
            if (Main.tile[i, j].TileFrameX >= 36)
            {
                info.TargetDirection = 1;
            }

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
            player.cursorItemIconID = ModContent.ItemType<BunkerChairRubbleHover>();

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

    public class BunkerChairRubbleEcho : BunkerChairRubble
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "BunkerChairRubble";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ModContent.ItemType<DullPlatingItem>());
            FlexibleTileWand.RubblePlacementSmall.AddVariations(ModContent.ItemType<DullPlatingItem>(), Type, 0);
        }
    }

    internal class BunkerChairRubbleHover : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("");
            Tooltip.SetDefault("You shouldn't be seeing this");
            Item.ResearchUnlockCount = 0;
            ItemID.Sets.Deprecated[Type] = true;
            ItemID.Sets.ItemsThatShouldNotBeInInventory[Type] = true;
        }
    }

}
