using CalamityFables.Content.Tiles.Graves;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    //Currently barely different from regular sandstone graves. Distinction will be needed for grave ghosts
    public class SandstoneGraveUnsafe : SandstoneGrave
    {
        public override string Texture => AssetDirectory.BurntDesert + "SandstoneGrave";
        public override int RandomStyleRange => 4;
        public override void Load() { }
        public override void Unload() { }
        public override bool WorldgenOnly => true;

        public static int GhostSpawnAttempts = 0;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Tile t = Main.tile[i, j];
            bool moody = !Main.IsItDay() || j > Main.worldSurface + 100 || Main.raining;

            int ghostCap = moody ? 3 : 1;

            if (t.TileFrameY == 0 && t.TileFrameX % 36 == 0)
            {
                if (closer && Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(440))
                {
                    //only spawns ghosts once every X attempts (4 during the day, 2 during the night)
                    GhostSpawnAttempts++;
                    if (!moody && GhostSpawnAttempts < 7)
                        return;
                    else if (moody && GhostSpawnAttempts < 2)
                        return;

                    GhostSpawnAttempts = 0;

                    int ghostType = SandstoneGraveyardGhost.GhostType;
                    int nearbyGhosts = 0;
                    bool[] seenVariants = new bool[SandstoneGraveyardGhost.GHOST_VARIANT_COUNT];

                    foreach (Projectile ghost in SandstoneGraveyardGhost.LoadedGhosts)
                    {
                        if (ghost.active && ghost.type == ghostType && ghost.WithinRange(new Vector2(i, j) * 16, 3000))
                        {
                            nearbyGhosts++;

                            seenVariants[(int)ghost.ai[0]] = true;

                            //Won't spawn ever if a ghost is right on top or if there are too many ghosts
                            if (nearbyGhosts >= ghostCap || ghost.WithinRange(new Vector2(i, j) * 16, 50) || new Vector2(ghost.ai[1], ghost.Center.Y).WithinRange(new Vector2(i, j) * 16, 50))
                                return;
                        }
                    }

                    int variant = Main.rand.Next(SandstoneGraveyardGhost.GHOST_VARIANT_COUNT);
                    while (seenVariants[variant])
                    {
                        variant++;
                        if (variant >= SandstoneGraveyardGhost.GHOST_VARIANT_COUNT)
                            variant = 0;
                    }

                    Vector2 position = new Vector2(i, j) * 16f;
                    int p = Projectile.NewProjectile(Entity.GetSource_NaturalSpawn(), position, Vector2.Zero, ghostType, 0, 0, -1, variant, 0, Main.rand.NextFloat(60f, 120f));
                    
                    if (p < Main.maxProjectiles)
                        SandstoneGraveyardGhost.LoadedGhosts.Add(Main.projectile[p]);
                }
            }
        }

        public override void MouseOver(int i, int j) {  }
    }
}
