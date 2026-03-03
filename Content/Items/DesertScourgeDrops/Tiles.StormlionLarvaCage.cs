using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Shaders;
using CalamityFables.Content.Boss.DesertWormBoss;
using Terraria.ObjectData;


namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    public class StormlionLarvaCage : ModTile
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + "StormlionLarvaTerrarium";

        public static int[] cageFrames = new int[Main.cageFrames];
        public static int[] cageFrameCounters = new int[Main.cageFrames];

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
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = 2; // From vanilla
            Main.critterCage = true; // Vanilla doesn't run the animation code for critters unless this is checked
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (!Main.critterCage)
                return;

            for (int i = 0; i < Main.cageFrames; i++)
            {
                //Standing still right or left
                if (cageFrames[i] == 0 || cageFrames[i] == 10)
                {
                    cageFrameCounters[i]++;
                    //Lasts 1.5 to 15 seconds waiting
                    if (cageFrameCounters[i] <= Main.rand.Next(90, 900))
                        continue;

                    int choice = Main.rand.Next(10);

                    //From right
                    if (cageFrames[i] == 0)
                    {
                        //Crawl
                        if (choice < 4)
                            cageFrames[i] = 20;
                        //Burrow
                        else if (choice < 8)
                            cageFrames[i] = 100;
                        //Electrify
                        else
                            cageFrames[i] = 1;
                    }
                    //From left
                    else
                    {
                        //Crawl
                        if (choice < 4)
                            cageFrames[i] = 60;
                        //Burrow
                        else if (choice < 8)
                            cageFrames[i] = 115;
                        //Electrify
                        else
                            cageFrames[i] = 11;
                    }

                    cageFrameCounters[i] = 0;
                }
                //Electrified
                else if ((cageFrames[i] >= 1 && cageFrames[i] < 10) || (cageFrames[i] >= 11 && cageFrames[i] < 20))
                {
                    cageFrameCounters[i]++;
                    if (cageFrameCounters[i] >= 4)
                    {
                        cageFrameCounters[i] = 0;
                        cageFrames[i] ++;

                        //Reset to the right and to the left
                        if (cageFrames[i] == 10)
                            cageFrames[i] = 0;
                        else if (cageFrames[i] == 20)
                            cageFrames[i] = 10;
                    }
                }

                //Crawling
                else if (cageFrames[i] >= 20 && cageFrames[i] < 100)
                {
                    cageFrameCounters[i]++;
                    if (cageFrameCounters[i] >= 6)
                    {
                        cageFrameCounters[i] = 0;
                        cageFrames[i]++;

                        //Reset to the right and to the left
                        if (cageFrames[i] == 60)
                            cageFrames[i] = 10;
                        else if (cageFrames[i] == 100)
                            cageFrames[i] = 0;
                    }
                }

                //Burrowing
                else if (cageFrames[i] >= 100)
                {
                    cageFrameCounters[i]++;
                    if (cageFrameCounters[i] >= 6)
                    {
                        cageFrameCounters[i] = 0;
                        cageFrames[i]++;

                        //Reset to the right and to the left
                        if (cageFrames[i] == 115)
                            cageFrames[i] = 10;
                        else if (cageFrames[i] == 130)
                            cageFrames[i] = 0;
                    }
                }

            }
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Tile tile = Main.tile[i, j];
            int tileCageFrameIndex = TileDrawing.GetBigAnimalCageFrame(i, j, tile.TileFrameX, tile.TileFrameY);
            frameYOffset = (cageFrames[tileCageFrameIndex] % 13) * AnimationFrameHeight;
            frameXOffset = (cageFrames[tileCageFrameIndex] / 13) * 108;
        }

        //Sadly terrariums that are bigger than 1 row break
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            //We only care about the top part
            if (t.TileFrameY != 0 || !TileDrawing.IsVisible(t))
                return true;

            Color drawColor = Lighting.GetColor(i, j);
            if (t.IsTileFullbright)
                drawColor = Color.White;

            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }

            Vector2 drawPos = FablesUtils.TileDrawPosition(i, j);
            int offsetX = 0;
            int offsetY = 0;
            AnimateIndividualTile(Type, i, j, ref offsetX, ref offsetY);
            Rectangle regularTileFrame = new Rectangle(t.TileFrameX + offsetX, t.TileFrameY + offsetY, 16, 16);

            //Trim the top
            Vector2 position = drawPos;
            Rectangle frame = regularTileFrame;
            position.Y += 10f;
            frame.Y += 8;
            frame.Height -= 8;
            Main.spriteBatch.Draw(texture, position, frame, drawColor, 0f, Vector2.Zero, 1f, 0, 0f);
            //Draw the top part of the cage
            position = drawPos;
            frame = regularTileFrame;
            frame.Y = 0;
            frame.X %= 108;
            frame.Height = 10;
            Main.spriteBatch.Draw(TextureAssets.CageTop[0].Value, position, frame, drawColor, 0f, Vector2.Zero, 1f, 0, 0f);
            return false;
        }
    }

    public class StormlionLarvaCageItem : ModItem
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + "StormlionLarvaTerrariumItem";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion Larva Cage");
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.FrogCage);
            Item.createTile = ModContent.TileType<StormlionLarvaCage>();
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Terrarium)
                .AddIngredient(ModContent.ItemType<StormlionLarvaItem>())
                .SortAfterFirstRecipesOf(ItemID.CageGrubby) // places the recipe right after vanilla frog cage recipe.
                .Register();
        }
    }
}