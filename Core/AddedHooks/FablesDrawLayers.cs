using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Generation;
using Terraria.Utilities;
using Terraria.Graphics.Renderers;
using Terraria.DataStructures;
using System.Reflection;

namespace CalamityFables.Core
{
    public enum DrawhookLayer
    {
        AbovePlayer =      0b00001,
        AboveProjectiles = 0b00010,
        AboveNPCs =        0b00100,
        AboveTiles =       0b01000,
        BehindTiles =      0b10000
    }

    /// <summary>
    /// Contains a lot of event hooks at different points of the rendering process
    /// </summary>
    public class FablesDrawLayers : ILoadable
    {
        private static readonly FieldInfo main_playersBehindNPCs = typeof(Main).GetField("_playersThatDrawBehindNPCs", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo main_playersAboveProjs = typeof(Main).GetField("_playersThatDrawAfterProjectiles", BindingFlags.NonPublic | BindingFlags.Instance);

        public static List<Player> PlayersBehindNPCs;
        public static List<Player> PlayersAboveProjectiles;

        public void Load(Mod mod)
        {
            IL_Main.DoDraw_WallsTilesNPCs += DrawHook_AddReasonableDrawing;
            On_Main.DoDraw_WallsAndBlacks += DrawHook_BehindWalls;
            IL_TileDrawing.PostDrawTiles += DrawHook_AddTileEntityDrawing;
            On_TileDrawing.PreDrawTiles += ClearForegroundStuff;
            On_TileDrawing.DrawTrees += DrawHook_AfterTrees;
            On_Main.DoDraw_Tiles_Solid += DrawBehindTiles;
            On_Main.DrawDust += DrawHook_DrawDust;
            On_Main.DoDraw_DrawNPCsOverTiles += DrawHook_AboveNPCs; ;
            On_Main.DrawProjectiles += DrawHook_AboveProjectiles;
            On_LegacyPlayerRenderer.DrawPlayerInternal += ModifyDrawPlayerScale;

            On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += DrawHook_ModifyDrawLayersAfterTransforms;

            On_Main.DrawPlayers_BehindNPCs += DrawHook_AfterPlayersBehindNPCs;
            On_Main.DrawPlayers_AfterProjectiles += DrawHook_AfterPlayers;
            On_Main.RefreshPlayerDrawOrder += ReorderPlayers;

            PlayersBehindNPCs = main_playersBehindNPCs.GetValue(Main.instance) as List<Player>;
            PlayersAboveProjectiles = main_playersAboveProjs.GetValue(Main.instance) as List<Player>;
        }

        public void Unload() { }


        public delegate bool PlayerReorderDelegate(Player player);
        public static event PlayerReorderDelegate ShouldPlayerDrawBehindNPCsEvent;
        private void ReorderPlayers(On_Main.orig_RefreshPlayerDrawOrder orig, Main self)
        {
            orig(self);

            if (Main.gameMenu || Main.dedServ || ShouldPlayerDrawBehindNPCsEvent == null)
                return;

            for (int i = PlayersAboveProjectiles.Count - 1; i >= 0; i--)
            {
                if (ShouldPlayerDrawBehindNPCsEvent.Invoke(PlayersAboveProjectiles[i]))
                {
                    PlayersBehindNPCs.Add(PlayersAboveProjectiles[i]);
                    PlayersAboveProjectiles.RemoveAt(i);
                }
            }
        }

        public delegate void ModifyPlayerDrawVariablesDelegate(Player player, ref Vector2 position, ref float rotation, ref float scale, ref float shadow);
        public static event ModifyPlayerDrawVariablesDelegate ModifyPlayerDrawingEvent;
        private void ModifyDrawPlayerScale(On_LegacyPlayerRenderer.orig_DrawPlayerInternal orig, LegacyPlayerRenderer self, Terraria.Graphics.Camera camera, Player drawPlayer, Vector2 position, float rotation, Vector2 rotationOrigin, float shadow, float alpha, float scale, bool headOnly)
        {
            if (!headOnly)
                ModifyPlayerDrawingEvent?.Invoke(drawPlayer, ref position, ref rotation, ref scale, ref shadow);

            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, alpha, scale, headOnly);
        }

