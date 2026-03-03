using CalamityFables.Content.Boss.MushroomCrabBoss;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.WorldBuilding;

namespace CalamityFables.Core
{
    public partial class FablesWorld : ModSystem
    {
        public static int PlacedHookstaves = 0;
        public static readonly List<Point> MushroomBiomesWithHookstaves = new List<Point>();
        public static readonly List<Point> HookstaffPositions = new List<Point>();

        //Avoids the walls from breaking weirdly
        private void PreventHookstaffWallDestruction(int i, int j, int type, ref bool fail)
        {
            if (WorldGen.generatingWorld && Main.tile[i, j].TileType == ModContent.TileType<HookstaffTile>())
                fail = true;
        }

        private void PlaceCabinHookstaff(On_HouseBuilder.orig_PlaceBiomeSpecificPriorityTool orig, HouseBuilder self, HouseBuilderContext context)
        {
            orig(self, context);

            if (self.Type != HouseType.Mushroom || PlacedHookstaves >= 8)
                return;

            int hookstaffTileType = ModContent.TileType<HookstaffTile>();

            //Only generate in the center third of the world - not
            //float worldThird = Main.maxTilesX / 3f;
            //if (self.TopRoom.X < worldThird || self.TopRoom.Y > worldThird * 2f)
            //    return;

            bool successfullyPlacedHookstaff = false;
            Point closestMushroomBiome = GenVars.mushroomBiomesPosition.Take(GenVars.numMushroomBiomes).
                OrderBy(p => p.DistanceTo(self.TopRoom.Center)).
                FirstOrDefault();

            if (MushroomBiomesWithHookstaves.Contains(closestMushroomBiome))
                return;

            foreach (Rectangle room in self.Rooms)
            {
                int ceilingLevel = room.Y + 3;

                //Randomly try to place hookstaves
                for (int i = 0; i < 10; i++)
                {
                    int randomX = WorldGen.genRand.Next(4, room.Width - 4) + room.X;
                    WorldGen.PlaceTile(randomX, ceilingLevel, hookstaffTileType, mute: true, forced: true);
                    if (successfullyPlacedHookstaff = Main.tile[randomX, ceilingLevel].HasTile && Main.tile[randomX, ceilingLevel].TileType == hookstaffTileType)
                    {
                        HookstaffPositions.Add(new Point(randomX, ceilingLevel));
                        if (WorldGen.genRand.NextBool())
                            MoldifyRoom(room);

                        break;
                    }
                }

                if (successfullyPlacedHookstaff)
                    break;

                //Scan across the room
                for (int j = room.X + 2; j <= room.X + room.Width - 2; j++)
                {
                    if (successfullyPlacedHookstaff = WorldGen.PlaceTile(j, ceilingLevel, hookstaffTileType, mute: true, forced: true))
                    {
                        HookstaffPositions.Add(new Point(j, ceilingLevel));

                        if (WorldGen.genRand.NextBool())
                            MoldifyRoom(room);

                        break;
                    }
                }

                if (successfullyPlacedHookstaff)
                    break;
            }

            if (successfullyPlacedHookstaff)
            {
                MushroomBiomesWithHookstaves.Add(closestMushroomBiome);
                PlacedHookstaves++;
            }
        }

        public static void MoldifyRoom(Rectangle room)
        {
            for (int i = room.X; i < room.X + room.Width; i++)
                for (int j = room.Y; j < room.Y + room.Height; j++)
                {
                    Tile t = Main.tile[i, j];
                    if (t.HasTile && t.TileType == TileID.MushroomBlock)
                        t.TileType = (ushort)ModContent.TileType<Content.Items.CrabulonDrops.MyceliumMoldEcho>();
                }    
        }

        public static void PlaceBuriedHookstaff()
        {
            Point worldTop = new Point(Main.maxTilesX / 2, 0);
            var closestMushroomBiome = GenVars.mushroomBiomesPosition.Take(GenVars.numMushroomBiomes).
                    OrderBy(p => p.DistanceTo(worldTop));

            int buriedHookstaffType = ModContent.TileType<HookstaffBuriedTile>();

            //Make sure the closest 4 mushroom biomes have a hookstaff
            for (int i = 0; i < Math.Min(GenVars.numMushroomBiomes, 4); i++)
            {
                Point position = closestMushroomBiome.ElementAt(i);
                //If the hookstaff was already placed in a cabin, we are fine
                if (MushroomBiomesWithHookstaves.Contains(position))
                    continue;

                int left = Math.Max(0, position.X - 80);
                int right = Math.Min(Main.maxTilesX, position.X + 80);

                int top = Math.Max(0, position.Y - 80);
                int bottom = Math.Min(Main.maxTilesY, position.Y + 80);

                bool placedHookstaff = false;


                for (int randomAttempts = 0; randomAttempts < 15; randomAttempts++)
                {
                    int randomX = Main.rand.Next(left, right);

                    for (int y = top + 15; y < bottom - 6; y++)
                    {
                        Tile t = Main.tile[randomX, y];
                        if (t.HasTile && t.TileType == TileID.MushroomGrass)
                            placedHookstaff = WorldGen.PlaceTile(randomX, y - 4, buriedHookstaffType, mute: true, forced: true) && Main.tile[randomX, y - 4].HasTile && Main.tile[randomX, y - 4].TileType == buriedHookstaffType;

                        if (placedHookstaff)
                            break;
                    }
                    if (placedHookstaff)
                        break;
                }

                if (placedHookstaff)
                    continue;

                for (int x = left + 10; x < right - 10; x++)
                {
                    for (int y = top; y < bottom; y++)
                    {
                        Tile t = Main.tile[x, y];
                        if (t.HasTile && t.TileType == TileID.MushroomGrass)
                            placedHookstaff = WorldGen.PlaceTile(x, y - 4, buriedHookstaffType, mute: true, forced: true) && Main.tile[x, y - 4].HasTile && Main.tile[x, y - 4].TileType == buriedHookstaffType;

                        if (placedHookstaff)
                            break;
                    }
                    if (placedHookstaff)
                        break;
                }
            }
        }

        public static void CleanupCabinHookstaves()
        {
            foreach (Point p in HookstaffPositions)
            {
                bool needsReplacement = false;

                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        Tile t = Main.tile[p.X + i - 3, p.Y + j - 1];

                        if (t.WallType == 0)
                            WorldGen.PlaceWall(p.X + i - 3, p.Y + j - 1, WallID.Mushroom, true);

                        if (t.HasTile && t.TileType == ModContent.TileType<HookstaffTile>())
                        {
                            t.TileFrameX = (short)(i * 18);
                            t.TileFrameY = (short)(j * 18);
                        }
                        //Sometimes the hookstaff will just break, so we put it again
                        else if (!t.HasTile)
                            needsReplacement = true;
                    }
                }

                if (needsReplacement)
                    WorldGen.PlaceTile(p.X, p.Y, ModContent.TileType<HookstaffTile>(), mute: true, forced: true);
            }
        }
    }
}