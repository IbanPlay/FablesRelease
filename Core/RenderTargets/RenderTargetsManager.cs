using MonoMod.Cil;
using System.Reflection;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader.Core;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public class RenderTargetsManager : ModSystem
    {
        private Vector2 oldScreenSize;
        public static float RTSize = 1f;
        public static bool NoViewMatrixPrims = false;

        public static List<ScreenRenderTarget> targets = new();
        public static List<ITemporaryRenderTargetHolder> temporaryTargets = new();

        /// <summary>
        /// Caches a temporary render target holder, so it may be automatically disposed after it stops being used
        /// </summary>
        public static void AddTemporaryTarget(ITemporaryRenderTargetHolder tempTarget)
        {
            tempTarget.RenderTargetsDisposed = false;
            temporaryTargets.Add(tempTarget);
        }

        public override void OnModLoad()
        {
            foreach (Type type in AssemblyManager.GetLoadableTypes(CalamityFables.Instance.Code))
            {
                if (type.IsSubclassOf(typeof(ScreenRenderTarget)) && !type.IsAbstract)
                {
                    targets.Add((ScreenRenderTarget)Activator.CreateInstance(type));
                }
            }
        }

        public override void OnModUnload()
        {
            foreach (ScreenRenderTarget rt in targets)
                rt.Dispose();
            targets.Clear();
        }

        public override void PostUpdateEverything()
        {
            CheckScreenSize();

            //Dispose targets that have been doing nothing for too long
            foreach (ITemporaryRenderTargetHolder temporaryRT in temporaryTargets)
            {
                temporaryRT.TicksSinceLastUsedRenderTargets++;
                if (temporaryRT.TicksSinceLastUsedRenderTargets > temporaryRT.AutoDisposeTime)
                {
                    temporaryRT.DisposeOfRenderTargets();
                    temporaryRT.RenderTargetsDisposed = true;
                }
            }
            temporaryTargets.RemoveAll(t => t.RenderTargetsDisposed);
        }

        public override void ClearWorld()
        {
            //Unload all temporary RTs
            foreach (ITemporaryRenderTargetHolder temporaryRT in temporaryTargets)
            {
                temporaryRT.DisposeOfRenderTargets();
                temporaryRT.RenderTargetsDisposed = true;
            }
            temporaryTargets.Clear();
        }

        public delegate void ResizeRenderTargetDelegate();
        public static event ResizeRenderTargetDelegate ResizeRenderTargetEvent;
        private void CheckScreenSize()
        {
            if (!Main.dedServ && !Main.gameMenu)
            {
                Vector2 newScreenSize = new Vector2(Main.screenWidth, Main.screenHeight);
                if (oldScreenSize != newScreenSize)
                {
                    ResizeRenderTargetEvent?.Invoke();
                    foreach (ScreenRenderTarget rt in targets)
                        rt.InitializeTarget();
                }
                oldScreenSize = newScreenSize;
            }
        }

        public delegate void DrawToRenderTargetsDelegate();
        public static event DrawToRenderTargetsDelegate DrawToRenderTargetsEvent;
        private void DrawToRenderTargets(On_Main.orig_CheckMonoliths orig)
        {
            if (!Main.dedServ && Main.spriteBatch != null && Main.graphics.GraphicsDevice != null && !Main.gameMenu)
            {
                DrawToRenderTargetsEvent?.Invoke();
                foreach (ScreenRenderTarget rt in targets)
                {
                    if (!rt.DrawToTarget())
                        rt.framesSinceLastDrawn++;
                }
            }

            orig();
        }

        /// <summary>
        /// Use this to clear your dust caches, before the dusts get updated
        /// Use this instead of looping through every dust before drawing to your RT to save on performance
        /// </summary>
        public static event Action ClearDustCachesEvent;
        public override void PreUpdateDusts() => ClearDustCachesEvent?.Invoke();

        /// <summary>
        /// Use this to sort your dust caches after they have been filled
        /// </summary>
        public static event Action SortDustCachesEvent;
        public override void PostUpdateDusts()
        {
            if (!Main.dedServ)
                SortDustCachesEvent?.Invoke();
        }

        public static bool SwitchToRenderTarget(RenderTarget2D renderTarget)
        {
            GraphicsDevice gD = Main.graphics.GraphicsDevice;

            if (Main.gameMenu || renderTarget is null)
                return false;

            gD.SetRenderTarget(renderTarget);
            gD.Clear(Color.Transparent);
            return true;
        }

        public override void Load()
        {
            On_Main.CheckMonoliths += DrawToRenderTargets;
            IL_TileDrawing.PrepareForAreaDrawing += LogTilePositions;
        }

        private void LogTilePositions(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.EmitDelegate(ResetCachedPositions);

            int tileIndex = 4;
            int iIndex = 2;
            int jIndex = 3;


            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(3),
                i => i.MatchStloc(out iIndex)
                ))
            {
                FablesUtils.LogILEpicFail("Add tile position caching", "Could not locate setting i");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(1),
                i => i.MatchLdcI4(2),
                i => i.MatchSub(),
                i => i.MatchStloc(out jIndex)
                ))
            {
                FablesUtils.LogILEpicFail("Add tile position caching", "Could not locate setting j");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsflda<Main>("tile"),
                i => i.MatchLdloc(jIndex),
                i => i.MatchLdloc(iIndex),
                i => i.MatchCall<Tilemap>("get_Item"),
                i => i.MatchStloc(out tileIndex)
                ))
            {
                FablesUtils.LogILEpicFail("Add tile position caching", "Could not locate the tile local field");
                return;
            }

            ILLabel skipLazyPreparationLabel = null;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(5),
                i => i.MatchBrtrue(out skipLazyPreparationLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Add tile position caching", "Could not locate the lazyPreparation check");
                return;
            }

            cursor.GotoLabel(skipLazyPreparationLabel);
            cursor.Emit(Ldloc, tileIndex);
            cursor.Emit(Ldloc, jIndex);
            cursor.Emit(Ldloc, iIndex);
            cursor.EmitDelegate(CheckTileForCaching);
        }

        private static void ResetCachedPositions() => ClearCachedTilePositions?.Invoke();
        public static event Action ClearCachedTilePositions;

        public delegate void CheckForTileCachingDelegate(Tile t, int i, int j);
        public static event CheckForTileCachingDelegate AddTileToCacheEvent;

        private static void CheckTileForCaching(Tile tile, int i, int j)
        {
            AddTileToCacheEvent?.Invoke(tile, i, j);
        }

        public override void Unload()
        {
            ResizeRenderTargetEvent = null;
        }
    }

    /// <summary>
    /// Use this to register any entity that uses rendertargets to <see cref="RenderTargetsManager"/>, so that they may get auto-disposed after enough time has passed
    /// </summary>
    public interface ITemporaryRenderTargetHolder
    {
        /// <summary>
        /// Reset this to zero whenever the render target holder is updated/drawn so the rendertargets don't get unloaded <br/>
        /// Automatically increments
        /// </summary>
        public int TicksSinceLastUsedRenderTargets { get; set; }
        /// <summary>
        /// How long since the last update/draw until the rendertargets gets disposed of automatically
        /// </summary>
        public int AutoDisposeTime { get; }
        /// <summary>
        /// Automatically set to false when disposed by the rendertargetmanager. Automatically set back to true when registered to the RT manager
        /// </summary>
        public bool RenderTargetsDisposed { get; set; }
        /// <summary>
        /// Run whatever you need here to dispose of the rendertargets
        /// </summary>
        public void DisposeOfRenderTargets();
    }
}