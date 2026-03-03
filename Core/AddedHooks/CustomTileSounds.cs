using MonoMod.Cil;
using System.Reflection.Emit;
using static Mono.Cecil.Cil.OpCodes;
using System.Reflection;
using MonoMod.Utils;

namespace CalamityFables.Core
{
    public interface ICustomPlaceSounds
    {
        public SoundStyle PlaceSound { get; }
    }

    public class CustomPlaceSoundHandler : ModSystem
    {
        public override void Load()
        {
            IL_WorldGen.PlaceObject += IL_WorldGen_PlaceObject;
            IL_WorldGen.PlaceTile += IL_WorldGen_PlaceTile;
            IL_Player.PlaceThing_Tiles_PlaceIt += IL_Player_PlaceThing_Tiles_PlaceIt;
        }

        private void IL_WorldGen_PlaceTile(ILContext il)
        {

            ILCursor cursor = new ILCursor(il);
            ILLabel returnLabel = cursor.DefineLabel();
            MethodInfo playSoundMethod = typeof(SoundEngine).GetMethod("PlaySound", BindingFlags.NonPublic | BindingFlags.Static, new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float) });


            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(3),
                i => i.MatchBrtrue(out _),
                i => i.MatchLdsfld<WorldGen>("generatingWorld"),
                i => i.MatchBrtrue(out _)
                ))
            {
                FablesUtils.LogILEpicFail("Add custom tile sounds in WorldGen.PlaceTile", "Could not locate if (!mute && !worldgen) check");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdcI4(0),
                i => i.MatchLdarg(0),
                i => i.MatchLdcI4(16),
                i => i.MatchMul(),
                i => i.MatchLdarg(1),
                i => i.MatchLdcI4(16),
                i => i.MatchMul(),
                i => i.MatchLdcI4(1),
                i => i.MatchLdcR4(1),
                i => i.MatchLdcR4(0),

                //int type, int x = -1, int y = -1, int Style = 1, float volumeScale = 1f, float pitchOffset = 0f)
                i => i.MatchCall(playSoundMethod),
                i => i.MatchPop()
                ))
            {
                FablesUtils.LogILEpicFail("Add custom tile sounds in WorldGen.PlaceTile", "Could not move before playsound call");
                return;
            }

            cursor.Emit(Ldarg_0);
            cursor.Emit(Ldarg_1);
            cursor.Emit(Ldarg_2);
            cursor.EmitDelegate(CheckForCustomSoundTile);
            cursor.Emit(Brtrue, returnLabel);

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(0),
                i => i.MatchLdarg(0),
                i => i.MatchLdcI4(16),
                i => i.MatchMul(),
                i => i.MatchLdarg(1),
                i => i.MatchLdcI4(16),
                i => i.MatchMul(),
                i => i.MatchLdcI4(1),
                i => i.MatchLdcR4(1),
                i => i.MatchLdcR4(0),
                i => i.MatchCall(playSoundMethod),
                i => i.MatchPop()
                ))
            {
                FablesUtils.LogILEpicFail("Add custom tile sounds in WorldGen.PlaceTile", "Could not move after playsound call");
                return;
            }

            cursor.MarkLabel(returnLabel);
        }

        private void IL_Player_PlaceThing_Tiles_PlaceIt(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel returnLabel = null;
            ILLabel soundPlayStartLabel = null;

            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>("netMode"),
                i => i.MatchLdcI4(1),
                i => i.MatchBneUn(out soundPlayStartLabel),

                i => i.MatchLdsfld(typeof(TileID.Sets).GetField("IsAContainer", BindingFlags.Static | BindingFlags.Public)),
                i => i.MatchLdarg(3),
                i => i.MatchLdelemU1(),
                i => i.MatchBrtrue(out returnLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Add custom tile sounds in Player.PlaceThing", "Could not locate (Main.netMode != NetmodeID.MultiplayerClient || !TileID.Sets.IsAContainer[tileToCreate]) check");
                return;
            }

            cursor.GotoLabel(soundPlayStartLabel);

            cursor.Emit(Ldarg_3);
            cursor.EmitDelegate(CheckForCustomSoundTile_PlayerPlaced);
            cursor.Emit(Brtrue, returnLabel);
        }

        private void IL_WorldGen_PlaceObject(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel returnLabel = null;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(3),
                i => i.MatchBrtrue(out returnLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Add custom tile sounds in WorldGen.PlaceObject", "Could not locate if (!mute) check");
                return;
            }

            cursor.Emit(Ldarg_0);
            cursor.Emit(Ldarg_1);
            cursor.Emit(Ldarg_2);
            cursor.EmitDelegate(CheckForCustomSoundTile);
            cursor.Emit(Brtrue, returnLabel);
        }

        public static bool CheckForCustomSoundTile(int x, int y, int type)
        {
            if (FablesSets.CustomPlaceSound[type] && TileLoader.GetTile(type) is ICustomPlaceSounds customSoundsTile)
            {
                SoundEngine.PlaySound(customSoundsTile.PlaceSound, new Vector2(x * 16, y * 16));
                return true;
            }
            return false;
        }

        public static bool CheckForCustomSoundTile_PlayerPlaced(int type)
        {
            if (FablesSets.CustomPlaceSound[type] && TileLoader.GetTile(type) is ICustomPlaceSounds customSoundsTile)
            {
                SoundEngine.PlaySound(customSoundsTile.PlaceSound, new Vector2(Player.tileTargetX * 16, Player.tileTargetY * 16));
                return true;
            }
            return false;
        }
    }

}
