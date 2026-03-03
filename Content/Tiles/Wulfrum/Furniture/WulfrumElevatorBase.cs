using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.WulfrumScrapyard;
using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Bestiary;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    #region Tile
    public class WulfrumElevatorBaseItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Elevator Base");
            Tooltip.SetDefault("Connects with elevator rails and elevator stations");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumElevatorBase>());
            Item.maxStack = 9999;
            Item.value = 0;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(20).
                AddIngredient<EnergyCore>().
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumElevatorBase : ModTile, ICustomPaintable, ICustomLayerTile
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public static Asset<Texture2D> ButtonHighlights;

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void Load()
        {
            if (!Main.dedServ)
                ButtonHighlights = Request<Texture2D>(AssetDirectory.WulfrumFurniture + "WulfrumElevator_Highlights");
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileTable[Type] = true;
            Main.tileSolidTop[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Width = 5;
            TileObjectData.newTile.Origin = new Point16(2, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };

            //TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(pylonHook.PlacementPreviewHook_CheckIfCanPlace, 1, 0, true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(ModContent.GetInstance<WulfrumElevatorController>().Hook_AfterPlacement, -1, 0, false);

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

            TileObjectData.addTile(Type);
            //Counts as "door"
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AddMapEntry(CommonColors.WulfrumMetalDark);
            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.ForceHouseWall[Type] = true;
            WulfrumElevatorRail.ElevatorBaseType = Type;
            WulfrumElevatorController._elevatorBaseType = Type;
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
            if (tile.TileFrameX % 36 != 0 || tile.TileFrameX >= 18 * 3)
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
            Tile t = Main.tile[i, j];
            if (!t.IsTileInvisible)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.AboveTiles, true);
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y);

            Texture2D drawTexture = TextureAssets.Tile[Type].Value;
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
            spriteBatch.Draw(drawTexture, drawOffset - Vector2.UnitY * 2, new Rectangle(tile.TileFrameX, rectStartY, 18, 18), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {

            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX % 36 != 0 || tile.TileFrameX >= 18 * 3 || tile.TileFrameY == 0)
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

            Texture2D highlight = ButtonHighlights.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            drawOffset += new Vector2(8, -8);
            if (direction == 0)
                drawOffset.Y -= 2;
            else
                drawOffset.X += 1 * direction;


            Rectangle frame = direction == 0 ? new Rectangle(12, 0, 16, 16) : new Rectangle(0 + (direction < 0 ? 30 : 0), 0, 10, 12);
            Color color = Main.OurFavoriteColor * Lighting.GetColor(i, j).GetBrightness();

            Main.spriteBatch.Draw(highlight, drawOffset, frame, color, 0f, new Vector2(frame.Width / 2f, 0), 1f, SpriteEffects.None, 0.0f);
        }

        public override void HitWire(int i, int j) => WulfrumElevatorController.StationWireInteraction(i, j);
    }
    #endregion

    #region TE
    public class WulfrumElevatorController : ModTileEntity
    {
        private static Dictionary<int, Rectangle> playerPositions = new Dictionary<int, Rectangle>();
        private static bool registeredPlayerPositions;

        public static int _elevatorBaseType;
        public static int _elevatorStationType;
        public static int _elevatorRailType;
        public static int _platformType;
        public static readonly int MAX_RAIL_HEIGHT = 200;

        public static readonly SoundStyle InteractionFailSound = new(SoundDirectory.Wulfrum + "WulfrumElevatorInvalidInteraction") { Volume = 0.4f };

        public virtual int PlatformType => _platformType;
        public virtual int ElevatorBaseType => _elevatorBaseType;
        public virtual int ElevatorRailType => _elevatorRailType;
        public virtual int ElevatorStationType => _elevatorStationType;

        public Vector2 WorldPosition => Position.ToVector2() * 16;
        public int platform = -1;
        public NPC Platform => Main.npc[platform];
        public int railHeight;
        public int stations;
        public int previousStations = -1;
        public List<int> stationHeights = new List<int>();

        public int _lastPlatformAnchorStation;
        public int PlatformAnchorStation {
            get => (int)Main.npc[platform].ai[0];
            set => Main.npc[platform].ai[0] = value;
        }
        public int PlatformDestinationStation {
            get => (int)Main.npc[platform].ai[1];
            set => Main.npc[platform].ai[1] = value;
        }

        public float PlatformTargetHeight
        {
            get => Main.npc[platform].ai[3];
            set => Main.npc[platform].ai[3] = value;
        }

        public bool PlatformIsMoving => PlatformAnchorStation != PlatformDestinationStation;

        //Dummies use the same thing to only register player hitboxes once
        public override void Load()
        {
            _UpdateStart += ClearBoxes;
        }

        #region Platform behavior
        public override void Update()
        {
            //If we have a platform but the platform isnt valid, deactivate
            if (platform != -1 && !HasValidPlatform)
                platform = -1;

            RecalculateRailData();
            if (HasValidPlatform)
            {
                bool platformNeedsSyncing = false;
                _lastPlatformAnchorStation = PlatformAnchorStation;

                int previousPaintColor = (Platform.ModNPC as WulfrumElevatorPlatform).paintColor;
                (Platform.ModNPC as WulfrumElevatorPlatform).paintColor = Main.tile[Position.X, Position.Y].TileColor; //Set the platforms paint
                if (previousPaintColor != Main.tile[Position.X, Position.Y].TileColor)
                    platformNeedsSyncing = true;

                if (PlatformAnchorStation > stations)
                {
                    //break the platform (It will reappear at the end
                    Platform.active = false;
                    platform = -1;
                }
                //moving platform
                else if (PlatformIsMoving)
                {
                    //Clamp to highest station if the top of the stations got broken midway through the ascent
                    if (PlatformDestinationStation > stations)
                    {
                        PlatformDestinationStation = stations;
                        PlatformTargetHeight = GetFloorWorldPosition(PlatformDestinationStation).Y;
                        platformNeedsSyncing = true;
                    }

                    bool goingUp = PlatformDestinationStation > PlatformAnchorStation;
                    //if we are moving up, we only count the rails ABOVE the destination platform to count as part of that station
                    //if we are moving down, we only count the station below us and the rails under it as part of the station
                    //https://media.discordapp.net/attachments/802291445360623686/1081749726938013746/image.png
                    int platformStation = GetPlatformStationBasedOnPosition(!goingUp);

                    //Reaching the stop point
                    if ((!goingUp && platformStation <= PlatformDestinationStation) || (goingUp && platformStation >= PlatformDestinationStation))
                    {
                        PlatformAnchorStation = PlatformDestinationStation;
                        Platform.Center = GetFloorWorldPosition(PlatformAnchorStation);
                        PlatformTargetHeight = Platform.Center.Y;
                        Platform.velocity.Y = 0;
                        new WulfrumElevatorPlatformPacket(Platform, true).Send(-1, -1, false);
                        SoundEngine.PlaySound(WulfrumAcrobaticsPack.GrabSound, Platform.Center);

                        platformNeedsSyncing = false;
                    }
                }

                if (platformNeedsSyncing)
                    new WulfrumElevatorPlatformPacket(Platform).Send(-1, -1, false);
            }

            FillPlayerHitboxes();
            bool nearbyPlayer = PlayerNearElevatorShaft(2800);

            if (nearbyPlayer && !HasValidPlatform)
                RespawnPlatform();
            else if (!nearbyPlayer)
                Deactivate();

            if (!IsTileValidForEntity(Position.X, Position.Y))
            {
                Deactivate();
                Kill(Position.X, Position.Y);
            }
        }

        public int GetPlatformStationBasedOnPosition(bool reverse)
        {
            float heightInElevator = Position.Y * 16 - Platform.Center.Y;
            if (stations == 0)
                return 0;

            //"Not reversed" means that any rails above a station still count as that station, until we reach the next one above.
            if (!reverse)
            {
                for (int i = 1; i < stationHeights.Count; i++)
                {
                    if (heightInElevator < stationHeights[i] * 16)
                        return i - 1;
                }
            }
            //"Reversed" means that any rail below a station still counts as that station, until we reach the next one below
            else
            {
                for (int i = 0; i < stationHeights.Count; i++)
                {
                    if (heightInElevator <= stationHeights[i] * 16)
                        return i;
                }
            }

            return stations;
        }

        public Vector2 GetFloorWorldPosition(int floor)
        {
            if (stationHeights.Count <= floor)
                RecalculateRailData();
            return WorldPosition + Vector2.UnitX * 8 - Vector2.UnitY * (16 * stationHeights[floor]);
        }
        #endregion

        #region player hitbox check
        private static void FillPlayerHitboxes()
        {
            if (registeredPlayerPositions)
                return;

            for (int i = 0; i < 255; i++)
            {
                if (Main.player[i].active)
                    playerPositions[i] = Main.player[i].getRect();
            }

            registeredPlayerPositions = true;
        }
        public static void ClearBoxes()
        {
            playerPositions.Clear();
            registeredPlayerPositions = false;
        }
        public bool PlayerNearElevatorShaft(int paddingSize)
        {
            Rectangle nearbyArea = new Rectangle((int)WorldPosition.X - paddingSize / 2, (int)WorldPosition.Y - paddingSize / 2 - railHeight * 16, paddingSize, paddingSize + railHeight * 16);

            foreach (KeyValuePair<int, Rectangle> item in playerPositions)
            {
                if (item.Value.Intersects(nearbyArea))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Placement

        public override void OnNetPlace()
        {
            NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
        }

        public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
        {
            TileObjectData tileData = TileObjectData.GetTileData(type, style, alternate);
            int num = i - tileData.Origin.X;
            int num2 = j - tileData.Origin.Y;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(Main.myPlayer, num, num2, tileData.Width, tileData.Height);
                NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j - 1, Type);
                return -1;
            }

            return Place(i, j - 1);
        }

        #endregion

        #region Stat calculation & spawning / despawning the platform
        public override bool IsTileValidForEntity(int x, int y)
        {
            return Main.tile[x, y].HasTile && Main.tile[x, y].TileType == ElevatorBaseType && Main.tile[x, y].TileFrameY == 0 && Main.tile[x, y].TileFrameX == 36;
        }

        public void RecalculateRailData()
        {
            stationHeights = [0];
            stations = 0;
            railHeight = 0;

            Point basePosition = Position.ToPoint();
            basePosition.Y--;
            int check = 0;
            bool justOutOfTheStation = true;

            while (check < MAX_RAIL_HEIGHT)
            {
                //Dust.QuickDust(basePosition, Color.Red);

                //Inbounds check
                if (basePosition.Y < 0)
                    break;

                Tile tile = Main.tile[basePosition];
                if (!tile.HasTile)
                    break;
                if (tile.TileType == ElevatorRailType && tile.TileFrameX == 18)
                {
                    railHeight++;
                    justOutOfTheStation = false;
                }

                else if (tile.TileType == ElevatorStationType && tile.TileFrameX == 36 && !justOutOfTheStation)
                {
                    stationHeights.Add(Position.Y - basePosition.Y + 1);
                    railHeight += 2;
                    stations++;
                    basePosition.Y--; //Skip over the second half of the station
                    //check++;
                    check = 0; //Refresh check height
                    justOutOfTheStation = true;
                }

                else
                    break;

                basePosition.Y--;
                check++;
            }

            if (previousStations != stations && Main.dedServ)
                new SyncWulfrumElevatorStationCount(this, stations).Send(runLocally:false);
            previousStations = stations;
        }

        public bool HasValidPlatform => platform != -1 && Main.npc[platform].active && Main.npc[platform].type == PlatformType;

        public void RespawnPlatform()
        {
            int spawnFloor = _lastPlatformAnchorStation;
            //If the last station was above the current station count (aka the elevator was broken up), clamp the respawn height
            if (_lastPlatformAnchorStation > stations)
                spawnFloor = stations;

            Vector2 spawnPosition = GetFloorWorldPosition(spawnFloor);

            int platformID = NPC.NewNPC(new EntitySource_TileEntity(this), (int)spawnPosition.X, (int)spawnPosition.Y + 16, PlatformType, 0, spawnFloor, spawnFloor);
            Main.npc[platformID].Center = spawnPosition;
            Main.npc[platformID].netUpdate = true;
            platform = platformID;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
        }

        public void Deactivate()
        {
            if (platform != -1)
                Main.npc[platform].active = false;

            platform = -1;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
        }
        #endregion

        #region Tile interaction helpers
        public static WulfrumElevatorController GetControllerAtPosition(int i, int j, out int floor)
        {
            Tile tile = Main.tile[i, j];

            j -= tile.TileFrameY / 18;
            i -= (tile.TileFrameX - 36) / 18;

            Point basePosition = new Point(i, j);
            int check = 0;
            floor = 0;

            int baseType = _elevatorBaseType;
            int railType = _elevatorRailType;
            int stationType = _elevatorStationType;
            if (tile.TileType == RustyWulfrumElevatorController._elevatorBaseType || tile.TileType == RustyWulfrumElevatorController._elevatorStationType)
            {
                baseType = RustyWulfrumElevatorController._elevatorBaseType;
                railType = RustyWulfrumElevatorController._elevatorRailType;
                stationType = RustyWulfrumElevatorController._elevatorStationType;
            }

            while (check < MAX_RAIL_HEIGHT)
            {
                //Inbounds check
                if (basePosition.Y >= Main.maxTilesY)
                    break;

                tile = Main.tile[basePosition];

                check++;
                basePosition.Y++;

                if (!tile.HasTile)
                    break;
                if ((tile.TileType == railType && tile.TileFrameX == 18))
                    continue;
                if (tile.TileType == stationType && tile.TileFrameX == 36)
                {
                    floor++;
                    basePosition.Y++;
                    //check++;
                    check = 0;
                    continue;
                }

                if (tile.TileType == baseType && tile.TileFrameX == 36)
                {
                    basePosition.Y--;
                    if (ByPosition.TryGetValue(new Point16(basePosition), out TileEntity tileEntity))
                        return tileEntity as WulfrumElevatorController;
                }

                break;
            }

            return null;
        }

        public bool InteractAtFloor(int floor, int requestedDirection, bool fastTravelWhenSkippingFloors = true)
        {
            //Can't interact if the platform is moving or if the platform doesnt exist
            if (!HasValidPlatform || PlatformIsMoving)
                return false;

            //If the platform is requested to move at the floor we are at.
            if (requestedDirection == 0 && floor != PlatformAnchorStation)
            {
                PlatformDestinationStation = floor;
                PlatformTargetHeight = GetFloorWorldPosition(PlatformDestinationStation).Y;
                Platform.ai[2] = fastTravelWhenSkippingFloors ? 1 : 0;
                new WulfrumElevatorPlatformPacket(Platform).Send(-1, -1, false);
                return true;
            }
            else if (requestedDirection == 0)
                return false;

            //Can only interact with the platform to go up/down if the platform is at the station
            if (PlatformAnchorStation != floor)
            {
                SoundEngine.PlaySound(InteractionFailSound);
                return false;
            }

            int destination = floor + requestedDirection;
            if (destination >= 0 && destination <= stations)
            {
                PlatformDestinationStation = destination;
                PlatformTargetHeight = GetFloorWorldPosition(PlatformDestinationStation).Y;
                new WulfrumElevatorPlatformPacket(Platform).Send(-1, -1, false);
                return true;
            }

            SoundEngine.PlaySound(InteractionFailSound);
            return false;
        }

        public bool CanInteractAtFloor(int floor, int requestedDirection)
        {
            //Can't interact if the platform is moving or if the platform doesnt exist
            if (!HasValidPlatform || PlatformIsMoving)
                return false;

            //If the platform is requested to move at the floor we are at.
            if (requestedDirection == 0 && floor != PlatformAnchorStation)
                return true;
            else if (requestedDirection == 0)
                return false;

            //Can only interact with the platform to go up/down if the platform is at the station
            if (PlatformAnchorStation != floor)
                return false;

            int destination = floor + requestedDirection;
            if (destination >= 0 && destination <= stations)
                return true;

            return false;
        }

        public static bool StationInteraction(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            //Only odd columns have right clicks
            if (tile.TileFrameX % 36 != 0)
                return false;

            WulfrumElevatorController controller = GetControllerAtPosition(i, j, out int floor);
            if (controller == null)
                return false;

            //Elevator base only has the button to go up, no button to go down
            if (tile.TileType == controller.ElevatorBaseType && tile.TileFrameX >= 18 * 3)
                return false;

            int direction = ((tile.TileFrameX - 36) / 36) * -1;
            if (!controller.InteractAtFloor(floor, direction))
                return false;

            return true;
        }

        public static void StationWireInteraction(int i, int j)
        {
            WulfrumElevatorController controller = GetControllerAtPosition(i, j, out int floor);
            if (controller == null)
                return;

            if (!controller.InteractAtFloor(floor, 0, true))
                return;

            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 2; y++)
                    Wiring.SkipWire(i + x, j + y);
        }
        #endregion

        #region Saving and syncing
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((short)platform);
            writer.Write((short)stations);
        }

        public override void NetReceive(BinaryReader reader)
        {
            platform = reader.ReadInt16();
            stations = reader.ReadInt16();
        }
        #endregion
    }
    #endregion

    public class WulfrumElevatorPlatform : ModNPC, IMovingSurface
    {
        public virtual SoundStyle ClickingSound => NPCs.Wulfrum.WulfrumRoller.GearClick with { Identifier = "WulfElevator", SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        private List<Player> ridingPlayers = new List<Player>();
        public List<Player> RidingPlayers => ridingPlayers;

        public virtual int BaseType => WulfrumElevatorController._elevatorBaseType;
        public virtual int RailType => WulfrumElevatorController._elevatorRailType;
        public virtual int StationType => WulfrumElevatorController._elevatorStationType;

        public int FloorStation {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public int TravelToFloor {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public bool FastTravel {
            get => NPC.ai[2] == 1;
            set => NPC.ai[2] = value ? 1 : 0;
        }
        public float TargetWorldHeight
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        public int paintColor;

        public float previousHeight;

        public Vector2 LastPosition => new Vector2(NPC.position.X, previousHeight);

        public int previousFloor = -1;

        public bool Solid => !FastTravel || true;
        public bool DisableTopSurfaces => true;

        public override string Texture => AssetDirectory.WulfrumFurniture + "WulfrumElevator";
        public static readonly Dictionary<int, Asset<Texture2D>> PaintTextures = new Dictionary<int, Asset<Texture2D>>();

        public override bool? CanBeHitByProjectile(Projectile Projectile) => false;
        public override bool? CanBeHitByItem(Player Player, Item Item) => false;
        public override bool CheckActive() => false;
        public override bool? CanFallThroughPlatforms() => true;

        public sealed override void SetStaticDefaults()
        {
            DisplayName.SetDefault("");
            SafeStaticDefaults();
            this.HideFromBestiary();
        }

        public virtual void SafeStaticDefaults()
        {
            WulfrumElevatorController._platformType = Type;
        }

        public sealed override void SetDefaults()
        {
            NPC.lifeMax = 10;
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0; //very very important!!
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.netAlways = true;
            NPC.width = 48;
            NPC.height = 8;
            NPC.behindTiles = true;

            for (int k = 0; k < NPC.buffImmune.Length; k++)
            {
                NPC.buffImmune[k] = true;
            }
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            database.Entries.Remove(bestiaryEntry);
        }

        public sealed override void AI()
        {
            PlatformBehavior();

            SafeAI();

            previousFloor = TravelToFloor; 
            previousHeight = NPC.position.Y;
        }

        public virtual void SafeAI()
        {
            Lighting.AddLight(NPC.Center, new Vector3(0.3f, 0.6f, 0.4f) * Main.rand.NextFloat(0.95f, 1f));

            if (previousFloor != -1 && previousFloor != TravelToFloor)
            {
                SoundEngine.PlaySound(FastTravel ? WulfrumDroid.HurrySound : WulfrumDroid.RandomChirpSound, NPC.Center);

                Vector2 emoteDirection = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 0.7f);
                Particle emote = new WulfrumDroidEmote(NPC.Center + emoteDirection * 3f, emoteDirection * Main.rand.NextFloat(1f, 3f), Main.rand.Next(30, 65), Main.rand.NextFloat(1.4f, 2f));
                ParticleHandler.SpawnParticle(emote);
            }
        }

        public void PlatformBehavior()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && ShouldBreak())
            {
                NPC.StrikeInstantKill();
                NPC.active = false;
                return;
            }

            if (Main.netMode != NetmodeID.SinglePlayer && previousHeight != 0)
                MovingPlatformSystem.MultiplayerCollisionFailsafe(NPC, previousHeight);

            if (TravelToFloor == FloorStation)
                FastTravel = false;

            NPC.velocity.X = 0;
            float speed = 4f;

            float initialYVelocity = NPC.velocity.Y;

            if (TravelToFloor > FloorStation)
                NPC.velocity.Y = -speed;
            else if (TravelToFloor < FloorStation)
                NPC.velocity.Y = speed;
            else
                NPC.velocity.Y = 0;

            if (FastTravel)
                NPC.velocity.Y *= 1.75f;

            float ease = TravelToFloor < FloorStation ? 0.9f : 0.04f;
            NPC.velocity.Y = MathHelper.Lerp(initialYVelocity, NPC.velocity.Y, ease);

            if (NPC.velocity.Y != 0)
            {
                NPC.frameCounter++;
            }

            //Snap to the desired height if we would overshoot it. This is to avoid flinging the player off platform when going too fast
            if ((TravelToFloor > FloorStation && NPC.Center.Y + NPC.velocity.Y < TargetWorldHeight) ||
                (TravelToFloor < FloorStation && NPC.Center.Y + NPC.velocity.Y > TargetWorldHeight)
                )
            {
                NPC.Center = new Vector2(NPC.Center.X, TargetWorldHeight);
                NPC.velocity.Y = 0;
            }

            foreach (Player player in ridingPlayers)
            {
                player.gfxOffY = NPC.gfxOffY;
                player.velocity.Y = 0;
                player.jump = 0;
                player.fallStart = (int)(player.position.Y / 16f);
                player.position.Y = NPC.position.Y - player.height + 4;
                player.position += NPC.velocity;
            }

            if (NPC.frameCounter > 6)
            {
                SoundEngine.PlaySound(ClickingSound, NPC.Center);

                NPC.frameCounter = 0;
                NPC.frame.Y++;
                if (NPC.frame.Y >= 3)
                    NPC.frame.Y = 0;
            }
        }



        public bool ShouldBreak()
        {
            Point worldPosition = NPC.Center.ToTileCoordinates();
            if (worldPosition.Y < 0)
                return true;

            Tile myTile = Main.tile[worldPosition];
            if (!myTile.HasTile)
                return true;

            if (myTile.TileType == RailType && myTile.TileFrameX == 18)
                return false;
            if ((myTile.TileType == BaseType || myTile.TileType == StationType) && myTile.TileFrameX == 36)
                return false;

            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[Type].Value;
            if (CommonColors.WulfrumCustomPaintjob(paintColor))
            {
                if (PaintTextures.TryGetValue(paintColor, out var paintedTexture))
                    tex = paintedTexture.Value;
                else
                {
                    paintedTexture = Request<Texture2D>(CommonColors.WulfrumPaintName(paintColor, "WulfrumElevator"));
                    PaintTextures.Add(paintColor, paintedTexture);
                    tex = paintedTexture.Value;
                }
            }
            else if (paintColor != 0)
            {
                drawColor = GetDrawColour(paintColor, drawColor);
            }

            Rectangle frame = tex.Frame(1, 3, 0, NPC.frame.Y);
            if (FastTravel && false)
                drawColor *= 0.56f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);

            spriteBatch.Draw(tex, NPC.Center - screenPos, frame, drawColor, 0, new Vector2(tex.Width / 2, 0), 1, 0, 0);
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
                SoundEngine.PlaySound(SoundDirectory.CommonSounds.WulfrumNPCDeathSound, NPC.Center);
        }

        private Color GetDrawColour(int colType, Color colour)
        {
            Color paintCol = WorldGen.paintColor(colType);
            if (colType >= 13 && colType <= 24)
            {
                colour.R = (byte)(paintCol.R / 255f * colour.R);
                colour.G = (byte)(paintCol.G / 255f * colour.G);
                colour.B = (byte)(paintCol.B / 255f * colour.B);
            }
            return colour;
        }
    }

    [Serializable]
    public class WulfrumElevatorPlatformPacket : Module
    {
        byte whoAmI;
        int platformIndex;

        int fromFloor;
        int toFloor;
        int paintColor;
        float targetHeight;
        bool fastTravel;
        bool playArrivalSound;

        public WulfrumElevatorPlatformPacket(NPC npc, bool playArrivalSound = false)
        {
            whoAmI = (byte)Main.myPlayer;
            platformIndex = npc.whoAmI;

            fromFloor = (int)npc.ai[0];
            toFloor = (int)npc.ai[1];
            fastTravel = npc.ai[2] == 1;
            targetHeight = npc.ai[3];

            paintColor = (npc.ModNPC as WulfrumElevatorPlatform)?.paintColor ?? 0;

            this.playArrivalSound = playArrivalSound;
        }

        protected override void Receive()
        {
            NPC platform = Main.npc[platformIndex];
            platform.velocity.Y = 0;
            platform.ai[0] = fromFloor;
            platform.ai[1] = toFloor;
            platform.ai[2] = fastTravel ? 1 : 0;
            platform.ai[3] = targetHeight;

            if (platform.ModNPC is WulfrumElevatorPlatform wulfPlatform)
                wulfPlatform.paintColor = paintColor;

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
            else if (playArrivalSound)
                SoundEngine.PlaySound(WulfrumAcrobaticsPack.GrabSound, platform.Center);
        }
    }

    [Serializable]
    public class SyncWulfrumElevatorStationCount : Module
    {
        int tileEntityID;
        int stations;

        public SyncWulfrumElevatorStationCount(TileEntity te, int stationsCount)
        {
            tileEntityID = te.ID;
            stations = stationsCount;
        }

        protected override void Receive()
        {
            if (!TileEntity.ByID.TryGetValue(tileEntityID, out TileEntity elevatorController) || elevatorController is not WulfrumElevatorController wulfrumElevator)
                return;
            wulfrumElevator.stations = stations;
        }
    }

    #region Hover items
    internal class WulfrumElevatorHoverIndicatorUp : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("");
            Tooltip.SetDefault("You shouldn't be seeing this");
            Item.ResearchUnlockCount = 0;
            ItemID.Sets.Deprecated[Type] = true;
            ItemID.Sets.ItemsThatShouldNotBeInInventory[Type] = true;
        }
    }

    internal class WulfrumElevatorHoverIndicatorDown : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("");
            Tooltip.SetDefault("You shouldn't be seeing this");
            Item.ResearchUnlockCount = 0;
            ItemID.Sets.Deprecated[Type] = true;
            ItemID.Sets.ItemsThatShouldNotBeInInventory[Type] = true;
        }
    }
    #endregion
}
