using MonoMod.Cil;
using Terraria.GameContent.Drawing;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public interface IWallFrameAnimatable
    {
        public void AnimateIndividualWall(int type, int i, int j, ref int frameXOffset, ref int frameYOffset);
    }

    public class IndividualWallFrameAnimation : ModSystem
    {
        public override void Load()
        {
            Terraria.GameContent.Drawing.IL_WallDrawing.DrawWalls += AnimateIndividualWalls;
        }

        private void AnimateIndividualWalls(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            int wallXVariableIndex = 0;
            int wallYVariableIndex = 0;
            int wallTypeVariableIndex = 0;
            int rectangleVariableIndex = 0;


            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out wallXVariableIndex),
                i => i.MatchLdloc(out wallYVariableIndex),
                i => i.MatchCall<WallDrawing>("FullTile")
                ))
            {
                FablesUtils.LogILEpicFail("Add individual wall animation", "Could not locate FullTile()");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdloc(out wallTypeVariableIndex),
                i => i.MatchCallvirt<Main>("LoadWall")
                ))
            {
                FablesUtils.LogILEpicFail("Add individual wall animation", "Could not locate Main.LoadWall()");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloca(out rectangleVariableIndex),
                i => i.MatchLdloca(out int _), //tile
                i => i.MatchCall<Tile>("wallFrameX"),
                i => i.MatchStfld<Rectangle>("X"),

                i => i.MatchLdloca(rectangleVariableIndex),
                i => i.MatchLdloca(out int _), //tile
                i => i.MatchCall<Tile>("wallFrameY")
                ))
            {
                FablesUtils.LogILEpicFail("Add individual wall animation", "Could not locate the setting of the wall frame X");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchStfld<Rectangle>("Y")
                ))
            {
                FablesUtils.LogILEpicFail("Add individual wall animation", "Could not locate the setting of the wall frame Y");
                return;
            }

            cursor.Emit(Ldloc, rectangleVariableIndex);
            cursor.Emit(Ldloc, wallTypeVariableIndex);
            cursor.Emit(Ldloc, wallXVariableIndex);
            cursor.Emit(Ldloc, wallYVariableIndex);
            cursor.EmitDelegate(MakeItWork);
            cursor.Emit(Stloc, rectangleVariableIndex);
        }

        public Rectangle MakeItWork(Rectangle originalRectangle, int type, int i, int j)
        {
            if (FablesSets.IndividuallyAnimatedWall[type])
            {
                if (WallLoader.GetWall(type) is IWallFrameAnimatable animatedWall)
                {
                    int XOffset = 0;
                    int YOffset = 0;
                    animatedWall.AnimateIndividualWall(type, i, j, ref XOffset, ref YOffset);
                    originalRectangle.X += XOffset;
                    originalRectangle.Y += YOffset;
                }
            }

            return originalRectangle;
        }
    }
}
