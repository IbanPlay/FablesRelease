using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Shaders;
using CalamityFables.Content.Boss.DesertWormBoss;
using Terraria.ObjectData;


namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    public class DeadStormlionLarvaCage : ModTile
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + "DeadStormlionLarvaTerrarium";

        public static bool onScreen = false;
        public static int[] cageFrames = new int[Main.cageFrames];

        public override void SetStaticDefaults()
        {
            TileID.Sets.CritterCageLidStyle[Type] = 0; // This is how vanilla draws the roof of the cage
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileSolidTop[Type] = true;
            Main.tileTable[Type] = true;
            AnimationFrameHeight = 54;

            DustType = DustID.Glass;

            TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.ScorpionCage, 0));
            TileObjectData.addTile(Type);

            // Since this tile is only used for a single item, we can reuse the item localization for the map entry.
            AddMapEntry(new Color(122, 217, 232));

            FablesGeneralSystemHooks.ResetNearbyTileEffectsEvent += ResetVisible;
        }

        private void ResetVisible() => onScreen = false;

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = 2; // From vanilla
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (!onScreen)
            {
                for (int i = 0; i < Main.cageFrames; i++)
                {
                    cageFrames[i] = Main.rand.Next(6);

                    //Walbert
                    if (Main.rand.NextBool(60))
                        cageFrames[i] = 6;
                }
            }
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Tile tile = Main.tile[i, j];
            int tileCageFrameIndex = TileDrawing.GetBigAnimalCageFrame(i, j, tile.TileFrameX, tile.TileFrameY);
            frameYOffset = cageFrames[tileCageFrameIndex] * AnimationFrameHeight;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            onScreen = true;

            if ((!Main.gamePaused && Main.instance.IsActive) && Main.rand.NextBool(1800))
            {
                ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.PooFly, new ParticleOrchestraSettings
                {
                    PositionInWorld = new Vector2(i * 16 + 8, j * 16 - 8)
                });
            }
        }
    }

    public class DeadStormlionLarvaCageItem : ModItem
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + "DeadStormlionLarvaTerrariumItem";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dead Stormlion Larva Cage");
            Tooltip.SetDefault("'Something's off...'");
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.FrogCage);
            Item.createTile = ModContent.TileType<DeadStormlionLarvaCage>();
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Terrarium)
                .AddIngredient(ModContent.ItemType<DeadStormlionLarvaItem>())
                .SortAfterFirstRecipesOf(ItemID.CageGrubby) // places the recipe right after vanilla frog cage recipe.
                .Register();
        }
    }
}