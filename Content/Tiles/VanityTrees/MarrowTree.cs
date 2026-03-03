using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using Steamworks;
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
    public class MarrowTree : ModTile, ICustomLayerTile
    {
        public static readonly SoundStyle HitSFX = new SoundStyle(SoundDirectory.Tiles + "MarrowTreeHit", 3) with { Volume = 0.4f, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest  };

        public override string Texture => AssetDirectory.VanityTrees + Name;

        public static int SaplingType;
        public static int TreeType;
        public static int BloodDustType;
        public static int BoneDustType;

        public const int MIN_TREE_HEIGHT = 9;
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
            DustType = DustID.DynastyWall;

            HitSound = HitSFX;
            TreeType = Type;
            AddMapEntry(new Color(226, 180, 169), Language.GetText("MapObject.Tree"));
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            yield return new Item(ItemID.ViciousPowder, Main.rand.Next(1, 3)).OverrideTexture("Tiles/VanityTrees/MarrowDust");
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
                return Main.tile[i, j - 1].TileFrameX >= 160;
            }

            return null;
        }

        public bool IsTreeTop(int i, int j) => Main.tile[i, j].TileFrameY >= 200;
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
                lootPool.Add(ItemID.ViciousPowder, woodChance);  //Vicious powder 1 - 3
                lootPool.Add(ItemID.ViciousMushroom, fruitChance); //Vicious powder 1 - 2
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
                    if (drop == ItemID.RottenEgg || drop == ItemID.ViciousMushroom)
                        dropCount = WorldGen.genRand.Next(1, 3);
                    else if (drop == ItemID.ViciousPowder)
                        dropCount = WorldGen.genRand.Next(1, 4);

                    Item item = new Item(drop, dropCount);
                    if (drop == ItemID.ViciousPowder)
                        item.OverrideTexture("Tiles/VanityTrees/MarrowDust");

                    Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), i * 16, j * 16, 16, 16, item);
                }

                new MarrowGrowFXPacket(i, j, 1, 2).Send();
            }
        }

        #region Generation and framing

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tile tile = Main.tile[i, j];
            int originalFrameX = tile.TileFrameX;
            int originalFrameY = tile.TileFrameY;

            int topTileType = -1;
            int leftTileType = -1;
            int righTileType = -1;
            int bottomTileType = -1;
            if (Main.tile[i - 1, j].HasTile)
                leftTileType = Main.tile[i - 1, j].TileType;
            if (Main.tile[i + 1, j].HasTile)
                righTileType = Main.tile[i + 1, j].TileType;
            if (Main.tile[i, j - 1].HasTile)
                topTileType = Main.tile[i, j - 1].TileType;
            if (Main.tile[i, j + 1].HasTile)
                bottomTileType = Main.tile[i, j + 1].TileType;


            bool branchRight = righTileType == Type;
            bool branchLeft = leftTileType == Type;

            //Branch without a tile to connect to
            if (tile.TileFrameX >= 160)
            {
                if ((tile.TileFrameY == 0 && !branchRight) || (tile.TileFrameY == 16 && !branchLeft))
                    WorldGen.KillTile(i, j);

                //Breaks if connected to a broken top
                Tile connectedTile = Main.tile[i + (tile.TileFrameY == 0 ? 1 : -1), j];
                if (connectedTile.TileFrameY == 56)
                    WorldGen.KillTile(i, j);
            }
            else
            {
                //If we're not a branch and there's neither crimson grass nor another bone tree trunk under, break
                if (!ValidTileToGrowOn(bottomTileType) && bottomTileType != Type)
                    WorldGen.KillTile(i, j);

                bool isBrokenTop = tile.TileFrameY == 56 || tile.TileFrameY == 166;
                bool isFoliage = tile.TileFrameY == 200;
                bool isStump = tile.TileFrameY == 116;

                if (topTileType != Type && !isBrokenTop && !isFoliage)
                {
                    if (isStump)
                    {
                        tile.TileFrameY = 166;
                        tile.TileFrameX = (short)(32 * tile.TileFrameNumber);
                    }
                    else
                    {
                        tile.TileFrameY = 56;
                        tile.TileFrameX = (short)(32 * tile.TileFrameNumber);
                    }
                }


                if (isStump && topTileType == Type && Main.tile[i, j - 1].TileFrameY == 56)
                    tile.TileFrameY = 166;
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

        public static bool ValidTileToGrowOn(int tileType) => tileType == TileID.CrimsonGrass || tileType == TileID.CrimsonJungleGrass;
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

            bool brokenTreetop = WorldGen.genRand.NextBool(13);
            bool hasBranch = !WorldGen.genRand.NextBool(5);

            //Go up the tree
            for (int j = floorY - 1; j >= floorY - height; j--)
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
                    tile.TileFrameY = 116;
                    tile.TileFrameX = (short)(32 * frameNumber);
                    continue;
                }

                //Treetop
                if (j == floorY - height)
                {
                    tile.TileFrameY = (short)(brokenTreetop ? 56 : 200);
                    tile.TileFrameX = (short)(32 * frameNumber);
                    continue;
                }

                int trunkVariant = WorldGen.genRand.Next(9);
                //Half the variants are the basic trunk
                if (trunkVariant > 3)
                {
                    tile.TileFrameX = 0;
                    tile.TileFrameY = (short)(18 * frameNumber);
                }
                //Other half are the variants
                else
                {
                    tile.TileFrameX = (short)(32 * (1 + trunkVariant));
                    tile.TileFrameY = (short)(18 * frameNumber);
                }
            }

            //1 in 5 chance to have no branches
            if (hasBranch)
            {
                int branchHeight = WorldGen.genRand.Next(5, height - 2);
                int branchSide = WorldGen.genRand.NextBool() ? 1 : -1;

                Tile branch = Main.tile[i + branchSide, floorY - branchHeight];
                branch.HasTile = true;
                branch.TileType = (ushort)TreeType;
                branch.IsHalfBlock = true; //Set to half block so the tile cracks don't render on top of it. Doesnt change anything besides that
                branch.UseBlockColors(palette);
                int frameVariantRNG = WorldGen.genRand.Next(3);
                branch.TileFrameNumber = frameVariantRNG;
                branch.TileFrameY = (short)(branchSide < 0 ? 0 : 16);
                branch.TileFrameX = (short)(160 + 16 * frameVariantRNG);


                //If the connected tile is a cracked trunk so we turn them into a regular trunk. to avoid mising outlines
                Tile connectedTile = Main.tile[i, floorY - branchHeight];
                if (connectedTile.TileFrameX >= 48)
                    connectedTile.TileFrameX = 0;
            }

            WorldGen.RangeFrame(i - 2, floorY - height - 1, i + 2, floorY);
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, i - 1, floorY - height - 2, 3, height + 2);

            if (!WorldGen.generatingWorld && WorldGen.PlayerLOS(i, floorY - height))
                new MarrowGrowFXPacket(i, floorY - 1, height, 2).Send();

            return true;
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
            float widthExtra = 8f;
            Vector2 position = new Vector2(i * 16 + Main.rand.NextFloat(-widthExtra, (16f + widthExtra * 2)), j * 16 + Main.rand.NextFloat(16f));
            Dust d = Dust.NewDustPerfect(position, BoneDustType, -Vector2.UnitY);
            d.velocity.Y *= Main.rand.NextFloat(1f, 3f);

            //Chance to flip velocity
            if (Main.rand.NextBool(3))
                d.velocity.Y *= -1;

            if (!tileBreak)
            {
                d.noGravity = true;
                d.velocity.Y *= 0.2f;
                d.scale *= 1.2f;
            }

            if (Main.rand.NextBool(3))
            {
                position = new Vector2(i * 16 + Main.rand.NextFloat(-4f, 24f), j * 16 + Main.rand.NextFloat(16f));
                d = Dust.NewDustPerfect(position, BloodDustType, -Vector2.UnitY.RotatedByRandom(0.4f) * 3f);
            }


            //Treetop
            if (Main.tile[i, j].TileFrameY == 200)
            {
                for (int y = 0; y < 2; y++)
                {
                    float angle = -(Main.rand.NextFloat(MathHelper.Pi * 0.7f) + MathHelper.Pi * 0.15f);
                    float distance = Main.rand.NextFloat(40f, 90f);
                    float direction = Main.rand.NextBool() ? 1 : -1;

                    for (int z = 0; z < 3; z++)
                    {
                        position = new Vector2(i * 16 + 8, j * 16);
                        Vector2 directionalVector = (angle + z * direction * 0.46f).ToRotationVector2();
                        position += directionalVector * distance;

                        d = Dust.NewDustPerfect(position, BloodDustType, directionalVector * Main.rand.NextFloat(0.2f, 3f));
                        d.scale = Main.rand.NextFloat(1f, 1.4f);
                        d.color = Color.White * Main.rand.NextFloat(0.7f, 1f);
                        d.color.A = 255;
                        d.velocity.Y -= 2f;
                    }
                }
            }

        }

        #region Drawing
        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];
            if (!TileDrawing.IsVisible(t))
                return;

            //Add custom draw point for the branches and treetops
            if (t.TileFrameX >= 160 || t.TileFrameY >= 200) 
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.PostDrawTiles, true);

            //Broken treetop squirts out blood
            else if (t.TileFrameY == 56 && (!Main.gamePaused && Main.instance.IsActive))
            {
                if (true)
                {
                    Vector2 dPosition = new Vector2(i * 16 + 6 + Main.rand.NextFloat(4f), j * 16 + 7);
                    Dust d = Dust.NewDustPerfect(dPosition, ModContent.DustType<MarrowBloodDust>(), -Vector2.UnitY.RotatedByRandom(0.06f));
                    d.velocity *= Main.rand.NextFloat(1f, 3f);

                    if (t.TileFrameX == 0)
                        d.velocity.X += 1.2f;
                    else if (t.TileFrameX == 32)
                    {
                        d.velocity.X -= 1.3f;
                        d.velocity.Y -= 1f;
                    }
                    else
                    {
                        d.velocity.X += 1f;
                        d.position.X += 2f;
                    }

                    d.fadeIn = 0.4f;
                    d.scale = Main.rand.NextFloat(1f, 1.2f);
                }
            }
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

            //Branch offset so we color pick the right spot
            if (t.TileFrameX >= 160)
                i += 1 + (t.TileFrameY == 0 ? 0 : -2);

            Color drawColor = Lighting.GetColor(i, j);
            if (t.IsTileFullbright)
                drawColor = Color.White;

            Vector2 camPos = Main.Camera.UnscaledPosition;
            Vector2 drawPos = new Vector2(i * 16 - (int)camPos.X + 8, j * 16 - (int)camPos.Y + 16);

            //Branch drawing
            if (t.TileFrameX >= 160)
            {
                int frameX = (t.TileFrameX - 160) / 16 * 64 + 96;
                int frameY = (t.TileFrameY / 16) * 64 + 64;

                Vector2 origin;
                if (t.TileFrameY == 0)
                    origin = new Vector2(54, 18);
                else
                    origin = new Vector2(8, 18);
                drawPos.Y -= 16;

                Main.spriteBatch.Draw(texture, drawPos, new Rectangle(frameX, frameY, 64, 64), drawColor, 0, origin, 1f, SpriteEffects.None, 0f);

                int variant = (t.TileFrameY == 0 ? 0 : 3) + (t.TileFrameX - 160) / 16;

                Vector2 bloodDropAnchor;
                switch (variant)
                {
                    case 0:
                        bloodDropAnchor = drawPos + new Vector2(-40, -6);
                        DrawBloodDrip(texture, bloodDropAnchor, drawColor, i + j);
                        break;
                    case 1:
                        bloodDropAnchor = drawPos + new Vector2(-40, 4);
                        DrawBloodDrip(texture, bloodDropAnchor, drawColor, i + j, 1f, 10f, 40f);
                        break;
                    case 2:
                        bloodDropAnchor = drawPos + new Vector2(-26, 32);
                        DrawBloodDrip(texture, bloodDropAnchor, drawColor, i + j, 1f, 10f, 40f);
                        break;
                    case 3:
                        bloodDropAnchor = drawPos + new Vector2(38, -2);
                        DrawBloodDrip(texture, bloodDropAnchor, drawColor, i + j);
                        break;
                    case 4:
                        bloodDropAnchor = drawPos + new Vector2(36, 18);
                        DrawBloodDrip(texture, bloodDropAnchor, drawColor, i + j, 1f, 10f, 40f);
                        break;
                    case 5:
                        bloodDropAnchor = drawPos + new Vector2(32, 8);
                        DrawBloodDrip(texture, bloodDropAnchor, drawColor, i + j, 1f, 10f, 40f);
                        break;
                }
            }
            //Treetop drawing
            else
            {
                int variant = t.TileFrameX / 32;
                int frameY = 188 + variant * 126;
                Main.spriteBatch.Draw(texture, drawPos, new Rectangle(100, frameY, 148, 124), drawColor, 0, new Vector2(74, 124), 1f, SpriteEffects.None, 0f);
            
                switch (variant)
                {
                    case 0:
                        DrawBloodDrip(texture, drawPos + new Vector2(-50, -4), drawColor, i + j);
                        DrawBloodDrip(texture, drawPos + new Vector2(56, -20), drawColor, i + j + 0.6f, 1.2f);
                        DrawBloodDrip(texture, drawPos + new Vector2(-36, -98), drawColor, i + j + 4.6f, 2f, 4f, 14f);
                        break;
                    case 1:
                        DrawBloodDrip(texture, drawPos + new Vector2(-46, -14), drawColor, i + j, 1f, 10, 40f);
                        DrawBloodDrip(texture, drawPos + new Vector2(-70, -60), drawColor, i + j + 1.6f, 1.6f, 5f, 30f);
                        DrawBloodDrip(texture, drawPos + new Vector2(62, -14), drawColor, i + j + 0.6f, 1.3f, 15f, 40f);
                        break;
                    case 2:
                        DrawBloodDrip(texture, drawPos + new Vector2(-66, -20), drawColor, i + j);
                        DrawBloodDrip(texture, drawPos + new Vector2(-24, -110), drawColor, i + j + 4.6f, 2f, 4f, 16f);
                        DrawBloodDrip(texture, drawPos + new Vector2(42, -8), drawColor, i + j + 2.6f, 1.75f, 10f, 36f);

                        break;
                }
            }
        }

        public void DrawBloodDrip(Texture2D texture, Vector2 drawPosition, Color drawColor, float rngOffset, float dripSpeed = 1f, float minDroop = 20, float maxDroop = 60)
        {
            //Blood droplet
            float bloodDroop = minDroop + (maxDroop - minDroop) * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.6f * dripSpeed + rngOffset) * 0.5f + 0.5f);

            Rectangle bloodstringFrame = new Rectangle(290, 94, 2, 2);
            Rectangle blooddropFrame = new Rectangle(288, 98, 6, 8);

            Main.spriteBatch.Draw(texture, drawPosition, bloodstringFrame, drawColor, 0, new Vector2(0, 0), new Vector2(1f, bloodDroop / 2f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture, drawPosition + Vector2.UnitY * bloodDroop, blooddropFrame, drawColor, 0, new Vector2(2, 0), 1f, SpriteEffects.None, 0f);
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            width = 32;
            if (tileFrameY >= 56 && tileFrameY <= 116 && tileFrameX <= 64)
            {
                height = 44;
                offsetY = -26;
                if (tileFrameY == 56) //Broken top
                    offsetY += 2;
            }
            if (tileFrameY == 166)
            {
                height = 26;
                offsetY = -8;
            }
        }
        #endregion
    }

    #region Sapling
    public class MarrowSapling : ModTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            MarrowTree.SaplingType = Type;
            FablesGeneralSystemHooks.FertilizeSaplingEvent.Add(Type, MarrowTree.GrowTree);

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
            TileObjectData.newTile.AnchorValidTiles = new[] { (int)TileID.CrimsonGrass, (int)TileID.CrimsonJungleGrass };
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

            AddMapEntry(new Color(76, 17, 13), Language.GetText("MapObject.Sapling"));

            DustType = DustID.Blood;
            AdjTiles = new int[] { TileID.Saplings };
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void RandomUpdate(int i, int j)
        {
            if (WorldGen.genRand.NextBool(5) && Main.netMode != NetmodeID.MultiplayerClient)
                MarrowTree.GrowTree(i, j, j > (int)Main.worldSurface - 1);
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
        {
            if (i % 2 == 0)
                effects = SpriteEffects.FlipHorizontally;
        }

        public override bool CanDrop(int i, int j) => false;
    }

    public class MarrowSaplingItem : ModItem
    {
        public override string Texture => AssetDirectory.VanityTrees + "PottedMarrowTree";

        public override void Load()
        {
            FablesNPC.ModifyShopEvent += AddToZoologist;
        }

        public static readonly Condition CrimsonVegetation = new("Conditions.WorldCrimson", () => (Main.bloodMoon && WorldGen.crimson) || (!WorldGen.crimson && Main.hardMode && Main.LocalPlayer.ZoneGraveyard));
        public static readonly Condition CorruptionVegetation = new("Conditions.WorldCorrupt", () => (Main.bloodMoon && !WorldGen.crimson) || (WorldGen.crimson && Main.hardMode && Main.LocalPlayer.ZoneGraveyard));

        private void AddToZoologist(NPCShop shop)
        {
            if (shop.NpcType == NPCID.BestiaryGirl)
            {
                //Add all the saplings to zoologist so they appear next to one another / in the same spot without worrying about load time
                shop.Add(ModContent.ItemType<FrostSaplingItem>(), Condition.InSnow, Condition.BestiaryFilledPercent(30));
                shop.Add(ModContent.ItemType<WisteriaSaplingItem>(), Condition.InSnow, Condition.BestiaryFilledPercent(30));

                shop.Add(Type, CrimsonVegetation, Condition.BestiaryFilledPercent(30));
                shop.Add(ModContent.ItemType<SpiderSaplingItem>(), CrimsonVegetation, Condition.BestiaryFilledPercent(30));

                shop.Add(ModContent.ItemType<InkySaplingItem>(), CorruptionVegetation, Condition.BestiaryFilledPercent(30));
                shop.Add(ModContent.ItemType<MallowSaplingItem>(), CorruptionVegetation, Condition.BestiaryFilledPercent(30));
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Marrow Willow Sapling");
            Item.ResearchUnlockCount = 25;
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<MarrowSapling>());
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }
    }
    #endregion

    [Serializable]
    public class MarrowGrowFXPacket : TreeGrowFXPacket
    {
        public MarrowGrowFXPacket(int i, int baseY, int height, byte effectCount) : base(i, baseY, height, effectCount) { }
        public override GrowEffectDelegate GrowEffect => MarrowTree.GrowthEffects;
    }

    #region Dust

    public class MarrowBloodDust : ModDust
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            MarrowTree.BloodDustType = Type;
        }

        public override void OnSpawn(Dust dust)
        {
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;

            if (!dust.noGravity)
                dust.velocity.Y += 0.15f;
            else
            {
                dust.scale *= 0.98f;
            }

            dust.rotation += 0.01f;
            dust.scale *= 0.97f;

            dust.alpha = (int)(dust.fadeIn * 255f);
            if (dust.fadeIn > 0)
            {
                dust.fadeIn -= 0.1f;
                if (dust.fadeIn < 0)
                    dust.fadeIn = 0f;
            }

            if (dust.scale <= 0.3)
                dust.active = false;

            return false;
        }
    }

    public class MarrowBoneDust : ModDust
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            MarrowTree.BoneDustType = Type;
        }

        public override void OnSpawn(Dust dust)
        {
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            if (!dust.noGravity)
                dust.velocity.Y += 0.15f;
            else
                dust.scale *= 0.95f;

            dust.rotation += 0.1f;
            dust.scale *= 0.98f;

            if (dust.velocity.Y > 2f)
            {
                dust.velocity.Y = 2f;
                dust.scale *= 0.96f;
            }

            if (dust.scale <= 0.2)
                dust.active = false;

            return false;
        }
    }

    #endregion
}