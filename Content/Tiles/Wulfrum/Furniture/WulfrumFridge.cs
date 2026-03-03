using NetEasy;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumFridgeItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Fridge");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumFridge>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 5, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(6).
                AddRecipeGroup("IronBar", 2).
                AddIngredient(ItemID.IceBlock).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumFridge : ModTile, ICustomPaintable, IModdedContainer
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public static Asset<Texture2D> GlowMask;

        public override void Load()
        {
            if (!Main.dedServ)
                GlowMask = Request<Texture2D>(Texture + "Glow");
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileSolidTop[Type] = true;
            Main.tileContainer[Type] = true;
            TileID.Sets.BasicChest[Type] = true;
            Main.tileTable[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true; 
            TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.Origin = new(1, 2);
            TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, 3).ToArray();
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight; // Player faces to the left

            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, processedCoordinates: true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(PostPlaceHook, -1, 0, processedCoordinates: false);
            TileObjectData.newTile.AnchorInvalidTiles = new int[5] {
                127, //Ice rod ice blocks
                138, //Boulders
                484, //Rolling cacti
                664,
                665
            };

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; // Player faces to the right
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Fridge");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomContainer[Type] = true;
        }

        #region Container
        public override LocalizedText DefaultContainerName(int frameX, int frameY)
        {
            LocalizedText text = this.GetLocalization("ContainerName", () => "Wulfrum Fridge");
            //text.SetDefault("Wulfrum Locker");
            return text;
        }

        public static void GetRowOrigin(ref int i, ref int j)
        {
            Tile tile = Main.tile[i, j];
            i -= (tile.TileFrameX % 54) / 18;
            j -= tile.TileFrameY / 18;
        }

        public bool IsChestThere(Tile t) => t.TileFrameY == 0 && (t.TileFrameX % 54) == 0;

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);
        public override void MouseOver(int i, int j) => Hovering(i, j);
        public override void MouseOverFar(int i, int j) => Hovering(i, j, true);
        public static void Hovering(int i, int j, bool far = false)
        {
            GetRowOrigin(ref i, ref j);
            if (!far)
                FablesUtils.ChestMouseOver<WulfrumFridgeItem>(i, j, Main.tile[i, j].TileFrameX == 0);
            else
                FablesUtils.ChestMouseFar<WulfrumFridgeItem>(i, j);
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            GetRowOrigin(ref i, ref j);
            return FablesUtils.ChestRightClick(i, j);
        }

        public int StorageWidth => 3;
        public int StorageHeight => 3;

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => ModdedContainers.PreventAutodestructionTileFrame(Type, i, j, ref resetFrame, ref noBreak);

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            GetRowOrigin(ref i, ref j);
            return Chest.CanDestroyChest(i, j);
        }

        public bool TryDestroying(int x, int y)
        {
            GetRowOrigin(ref x, ref y);

            CalamityFables.Instance.Logger.Debug("Attempting to destroy modded chest");

            return !Chest.DestroyChest(x, y);
        }

        public static int PostPlaceHook(int x, int y, int type = 21, int style = 0, int direction = 1, int alternate = 0)
        {
            GetRowOrigin(ref x, ref y);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Chest.CreateChest(x, y);
            }
            else
            {
                var packet = new SyncGenericContainerModule(Main.myPlayer, true, x, y, direction, type, ItemType<WulfrumFridgeItem>());
                packet.Send(-1, -1, false);
            }

            return 1;
        }

        public Module GetDestructionPacket(int x, int y)
        {
            GetRowOrigin(ref x, ref y);
            return new SyncGenericContainerModule(Main.myPlayer, false, x, y);
        }
        #endregion

        #region Visual
        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile tile = Main.tile[i, j];
            r = 0;
            g = 0;
            b = 0;

            if (tile.TileFrameY > 0)
                return;
            if (tile.TileFrameX / 18 < 2 || tile.TileFrameX / 18 >= 4)
                return;

            float squareWave = (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 4 + 0.2f) * 0.99f + 1) * 0.4f + 0.2f;

            float colorMult = 1 / 265f * 0.5f * squareWave;

            r = CommonColors.WulfrumBlue.R * colorMult;
            g = CommonColors.WulfrumBlue.G * colorMult;
            b = CommonColors.WulfrumBlue.B * colorMult;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            float squareWave = (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 4) * 0.99f + 1) + 0.2f;

            Tile tile = Main.tile[i, j];
            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY;

            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            Color drawColour = GetDrawColour(i, j, Color.White);
            drawColour *= squareWave;

            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos - 2, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }

        private Color GetDrawColour(int i, int j, Color colour)
        {
            int colType = Main.tile[i, j].TileColor;
            Color paintCol = WorldGen.paintColor(colType);
            if (colType >= 13 && colType <= 24)
            {
                colour.R = (byte)(paintCol.R / 255f * colour.R);
                colour.G = (byte)(paintCol.G / 255f * colour.G);
                colour.B = (byte)(paintCol.B / 255f * colour.B);
            }
            return colour;
        }
        #endregion
    }
}
