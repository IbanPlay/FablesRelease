using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Drawing.Drawing2D;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Metadata;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Tiles.VanityTrees
{
    public class MallowTree : ModTile, ICustomLayerTile
    {
        public static readonly SoundStyle HitSFX = new SoundStyle(SoundDirectory.Tiles + "PaleblotTreeHit", 3) with { Volume = 0.4f, Pitch = 0.6f, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };

        public override string Texture => AssetDirectory.VanityTrees + Name;

        public static int SaplingType;
        public static int TreeType;
        public static int MallowBlossomType;
        public static int LeafType;
        public static int GreenLeafType;

        public const int MIN_TREE_HEIGHT = 8;
        public const int MAX_TREE_HEIGHT = 12;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = false;
            Main.tileLavaDeath[Type] = true;
            Main.tileAxe[Type] = true;

            TileID.Sets.IsATreeTrunk[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;
            FablesTile.OverrideKillTileEvent += PreventTileBreakIfOnTopOfIt;

            //TileID.Sets.IsShakeable[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
            DustType = DustID.MartianHit;
            HitSound = HitSFX;

            TreeType = Type;
            AddMapEntry(new Color(74, 13, 76), Language.GetText("MapObject.Tree"));
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            int dropCount = 1;
            int closestPlayer = Player.FindClosest(new Vector2(i * 16, j * 16), 16, 16);
            int axe = Main.player[closestPlayer].inventory[Main.player[closestPlayer].selectedItem].axe;
            if (WorldGen.genRand.Next(100) < axe || Main.rand.NextBool(3))
                dropCount = 2;

            yield return new Item(ItemID.Ebonwood, dropCount);
        }

        private bool? PreventTileBreakIfOnTopOfIt(int i, int j, int type)
        {
            //Can always break the tree itself
            if (type == Type || j <= 0)
                return true;

            //Tree above
            if (Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].TileType == Type)
            {
                //Can break it if its not the trunk above (aka a branch)
                return Main.tile[i, j - 1].TileFrameX >= 104;
            }

            return null;
        }

        public bool IsTreeTop(int i, int j) => Main.tile[i, j].TileFrameY == 110 && Main.tile[i, j].TileFrameX <= 2;
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !fail)
                return;

            //Shake tree
            if (ShakeTree(ref i, ref j, Type, IsTreeTop))
            {
                WeightedRandom<int> lootPool = new WeightedRandom<int>();

                float woodChance = 1 / 12f;
                float fruitChance = 1 / 12f;
                float coinChance = 1 / 20f;
                float enemyChance = 1 / 30f;

                lootPool.Add(ItemID.None, 1f - woodChance - fruitChance - coinChance - enemyChance);
                lootPool.Add(ItemID.Ebonwood, woodChance);  //Wood 1 - 3
                lootPool.Add(ItemID.BlackCurrant, fruitChance * 0.5f);
                lootPool.Add(ItemID.Elderberry, fruitChance * 0.5f);
                lootPool.Add(ItemID.CopperCoin, coinChance);
                lootPool.Add(-1, enemyChance);
                if (Main.halloween)
                    lootPool.Add(ItemID.RottenEgg, 1 / 35f); //Rotten eggs : 1 - 2

                int drop = (int)lootPool;

                //Spawn enemy
                if (drop == -1)
                    NPC.NewNPC(new EntitySource_ShakeTree(i, j), i * 16 + 8, (j - 1) * 16, NPCID.LittleEater);
                else if (drop == ItemID.CopperCoin)
                    DropCoinsFromTreeShake(i, j);
                else
                {
                    int dropCount = 1;
                    if (drop == ItemID.RottenEgg)
                        dropCount = WorldGen.genRand.Next(1, 3);
                    else if (drop == ItemID.Shadewood)
                        dropCount = WorldGen.genRand.Next(1, 4);

                    Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), i * 16, j * 16, 16, 16, drop, dropCount);
                }

                new MallowGrowFXPacket(i, j, 1, 2).Send();
            }
        }

        #region Generation and framing

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tile tile = Main.tile[i, j];
            int originalFrameX = tile.TileFrameX;
            int originalFrameY = tile.TileFrameY;

            int topTileType = -1;
            int bottomTileType = -1;
            if (Main.tile[i, j - 1].HasTile)
                topTileType = Main.tile[i, j - 1].TileType;
            if (Main.tile[i, j + 1].HasTile)
                bottomTileType = Main.tile[i, j + 1].TileType;

            //Branch without a tile to connect to
            if (tile.TileFrameX >= 104)
            {
                int connectedTileType = -1;
                Tile adjacentTile = Main.tile[i - (originalFrameY - 111), j];
                if (adjacentTile.HasTile)
                    connectedTileType = adjacentTile.TileType;

                if (connectedTileType != Type)
                    WorldGen.KillTile(i, j);

                //Breaks if connected to a broken top
                if (adjacentTile.TileFrameY >= 290 && adjacentTile.TileFrameX == 0)
                    WorldGen.KillTile(i, j);
            }
            else
            {
                //If we're not a branch and there's neither corrupt grass nor another mallow tree trunk under, break
                if (!ValidTileToGrowOn(bottomTileType) && bottomTileType != Type)
                    WorldGen.KillTile(i, j);

                //Not a treetop or a broken trunk but there's a missing tile above, turn into a broken trunk
                else if (topTileType != Type && tile.TileFrameY != 110 && tile.TileFrameY < 290 && (tile.TileFrameX < 26 || tile.TileFrameX > 78 || tile.TileFrameY < 184))
                {
                    //Not part of the special 3 tile tall custom ground connection bit
                    if (tile.TileFrameX == 0)
                        tile.TileFrameY = (short)(290 + WorldGen.genRand.Next(3) * 26);

                    else
                    {
                        int baseHeight = (tile.TileFrameY - 128) / 18;
                        tile.TileFrameY += (short)(56 + 8 * baseHeight); //56, 64, 72
                    }
                }
            }

            if (tile.TileFrameX != originalFrameX || tile.TileFrameY != originalFrameY)
            {
                WorldGen.TileFrame(i - 1, j);
                WorldGen.TileFrame(i + 1, j);
                WorldGen.TileFrame(i, j - 1);
                WorldGen.TileFrame(i, j + 1);
            }

            return true;
        }

        public static bool ValidTileToGrowOn(int tileType) => tileType == TileID.CorruptGrass || tileType == TileID.CorruptJungleGrass;
        public static bool PreGrowChecks(int i, int j, int heightClearance, int widthClearance = 2, bool ignoreTrees = false)
        {
            //Can't grow if submerged
            if (Main.tile[i - 1, j - 1].LiquidAmount != 0 || Main.tile[i, j - 1].LiquidAmount != 0 || Main.tile[i + 1, j - 1].LiquidAmount != 0)
                return false;

            //Can't grow if the tile below isnt a full tile
            Tile groundTile = Main.tile[i, j];
            if (!groundTile.HasUnactuatedTile || groundTile.IsHalfBlock || groundTile.Slope != SlopeType.Solid)
                return false;

            //Can't grow if the ground isnt crimson grass , or if the walls are filled
            if (!ValidTileToGrowOn(groundTile.TileType) || !WorldGen.DefaultTreeWallTest(Main.tile[i, j - 1].WallType))
                return false;

            //Can't grow if there's not a valid ground tile on the left or right of where its growing
            if ((!Main.tile[i - 1, j].HasUnactuatedTile || !ValidTileToGrowOn(Main.tile[i - 1, j].TileType)) && 
                (!Main.tile[i + 1, j].HasUnactuatedTile || !ValidTileToGrowOn(Main.tile[i + 1, j].TileType)))
                return false;

            //Check for sufficient empty space
            if (!FablesWorld.TreeTileClearanceCheck(i - widthClearance, i + widthClearance, j - heightClearance - 1, j - 1, ignoreTrees))
                return false;

            return true;
        }

        public static bool GrowTree(int i, int y, bool underground = false, bool needsSapling = true, int generateOverrideHeight = 0)
        {
            //Generate override height is used by worldgen, where it does the pre growth checks by itself and lets us skip doing that check ourselves
            int height = generateOverrideHeight;
            int floorY = y;

            if (height == 0)
            {
                if (underground)
                    return false;

                if (needsSapling && Main.tile[i, y].TileType != SaplingType)
                    return false;

                //Scroll down if there's a sapling so we can find the ground
                while (Main.tile[i, floorY].TileType == SaplingType && floorY < Main.maxTilesY - 1)
                    floorY++;

                //Get a random height
                height = WorldGen.genRand.Next(MIN_TREE_HEIGHT, MAX_TREE_HEIGHT + 1);
                if (!PreGrowChecks(i, floorY, height + 4))
                    return false;
            }

            //Copies the palette of the floor
            TileColorCache palette = Main.tile[i, floorY].BlockColorAndCoating();
            if (Main.tenthAnniversaryWorld && WorldGen.generatingWorld)
                palette.Color = FablesWorld.celebrationRainbowPaint;

            int stumpRand = WorldGen.genRand.Next(3);
            bool lastBranchDirection = WorldGen.genRand.NextBool();
            int tilesSinceBranch = 0;
            bool generatedLeaf = false;

            //Go up the tree
            for (int j = floorY - 1; j >= floorY - height; j--)
            {
                int frameNumber = WorldGen.genRand.Next(3);

                Tile tile = Main.tile[i, j];
                tile.TileFrameNumber = frameNumber;
                tile.HasTile = true;
                tile.TileType = (ushort)TreeType;
                tile.UseBlockColors(palette);

                tilesSinceBranch++;

                //First three tiles use unique frames covered in vines
                if (j >= floorY - 3)
                {
                    tile.TileFrameX = (short)(26 * (stumpRand + 1));
                    tile.TileFrameY = (short)(164 - (floorY - j - 1) * 18);
                    continue;
                }

                //Treetop
                if (j == floorY - height)
                {
                    tile.TileFrameY = 110;
                    tile.TileFrameX = (short)(frameNumber);
                    continue;
                }

                tile.TileFrameX = 0;

                int trunkVariant = WorldGen.genRand.Next(9);
                if (trunkVariant >= 3 && WorldGen.genRand.NextBool()) // One in 2 chance that the special trunk variants turn into normal variants
                    trunkVariant = WorldGen.genRand.Next(3);

                tile.TileFrameY = (short)(128 + 18 * trunkVariant);


                //Branch
                if (tilesSinceBranch >= 2 && j > floorY - height + 1 && j < floorY - 4 && WorldGen.genRand.NextBool(2))
                {
                    //Alternating direction on the branches
                    int branchDir;
                    if (lastBranchDirection)
                        branchDir = 1;
                    else
                        branchDir = -1;
                    lastBranchDirection = !lastBranchDirection;
                    tilesSinceBranch = 0;

                    //Higher branches have a chance to be flowers
                    if (generatedLeaf || (j <= floorY - height + 3 && WorldGen.genRand.NextBool()))
                    {
                        generatedLeaf = true;
                        tilesSinceBranch = 2;
                    }


                    int branchRand = WorldGen.genRand.Next(3);
                    Tile tileBranch = Main.tile[i + branchDir, j];
                    tileBranch.TileFrameX = (short)(104 + branchRand);
                    tileBranch.TileFrameY = (short)(111 + branchDir); //110 for left branch, 112 for right branch

                    tileBranch.HasTile = true;
                    tileBranch.TileType = (ushort)TreeType;
                    tileBranch.UseBlockColors(palette);

                    if (generatedLeaf)
                        tileBranch.TileFrameX = (short)(107 + WorldGen.genRand.Next(2)); //Not three because the third leaf looks kinda dumb

                    //tileBranch.IsHalfBlock = true; //Prevents cracks from drawing on it
                }
            }

            WorldGen.RangeFrame(i - 2, floorY - height - 1, i + 2, floorY);
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, i - 1, floorY - height - 2, 3, height + 2);

            if (!WorldGen.generatingWorld && WorldGen.PlayerLOS(i, floorY - height))
                new MallowGrowFXPacket(i, floorY - 1, height, 2).Send();
            return true;
        }


        //Rng chance to spawn mallow blossoms
        public override void RandomUpdate(int i, int j)
        {
            if (i < 6 || i > Main.maxTilesX - 6)
                return;
            //Crawl to the trees bottom
            while (j < Main.maxTilesY - 2 && Main.tile[i, j].TileType == Type)
                j++;
            j -= 4;

            int mallowBlossomCount = 0;
            bool closeBlossom = false;

            for (int x = i - 8; x <= i + 8; x++)
            {
                for (int y = j; y <= j + 8; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t.HasTile && t.TileType == MallowBlossomType)
                    {
                        mallowBlossomCount++;
                        if (x == i - 1 || x == i + 1)
                            closeBlossom = true;
                    }
                }
            }

            //No more than 3 nearby spider lilies
            if (mallowBlossomCount >= 4)
                return;

            int closestDistance = 2;
            if (!closeBlossom && WorldGen.genRand.NextBool(3))
                closestDistance = 1;

            int randomX = WorldGen.genRand.Next(closestDistance, 7);
            if (WorldGen.genRand.NextBool())
                randomX *= -1;
            randomX += i;


            for (int y = j; y <= j + 8; y++)
            {
                Tile t = Main.tile[randomX, y];
                if (t.HasTile && !t.IsHalfBlock && t.Slope == SlopeType.Solid && ValidTileToGrowOn(t.TileType))
                {
                    Tile above = Main.tile[randomX, y - 1];
                    if (above.TileType == MallowBlossomType)
                        break;

                    if (WorldGen.PlaceTile(randomX, y - 1, MallowBlossomType, false, false))
                    {
                        //Fsr the tile doesnt get a random variant???
                        Tile plant = Main.tile[randomX, y - 1];
                        if (plant.TileType == MallowBlossomType)
                        {
                            plant.UseBlockColors(t.BlockColorAndCoating());
                            plant.TileFrameX = (short)(Main.rand.Next(6) * 26);
                        }
                    }
                    NetMessage.SendTileSquare(-1, randomX - 1, y - 1, 3, 3);
                    return;
                }
            }
        }
        #endregion

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 2;

        public override bool CreateDust(int i, int j, ref int type)
        {
            GrowthEffects(i, j, true);
            return false;
        }

        public static void GrowthEffects(int i, int j, bool tileBreak)
        {
            float widthExtra = tileBreak ? 8f : 1f;
            Vector2 position = new Vector2(i * 16 + Main.rand.NextFloat(-widthExtra, (16f + widthExtra * 2)), j * 16 + Main.rand.NextFloat(16f));
            Dust d = Dust.NewDustPerfect(position, DustID.RedsWingsRun, -Vector2.UnitY);
            d.velocity.Y *= Main.rand.NextFloat(1f, 2f);

            //Chance to flip velocity
            if (Main.rand.NextBool(3))
                d.velocity.Y *= -1;

            if (!tileBreak)
            {
                d.noGravity = true;
                d.velocity.Y *= 0.7f;
                d.velocity.X += Main.rand.NextFloat(-6f, 6f);
                d.scale *= 1.2f;
            }
            else
            {
                d.velocity.X += Main.rand.NextFloat(-1f, 1f);
                d.noGravity = Main.rand.NextBool();
            }

            if (Main.rand.NextBool(3))
            {
                position = new Vector2(i * 16 + Main.rand.NextFloat(-4f, 24f), j * 16 + Main.rand.NextFloat(16f));
                d = Dust.NewDustPerfect(position, DustID.BubbleBurst_Purple, -Vector2.UnitY.RotatedByRandom(0.4f) * 2f);
            }


            Vector2 tileCenter = new Vector2(i, j) * 16f + new Vector2(8f, 8f);
            tileCenter -= new Vector2(5f, 7f); //Center it for the gore spawned
            var source = WorldGen.GetItemSource_FromTreeShake(i, j);


            //Treetop
            if (Main.tile[i, j].TileFrameY == 110 && Main.tile[i, j].TileFrameX <= 5)
            {
                int leafCount = 4;
                if (tileBreak)
                    leafCount = 3;

                for (int g = 0; g < leafCount; g++)
                {
                    int goreType = Main.rand.NextBool(5) ? GreenLeafType : LeafType;
                    Gore.NewGore(source, tileCenter - Vector2.UnitY * 30 + Main.rand.NextVector2Circular(60f, 40f), Main.rand.NextVector2Circular(10, 10), goreType, 1f); ;
                }
            }
            else if (!tileBreak && Main.rand.NextBool())
            {
                Gore.NewGore(source, tileCenter + Main.rand.NextVector2Circular(10, 10), Main.rand.NextVector2Circular(10, 10), LeafType, 0.7f + Main.rand.NextFloat(0.6f));
            }
        }

        #region Drawing
        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];
            if (!TileDrawing.IsVisible(t))
                return;

            //Add custom draw point for the treetops
            if (t.TileFrameX <= 2 && t.TileFrameY == 110) 
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.PostDrawTiles, true);

            //Draw point for the "branches" (little flowers and buds that sprout from the sides
            else if (t.TileFrameX >= 104)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.PostDrawTiles, true);

            //Draw point for veins creeping up the trunk that continues from the custom trunk
            else if (t.TileFrameX >= 26 && t.TileFrameX <= 78 && t.TileFrameY == 128)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.BehindTiles, true);
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }

            //An extension of the veins on the custom trunk base
            bool veinGrowth = t.TileFrameX >= 26 && t.TileFrameX <= 78 && t.TileFrameY == 128;
            bool treeTop =    t.TileFrameX <= 2 && t.TileFrameY == 110;
            bool branch =  t.TileFrameX >= 104;

            //Branch offset so we color pick the right spot
            if (branch)
                i -= (t.TileFrameY - 111);

            Color drawColor = Lighting.GetColor(i, j);
            if (t.IsTileFullbright)
                drawColor = Color.White;

            Vector2 camPos = Main.Camera.UnscaledPosition;
            Vector2 drawPos = new Vector2(i * 16 - (int)camPos.X + 8, j * 16 - (int)camPos.Y);

            //Branches drawing
            if (branch)
            {
                int side = t.TileFrameY - 111;
                drawPos.X += side * 16; //Correction because we shifted i over a tile to get accurate light color
                float sway = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.treeWindCounter);

                //Small buds
                if (t.TileFrameX < 107)
                {
                    int variant = t.TileFrameX - 104;
                    drawPos.Y += 16;

                    Rectangle budFrame = new Rectangle(104, 128 + 54 * variant, 36, 52);
                    if (side == 1)
                        budFrame.X += 76;
                    Vector2 origin = new Vector2(12, 22);
                    if (side == 1)
                        origin.X = 24;

                    //Draw the dark roots
                    Main.spriteBatch.Draw(texture, drawPos, budFrame, drawColor, 0, origin, 1f, SpriteEffects.None, 0f);

                    budFrame.X += 38;
                    origin.Y -= 4;
                    drawPos.Y -= 4;
                    origin.X -= 6 * side;
                    drawPos.X -= 6 * side;
                    //Draw the flowers swaying
                    Main.spriteBatch.Draw(texture, drawPos, budFrame, drawColor, sway * 0.09f, origin, 1f, SpriteEffects.None, 0f);
                }
                //Big leaf branch
                else
                {
                    int variant = t.TileFrameX - 107;
                    drawPos.Y += 6;
                    drawPos.X -= 8 * side;

                    Rectangle branchFrame = new Rectangle(316, 128 + 52 * variant, 16, 50);
                    if (side == 1)
                        branchFrame.X += 18;

                    Vector2 origin = new Vector2(0, 22);
                    if (side == 1)
                        origin.X = 16;
                    //Draw the connection point
                    Main.spriteBatch.Draw(texture, drawPos, branchFrame, drawColor, 0, origin, 1f, SpriteEffects.None, 0f);

                    //Draw the leaf swaying
                    branchFrame.X = 256;
                    branchFrame.Width = 60;
                    branchFrame.Height = 52;
                    if (side == 1)
                        branchFrame.X += 94;

                    origin.X = 60;
                    if (side == 1)
                        origin.X = 0;

                    Main.spriteBatch.Draw(texture, drawPos, branchFrame, drawColor, sway * 0.09f, origin, 1f, SpriteEffects.None, 0f);
                }
            }

            //Treetop drawing
            else if (treeTop)
            {
                Vector2 treeTopPos = drawPos + Vector2.UnitY * 16;
                float sway = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.treeWindCounter);
                treeTopPos.Y += Math.Abs(sway) * 2;
                int variant = t.TileFrameX;

                Rectangle treetopFrame = new Rectangle(variant * 144, 0, 144, 110);
                Vector2 treetopOrigin = new Vector2(72, 110);

                SpriteEffects effects = SpriteEffects.None;
                if (i % 2 == 1)
                    effects = SpriteEffects.FlipHorizontally;

                Main.spriteBatch.Draw(texture, treeTopPos, treetopFrame, drawColor, sway * 0.04f, treetopOrigin, 1f, effects, 0f);


                if (!Main.gamePaused && Main.instance.IsActive)
                {
                    int leafSpawnChance = WindHelper.leafFrequency;
                    if (!WorldGen.DoesWindBlowAtThisHeight(j))
                        leafSpawnChance = 10000;
                    if (!Main.rand.NextBool(leafSpawnChance))
                        return;

                    Vector2 leafPos = new Vector2(i * 16 + Main.rand.NextFloat(-22, 22), j * 16 - Main.rand.NextFloat(0f, 40f));
                    if (WorldGen.SolidTile(leafPos.ToTileCoordinates()))
                        return;

                    int goreType = Main.rand.NextBool(3) ? GreenLeafType : LeafType;
                    Gore.NewGore(WorldGen.GetItemSource_FromTreeShake(i, j), leafPos, Main.rand.NextVector2Circular(10, 10), goreType, 0.7f + Main.rand.NextFloat(0.6f));
                }
            }

            else if (veinGrowth)
            {
                Rectangle veinFrame = new Rectangle(t.TileFrameX, 264, 24, 32);

                //If the vein is gonna be drawn over a broken chunk of the trunk, use a slightly more cropped frame for the vein vines
                Tile tile2Above = Main.tile[i, j - 2];
                if (!tile2Above.HasTile || tile2Above.TileType != Type)
                    veinFrame.Y += 34;

                Main.spriteBatch.Draw(texture, drawPos, veinFrame, drawColor, 0f, new Vector2(12, 32), 1f, SpriteEffects.None, 0f);
            }
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            width = 24;

            //Connected to ground, get an extra 2 pixels of height so it can sink into the floor
            if (tileFrameX >= 26 && tileFrameX <= 78 && (tileFrameY == 164 || tileFrameY == 236))
                height += 2;

            //Broken top that portrudes up
            if ((tileFrameX == 0 && tileFrameY >= 290) || (tileFrameX >= 26 && tileFrameX <= 78 && tileFrameY >= 184))
            {
                height += 8;
                offsetY -= 8;
            }
        }
        #endregion
    }

    #region Sapling
    public class MallowSapling : ModTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            MallowTree.SaplingType = Type;
            FablesGeneralSystemHooks.FertilizeSaplingEvent.Add(Type, MallowTree.GrowTree);

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.CoordinateHeights = new[] { 40, 0 };
            TileObjectData.newTile.DrawYOffset = -6;
            TileObjectData.newTile.CoordinateWidth = 20;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.AnchorValidTiles = new[] { (int)TileID.CorruptGrass, (int)TileID.CorruptJungleGrass };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.DrawFlipHorizontal = true;
            TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.RandomStyleRange = 3;
            TileObjectData.newTile.StyleMultiplier = 3;
            TileObjectData.addTile(Type);

           // TileID.Sets.TreeSapling[Type] = true;
            TileID.Sets.CommonSapling[Type] = true;
            TileID.Sets.SwaysInWindBasic[Type] = true;
            TileID.Sets.IgnoredByGrowingSaplings[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            AddMapEntry(new Color(36, 0, 50), Language.GetText("MapObject.Sapling"));

            DustType = DustID.Ebonwood;
            AdjTiles = new int[] { TileID.Saplings };
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void RandomUpdate(int i, int j)
        {
            if (WorldGen.genRand.NextBool(5) && Main.netMode != NetmodeID.MultiplayerClient)
                MallowTree.GrowTree(i, j, j > (int)Main.worldSurface - 1);
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
        {
            if (i % 2 == 0)
                effects = SpriteEffects.FlipHorizontally;
        }

        public override bool CanDrop(int i, int j) => false;
    }

    public class MallowSaplingItem : ModItem
    {
        public override string Texture => AssetDirectory.VanityTrees + "PottedMallowTree";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crepuscular Mallow Sapling");
            Item.ResearchUnlockCount = 25;
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<MallowSapling>());
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }
    }
    #endregion

    #region Growth FX packet
    [Serializable]
    public class MallowGrowFXPacket : TreeGrowFXPacket
    {
        public MallowGrowFXPacket(int i, int baseY, int height, byte effectCount) : base(i, baseY, height, effectCount) { }
        public override GrowEffectDelegate GrowEffect => MallowTree.GrowthEffects;
    }
    #endregion

    #region Leaf gores
    public class MallowTreeLeaf : ModGore
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            MallowTree.LeafType = Type;

            ChildSafety.SafeGore[Type] = true; // Leaf gore should appear regardless of the "Blood and Gore" setting
            GoreID.Sets.SpecialAI[Type] = 3; // Falling leaf behavior
                                             // GoreID.Sets.PaintedFallingLeaf[Type] = true; // This is used for all vanilla tree leaves, related to the bigger spritesheet for tile paints
        }
    }

    public class MallowTreeLeafGreen : ModGore
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            MallowTree.GreenLeafType = Type;
            ChildSafety.SafeGore[Type] = true; // Leaf gore should appear regardless of the "Blood and Gore" setting
            GoreID.Sets.SpecialAI[Type] = 3; // Falling leaf behavior
                                             // GoreID.Sets.PaintedFallingLeaf[Type] = true; // This is used for all vanilla tree leaves, related to the bigger spritesheet for tile paints
        }
    }
    #endregion

}