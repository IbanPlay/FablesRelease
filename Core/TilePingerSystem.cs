using System.Runtime.Serialization;
using Terraria.DataStructures;
using Terraria.Graphics.Light;
using Terraria.ModLoader.Core;

namespace CalamityFables.Core
{
    public interface IPingedTileEffect
    {
        /// <summary>
        /// The blend state of the tile effect's main shader.
        /// </summary>
        public BlendState BlendState => BlendState.AlphaBlend;
        /// <summary>
        /// Create and set up an effect to be drawn over all the registered tiles for this shader.
        /// </summary>
        /// <returns>The configured effect</returns>
        public Effect SetupEffect();
        /// <summary>
        /// Modifies the shader for each tile. Use this if your shader is using tile-specific data.
        /// </summary>
        /// <param name="pos">The position of the tile</param>
        /// <param name="effect">The effect being used</param>
        public void PerTileSetup(Point pos, ref Effect effect) { }
        /// <summary>
        /// Draws the tile, or an overlay for it. The shader is automatically applied.
        /// </summary>
        /// <param name="pos">The position of the tile</param>
        public void DrawTile(Point pos);
        /// <summary>
        /// What happens when a ping for this effect gets requested. Return false if the ping couldn't get added.
        /// </summary>
        /// <param name="position">Position of the ping being requested</param>
        /// <param name="pinger">The player that initiated the ping</param>
        /// <returns>Wether or not the ping's setup was successful</returns>
        public bool TryAddPing(Vector2 position, Player pinger);
        /// <summary>
        /// Wether or not this effect is active.
        /// </summary>
        public bool Active => true;
        /// <summary>
        /// Check to know if a tile needs to be drawn with this effect. This is only called if the effect is active.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>Wether or not the tile should be drawn with the effect</returns>
        public bool ShouldRegisterTile(int x, int y);
        /// <summary>
        /// Called after a tile has been queued to be drawn with this effect. Can be used to edit the draw data of the tile.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="drawData">The tile's draw data</param>
        public void EditDrawData(int i, int j, ref TileDrawInfo drawData) { }
        /// <summary>
        /// Update call ran once per frame.
        /// </summary>
        public void UpdateEffect() { }
    }

    public class TilePingerSystem : ModSystem
    {
        internal static Dictionary<IPingedTileEffect, List<Point>> pingedTiles;
        internal static Dictionary<IPingedTileEffect, List<Point>> pingedNonSolidTiles;
        internal static Dictionary<IPingedTileEffect, List<Point>> drawCache;

        internal static Dictionary<string, IPingedTileEffect> tileEffects;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            Terraria.GameContent.Drawing.On_TileDrawing.Draw += ClearTilePings;

            drawCache = new Dictionary<IPingedTileEffect, List<Point>>();
            pingedTiles = new Dictionary<IPingedTileEffect, List<Point>>();
            pingedNonSolidTiles = new Dictionary<IPingedTileEffect, List<Point>>();
            tileEffects = new Dictionary<string, IPingedTileEffect>();

            foreach (Type type in AssemblyManager.GetLoadableTypes(CalamityFables.Instance.Code))
            {
                if (type.GetInterface(nameof(IPingedTileEffect)) != null)
                {
                    tileEffects.Add(type.Name, (IPingedTileEffect)FormatterServices.GetUninitializedObject(type));

                }
            }

        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            Terraria.GameContent.Drawing.On_TileDrawing.Draw -= ClearTilePings;
            drawCache = null;
            pingedTiles = null;
            pingedNonSolidTiles = null;
            tileEffects = null;
        }

        //Demonic
        private static void ClearTilePings(Terraria.GameContent.Drawing.On_TileDrawing.orig_Draw orig, Terraria.GameContent.Drawing.TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets, int waterStyleOverride)
        {
            //Retro & Trippy light modes are fine. Just reset the cache before every time stuff gets drawn.
            if (Lighting.UpdateEveryFrame)
            {
                //But only if its not on the non solid layer, assumedly because it draws first or something
                if (!solidLayer)
                    ClearTiles();
            }

            else
            {
                //For the white color mode, we also can simply clear all the cache at once, but this time its only on the solid layers. Don't ask me why i don't know it just works
                if (Lighting.Mode == LightMode.White)
                {
                    if (solidLayer)
                        ClearTiles();
                }

                //In color mode, the tiles get cleared alternating between solid and non solid tiles
                else
                    ClearTiles(solidLayer);

            }
            orig(self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride);
        }


