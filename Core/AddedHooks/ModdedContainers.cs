using CalamityFables.Helpers;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using NetEasy;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    /// <summary>
    /// Don't remember to include a CanKillTile check that prevents the tile from being broken if the chest is full. <br/>
    /// Need to use <see cref="ModdedContainers.PreventAutodestructionTileFrame(int, int, int, ref bool, ref bool)"/> to avoid the containers breaking due to thinking their own <see cref="Chest"/> is in the way
    /// </summary>
    public interface IModdedContainer
    {
        /// <summary>
        /// Called when the player tries to break the container. If the container has items inside, the container should remain without breaking. 
        /// Associated <see cref="Chest"/>(s) should be deleted if they can be broken <br/>
        /// Please also include a <see cref="ModTile.CanKillTile(int, int, ref bool)"/> check if your modded container isn't 2x2
        /// </summary>
        /// <returns>If the tile should survive the destruction or not</returns>
        public bool TryDestroying(int x, int y);

        /// <summary>
        /// Get a packet to be sent to the server so it can try breaking the container
        /// </summary>
        /// <returns>The net packet to send to the server</returns>
        public Module GetDestructionPacket(int x, int y);

        /// <summary>
        /// Width of the "chest hitbox" for the purposes of knowing when the player is out of range of the chest
        /// </summary>
        public int StorageWidth => 2;
        /// <summary>
        /// Height of the "chest hitbox" for the purposes of knowing when the player is out of range of the chest
        /// </summary>
        public int StorageHeight => 2;

        /// <summary>
        /// Called when unlocked on the server, to know what range of frames to synchronize. Call <see cref="NetMessage.SendTileSquare"/> for that
        /// </summary>
        public void SyncFrameOnUnlock(int x, int y) { }

        /// <summary>
        /// Is there a chest object on this specific tile. Used to know wether or not to send a packet to sync that chest in multiplayer when loading the world
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool IsChestThere(Tile tile)
        {
            return true;
        }
    }

    public class ModdedContainers : ModSystem
    {
        //public static readonly System.Reflection.MethodInfo GetUnloadedTileTypeMethod = typeof(TileEntry).GetMethod("GetUnloadedType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //public delegate string orig_GetUnloadedType(TileEntry self, ushort type);
        //public delegate string hook_GetUnloadedType(orig_GetUnloadedType orig, TileEntry self, ushort type);
        public static Hook SaveAsUnloadedChestHook;


        public override void Load()
        {
            //IsAContainer is used for some cases where the worldgen checks for the tile above a tile. We have to make it so that wall-mounted containers don't count
            On_WorldGen.IsAContainer += HijackContainerCheck;

            //CanKillTile and CanPoundTile check for the tile above being a BasicChest. Prevent wall mounted chests from counting towards that
            IL_WorldGen.CanKillTile_int_int_refBoolean += PreventWallMountedChestsFromTurningFloorUnbreakable;
            IL_WorldGen.CanPoundTile += PreventWallMountedChestsFromTurningFloorUnpoundable;

            //Runs our own modded chest break check before vanilla can do its own check which assumes the chest is 2x2
            On_WorldGen.CheckTileBreakability2_ShouldTileSurvive += ShouldFilledContainersSurvive;

            //Tiledrawing automatically changes the frame to match the chest's open status. We don't want that
            IL_TileDrawing.CacheSpecialDraws_Part2 += PreventVanillaChestFrameManipulation;

            //BasicChests use PlaceChest when WorldGen.PlaceTile is called, and PlaceChest is full of 2x2 chest specific code. Replace it with regular modded container placement
            On_WorldGen.PlaceChest += UseRegularPlaceObjetLogicForModdedContainers;

            //Big mimics shouldn't spawn from modded chests
            On_NPC.BigMimicSummonCheck += PreventModdedContainersFromSpawningMimics;

            //Modded containers need custom code to add their chests to the _compressChestList
            IL_NetMessage.CompressTileBlock_Inner += SyncModdedChestsMultiplayer;

            //Interaction range code for unrecognized tiles is borked and always thinks the player is out of range, which instantly closes the UI when opened
            On_Player.IsInInteractionRangeToMultiTileHitbox += HijackInteractionRange;
            //Sends a modded packet to the server if the player tries breaking a modded container in multiplayer
            IL_Player.PickTile += SyncStorageDestruction;

            On_Player.TileInteractionsMouseOver_Containers += DisableVanillaMouseOver;

            //The chest unlock netmessage only frames a 2x2 area. We have to extend it for modded locked containers
            FablesGeneralSystemHooks.HijackGetDataEvent += SyncBigChestUnlocks;

            FablesWall.CanExplodeEvent += PreventWallExplosion;
            FablesWall.KillWallEvent += PreventWallDestruction;
            FablesTile.CanReplaceTileEvent += PreventModdedContainerReplacement;
        }

        private void DisableVanillaMouseOver(On_Player.orig_TileInteractionsMouseOver_Containers orig, Player self, int myX, int myY)
        {
            if (FablesSets.CustomContainer[Main.tile[myX, myY].TileType])
                return;
            orig(self, myX, myY);
        }

        #region Hooks onto FablesWall and FablesTile
        private void PreventWallDestruction(int i, int j, int type, ref bool fail)
        {
            if (FablesSets.WallMountedContainer[Main.tile[i, j].TileType])
                fail = true;
        }
        private bool PreventWallExplosion(int i, int j, int type)
        {
            return !FablesSets.WallMountedContainer[Main.tile[i, j].TileType];
        }

        private bool PreventModdedContainerReplacement(int i, int j, int type, int tileTypeBeingPlaced) => !FablesSets.CustomContainer[type];

        #endregion

        // Prevent wall mounted containers from interfering with tiles below
        private bool HijackContainerCheck(On_WorldGen.orig_IsAContainer orig, Tile t) => orig(t) && !FablesSets.WallMountedContainer[t.TileType];

        //By default, BasicChests prevent tiles below themselves from being broken or pounded. Prevent that by adding a check for them
        #region Wall mounted chests don't affect the floor
        private void PreventWallMountedChestsFromTurningFloorUnbreakable(ILContext il)
        {
            //This is a switch statement so its gonna suck assss
            ILCursor cursor = new ILCursor(il);
            int tileAboveTypeIndex = 2;
            ILLabel notAChestLabel = null;
            ILLabel yesAChestLabel = null;

            //Go after the (!TileID.Sets.BasicDresser[type] && !TileID.Sets.BasicChest[type]) check
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(typeof(TileID.Sets), "BasicDresser"),
                i => i.MatchLdloc(out tileAboveTypeIndex),
                i => i.MatchLdelemU1(),
                i => i.MatchBrtrue(out yesAChestLabel),
                i => i.MatchLdsfld(typeof(TileID.Sets), "BasicChest"),
                i => i.MatchLdloc(out tileAboveTypeIndex),
                i => i.MatchLdelemU1(),
                i => i.MatchBrfalse(out notAChestLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Prevent wall mounted chests from turning floor below unbreakable", "Could not locate TileID.Sets.BasicDresser and TileID.Sets.BasicChest check");
                return;
            }

            //Check for if we're a wall mounted container and ignore if so
            cursor.EmitLdloc(tileAboveTypeIndex);
            cursor.EmitDelegate(PreventWallMountedChestsFromTurningFloorUnbreakable_WallContainerCheck);
            cursor.EmitBrtrue(notAChestLabel);
        }

        private void PreventWallMountedChestsFromTurningFloorUnpoundable(ILContext il)
        {
            //This is a switch statement so its gonna suck assss
            ILCursor cursor = new ILCursor(il);
            int tileAboveTypeIndex = 2;
            ILLabel notAChestLabel = null;
            ILLabel yesAChestLabel = null;

            //Go after the (!TileID.Sets.BasicChest[type] && !TileID.Sets.BasicDresser[type]) check
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdsfld(typeof(TileID.Sets), "BasicChest"),
                i => i.MatchLdloc(out tileAboveTypeIndex),
                i => i.MatchLdelemU1(),
                i => i.MatchBrtrue(out yesAChestLabel),
                i => i.MatchLdsfld(typeof(TileID.Sets), "BasicDresser"),
                i => i.MatchLdloc(out tileAboveTypeIndex),
                i => i.MatchLdelemU1(),
                i => i.MatchBrtrue(out yesAChestLabel),
                i => i.MatchLdsfld(typeof(TileID.Sets), "PreventsTileHammeringIfOnTopOfIt"),
                i => i.MatchLdloc(out tileAboveTypeIndex),
                i => i.MatchLdelemU1(),
                i => i.MatchBrfalse(out notAChestLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Prevent wall mounted chests from turning floor below unpoundable", "Could not locate TileID.Sets.BasicChest and TileID.Sets.BasicDresser check");
                return;
            }

            //Before the chest check happens, we check for if our tile is a wall mounted container, and ignore it if so
            cursor.EmitLdloc(tileAboveTypeIndex);
            cursor.EmitDelegate(PreventWallMountedChestsFromTurningFloorUnbreakable_WallContainerCheck);
            cursor.EmitBrtrue(notAChestLabel);
        }

        private static bool PreventWallMountedChestsFromTurningFloorUnbreakable_WallContainerCheck(int tileAbove) => FablesSets.WallMountedContainer[tileAbove];
        #endregion

        #region TileDrawing frame offset for open chests
        private void PreventVanillaChestFrameManipulation(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel notAChestLabel = null; 

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(typeof(TileID.Sets), "BasicChest"),
                i => i.MatchLdarg3(),
                i => i.MatchLdfld<TileDrawInfo>("typeCache"),
                i => i.MatchLdelemU1(),
                i => i.MatchBrfalse(out notAChestLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Prevent TileDrawing_CacheSpecialDraws_Part2 from messing up modded container frames", "Could not locate TileID.Sets.BasicChest check");
                return;
            }

            cursor.EmitLdarg3();
            cursor.EmitDelegate(PreventVanillaChestFrameManipulation_ModdedContainerCheck);
            cursor.EmitBrtrue(notAChestLabel);
        }

        public static bool PreventVanillaChestFrameManipulation_ModdedContainerCheck(TileDrawInfo drawInfo) => FablesSets.CustomContainer[drawInfo.typeCache];
        #endregion

        private int UseRegularPlaceObjetLogicForModdedContainers(On_WorldGen.orig_PlaceChest orig, int x, int y, ushort type, bool notNearOtherChests, int style)
        {
            if (FablesSets.CustomContainer[type])
            {
                WorldGen.PlaceObject(x, y, type, false, style);
                return 0;
            }

            return orig(x, y, type, notNearOtherChests, style);
        }

        private bool PreventModdedContainersFromSpawningMimics(On_NPC.orig_BigMimicSummonCheck orig, int x, int y, Player user)
        {
            if (FablesSets.CustomContainer[Main.tile[x, y].TileType])
                return false;

            return orig(x, y, user);
        }

        #region Multiplayer load chests
        private void SyncModdedChestsMultiplayer(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            int tileIndex = 11;
            ILLabel skipBasicChestLabel = null;

            //Go before the BasicChest check
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdsfld(typeof(TileID.Sets), "BasicChest"),
                i => i.MatchLdloca(out tileIndex),
                i => i.MatchCall<Tile>("get_type"),
                i => i.MatchLdindU2(),
                i => i.MatchLdelemU1(),
                i => i.MatchBrfalse(out skipBasicChestLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Make modded containers load clientside in MP", "Could not locate TileID.Sets.BasicChest check");
                return;
            }

            //Set a label to right before the chest instruction so we can go back to it.
            ILLabel beforeChestCheck = cursor.DefineLabel();
            beforeChestCheck.Target = cursor.Next;

            //Now we go on an adventure, and go grab all the variable indices we need, including:
            // -The x/y coordinates,
            // -The current amount of chests that were registered to the compressed list,
            // -The local variable used to store the chest index (we will use it when checking our own chest index)
            #region Going on an adventure
            int jIndex = 0;
            int iIndex = 0;
            int chestIndexVariableIndex = 0;
            int chestCountIndex = 0;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out jIndex),
                i => i.MatchLdloc(out iIndex),
                i => i.MatchCall<Chest>("FindChest"),
                i => i.MatchConvI2(),
                i => i.MatchStloc(out chestIndexVariableIndex)
                ))
            {
                FablesUtils.LogILEpicFail("Make modded containers load clientside in MP", "Could not find FindChest chest index assignment");
                return;
            }
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<NetMessage>("_compressChestList"),
                i => i.MatchLdloc(out chestCountIndex)
                ))
            {
                FablesUtils.LogILEpicFail("Make modded containers load clientside in MP", "Could not retrieve _compressChestList variable");
                return;
            }
            #endregion

            //Checks if the tile is a modded container : If it is, we skip over the basicchest code because we don't want it to run on modded containers
            cursor.GotoLabel(beforeChestCheck);
            cursor.Emit(Ldloc, tileIndex);
            cursor.EmitDelegate(SyncModdedChestsMultiplayer_DoesTileHaveModdedContainerAtAll);
            cursor.Emit(Brtrue, skipBasicChestLabel);

            ILLabel skipModdedContainerLogicLabel = cursor.DefineLabel();

            //Past the basic chest, we do our check once more to see if we have a chest at the specific coordinates for our modded tile
            cursor.GotoLabel(skipBasicChestLabel);
            cursor.Emit(Ldloc, tileIndex);
            cursor.EmitDelegate(SyncModdedChestsMultiplayer_DoesTileHaveModdedContainerChest);
            cursor.Emit(Brfalse, skipModdedContainerLogicLabel);

            //chestIndex = (short)FindChest(i, j)
            cursor.Emit(Ldloc, jIndex);
            cursor.Emit(Ldloc, iIndex);
            cursor.Emit(Call, typeof(Chest).GetMethod("FindChest"));
            cursor.Emit(Conv_I2);
            cursor.Emit(Stloc, chestIndexVariableIndex);

            //if (chestIndex != -1)
            cursor.Emit(Ldloc, chestIndexVariableIndex);
            cursor.Emit(Ldc_I4_M1);
            cursor.Emit(Beq_S, skipModdedContainerLogicLabel);

            //_compressChestList[chestCount] = chestIndex;
            cursor.Emit(Ldsfld, typeof(NetMessage).GetField("_compressChestList", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            cursor.Emit(Ldloc, chestCountIndex);
            cursor.Emit(Ldloc, chestIndexVariableIndex);
            cursor.Emit(Stelem_I2);

            //chestCount++
            cursor.Emit(Ldloc, chestCountIndex);
            cursor.Emit(Ldc_I4_1);
            cursor.Emit(Add);
            cursor.Emit(Conv_I2);
            cursor.Emit(Stloc, chestCountIndex);

            //Mark the label after this part of code so stuff can skip over it
            skipModdedContainerLogicLabel.Target = cursor.Next;
        }

        private static bool SyncModdedChestsMultiplayer_DoesTileHaveModdedContainerChest(Tile tile)
        {
            if (FablesSets.CustomContainer[tile.TileType] && (TileLoader.GetTile(tile.TileType) is IModdedContainer modContainer) && modContainer.IsChestThere(tile))
                return true;
            return false;
        }

        private static bool SyncModdedChestsMultiplayer_DoesTileHaveModdedContainerAtAll(Tile tile) => FablesSets.CustomContainer[tile.TileType];
        #endregion


        private bool HijackInteractionRange(On_Player.orig_IsInInteractionRangeToMultiTileHitbox orig, Player self, int chestPointX, int chestPointY)
        {
            Tile tile = Main.tile[chestPointX, chestPointY];
            if (FablesSets.CustomContainer[tile.TileType] && TileLoader.GetTile(tile.TileType) is IModdedContainer container)
            {
                Rectangle storageRect = new Rectangle(chestPointX * 16, chestPointY * 16, container.StorageWidth * 16, container.StorageHeight * 16);
                storageRect.Inflate(-1, -1);
                Point closestTile = storageRect.ClosestPointInRect(self.Center).ToTileCoordinates();
                Point playerTileCenter = (self.Center / 16).ToPoint();

                return playerTileCenter.X >= closestTile.X - Player.tileRangeX && playerTileCenter.X <= closestTile.X + Player.tileRangeX + 1 && playerTileCenter.Y >= closestTile.Y - Player.tileRangeY && playerTileCenter.Y <= closestTile.Y + Player.tileRangeY + 1; ;
            }

            return orig(self, chestPointX, chestPointY);
        }

        /// <summary>
        /// Executes basic tileframe checks for the tile, but without running the CanPlace check <br/>
        /// This avoids the issue of the modded container breaking instantly because its own <see cref="Chest"/> is already "in the way"
        /// </summary>
        /// <param name="tileType"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="resetFrame"></param>
        /// <param name="noBreak"></param>
        /// <returns></returns>
        public static bool PreventAutodestructionTileFrame(int tileType, int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tile tile = Main.tile[i, j];
            TileObjectData tileData = TileObjectData.GetTileData(tileType, 0, 0);

            //Safety check
            if (tile.TileFrameX < 0)
                tile.TileFrameX = (short)FablesUtils.Modulo(tile.TileFrameX, tileData.CoordinateFullWidth);

            int partFrameX = tile.TileFrameX % tileData.CoordinateFullWidth;
            int partFrameY = tile.TileFrameY % tileData.CoordinateFullHeight;
            int partX = partFrameX / (tileData.CoordinateWidth + tileData.CoordinatePadding);
            int partY = 0;
            int remainingFrameY = partFrameY;
            while (remainingFrameY > 0)
            {
                remainingFrameY -= tileData.CoordinateHeights[partY] + tileData.CoordinatePadding;
                partY++;
            }
            i -= partX;
            j -= partY;

            for (int x = i; x < i + tileData.Width; x++)
            {
                for (int y = j; y < j + tileData.Height; y++)
                {
                    if (!Main.tile[x, y].HasTile || Main.tile[x, y].TileType != tileType)
                        return true;
                }
            }

            return false;
        }

        #region Breaking and syncing broken chests
        private bool ShouldFilledContainersSurvive(On_WorldGen.orig_CheckTileBreakability2_ShouldTileSurvive orig, int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            //Vanilla runs chest break checks using the assumption of a 2x2 tile, so we have to override htat
            if (FablesSets.CustomContainer[tile.TileType] && TileLoader.GetTile(tile.TileType) is IModdedContainer modContainer)
                return modContainer.TryDestroying(x, y);

            return orig(x, y);
        }

        private void SyncStorageDestruction(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchCall<Tile>("get_type"),
                i => i.MatchLdindU2(),
                i => i.MatchLdsfld<TileID>("Count"),
                i => i.MatchBlt(out _)
                ))
            {
                FablesUtils.LogILEpicFail("Make modded containers breakable in multiplayer with PickTile", "Could not locate Main.tile[x, y].type >= TileID.TileCount check");
                return;
            }

            ILLabel skipModdedBasicChestsLabel = null;

            //Find the check for regular basicchests so we can use the label to skip past it if we have a modded container
            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld(typeof(TileID.Sets), "BasicChest"),
                i => i.MatchLdsflda<Main>("tile"),
                i => i.MatchLdarg(1),
                i => i.MatchLdarg(2),
                i => i.MatchCall<Tilemap>("get_Item"),
                i => i.MatchStloc(out int _),
                i => i.MatchLdloca(out int _),
                i => i.MatchCall<Tile>("get_type"),
                i => i.MatchLdindU2(),
                i => i.MatchLdelemU1(),
                i => i.MatchBrfalse(out skipModdedBasicChestsLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Make modded containers breakable in multiplayer with PickTile", "Could not locate TileID.Sets.BasicChest[Main.tile[x, y].type] check");
                return;
            }

            cursor.Emit(Ldarg_1);
            cursor.Emit(Ldarg_2);
            cursor.EmitDelegate(SendPickRequest);
            cursor.EmitBrtrue(skipModdedBasicChestsLabel);
        }

        public static bool SendPickRequest(int x, int y)
        {
            if (!FablesSets.CustomContainer[Main.tile[x, y].TileType])
                return false;
            IModdedContainer container = TileLoader.GetTile(Main.tile[x, y].TileType) as IModdedContainer;
            var packet = container.GetDestructionPacket(x, y);
            packet.Send(-1, -1, false);
            return true;
        }
        #endregion


        private bool SyncBigChestUnlocks(ref byte messageID, ref System.IO.BinaryReader binaryReader, int playerNumber)
        {
            if (messageID != MessageID.LockAndUnlock)
                return false;
            long originalPosition = binaryReader.BaseStream.Position;
            int actionType = binaryReader.ReadByte();
            //Unlock chest
            if (actionType == 1)
            {
                int x = binaryReader.ReadInt16();
                int y = binaryReader.ReadInt16();
                if (!FablesSets.CustomContainer[Main.tile[x, y].TileType])
                {
                    //Go back to where we were
                    binaryReader.BaseStream.Position = originalPosition;
                    return false;
                }

                //Unlock our modded container ourselves
                Chest.Unlock(x, y);
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.TrySendData(52, -1, playerNumber, null, 0, actionType, x, y);

                    IModdedContainer container = TileLoader.GetTile(Main.tile[x, y].TileType) as IModdedContainer;
                    container.SyncFrameOnUnlock(x, y);
                }
                return true;
            }
            else
            {
                //Go back to where we were
                binaryReader.BaseStream.Position = originalPosition;
                return false;
            }
        }

        //public string SaveModdedContainersAsUnloadedChests(orig_GetUnloadedType orig, TileEntry self, ushort type)
        //{
        //    if (FablesSets.CustomContainer[type])
        //        return ModContent.GetInstance<UnloadedChest>().FullName;
        //    return orig(self, type);
        //}
    }

    [Serializable]
    public class SyncGenericContainerModule : Module
    {
        public readonly byte whoAmI;
        public readonly bool place;
        public readonly int direction;
        public int x;
        public int y;
        public readonly int tileType;
        public readonly int itemType;
        public bool unplace = false;
        public int chestID = 0;

        public SyncGenericContainerModule(int whoAmI, bool place, int x, int y, int direction = 1, int tileType = 0, int itemType = 0)
        {
            this.whoAmI = (byte)whoAmI;
            this.place = place;
            this.x = x;
            this.y = y;
            this.direction = direction;
            this.tileType = tileType;
            this.itemType = itemType;
        }

        protected override void Receive()
        {
            //Referenced from the netmessage 34

            if (Main.netMode == NetmodeID.Server)
            {
                //Trying to place
                if (place)
                {
                    TileObjectData data = TileObjectData.GetTileData(tileType, 0);
                    int placeX = x + data.Origin.X;
                    int placeY = y + data.Origin.Y;

                    bool placeSuccess = TileObject.CanPlace(placeX, placeY, tileType, 0, direction, out TileObject objectData);
                    CalamityFables.Instance.Logger.Debug("Trying to place the container: Result of CanPlace check " + placeSuccess.ToString() );

                    if (placeSuccess)
                    {
                        TileObject.Place(objectData);
                        chestID = Chest.CreateChest(x, y);

                        //Send to all other clients the request to place the chest
                        if (chestID != -1)
                            Send(-1, -1, false);
                        else
                        {
                            placeSuccess = false;
                            CalamityFables.Instance.Logger.Debug("Trying to place the container: We could place the tile, but there already exists a chest in the coordinates. Unplacing");
                        }
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
                    chestID = Chest.FindChest(x, y);

                    CalamityFables.Instance.Logger.Debug("Recieved a destruction request: Chest id found at " + chestID.ToString());
                    WorldGen.KillTile(x, y); //Attempt to break the chest
                    //If we successfully broke the tile, share the destruction to all other clients
                    if (!tile.HasTile)
                    {
                        Send(-1, -1, false);
                        CalamityFables.Instance.Logger.Debug("Successfully recieved a packet and broke a container");
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

                //Place the chest using the same ID as the one the server created, and then place the tile
                if (place)
                {
                    //Play a sound
                    SoundStyle placeSound = SoundID.Dig;
                    if (FablesSets.CustomPlaceSound[tileType] && TileLoader.GetTile(tileType) is ICustomPlaceSounds customSoundsTile)
                        placeSound = customSoundsTile.PlaceSound;

                    SoundEngine.PlaySound(placeSound, new Vector2(x, y) * 16f);


                    Chest.CreateChest(x, y, chestID);

                    PlaceContainerDirect();
                }
                //Break the chest & destroy the tile
                else
                {
                    Chest.DestroyChestDirect(x, y, chestID);
                    WorldGen.KillTile(x, y);
                }
            }
        }

        public void PlaceContainerDirect()
        {
            TileObjectData data = TileObjectData.GetTileData(tileType, 0); //magic numbers and uneccisary params begone!
            int widthOffset = 0;

            if (data.AlternatesCount != 0)
            {
                int alternateStyles = data.AlternatesCount;
                int alternate = -1;

                while (alternate < alternateStyles)
                {
                    alternate++;
                    TileObjectData tileData2 = TileObjectData.GetTileData(tileType, 0, alternate);
                    if (tileData2.Direction != 0 && ((tileData2.Direction == TileObjectDirection.PlaceLeft && direction == 1) || (tileData2.Direction == TileObjectDirection.PlaceRight && direction == -1)))
                        continue;

                    widthOffset = tileData2.Width * alternate;
                }
            }


            if (x + data.Width > Main.maxTilesX || x < 0) return; //make sure we dont spawn outside of the world!
            if (y + data.Height > Main.maxTilesY || y < 0) return;

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
