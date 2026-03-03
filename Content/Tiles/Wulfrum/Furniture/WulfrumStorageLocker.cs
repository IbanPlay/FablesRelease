using CalamityFables.Content.Tiles.WulfrumScrapyard;
using NetEasy;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumLockerItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Storage Locker");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumLocker>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 2, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(8).
                AddRecipeGroup("IronBar", 2).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumLocker : ModTile, ICustomPaintable, IModdedContainer
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
            Main.tileContainer[Type] = true;
            TileID.Sets.BasicChest[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Origin = new(1, 1);
            TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, 2).ToArray();
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = false;

            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, processedCoordinates: true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(PostPlaceHook, -1, 0, processedCoordinates: false);

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Storage Locker");
            AddMapEntry(CommonColors.WulfrumLeatherRed, name);
            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomContainer[Type] = true;
            FablesSets.WallMountedContainer[Type] = true;
        }

        #region Container
        public override LocalizedText DefaultContainerName(int frameX, int frameY)
        {
            LocalizedText text = this.GetLocalization("ContainerName", () => "Wulfrum Locker");
            //text.SetDefault("Wulfrum Locker");
            return text;
        }

        public static void GetRowOrigin(ref int i, ref int j)
        {
            Tile tile = Main.tile[i, j];
            i -= tile.TileFrameX / 18;
            j -= tile.TileFrameY / 18;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);
        public override void MouseOver(int i, int j) => Hovering(i, j);
        public override void MouseOverFar(int i, int j) => Hovering(i, j, true);
        public static void Hovering(int i, int j, bool far = false)
        {
            GetRowOrigin(ref i, ref j);
            if (!far)
                FablesUtils.ChestMouseOver<WulfrumLockerItem>(i, j);
            else
                FablesUtils.ChestMouseFar<WulfrumLockerItem>(i, j);
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            GetRowOrigin(ref i, ref j);
            return FablesUtils.ChestRightClick(i, j);
        }

        public bool IsChestThere(Tile t) => t.TileFrameY == 0 && t.TileFrameX == 0;

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => ModdedContainers.PreventAutodestructionTileFrame(Type, i, j, ref resetFrame, ref noBreak);


        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            GetRowOrigin(ref i, ref j);
            return Chest.CanDestroyChest(i, j);
        }

        public bool TryDestroying(int x, int y)
        {
            GetRowOrigin(ref x, ref y);
            return !Chest.DestroyChest(x, y);
        }

        public static int PostPlaceHook(int x, int y, int type = 21, int style = 0, int direction = 1, int alternate = 0)
        {
            GetRowOrigin(ref x, ref y);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int chest = Chest.CreateChest(x, y);
            }
            else
            {
                var packet = new SyncGenericContainerModule(Main.myPlayer, true, x, y, direction, type, ItemType<WulfrumLockerItem>());
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
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            GetRowOrigin(ref i, ref j);
            int chestIndex = Chest.FindChest(i, j);
            if (chestIndex == -1)
                return;

            Chest chest = Main.chest[chestIndex];
            frameYOffset += 36 * chest.frame;
            FablesUtils.IsChestFull(chest, out float percent);
            if (percent == 0)
                return;
            if (percent > 0)
                frameXOffset += 36;
            if (percent >= 0.5)
                frameXOffset += 36;
            if (percent == 1)
                frameXOffset += 36;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            float squareWave = (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 4) * 0.99f + 1) + 0.2f;

            Tile tile = Main.tile[i, j];
            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY;
            AnimateIndividualTile(tile.TileType, i, j, ref xPos, ref yPos);

            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            Color drawColour = GetDrawColour(i, j, Color.White);
            drawColour *= squareWave;

            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
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
