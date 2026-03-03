using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;
using Terraria.ID;
using CalamityFables.Content.Tiles.BaseTypes;
using CalamityFables.Content.Boss.SeaKnightMiniboss;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class SandstonePillar : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sandstone Pillar");
            Tooltip.SetDefault("Can be hammered to add or remove cracks");
            Item.ResearchUnlockCount = 30;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<SandstonePillarTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SmoothSandstone, 3).
                AddTile(TileID.HeavyWorkBench).
                Register();
        }
    }

    public class SandstonePillarTile : ModTile, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => (paintColor > 0 && paintColor <= 12) || (paintColor > 24 && paintColor <= 28);
        public string PaintedTexturePath(int paintColor)
        {
            if (paintColor > 24)
                paintColor -= 12; //Go from 25 (black) to 13
            return AssetDirectory.BurntDesert + "SandstoneColumnPaint/SandstonePillarTile_Paint" + paintColor.ToString();
        }

        public override void Load()
        {
            FablesPlayer.TileSpecificPoundOverrideEvent.Add(Type, PoundingToUndoCracks);
        }
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public static int BrickPillarType;
        public static int BrasierType;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileID.Sets.CanBeSloped[Type] = true;


            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.StyleWrapLimit = 9;
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.RandomStyleRange = 3;
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(CheckForPillarAlignment, -1, 0, false);

            //Get anchor from top <- doesnt work (trolled). Oh well
            //TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            //TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidBottom | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
            //TileObjectData.newAlternate.AnchorAlternateTiles = [Type];
            //TileObjectData.addAlternate(0);

            //Set the default anchor afterwards
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.Platform | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.AnchorAlternateTiles = [Type, TileType<DesertBrickPillarTile>()];
            TileObjectData.addTile(Type);

            DustType = DustID.Sand;

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(192, 110, 69));

            RegisterItemDrop(ItemType<SandstonePillar>());
            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            //Braziers use small offsets to store their data, so we need to rectify the frame there
            tileFrameX = (short)((tileFrameX / 18) * 18);

            offsetY = 2;
        }


        public static int CheckForPillarAlignment(int x, int y, int type, int style, int direction, int alternate)
        {
            //Placing on a pillar needs the tile to be aligned with it
            if (y < Main.maxTilesY - 2)
            {
                Tile tileBelow = Main.tile[x, y + 1];
                if (tileBelow.HasTile && (tileBelow.TileType == type || tileBelow.TileType == BrickPillarType) && (tileBelow.TileFrameX / 18) % 3 != 1)
                    return -1;
            }

            //Needs to align below itself properly as well
            if (y > 1)
            {
                Tile tileAbove = Main.tile[x, y - 1];
                if (tileAbove.HasTile && tileAbove.TileType == type && (tileAbove.TileFrameX / 18) % 3 != 1)
                    return -1;
            }

            return 0;
        }

        #region Brazier activities
        public enum BrasierPlacementState
        {
            NotCenter,
            FreeCenter,
            FreeCenterAboveBrasier,
            Brasier,
            SpectralBrasier
        }

        public static bool HasBrazier(int i, int j)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX % 54;
            return frameX >= 19 && frameX <= 22;
        }

        public static bool HasBrazier(int i, int j, out BrasierPlacementState state)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX % 54;

            state = BrasierPlacementState.NotCenter;
            if (frameX < 18 || frameX > 36)
                return false;

            if (frameX == 18)
            {
                state = BrasierPlacementState.FreeCenter;

                Tile tileBelow = Main.tile[i, j + 1];
                frameX = tileBelow.TileFrameX % 54;
                if (tileBelow.TileType == t.TileType && tileBelow.HasTile && frameX > 18 && frameX <= 20)
                {
                    state = BrasierPlacementState.FreeCenterAboveBrasier;
                }

                return false;
            }

            if (frameX == 19 || frameX == 21)
                state = BrasierPlacementState.Brasier;
            else
                state = BrasierPlacementState.SpectralBrasier;
            return true;
        }

        public static void BreakBrazier(int i, int j)
        {
            Tile t = Main.tile[i, j];

            bool spectralBrazier = (t.TileFrameX % 54) == 20 || (t.TileFrameX % 54) == 22;
            //Set it back to aligned coordinates (aka brazierless)
            t.TileFrameX = (short)((t.TileFrameX / 18) * 18);

            int itemID = Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, spectralBrazier ? ItemType<DesertBrazierSpectral>() : ItemType<DesertBrazier>());
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemID, 1f);
        }

        public static bool CanPlaceBrasier(int i, int j)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX % 54;
            if (frameX != 18 || (t.TileFrameY >= 108 && t.TileFrameX < 54))
                return false;

            Tile tileAbove = Main.tile[i, j - 1];
            frameX = tileAbove.TileFrameX % 54;
            if (tileAbove.TileType != t.TileType || !tileAbove.HasTile || frameX != 18)
                return false;

            Tile tileBelow = Main.tile[i, j + 1];
            frameX = tileBelow.TileFrameX % 54;
            //Cant place too close above another brazier
            if (tileBelow.TileType == t.TileType && tileAbove.HasTile && frameX > 18 && frameX <= 22)
                return false;

            return true;
        }


        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            int frameX = Main.tile[i, j].TileFrameX % 54;
            if (frameX == 19)
            {
                r = 1f;
                g = 0.95f;
                b = 0.65f;

                if (SirNautilus.SignathionVisualInfluence < 1)
                {
                    Color color = CommonColors.DesertMirageBlue;
                    r = MathHelper.Lerp(r, color.R / 255f, SirNautilus.TorchLightColorShift);
                    g = MathHelper.Lerp(g, color.G / 255f, SirNautilus.TorchLightColorShift);
                    b = MathHelper.Lerp(b, color.B / 255f, SirNautilus.TorchLightColorShift);
                }
            }
            else if (frameX == 20)
            {
                Color color = CommonColors.DesertMirageBlue;
                r = color.R / 255f;
                g = color.G / 255f;
                b = color.B / 255f;
            }
        }

        public override void HitWire(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            bool hasBrazier = HasBrazier(i, j);
            if (!hasBrazier)
            {
                j += 1;
                hasBrazier = HasBrazier(i, j);
                tile = Main.tile[i, j];
            }
            if (!hasBrazier)
                return;

            int frameX = tile.TileFrameX % 54;
            int frameOffset = (frameX == 21 || frameX == 22) ? -2 : 2;

            tile.TileFrameX += (short)frameOffset;
            Wiring.SkipWire(i, j);
            Wiring.SkipWire(i, j - 1);

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendTileSquare(-1, i, j - 1, 1, 2);
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.gamePaused || !Main.instance.IsActive || Lighting.UpdateEveryFrame && !Main.rand.NextBool(4))
                return;
            Tile tile = Main.tile[i, j];
            if (!TileDrawing.IsVisible(tile))
                return;

            if (!Main.rand.NextBool(20) || !HasBrazier(i, j) || (tile.TileFrameX % 54) > 20)
                return;

            bool ghostly = (tile.TileFrameX % 54) == 20 || SirNautilus.SignathionVisualInfluence == 0;

            if (!ghostly)
            {
                var dust = Dust.NewDustDirect(new Vector2(i * 16 - 4, j * 16 - 10), 12, 4, DustID.Torch, 0f, 0f, 100, default, 1f);
                dust.noGravity = !Main.rand.NextBool(3);
                dust.velocity *= 0.3f;
                dust.velocity.Y = dust.velocity.Y - 1.5f;
            }
            else
            {
                Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(new Rectangle(i * 16 - 4, j * 16 - 16, 24, 16)),
                    DustType<SpectralWaterDustEmbers>(), -Vector2.UnitY, 15, Color.White, Main.rand.NextFloat(0.6f, 1.3f));
            }
        }
        #endregion

        #region Connection checks
        public bool ValidTopTileForPillar(int i, int j, out bool connectedToSelf)
        {
            connectedToSelf = false;
            //Failsave
            if (j < 0)
                return true;

            Tile tileTop = Main.tile[i, j];
            if (!tileTop.HasTile ||
                !Main.tileSolid[tileTop.TileType] ||
                Main.tileSolidTop[tileTop.TileType] ||
                TileID.Sets.NotReallySolid[tileTop.TileType] ||
                (tileTop.Slope != SlopeType.Solid && !tileTop.BottomSlope))
            {
                connectedToSelf = tileTop.TileType == Type;
                return tileTop.TileType == Type;
            }

            return true;
        }

        public bool ValidBotTileForPillar(int i, int j)
        {
            //Failsave
            if (j >= Main.maxTilesY - 1)
                return true;

            Tile tileBottom = Main.tile[i, j];
            if (!tileBottom.HasTile ||
                !Main.tileSolid[tileBottom.TileType] ||
                Main.tileSolidTop[tileBottom.TileType] ||
                TileID.Sets.NotReallySolid[tileBottom.TileType] ||
                (tileBottom.Slope != SlopeType.Solid && !tileBottom.TopSlope))
            {
                return tileBottom.TileType == Type || tileBottom.TileType == BrickPillarType;
            }

            return true;
        }
        #endregion


        private bool PoundingToUndoCracks(Player player, Item item, int tileHitType, ref bool hitWall, int x, int y)
        {
            if (item.hammer > 0 && player.toolTime == 0 && tileHitType == Type && player.poundRelease)
            {
                hitWall = false;

                player.ApplyItemTime(item);
                player.poundRelease = false;
                SoundEngine.PlaySound(SoundID.Dig, new Vector2(x, y) * 16);
                WorldGen.KillTile(x, y, fail: true, effectOnly: true);

                Tile t = Main.tile[x, y];
                //Third column has the 2 tall cracks
                bool hasDoubleHeightCrack = t.TileFrameX >= 108;

                int i = x - (t.TileFrameX % 54) / 18;
                int j = y;

                if (hasDoubleHeightCrack)
                {
                    bool doubleCrackTop = t.TileFrameY / 18 % 2 == 0;
                    j = y - (doubleCrackTop ? 0 : 1);

                    int frameY = WorldGen.genRand.Next(3);
                    for (int py = j; py < j + 2; py++)
                    {
                        for (int px = i; px < i + 3; px++)
                        {
                            Tile pillarTile = Main.tile[px, py];
                            pillarTile.TileFrameY = (short)(frameY * 18);
                            pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54);
                        }
                        frameY = WorldGen.genRand.Next(3);
                    }
                }
                //Single width crack to undo
                else if (t.TileFrameX >= 54)
                {
                    int frameY = WorldGen.genRand.Next(3);
                    for (int px = i; px < i + 3; px++)
                    {
                        Tile pillarTile = Main.tile[px, j];
                        pillarTile.TileFrameY = (short)(frameY * 18);
                        pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54);
                    }
                }
                //place cracks
                else
                {
                    PlaceCracksOnTile(i, j, i);
                }



                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendTileSquare(-1, i, j, 3, hasDoubleHeightCrack ? 2 : 1);

                return true;
            }
            return false;
        }


        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile t = Main.tile[i, j];
            int centerX = i;
            centerX -= (t.TileFrameX / 18 % 3) - 1;

            bool breakTop = false;
            bool breakBottom = false;

            for (int x = centerX - 1; x < centerX + 2; x++)
            {
                if (!ValidTopTileForPillar(x, j - 1, out _))
                {
                    breakTop = true;
                    break;
                }

                if (!ValidBotTileForPillar(x, j + 1))
                {
                    breakBottom = true;
                }
            }

            int newFrameY = -1;
            bool hasBrokenTop = (t.TileFrameX < 54 && t.TileFrameY >= 54 && t.TileFrameY < 126);
            bool hasBrokenBottom = (t.TileFrameX < 54 && t.TileFrameY >= 126);

            //Third column has the 2 tall cracks
            bool hasDoubleHeightCrack = t.TileFrameX >= 108;
            bool doubleCrackTop = t.TileFrameY / 18 % 2 == 0;

            //Has a broken top already - fix it
            if (hasBrokenTop && !breakTop)
            {
                newFrameY = WorldGen.genRand.Next(3);
                hasBrokenTop = false;
            }

            //Has a broken bottom already - fix it
            if (hasBrokenBottom && !breakBottom)
            {
                newFrameY = WorldGen.genRand.Next(3);
                hasBrokenBottom = false;
            }

            //Doesnt have a broken top
            if (!hasBrokenTop && breakTop)
            {
                newFrameY = 3 + WorldGen.genRand.Next(3);
                hasBrokenTop = true;
            }
            else if (!hasBrokenBottom && breakBottom)
            {
                newFrameY = 6 + WorldGen.genRand.Next(3);
                hasBrokenBottom = true;
            }

            //Update the row if we had to change it
            if (newFrameY != -1)
            {
                int pillarLeft = i - (t.TileFrameX % 54) / 18;
                for (int p = pillarLeft; p < pillarLeft + 3; p++)
                {
                    Tile pillarTile = Main.tile[p, j];
                    pillarTile.TileFrameY = (short)(newFrameY * 18);
                    pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54);
                }
            }

            //cant have brazier on broken bottom or broken top
            if (hasBrokenBottom && HasBrazier(centerX, j))
                BreakBrazier(centerX, j);
            else if (hasBrokenTop && HasBrazier(centerX, j + 1))
                BreakBrazier(centerX, j + 1);
            
            //Undo adjacent 2x height cracks, reset them to crackless frames
            if (hasDoubleHeightCrack && ((hasBrokenTop && doubleCrackTop) || (hasBrokenBottom && !doubleCrackTop)))
            {
                int pillarLeft = i - (t.TileFrameX % 54) / 18;

                for (int p = pillarLeft; p < pillarLeft + 3; p++)
                {
                    Tile pillarTile = breakTop ? Main.tile[p, j + 1] : Main.tile[p, j - 1];
                    pillarTile.TileFrameY = (short)(newFrameY * 18);
                    pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54);
                }
            }

            if (hasBrokenTop || hasBrokenBottom)
                return;

            //This is to avoid cases where it'll reframe itself simply by placing a block to the side
            Tile tileTwoAbove = Main.tile[i, j - 2];
            if (tileTwoAbove.HasTile && tileTwoAbove.TileType == Type)
                return;

            //Cracks
            if (t.TileFrameX % 54 > 0 || !WorldGen.genRand.NextBool(3) || WorldGen.generatingWorld)
                return;

            PlaceCracksOnTile(i, j, i - (t.TileFrameX % 54) / 18);
        }

        public void RemoveCracksOnTile(int x, int y)
        {
            Tile t = Main.tile[x, y];
            //Third column has the 2 tall cracks
            bool hasDoubleHeightCrack = t.TileFrameX >= 108;

            int i = x - (t.TileFrameX % 54) / 18;
            int j = y;

            if (hasDoubleHeightCrack)
            {
                bool doubleCrackTop = t.TileFrameY / 18 % 2 == 0;
                j = y - (doubleCrackTop ? 0 : 1);

                int frameY = WorldGen.genRand.Next(3);
                for (int py = j; py < j + 2; py++)
                {
                    for (int px = i; px < i + 3; px++)
                    {
                        Tile pillarTile = Main.tile[px, py];
                        pillarTile.TileFrameY = (short)(frameY * 18);
                        pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54);
                    }
                    frameY = WorldGen.genRand.Next(3);
                }
            }

            //Single width crack to undo
            else if (t.TileFrameX >= 54)
            {
                int frameY = WorldGen.genRand.Next(3);
                for (int px = i; px < i + 3; px++)
                {
                    Tile pillarTile = Main.tile[px, j];
                    pillarTile.TileFrameY = (short)(frameY * 18);
                    pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54);
                }
            }
        }

        public void PlaceCracksOnTile(int i, int j, int pillarLeft)
        {
            //Check for crackless bottom tile
            Tile botTile = Main.tile[i, j + 1];
            bool canDoTwoTallCracks = botTile.TileType == Type && botTile.TileFrameX < 54;

            Tile topTile = Main.tile[i, j - 1];
            bool cracksAbove = topTile.TileType == Type && topTile.TileFrameX >= 54;
            if (cracksAbove || (botTile.TileType == Type && !canDoTwoTallCracks))
                return;

            //1 tall cracks
            if (!canDoTwoTallCracks || !WorldGen.genRand.NextBool(3))
            {
                int crackVariant = WorldGen.genRand.Next(2);

                for (int p = pillarLeft; p < pillarLeft + 3; p++)
                {
                    Tile pillarTile = Main.tile[p, j];
                    pillarTile.TileFrameY = (short)(crackVariant * 18);
                    pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54 + 54);
                }
            }
            //2 tall cracks
            else if (canDoTwoTallCracks)
            {
                int crackVariant = WorldGen.genRand.Next(6) * 2;

                int frameY = 0;

                for (int py = j; py < j + 2; py++)
                {
                    for (int px = pillarLeft; px < pillarLeft + 3; px++)
                    {
                        Tile pillarTile = Main.tile[px, py];
                        pillarTile.TileFrameY = (short)((crackVariant + frameY) * 18);
                        pillarTile.TileFrameX = (short)(pillarTile.TileFrameX % 54 + 108);
                    }
                    frameY++;
                }
            }
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (noItem)
                return;

            if (fail && HasBrazier(i, j))
                BreakBrazier(i, j);

            if (!effectOnly && !fail && HasBrazier(i, j))
            {
                BreakBrazier(i, j);

                //Copied from TileLoader.checktile

                Tile t = Main.tile[i, j];
                TileObjectData tileData = TileObjectData.GetTileData(Type, 0, 0);
                int partFrameX = t.TileFrameX % 54;
                i -= partFrameX / 18;
                int originX = i + tileData.Origin.X;
                int originY = j + tileData.Origin.Y;
                bool partiallyDestroyed = false;
                for (int x = i; x < i + 3; x++)
                {
                    if (!Main.tile[x, j].HasTile || Main.tile[x, j].TileType != Type)
                    {
                        partiallyDestroyed = true;
                        break;
                    }
                }

                //If the tile doesnt have framing issues, keep it as is and just break the brazier
                if (!partiallyDestroyed && TileObject.CanPlace(originX, originY, Type, 0, 0, out _, onlyCheck: true, checkStay: true))
                {
                    fail = true;
                }
                TileObject.objectPreview.Active = false;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (j == 0 || j == Main.maxTilesY - 1)
                return;
            Tile tile = Main.tile[i, j];
            if (!TileDrawing.IsVisible(tile))
                return;

            Vector2 drawOffset = FablesUtils.TileDrawOffset();
            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (tile.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, tile.TileColor);
                texture = paintedTex ?? texture;
            }

            Tile tileBelow = Main.tile[i, j + 1];
            if (tileBelow.HasTile && tileBelow.TileType == BrickPillarType)
            {

                Color drawColorBelow = Lighting.GetColor(i, j + 1);
                if (tile.IsTileFullbright)
                    drawColorBelow = Color.White;


                int rimFrameY = (j * 18) % 3;
                int rimFrameX = ((tile.TileFrameX / 18) % 3) * 18;

                spriteBatch.Draw(texture, new Vector2(i * 16, j * 16 + 18) - drawOffset, new Rectangle(rimFrameX, rimFrameY, 16, 16), drawColorBelow, 0f, default, 1f, 0, 0f);
            }

            //If we're not a broken top frame and yet there's no pillar continuation above this, that means the pillar is touching a ceiling, so we need to put a cap on it
            if ((tile.TileFrameX > 54 || tile.TileFrameY < 54 || tile.TileFrameY >= 126) && Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].TileType != Type)
            {
                Color drawColor = Lighting.GetColor(i, j);
                if (tile.IsTileFullbright)
                    drawColor = Color.White;

                int topFrameX = ((tile.TileFrameX / 18) % 3) * 18;
                spriteBatch.Draw(texture, new Vector2(i * 16, j * 16 - 2) - drawOffset, new Rectangle(topFrameX, 162, 16, 16), drawColor, 0f, default, 1f, 0, 0f);

            }

            //Draw brazier
            int frameX = tile.TileFrameX % 54;
            //We draw the brazier on the tile to its left because of draw order matters
            if (frameX == 36 && HasBrazier(i - 1, j))
            {
                texture = TextureAssets.Tile[BrasierType].Value;
                if (Main.tile[i - 1, j].TileColor != PaintID.None)
                {
                    Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(BrasierType, 0, Main.tile[i - 1, j].TileColor);
                    texture = paintedTex ?? texture;
                }

                frameX = Main.tile[i - 1, j].TileFrameX % 54;

                int brazierFrameX;
                int brazierFrameY = 0;

                //Turned off
                if (frameX >= 21)
                    brazierFrameX = 216;
                else
                    brazierFrameX = Main.tileFrame[BrasierType] * 54;

                if (frameX == 20 || frameX == 22 || SirNautilus.SignathionVisualInfluence == 0)
                    brazierFrameY += 54;

                for (int bx = 0; bx < 3; bx++)
                    for (int by = 0; by < 3; by++)
                    {
                        int brazierX = i - 2 + bx;
                        int brazierY = j - 2 + by;
                        Color drawColor = Lighting.GetColor(brazierX, brazierY);
                        if (tile.IsTileFullbright)
                            drawColor = Color.White;

                        spriteBatch.Draw(texture, new Vector2(brazierX * 16, brazierY * 16 + 2) - drawOffset, new Rectangle(brazierFrameX + bx * 18, brazierFrameY + by * 18, 16, 16), drawColor, 0f, default, 1f, 0, 0f);
                        
                        //little shadow
                        if (bx == 1 && by == 2)
                            spriteBatch.Draw(texture, new Vector2(brazierX * 16, brazierY * 16 + 16) - drawOffset, new Rectangle(18, 108, 16, 16), drawColor, 0f, default, 1f, 0, 0f);


                        DesertBrazierTile.FlameTex ??= Request<Texture2D>(AssetDirectory.BurntDesert + "DesertBrazierTileFlame");
                        spriteBatch.Draw(DesertBrazierTile.FlameTex.Value, new Vector2(brazierX * 16, brazierY * 16 + 2) - drawOffset
                            , new Rectangle(brazierFrameX + bx * 18, brazierFrameY + by * 18, 16, 16), Color.White with { A = 0 } * 0.3f, 0f, default, 1f, 0, 0f);
                    }
            }
        }


        public override void MouseOver(int i, int j)
        {
            HasBrazier(i, j, out BrasierPlacementState state);
            if (state == BrasierPlacementState.FreeCenterAboveBrasier)
            {
                HasBrazier(i, j + 1, out state);
            }

            if (state == BrasierPlacementState.Brasier)
                SpectralWater.MouseOverIcon(i, j);
        }
    }
}
