using CalamityFables.Particles;
using Terraria.DataStructures;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class DesertRumbles : ILoadable
    {
        public static readonly SoundStyle RumbleSound = new SoundStyle(SoundDirectory.Sounds + "DesertRumble");

        public const int RUMBLE_PROBABILITY = 60 * 4 * 60;

        public static int rumbleCooldown = 0;
        public static int rumbleTimer = 0;
        public static int rumbleFullDuration = 0;
        public static Point rumblePosition;
        public static readonly List<(Point, float, float)> rumbleDustFalls = new();

        public void Load(Mod mod)
        {
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += RandomRumbleChance;
        }

        private void RandomRumbleChance()
        {
            //Rumbles dont happen serverside
            if (Main.dedServ)
                return;

            for (int i = rumbleDustFalls.Count - 1; i >= 0; i--)
            {
                var dustFallData = rumbleDustFalls[i];

                //Timer before the rumble happens
                if (dustFallData.Item2 > 0)
                {
                    dustFallData.Item2 -= 1 / 60f;
                    rumbleDustFalls[i] = dustFallData;
                    continue;
                }

                Vector2 rumblePos = dustFallData.Item1.ToWorldCoordinates() + Main.rand.NextVector2Circular(8f, 8f);
                if (Main.rand.NextBool(6))
                {
                    int dustType = Main.rand.NextBool() ? 32 : Main.rand.NextBool() ? 287 : Main.rand.NextBool() ? 280 : 283;
                    Dust.NewDustPerfect(rumblePos, dustType, Vector2.UnitY * 2f, Scale: Main.rand.NextFloat(0.8f, 1.2f));
                }

                if (Main.rand.NextBool(6))
                {
                    float dustSpacing = (float)Math.Pow(Main.rand.NextFloat(), 1.5f);
                    Vector2 dustOrigin = rumblePos + Vector2.UnitX * dustSpacing * 7f * (Main.rand.NextBool() ? -1 : 1);
                    dustOrigin += Vector2.UnitY * Main.rand.NextFloat(50f);

                    Vector2 dustSpeed = Vector2.UnitY * Main.rand.NextFloat(4f, 8f) * (dustSpacing * 0.6f + 0.4f);

                    Dust zeSand = Dust.NewDustPerfect(dustOrigin, 148, dustSpeed);
                    zeSand.fadeIn = 1f;
                    zeSand.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                }

                if (Main.rand.NextBool(9))
                {
                    Particle smokey = new SmallSmokeParticle(rumblePos, Vector2.UnitY * Main.rand.NextFloat(4f, 20f), Color.SaddleBrown * 0.6f, Color.SaddleBrown * 0.6f, Main.rand.NextFloat(1f, 1.5f), 150, MathHelper.PiOver4 * 0.03f * Main.rand.NextFloatDirection(), true);
                    ParticleHandler.SpawnParticle(smokey);
                }

                if (Main.rand.NextBool(3))
                {
                    Tile rumbledTile = Main.tile[dustFallData.Item1];
                    Dust rubble = Main.dust[WorldGen.KillTile_MakeTileDust(dustFallData.Item1.X, dustFallData.Item1.Y, rumbledTile)];
                    rubble.position = rumblePos;
                    rubble.noGravity = true;
                    rubble.velocity = Vector2.UnitY * 4f;
                    rubble.scale = Main.rand.NextFloat(0.5f, 0.8f);
                }

                dustFallData.Item3 -= 1 / 60f;
                rumbleDustFalls[i] = dustFallData;
                if (dustFallData.Item3 <= 0)
                    rumbleDustFalls.RemoveAt(i);
                continue;
            }

            if (rumbleTimer > 0)
            {
                //Screenshake
                rumbleTimer--;
                float rumbleProgress = rumbleTimer / (float)rumbleFullDuration;
                float effectIntensity = (float)Math.Pow(Utils.GetLerpValue(1f, 0.7f, rumbleProgress, true), 0.6f); //Fade in
                effectIntensity *= Utils.GetLerpValue(0f, 0.5f, rumbleProgress, true); //Fade out

                float rumbleStrenght = 10f * (0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2f));
                rumbleStrenght *= Utils.GetLerpValue(3000f, 1000f, Main.LocalPlayer.Center.Distance(rumblePosition.ToWorldCoordinates()), true);

                CameraManager.Quake = Math.Max(CameraManager.Quake, rumbleStrenght * effectIntensity);
                return;
            }

            //Rumbles only trigger post DS
            if (WorldProgressionSystem.DefeatedDesertScourge)
                return;

            if (rumbleCooldown > 0)
            {
                rumbleCooldown--;
                return;
            }    

            if (!Main.LocalPlayer.dead && Main.LocalPlayer.ZoneUndergroundDesert && Main.rand.NextBool(RUMBLE_PROBABILITY))
            {
                Point rumbleCenter = Main.LocalPlayer.Center.ToTileCoordinates() + Main.rand.NextVector2Circular(1000f, 1000f).ToTileCoordinates();
                int rumbleDuration = Main.rand.Next((int)(6.5f * 60), 8 * 60);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    new DesertRumblesPacket(rumbleCenter, rumbleDuration).Send(-1, -1, false);
                else
                    StartRumble(rumbleCenter, rumbleDuration);
            }
        }

        public static void StartRumble(Point origin, int duration)
        {
            rumblePosition = origin;
            rumbleTimer = duration;
            rumbleFullDuration = duration;
            SoundEngine.PlaySound(RumbleSound, Vector2.Lerp(Main.LocalPlayer.Center, rumblePosition.ToWorldCoordinates(), 0.7f));

            //5-8 minutes cooldown
            rumbleCooldown = 60 * Main.rand.Next(5 * 60, 8 * 60);

            for (int i = 0; i < 16; i++)
            {
                Vector2 randomTilePos = Main.rand.NextVector2FromRectangle(new Rectangle(rumblePosition.X * 16 - 400, rumblePosition.Y * 16 - 400, 800, 800));
                Point tilePos = randomTilePos.ToTileCoordinates();

                tilePos.X = Math.Clamp(tilePos.X, 5, Main.maxTilesX - 5);
                tilePos.Y = Math.Clamp(tilePos.Y, 55, Main.maxTilesY - 55);

                int tries = 0;

                bool insideTerrain = Main.tile[tilePos].HasUnactuatedTile && Main.tileSolid[Main.tile[tilePos].TileType];
                if (insideTerrain)
                {
                    while (insideTerrain)
                    {
                        insideTerrain = Main.tile[tilePos].HasUnactuatedTile && Main.tileSolid[Main.tile[tilePos].TileType];
                        tilePos.Y++;
                        if (tries++ > 20)
                            break;
                    }

                    if (!insideTerrain)
                    {
                        if (Main.tile[tilePos].Slope != SlopeType.Solid)
                            tilePos.Y--;
                        rumbleDustFalls.Add((tilePos, Main.rand.NextFloat(0f, 3f), Main.rand.NextFloat(1f, 4f)));
                    }
                }
                else
                {
                    while (!insideTerrain)
                    {
                        insideTerrain = Main.tile[tilePos].HasUnactuatedTile && Main.tileSolid[Main.tile[tilePos].TileType];
                        tilePos.Y--;
                        if (tries++ > 20)
                            break;
                    }

                    if (insideTerrain)
                    {
                        tilePos.Y++;
                        if (Main.tile[tilePos].Slope != SlopeType.Solid)
                            tilePos.Y--;
                        rumbleDustFalls.Add((tilePos, Main.rand.NextFloat(0f, 3f), Main.rand.NextFloat(1f, 4f)));
                    }
                }
            }
        }

        public void Unload() { }
    }

    [Serializable]
    public class DesertRumblesPacket : Module
    {
        public Point rumbleOrigin;
        public int rumbleDuration;

        public DesertRumblesPacket(Point rumbleOrigin,int rumbleDuration)
        {
            this.rumbleOrigin = rumbleOrigin;
            this.rumbleDuration = rumbleDuration;
        }

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, -1, false);
            }
            else
            {
                DesertRumbles.StartRumble(rumbleOrigin, rumbleDuration);
            }
        }
    }
}
