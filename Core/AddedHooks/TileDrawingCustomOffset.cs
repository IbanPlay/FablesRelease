using MonoMod.Cil;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public interface ICustomTileDrawOffset
    {
        /// <summary>
        /// Applies an offset to the position of a tile before it is drawn
        /// </summary>
        public void DrawOffset(TileDrawInfo drawData, int i, int j, ref Vector2 drawPosition) { }

    }

    public class TileDrawingCustomOffsets : ModSystem
    {
        public override void Load()
        {
            IL_TileDrawing.DrawSingleTile += IL_TileDrawing_DrawSingleTile; ; ;
        }

        private void IL_TileDrawing_DrawSingleTile(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            int drawPositionVectorIndex = 1;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchNewobj<Vector2>(),
                i => i.MatchLdarg(out _), //Screenoffset
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(out drawPositionVectorIndex)
                ))
            {
                FablesUtils.LogILEpicFail("Add tile framing post processing", "Could not locate drawPosition vector initialization");
                return;
            }


            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchCall<TileDrawing>("GetFinalLight"),
                i => i.MatchStfld(out _)

                ))
            {
                FablesUtils.LogILEpicFail("Add tile drawing custom offsets", "Could not locate GetFinaLLight call");
                return;
            }

            cursor.Emit(Ldarg_1); //Drawdata
            cursor.Emit(Ldarg, 6); //X
            cursor.Emit(Ldarg, 7); //Y
            cursor.Emit(Ldloca, drawPositionVectorIndex); //Draw position
            cursor.EmitDelegate(AddTileOffset);

        }

        public static void AddTileOffset(TileDrawInfo drawData, int tileX, int tileY, ref Vector2 drawPosition)
        {
            if (!FablesSets.CustomDrawOffset[drawData.typeCache])
                return;
            if (TileLoader.GetTile(drawData.typeCache) is ICustomTileDrawOffset offset)
                offset.DrawOffset(drawData, tileX, tileY, ref drawPosition);
        }

    }
}
