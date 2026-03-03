using NetEasy;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumBulkContainerItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Bulk Container");
            Tooltip.SetDefault("A giant storage crate that can store the contents of up to 3 chests");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumBulkContainer>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 10, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(20).
                AddRecipeGroup("IronBar", 2).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumBulkContainer : ModTile, ICustomPaintable, IModdedContainer
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);


        public static readonly SoundStyle OpenSound = new(SoundDirectory.Wulfrum + "WulfrumBulkStorageOpen");
        public static readonly SoundStyle CloseSound = new(SoundDirectory.Wulfrum + "WulfrumBulkStorageClose");

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public static Asset<Texture2D> GlowMask;
        public static Asset<Texture2D> Highlight;
        public static int TileType;

        public static int[,] PanelItemTypes;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                GlowMask = Request<Texture2D>(Texture + "Glow");
                Highlight = Request<Texture2D>(Texture + "_CompartmentHighlight");
            }
        }

        public Module GetDestructionPacket(int x, int y) => new SyncWulfrumBulkContainerModule(Main.myPlayer, false, x, y, 1);

        public int StorageWidth => 1;
        public int StorageHeight => 2;

        //Unbreakable when full
        public bool TryDestroying(int x, int y)
        {
            bool pointless = false;
            if (!CanKillTile(x, y, ref pointless))
                return true;

            Point chestRowOrigin = FindChestRowOrigin(x, y);
            //If all 3 chests are breakable, break them.
            for (int i = 0; i < 3; i++)
            {
                Chest.DestroyChest(chestRowOrigin.X + i, chestRowOrigin.Y);
            }

            return false;
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            Point chestRowOrigin = FindChestRowOrigin(i, j);
            //Check all 3 chests to see if theyre even breakable
            for (int x = 0; x < 3; x++)
            {
                if (!Chest.CanDestroyChest(chestRowOrigin.X + x, chestRowOrigin.Y))
                    return false;
            }
            return true;
        }

        #region Placement
        public static int CanPlaceBulkContainer(int x, int y, int type = 21, int style = 0, int direction = 1, int alternate = 0)
        {
            int chestSlotsFound = 0;
            Point16 baseCoords = new Point16(x, y);
            TileObjectData.OriginToTopLeft(type, style, ref baseCoords);

            int chestRowOriginY = baseCoords.Y + 1; //Chests are 1 tile below the top of the container
            int chestRowOriginX = baseCoords.X + (direction == 1 ? 1 : 3); //Either 1 tile to the left or 3 tiles to the left of the left side

            for (int i = 0; i < 8000; i++)
            {
                Chest chest = Main.chest[i];
                if (chest != null)
                {
                    if ((chest.x >= chestRowOriginX && chest.x < chestRowOriginX + 3) && chest.y == chestRowOriginY) //If theres a chest where we want to place it already, we cant
                        return -1;
                }

                //Count up if we found an empty chest slot!
                else
                    chestSlotsFound++;
            }

            //Return -1 if theres less than 3 chest slots available (cant place)
            if (chestSlotsFound < 3)
                return -1;
            return chestSlotsFound;
        }

        public static int PostPlaceBulkContainer(int x, int y, int type = 21, int style = 0, int direction = 1, int alternate = 0)
        {
            Point16 baseCoords = new Point16(x, y);
            TileObjectData.OriginToTopLeft(type, style, ref baseCoords);

            int chestRowOriginY = baseCoords.Y + 1; //Chests are 1 tile below the top of the container
            int chestRowOriginX = baseCoords.X + (direction == 1 ? 1 : 3); //Either 1 tile to the left or 3 tiles to the left of the left side

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Create 3 chests (Only done on singleplayer client)
                PlaceThreeContainers(chestRowOriginX, chestRowOriginY);
            }
            else
            {
                var packet = new SyncWulfrumBulkContainerModule(Main.myPlayer, true, x, y, direction);
                packet.Send(-1, -1, false);
            }

            return 1;
        }

        public static int[] PlaceThreeContainers(int rowX, int rowY)
        {
            int[] chestIDs = new int[3];
            for (int i = 0; i < 3; i++)
            {
                chestIDs[i] = Chest.CreateChest(rowX + i, rowY);
            }

            return chestIDs;
        }

        public static int[] FindTheThreeContainers(int rowX, int rowY)
        {
            int[] chestIDs = new int[3];

            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest chest = Main.chest[i];
                if (chest != null)
                {
                    if ((chest.x >= rowX && chest.x < rowX + 3) && chest.y == rowY)
                        chestIDs[chest.x - rowX] = i;
                }
            }

            return chestIDs;

        }
        #endregion

        public static Point FindChestRowOrigin(int x, int y)
        {
            Tile tile = Main.tile[x, y];

            int topY = y - tile.TileFrameY / 18; //Get to the top of the tile
            topY++; //The chest row starts 1 tile down from the top of the tile

            bool flipped = tile.TileFrameX >= 18 * 7;
            int leftX = x - ((tile.TileFrameX / 18) % 7); //Get the left side of the tile

            if (flipped)
                leftX += 3;
            else
                leftX += 1;

            return new Point(leftX, topY);
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            TileType = Type;

            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileSolidTop[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileContainer[Type] = true;
            TileID.Sets.BasicChest[Type] = true;
            Main.tileTable[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.IsAContainer[Type] = true;
            TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style5x4);
            TileObjectData.newTile.Width = 7;
            TileObjectData.newTile.DrawYOffset = 0;
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 18];
            TileObjectData.newTile.Origin = new Point16(3, 3);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 7, 0);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.StyleMultiplier = 2;

            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(CanPlaceBulkContainer, -1, 0, processedCoordinates: true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(PostPlaceBulkContainer, -1, 0, processedCoordinates: false);
            TileObjectData.newTile.AnchorInvalidTiles = new int[3] {
                127, //Ice rod ice blocks
                138, //Boulders
                484 //Rolling cacti
            };

            TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight; // Player faces to the left

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; // Player faces to the right
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Bulk Storage");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomContainer[Type] = true;
        }

        public override LocalizedText DefaultContainerName(int frameX, int frameY)
        {
            LocalizedText text = this.GetLocalization("ContainerName", () => "Wulfrum Bulk Container");
            //text.SetDefault("Wulfrum Locker");
            return text;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = 1;

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => ModdedContainers.PreventAutodestructionTileFrame(Type, i, j, ref resetFrame, ref noBreak);

        #region Hovering / Opening

        public bool IsChestThere(Tile t)
        {
            if (t.TileFrameY != 18)
                return false;

            int rowStart = t.TileFrameX >= 18 * 7 ? 3 : 1;
            int column = (t.TileFrameX / 18) % 7;
            return column >= rowStart && column < rowStart + 3;
        }

        public override void MouseOver(int i, int j) => Hovering(i, j);
        public override void MouseOverFar(int i, int j) => Hovering(i, j, true);

        public static void Hovering(int i, int j, bool far = false)
        {
            Player player = Main.LocalPlayer;
            Point rowOrigin = FindChestRowOrigin(i, j);

            //Hover icons only accessible from the middle 2 rows
            if (j < rowOrigin.Y || j > rowOrigin.Y + 1)
                return;
            if (i < rowOrigin.X || i > rowOrigin.X + 2)
                return;

            //Using the row origin for the Y coordinates, cuz its 2 high
            int chest = Chest.FindChest(i, rowOrigin.Y);
            if (chest == -1)
                return;

            player.cursorItemIconEnabled = false;

            //Display the chest name if it has one
            if (Main.chest[chest].name.Length > 0)
            {
                player.cursorItemIconText = Main.chest[chest].name;
                player.cursorItemIconID = -1;
            }

            //Display the icon if far
            else if (!far)
            {
                int bulkStorageIndex = i - rowOrigin.X;
                int fillState = FablesUtils.IsChestFull(Main.chest[chest], out float fillPercent) ? 2 : 0;
                if (fillState == 0 && fillPercent > 0f)
                    fillState = 1;


                if (PanelItemTypes == null)
                {
                    PanelItemTypes = new int[3, 3]
                    { { ItemType<WulfrumBulkContainer_FirstSlotEmpty>(), ItemType<WulfrumBulkContainer_FirstSlotUsed>(), ItemType<WulfrumBulkContainer_FirstSlotFull>() },
                      { ItemType<WulfrumBulkContainer_SecondSlotEmpty>(), ItemType<WulfrumBulkContainer_SecondSlotUsed>(), ItemType<WulfrumBulkContainer_SecondSlotFull>() },
                      { ItemType<WulfrumBulkContainer_ThirdSlotEmpty>(), ItemType<WulfrumBulkContainer_ThirdSlotUsed>(), ItemType<WulfrumBulkContainer_ThirdSlotFull>() }
                    };
                }

                int itemType = PanelItemTypes[bulkStorageIndex, fillState];
                player.cursorItemIconID = itemType;
                player.cursorItemIconText = "";
                player.cursorItemIconEnabled = true;
            }


            player.noThrow = 2;
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Point rowOrigin = FindChestRowOrigin(Player.tileTargetX, Player.tileTargetY);

            //Chests only accessible from the middle 2 rows
            if (j < rowOrigin.Y || j > rowOrigin.Y + 1)
                return false;
            if (i < rowOrigin.X || i > rowOrigin.X + 2)
                return false;

            Main.CancelClothesWindow(true);

            int left = i;
            int top = rowOrigin.Y;

            if (player.sign > -1)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                player.sign = -1;
                Main.editSign = false;
                Main.npcChatText = string.Empty;
            }
            if (Main.editChest)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = string.Empty;
            }
            if (player.editedChestName)
            {
                NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f, 0f, 0f, 0, 0, 0);
                player.editedChestName = false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (left == player.chestX && top == player.chestY && player.chest != -1)
                {
                    player.chest = -1;
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(SoundID.MenuClose);
                }
                else
                {
                    NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, left, (float)top, 0f, 0f, 0, 0, 0);
                    Main.stackSplit = 600;
                }
                return true;
            }

            else
            {
                player.piggyBankProjTracker.Clear();
                player.voidLensChest.Clear();
                int chestToOpen = Chest.FindChest(left, top);
                if (chestToOpen != -1)
                {
                    Main.stackSplit = 600;
                    if (chestToOpen == player.chest) //Close the chest if it was clicked on
                    {
                        player.chest = -1;
                        Recipe.FindRecipes();
                        SoundEngine.PlaySound(CloseSound with { Volume = 0.7f, PitchVariance = 0.2f });
                    }
                    else if (chestToOpen != player.chest) //Open the chest
                    {
                        player.OpenChest(left, top, chestToOpen);
                        SoundEngine.PlaySound(OpenSound with { Volume = 0.6f, PitchVariance = 0.2f });
                    }
                    Recipe.FindRecipes();
                    return true;
                }
            }


            return false;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            Point baseCoords = FindChestRowOrigin(i, j);

            if (j != baseCoords.Y)
                return false;

            int lateralOffset = i - baseCoords.X;
            if (lateralOffset >= 0 && lateralOffset < 3)
                return settings.player.IsInInteractionRangeToMultiTileHitbox(i, j);
            return false;
        }

        public override void ModifySmartInteractCoords(ref int width, ref int height, ref int frameWidth, ref int frameHeight, ref int extraY)
        {
            width = 1;
            height = 1;
        }
        #endregion

        #region Light and glow
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile tile = Main.tile[i, j];
            r = 0;
            g = 0;
            b = 0;

            int verticalFrame = (int)(tile.TileFrameY / 18);
            if (verticalFrame == 0 || verticalFrame == 3)
                return;

            int horizontalFrame = (int)(tile.TileFrameX / 18);
            if (horizontalFrame != 2 && horizontalFrame != 3 && horizontalFrame != 10 && horizontalFrame != 11)
                return;

            float colorMult = 1 / 265f * 0.5f * CommonColors.WulfrumLightMultiplier;
            Color lightColor = CommonColors.WulfrumGreen;

            r = lightColor.R * colorMult;
            g = lightColor.G * colorMult;
            b = lightColor.B * colorMult;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY;

            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            Color drawColour = GetDrawColour(i, j, Color.White);
            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);

            if (yPos != 36)
                return;

            int tileSelectionTier = 0;
            if (Main.InSmartCursorHighlightArea(i, j - 1, out var actuallySelected))
            {
                tileSelectionTier = 1;
                if (actuallySelected)
                    tileSelectionTier = 2;
            }

            //Draw the selection glow
            if (tileSelectionTier != 0)
            {
                int averageBrightness = (int)(Lighting.GetColor(new Point(i, j)).GetBrightness() * 256);
                if (averageBrightness > 10)
                {
                    //Use the vanilla crystal sheet to get the autoselect outline
                    Color selectionGlowColor = Colors.GetSelectionGlowColor(tileSelectionTier == 2, averageBrightness);
                    spriteBatch.Draw(Highlight.Value, drawOffset + new Vector2(2, -6), null, selectionGlowColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
            }
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

    #region Panel items
    internal class WulfrumBulkContainer_FirstSlotEmpty : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }
    internal class WulfrumBulkContainer_FirstSlotUsed : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }
    internal class WulfrumBulkContainer_FirstSlotFull : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }

    internal class WulfrumBulkContainer_SecondSlotEmpty : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }
    internal class WulfrumBulkContainer_SecondSlotUsed : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }
    internal class WulfrumBulkContainer_SecondSlotFull : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }

    internal class WulfrumBulkContainer_ThirdSlotEmpty : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }
    internal class WulfrumBulkContainer_ThirdSlotUsed : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }
    internal class WulfrumBulkContainer_ThirdSlotFull : WulfrumBulkContainer_Panel
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
    }


    internal abstract class WulfrumBulkContainer_Panel : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bulk Container Slot");
            Tooltip.SetDefault("You shouldn't be seeing this");
            Item.ResearchUnlockCount = 0;
            ItemID.Sets.Deprecated[Type] = true;
            ItemID.Sets.ItemsThatShouldNotBeInInventory[Type] = true;
        }
    }
    #endregion

    [Serializable]
    public class SyncWulfrumBulkContainerModule : Module
    {
        public readonly byte whoAmI;
        public readonly bool place;
        public bool unplace;
        public readonly int direction;
        public int x;
        public int y;
        public readonly int tileType;
        public readonly int itemType;

        public int[] chestIDs;

        public SyncWulfrumBulkContainerModule(int whoAmI, bool place, int x, int y, int direction)
        {
            this.whoAmI = (byte)whoAmI;
            this.place = place;
            this.unplace = false;
            this.x = x;
            this.y = y;
            this.direction = direction;
            this.tileType = ModContent.TileType<WulfrumBulkContainer>();
            this.itemType = ModContent.ItemType<WulfrumBulkContainerItem>();
            this.chestIDs = new int[3];
        }

        public Point GetChestRowOrigin()
        {
            Point16 baseCoords = new Point16(x, y);
            TileObjectData.OriginToTopLeft(tileType, 0, ref baseCoords);

            int chestRowOriginX = baseCoords.X + (direction == 1 ? 1 : 3); //Chests are 1 tile below the top of the container
            int chestRowOriginY = baseCoords.Y + 1; //Either 1 tile to the left or 3 tiles to the left of the left side
            return new Point(chestRowOriginX, chestRowOriginY);
        }

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                //Trying to place
                if (place)
                {
                    TileObjectData data = TileObjectData.GetTileData(tileType, 0);
                    bool placeSuccess = TileObject.CanPlace(x, y, tileType, 0, direction, out TileObject objectData);
                    CalamityFables.Instance.Logger.Debug("Trying to place the bulk container: Result of CanPlace check " + placeSuccess.ToString());

                    if (true)
                    {
                        TileObject.Place(objectData);
                        //Create the 3 chests
                        Point rowOrigin = GetChestRowOrigin(); //Find the row origin from the top left
                        chestIDs = WulfrumBulkContainer.PlaceThreeContainers(rowOrigin.X, rowOrigin.Y);

                        //If one of the chest spots was already taken
                        if (chestIDs.Contains(-1))
                        {
                            placeSuccess = false;
                            CalamityFables.Instance.Logger.Debug("Trying to place the bulk container: We could place the tile, but there already exists a chest in the coordinates. Unplacing");

                            for (int i = 0; i < chestIDs.Length; i++)
                            {
                                //remove all chests we placed
                                if (chestIDs[i] != -1)
                                    Chest.DestroyChestDirect(rowOrigin.X + i, rowOrigin.Y, chestIDs[i]);
                            }
                        }
                        //Send to all other clients the request to place the chest
                        else
                            Send(-1, -1, false);
                    }
                    if (!placeSuccess) //If we couldn't place the tile, tell the client that placed it to break it
                    {
                        unplace = true;
                        Send(whoAmI, -1, false);
                        Item.NewItem(new EntitySource_TileBreak(x, y), x * 16, y * 16, 32, 32, itemType, 1, noBroadcast: true); //Drops the item
                    }
                }
                //Trying to break
                else
                {
                    Tile tile = Main.tile[x, y];
                    Point rowOrigin = WulfrumBulkContainer.FindChestRowOrigin(x, y); //Get the row origin from wherever on the tile
                    chestIDs = WulfrumBulkContainer.FindTheThreeContainers(rowOrigin.X, rowOrigin.Y);

                    CalamityFables.Instance.Logger.Debug("Recieved a destruction request for a bulk container");
                    WorldGen.KillTile(x, y); //Break the chest (attempt to)
                    //If we successfully broke the tile, share the destruction to all other clients
                    if (!tile.HasTile)
                    { 
                        Send(-1, -1, false);
                        CalamityFables.Instance.Logger.Debug("Successful broke a bulk contaner");
                    }
                }
            }

            else
            {
                //Server request to break the tile that was just placed
                if (unplace)
                {
                    WorldGen.KillTile(x, y);
                    return;
                }

                if (place)
                {
                    SoundEngine.PlaySound(SoundID.Dig, new Vector2(x, y) * 16f); //Play a sound
                    //Create 3 chests
                    Point rowOrigin = GetChestRowOrigin(); //Get the row originf rom the top left
                    for (int i = 0; i < 3; i++)
                    {
                        chestIDs[i] = Chest.CreateChest(rowOrigin.X + i, rowOrigin.Y, chestIDs[i]);
                    }

                    PlaceContainerDirect();
                }
                else
                {
                    //Find the row origin from wherever on the tile
                    Point rowOrigin = WulfrumBulkContainer.FindChestRowOrigin(x, y);
                    for (int i = 0; i < 3; i++)
                    {
                        Chest.DestroyChestDirect(rowOrigin.X + i, rowOrigin.Y, chestIDs[i]);
                    }

                    WorldGen.KillTile(x, y);
                }
            }
        }

        public void PlaceContainerDirect()
        {
            Point16 baseCoords = new Point16(x, y);
            TileObjectData.OriginToTopLeft(tileType, 0, ref baseCoords);
            x = baseCoords.X;
            y = baseCoords.Y;

            TileObjectData data = TileObjectData.GetTileData(tileType, 0); //magic numbers and uneccisary params begone!

            if (x + data.Width > Main.maxTilesX || x < 0) return; //make sure we dont spawn outside of the world!
            if (y + data.Height > Main.maxTilesY || y < 0) return;

            int widthOffset = 0;
            if (direction == -1)
                widthOffset += data.Width;

            for (int rx = 0; rx < data.Width; rx++) //generate each column
            {
                for (int ry = 0; ry < data.Height; ry++) //generate each row
                {
                    Tile tile = Framing.GetTileSafely(x + rx, y + ry); //get the targeted tile
                    tile.IsHalfBlock = false;
                    tile.Slope = SlopeType.Solid;
                    tile.TileType = (ushort)tileType; //set the type of the tile to our multitile

                    tile.TileFrameX = (short)((rx + widthOffset) * (data.CoordinateWidth + data.CoordinatePadding)); //set the X frame appropriately
                    //tile.TileFrameY = (short)((y + data.Height * yVariants) * (data.CoordinateHeights[1] + data.CoordinatePadding)); <= Doesn't work lmao! does some ugly corruption shit
                    tile.TileFrameY = (short)(ry * (data.CoordinateHeights[0] + data.CoordinatePadding)); //set the Y frame appropriately
                    tile.HasTile = true; //activate the tile
                }
            }
        }
    }
}
