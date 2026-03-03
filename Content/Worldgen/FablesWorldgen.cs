using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Tiles.VanityTrees;
using CalamityFables.Noise;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.GameContent.Generation;
using Terraria.Localization;
using Terraria.WorldBuilding;

namespace CalamityFables.Core
{
    public partial class FablesWorld : ModSystem
    {
        public static FastNoise genNoise = new FastNoise(WorldGen._genRandSeed);

        public override void Load()
        {
            On_HouseBuilder.PlaceBiomeSpecificPriorityTool += PlaceCabinHookstaff;
            On_WorldGen.Pyramid += PeculiarPotGeneration;
            FablesWall.KillWallEvent += PreventHookstaffWallDestruction;

            sealedChamberMessage = Mod.GetLocalization("Extras.WorldgenTasks.SealedChamber");
            desertGraveyardMessage = Mod.GetLocalization("Extras.WorldgenTasks.DesertGraveyard");
            scourgeFossilsMessage = Mod.GetLocalization("Extras.WorldgenTasks.ScourgeFossils");
            cleanupPass = Mod.GetLocalization("Extras.WorldgenTasks.CleanupPass");
            hookstaffMessage = Mod.GetLocalization("Extras.WorldgenTasks.Hookstaff");
            scrapyardMessage = Mod.GetLocalization("Extras.WorldgenTasks.WulfrumScrapyard");
            vanityTreesMessage = Mod.GetLocalization("Extras.WorldgenTasks.VanityTrees");
            desertWorldDebugMessage = Mod.GetLocalization("Extras.WorldgenTasks.DebugDesertWorld");
        }

        public override void PostSetupContent()
        {
            saltTilesMask = TileID.Sets.Factory.CreateIntSet(0);
            if (CalamityFables.SpiritEnabled)
            {
                if (CalamityFables.SpiritReforged.TryFind("SaltBlockReflective", out ModTile saltReflective))
                    saltTilesMask[saltReflective.Type] = 1;
                if (CalamityFables.SpiritReforged.TryFind("saltBlockDull", out ModTile saltBlockDull))
                    saltTilesMask[saltBlockDull.Type] = 1;
            }
        }

        public static LocalizedText sealedChamberMessage;
        public static LocalizedText desertGraveyardMessage;
        public static LocalizedText scourgeFossilsMessage;
        public static LocalizedText hookstaffMessage;
        public static LocalizedText cleanupPass;
        public static LocalizedText scrapyardMessage;
        public static LocalizedText vanityTreesMessage;
        public static LocalizedText desertWorldDebugMessage;

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            genNoise.Seed = WorldGen._genRandSeed;
            PlacedHookstaves = 0;
            PlacedPeculiarPot = false;
            MushroomBiomesWithHookstaves.Clear();
            HookstaffPositions.Clear();
            PyramidPositions.Clear();


            int treesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Planting Trees"));
            if (treesIndex >= 0)
                tasks.Insert(treesIndex + 1, new PassLegacy("Modded Vanity Trees", (progress, config) =>
                {
                    progress.Message = vanityTreesMessage.Value;
                    GenerateExtraVanityTrees();
                }));

            int trapsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Traps"));
            if (trapsIndex >= 0)
                tasks.Insert(trapsIndex + 1, new PassLegacy("Nautilus Chamber", (progress, config) =>
                {
                    progress.Message = sealedChamberMessage.Value;
                    SealedChamber.TryGenerate();
                }));


            int SunflowersIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Sunflowers"));
            if (SunflowersIndex >= 1)
            {
                tasks.Insert(SunflowersIndex - 1, new PassLegacy("Sandstone Graveyard", (progress, config) =>
                {
                    progress.Message = desertGraveyardMessage.Value;
                    SealedChamber.SandstoneGraveyard();
                }));

                tasks.Insert(SunflowersIndex + 2, new PassLegacy("Buried Hookstaves", (progress, config) =>
                {
                    progress.Message = hookstaffMessage.Value;
                    PlaceBuriedHookstaff();
                }));
            }

            int microbiomeIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
            if (microbiomeIndex != -1)
            {
                tasks.Insert(microbiomeIndex + 1, new PassLegacy("Scourge Skeletons", (progress, config) =>
                {
                    progress.Message = scourgeFossilsMessage.Value;
                    FillDesertWithScourgeSkeletons();
                }));

                tasks.Insert(microbiomeIndex + 2, new PassLegacy("Hookstaff Cleanup", (progress, config) =>
                {
                    progress.Message = cleanupPass.Value;
                    CleanupCabinHookstaves();
                    CleanupPyramidDeco();
                }));

                //No need since generates in nautie chamber
                /*
                tasks.Insert(microbiomeIndex + 2, new PassLegacy("Scourgekiller Painting", (progress, config) =>
                {
                    progress.Message = "Writing in Hieroglyphics";
                    GenerateScourgekillerPainting();
                }));
                */

                tasks.Insert(microbiomeIndex + 3, new PassLegacy("Wulfrum Scrapyard", (progress, config) =>
                {
                    progress.Message = scrapyardMessage.Value;
                    WulfrumScrapyard.TryGenerate();
                }));
            }

           

            if (CalamityFables.NautilusDemo)
            {
                tasks.Insert(tasks.Count, new PassLegacy("Deserting the world", (progress, config) =>
                {
                    progress.Message = desertWorldDebugMessage.Value;
                    for (int i = 0; i < Main.maxTilesX; i++)
                    {
                        for (int y = 0; y < Main.maxTilesY; y++)
                        {
                            if (!SealedChamber.NautilusChamberRect.Contains(i, y))
                            {
                                Tile myTile = Main.tile[i, y];
                                myTile.HasTile = true;
                                myTile.TileType = Terraria.ID.TileID.Sandstone;
                                myTile.Slope = Terraria.ID.SlopeType.Solid;
                                myTile.IsHalfBlock = false;
                            }
                        }
                    }

                    Main.spawnTileX = SealedChamber.InnerChamberRect.Center.X;
                    Main.spawnTileY = SealedChamber.InnerChamberRect.Bottom - 2;
                }));
            }
        }

        public delegate bool ModifyChestContentsDelegate(Chest chest, Tile chestTile, bool alreadyAddedItem);
        public static event ModifyChestContentsDelegate ModifyChestContentsEvent;

        public static event Action PostWorldgenEvent;

        public override void PostWorldGen()
        {
            PostWorldgenEvent?.Invoke();

            if (ModifyChestContentsEvent != null)
            {
                //Add fables misc loot
                for (int i = 0; i < Main.maxChests; i++)
                {
                    Chest chest = Main.chest[i];
                    if (chest == null)
                        continue;

                    Tile chestTile = Main.tile[chest.x, chest.y];
                    if (!chestTile.HasTile)
                        continue;

                    bool alreadyAddedItem = false;

                    foreach (ModifyChestContentsDelegate modify in ModifyChestContentsEvent.GetInvocationList())
                    {
                        if (modify(chest, chestTile, alreadyAddedItem))
                            alreadyAddedItem = true;
                    }

                }
            }
        }

    }
}