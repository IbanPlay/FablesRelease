using CalamityFables.Content.Tiles.Graves;
using CalamityFables.Particles;
using Microsoft.Build.Tasks;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class SpectralWater : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;
        public static readonly SoundStyle HauntSound = new SoundStyle(SoundDirectory.Sounds + "SpectralWaterUse", 2) { PitchVariance = 0.1f };

        public static int ItemType;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Spectral Water");
            Tooltip.SetDefault("Contains swirling spirits inside\n" +
                "Can be used on player-placed sandstone graves to make them spawn ghosts\n" +
                "Can also force sandstone light sources to appear as spectral fire");

            ItemType = Type;
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.BottledWater).
                AddCondition(SealedChamber.InNautilusChamber).
                Register();
        }

        public override bool? UseItem(Player player)
        {
            Tile tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);
            int sandstoneGraveType = ModContent.TileType<SandstoneGrave>();
            int unsafeGraveType = ModContent.TileType<SandstoneGraveUnsafe>();

            if (tile.HasTile && player.IsInTileInteractionRange(Player.tileTargetX, Player.tileTargetY, TileReachCheckSettings.Simple))
            {
                if (tile.TileType == ModContent.TileType<SandstoneGrave>())
                {
                    Point anchor = new Point(Player.tileTargetX - (tile.TileFrameX % 36) / 18, Player.tileTargetY - (tile.TileFrameY % 36) / 18);

                    for (int i = 0; i < 2; i++)
                        for (int j = 0; j < 2; j++)
                        {
                            Tile replaceTile = Main.tile[i + anchor.X, j + anchor.Y];
                            if (replaceTile.TileType == sandstoneGraveType)
                                replaceTile.TileType = (ushort)unsafeGraveType;
                        }

                    int dustType = ModContent.DustType<SpectralWaterDustEmbers>();
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 position = anchor.ToWorldCoordinates(0, 30);
                        position.X += i / 12f * 32 + Main.rand.NextFloat(-5f, 5f);
                        position.Y -= Main.rand.NextFloat(0f, 6f);

                        Dust.NewDustPerfect(position, dustType, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 2f), 0, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                    }



                    ParticleHandler.SpawnParticle(new CircularPulseShine(anchor.ToWorldCoordinates(16, 16), Color.Cyan with { A = 0 }, Main.rand.NextFloat(0.7f, 1f)));


                    //NetMessage.SendTileSquare(-1, anchor.X, anchor.Y, 2, 2, TileChangeType.ShimmerWater);
                    SoundEngine.PlaySound(HauntSound, player.Center);
                    return true;
                }

                else if (tile.TileType == ModContent.TileType<SandstoneCampfireTile>() && tile.TileFrameX < 54)
                {
                    int topX = Player.tileTargetX - tile.TileFrameX / 18;
                    int topY = Player.tileTargetY - tile.TileFrameY % 48 / 24;

                    for (int x = topX; x < topX + 3; x++)
                        for (int y = topY; y < topY + 2; y++)
                            Main.tile[x, y].TileFrameX += 54;

                    int dustType = ModContent.DustType<SpectralWaterDustEmbers>();
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 position = new Vector2(topX * 16, (topY + 2) * 16);
                        position.X += i / 12f * 48 + Main.rand.NextFloat(-5f, 5f);
                        position.Y -= Main.rand.NextFloat(0f, 6f);
                        Dust.NewDustPerfect(position, dustType, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 2f), 0, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                    }


                    ParticleHandler.SpawnParticle(new CircularPulseShine(new Vector2(topX * 16 + 24, topY * 16 + 16), Color.Cyan with { A = 0 }, Main.rand.NextFloat(0.7f, 1f)));

                    SoundEngine.PlaySound(HauntSound, player.Center);
                    return true;
                }
                else if (tile.TileType == ModContent.TileType<TripodTorchTile>() && tile.TileFrameX == 0)
                {
                    int topY = Player.tileTargetY - tile.TileFrameY / 26 % 3;
                    for (int y = topY; y < topY + 3; y++)
                        Main.tile[Player.tileTargetX, y].TileFrameX += 18;


                    int dustType = ModContent.DustType<SpectralWaterDustEmbers>();
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 position = new Vector2(Player.tileTargetX * 16, (topY + 3) * 16);
                        position.X += i / 7f * 16 + Main.rand.NextFloat(-5f, 5f);
                        position.Y -= Main.rand.NextFloat(0f, 6f);
                        Dust.NewDustPerfect(position, dustType, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 2f), 0, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                    }


                    ParticleHandler.SpawnParticle(new CircularPulseShine(new Vector2(Player.tileTargetX * 16 + 8, topY * 16 + 6), Color.Cyan with { A = 0 }, Main.rand.NextFloat(0.7f, 1f)));

                    SoundEngine.PlaySound(HauntSound, player.Center);
                    return true;
                }
                else if (tile.TileType == ModContent.TileType<DesertBrazierTile>() && tile.TileFrameY < 54)
                {
                    int topX = Player.tileTargetX - tile.TileFrameX % 54 / 18;
                    int topY = Player.tileTargetY - tile.TileFrameY / 18;

                    for (int x = topX; x < topX + 3; x++)
                        for (int y = topY; y < topY + 3; y++)
                            Main.tile[x, y].TileFrameY += 54;

                    int dustType = ModContent.DustType<SpectralWaterDustEmbers>();
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 position = new Vector2(topX * 16, (topY + 1) * 16);
                        position.X += i / 12f * 48 + Main.rand.NextFloat(-5f, 5f);
                        position.Y -= Main.rand.NextFloat(0f, 6f);
                        Dust.NewDustPerfect(position, dustType, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 2f), 0, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                    }

                    ParticleHandler.SpawnParticle(new CircularPulseShine(new Vector2(topX * 16 + 24, topY * 16 + 24), Color.Cyan with { A = 0 }, Main.rand.NextFloat(0.7f, 1f)));

                    SoundEngine.PlaySound(HauntSound, player.Center);
                    return true;
                }
                else if (tile.TileType == ModContent.TileType<SandstonePillarTile>())
                {
                    int targetY = Player.tileTargetY;
                    SandstonePillarTile.HasBrazier(Player.tileTargetX, targetY, out SandstonePillarTile.BrasierPlacementState state);
                    if (state == SandstonePillarTile.BrasierPlacementState.FreeCenterAboveBrasier)
                    {
                        targetY++;
                        SandstonePillarTile.HasBrazier(Player.tileTargetX, targetY, out state);
                    }

                    if (state == SandstonePillarTile.BrasierPlacementState.Brasier)
                    {
                        Tile pillarTile = Main.tile[Player.tileTargetX, targetY];
                        pillarTile.TileFrameX += 1;

                        int dustType = ModContent.DustType<SpectralWaterDustEmbers>();
                        for (int i = 0; i < 12; i++)
                        {
                            Vector2 position = new Vector2(Player.tileTargetX * 16 - 8, (targetY) * 16);
                            position.X += i / 12f * 32 + Main.rand.NextFloat(-5f, 5f);
                            position.Y -= Main.rand.NextFloat(0f, 6f);
                            Dust.NewDustPerfect(position, dustType, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 2f), 0, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                        }

                        ParticleHandler.SpawnParticle(new CircularPulseShine(new Vector2(Player.tileTargetX * 16 + 8, targetY * 16 - 8), Color.Cyan with { A = 0 }, Main.rand.NextFloat(0.7f, 1f)));
                        SoundEngine.PlaySound(HauntSound, player.Center);
                        return true;
                    }
                }
            }

            return false;
        }

        public static void MouseOverIcon(int i, int j)
        {
            Player player = Main.LocalPlayer;
            if (!player.IsInTileInteractionRange(Player.tileTargetX, Player.tileTargetY, TileReachCheckSettings.Simple))
                return;

            if (player.HeldItem.type != ItemType)
                return;

            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemType;
        }
    }
}