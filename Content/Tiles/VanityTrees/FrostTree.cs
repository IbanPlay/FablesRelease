using CalamityFables.Helpers;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Cryptography;
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
    public class FrostTree : ModTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public static Asset<Texture2D> ShimmerNoiseTexture;

        public static int SaplingType;
        public static int TreeType;
        public static int NeedleDustType;
        public static int DustSpeckType;

        public const int MIN_TREE_HEIGHT = 6;
        public const int MAX_TREE_HEIGHT = 9;

        public const int TREETOP_HEIGHT = 10;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = false;
            Main.tileLavaDeath[Type] = true;
            Main.tileAxe[Type] = true;
            Main.tileLighted[Type] = true;

            TileID.Sets.IsATreeTrunk[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;
            FablesTile.OverrideKillTileEvent += PreventTileBreakIfOnTopOfIt;

            //TileID.Sets.IsShakeable[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
            DustType = 7;

            //HitSound = HitSFX;
            TreeType = Type;
            AddMapEntry(new Color(189, 137, 94), Language.GetText("MapObject.Tree"));
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

        private bool? PreventTileBreakIfOnTopOfIt(int i, int j, int type)
        {
            //Can always break the tree itself
            if (type == Type || j <= 0)
                return true;

            //Tree above
            if (Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].TileType == Type)
            {
                //Can break it if its a branch
                return Main.tile[i, j - 1].TileFrameX >= 66 && Main.tile[i, j - 1].TileFrameX <= 88 && Main.tile[i, j - 1].TileFrameY < 120;
            }

            return null;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile t = Main.tile[i, j];
            if (t.TileFrameY >= 200 && t.TileFrameX <= 2)
            {
                r += 0.2f;
                g += 0.4f;
                b += 0.4f;
            }
        }

        public bool IsTreeTop(int i, int j) => Main.tile[i, j].TileFrameY >= 200 && Main.tile[i, j].TileFrameX == 1;
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !fail)
                return;

            //Shake tree
            if (ShakeTree(ref i, ref j, Type, IsTreeTop))
            {
                j += TREETOP_HEIGHT;

                WeightedRandom<int> lootPool = new WeightedRandom<int>();

                float woodChance = 1 / 12f;
                float iceChance = 1 / 12f;
                float coinChance = 1 / 20f;
                float snowfallChance = 1 / 30f;

                lootPool.Add(ItemID.None, 1f - woodChance - iceChance - coinChance - snowfallChance);
                lootPool.Add(ItemID.BorealWood, woodChance);  //Wood 1 - 3
                lootPool.Add(ItemID.IceBlock, iceChance);
                lootPool.Add(ItemID.CopperCoin, coinChance);
                lootPool.Add(-1, snowfallChance);
                if (Main.halloween)
                    lootPool.Add(ItemID.RottenEgg, 1 / 35f); //Rotten eggs : 1 - 2

                int drop = (int)lootPool;

                if (drop == -1)
                {
                    for (int s = 0; s < 10; s++)
                    {
                        Vector2 position = new Vector2(i, j - 5) * 16 + Main.rand.NextVector2Circular(80, 40);
                        Projectile.NewProjectile(WorldGen.GetNPCSource_ShakeTree(i, j), position, Vector2.UnitY * 8f, ProjectileID.SnowBallHostile, 10, 1, Main.myPlayer);
                    }
                }
                else if (drop == ItemID.CopperCoin)
                    DropCoinsFromTreeShake(i, j);
                else
                {
                    int dropCount = 1;
                    if (drop == ItemID.RottenEgg || drop == ItemID.BorealWood)
                        dropCount = WorldGen.genRand.Next(1, 3);
                    if (drop == ItemID.IceBlock)
                        dropCount = WorldGen.genRand.Next(3, 10);

                    Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), i * 16, j * 16, 16, 16, drop, dropCount);
                }

                new FrostGrowFXPacket(i, j, 1, 2).Send();
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

            bool isBranch = originalFrameX >= 66 && originalFrameX <= 88 && originalFrameY < 120;
            bool isRoot = originalFrameX >= 22 && originalFrameX <= 44 && originalFrameY >= 120;
            bool isRegularTrunk = (originalFrameX < 66 && originalFrameY < 120);
            bool isTreetop = originalFrameY >= 200;

            //Branch or stum roots without a tile to connect to
            if (isBranch || isRoot)
            {
                int connectionDirection = ((originalFrameX / 22) % 2) * 2 - 1; //alternates between -1 and 1
                if (!Main.tile[i + connectionDirection, j].HasTile || Main.tile[i + connectionDirection, j].TileType != Type)
                    WorldGen.KillTile(i, j);
            }
            if (!isBranch)
            {
                //Break without the appropriate tile underneath
                if (!ValidTileToGrowOn(Main.tile[i, j + 1]) && bottomTileType != Type)
                    WorldGen.KillTile(i, j);
            }

            if (isTreetop)
            {
                //FrameX == 1 is the final stop for the treetop
                if (originalFrameX != 1 && topTileType != Type)
                    WorldGen.KillTile(i, j);
            }


            if (!isBranch && !isRoot)
            {
                //Treetop break
                bool hasUnbrokenTop = isRegularTrunk || originalFrameX / 22 % 2 == 1;
                if (topTileType != Type && hasUnbrokenTop && !isTreetop)
                {
                    if (isRegularTrunk)
                    {
                        tile.TileFrameX = 0;
                        tile.TileFrameY = (short)(120 + WorldGen.genRand.Next(3) * 20);
                    }
                    else
                        tile.TileFrameX += 22;
                }

                bool connectedLeft = Main.tile[i - 1, j].HasTile && Main.tile[i - 1, j].TileType == Type;
                bool connectedRight = Main.tile[i + 1, j].HasTile && Main.tile[i + 1, j].TileType == Type;

                //Stumps with connections
                if ((originalFrameX >= 154) || (originalFrameX >= 110 && originalFrameY >= 120))
                {
                    //Stump connected on both sides
                    if (originalFrameY >= 120 && !connectedLeft) //If not connected left, turn into a stump with only a connection on the right side
                    {
                        tile.TileFrameX += 44;
                        tile.TileFrameY -= 60;
                    }

                    if (tile.TileFrameY >= 120 && !connectedRight) //If not connected right, turn into a stump with only a connection on the left side
                    {
                        tile.TileFrameX += 44;
                        tile.TileFrameY -= 120;
                    }


                    //Stump connected to the right side without a right connection or stump connected to the left side without a stump connection on the left
                    if (tile.TileFrameX >= 154 && ((tile.TileFrameY >= 60 && !connectedRight) || (tile.TileFrameY < 60 && !connectedLeft)))
                    {
                        tile.TileFrameX -= 88;
                        tile.TileFrameY = (short)(120 + ((tile.TileFrameY / 20) % 3) * 20);
                    }
                }

                //Trunks with left branch / trunks with right branch
                if ((tile.TileFrameX >= 110 && tile.TileFrameX < 154 && tile.TileFrameY < 60 && !connectedLeft) ||
                    (tile.TileFrameX >= 110 && tile.TileFrameX < 154 && tile.TileFrameY >= 60 && tile.TileFrameY < 120 && !connectedRight)
                    )
                {
                    //Unbroken top
                    if (tile.TileFrameX == 110)
                    {
                        tile.TileFrameX = 0;
                        tile.TileFrameY = (short)(WorldGen.genRand.Next(3) * 20);
                    }
                    //Broken top
                    else
                    {
                        tile.TileFrameX = 0;
                        tile.TileFrameY = (short)(120 + WorldGen.genRand.Next(3) * 20);
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

        public static bool ValidTileToGrowOn(Tile t) => t.HasUnactuatedTile && !t.IsHalfBlock && t.Slope == SlopeType.Solid &&
            (t.TileType == TileID.SnowBlock || t.TileType == TileID.IceBlock || t.TileType == TileID.CorruptIce || t.TileType == TileID.FleshIce || t.TileType == TileID.HallowedIce);

        public static bool PreGrowChecks(int i, int j, int heightClearance, int widthClearance = 2, bool ignoreTrees = false)
        {
            //Can't grow if submerged
            if (Main.tile[i - 1, j - 1].LiquidAmount != 0 || Main.tile[i, j - 1].LiquidAmount != 0 || Main.tile[i + 1, j - 1].LiquidAmount != 0)
                return false;

            //Can't grow if the tile below isnt a full tile
            Tile groundTile = Main.tile[i, j];
            if (!ValidTileToGrowOn(groundTile) || !WorldGen.DefaultTreeWallTest(Main.tile[i, j - 1].WallType))
                return false;

            //Can't grow if there's not a valid ground tile on the left or right of where its growing
            if (!ValidTileToGrowOn(Main.tile[i - 1, j]) && !ValidTileToGrowOn(Main.tile[i + 1, j]))
                return false;

            //Check for sufficient empty space
            if (!FablesWorld.TreeTileClearanceCheck(i - widthClearance, i + widthClearance, j - heightClearance - 1, j - 2, ignoreTrees))
                return false;
            if (!FablesWorld.TreeTileClearanceCheck(i - 1, i + 1, j - 1, j - 1, ignoreTrees))
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

                if (!PreGrowChecks(i, floorY, height + TREETOP_HEIGHT + 2))
                    return false;
            }
            else
                height -= TREETOP_HEIGHT;

            //Copies the palette of the floor
            TileColorCache palette = Main.tile[i, floorY].BlockColorAndCoating();

            if (Main.tenthAnniversaryWorld && WorldGen.generatingWorld)
                palette.Color = FablesWorld.celebrationRainbowPaint;

            bool hasBranch = !WorldGen.genRand.NextBool(3);

            //Go up the tree
            for (int j = floorY - 1; j >= floorY - height - TREETOP_HEIGHT; j--)
            {
                int frameNumber = WorldGen.genRand.Next(3);
                Tile tile = Main.tile[i, j];
                tile.TileFrameNumber = frameNumber;
                tile.HasTile = true;
                tile.TileType = (ushort)TreeType;
                tile.UseBlockColors(palette);

                //Stump
                if (j == floorY - 1)
                {

                    tile.TileFrameX = 66;
                    tile.TileFrameY = (short)(120 + 20 * frameNumber);
                    continue;
                }

                //Treetop
                if (j <= floorY - height)
                {
                    frameNumber = WorldGen.genRand.Next(2);
                    tile.TileFrameY = (short)(200 + frameNumber);
                    tile.TileFrameX = 2;

                    //First treetop tile which is the one that handles drawing
                    if (j == floorY - height)
                        tile.TileFrameX = 0;

                    //Last treetop tile which is the one used for shaking tree detection
                    if (j == floorY - height - TREETOP_HEIGHT)
                        tile.TileFrameX = 1;

                    continue;
                }

                int trunkVariant = WorldGen.genRand.Next(10);
                //Half the variants are the basic trunk
                if (trunkVariant > 4)
                {
                    tile.TileFrameX = 0;
                    tile.TileFrameY = (short)(20 * frameNumber);
                }
                //Other half are the variants
                else
                {
                    //Trunk variants with holes look weird due to the gradient applied on the trunk by the treetop, so we dont have those when were tall enough
                    if (trunkVariant < 2 && j < floorY - height + 3)
                        trunkVariant = 4;

                    tile.TileFrameX = (short)(((trunkVariant + 1) % 3) * 22);
                    tile.TileFrameY = (short)(((trunkVariant + 1) / 3) * 20);

                    tile.TileFrameY += (short)(20 * frameNumber);
                }
            }

            bool hasLeftRoot = WorldGen.genRand.NextBool();
            bool hasRightRoot = WorldGen.genRand.NextBool();

            Tile treeStump = Main.tile[i, floorY - 1];

            if (hasLeftRoot)
            {
                Tile root = Main.tile[i - 1, floorY - 1];
                root.HasTile = true;
                root.TileType = (ushort)TreeType;
                root.UseBlockColors(palette);
                int frameVariantRNG = WorldGen.genRand.Next(3);
                root.TileFrameNumber = frameVariantRNG;
                root.TileFrameX = 22;
                root.TileFrameY = (short)(120 + frameVariantRNG * 20);

                treeStump.TileFrameX = 154;
                treeStump.TileFrameY = (short)(WorldGen.genRand.Next(3) * 20);
            }
            if (hasRightRoot)
            {
                Tile root = Main.tile[i + 1, floorY - 1];
                root.HasTile = true;
                root.TileType = (ushort)TreeType;
                root.UseBlockColors(palette);
                int frameVariantRNG = WorldGen.genRand.Next(3);
                root.TileFrameNumber = frameVariantRNG;
                root.TileFrameX = 44;
                root.TileFrameY = (short)(120 + frameVariantRNG * 20);

                treeStump.TileFrameX = 154;
                treeStump.TileFrameY = (short)(WorldGen.genRand.Next(3) * 20 + 60);
            }

            if (hasRightRoot && hasLeftRoot)
            {
                treeStump.TileFrameX = 110;
                treeStump.TileFrameY = (short)(WorldGen.genRand.Next(3) * 20 + 120);
            }


            //1 in 3 chance to have no branches
            if (hasBranch)
            {
                int branchHeight = WorldGen.genRand.Next(5, height);
                int branchSide = WorldGen.genRand.NextBool() ? 1 : -1;

                Tile branch = Main.tile[i + branchSide, floorY - branchHeight];
                branch.HasTile = true;
                branch.TileType = (ushort)TreeType;
                branch.UseBlockColors(palette);
                int frameVariantRNG = WorldGen.genRand.Next(3);
                branch.TileFrameNumber = frameVariantRNG;
                branch.TileFrameX = (short)(branchSide < 0 ? 66 : 88);
                branch.TileFrameY = (short)(branchSide < 0 ? 0 : 60);
                branch.TileFrameY += (short)(frameVariantRNG * 20);

                //Turn the connected tile into a connection tile
                Tile connectedTile = Main.tile[i, floorY - branchHeight];
                {
                    frameVariantRNG = WorldGen.genRand.Next(3);
                    connectedTile.TileFrameX = 110;
                    connectedTile.TileFrameY = (short)(frameVariantRNG * 20);
                    connectedTile.TileFrameY += (short)(branchSide < 0 ? 0 : 60);
                }
            }

            WorldGen.RangeFrame(i - 2, floorY - height - TREETOP_HEIGHT - 1, i + 2, floorY);
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, i - 1, floorY - height - TREETOP_HEIGHT - 2, 3, height + TREETOP_HEIGHT + 2);

            if (!WorldGen.generatingWorld && WorldGen.PlayerLOS(i, floorY - height - TREETOP_HEIGHT))
                new FrostGrowFXPacket(i, floorY - 1, height, 2).Send();

            return true;
        }
        #endregion

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 2;

        public override bool CreateDust(int i, int j, ref int type)
        {
            GrowthEffects(i, j, true);
            return true;
        }

        public static void GrowthEffects(int i, int j, bool tileBreak)
        {
            Vector2 tileCenter = new Vector2(i, j) * 16f + new Vector2(8f, 8f);
            tileCenter -= new Vector2(5f, 7f); //Center it for the gore spawned
            var source = WorldGen.GetItemSource_FromTreeShake(i, j);
            ;
            //Treetop

            if (Main.tile[i, j].TileFrameY >= 200 && Main.tile[i, j].TileFrameX == 0)
            {
                int leafCount = 6;
                if (tileBreak)
                    leafCount = 5;

                for (int g = 0; g < leafCount; g++)
                {
                    float heightPercent = Main.rand.NextFloat();

                    Vector2 leafPos = new Vector2(i * 16 + Main.rand.NextFloat(-120, 120) * (1 - heightPercent), j * 16 - heightPercent * 300f);

                    Vector2 velocity = tileBreak ? Vector2.UnitY * 2f + Main.rand.NextVector2CircularEdge(1, 1) * 6f : Vector2.Zero;

                    Dust.NewDustPerfect(leafPos, NeedleDustType, velocity);
                }
            }
            else if (!tileBreak && Main.rand.NextBool())
            {
                Vector2 leafPos = new Vector2(i * 16 + 8, j * 16 + 8) + Main.rand.NextVector2Circular(8f, 9f);
                Dust.NewDustPerfect(leafPos, NeedleDustType, Vector2.Zero);
            }

        }

        #region Drawing
        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];
            if (!TileDrawing.IsVisible(t))
                return;

            //Add custom draw point for the treetops
            if (t.TileFrameY >= 200 && t.TileFrameX == 0)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.AboveTiles, true);
        }

        public void GetColorSatTint(int i, int j, out Vector4 colorTint, out Vector2 satTint)
        {
            float hueTintStrenght = 0f;
            float hueTintColor = 0.45f;

            int rngIndex = i + j;
            if (rngIndex % 5 == 0)
            {
                hueTintStrenght = 0.4f;
            }
            else if (rngIndex % 5 == 1)
            {
                hueTintStrenght = 0.4f;
                hueTintColor = 0.58f;
            }


            if (Main.LocalPlayer.ZoneCrimson)
            {
                float biomeInfluence = Math.Clamp(Main.SceneMetrics.BloodTileCount / (float)SceneMetrics.CrimsonTileMax, 0, 1);
                hueTintColor = MathHelper.Lerp(hueTintColor, 0.98f, biomeInfluence);
                hueTintStrenght = MathHelper.Lerp(hueTintStrenght, biomeInfluence, (float)Math.Pow(biomeInfluence, 0.5f)) * 0.9f;
            }

            if (Main.LocalPlayer.ZoneCorrupt)
            {
                float biomeInfluence = Math.Clamp(Main.SceneMetrics.EvilTileCount / (float)SceneMetrics.CorruptionTileMax, 0, 1f);
                hueTintColor = MathHelper.Lerp(hueTintColor, 0.65f, biomeInfluence);
                hueTintStrenght = MathHelper.Lerp(hueTintStrenght, biomeInfluence, (float)Math.Pow(biomeInfluence, 0.5f)) * 0.9f;
            }

            if (Main.LocalPlayer.ZoneHallow)
            {
                float biomeInfluence = Math.Clamp(Main.SceneMetrics.HolyTileCount / (float)SceneMetrics.HallowTileMax, 0, 1);
                hueTintColor = MathHelper.Lerp(hueTintColor, 0.87f, biomeInfluence);
                hueTintStrenght = MathHelper.Lerp(hueTintStrenght, biomeInfluence, (float)Math.Pow(biomeInfluence, 0.5f)) * 0.9f;
            }

            float tintR = Math.Abs(hueTintColor * 6 - 3) - 1;
            float tintG = 2 - Math.Abs(hueTintColor * 6 - 2);
            float tintB = 2 - Math.Abs(hueTintColor * 6 - 4);
            colorTint = new Vector4(Math.Clamp(tintR, 0, 1), Math.Clamp(tintG, 0, 1), Math.Clamp(tintB, 0, 1), Math.Clamp(hueTintStrenght, 0f, 0.95f));

            satTint = Vector2.Zero;
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

            Vector2 camPos = Main.Camera.UnscaledPosition;
            Color color = t.IsTileFullbright ? Color.White : Lighting.GetColor(i, j);

            GetColorSatTint(i, j, out Vector4 colorTint, out Vector2 satTint);



            Rectangle trunkExtensionFrame = new Rectangle(2, 240, 16, 22);
            Rectangle trunkTintFrame = new Rectangle(156, 122, 16, 66);
            Color trunkTintColor = color;
            trunkTintColor = Color.Lerp(trunkTintColor, new Color(colorTint.X, colorTint.Y, colorTint.Z), colorTint.W * 0.5f);

            Main.spriteBatch.Draw(texture, new Vector2(i * 16 - 1, j * 16 + 16) - camPos, trunkExtensionFrame, color, 0f, new Vector2(0, 22), 1f, 0, 0);
            Main.spriteBatch.Draw(texture, new Vector2(i * 16 - 1, j * 16 - 16) - camPos, trunkTintFrame, trunkTintColor * 0.6f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            Vector2 drawPos = new Vector2(i * 16 - (int)camPos.X + 7, j * 16 - (int)camPos.Y + 16);

            int variant = t.TileFrameY - 200;

            Rectangle frame = variant == 0 ? new Rectangle(198, 0, 248, 344) : new Rectangle(448, 0, 250, 344);
            Vector2 origin = variant == 0 ? new Vector2(frame.Width / 2, 306) : new Vector2(122, 306);
            float sway = Main.instance.TilesRenderer.GetWindCycle(i, j, WindHelper.treeWindCounter) * 15f;

            ShimmerNoiseTexture ??= ModContent.Request<Texture2D>(AssetDirectory.Noise + "RGBNoise");


            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            Matrix renderMatrix = view * projection;
            Effect effect = Scene["FrozenLeaves"].GetShader().Shader;
            effect.Parameters["satTint"].SetValue(satTint);
            effect.Parameters["hueTint"].SetValue(colorTint);

            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.06f);
            RenderWithSway(effect, renderMatrix, texture, drawPos, frame, origin, new Point(i, j), sway, 5, 3f, i % 2 == 0, t.IsTileFullbright);

            if (!Main.gamePaused && Main.instance.IsActive)
            {
                int leafSpawnChance = WindHelper.leafFrequency / 15;
                if (!WorldGen.DoesWindBlowAtThisHeight(j))
                    leafSpawnChance = 10000;
                if (!Main.rand.NextBool(leafSpawnChance))
                    return;

                float heightPercent = Main.rand.NextFloat();

                Vector2 leafPos = new Vector2(i * 16 + Main.rand.NextFloat(-120, 120) * (1 - heightPercent), j * 16 - heightPercent * 300f);
                if (WorldGen.SolidTile(leafPos.ToTileCoordinates()))
                    return;

                Dust.NewDustPerfect(leafPos, NeedleDustType, Vector2.Zero);
            }
        }


        public void RenderWithSway(Effect effect, Matrix worldViewProjection, Texture2D texture, Vector2 position, Rectangle frame, Vector2 origin, Point tileCoords, float sway, int divisions, float swayBoostPerDivision, bool flipped = false, bool fullBright = false)
        {
            if (flipped)
                origin.X = frame.Width - origin.X;

            Vector3 topLeft = (position - origin).Vec3();
            Vector3 botLeft = topLeft + Vector3.UnitY * frame.Height;
            float leftUv = flipped ? 1 : 0;
            float rightUv = flipped ? 0 : 1;

            short[] indices = new short[6 + 6 * divisions];
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[4 + divisions * 2];
            float worldPosHeight = tileCoords.Y * 16;
            float worldPosLeft = tileCoords.X * 16 - frame.Width / 2; //-2 and +2 so that the sample points are closer to the center
            float worldPosRight = tileCoords.X * 16 + frame.Width / 2;

            int tilePosLeft = (int)(worldPosLeft / 16) + 2;
            int tilePosRight = (int)(worldPosRight / 16) - 2;

            Color colorLeft = fullBright ? Color.White : Lighting.GetColor(new Point(tilePosLeft, tileCoords.Y));
            Color colorRight = fullBright ? Color.White : Lighting.GetColor(new Point(tilePosRight, tileCoords.Y));

            if (!fullBright)
            {
                colorLeft = TintTreetopSegmentColor(colorLeft, worldPosLeft, worldPosHeight);
                colorRight = TintTreetopSegmentColor(colorRight, worldPosRight, worldPosHeight);
            }

            vertices[0] = new(botLeft, colorLeft, new Vector2(leftUv, 1));
            vertices[1] = new(botLeft + Vector3.UnitX * frame.Width, colorRight, new Vector2(rightUv, 1));

            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 3;
            indices[5] = 2;

            int vertexIndex = 2;
            int indexIndex = 6;
            float sideSway = 0;
            float divisionHeight = 1 / (float)(divisions + 1);


            for (int i = 0; i <= divisions; i++)
            {
                float uvY = 1 - (i + 1) / (float)(divisions + 1);
                Vector3 divisionLeft = botLeft - Vector3.UnitY * frame.Height * divisionHeight * (i + 1);
                divisionLeft.X += sideSway;

                Color segmentColorLeft = fullBright ? Color.White : Lighting.GetColor(new Point(tilePosLeft, (int)((worldPosHeight - frame.Height * divisionHeight * (i + 1)) / 16)));
                Color segmentColorRight = fullBright ? Color.White : Lighting.GetColor(new Point(tilePosRight, (int)((worldPosHeight - frame.Height * divisionHeight * (i + 1)) / 16)));

                if (!fullBright)
                {
                    float positionY = worldPosHeight - frame.Height * divisionHeight * (i + 1);
                    segmentColorLeft = TintTreetopSegmentColor(segmentColorLeft, worldPosLeft, positionY);
                    segmentColorRight = TintTreetopSegmentColor(segmentColorRight, worldPosRight, positionY);
                }

                vertices[vertexIndex] = new(divisionLeft, segmentColorLeft, new Vector2(leftUv, uvY));
                vertices[vertexIndex + 1] = new(divisionLeft + Vector3.UnitX * frame.Width, segmentColorRight, new Vector2(rightUv, uvY));

                //Rotational tilt
                if (sideSway > 0)
                {
                    vertices[vertexIndex + 1].Position.Y += sideSway * 2;
                    vertices[vertexIndex].Position.Y -= sideSway * 1.2f;
                }
                else
                {
                    vertices[vertexIndex].Position.Y -= sideSway * 2;
                    vertices[vertexIndex + 1].Position.Y += sideSway * 1.2f;
                }

                if (i < divisions)
                {
                    indices[indexIndex] = (short)(vertexIndex);
                    indices[indexIndex + 1] = (short)(vertexIndex + 1);
                    indices[indexIndex + 2] = (short)(vertexIndex + 2);
                    indices[indexIndex + 3] = (short)(vertexIndex + 1);
                    indices[indexIndex + 4] = (short)(vertexIndex + 3);
                    indices[indexIndex + 5] = (short)(vertexIndex + 2);
                }

                vertexIndex += 2;
                indexIndex += 6;


                sideSway = (float)Math.Pow((i + 1) / (float)divisions, swayBoostPerDivision) * sway;
            }


            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                effect.Parameters["textureResolution"].SetValue(texture.Size());
                effect.Parameters["sampleTexture"].SetValue(texture);
                effect.Parameters["frame"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
                effect.Parameters["uWorldViewProjection"].SetValue(worldViewProjection);
                pass.Apply();


                RasterizerState cache = Main.instance.GraphicsDevice.RasterizerState;
                Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

                Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4 + 2 * divisions, indices, 0, 2 + 2 * divisions);

                Main.instance.GraphicsDevice.RasterizerState = cache;
            }
        }

        public Color TintTreetopSegmentColor(Color baseColor, float positionX, float positionY)
        {
            baseColor.R = (byte)Math.Clamp(baseColor.R + 10, 0, 255);
            baseColor.G = (byte)Math.Clamp(baseColor.G + MathF.Sin(positionY + Main.GlobalTimeWrappedHourly * 1.5f + positionX * 0.3f) * 17 + 17, 0, 255);
            baseColor.B = (byte)Math.Clamp(baseColor.B + MathF.Sin(positionY + Main.GlobalTimeWrappedHourly - positionX * 0.3f) * 28 + 28, 0, 255);
            return baseColor;
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            width = 22;
            height = 18;

            //Branches have an offset
            if (tileFrameX >= 66 && tileFrameX <= 88 && tileFrameY < 120)
            {
                offsetY = -2;
            }
        }
        #endregion
    }

    #region Sapling
    public class FrostSapling : ModTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            FrostTree.SaplingType = Type;
            FablesGeneralSystemHooks.FertilizeSaplingEvent.Add(Type, FrostTree.GrowTree);

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
            TileObjectData.newTile.AnchorValidTiles = new[] { (int)TileID.SnowBlock };
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

            AddMapEntry(new Color(93, 238, 248), Language.GetText("MapObject.Sapling"));

            DustType = DustID.Ice;
            AdjTiles = new int[] { TileID.Saplings };
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void RandomUpdate(int i, int j)
        {
            if (WorldGen.genRand.NextBool(5) && Main.netMode != NetmodeID.MultiplayerClient)
                FrostTree.GrowTree(i, j, j > (int)Main.worldSurface - 1);
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
        {
            if (i % 2 == 0)
                effects = SpriteEffects.FlipHorizontally;
        }

        public override bool CanDrop(int i, int j) => false;
    }

    public class FrostSaplingItem : ModItem
    {
        public override string Texture => AssetDirectory.VanityTrees + "PottedFrostTree";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Everfrost Pine Sapling");
            Item.ResearchUnlockCount = 25;
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<FrostSapling>());
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }
    }
    #endregion

    [Serializable]
    public class FrostGrowFXPacket : TreeGrowFXPacket
    {
        public FrostGrowFXPacket(int i, int baseY, int height, byte effectCount) : base(i, baseY, height, effectCount) { }
        public override GrowEffectDelegate GrowEffect => FrostTree.GrowthEffects;
    }

    #region dust

    public class FrostNeedleDust : ModDust
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public static int SpeckType;

        public override void SetStaticDefaults()
        {
            FrostTree.NeedleDustType = Type;
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

            if (Main.rand.NextBool(60))
            {
                Dust.NewDustPerfect(dust.position + Main.rand.NextVector2Circular(3f, 3f), SpeckType, dust.velocity * 0.4f, Scale: Main.rand.NextFloat(1f, 1.2f));
            }

            if (dust.scale <= 0.3)
                dust.active = false;

            return false;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor) => Color.Lerp(lightColor, Color.White, 0.3f) with { A = 0 };
    }

    public class FrostSpeckDust : ModDust
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            FrostTree.DustSpeckType = Type;
            FrostNeedleDust.SpeckType = Type;
        }

        public override void OnSpawn(Dust dust)
        {
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;

            dust.scale *= 0.98f;
            dust.rotation += 0.01f;

            dust.alpha = 1;

            if (Collision.SolidCollision(dust.position, 1, 1))
            {
                dust.scale *= 0.96f;
                dust.velocity *= 0.2f;
            }

            if (dust.scale <= 0.2)
                dust.active = false;

            return false;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor) => Color.Lerp(lightColor, Color.White, 0.3f) with { A = 0 };
    }
    #endregion
}