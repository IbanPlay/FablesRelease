
﻿using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
﻿using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Helpers;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class WulfrumBigConduitExhaustVertical : WulfrumBigConduitCouplingVertical, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Iron;
            Main.tileSolid[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.CanBeSloped[Type] = false;
            FablesSets.ConduitTiles3xVertical[Type] = true;
            TileID.Sets.DontDrawTileSliced[Type] = true; //Necessary or else the extra with thingy breaks

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.CoordinateWidth = 20;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = default;

            //Anchored to the top
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidBottom, TileObjectData.newTile.Width, 0);
            TileObjectData.addAlternate(0);

            //Anchored to the floor
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.Platform, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(131, 128, 122));
            FablesPlayer.GetPlacementOverlapCheckDimensionsEvent.Add(Type, OverlapCheckDimensions);


            RegisterItemDrop(ModContent.ItemType<WulfrumBigConduitExhaustItem>());
        }
        

        //Vertical top facing exhaust makes puffs of smoke
        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (!closer || Main.dedServ)
                return;

            Tile t = Main.tile[i, j];
            if (t.TileFrameY == 18)
            {
                Tile connectionTile = Main.tile[i, j + 1];
                //Exhausts only produce smoke when connected to a conduit
                if (!connectionTile.HasTile || connectionTile.TileType != ModContent.TileType<WulfrumBigConduit>())
                    return;

                Tile exhaustTileSide = Main.tile[i, j - 1];
                if (exhaustTileSide.HasTile && Main.tileSolid[exhaustTileSide.TileType])
                    return;

                Vector2 smokePos = new Vector2(i, j - 1) * 16f + Vector2.One * 8;

                if (Main.rand.NextBool(10))
                {
                    Vector2 dustPosition = smokePos + Vector2.UnitY * 7 + Vector2.UnitX * Main.rand.NextFloat(-8f, 8f);
                    Dust must = Dust.NewDustPerfect(dustPosition, DustID.Smoke, -Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.4f), Scale: Main.rand.NextFloat(0.7f, 2.3f));
                    must.rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    must.noGravity = true;
                }

                if (Main.rand.NextBool(4))
                {
                    Vector2 smokePosition = new Vector2(i, j - 1) * 16 + Vector2.One * 8 + Vector2.UnitX * Main.rand.NextFloat(-8f, 8f);
                    smokePosition.Y += 4f;
                    Vector2 smokeVelocity = Vector2.Zero;
                    if (Main.WindForVisuals < 0f)
                        smokeVelocity.X = 0f - Main.WindForVisuals;

                    smokeVelocity.Y -= Main.rand.NextFloat(1.5f, 3f);

                    int[] goreIDs = new int[] { GoreID.ChimneySmoke1, GoreID.ChimneySmoke2, GoreID.ChimneySmoke3 };
                    int smokeType = Main.rand.Next(goreIDs);

                    if (Main.rand.NextBool(4))
                        Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.45f + 0.45f);
                    else if (Main.rand.NextBool(2))
                        Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.55f + 0.55f);
                    else
                        Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.6f + 0.7f);
                }
            }
            else if (t.TileFrameX == 0 && t.TileFrameY == 36)
            {
                for (int x = i; x < i + 3; x++)
                {
                    Tile t2 = Main.tile[i, j - 1];

                    //Exhausts only produce water when connected to a conduit
                    if (!t2.HasTile || t2.TileType != ModContent.TileType<WulfrumBigConduit>())
                        return;

                    Tile t3 = Main.tile[i, j + 1];
                    if (t3.HasTile && Main.tileSolid[t3.TileType])
                        return;
                }

                Main.SceneMetrics.ActiveFountainColor = ModContent.GetInstance<GrimyWaterStyle>().Slot;
            }
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];

            //Add custom draw point for the pipes leading down
            if (t.TileFrameX == 0 && t.TileFrameY == 36)
            {
                for (int x = i; x < i + 3; x++)
                {
                    Tile t2 = Main.tile[x, j + 1];
                    if (t2.HasTile && Main.tileSolid[t2.TileType])
                        return;
                }

                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.BehindTiles, false);
            }
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            Vector2 camPos = Main.Camera.UnscaledPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            Vector2 drawPos = new Vector2(i * 16 - (int)camPos.X, j * 16 - (int)camPos.Y + 16);

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            Matrix renderMatrix = view * projection;
            Effect effect = Scene["SewageExhaust"].GetShader().Shader;

            int liquidTextureIndex = ModContent.GetInstance<GrimyWaterStyle>().Slot;
            if (Main.SceneMetrics.ActiveFountainColor >= 0)
                liquidTextureIndex = Main.SceneMetrics.ActiveFountainColor;

            effect.Parameters["waterTexture"].SetValue(TextureAssets.Liquid[liquidTextureIndex].Value);
            effect.Parameters["mainNoiseTexture"].SetValue(AssetDirectory.NoiseTextures.ManifoldRidges.Value);
            effect.Parameters["displaceTexture"].SetValue(AssetDirectory.NoiseTextures.DisplaceSmall.Value); 
            effect.Parameters["fadeTexture"].SetValue(AssetDirectory.NoiseTextures.DownwardsWateryCoulée.Value);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly  + i * j % 40);
            effect.Parameters["opacityMultiplier"].SetValue(0.8f);

            int[] tileHeights = new int[3];
            float[] tileHeightsFinalOffset = [16, 16, 16];

            for (int x = i; x < i + 3; x++)
            {
                int heightIndex = x - i;
                for (tileHeights[heightIndex] = 1; tileHeights[heightIndex] < 22; tileHeights[heightIndex]++)
                {
                    if (j + tileHeights[heightIndex] + 1 >= Main.maxTilesY)
                        break;

                    Tile t2 = Main.tile[x, j + tileHeights[heightIndex] + 1];

                    if (t2.HasUnactuatedTile && Main.tileSolid[t2.TileType])
                    {
                        if (t2.TopSlope)
                        {
                            tileHeightsFinalOffset[heightIndex] = 14f;
                            tileHeights[heightIndex]++;
                            FoamEffects(x, j + tileHeights[heightIndex] + 2, tileHeightsFinalOffset[heightIndex], tileHeights[heightIndex]);
                        }
                        else
                        {
                            tileHeightsFinalOffset[heightIndex] = 8f;
                            tileHeights[heightIndex]++;

                            if (t2.IsHalfBlock)
                                FoamEffects(x, j + tileHeights[heightIndex] + 2, tileHeightsFinalOffset[heightIndex], tileHeights[heightIndex]);
                            else
                                FoamEffects(x, j + tileHeights[heightIndex] + 2, 16, tileHeights[heightIndex]);
                        }
                        break;
                    }

                    if (t2.LiquidAmount > 0)
                    {
                        tileHeightsFinalOffset[heightIndex] = (1 - t2.LiquidAmount / 255f) * 16;

                        tileHeights[heightIndex]++;
                        FoamEffects(x, j + tileHeights[heightIndex] + 2, tileHeightsFinalOffset[heightIndex] + 8, tileHeights[heightIndex]);
                        break;
                    }
                }
            }

            int maxHeight = Math.Max(tileHeights[0], Math.Max(tileHeights[1], tileHeights[2]));

            int fadeHeight = maxHeight - 6;
            //Disable fading
            if (maxHeight < 20)
                fadeHeight = 30;
            else
                fadeHeight = 22 - (maxHeight - 20) * 3;

            effect.Parameters["resolution"].SetValue(new Vector2(30, maxHeight * 8));
            RenderWaterfall(effect, renderMatrix, drawPos.Y, new Point(i, j + 1), tileHeights[0], maxHeight, drawPos.X - 3, 22, 0, 11 / 30f, tileHeightsFinalOffset[0], fadeHeight);
            RenderWaterfall(effect, renderMatrix, drawPos.Y, new Point(i + 1, j + 1), tileHeights[1], maxHeight, drawPos.X + 19, 16, 11 / 30f, 19 / 30f, tileHeightsFinalOffset[1], fadeHeight);
            RenderWaterfall(effect, renderMatrix, drawPos.Y, new Point(i + 2, j + 1), tileHeights[2], maxHeight, drawPos.X + 35, 22, 19 / 30f, 1f, tileHeightsFinalOffset[2], fadeHeight);
        }

        public static int FoamDustType;

        public static void FoamEffects(int i, int j, float heightOffset, int fallHeight)
        {
            if (Main.gamePaused || !Main.instance.IsActive)
                return;

            int probability = 5;
            if (fallHeight > 18)
                probability += 6;
            if (fallHeight > 19)
                probability += 9;
            if (fallHeight > 20)
                probability += 43;

            if (!Main.rand.NextBool(probability))
                return;

            if (FoamDustType == 0)
                FoamDustType = ModContent.DustType<ExhaustPipeFoam>();

            Vector2 foamPos = new Vector2(i * 16 + Main.rand.NextFloat(16f), j * 16 - heightOffset - Main.rand.NextFloat(6f));
            Dust d = Dust.NewDustPerfect(foamPos, FoamDustType, -Vector2.UnitY * Main.rand.NextFloat(1f, 2f), 160);
            d.noGravity = true;

            d = Dust.NewDustPerfect(foamPos, FoamDustType, -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1f), 160, Scale: WorldGen.genRand.NextFloat(0.3f, 0.7f));
            d.noGravity = true;
        }

        public static void RenderWaterfall(Effect effect, Matrix worldViewProjection, float originHeight, Point tileCoords, int tileHeight, int maxTileHeight, float startX, float waterfallSectionWidth, float uvLeft, float uvRight, float finalHeightIncrement = 16, int fadeFromHeight = 10, int maxWaterfallHeight = 22)
        {
            Vector3 topLeft = new Vector3(startX, originHeight, 0) - Vector3.UnitX * 3;

            short[] indices = new short[6 + 6 * tileHeight];
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[4 + tileHeight * 2];

            Color color = Lighting.GetColor(tileCoords);
            vertices[0] = new(topLeft, color, new Vector2(uvLeft, 0));
            vertices[1] = new(topLeft + Vector3.UnitX * waterfallSectionWidth, color, new Vector2(uvRight, 0));

            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 3;
            indices[5] = 2;

            int vertexIndex = 2;
            int indexIndex = 6;

            for (int i = 1; i < tileHeight; i++)
            {
                float uvY = (i) / (float)(maxTileHeight - 1);
                Vector3 divisionLeft = topLeft + Vector3.UnitY * (i + 1) * 16;

                if (i == tileHeight - 1)
                    divisionLeft.Y -= 16 - finalHeightIncrement;

                Color segmentColor = Lighting.GetColor(new Point(tileCoords.X, tileCoords.Y + i));

                //float opacity = (1 - uvY) * fadeStrenght;
                //segmentColor.A = (byte)(255 * opacity);
                if (fadeFromHeight < maxWaterfallHeight)
                    segmentColor *= Utils.GetLerpValue(maxWaterfallHeight, fadeFromHeight, i, true);

                vertices[vertexIndex] = new(divisionLeft, segmentColor, new Vector2(uvLeft, uvY));
                vertices[vertexIndex + 1] = new(divisionLeft + Vector3.UnitX * waterfallSectionWidth, segmentColor, new Vector2(uvRight, uvY));


                if (i < tileHeight - 1)
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
            }


            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                effect.Parameters["uWorldViewProjection"].SetValue(worldViewProjection);
                effect.Parameters["fadeNoiseUvOffset"].SetValue(new Vector2(0.5f, 1f));
                effect.Parameters["fadeNoiseStretch"].SetValue(1f);
                effect.Parameters["ditherIntensity"].SetValue(0.8f);

                pass.Apply();
                RasterizerState cache = Main.instance.GraphicsDevice.RasterizerState;
                Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4 + 2 * tileHeight, indices, 0, 2 + 2 * tileHeight);
                Main.instance.GraphicsDevice.RasterizerState = cache;
            }
        }
    }

    public class WulfrumBigConduitExhaustHorizontal : WulfrumBigConduitCouplingHorizontal 
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Iron;
            Main.tileSolid[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.CanBeSloped[Type] = false;
            FablesSets.ConduitTiles3xHorizontal[Type] = true;
            TileID.Sets.DontDrawTileSliced[Type] = true; //Necessary or else the extra with thingy breaks

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 20, 20, 20 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = default;
            TileObjectData.newTile.DrawYOffset = -2;

            //Anchored to the right
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Height, 0);
            TileObjectData.addAlternate(0);

            //Anchored to the left
            TileObjectData.newTile.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Height, 0);
            TileObjectData.addTile(Type);

            ModContent.GetInstance<WulfrumBigConduitExhaustItem>().HorizontalConnectorType = Type;

            RegisterItemDrop(ModContent.ItemType<WulfrumBigConduitExhaustItem>());
            AddMapEntry(new Color(131, 128, 122));

            FablesPlayer.GetPlacementOverlapCheckDimensionsEvent.Add(Type, OverlapCheckDimensions);
        }


        //Horizontal exhaust makes puffs of smoke
        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (!closer || Main.dedServ)
                return;

            Tile t = Main.tile[i, j];
            //Nothing happens if single conduit or double connected conduit
            if (t.TileFrameX < 18 || t.TileFrameX >= 80)
                return;

            int direction = t.TileFrameX == 18 ? -1 : 1;
            Tile connectionTile = Main.tile[i - direction, j];

            //Exhausts only produce smoke when connected to a conduit
            if (!connectionTile.HasTile || connectionTile.TileType != ModContent.TileType<WulfrumBigConduit>())
                return;

            Tile exhaustTileSide = Main.tile[i + direction, j];
            if (exhaustTileSide.HasTile && Main.tileSolid[exhaustTileSide.TileType])
                return;

            Vector2 smokePos = new Vector2(i + direction, j) * 16f + Vector2.One * 8;

            if (Main.rand.NextBool(10))
            {
                Vector2 dustPosition = smokePos - Vector2.UnitX * direction * 7 + Vector2.UnitY * Main.rand.NextFloat(-8f, 8f);
                Dust must = Dust.NewDustPerfect(dustPosition, DustID.Smoke, Vector2.UnitX * direction * Main.rand.NextFloat(0.4f, 1.4f), Scale: Main.rand.NextFloat(0.7f, 2.3f));
                must.rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                must.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 smokePosition = new Vector2(i + direction, j) * 16 + Vector2.One * 8 + Vector2.UnitY * Main.rand.NextFloat(-8f, 8f);
                smokePosition.X -= direction * 4f;
                Vector2 smokeVelocity = Vector2.Zero;
                if (Main.WindForVisuals < 0f)
                    smokeVelocity.X = 0f - Main.WindForVisuals;

                smokeVelocity.X += direction * Main.rand.NextFloat(2f, 4f);

                int[] goreIDs = new int[] { GoreID.ChimneySmoke1, GoreID.ChimneySmoke2, GoreID.ChimneySmoke3 };
                int smokeType = Main.rand.Next(goreIDs);

                if (Main.rand.NextBool(4))
                    Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.45f + 0.45f);
                else if (Main.rand.NextBool(2))
                    Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.55f + 0.55f);
                else
                    Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.6f + 0.7f);
            }
        }
    }

    public class WulfrumBigConduitExhaustItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        public override void Load()
        {
            FablesPlayer.OverridePlacedTileEvent += DecideOrientation;
        }

        public int HorizontalConnectorType;

        private void DecideOrientation(Player player, Tile targetTile, Item item, ref int tileToPlace, ref int previewPlaceStyle, ref bool? overrideCanPlace)
        {
            if (tileToPlace != Item.createTile)
                return;

            int i = Player.tileTargetX;
            int j = Player.tileTargetY;
            Tile tileTop = Main.tile[i, j - 1];
            Tile tileBot = Main.tile[i, j + 1];
            Tile tileLeft = Main.tile[i - 1, j];
            Tile tileRight = Main.tile[i + 1, j];

            //Can't be horizontal if we have a solid tile below
            if (tileBot.HasTile && !TileID.Sets.BreakableWhenPlacing[tileBot.TileType])
                return;

            //Won't turn horizontal if theres a full pipe above and the space to place it 
            if ((tileTop.HasTile && FablesSets.ConduitTiles3xVertical[tileTop.TileType]) &&
                (!tileRight.HasTile || TileID.Sets.BreakableWhenPlacing[tileRight.TileType]))
            {
                Tile tileTopRight = Main.tile[i + 1, j - 1];
                if (tileTopRight.HasTile && FablesSets.ConduitTiles3xVertical[tileTop.TileType])
                    return;
            }

            //Turn horizontal if theres a pipe to the left or right
            if (tileRight.HasTile && FablesSets.ConduitTiles3xHorizontal[tileRight.TileType])
            {
                Tile tileBotRight = Main.tile[i + 1, j + 1];
                if (tileBotRight.HasTile && FablesSets.ConduitTiles3xHorizontal[tileBotRight.TileType])
                {
                    tileToPlace = HorizontalConnectorType;
                    return;
                }
            }

            if (tileLeft.HasTile && FablesSets.ConduitTiles3xHorizontal[tileLeft.TileType])
            {
                Tile tileBotLeft = Main.tile[i - 1, j + 1];
                if (tileBotLeft.HasTile && FablesSets.ConduitTiles3xHorizontal[tileBotLeft.TileType])
                {
                    tileToPlace = HorizontalConnectorType;
                    return;
                }
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Large Wulfrum Conduit Exhaust");
            Tooltip.SetDefault("Produces steam when connected to the top, left, or right sides of a conduit\n" +
                "Pours down water when connected to the bottom side of a conduit, tinting nearby water a dull green color");
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<WulfrumBigConduitExhaustVertical>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddIngredient(ModContent.ItemType<DullPlatingItem>(), 2).
                AddRecipeGroup(FablesRecipes.AnyCopperBarGroup, 5).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    public class ExhaustPipeFoam : ModDust
    {
        public override string Texture => AssetDirectory.Dust + "DirtyFoam";

        public override void OnSpawn(Dust dust)
        {
            dust.scale = Main.rand.NextFloat(1f, 3f);
        }

        public override bool Update(Dust dust)
        {
            // Update position
            dust.position += dust.velocity;
            dust.velocity += Vector2.UnitY * 0.07f;

            dust.scale *= 0.95f;
            if (dust.scale < 0.1f)
                dust.active = false;

            return false;
        }
    }
}