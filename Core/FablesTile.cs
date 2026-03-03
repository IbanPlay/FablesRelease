using Terraria.DataStructures;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using static Mono.Cecil.Cil.OpCodes;
using Terraria.GameContent.Drawing;


namespace CalamityFables.Core
{
    public partial class FablesTile : GlobalTile
    {
        public override void Load()
        {
            On_WorldGen.CheckTileBreakability += PreventTileBreakAndGrassConversion;
            On_WorldGen.CanKillTile_int_int_refBoolean += PreventOrForceTileBreak;
            On_Liquid.tilesIgnoreWater += WaterIgnoreBehavior;
            On_WaterfallManager.DrawWaterfall_int_float += WaterfallsIgnoreBehavior;

            IL_Player.ItemCheck_UseBuckets += AllowLiquidPlacementInsideGrates;
            IL_Liquid.AddWater += AllowAddWaterInsideGrates;
            IL_TileDrawing.DrawTile_LiquidBehindTile += DrawLiquidBehindGrates;
        }

        #region Grate-type tiles that let water through

        public delegate void SetWaterSolidityDelegate(bool ignoreSolids);
        /// <summary>
        /// Use this to change <see cref="Main.tileSolid"/> for your tile when liquids are updated, to make the tile act like grates in vanilla
        /// </summary>
        public static event SetWaterSolidityDelegate MakeWaterIgnoreTilesEvent;
        private void WaterIgnoreBehavior(On_Liquid.orig_tilesIgnoreWater orig, bool ignoreSolids)
        {
            orig(ignoreSolids);
            MakeWaterIgnoreTilesEvent?.Invoke(ignoreSolids);
        }
        private void WaterfallsIgnoreBehavior(On_WaterfallManager.orig_DrawWaterfall_int_float orig, WaterfallManager self, int Style, float Alpha)
        {
            MakeWaterIgnoreTilesEvent?.Invoke(true);
            orig(self, Style, Alpha);
            MakeWaterIgnoreTilesEvent?.Invoke(false);
        }

        private void DrawLiquidBehindGrates(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel afterGrateCheckLabel = cursor.DefineLabel();
            int tileCacheIndex = 8;

            //Go before the grate check, since we're gonna have to skip it
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdarga(out tileCacheIndex),
                i => i.MatchCall<Tile>("get_type"),
                i => i.MatchLdindU2(),
                i => i.MatchLdcI4(TileID.Grate),
                i => i.MatchBneUn(out ILLabel _))) //We don't care about this label
            {
                FablesUtils.LogILEpicFail("Gratelike tiles - Draw liquid behind grates always", "Could not locate grate type check");
                return;
            }

            cursor.Emit(Ldarg, tileCacheIndex);
            cursor.EmitDelegate(IsThisTileAGrateTile);
            cursor.EmitBrtrue(afterGrateCheckLabel);

