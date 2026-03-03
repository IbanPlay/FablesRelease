using CalamityFables.Content.NPCs.Cursed;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Drawing.Imaging;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Metadata;
using Terraria.Graphics.Effects;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Tiles.VanityTrees
{
    
    public class WisteriaTree : ModTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public static int SaplingType;
        public static int TreeType;
        public static int LeafType;

        public const int MIN_TREE_HEIGHT = 8;
        public const int MAX_TREE_HEIGHT = 11;

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
            DustType = DustID.t_Meteor;
            //HitSound = HitSFX;

            TreeType = Type;
            AddMapEntry(new Color(86, 38, 55), Language.GetText("MapObject.Tree"));
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
                //Can break it if its not the thick trunk tiles (aka connecting to the floor)
                return !(Main.tile[i, j - 1].TileFrameY >= 132 && Main.tile[i, j - 1].TileFrameY < 200);
            }

            return null;
        }

        public static bool IsTreeTop(int i, int j) => Main.tile[i, j].TileFrameY >= 200;


        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !fail)
                return;

            //Shake tree
            if (ShakeTree(ref i, ref j, Type))
            {
                WeightedRandom<int> lootPool = new WeightedRandom<int>();

                float woodChance = 1 / 12f;
                float fruitChance = 1 / 12f;
                float coinChance = 1 / 20f;

                lootPool.Add(ItemID.None, 1f - woodChance - fruitChance - coinChance);
                lootPool.Add(ItemID.BorealWood, woodChance);  //Wood 1 - 3
                lootPool.Add(ItemID.Plum, fruitChance * 0.5f); 
                lootPool.Add(ItemID.Cherry, fruitChance * 0.5f);
                lootPool.Add(ItemID.CopperCoin, coinChance);
                if (Main.halloween)
                    lootPool.Add(ItemID.RottenEgg, 1 / 35f); //Rotten eggs : 1 - 2

                int drop = (int)lootPool;

                if (drop == ItemID.CopperCoin)
                    DropCoinsFromTreeShake(i, j);
                else
                {
                    int dropCount = 1;
                    if (drop == ItemID.RottenEgg || drop == ItemID.BorealWood)
                        dropCount = WorldGen.genRand.Next(1, 3);

                    Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), i * 16, j * 16, 16, 16, drop, dropCount);
                }

                new WisteriaGrowthFXPacket(i, j, 1, 2).Send();
            }
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            int dropCount = 1;
            int closestPlayer = Player.FindClosest(new Vector2(i * 16, j * 16), 16, 16);
            int axe = Main.player[closestPlayer].inventory[Main.player[closestPlayer].selectedItem].axe;
            if (WorldGen.genRand.Next(100) < axe || Main.rand.NextBool(3))
                dropCount = 2;

            yield return new Item(ItemID.BorealWood, dropCount);
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

            int topTileType = -1;
            if (Main.tile[i, j - 1].HasTile)
                topTileType = Main.tile[i, j - 1].TileType;


            bool treetop = tile.TileFrameY >= 200;
            bool thinTrunk = (tile.TileFrameX < 66 && tile.TileFrameY < 66) || treetop; //Count the treetop as part of the think trunk
            bool brokenThinTrunk = tile.TileFrameX == 0 && tile.TileFrameY < 132;

            bool sideRoot = tile.TileFrameX >= 176;
            bool thickTrunk = (tile.TileFrameY >= 132 && !treetop) || (tile.TileFrameY <= 22 && tile.TileFrameX >= 66 );
            bool thickTrunkBase = (tile.TileFrameY >= 132 && !treetop && tile.TileFrameX >= 88);

            if (thinTrunk || brokenThinTrunk || treetop)
            {
                //Turn into a broken trunk if the tile ontop breaks
                if (thinTrunk && topTileType != Type && tile.TileFrameY < 200)
                {
                    tile.TileFrameX = 0;
                    tile.TileFrameY = (short)(66 + WorldGen.genRand.Next(3) * 22);
                }

                //Thin trunk can only connect to another tree tile of the same type otherwise it should break (cant connect to ground, only 2 thick can
                if (bottomTileType != Type)
                    WorldGen.KillTile(i, j);
            }

            //Break side root if not ontop of valid floor or to the side of a valid root tile
            if (sideRoot)
            {
                if (!ValidTileToGrowOn(Main.tile[i, j + 1]))
                    WorldGen.KillTile(i, j);

                Tile adjacentTile;
                if (tile.TileFrameX == 176)
                    adjacentTile = Main.tile[i + 1, j];
                else
                    adjacentTile = Main.tile[i - 1, j];

                //Needs to connect to trunk
                if (!adjacentTile.HasTile || adjacentTile.TileType != Type)
                    WorldGen.KillTile(i, j);
            }

            else if (thickTrunk)
            {
                
                //Break if neither valid floor or another trunk tile is found below this tile
                if (!ValidTileToGrowOn(Main.tile[i, j + 1]) && bottomTileType != Type)
                    WorldGen.KillTile(i, j);
                else
                {
                    int adjacentCheckDir;
                    if (tile.TileFrameY >= 132)
                        adjacentCheckDir = (tile.TileFrameX / 22) % 2 == 0 ? 1 : -1;
                    else
                    {
                        adjacentCheckDir = tile.TileFrameX == 66 ? 1 : -1;
                        //Flipped for bottom row
                        if (tile.TileFrameY == 22)
                            adjacentCheckDir *= -1;
                    }

                    //Needs to be connected to the adjacent trunk tile
                    Tile adjacentTile = Main.tile[i + adjacentCheckDir, j];
                    if (!adjacentTile.HasTile || adjacentTile.TileType != Type)
                        WorldGen.KillTile(i, j);

                    else if (thickTrunkBase)
                    {
                        adjacentTile = Main.tile[i - adjacentCheckDir, j];
                        if (!adjacentTile.HasTile || adjacentTile.TileType != Type)
                        {
                            tile.TileFrameX -= 88; //remove root connection if the root broke
                        }
                    }

                    //Unbroken regular thick trunk tiles, will change to a different frame is the tree above is chopped off
                    if (tile.TileFrameY >= 132 && (tile.TileFrameX / 44) % 2 == 0 && topTileType != Type)
                        tile.TileFrameX += 44;

                    //The same as before, but for the section of the thick trunk that shrinks to transition into the 1 thick section
                    else if (tile.TileFrameY <= 22 && tile.TileFrameX == 88 && topTileType != Type)
                        tile.TileFrameX += 22;
                }
            }

            else if (tile.TileFrameY >= 66 && tile.TileFrameY < 132 && tile.TileFrameX >= 22)
            {
                int adjacentCheckDir = (tile.TileFrameX / 22) % 2 == 0 ? -1 : 1;
                Tile adjacentTile = Main.tile[i + adjacentCheckDir, j];

                //Needs to connect to adjacent tile or else break
                if (!adjacentTile.HasTile || adjacentTile.TileType != Type)
                    WorldGen.KillTile(i, j);

                if (tile.TileFrameX >= 44 && tile.TileFrameX <= 66)
                {
                    //Needs to connect to tile from the bottom or else break
                    if (bottomTileType != Type)
                        WorldGen.KillTile(i, j);
                }
                else if (tile.TileFrameX < 110 )
                {
                    //Needs to connect to tile from the top or else change to broken frame
                    if (topTileType != Type)
                        tile.TileFrameX += 88;
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

        public static bool ValidTileToGrowOn(Tile t) => t.HasUnactuatedTile && !t.IsHalfBlock && t.Slope == SlopeType.Solid && 
            (t.TileType == TileID.SnowBlock || t.TileType == TileID.IceBlock || t.TileType == TileID.CorruptIce || t.TileType == TileID.FleshIce || t.TileType == TileID.HallowedIce);

        public static bool PreGrowChecks(int i, int j, int heightClearance, int widthClearance = 2, bool ignoreTrees = false)
        {
            //Can't grow if submerged
            if (Main.tile[i - 1, j - 1].LiquidAmount != 0 || Main.tile[i, j - 1].LiquidAmount != 0 || Main.tile[i + 1, j - 1].LiquidAmount != 0)
                return false;

            Tile groundTile = Main.tile[i, j];

            //Can't grow if the ground isnt snow/a full tile etc , or if the walls are filled
            if (!ValidTileToGrowOn(groundTile) || !WorldGen.DefaultTreeWallTest(Main.tile[i, j - 1].WallType))
                return false;

            //Needs valid tiles on both sides of the tree
            if (!ValidTileToGrowOn(Main.tile[i - 1, j]) || !ValidTileToGrowOn(Main.tile[i + 1, j]))
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
                height = Main.rand.Next(MIN_TREE_HEIGHT, MAX_TREE_HEIGHT + 1);
                if (!PreGrowChecks(i, floorY, height + 4, 3))
                    return false;
            }

            //Copies the palette of the floor
            TileColorCache palette = Main.tile[i, floorY].BlockColorAndCoating();
            if (Main.tenthAnniversaryWorld && WorldGen.generatingWorld)
                palette.Color = FablesWorld.celebrationRainbowPaint;

            int thickBaseHeight = WorldGen.genRand.Next(2, 5);
            if (thickBaseHeight == 2 && height > 9)
                thickBaseHeight = 3;

            int tiltDirection = WorldGen.genRand.NextBool() ? 1 : -1;

            int thickBaseLeft = i; //Bends towards the left
            if (tiltDirection == 1) //Bends towards the right
                thickBaseLeft = i - 1;

            int trunkBendHeight = WorldGen.genRand.Next(thickBaseHeight + 1, height - 2);

            if (thickBaseHeight == 2 && trunkBendHeight == 3)
                trunkBendHeight = WorldGen.genRand.Next(thickBaseHeight + 3, height - 2);

            int trunkX = i;

            //Go up the tree
            for (int j = floorY - 1; j >= floorY - height; j--)
            {
                if (j <= floorY - 1 && j >= floorY - thickBaseHeight)
                {
                    for (int tr = 0; tr < 2; tr ++)
                    {
                        Tile trunkTile = Main.tile[thickBaseLeft + tr, j];
                        trunkTile.TileFrameNumber = WorldGen.genRand.Next(3);
                        trunkTile.HasTile = true;
                        trunkTile.TileType = (ushort)TreeType;
                        trunkTile.UseBlockColors(palette);

                        trunkTile.TileFrameY = (short)(132 + 22 * trunkTile.TileFrameNumber);
                        trunkTile.TileFrameX = (short)(tr * 22);
                    }

                    //Base
                    if (j == floorY - 1)
                    {
                        bool connectLeft = !WorldGen.genRand.NextBool(3);
                        bool connectRight = !WorldGen.genRand.NextBool(3);

                        if (connectLeft)
                        {
                            Tile rootTile = Main.tile[thickBaseLeft - 1, j];
                            rootTile.TileFrameNumber = WorldGen.genRand.Next(3);
                            rootTile.HasTile = true;
                            rootTile.TileType = (ushort)TreeType;
                            rootTile.UseBlockColors(palette);

                            rootTile.TileFrameX = 176;
                            rootTile.TileFrameY = (short)(132 + 22 * rootTile.TileFrameNumber);

                            Tile rootConnectTile = Main.tile[thickBaseLeft, j];
                            rootConnectTile.TileFrameX += 88;
                        }

                        if (connectRight)
                        {
                            Tile rootTile = Main.tile[thickBaseLeft + 2, j];
                            rootTile.TileFrameNumber = WorldGen.genRand.Next(3);
                            rootTile.HasTile = true;
                            rootTile.TileType = (ushort)TreeType;
                            rootTile.UseBlockColors(palette);

                            rootTile.TileFrameX = 198;
                            rootTile.TileFrameY = (short)(132 + 22 * rootTile.TileFrameNumber);

                            Tile rootConnectTile = Main.tile[thickBaseLeft + 1, j];
                            rootConnectTile.TileFrameX += 88;
                        }

                    }

                    //Transition between 2 thick and 1 thick
                    else if (j == floorY - thickBaseHeight)
                    {
                        //Main trunk continus on the left side, with the right half of the thick trunk sloping towards it
                        if (thickBaseLeft == i)
                        {
                            Tile mainTrunkTile = Main.tile[thickBaseLeft, j];
                            Tile diagonalTrunkTile = Main.tile[thickBaseLeft + 1, j];
                            mainTrunkTile.TileFrameX = 88;
                            mainTrunkTile.TileFrameY = 22;
                            diagonalTrunkTile.TileFrameX = 66;
                            diagonalTrunkTile.TileFrameY = 22;
                        }
                        //Main trunk continues on the right side, with the left half of the thick trunk sloping towards it
                        else
                        {
                            Tile diagonalTrunkTile = Main.tile[thickBaseLeft, j];
                            Tile mainTrunkTile = Main.tile[thickBaseLeft + 1, j];
                            mainTrunkTile.TileFrameX = 88;
                            mainTrunkTile.TileFrameY = 0;
                            diagonalTrunkTile.TileFrameX = 66;
                            diagonalTrunkTile.TileFrameY = 0;
                        }
                    }
                }

                else
                {
                    int frameNumber = WorldGen.genRand.Next(3);
                    Tile tile = Main.tile[trunkX, j];
                    tile.TileFrameNumber = frameNumber;
                    tile.HasTile = true;
                    tile.TileType = (ushort)TreeType;
                    tile.UseBlockColors(palette);

                    tile.TileFrameX = 0;
                    tile.TileFrameY = (short)(frameNumber * 22);

                    //Random variants for trunk
                    if (WorldGen.genRand.NextBool(3))
                        tile.TileFrameX += (short)(WorldGen.genRand.NextBool() ? 22 : 44);

                    //Treetop
                    if (j == floorY - height)
                    {
                        frameNumber = WorldGen.genRand.Next(6);
                        tile.TileFrameX = 0;
                        tile.TileFrameY = (short)(200 + frameNumber);
                    }
                }

                //Sideways bend where it misaligns
                if (j == floorY - trunkBendHeight)
                {
                    int bendLeft = trunkX;
                    if (tiltDirection == -1)
                        bendLeft -= 1;

                    int bendRand = WorldGen.genRand.Next(3);

                    for (int bnd = 0; bnd < 2; bnd++)
                    {
                        Tile tile = Main.tile[bendLeft + bnd, j];
                        tile.HasTile = true;
                        tile.TileType = (ushort)TreeType;
                        tile.UseBlockColors(palette);
                        tile.TileFrameNumber = bendRand;
                        tile.TileFrameY = (short)(66 + bendRand * 22);
                        tile.TileFrameX = (short)(22 + bnd * 22);

                        if (tiltDirection == 1)
                            tile.TileFrameX += 44;
                    }

                    trunkX += tiltDirection;
                }
            }

            WorldGen.RangeFrame(i - 2, floorY - height - 1, i + 2, floorY);
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, i - 1, floorY - height - 2, 3, height + 2);

            if (!WorldGen.generatingWorld && WorldGen.PlayerLOS(i, floorY - height))
                new WisteriaGrowthFXPacket(i, floorY - 1, height, 1).Send();

            return true;
        }

        public static void GrowthEffects(int i, int j, bool tileBreak)
        {
            Vector2 tileCenter = new Vector2(i, j) * 16f + new Vector2(8f, 8f);
            tileCenter -= new Vector2(5f, 7f); //Center it for the gore spawned
            var source = WorldGen.GetItemSource_FromTreeShake(i, j);

            //Treetop
            if (Main.tile[i, j].TileFrameY >= 200)
            {
                int leafCount = 4;
                if (tileBreak)
                    leafCount = 10;

                for (int g = 0; g < leafCount; g++)
                    Gore.NewGore(source, tileCenter - Vector2.UnitY * 30 + Main.rand.NextVector2Circular(60f, 40f), Main.rand.NextVector2Circular(10, 10), LeafType, 1f); ;
            }
            else if (!tileBreak && Main.rand.NextBool())
                Gore.NewGore(source, tileCenter + Main.rand.NextVector2Circular(10, 10), Main.rand.NextVector2Circular(10, 10), LeafType, 0.7f + Main.rand.NextFloat(0.6f));
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
            if (TileDrawing.IsVisible(t) && t.TileFrameY >= 200) //Treetop
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.AboveTiles, true);
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

            int variant = (t.TileFrameY - 200) / 3;
            int tintVariant = (t.TileFrameY - 200) % 3;
            bool flipped = i % 2 == 1;

            Color tint = tintVariant == 0 ? Color.Transparent : tintVariant == 1 ? new Color(0, 200, 156, 64) : new Color(210, 2, 200, 64);


            bool blizzard = Main.LocalPlayer.ZoneRain && Main.LocalPlayer.ZoneSnow;
            if (Main.remixWorld)
                blizzard = (double)(Main.LocalPlayer.position.Y / 16f) > Main.worldSurface && Main.raining && Main.LocalPlayer.ZoneSnow;

            if (blizzard)
            {
                float blizzStrenght = Scene["Blizzard"].GetShader().CombinedOpacity;
                tint = Color.Lerp(tint, new Color(143, 215, 220, 175), blizzStrenght * 0.8f);
            }

            Rectangle treetopBranchesFrame = new Rectangle(220, variant * 58, 150, 56);
            Vector2 treetopBranchesOrigin = new Vector2(66, 56);
            Vector2 drawPos = new Vector2(i * 16 + 8, j * 16 + 16) - Main.Camera.UnscaledPosition;

            if (flipped)
            {
                treetopBranchesOrigin.X = treetopBranchesFrame.Width - treetopBranchesOrigin.X;
                treetopBranchesFrame.Y += 116;
            }

            Rectangle treetopLeavesFrame = new Rectangle(372 + 218 * variant, 0, 216, 158);
            Vector2 treetopLeavesOrigin = new Vector2(108, 88);

            float sway1 = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.treeWindCounter) * 5f;
            float sway2 = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.treeWindCounter + 0.6f) * 5f;
            float sway3 = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.treeWindCounter + 1.8f) * 5f;

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            Matrix renderMatrix = view * projection;
            Effect effect = Scene["WisteriaLeaves"].GetShader().Shader;

            //Lower layer
            Rectangle backLeavesFrame = treetopLeavesFrame;
            backLeavesFrame.Y += 160 * 2;
            effect.Parameters["screenBlendColor"].SetValue(tint.ToVector4());
            RenderWithSway(effect, renderMatrix, texture, drawPos, backLeavesFrame, treetopLeavesOrigin, drawColor, sway1, flipped);

            //Branches
            effect.Parameters["screenBlendColor"].SetValue(Color.Transparent);
            RenderWithSway(effect, renderMatrix, texture, drawPos, treetopBranchesFrame, treetopBranchesOrigin, drawColor, 0f, false);

            //Main leaves
            effect.Parameters["screenBlendColor"].SetValue(tint.ToVector4());
            RenderWithSway(effect, renderMatrix, texture, drawPos, treetopLeavesFrame, treetopLeavesOrigin, drawColor, sway2, flipped);

            //Top layer
            Rectangle frontLeavesFrame = treetopLeavesFrame;
            frontLeavesFrame.Y += 160 ;
            RenderWithSway(effect, renderMatrix, texture, drawPos, frontLeavesFrame, treetopLeavesOrigin, drawColor, sway3, flipped);



            //Spawn falling dust
            if (!Main.gamePaused && Main.instance.IsActive)
            {
                int leafSpawnChance = WindHelper.leafFrequency / 2;
                if (!WorldGen.DoesWindBlowAtThisHeight(j))
                    leafSpawnChance = 10000;
                if (!Main.rand.NextBool(leafSpawnChance))
                    return;

                Vector2 leafPos = new Vector2(i * 16 + Main.rand.NextFloat(-22, 22), j * 16 - Main.rand.NextFloat(0f, 40f));
                if (WorldGen.SolidTile(leafPos.ToTileCoordinates()))
                    return;
                Gore.NewGore(WorldGen.GetItemSource_FromTreeShake(i, j), leafPos, Main.rand.NextVector2Circular(10, 10), LeafType, 0.7f + Main.rand.NextFloat(0.6f));
            }
        }

        public void RenderWithSway(Effect effect, Matrix worldViewProjection, Texture2D texture, Vector2 position, Rectangle frame, Vector2 origin, Color color, float sway, bool flipped = false)
        {
            if (flipped)
                origin.X = frame.Width - origin.X;

            Vector3 topLeft = (position - origin).Vec3();
            Vector3 botLeft = topLeft + Vector3.UnitY * frame.Height;
            botLeft.X += sway;

            float leftUv = flipped ? 1 : 0;
            float rightUv = flipped ? 0 : 1;

            short[] indices = [0, 1, 2, 1, 3, 2];
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[4];
            vertices[0] = new(topLeft,                               color, new Vector2(leftUv,  0));
            vertices[1] = new(topLeft + Vector3.UnitX * frame.Width, color, new Vector2(rightUv, 0));
            vertices[2] = new(botLeft,                               color, new Vector2(leftUv,  1));
            vertices[3] = new(botLeft + Vector3.UnitX * frame.Width, color, new Vector2(rightUv, 1));


            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                effect.Parameters["textureResolution"].SetValue(texture.Size());
                effect.Parameters["sampleTexture"].SetValue(texture);
                effect.Parameters["frame"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
                effect.Parameters["uWorldViewProjection"].SetValue(worldViewProjection);
                pass.Apply();

                Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
            }
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            width = 20;

            //Connected to the floor
            if (tileFrameY >= 132 && tileFrameY < 200)
                height = 18;
            else if (tileFrameY >= 66 && tileFrameY <= 110 && tileFrameX >= 22)
            {
                height = 20;
                offsetY = -2;
            }
        }

        #endregion

        #region Shaking code that has to be custom because of the bent shape
        public static bool ShakeTree(ref int i, ref int j, int type)
        {
            if (ReachedTreeShakeCap())
                return false;

            int treeTopY = j;
            int treeTopX = i;


            while (treeTopY > 10 && Main.tile[treeTopX, treeTopY].HasTile && Main.tile[treeTopX, treeTopY].TileType == type)
            {
                Tile t = Main.tile[treeTopX, treeTopY];

                //Funnel part that turns the 2 thick trunk into a 1 thick trunk
                if (t.TileFrameY <= 22 && t.TileFrameX == 66)
                {
                    if (t.TileFrameY == 0)
                        treeTopX++;
                    else
                        treeTopX--;
                }

                //Bend in the 1 thick trunk
                if (t.TileFrameY < 132 && t.TileFrameY >= 66 && t.TileFrameX >= 44 && t.TileFrameX <= 66)
                {
                    if (t.TileFrameX == 44)
                        treeTopX--;
                    else
                        treeTopX++;
                }
                treeTopY--;
            }
            treeTopY++;

            if (!IsTreeTop(treeTopX, treeTopY) || Collision.SolidTiles(treeTopX - 2, treeTopX + 2, treeTopY - 2, treeTopY + 2))
                return false;

            int treeBottomX = treeTopX;
            int treeBottomY = treeTopY;
            while (treeBottomY < Main.maxTilesY - 10 && Main.tile[treeBottomX, treeBottomY].HasTile && Main.tile[treeBottomX, treeBottomY].TileType == type)
            {
                Tile t = Main.tile[treeBottomX, treeBottomY];

                //Bend in the 1 thick trunk
                if (t.TileFrameY < 132 && t.TileFrameY >= 66 && (t.TileFrameX == 22 || t.TileFrameX == 88))
                {
                    if (t.TileFrameX == 22)
                        treeBottomX++;
                    else
                        treeBottomX--;
                }

                treeBottomY++;
            }
            treeBottomY--;

            if (!CheckIfTreeAlreadyShakenAndRegisterOtherwise(treeBottomX, treeBottomY))
                return false;


            j = treeTopY;
            i = treeTopX;
            return true;
        }
        #endregion

    }

    #region Sapling
    public class WisteriaSapling : ModTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            WisteriaTree.SaplingType = Type;
            FablesGeneralSystemHooks.FertilizeSaplingEvent.Add(Type, WisteriaTree.GrowTree);

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.CoordinateHeights = new[] { 40, 0 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = -2;
            TileObjectData.newTile.AnchorValidTiles = new[] { (int)TileID.SnowBlock };
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

            AddMapEntry(new Color(111, 142, 24), Language.GetText("MapObject.Sapling"));

            DustType = DustID.BubbleBurst_Purple;
            AdjTiles = new int[] { TileID.Saplings };
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 4;

        public override void RandomUpdate(int i, int j)
        {
            if (WorldGen.genRand.NextBool(5) && Main.netMode != NetmodeID.MultiplayerClient)
                WisteriaTree.GrowTree(i, j, j > (int)Main.worldSurface - 1);
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
        {
            if (i % 2 == 0)
                effects = SpriteEffects.FlipHorizontally;
        }

        public override bool CanDrop(int i, int j) => false;
    }

    public class WisteriaSaplingItem : ModItem
    {
        public override string Texture => AssetDirectory.VanityTrees + "PottedWisteriaTree";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Arctic Wisteria Sapling");
            Item.ResearchUnlockCount = 25;
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<WisteriaSapling>());
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }
    }
    #endregion

    [Serializable]
    public class WisteriaGrowthFXPacket : TreeGrowFXPacket
    {
        public WisteriaGrowthFXPacket(int i, int baseY, int height, byte effectCount) : base(i, baseY, height, effectCount) { }
        public override GrowEffectDelegate GrowEffect => WisteriaTree.GrowthEffects;
    }


    #region Leaf gores
    public class WisteriaTreeLeaf : ModGore
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            WisteriaTree.LeafType = Type;

            ChildSafety.SafeGore[Type] = true; // Leaf gore should appear regardless of the "Blood and Gore" setting
            GoreID.Sets.SpecialAI[Type] = 3; // Falling leaf behavior
                                             // GoreID.Sets.PaintedFallingLeaf[Type] = true; // This is used for all vanilla tree leaves, related to the bigger spritesheet for tile paints
        }
    }
    #endregion
}