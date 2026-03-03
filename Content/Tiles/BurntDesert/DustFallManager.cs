using CalamityFables.Content.Tiles.BurntDesert;
using Microsoft.Xna.Framework.Graphics;
using Stubble.Core.Classes;
using System.IO;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class DustFallManager : ModSystem
    {
        public static Dictionary<Point, DustFall> DustfallsByPosition;

        public override void Load()
        {
            DustfallsByPosition = new Dictionary<Point, DustFall>();

            if (!Main.dedServ)
            {
                FablesDrawLayers.DrawThingsBehindSolidTilesAndBackgroundNPCsEvent += DrawDustfalls;
            }
        }

        #region Placing
        /// <summary>
        /// Basic check for if a dust can be placed on this tile: Checking if there's at least one adjacent empty tile and that there is no other opalescent prism placed there already
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool PositionValidForDustFall(Point position, int dustFallDir = 1, bool checkContinuedExistence = false)
        {
            if (!WorldGen.InWorld(position.X, position.Y))
                return false;

            Tile tile = Framing.GetTileSafely(position);
            //Only placeable on solid tiles
            if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                return false;

            //Only placeable on tiles that don't already contain a dust fall
            if (!checkContinuedExistence && DustfallsByPosition.ContainsKey(position))
                return false;

            //Needs an empty tile to fall down to
            Tile adjacentTile = Framing.GetTileSafely(position + new Point(0, dustFallDir));
            if (!adjacentTile.HasTile || !Main.tileSolid[adjacentTile.TileType] || Main.tileSolidTop[adjacentTile.TileType])
                return true;

            return false;
        }

        /// <summary>
        /// Attempts to place a dust fall at the specified coordinates. Returns true if the placement succeeded
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool TryPlaceDustfall(Point position, bool ghostly = false, bool naturallyPlaced = false)
        {
            DustFall dustFall = new DustFall(position);
            dustFall.ghostly = ghostly;
            dustFall.naturallySpawned = naturallyPlaced;
            return true;
        }
        #endregion

        public static void RemoveDustFall(DustFall dustfall)
        {
            dustfall.OnBreak();
            DustfallsByPosition.Remove(dustfall.anchor);
        }

        /// <summary>
        /// Gets the dustfall object associated with the coordinates, provided that the tile in question is an <see cref="OpalescentPrismTile"/> and using its frameX/frameY as an offset from its position to the crystal's anchor 
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static DustFall GetDustfall(int i, int j)
        {
            if (DustfallsByPosition.TryGetValue(new Point(i, j), out var dustFall))
                return dustFall;
            return null;
        }

        #region Update and draw hooks
        public override void PostUpdateEverything()
        {
            //Update all dust falls
            foreach (var item in DustfallsByPosition)
                item.Value.Update();
        }

        private void DrawDustfalls()
        {
            if (DustfallsByPosition.Count == 0)
                return;
            List<DustFall> visibleDustFalls = new();
            foreach (var item in DustfallsByPosition)
            {
                DustFall dustFall = item.Value;
                Rectangle drawRect = new Rectangle(dustFall.anchor.X * 16 - 200 - (int)Main.screenPosition.X, (int)dustFall.anchor.Y * 16 - 500 - (int)Main.screenPosition.Y, 400, 600);
                if (FablesUtils.OnScreen(drawRect))
                    visibleDustFalls.Add(dustFall);
            }

            if (visibleDustFalls.Count == 0)
                return;

            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            Matrix translation = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0));

            Effect dustfallEffect = Scene["CeilingDustColumn"].GetShader().Shader;
            dustfallEffect.Parameters["uWorldViewProjection"].SetValue(translation * Main.GameViewMatrix.TransformationMatrix * projection);
            dustfallEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.06f);
            dustfallEffect.Parameters["resolution"].SetValue(screenSize);
            dustfallEffect.Parameters["dustThresholds"].SetValue(new Vector2(0.99f, 0.99f));

            dustfallEffect.Parameters["noise"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value); //Voronoi
            dustfallEffect.Parameters["dustNoise"].SetValue(AssetDirectory.NoiseTextures.RGBGrime.Value); //Rainbow grime
            dustfallEffect.Parameters["rgbNoise"].SetValue(AssetDirectory.NoiseTextures.RGBDark.Value); //RGB noise

            dustfallEffect.Parameters["columnHeight"].SetValue(DustFall.COLUMN_HEIGHT);


            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[4 * visibleDustFalls.Count];
            short[] indices = new short[6 * visibleDustFalls.Count];
            int index = 0;

            foreach (DustFall dustFall in visibleDustFalls)
            {
                dustFall.DrawGlow(index, ref vertices, ref indices);
                index++;
            }

            dustfallEffect.CurrentTechnique.Passes[0].Apply();
            var lastRasterizerState = Main.instance.GraphicsDevice.RasterizerState;
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4 * visibleDustFalls.Count, indices, 0, 2 * visibleDustFalls.Count);
            Main.instance.GraphicsDevice.RasterizerState = lastRasterizerState;
        }
        #endregion

        #region Saving and loading data

        public override void SaveWorldData(TagCompound tag)
        {
            TagCompound dustFallTag = new TagCompound();
            tag["dustfallCount"] = DustfallsByPosition.Count;

            int p = 0;
            foreach (var item in DustfallsByPosition)
            {
                dustFallTag.Add("dustfall" + p, DustFall.Serialize(item.Value));
                p++;
            }
            tag["dustfalls"] = dustFallTag;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            DustfallsByPosition.Clear();

            if (tag.TryGet<int>("dustfallCount", out int count) && tag.TryGet<TagCompound>("dustfalls", out TagCompound dustFallTag))
            {
                for (int p = 0; p < count; p++)
                {
                    DustFall.Deserialize(dustFallTag.GetCompound("dustfall" + p));
                }
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(DustfallsByPosition.Count);
            foreach (var item in DustfallsByPosition)
                item.Value.NetSend(writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            int dustFallCount = reader.ReadInt32();
            DustfallsByPosition.Clear();

            for (int p = 0; p < dustFallCount; p++)
            {
                DustFall.NetReceive(reader);
            }
        }
        #endregion
    }
}