            //Now that we put our custom logic , we skip to the liquid amount check, bypassing the vanilla grate check
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(TileID.Grate),
                i => i.MatchBneUn(out ILLabel _))) //We don't care about this label
            {
                FablesUtils.LogILEpicFail("Gratelike tiles - Draw liquid behind grates always", "Could not locate grate type check, the second time... how???");
                return;
            }

            //mark the label to skip
            afterGrateCheckLabel.Target = cursor.Next;
        }
        private void AllowLiquidPlacementInsideGrates(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel skipReturnLabel = null;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(TileID.Grate),
                i => i.MatchBeq(out skipReturnLabel)))
            {
                FablesUtils.LogILEpicFail("Gratelike tiles - Player bucket use", "Could not locate grate type check");
                return;
            }

            cursor.EmitDelegate(IsPlayerHoveringGrateTile);
            cursor.EmitBrtrue(skipReturnLabel);
        }
        private static bool IsPlayerHoveringGrateTile() =>  FablesSets.ActsAsAGrate[Main.tile[Player.tileTargetX, Player.tileTargetY].TileType];

        private void AllowAddWaterInsideGrates(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel skipReturnLabel = null;
            int tileIndex = 0;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloca(out tileIndex),
                i => i.MatchCall<Tile>("get_type"),
                i => i.MatchLdindU2(),
                i => i.MatchLdcI4(TileID.Grate),
                i => i.MatchBeq(out skipReturnLabel)))
            {
                FablesUtils.LogILEpicFail("Gratelike tiles - Liquid.AddWater", "Could not locate grate type check");
                return;
            }

            cursor.Emit(Ldloc, tileIndex);
            cursor.EmitDelegate(IsThisTileAGrateTile);
            cursor.Emit(Brtrue, skipReturnLabel);
        }
        private static bool IsThisTileAGrateTile(Tile t) => FablesSets.ActsAsAGrate[t.TileType];
        #endregion

        public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
        {
            //Tile t = Main.tile[i, j];

            //CalamityFables.Instance.Logger.Debug($"Framing tile at {i}, {j} of type {type}. Tile frame is {t.TileFrameX}, {t.TileFrameY}");

            return true;
        }


        public delegate void KillTileDelegate(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem);
        public static event KillTileDelegate KillTileEvent;
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (KillTileEvent == null)
                return;

            foreach (KillTileDelegate check in KillTileEvent.GetInvocationList())
            {
                check(i, j, type, ref fail, ref effectOnly, ref noItem);
            }
        }


        public delegate bool CanReplaceTileDelegate(int i, int j, int type, int tileTypeBeingPlaced);
        public static EventSet<CanReplaceTileDelegate> CanReplaceSpecificTileEvent = new();
        public static event CanReplaceTileDelegate CanReplaceTileEvent;

        public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced)
        {
            var invocation = CanReplaceSpecificTileEvent.GetInvocation(type);
            if (invocation != null && !invocation(i, j, type, tileTypeBeingPlaced))
                return false;

            if (CanReplaceTileEvent != null)
            {
                foreach (CanReplaceTileDelegate check in CanReplaceTileEvent.GetInvocationList())
                {
                    if (!check(i, j, type, tileTypeBeingPlaced))
                        return false;
                }
            }

            return true;            
        }


        public delegate bool CanKillTileDelegate(int i, int j, int type);
        public static EventSet<CanKillTileDelegate> CanKillSpecificTileEvent = new();
        public static event CanKillTileDelegate CanKillTileEvent;
        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            var invocation = CanKillSpecificTileEvent.GetInvocation(type);
            if (invocation != null && !invocation(i, j, type))
                return false;

            if (CanKillTileEvent != null)
            {
                foreach (CanKillTileDelegate check in CanKillTileEvent.GetInvocationList())
                {
                    if (!check(i, j, type))
                        return false;
                }
            }

            return true;
        }


        //Not using the tml cankilltile hook because otherwise it also applies some extra logic we dont need (trees always preventing break if ontop of it, except specific hardcoded branch frames)
        public delegate bool? OverrideKillTileDelegate(int i, int j, int type);
        /// <summary>
        /// This is like CanKillTile, except it entirely overrides it, and gets ran in <see cref="WorldGen.CheckTileBreakability(int, int)"/> which prevents grass from being turned into dirt<br/>
        /// Used by modded trees to prevent grass break below, and to prevent vanilla tile break logic from running for them. (Vanilla tilebreak checks specific treebranch frames, which may not match our modded tilesheet format)
        /// </summary>
        public static event OverrideKillTileDelegate OverrideKillTileEvent;
        private bool PreventOrForceTileBreak(On_WorldGen.orig_CanKillTile_int_int_refBoolean orig, int i, int j, out bool blockDamaged)
        {
            blockDamaged = false; 
            if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY)
                return false;
            if (!Main.tile[i, j].HasTile)
                return false;

            int type = Main.tile[i, j].TileType;

            if (j > 0)
            {
                Tile tileAbove = Main.tile[i, j - 1];
                if (tileAbove.HasTile && FablesSets.AlwaysPreventTileBreakIfOnTopOfIt[tileAbove.TileType] && type != tileAbove.TileType)
                    return false;
            }

            if (OverrideKillTileEvent == null)
                return true;

            foreach (OverrideKillTileDelegate check in OverrideKillTileEvent.GetInvocationList())
            {
                bool? result = check(i, j, type);
                if (result.HasValue)
                    return result.Value;
            }

            return orig(i, j, out blockDamaged);
        }

        private int PreventTileBreakAndGrassConversion(On_WorldGen.orig_CheckTileBreakability orig, int x, int y)
        {
            if (y > 0)
            {
                Tile tileAbove = Main.tile[x, y - 1];
                if (tileAbove.HasTile && FablesSets.AlwaysPreventTileBreakIfOnTopOfIt[tileAbove.TileType] && Main.tile[x, y].TileType != tileAbove.TileType)
                    return 2;
            }

            if (OverrideKillTileEvent != null)
            {
                foreach (OverrideKillTileDelegate check in OverrideKillTileEvent.GetInvocationList())
                {
                    bool? canBreakCheck = check(x, y, Main.tile[x, y].TileType);
                    if (canBreakCheck.HasValue)
                        return canBreakCheck.Value ? 0 : 2;
                }
            }

            return orig(x, y);
        }
    }
}
