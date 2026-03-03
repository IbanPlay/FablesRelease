using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumModularChairItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Modular Chair");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumModularChair>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(6).
                AddIngredient(ItemID.Silk).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumModularChair : ModTile, ICustomPaintable
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

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 2, 0);
            TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, 2).ToArray();
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleMultiplier = 4;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Modular Chair");
            AddMapEntry(CommonColors.WulfrumLeatherRed, name);
            AdjTiles = new int[] { TileID.Chairs };
            TileID.Sets.CanBeSatOnForNPCs[Type] = true;
            TileID.Sets.CanBeSatOnForPlayers[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile myTile = Main.tile[i, j];
            Tile leftTile = Framing.GetTileSafely(i - 2, j);
            Tile rightTile = Framing.GetTileSafely(i + 2, j);

            bool chairLeft = leftTile.TileType == Type && leftTile.TileFrameX % 36 == myTile.TileFrameX % 36 && leftTile.TileFrameY == myTile.TileFrameY;
            bool chairRight = rightTile.TileType == Type && rightTile.TileFrameX % 36 == myTile.TileFrameX % 36 && rightTile.TileFrameY == myTile.TileFrameY;

            myTile.TileFrameX = (short)(myTile.TileFrameX % 36);

            //Aligned chairs
            if (chairLeft)
                myTile.TileFrameX += 36 * 2;
            if (chairRight)
                myTile.TileFrameX += 36;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            if (!TileDrawing.IsVisible(t))
                return;

            //Surrounded
            if (t.TileFrameX / 36 == 3)
                return;

            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }

            Vector2 drawPos = FablesUtils.TileDrawPosition(i, j);
            drawPos.Y += 2;

            Color drawColor = Lighting.GetColor(i, j);
            if (t.IsTileFullbright)
                drawColor = Color.White;

            if (t.IsTileFullbright)
                drawColor = Color.White;

            Rectangle chairEdgeFrame = new Rectangle(36 * 4, t.TileFrameY, 2, 16);
            bool leftHalf = t.TileFrameX % 36 == 0;


            //Nothing on the left
            if (t.TileFrameX / 36 < 2 && leftHalf)
                Main.spriteBatch.Draw(texture, drawPos - Vector2.UnitX * 2, chairEdgeFrame, drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            //Nothing to the right
            if (t.TileFrameX / 36 % 2 == 0 && !leftHalf)
                Main.spriteBatch.Draw(texture, drawPos + Vector2.UnitX * 16, chairEdgeFrame, drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }


        //Peeposit
        #region Peeposit
        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info)
        {
            info.DirectionOffset = 0;
            info.VisualOffset = new Vector2(-8f, 0f);

            if (info.RestingEntity != null)
                info.TargetDirection = info.RestingEntity.direction;

            Tile tile = Framing.GetTileSafely(i, j);

            i -= (tile.TileFrameX / 18) % 2;
            if (tile.TileFrameY == 0)
                j++;

            if (info.TargetDirection == 1)
                i++;

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
            player.cursorItemIconID = ItemType<WulfrumModularChairItem>();
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
