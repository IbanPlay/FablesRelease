using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class RustyWulfrumElevatorStationItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Derelict Wulfrum Elevator Station");
            Item.ResearchUnlockCount = 1;

            ItemID.Sets.ShimmerTransformToItem[Type] = ItemType<WulfrumElevatorStationItem>();
            ItemID.Sets.ShimmerTransformToItem[ItemType<WulfrumElevatorStationItem>()] = Type;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<RustyWulfrumElevatorStation>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 2, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<DullPlatingItem>(10).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    public class RustyWulfrumElevatorStation : ModTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

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
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[] { TileType<RustyWulfrumElevatorRail>() };
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.AlternateTile, 3, 1);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            TileObjectData.addTile(Type);

            //Counts as "door"
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AddMapEntry(CommonColors.WulfrumMetalDark);
            FablesSets.ForceHouseWall[Type] = true;
            RustyWulfrumElevatorRail.ElevatorStationType = Type;
            RustyWulfrumElevatorController._elevatorStationType = Type;
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

            player.cursorItemIconID = direction == 1 ? ItemType<RustyWulfrumElevatorHoverIndicatorUp>() : ItemType<RustyWulfrumElevatorHoverIndicatorDown>();
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

            //Draw the front cover of the tile, that goes over the platform 
            Texture2D drawTexture =TextureAssets.Tile[Type].Value;
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
            spriteBatch.Draw(drawTexture, drawOffset, new Rectangle(tile.TileFrameX, rectStartY, 18, 18), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            DrawHoverIcons(i, j, drawOffset, spriteBatch);
        }

        public void DrawHoverIcons(int i, int j, Vector2 drawPosition, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            int frameX = tile.TileFrameX;
            //Offset necessary so that the arrow on the left side draws over the left-middle row
            if (frameX < 36)
            {
                frameX -= 18;
                drawPosition.X -= 16;
                i -= 1;
            }

            if (frameX % 36 != 0 || tile.TileFrameY == 0)
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

            int direction = (frameX - 36) / -36;
            bool canInteract = controller.CanInteractAtFloor(floor, direction);
            if (!canInteract)
                return;

            Texture2D highlight = RustyWulfrumElevatorBase.ButtonHighlights.Value;
            drawPosition += new Vector2(8, -10);
            if (direction != 0)
            {
                drawPosition.Y += 2;
                drawPosition.X += 5 * direction;
            }

            Color color = Main.OurFavoriteColor * Lighting.GetColor(i, j).GetBrightness();
            Rectangle frame = direction == 0 ? new Rectangle(12, 0, 16, 18) : new Rectangle(0 + (direction < 0 ? 30 : 0), 0, 10, 14);
            spriteBatch.Draw(highlight, drawPosition, frame, color, 0f, new Vector2(frame.Width / 2f, 0), 1f, SpriteEffects.None, 0.0f);
        }

        public override void HitWire(int i, int j) => WulfrumElevatorController.StationWireInteraction(i, j);

    }
}
