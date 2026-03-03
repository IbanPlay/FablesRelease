using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Diagnostics.Metrics;
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
    
    public class SpiderTree : ModTile, ICustomLayerTile
    {
        public static readonly SoundStyle HitSFX = new SoundStyle(SoundDirectory.Tiles + "SpiderTreeHit", 3) with { Volume = 0.4f, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };

        public override string Texture => AssetDirectory.VanityTrees + Name;

        public static int SaplingType;
        public static int TreeType;
        public static int SpiderBlossomType;
        public static int LeafType;

        const int TRUNK_UV_CIRCUMFERENCE = 132;
        const int TRUNK_UV_HEIGHT = 216;
        public const int MIN_TREE_HEIGHT = 13;
        public const int MAX_TREE_HEIGHT = 15;

        private static int[] TRUNK_WIDTHS = [36, 36, 40, 40, 36, 36, 32, 32, 28, 28, 24, 24, 20, 20];

        public override void SetStaticDefaults()
        {
            FablesTile.OverrideKillTileEvent += PreventTileBreakIfOnTopOfIt;

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileAxe[Type] = true;

            TileID.Sets.IsATreeTrunk[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;

            //TileID.Sets.IsShakeable[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
            DustType = DustID.SpookyWood;
            HitSound = HitSFX;

            TreeType = Type;
            AddMapEntry(new Color(47, 10, 11), Language.GetText("MapObject.Tree"));
        }

        private bool? PreventTileBreakIfOnTopOfIt(int i, int j, int type)
        {
            //Can always break the tree itself
            if (type == Type || j <= 0 || i <= 0 || i >= Main.maxTilesX - 1)
                return true;

            //Tree above / Diagonally above
            if ((Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].TileType == Type && Main.tile[i, j - 1].TileFrameY == (short)(TRUNK_UV_HEIGHT - 16)) ||
                (i > 0 && Main.tile[i - 1, j - 1].HasTile && Main.tile[i - 1, j - 1].TileType == Type && Main.tile[i - 1, j - 1].TileFrameY == (short)(TRUNK_UV_HEIGHT - 16)) ||
                (i < Main.maxTilesX - 1 && Main.tile[i + 1, j - 1].HasTile && Main.tile[i + 1, j - 1].TileType == Type && Main.tile[i + 1, j - 1].TileFrameY == (short)(TRUNK_UV_HEIGHT - 16))
                )
                return false;

            return null;
        }

        public bool IsTreeTop(int i, int j) => Main.tile[i, j].TileFrameX == 752;
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
                lootPool.Add(ItemID.Shadewood, woodChance);  //Wood 1 - 3
                lootPool.Add(ItemID.BloodOrange, fruitChance * 0.5f);
                lootPool.Add(ItemID.Rambutan, fruitChance * 0.5f);
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
                    if (drop == ItemID.RottenEgg)
                        dropCount = WorldGen.genRand.Next(1, 3);
                    else if (drop == ItemID.Shadewood)
                        dropCount = WorldGen.genRand.Next(1, 4);

                    Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), i * 16, j * 16, 16, 16, drop, dropCount);
                }

                new SpiderGrowFXPacket(i, j, 1, 2).Send();
            }
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            int dropCount = 1;
            int closestPlayer = Player.FindClosest(new Vector2(i * 16, j * 16), 16, 16);
            int axe = Main.player[closestPlayer].inventory[Main.player[closestPlayer].selectedItem].axe;
            if (WorldGen.genRand.Next(100) < axe || Main.rand.NextBool(3))
                dropCount = 2;

            yield return new Item(ItemID.Shadewood, dropCount);
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

            if (i == 1092 && bottomTileType == TileID.CrimsonGrass)
            {

            }

            //If there's neither crimson grass nor another spider tree under, break
            if (!ValidTileToGrowOn(bottomTileType) && bottomTileType != Type)
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

        public static bool ValidTileToGrowOn(int tileType) => tileType == TileID.CrimsonGrass || tileType == TileID.CrimsonJungleGrass || tileType == TileID.Crimsand;
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
            //SPECIAL NOTE: This one needs valid ground on BOTH sides
            if ((!Main.tile[i - 1, j].HasUnactuatedTile || !ValidTileToGrowOn(Main.tile[i - 1, j].TileType)) || 
                (!Main.tile[i + 1, j].HasUnactuatedTile || !ValidTileToGrowOn(Main.tile[i + 1, j].TileType)))
                return false;


            //Check for sufficient empty space
            if (!FablesWorld.TreeTileClearanceCheck(i - 1, i + 1, j - 1, j - 1, ignoreTrees))
                return false;
            if (!FablesWorld.TreeTileClearanceCheck(i - widthClearance, i + widthClearance, j - heightClearance - 1, j - 2, ignoreTrees))
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

            int counter = 0;

            int torse = WorldGen.genRand.Next(TRUNK_UV_CIRCUMFERENCE);
            int tint = WorldGen.genRand.Next(3);
            short baselineFrameX = (short)(torse + (tint * TRUNK_UV_CIRCUMFERENCE * 2));

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
                tile.TileFrameX = baselineFrameX;
                tile.TileFrameY = (short)(TRUNK_UV_HEIGHT - 16 * counter);

                //Treetop
                if (j == floorY - height)
                {
                    tile.TileFrameX = 752;
                    tile.TileFrameY = (short)(338 + frameNumber);
                    continue;
                }
            }

            if (!WorldGen.generatingWorld)
             WorldGen.RangeFrame(i, floorY - height, i, floorY);

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, i - 1, floorY - height - 2, 3, height + 2);

            if (!WorldGen.generatingWorld && WorldGen.PlayerLOS(i, floorY - height))
                new SpiderGrowFXPacket(i, floorY - 1, height, 1).Send();

            return true;
        }

        public static void GrowthEffects(int i, int j, bool tileBreak)
        {
            int dustType = DustID.SpookyWood;
            if (Main.rand.NextBool(5))
                dustType = DustID.CrimsonPlants;

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


            Vector2 tileCenter = new Vector2(i, j) * 16f + new Vector2(8f, 8f);
            tileCenter -= new Vector2(5f, 7f); //Center it for the gore spawned

            var source = WorldGen.GetItemSource_FromTreeShake(i, j);

            //Treetop
            if (Main.tile[i, j].TileFrameX == 752)
            {
                int leafCount = 8;
                if (tileBreak)
                    leafCount = 6;

                for (int g = 0; g < leafCount; g++)
                {
                    Gore.NewGore(source, tileCenter - Vector2.UnitY * 30 + Main.rand.NextVector2Circular(60f, 40f), Main.rand.NextVector2Circular(10, 10), LeafType, 1f);;
                }
            }
            else if (!tileBreak)
            {
                Gore.NewGore(source, tileCenter + Main.rand.NextVector2Circular(10, 10), Main.rand.NextVector2Circular(10, 10), LeafType, 0.7f + Main.rand.NextFloat(0.6f));
            }
        }

        //Rng chance to spawn spider lily
        public override void RandomUpdate(int i, int j)
        {
            if (i < 6 || i > Main.maxTilesX - 6)
                return;

            //Crawl to the trees bottom
            while (j < Main.maxTilesY - 2 && Main.tile[i, j].TileType == Type)
                j++;

            j -= 4;

            int spiderLilyCount = 0;
            bool closeSpiderLily = false;

            for (int x = i - 6; x <= i + 6; x++)
            {
                for (int y = j; y <= j + 8; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t.HasTile && t.TileType == SpiderBlossomType)
                    {
                        spiderLilyCount++;
                        if (x == i - 1 || x == i + 1)
                            closeSpiderLily = true;
                    }
                }
            }

            //No more than 3 nearby spider lilies
            if (spiderLilyCount >= 3)
                return;

            int closestDistance = 2;
            if (!closeSpiderLily && WorldGen.genRand.NextBool(3))
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
                    if (above.TileType == SpiderBlossomType)
                        break;

                    if (WorldGen.PlaceTile(randomX, y - 1, SpiderBlossomType, false, false))
                    {
                        //Fsr the tile doesnt get a random variant???
                        Tile plant = Main.tile[randomX, y - 1];
                        if (plant.TileType == SpiderBlossomType)
                        {
                            plant.UseBlockColors(t.BlockColorAndCoating());
                            plant.TileFrameX = (short)(Main.rand.Next(6) * 24);
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

        #region Drawing
        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];
            if (TileDrawing.IsVisible(t) && t.TileFrameX == 752)  //Treetop
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

            int variant = t.TileFrameY - 338;

            //Tree trunk
            Color drawColor = Lighting.GetColor(i, j);
            if (t.IsTileFullbright)
                drawColor = Color.White;

            Vector2 baseDrawPos = new Vector2(i * 16 + 8, j * 16 + 16) - Main.Camera.UnscaledPosition;


            DrawClaws(texture, i, j, baseDrawPos, drawColor, variant, 0.4f, true, true);
            DrawClaws(texture, i, j, baseDrawPos, drawColor, variant, 2f, false, true);

            DrawClaws(texture, i, j, baseDrawPos, drawColor, variant, 0, true, false);
            DrawClaws(texture, i, j, baseDrawPos, drawColor, variant, 1.6f, false, false);

            Rectangle treetopFrame = new Rectangle(0, 258 + 108 * variant, 168, 106);
            Vector2 treetopOrigin = new Vector2(82, 104);
            Vector2 spiderDrawPos = baseDrawPos;
            float sway = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.treeWindCounter);
            spiderDrawPos.Y += Math.Abs(sway) * 2;
            Main.spriteBatch.Draw(texture, spiderDrawPos, treetopFrame, drawColor, sway * 0.04f, treetopOrigin, 1f, SpriteEffects.None, 0f);
        }

        public void DrawClaws(Texture2D tex, int i, int j, Vector2 drawPos, Color drawColor, int variant, float windOffset, bool left, bool back)
        {
            float swayClaws = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.sunflowerWindCounter + windOffset);
            drawPos.Y += swayClaws * 2;
            drawPos.X -= swayClaws * 1;
            Rectangle clawFrame = new Rectangle(266, 220 + 114 * variant, 100, 112);
            Vector2 clawOrigin = new Vector2(100, 100);

            if (!left)
            {
                clawFrame.X += 100;
                clawOrigin.X = 0;
            }
            if (back)
                clawFrame.X += 202;

            Main.spriteBatch.Draw(tex, drawPos, clawFrame, drawColor, swayClaws * 0.06f, clawOrigin, 1f, SpriteEffects.None, 0f);
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            if (tileFrameY > TRUNK_UV_HEIGHT)
                return;

            int heightAlongTrunk = (TRUNK_UV_HEIGHT - (tileFrameY + 16)) / 16;
            width = TRUNK_WIDTHS[Math.Min(heightAlongTrunk, TRUNK_WIDTHS.Length - 1)];

            tileFrameX += (short)((40 - width) / 2);

            if (heightAlongTrunk == 0)
                height += 2;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            if (!TileDrawing.IsVisible(t))
                return;

            if (t.TileFrameY > TRUNK_UV_HEIGHT)
                return;

            int heightAlongTrunk = (TRUNK_UV_HEIGHT - (t.TileFrameY + 16)) / 16;
            int width = TRUNK_WIDTHS[Math.Min(heightAlongTrunk, TRUNK_WIDTHS.Length - 1)];
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

            float outlineOpacity = 0.7f;
            //Draw outlines
            Vector2 drawPos = new Vector2(i * 16 + 8 - width / 2 +1 , j * 16) - FablesUtils.TileDrawOffset();
            Rectangle outlineFrame = new Rectangle(194, 222, 2, 16);
            Main.spriteBatch.Draw(texture, drawPos, outlineFrame, drawColor * outlineOpacity, 0, new Vector2(1, 0), 1f, SpriteEffects.None, 0f);
            drawPos.X += width - 2;
            Main.spriteBatch.Draw(texture, drawPos, outlineFrame, drawColor * outlineOpacity, 0, new Vector2(1, 0), 1f, SpriteEffects.None, 0f);

            //Draw shading only if not negative otherwise the alpha fucks it up
            if (t.TileColor != PaintID.NegativePaint)
            {
                drawPos = new Vector2(i * 16 + 8 - width / 2 + 2, j * 16) - FablesUtils.TileDrawOffset();
                Rectangle shadingFrameL = new Rectangle(236, 338, 10, 16);
                Rectangle shadingFrameR = new Rectangle(248, 338, 8, 16);

                //random frames
                shadingFrameL.Y += 18 * (int)(i * 51 * j + t.TileFrameX * t.TileFrameY * 3).ModulusPositive(3);
                shadingFrameR.Y += 18 * (int)(i * 2 * j * j - t.TileFrameX * t.TileFrameY * 2).ModulusPositive(3);

                Main.spriteBatch.Draw(texture, drawPos, shadingFrameL, drawColor, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
                drawPos.X += width - 4;
                Main.spriteBatch.Draw(texture, drawPos, shadingFrameR, drawColor, 0, new Vector2(8, 0), 1f, SpriteEffects.None, 0f);
            }

            //Add broken chunk at the top
            t = Main.tile[i, j - 1];
            if (!t.HasTile || t.TileType != Type)
            {
                Rectangle brokenTopFrame = new Rectangle(0, 220 + 12 * random, width, 10);
                switch (width)
                {
                    case 24:
                        brokenTopFrame.X = 22;
                        break;
                    case 28:
                        brokenTopFrame.X = 48;
                        break;
                    case 32:
                        brokenTopFrame.X = 78;
                        break;
                    case 36:
                        brokenTopFrame.X = 112;
                        break;
                    case 40:
                        brokenTopFrame.X = 150;
                        break;
                }

                drawPos = new Vector2(i * 16 + 8, j * 16) - FablesUtils.TileDrawOffset();
                Main.spriteBatch.Draw(texture, drawPos, brokenTopFrame, drawColor , 0, new Vector2(width / 2, 4), 1f, SpriteEffects.None, 0f);
            }

            //Draw trunk bottom
            if (heightAlongTrunk == 0)
            {
                drawPos = new Vector2(i * 16 + 8, j * 16 + 18) - FablesUtils.TileDrawOffset();
                Rectangle drawFrame = new Rectangle(206, 218, 58, 38);
                drawFrame.Y += 40 * random;
                Main.spriteBatch.Draw(texture, drawPos, drawFrame, drawColor, 0, new Vector2(58 / 2, 38), 1f, SpriteEffects.None, 0f);
            }
        }
        #endregion
    }

    #region Sapling
    public class SpiderSapling : ModTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            SpiderTree.SaplingType = Type;
            FablesGeneralSystemHooks.FertilizeSaplingEvent.Add(Type, SpiderTree.GrowTree);

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.AnchorValidTiles = new[] { (int)TileID.CrimsonGrass, (int)TileID.CrimsonJungleGrass, (int)TileID.Crimsand };
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

            AddMapEntry(new Color(173, 10, 69), Language.GetText("MapObject.Sapling"));

            DustType = DustID.Blood;
            AdjTiles = new int[] { TileID.Saplings };
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void RandomUpdate(int i, int j)
        {
            if (WorldGen.genRand.NextBool(5) && Main.netMode != NetmodeID.MultiplayerClient)
                SpiderTree.GrowTree(i, j, j > (int)Main.worldSurface - 1);
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
        {
            if (i % 2 == 0)
                effects = SpriteEffects.FlipHorizontally;
        }

        public override bool CanDrop(int i, int j) => false;
    }

    public class SpiderSaplingItem : ModItem
    {
        public override string Texture => AssetDirectory.VanityTrees + "PottedSpiderTree";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spider Palm Sapling");
            Item.ResearchUnlockCount = 25;
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<SpiderSapling>());
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }
    }
    #endregion

    [Serializable]
    public class SpiderGrowFXPacket : TreeGrowFXPacket
    {
        public SpiderGrowFXPacket(int i, int baseY, int height, byte effectCount) : base(i, baseY, height, effectCount) { }
        public override GrowEffectDelegate GrowEffect => SpiderTree.GrowthEffects;
    }

    #region Leaf gores
    public class SpiderTreeLeaf : ModGore
    {
        public override string Texture =>AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            SpiderTree.LeafType = Type;

            ChildSafety.SafeGore[Type] = true; // Leaf gore should appear regardless of the "Blood and Gore" setting
            GoreID.Sets.SpecialAI[Type] = 3; // Falling leaf behavior
           // GoreID.Sets.PaintedFallingLeaf[Type] = true; // This is used for all vanilla tree leaves, related to the bigger spritesheet for tile paints
        }
    }

    #endregion
}