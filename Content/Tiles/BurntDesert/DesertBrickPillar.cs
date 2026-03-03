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
    public class DesertBrickPillar : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Desert Brick Pillar");
            Item.ResearchUnlockCount = 30;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<DesertBrickPillarTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SandstoneBrick, 3).
                AddTile(TileID.HeavyWorkBench).
                Register();
        }
    }

    public class DesertBrickPillarTile : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;

            SandstonePillarTile.BrickPillarType = Type;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.StyleWrapLimit = 42;
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.Platform | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.AnchorAlternateTiles = [Type];
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.RandomStyleRange = 12;
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(CheckForPillarAlignment, -1, 0, false);
            TileObjectData.addTile(Type);

            DustType = DustID.Sand;

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(233, 227, 130));

            RegisterItemDrop(ItemType<DesertBrickPillar>());
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = 2;
        }

        public static int CheckForPillarAlignment(int x, int y, int type, int style , int direction, int alternate)
        {
            //Placing on a pillar needs the tile to be aligned with it
            if (y < Main.maxTilesY - 2)
            {
                Tile tileBelow = Main.tile[x, y + 1];
                if (tileBelow.HasTile && tileBelow.TileType == type && tileBelow.TileFrameX / 18 != 1)
                    return -1;
            }
            return 0;
        }

        public bool ValidTopTileForPillar(int i, int j, out bool connectedToSelf)
        {
            connectedToSelf = false;
            //Failsave
            if (j < 0)
                return true;

            Tile tileTop = Main.tile[i, j];
            if (!tileTop.HasTile ||
                !Main.tileSolid[tileTop.TileType] ||
                Main.tileSolidTop[tileTop.TileType] ||
                TileID.Sets.NotReallySolid[tileTop.TileType] ||
                (tileTop.Slope != SlopeType.Solid && !tileTop.BottomSlope))
            {
                connectedToSelf = tileTop.TileType == Type;
                return tileTop.TileType == Type;
            }

            return true;
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile t = Main.tile[i, j];
            int centerX = i;
            centerX -= t.TileFrameX / 18 - 1;

            bool breakTop = false;

            for (int x = centerX - 1; x < centerX + 2; x++)
            {
                if (!ValidTopTileForPillar(x, j - 1, out _))
                {
                    breakTop = true;
                    break;
                }
            }

            int newFrameY = -1;


            //Has a broken top already
            if (t.TileFrameY >= 214 && !breakTop)
                newFrameY = WorldGen.genRand.Next(12);
            //Doesnt have a broken top
            else if (t.TileFrameY < 214 && breakTop)
                newFrameY = 12 + WorldGen.genRand.Next(6);

            //Update the row if we had to change it
            if (newFrameY != -1)
            {
                int pillarLeft = i - t.TileFrameX / 18;
                for (int p = pillarLeft; p < pillarLeft + 3; p++)
                {
                    Tile pillarTile = Main.tile[p, j];
                    pillarTile.TileFrameY = (short)(newFrameY * 18);
                }
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (j == 0)
                return;

            Tile tile = Main.tile[i, j];
            if (!TileDrawing.IsVisible(tile))
                return;

            //If we're not a top frame and yet there's no pillar continuation above this, that means the pillar is touching a ceiling
            if (tile.TileFrameY >= 214 || !Main.tile[i, j - 1].HasTile || Main.tile[i, j - 1].TileType == Type)
            {
                return;
            }

            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (tile.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, tile.TileColor);
                texture = paintedTex ?? texture;
            }

            Vector2 drawOffset = FablesUtils.TileDrawOffset();
            bool middle = tile.TileFrameX / 18 == 1;

            int frameX = tile.TileFrameX == 0 ? 0 : middle ? 20 : 38;

            Color drawColor = Lighting.GetColor(i, j);
            if (tile.IsTileFullbright)
                drawColor = Color.White;

            spriteBatch.Draw(texture, new Vector2(i * 16, j * 16 - 2) - drawOffset, new Rectangle(frameX, 325, middle ? 16 : 18, 14), drawColor, 0f, default, 1f, 0, 0f);
        }
    }
}