        public static bool AddPing(string effectName, Vector2 position, Player pinger)
        {
            if (!Main.dedServ)
                return AddPing(tileEffects[effectName], position, pinger);

            return false;
        }

        public static bool AddPing(IPingedTileEffect effect, Vector2 position, Player pinger) => effect.TryAddPing(position, pinger);


        public static void RegisterTileToDraw(Point tilePos, string effectName, bool solid = true) => RegisterTileToDraw(tilePos, tileEffects[effectName], solid);
        public static void RegisterTileToDraw(Point tilePos, IPingedTileEffect effect, bool solid = true)
        {
            //Unless we are in color light mode, we do not need the distinction between solid and non solid tiles.
            if (solid || !(Lighting.Mode == LightMode.Color))
            {
                if (!pingedTiles.ContainsKey(effect))
                    pingedTiles.Add(effect, new List<Point>());

                if (!pingedTiles[effect].Contains(tilePos))
                    pingedTiles[effect].Add(tilePos);
            }

            else
            {
                if (!pingedNonSolidTiles.ContainsKey(effect))
                    pingedNonSolidTiles.Add(effect, new List<Point>());

                if (!pingedNonSolidTiles[effect].Contains(tilePos))
                    pingedNonSolidTiles[effect].Add(tilePos);


            }
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;

            foreach (IPingedTileEffect effect in tileEffects.Values)
            {
                effect.UpdateEffect();
            }
        }

        public override void PostDrawTiles()
        {
            if (pingedTiles.Keys.Count + pingedNonSolidTiles.Count < 1)
                return;

            drawCache.Clear();

            foreach (IPingedTileEffect solidEffect in pingedTiles.Keys)
            {
                drawCache.Add(solidEffect, pingedTiles[solidEffect].ConvertAll(position => new Point(position.X, position.Y)));
            }

            if (Lighting.Mode == LightMode.Color)
            {
                foreach (IPingedTileEffect nonSolidEffect in pingedNonSolidTiles.Keys)
                {
                    List<Point> clonedList = pingedNonSolidTiles[nonSolidEffect].ConvertAll(position => new Point(position.X, position.Y));

                    if (!drawCache.ContainsKey(nonSolidEffect))
                        drawCache.Add(nonSolidEffect, clonedList);

                    else
                    {
                        drawCache[nonSolidEffect].AddRange(clonedList);
                    }
                }
            }

            foreach (IPingedTileEffect tileEffect in drawCache.Keys)
            //foreach (IPingedTileEffect tileEffect in pingedTiles.Keys)
            {
                Effect effect = tileEffect.SetupEffect();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, tileEffect.BlendState, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

                foreach (Point tilePos in drawCache[tileEffect])
                //foreach (Point tilePos in pingedTiles[tileEffect])
                {
                    tileEffect.PerTileSetup(tilePos, ref effect);
                    tileEffect.DrawTile(tilePos);

                }

                Main.spriteBatch.End();
            }
        }

        public static void ClearTiles()
        {
            ClearTiles(true);
            ClearTiles(false);
        }
        public static void ClearTiles(bool solid)
        {
            if (solid)
                pingedTiles.Clear();

            else
                pingedNonSolidTiles.Clear();
        }
    }

    public class GlobalPingableTile : GlobalTile
    {
        public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            foreach (IPingedTileEffect effect in TilePingerSystem.tileEffects.Values)
            {
                if (effect.Active && effect.ShouldRegisterTile(i, j))
                {
                    int tileType = Main.tile[i, j].TileType;
                    bool solid = true;

                    //Necessary separation in the color lighting mode.
                    if (Lighting.Mode == LightMode.Color)
                    {
                        if (TileID.Sets.DrawTileInSolidLayer[tileType].HasValue)
                            solid = TileID.Sets.DrawTileInSolidLayer[tileType].Value;
                        else
                            solid = Main.tileSolid[tileType];
                    }

                    TilePingerSystem.RegisterTileToDraw(new Point(i, j), effect, solid);
                    effect.EditDrawData(i, j, ref drawData);
                }
            }
        }
    }
}