        public delegate void ModifyDrawSetDelegate(ref PlayerDrawSet drawinfo);
        public static event ModifyDrawSetDelegate ModifyDrawLayersAfterTransformsEvent;
        private void DrawHook_ModifyDrawLayersAfterTransforms(On_PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig, ref PlayerDrawSet drawinfo)
        {
            ModifyDrawLayersAfterTransformsEvent?.Invoke(ref drawinfo);
            orig(ref drawinfo);
        }

        #region Draw hooks
        #region IL edits to add the hooks
        private void DrawHook_AddReasonableDrawing(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchCall<Main>("DoDraw_Tiles_NonSolid")))
            {
                FablesUtils.LogILEpicFail("Add custom draw hooks", "Could not locate the Main.DoDraw_Tiles_NonSolid");
                return;
            }
            cursor.EmitDelegate(DrawThings_BehindNonSolidTiles);


            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>("player"),
                i => i.MatchLdsfld<Main>("myPlayer"),
                i => i.MatchLdelemRef(),
                i => i.MatchLdfld<Player>("detectCreature")))
            {
                FablesUtils.LogILEpicFail("Add custom draw hooks", "Could not locate the first Main.DrawPlayers_BehindNPCs");
                return;
            }

            cursor.EmitDelegate(DrawThings_BehindSolidTilesAndBackgroundNPCs);


            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCall<Main>("DrawPlayers_BehindNPCs")))
            {
                FablesUtils.LogILEpicFail("Add custom draw hooks", "Could not locate Main.DrawPlayers_BehindNPCs");
                return;
            }

            cursor.EmitDelegate(DrawThings_AboveSolidTiles);
        }

        private void DrawHook_AddTileEntityDrawing(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCall<TileDrawing>("DrawMasterTrophies")))
            {
                FablesUtils.LogILEpicFail("Add tile entity drawing hook", "Could not locate DrawMasterTrophies call");
                return;
            }
            cursor.EmitDelegate(DrawThings_NonsolidTileEntities);
        }
        #endregion


        //Layers
        //https://media.discordapp.net/attachments/1015737889486802997/1083198437635407963/image.png

        /// <summary>
        /// Drawn under walls, with the only thing drawing underneath that are NPCs who draw in background layers (Moonlord and eternia crystal) <br/>
        /// Example useage: Nautilus's chamber parallax wall
        /// </summary>
        public static event Action DrawThingsBehindWallsEvent;
        private void DrawHook_BehindWalls(On_Main.orig_DoDraw_WallsAndBlacks orig, Main self)
        {
            DrawThingsBehindWallsEvent?.Invoke();
            orig(self);
        }


        /// <summary>
        /// Drawn under everything, but walls and NPCs who draw in background layers (Moonlord and eternia crystal) <br/>
        /// Associated with <see cref="TileDrawLayer.Background"/><br/>
        /// Example useage: Wulfrum Lure's spinning cog, the back layer of the pearlstone grave's auroras
        /// </summary>
        public static event Action DrawThingsBehindNonSolidTilesEvent;
        public static void DrawThings_BehindNonSolidTiles()
        {
            DrawThingsBehindNonSolidTilesEvent?.Invoke();
        }

        /// <summary>
        /// Drawn under all solid tiles, including NPCs who draw behind tiles  <br/>
        /// Associated with <see cref="TileDrawLayer.BehindTiles"/>, although using SpecialDraw on non-solid tiles will also do the same job<br/>
        /// Example useage: Scourge skeletons, crabulon's strings, cogs from wulfrum landfill, the top layer of the pearlstone grave's auroras
        /// </summary>
        public static event Action DrawThingsBehindSolidTilesAndBackgroundNPCsEvent;
        public static void DrawThings_BehindSolidTilesAndBackgroundNPCs()
        {
            DrawThingsBehindSolidTilesAndBackgroundNPCsEvent?.Invoke();
        }

        /// <summary>
        /// Drawn under all solid tiles, but above NPCs who draw behind tiles<br/>
        /// Example useage: Pixelated primitives that are set to <see cref="DrawhookLayer.BehindTiles"/>, such as desert scourge's lightning streaks
        /// </summary>
        public static event Action DrawThingsBehindSolidTilesEvent;
        private void DrawBehindTiles(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
        {
            DrawThingsBehindSolidTilesEvent?.Invoke();
            orig(self);
        }


        /// <summary>
        /// Drawn above all solid tiles, SpecialDraw drawings on solid tiles, NPCs who draw behind tiles, even when the player has hunter potion<br/>
        /// Associated with <see cref="TileDrawLayer.AboveTiles"/><br/>
        /// Example useage: Particles that draw above tiles, cloud metaballs, wulfrum elevator station front layer, Pixelated prims that are set to <see cref="DrawhookLayer.AboveTiles"/>
        /// </summary>
        public static event Action DrawThingsAboveSolidTilesEvent;
        public static void DrawThings_AboveSolidTiles()
        {
            DrawThingsAboveSolidTilesEvent?.Invoke();
        }


        /// <summary>
        /// Drawn above all regular layer NPCs, but below projectiles<br/>
        /// Example useage: Pixelated primitives that are set to <see cref="DrawhookLayer.AboveNPCs"/>, such as the lightning arcs of the ampstring bow
        /// </summary>
        public static event Action DrawThingsAboveNPCsEvent;
        private void DrawHook_AboveNPCs(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            orig(self);
            DrawThingsAboveNPCsEvent?.Invoke();
        }


        public static event Action DrawAboveProjectilesEvent;
        private void DrawHook_AboveProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);
            DrawAboveProjectilesEvent?.Invoke();
        }

        /// <summary>
        /// Draws alongside other vanilla tile entities, right after master mode trophies
        /// Example useage : 
        /// </summary>
        public static event Action DrawNonsolidTileEntitiesEvent;
        public static void DrawThings_NonsolidTileEntities()
        {
            DrawNonsolidTileEntitiesEvent?.Invoke();
        }


        /// <summary>
        /// Drawn after vanilla and regular modded trees<br/>
        /// Example useage: Modded trees with custom drawing
        /// </summary>
        public static event Action DrawThingsAfterTreesEvent;
        private void DrawHook_AfterTrees(On_TileDrawing.orig_DrawTrees orig, TileDrawing self)
        {
            orig(self);
            DrawThingsAfterTreesEvent?.Invoke();
        }


        /// <summary>
        /// Draws behind dust, but above player
        /// Example useage: Spectral dust from nautilus, pixelated primitives that use <see cref="DrawhookLayer.AbovePlayer"/>
        /// </summary>
        public static event Action DrawBehindDustEvent;
        private void DrawHook_DrawDust(On_Main.orig_DrawDust orig, Main self)
        {
            DrawBehindDustEvent?.Invoke();
            orig(self);
        }


        public delegate void PlayerDrawingAction(bool afterProjectiles);

        /// <summary>
        /// Called right before players get drawn
        /// </summary>
        public static event PlayerDrawingAction PreDrawPlayersEvent;
        /// <summary>
        /// Draws right above the player, but below any projectile that draws over them
        /// Example useage: Cloud sprite in a bottle dust
        /// </summary>
        public static event PlayerDrawingAction DrawThingsAbovePlayersEvent;

        private void DrawHook_AfterPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
        {
            PreDrawPlayersEvent?.Invoke(true);
            orig(self);
            DrawThingsAbovePlayersEvent?.Invoke(true);
        }

        private void DrawHook_AfterPlayersBehindNPCs(On_Main.orig_DrawPlayers_BehindNPCs orig, Main self)
        {
            PreDrawPlayersEvent?.Invoke(false);
            orig(self);
            DrawThingsAbovePlayersEvent?.Invoke(false);
        }


        public delegate void ClearTileCacheDelegate(bool solidLayer);
        public static event ClearTileCacheDelegate ClearTileDrawingCachesEvent;
        private static void ClearForegroundStuff(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
        {
            orig(self, solidLayer, forRenderTargets, intoRenderTargets);

            //If we draw every frame or we draw into a RT, it means we're resetting stuff
            if (intoRenderTargets || Lighting.UpdateEveryFrame)
                ClearTileDrawingCachesEvent?.Invoke(solidLayer);
        }
        #endregion
    }
}
