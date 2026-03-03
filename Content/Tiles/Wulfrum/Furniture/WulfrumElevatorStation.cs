using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumElevatorStationItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Elevator Station");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumElevatorStation>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 2, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(10).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumElevatorStation : ModTile, ICustomPaintable, ICustomLayerTile
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileSolidTop[Type] = true;
            Main.tileTable[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Width = 5;
            TileObjectData.newTile.Origin = new Point16(2, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };

            //Attach from top of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 5, 0);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            //Attach from edge of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 2, 0);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            //Attach from edge of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 2, 0);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            //Attach from top of a rail
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[] { TileType<WulfrumElevatorRail>() };
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.AlternateTile, 3, 1);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            TileObjectData.addTile(Type);

            //Counts as "door"
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AddMapEntry(CommonColors.WulfrumMetalDark);
            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.ForceHouseWall[Type] = true;
            WulfrumElevatorRail.ElevatorStationType = Type;
            WulfrumElevatorController._elevatorStationType = Type;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
        public override bool RightClick(int i, int j) => WulfrumElevatorController.StationInteraction(i, j);

        public override void MouseOver(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX % 36 != 0)
                return;

            WulfrumElevatorController controller = WulfrumElevatorController.GetControllerAtPosition(i, j, out int floor);
            if (controller is null)
                return;

            int direction = (tile.TileFrameX - 36) / -36;
            bool canInteract = controller.CanInteractAtFloor(floor, direction);
            if (!canInteract)
                return;

            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            if (direction == 0)
                direction = floor > controller.PlatformAnchorStation ? 1 : -1;

            player.cursorItemIconID = direction == 1 ? ItemType<WulfrumElevatorHoverIndicatorUp>() : ItemType<WulfrumElevatorHoverIndicatorDown>();
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.tile[i, j].IsTileInvisible)
                return;
            ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.AboveTiles, true);
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y);

            Texture2D drawTexture = Terraria.GameContent.TextureAssets.Tile[Type].Value;
            if (tile.TileColor != 0)
            {
                Texture2D paintedTexture = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, tile.TileColor);
                drawTexture = paintedTexture ?? drawTexture;
            }

            Color drawColor = Lighting.GetColor(i, j);
            if (tile.IsTileFullbright)
                drawColor = Color.White;

            int rectStartY = tile.TileFrameY + 38;
            if (tile.TileFrameY > 0)
                rectStartY -= 2;
            Main.spriteBatch.Draw(drawTexture, drawOffset, new Rectangle(tile.TileFrameX, rectStartY, 18, 18), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX % 36 != 0 || tile.TileFrameY == 0)
                return;

            Vector2 mouseVect = Main.LocalPlayer.MouseWorld();
            Rectangle selectionRect = new Rectangle(i * 16, (j - tile.TileFrameY / 18) * 16, 16, 32);
            if (!selectionRect.Contains((int)mouseVect.X, (int)mouseVect.Y))
                return;

            if (!Main.LocalPlayer.InInteractionRange(i, j, TileReachCheckSettings.Simple))
                return;

            WulfrumElevatorController controller = WulfrumElevatorController.GetControllerAtPosition(i, j, out int floor);
            if (controller is null)
                return;
            int direction = (tile.TileFrameX - 36) / -36;
            bool canInteract = controller.CanInteractAtFloor(floor, direction);
            if (!canInteract)
                return;

            Texture2D highlight = WulfrumElevatorBase.ButtonHighlights.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            drawOffset += new Vector2(8, -8);
            if (direction == 0)
                drawOffset.Y -= 2;
            else
                drawOffset.X += 1 * direction;

            Color color = Main.OurFavoriteColor * Lighting.GetColor(i, j).GetBrightness();
            Rectangle frame = direction == 0 ? new Rectangle(12, 0, 16, 16) : new Rectangle(0 + (direction < 0 ? 30 : 0), 0, 10, 12);
            Main.spriteBatch.Draw(highlight, drawOffset, frame, color, 0f, new Vector2(frame.Width / 2f, 0), 1f, SpriteEffects.None, 0.0f);
        }

        public override void HitWire(int i, int j) => WulfrumElevatorController.StationWireInteraction(i, j);

    }
}
