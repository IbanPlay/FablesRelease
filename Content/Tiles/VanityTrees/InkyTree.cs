using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Drawing.Imaging;
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
    
    public class InkyTree : ModTile, ICustomLayerTile
    {
        public static readonly SoundStyle HitSFX = new SoundStyle(SoundDirectory.Tiles + "PaleblotTreeHit", 3) with { Volume = 0.4f, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };

        public override string Texture => AssetDirectory.VanityTrees + Name;

        public static int SaplingType;
        public static int TreeType;
        public static int SporeDustType;

        const int TRUNK_UV_CIRCUMFERENCE = 48;
        const int TRUNK_UV_HEIGHT = 240;
        public const int MIN_TREE_HEIGHT = 11;
        public const int MAX_TREE_HEIGHT = 15;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = false;
            Main.tileLavaDeath[Type] = true;
            Main.tileAxe[Type] = true;

            TileID.Sets.IsATreeTrunk[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;

            //TileID.Sets.IsShakeable[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
            DustType = DustID.DynastyWall;
            HitSound = HitSFX;

            TreeType = Type;
            AddMapEntry(new Color(73, 45, 167), Language.GetText("MapObject.Tree"));
            FablesTile.OverrideKillTileEvent += PreventTileBreakIfOnTopOfIt;
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
                return Main.tile[i, j - 1].TileFrameX > TRUNK_UV_CIRCUMFERENCE * 12;
            }

            return null;
        }


        public bool IsTreeTop(int i, int j) => Main.tile[i, j].TileFrameX == 12 * TRUNK_UV_CIRCUMFERENCE;
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
                lootPool.Add(ItemID.VilePowder, woodChance);  //Vile powder 1 - 3
                lootPool.Add(ItemID.VileMushroom, fruitChance); //Vile mushroom 1 - 2
                lootPool.Add(ItemID.CopperCoin, coinChance);
                lootPool.Add(-1, enemyChance);
                if (Main.halloween)
                    lootPool.Add(ItemID.RottenEgg, 1 / 35f); //Rotten eggs : 1 - 2

                int drop = (int)lootPool;

                //Spawn enemy
                if (drop == -1)
                    NPC.NewNPC(new EntitySource_ShakeTree(i, j), i * 16 + 8, (j - 1) * 16, NPCID.LittleCrimera);
                else if (drop == ItemID.CopperCoin)
                    DropCoinsFromTreeShake(i, j);
                else
                {
                    int dropCount = 1;
                    if (drop == ItemID.RottenEgg || drop == ItemID.VileMushroom)
                        dropCount = WorldGen.genRand.Next(1, 3);
                    else if (drop == ItemID.VilePowder)
                        dropCount = WorldGen.genRand.Next(1, 4);

                    Item item = new Item(drop, dropCount);
                    if (drop == ItemID.VilePowder)
                        item.OverrideTexture("Tiles/VanityTrees/InkyDust");

                    Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), i * 16, j * 16, 16, 16, item);
                }

                new InkyGrowFXPacket(i, j, 1, 2).Send();
            }
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            yield return new Item(ItemID.VilePowder, Main.rand.Next(1, 3)).OverrideTexture("Tiles/VanityTrees/InkyDust");
        }


        #region Generation and framing

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tile tile = Main.tile[i, j];
            int originalFrameX = tile.TileFrameX;
            int originalFrameY = tile.TileFrameY;

            int bottomTileType = -1;
            if (Main.tile[i, j + 1].HasTile)
                bottomTileType = Main.tile[i, j + 1].TileType;

            //Leaf tile
            if (originalFrameX > TRUNK_UV_CIRCUMFERENCE * 12)
            {
                int leafDir = originalFrameY < 3 ? -1 : 1;
                int connectedTile = -1;
                if (Main.tile[i - leafDir, j].HasTile)
                    connectedTile = Main.tile[i - leafDir, j].TileType;

                if (connectedTile != Type)
                    WorldGen.KillTile(i, j);
            }

            //If we're not a branch and there's neither crimson grass nor another bone tree trunk under, break
            else if (!ValidTileToGrowOn(bottomTileType) && bottomTileType != Type)
                WorldGen.KillTile(i, j);

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

            //Extra check for treetop
            int treeTop = j - heightClearance + 2;
            if (!FablesWorld.TreeTileClearanceCheck(i - 3, i + 3, treeTop, treeTop + 5, ignoreTrees))
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
                height = Main.rand.Next(MIN_TREE_HEIGHT, MAX_TREE_HEIGHT + 1);
                if (!PreGrowChecks(i, floorY, height + 4))
                    return false;
            }

            //Copies the palette of the floor
            TileColorCache palette = Main.tile[i, floorY].BlockColorAndCoating();
            if (Main.tenthAnniversaryWorld && WorldGen.generatingWorld)
                palette.Color = FablesWorld.celebrationRainbowPaint;

            //3 alt tints of the tree added onto the torsion
            int torse = WorldGen.genRand.Next(TRUNK_UV_CIRCUMFERENCE);
            int tint = WorldGen.genRand.Next(3);
            short baselineFrameX = (short)(torse + (tint * TRUNK_UV_CIRCUMFERENCE * 4));

            int counter = 0;
            bool branchLeft = false;
            bool branchRight = false;

            //Go up the tree
            for (int j = floorY - 1; j >= floorY - height; j--)
            {
                counter++;
                int frameNumber = WorldGen.genRand.Next(3);

                Tile tile = Main.tile[i, j];
                tile.TileFrameNumber = frameNumber;
                tile.HasTile = true;
                tile.TileType = (ushort)TreeType;
                tile.UseBlockColors(palette);
                tile.TileFrameX = (short)baselineFrameX;
                tile.TileFrameY = (short)(TRUNK_UV_HEIGHT - 16 * counter);

                //Striped variant layer
                if (WorldGen.genRand.NextBool(6))
                    tile.TileFrameX += TRUNK_UV_CIRCUMFERENCE * 2;

                //Treetop
                if (j == floorY - height)
                {
                    tile.TileFrameX = 12 * TRUNK_UV_CIRCUMFERENCE;
                    tile.TileFrameY = (short)(0 + frameNumber);
                    continue;
                }

                //Branch
                if (j > floorY - height + 3 && j < floorY - 2 && WorldGen.genRand.NextBool(6))
                {
                    int branchDir;
                    if (branchLeft)
                        branchDir = 1;
                    else if (branchRight)
                        branchDir = -1;
                    else
                        branchDir = WorldGen.genRand.NextBool() ? 1 : -1;

                    branchLeft = branchDir == -1;
                    branchRight = branchDir == 1;
                    int branchRand = WorldGen.genRand.Next(3);

                    Tile tileBranch = Main.tile[i + branchDir, j];
                    tileBranch.TileFrameNumber = WorldGen.genRand.Next(3);
                    tileBranch.HasTile = true;
                    tileBranch.TileType = (ushort)TreeType;
                    tileBranch.UseBlockColors(palette);
                    tileBranch.TileFrameX = 12 * TRUNK_UV_CIRCUMFERENCE + 1;
                    tileBranch.TileFrameY = (short)((branchRand) + (branchDir < 0 ? 0 : 3));

                    //tileBranch.IsHalfBlock = true; //Prevents cracks from drawing on it
                }
            }

            WorldGen.RangeFrame(i - 2, floorY - height - 1, i + 2, floorY);
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, i - 1, floorY - height - 2, 3, height + 2);

            if (!WorldGen.generatingWorld && WorldGen.PlayerLOS(i, floorY - height))
                new InkyGrowFXPacket(i, floorY - 1, height, 1).Send();

            return true;
        }


        public static void GrowthEffects(int i, int j, bool tileBreak)
        {
            int dustType = DustID.SpookyWood;
            if (Main.rand.NextBool(5))
                dustType = DustID.CorruptionThorns;

            int dustCount = tileBreak? 1 : 6;

            for (int d = 0; d < dustCount; d++)
            {
                float widthExtra = 18f;
                Vector2 position = new Vector2(i * 16 + Main.rand.NextFloat(-widthExtra, (16f + widthExtra * 2)), j * 16 + Main.rand.NextFloat(16f));
                Dust du = Dust.NewDustPerfect(position, dustType, -Vector2.UnitY + Vector2.UnitX * Main.rand.NextFloat(-5f, 5f));
                du.velocity.Y *= Main.rand.NextFloat(1f, 3f);
            

                //Chance to flip velocity
                if (Main.rand.NextBool(3))
                    du.velocity.Y *= -1;

                if (!tileBreak)
                {
                    du.noGravity = true;
                    du.velocity.Y *= 0.2f;
                    du.scale *= 1.2f;
                }
            }
        }
        #endregion

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 2;
        public override bool CreateDust(int i, int j, ref int type)
        {
            GrowthEffects(i, j, true);
            return true;
        }

        #region Drawing
        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];
            if (TileDrawing.IsVisible(t) && t.TileFrameX >= TRUNK_UV_CIRCUMFERENCE * 12) //Treetop or leaves
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.PostDrawTiles, true);
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
            Color drawColor = Lighting.GetColor(i, j);
            if (t.IsTileFullbright)
                drawColor = Color.White;


            if (t.TileFrameX == TRUNK_UV_CIRCUMFERENCE * 12)
            {
                Rectangle treetopFrame = new Rectangle(0, 268 + 100 * t.TileFrameY, 186, 100);
                Vector2 treetopOrigin = new Vector2(94, 46);
                Vector2 drawPos = new Vector2(i * 16 + 8, j * 16 + 16) - Main.Camera.UnscaledPosition;

                int tileBelowFrame = Main.tile[i, j + 1].TileFrameX % 3;
                Rectangle corrolaFrame = new Rectangle(192, 354 + 48 * tileBelowFrame, 36, 38);
                Vector2 corrolaOrigin = new Vector2(18, -4);

                //Draw cororola
                Main.spriteBatch.Draw(texture, drawPos, corrolaFrame, drawColor, 0f, corrolaOrigin, 1f, SpriteEffects.None, 0f);

                //Draw the ink dribbles
                switch (t.TileFrameY)
                {
                    default:

                        DrawInkDrip(texture, drawColor, 0, drawPos, new Vector2(-69, 20), 8,    12,     2f, 0f);
                        DrawInkDrip(texture, drawColor, 0, drawPos, new Vector2(-42, 16), 2,    18,     1f, 0.4f);
                        DrawInkDrip(texture, drawColor, 0, drawPos, new Vector2(38, 20), 10,    14,    1.3f, 1.7f);
                        DrawInkDrip(texture, drawColor, 0, drawPos, new Vector2(69, 16), 2,     16,     1f, 2.5f);
                        break;
                    case 1:
                        DrawInkDrip(texture, drawColor, 1, drawPos, new Vector2(-75, 12), 4,    10,     1.2f, 1.6f);
                        DrawInkDrip(texture, drawColor, 1, drawPos, new Vector2(-36, 32), 12,   12,     1.1f, 0.4f);
                        DrawInkDrip(texture, drawColor, 1, drawPos, new Vector2(34, 12), 2,     16,     1.5f, 0f);
                        DrawInkDrip(texture, drawColor, 1, drawPos, new Vector2(73, 18), 10,    8,     1f, 4f);
                        break;
                    case 2:
                        DrawInkDrip(texture, drawColor, 2, drawPos, new Vector2(-58, 18), 2,    12,     1f, 0.4f);
                        DrawInkDrip(texture, drawColor, 2, drawPos, new Vector2(-24, 18), 18,  -6,     1.3f, 0f);
                        DrawInkDrip(texture, drawColor, 2, drawPos, new Vector2(53, 18), 12,    12,     1.2f, 1.3f);
                        break;
                }

                //Draw treetop
                Main.spriteBatch.Draw(texture, drawPos, treetopFrame, drawColor, 0f, treetopOrigin, 1f, SpriteEffects.None, 0f);

                //Spawn falling dust
                if (!Main.gamePaused && Main.instance.IsActive)
                {
                    int leafSpawnChance = WindHelper.leafFrequency / 2;
                    if (!WorldGen.DoesWindBlowAtThisHeight(j))
                        leafSpawnChance = 10000;
                    if (!Main.rand.NextBool(leafSpawnChance))
                        return;

                    for (int z = 0; z < 3; z++)
                    {
                        Vector2 dustPos = new Vector2(i * 16 + 8 + Main.rand.NextFloat(-88, 88), j * 16 + 29);
                        if (WorldGen.SolidTile(dustPos.ToTileCoordinates()))
                            return;

                        Dust d = Dust.NewDustPerfect(dustPos, SporeDustType, new Vector2(Main.WindForVisuals * 0.04f, 0.7f), 100, Color.White, Main.rand.NextFloat(0.8f, 1f));
                        d.noGravity = false;
                        d.fadeIn = Main.rand.NextFloat(-0.5f, 0f);
                        d.velocity.Y = Main.rand.NextFloat(0.5f, 0.8f);
                        d.velocity.X += Main.rand.NextFloat(-0.2f, 0.3f);
                    }
                }
            }

            else
            {
                int variant = t.TileFrameY % 3;
                int side = t.TileFrameY - 3 >= 0 ? 1 : -1;

                Rectangle leafFrame = new Rectangle(192, 256 + 32 * variant, 48, 32);
                Vector2 leafOrigin = new Vector2(48, 8);

                if (side == 1)
                {
                    leafFrame.X += 68;
                    leafOrigin.X = 0;
                }

                Vector2 drawPos = new Vector2(i * 16 + 8, j * 16 + 8) - Main.Camera.UnscaledPosition;
                drawPos.X -= side * 20;

                //Draw leaf
                Main.spriteBatch.Draw(texture, drawPos, leafFrame, drawColor, 0f, leafOrigin, 1f, SpriteEffects.None, 0f);
            }
        }

        public void DrawInkDrip(Texture2D tex, Color drawColor, int variant, Vector2 baseCapPosition, Vector2 inkOffset, int dripLength, float dripDistance, float dripSpeed, float dripCycleOffset)
        {
            Vector2 drawPosition = baseCapPosition + inkOffset + Vector2.UnitX * 2;
            Rectangle dripFrame = new Rectangle(434, 314 + 100 * variant, 8, dripLength);
            dripFrame.X += (int)inkOffset.X;
            dripFrame.Y += (int)inkOffset.Y;
            dripFrame.X -= 4;

            float inkDroop = dripLength + (0.5f + 0.5f * (float)Math.Sin(dripSpeed * Main.GlobalTimeWrappedHourly * 0.9f + dripCycleOffset)) * dripDistance;

            //Draw drip cord
            Main.spriteBatch.Draw(tex, drawPosition, dripFrame, drawColor, 0f, new Vector2(4, 0), new Vector2(1f, inkDroop / (float)dripLength), SpriteEffects.None, 0f);

            dripFrame.Y += dripLength;
            dripFrame.Height = 14;
            //Draw drip end
            Main.spriteBatch.Draw(tex, drawPosition + Vector2.UnitY * inkDroop, dripFrame, drawColor, 0f, new Vector2(4, 0), 1f, SpriteEffects.None, 0f);
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            int heightAlongTrunk = (TRUNK_UV_HEIGHT - (tileFrameY + 16)) / 16;
            //Bottom is taller
            if (heightAlongTrunk == 0)
                height += 2;
        }


        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            if (!TileDrawing.IsVisible(t))
                return;

            //Treetop or branches
            if (t.TileFrameX >= TRUNK_UV_CIRCUMFERENCE * 12)
                return;

            int heightAlongTrunk = (TRUNK_UV_HEIGHT - (t.TileFrameY + 16)) / 16;
            int width = 16;
            if (heightAlongTrunk < 7)
                width = 20;

            int random = (t.TileFrameX + i * i - j).ModulusPositive(3);

            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }
            Color drawColor = Lighting.GetColor(i, j);
            if (t.IsTileFullbright)
                drawColor = Color.White;

            //Add broken chunk at the top
            Tile tAbove = Main.tile[i, j - 1];
            if (!tAbove.HasTile || tAbove.TileType != Type)
            {
                Rectangle brokenTopFrame = new Rectangle(4 + 18 * random, 248 , 16, 10);
                Vector2 brokenDrawPos = new Vector2(i * 16 + 8, j * 16) - FablesUtils.TileDrawOffset();
                Main.spriteBatch.Draw(texture, brokenDrawPos, brokenTopFrame, drawColor, 0, new Vector2(8, 2), 1f, SpriteEffects.None, 0f);
            }

            int outlineRandL = (int)(i * 51 * j + t.TileFrameX * t.TileFrameY * 3).ModulusPositive(3);
            int outlineRandR = (int)(i * 2 * j * j - t.TileFrameX * t.TileFrameY * 2).ModulusPositive(3);

            //Draw outlines
            Vector2 drawPos = new Vector2(i * 16 + 8 - width / 2, j * 16) - FablesUtils.TileDrawOffset();
            Rectangle outlineFrame = new Rectangle(64 + 10 * outlineRandL, 248, 8, 16);
            Main.spriteBatch.Draw(texture, drawPos, outlineFrame, drawColor, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
            outlineFrame = new Rectangle(96 + 10 * outlineRandR, 248, 8, 16);
            drawPos.X += width - 2;
            Main.spriteBatch.Draw(texture, drawPos, outlineFrame, drawColor, 0, new Vector2(6, 0), 1f, SpriteEffects.None, 0f);

            //Prolong the outline for the last bit that goes into the floor, and add a bulb outline below
            if (heightAlongTrunk == 0)
            {
                drawPos.X -= width - 2;
                drawPos.Y += 16;
                outlineFrame = new Rectangle(64, 248, 8, 2);
                Main.spriteBatch.Draw(texture, drawPos, outlineFrame, drawColor, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
                outlineFrame = new Rectangle(96, 248, 8, 2);
                drawPos.X += width ;
                Main.spriteBatch.Draw(texture, drawPos, outlineFrame, drawColor * 0.5f, 0, new Vector2(8, 0), 1f, SpriteEffects.None, 0f);

                //Bulb at the bottom
                outlineFrame = new Rectangle(126, 248, 20, 10);
                drawPos.X -= width / 2;
                Main.spriteBatch.Draw(texture, drawPos, outlineFrame, drawColor, 0, new Vector2(10, 2), 1f, SpriteEffects.None, 0f);
            }

            if (heightAlongTrunk <= 1)
            {
                drawPos = new Vector2(i * 16 + 8, j * 16 + 2) - FablesUtils.TileDrawOffset();
                int variant = (i).ModulusPositive(3);
                Rectangle lichenFrame = new Rectangle(230, 374 + 38 * variant - 16 * heightAlongTrunk, 24, 18);
                Main.spriteBatch.Draw(texture, drawPos, lichenFrame, drawColor, 0, new Vector2(12, 0), 1f, SpriteEffects.None, 0f);
            }
        }
        #endregion
    }

    #region Sapling
    public class InkySapling : ModTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            InkyTree.SaplingType = Type;
            FablesGeneralSystemHooks.FertilizeSaplingEvent.Add(Type, InkyTree.GrowTree);

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.CoordinateHeights = new[] { 26 };
            TileObjectData.newTile.CoordinateWidth = 24;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = -6;
            TileObjectData.newTile.AnchorValidTiles = new[] { (int)TileID.CorruptGrass, (int)TileID.CorruptJungleGrass };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.DrawFlipHorizontal = true;
            TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.RandomStyleRange = 3;
            TileObjectData.newTile.StyleMultiplier = 3;
            TileObjectData.addTile(Type);

            TileID.Sets.CommonSapling[Type] = true;
            TileID.Sets.SwaysInWindBasic[Type] = true;
            TileID.Sets.IgnoredByGrowingSaplings[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            AddMapEntry(new Color(135, 135, 144), Language.GetText("MapObject.Sapling"));

            DustType = DustID.CorruptPlants;
            AdjTiles = new int[] { TileID.Saplings };
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 4;

        public override void RandomUpdate(int i, int j)
        {
            //4 out of 10 chance, double the 1/5 chance of the others because the mushrooms are 1 tile high
            if (WorldGen.genRand.NextBool(4, 10) && Main.netMode != NetmodeID.MultiplayerClient)
                InkyTree.GrowTree(i, j, j > (int)Main.worldSurface - 1);
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
        {
            if (i % 2 == 0)
                effects = SpriteEffects.FlipHorizontally;
        }

        public override bool CanDrop(int i, int j) => false;
    }

    public class InkySaplingItem : ModItem
    {
        public override string Texture => AssetDirectory.VanityTrees + "PottedInkyTree";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Paleblot Bane Mushroom");
            Item.ResearchUnlockCount = 25;
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<InkySapling>());
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }
    }
    #endregion

    [Serializable]
    public class InkyGrowFXPacket : TreeGrowFXPacket
    {
        public InkyGrowFXPacket(int i, int baseY, int height, byte effectCount) : base(i, baseY, height, effectCount) { }
        public override GrowEffectDelegate GrowEffect => InkyTree.GrowthEffects;
    }


    #region Dust

    public class InkySporeDust : ModDust
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            InkyTree.SporeDustType = Type;
        }

        public override void OnSpawn(Dust dust)
        {
            dust.alpha = 255;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;

            dust.velocity.X = MathHelper.Lerp(dust.velocity.X, Main.WindForVisuals * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + dust.dustIndex * 2f) * 0.2f + 0.8f) * 1.2f, 0.05f);

            dust.velocity.Y = MathHelper.Lerp(dust.velocity.Y, 2f * (dust.scale), 0.11f);

            if (dust.noGravity)
                dust.scale *= 0.98f;

            dust.rotation += 0.01f;
            dust.scale *= 0.995f;

            dust.alpha = (int)((1 - dust.fadeIn) * 200f) + 55;
            if (dust.fadeIn < 1)
            {
                dust.fadeIn += 0.02f;
                if (dust.fadeIn > 1)
                    dust.fadeIn = 1f;
            }

            if (Collision.SolidCollision(dust.position, 1, 1))
            {
                dust.scale *= 0.96f;
                dust.velocity *= 0.2f;
            }

            if (dust.scale <= 0.3)
                dust.active = false;

            return false;
        }
    }
    #endregion
}