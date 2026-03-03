using MonoMod.Cil;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    //TODO eventually make more complex for doors that open weirdly
    public interface ICustomDoor
    {
        public int NewType { get; }
        public SoundStyle InteractSound { get; }

        /// <summary>
        /// Does this door count as "solid", even if not actually solid, for room check purposes.
        /// </summary>
        public bool CountsAsSolid(int frameX, int frameY) => false;
    }

    public class CustomDoorHandler : ModSystem
    {
        public override void Load()
        {
            On_WorldGen.OpenDoor += OpenCustomDoor;
            On_WorldGen.CloseDoor += CloseCustomDoor;
            IL_WorldGen.CheckRoom += LetCustomOpenDoorsCountAsSolid;
        }

        private void LetCustomOpenDoorsCountAsSolid(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel returnLabel = null;

            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdsflda<Main>("tile"),
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(1),
                i => i.MatchCall<Tilemap>("get_Item"),
                i => i.MatchCall(typeof(TileLoader), nameof(TileLoader.CloseDoorID)),
                i => i.MatchLdcI4(0),
                i => i.MatchBlt(out var _),

                i => i.MatchLdsflda<Main>("tile"),
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(1),
                i => i.MatchCall<Tilemap>("get_Item"),
                i => i.MatchStloc(out int _),
                i => i.MatchLdloca(out int _),
                i => i.MatchCall<Tile>("get_frameX"),
                i => i.MatchLdindI2(),
                i => i.MatchBrfalse(out returnLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Open custom doors count as solid", "Could not locate TileLoader.CloseDoorID");
                return;
            }

            cursor.Emit(Ldarg_0);
            cursor.Emit(Ldarg_1);
            cursor.EmitDelegate(ShouldBeSolidRoomWall);
            cursor.Emit(Brtrue, returnLabel);
        }

        public bool ShouldBeSolidRoomWall(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (FablesSets.ForceHouseWall[tile.TileType])
                return true;

            if (FablesSets.CustomDoor[tile.TileType] && TileLoader.GetTile(tile.TileType) is ICustomDoor door)
                return door.CountsAsSolid(tile.TileFrameX, tile.TileFrameY);
            return false;
        }

        private bool CloseCustomDoor(Terraria.On_WorldGen.orig_CloseDoor orig, int i, int j, bool forced)
        {
            Tile tile = Main.tile[i, j];
            if (FablesSets.CustomDoor[tile.TileType])
                return InteractWithCustomDoor(tile, i, j);
            return orig(i, j, forced);
        }

        private bool OpenCustomDoor(Terraria.On_WorldGen.orig_OpenDoor orig, int i, int j, int direction)
        {
            Tile tile = Main.tile[i, j];
            if (FablesSets.CustomDoor[tile.TileType])
                return InteractWithCustomDoor(tile, i, j, direction);
            return orig(i, j, direction);
        }

        public static bool InteractWithCustomDoor(Tile tile, int i, int j, int direction = 1)
        {
            tile.GetTopLeft(ref i, ref j, out int width, out int height);
            ModTile modTile = TileLoader.GetTile(tile.TileType);
            //Somehow
            if (modTile is not ICustomDoor customDoor)
                return false;

            int newType = customDoor.NewType;

            //Switch types
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Main.tile[i + x, j + y].TileType = (ushort)newType;
                    if (Main.netMode != NetmodeID.MultiplayerClient && Wiring.running)
                        Wiring.SkipWire(i + x, j + y);
                }
            }
            //Tile frame
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    WorldGen.TileFrame(i + x, j + y);

            SoundEngine.PlaySound(customDoor.InteractSound, new Vector2(i, j) * 16);
            return true;
        }
    }
}
