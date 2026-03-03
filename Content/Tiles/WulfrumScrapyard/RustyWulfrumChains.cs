using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class RustyWulfrumChainsItem : ModItem, IMultiAnchorPlaceable
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public static RustyWulfrumChain previewDummy;
        public static ExtraPlaceableManager<RustyWulfrumChain> chainManager;

        public const int MAX_CHAIN_SAG_VARIANTS = 10;

        public static readonly SoundStyle PlaceSound = new(SoundDirectory.Tiles + "ChainPlace") { PitchVariance = 0.3f };
        public static readonly SoundStyle ShiftSound = new(SoundDirectory.Tiles + "ChainShift", 5) { MaxInstances = 0 };

        public override void Load()
        {
            previewDummy = new RustyWulfrumChain();
            previewDummy.isPlacementPreview = true;

            chainManager = new ExtraPlaceableManager<RustyWulfrumChain>("rustyWulfrumChains", "chain", RustyWulfrumChain.Deserialize, RustyWulfrumChain.NetDeserialize, new Vector2(600f, 600f));
            FablesDrawLayers.DrawThingsBehindSolidTilesAndBackgroundNPCsEvent += DrawChainPlaceables;
            FablesDrawLayers.DrawThingsAboveSolidTilesEvent += DrawChainAnchors;
        }

        public static void DrawChainPlaceables()
        {
            RustyWulfrumChain.drawAnchors = false;
            chainManager.DrawExtraPlaceables();
        }
        public static void DrawChainAnchors()
        {
            RustyWulfrumChain.drawAnchors = true;
            chainManager.DrawExtraPlaceables();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rusty Wulfrum Chains");
            Tooltip.SetDefault("Press Up/Down to adjust the chain curvature");

            if (Main.dedServ)
                return;
            for (int i = 1; i < 5; i++)
            {
                ChildSafety.SafeGore[Mod.Find<ModGore>("RustyChainGore" + i.ToString()).Type] = true;
            }
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.width = 24;
            Item.height = 24;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 0;
            Item.rare = ItemRarityID.White;
            Item.UseSound = null;
            Item.maxStack = 9999;
            Item.autoReuse = true;
        }

        public override bool ConsumeItem(Player player)
        {
            return player.Fables().justPlacedMultiAnchorItem;
        }

        public static bool TileValidForChain(Point pos)
        {
            if (!WorldGen.InWorld(pos.X, pos.Y, 2))
                return false;

            if (Main.netMode == NetmodeID.MultiplayerClient && Main.sectionManager != null && !Main.sectionManager.TileLoaded(pos.X, pos.Y))
                return true;

            Tile t = Main.tile[pos];
            //Can only be placed on solid tiles!
            if (!t.HasTile || !Main.tileSolid[t.TileType])
                return false;

            return true;
        }

        public bool CheckForChainPlacingConditions(Point position)
        {
            if (!WorldGen.InWorld(position.X, position.Y, 5))
                return false;

            int multiAnchorPlaceCount = Main.LocalPlayer.Fables().multiAnchorPlaceCount;

            //Player reach check
            float distanceToPlayer = (new Vector2(position.X, position.Y) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer > (multiAnchorPlaceCount == 1 ? 15 : Player.tileRangeX))
                return false;

            //Trying to place a second one
            if (multiAnchorPlaceCount == 1)
            {
                Point currentAnchor = Main.LocalPlayer.Fables().multiAnchorPlacePoints[0];
                int distanceToOriginAnchorX = Math.Abs(position.X - currentAnchor.X);
                int distanceToOriginAnchorY = Math.Abs(position.Y - currentAnchor.Y);

                int minDistanceX = 3;
                if (distanceToOriginAnchorX == 0)
                    minDistanceX = -1;
                else if (position.Y > currentAnchor.Y && distanceToOriginAnchorY >= 4)
                    minDistanceX = -1;

                //Too close or too far
                if (distanceToOriginAnchorX <= minDistanceX || distanceToOriginAnchorX > 30)
                    return false;

                //Too high or too low
                int maxDistanceY = 30;
                int minDistanceY = -1;

                if (distanceToOriginAnchorX == 0)
                {
                    //Straight down chains cant hang upside down
                    if (position.Y < currentAnchor.Y)
                        return false;

                    minDistanceY = 2;
                    maxDistanceY = 30;
                }

                if (distanceToOriginAnchorY > maxDistanceY || distanceToOriginAnchorY < minDistanceY)
                    return false;

                //We can put the chain right down
                if (!TileValidForChain(position) && currentAnchor.X != position.X)
                    return false;
            }
            else
            {
                //Need valid tile to attach the first chain
                if (!TileValidForChain(position))
                    return false;
            }

            //Cant place fully surrounded
            if (TileValidForChain(position + new Point(-1, 0)) &&
                TileValidForChain(position + new Point(1, 0)) &&
                TileValidForChain(position + new Point(0, -1)) &&
                TileValidForChain(position + new Point(0, 1)))
                return false;

            //Can't overlap chains
            if (chainManager.GetExtraPlaceable(position.X, position.Y) != null)
                return false;
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return true;

            Point tileTarget = player.TileTarget();
            if (!CheckForChainPlacingConditions(tileTarget))
                return false;

            if (player.Fables().multiAnchorPlaceCount == 1)
            {
                //cant place if too short somehow
                if (previewDummy != null && previewDummy.segmentCount <= 1)
                    return false;
            }

            if (player.MultiAnchorPlace(Item, tileTarget, out List<Point> anchors))
            {
                //Do the placement in SP , its straighfoforward
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    RustyWulfrumChain newChain = new RustyWulfrumChain();
                    newChain.Anchor = anchors[0];
                    newChain.EndPoint = tileTarget;
                    newChain.randomSeed = previewDummy.randomSeed;
                    newChain.ChainSagValue = Player.FlexibleWandCycleOffset.ModulusPositive(MAX_CHAIN_SAG_VARIANTS);
                    chainManager.TryPlaceNewObject(newChain);

                    SoundEngine.PlaySound(PlaceSound, player.Center);
                }
                else
                    new WulfrumChainPlacementPacket(anchors[0], anchors[1], Player.FlexibleWandCycleOffset.ModulusPositive(MAX_CHAIN_SAG_VARIANTS), previewDummy.randomSeed, Type).Send(-1, -1, false);
            }
            //Mid placement
            else
                SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.3f, Pitch = 0.7f }, player.Center);

            return true;
        }

        public void DrawPreview(int anchorCount, List<Point> existingAnchors)
        {
            //Don't bother if hovering over the same tile we have already
            Point tileTarget = Main.LocalPlayer.TileTarget();
            if (anchorCount == 1 && existingAnchors[0] == tileTarget || !existingAnchors[0].ToWorldCoordinates().WithinRange(tileTarget.ToWorldCoordinates(), 1000))
                return;

            if (previewDummy == null)
            {
                previewDummy = new RustyWulfrumChain();
                previewDummy.isPlacementPreview = true;
                UpdatePreview(anchorCount, existingAnchors, true);
            }

            Color invalidColor = Color.Red * 0.35f;
            Color validColor = Color.White * 0.5f;
            Color usedColor = validColor;

            //Just dont draw it if its too far
            float distanceToPlayer = (new Vector2(tileTarget.X, tileTarget.Y) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer > 15)
                return;

            if (!CheckForChainPlacingConditions(tileTarget))
                usedColor = invalidColor;

            previewDummy.placementPreviewDrawColor = usedColor;
            previewDummy.Draw();
        }

        public void UpdatePreview(int anchorCount, List<Point> existingAnchors, bool movedTileTarget)
        {
            Point tileTarget = Main.LocalPlayer.TileTarget();
            previewDummy.chainLenght = -1;

            //Only reset the seed if we moved targets
            if (movedTileTarget)
                previewDummy.randomSeed = WorldGen.genRand.Next(int.MaxValue - 23);

            //After placing down the thing
            if (anchorCount == 0)
                return;


            //Don't play a sound if its gonna be invisible when drawing anyways!
            float distanceToPlayer = (new Vector2(tileTarget.X, tileTarget.Y) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer <= 15)
            {
                float pitch = Utils.GetLerpValue(400f, 80f, existingAnchors[0].ToWorldCoordinates().Distance(tileTarget.ToWorldCoordinates()), true);
                pitch -= (Player.FlexibleWandCycleOffset.ModulusPositive(MAX_CHAIN_SAG_VARIANTS)) / (float)(MAX_CHAIN_SAG_VARIANTS - 1f);
                SoundEngine.PlaySound(ShiftSound with { Pitch = pitch });
            }

            
            previewDummy.Anchor = existingAnchors[0];
            previewDummy.EndPoint = tileTarget;
            previewDummy.ChainSagValue = Player.FlexibleWandCycleOffset.ModulusPositive(MAX_CHAIN_SAG_VARIANTS);
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>().
                AddIngredient(ItemID.Chain, 3).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    [Serializable]
    public class WulfrumChainPlacementPacket : Module
    {
        public Point positionStart;
        public Point positionEnd;
        public int chainSagValue;
        public int rand;
        public int dropItem;

        public WulfrumChainPlacementPacket(Point placeStart, Point placeEnd, int chainSagValue, int random, int dropItem)
        {
            this.positionStart = placeStart;
            this.positionEnd = placeEnd;
            this.chainSagValue = chainSagValue;
            this.rand = random;
            this.dropItem = dropItem;
        }

        protected override void Receive()
        {
            RustyWulfrumChain newChain = new RustyWulfrumChain();
            newChain.Anchor = positionStart;
            newChain.EndPoint = positionEnd;
            newChain.randomSeed = rand;
            newChain.ChainSagValue = chainSagValue;

            if (Main.netMode == NetmodeID.Server)
            {
                //Edgecase check in case theres already an unsynced chain serverside
                //If we coldnt place it, we drop the item back again
                if (!RustyWulfrumChainsItem.chainManager.TryPlaceNewObject(newChain))
                {
                    Item.NewItem(new EntitySource_TileBreak(positionStart.X, positionStart.Y), positionStart.X * 16, positionStart.Y * 16, 32, 32, dropItem, 1, noBroadcast: true);
                    return;
                }

                Send(-1, -1, false);
            }
            else
            {
                //Play sound
                SoundEngine.PlaySound(RustyWulfrumChainsItem.PlaceSound, positionStart.ToWorldCoordinates());
                RustyWulfrumChainsItem.chainManager.TryPlaceNewObject(newChain);
            }
        }
    }


    [Serializable]
    public class WulfrumChainDestroyPacket : Module
    {
        public Point anchor;

        public WulfrumChainDestroyPacket(Point anchor)
        {
            this.anchor = anchor;
        }

        protected override void Receive()
        {
            var chain = RustyWulfrumChainsItem.chainManager.GetExtraPlaceable(anchor.X, anchor.Y);
            if (chain != null)
                RustyWulfrumChainsItem.chainManager.RemovePlaceable(chain);
        }
    }


    public class RustyWulfrumChain : IExtraPlaceable
    {
        public string TexturePath => AssetDirectory.WulfrumScrapyard + "RustyWulfrumChains";
        public string AnchorTexturePath => AssetDirectory.WulfrumScrapyard + "RustyWulfrumChainsAnchor";
        public static Asset<Texture2D> Texture;
        public static Asset<Texture2D> AnchorTexture;

        private Point _anchor;
        public Point Anchor
        {
            get => _anchor;
            set => _anchor = value;
        }

        private Point _endPoint;
        public Point EndPoint
        {
            get => _endPoint;
            set
            {
                if (_endPoint != value)
                    needVerletRecalculation = true;
                _endPoint = value;
            }
        }

        private bool needVerletRecalculation = true;
        private List<VerletPoint> chainSim = new();
        private int remainingSimIterations = 0;

        /// <summary>
        /// Rng seed used to generate the chain variants
        /// </summary>
        public int randomSeed;

        private int _chainSagValue;
        /// <summary>
        /// Value to calculate how much the chain sags down
        /// </summary>
        public int ChainSagValue
        {
            get => _chainSagValue;
            set
            {
                if (_chainSagValue != value)
                    needVerletRecalculation = true;
                _chainSagValue = value;
            }
        }

        //Cached stuff
        public int segmentCount = -1;
        public Vector2[] chainDrawPositions;
        public int[] segmentVariants;
        public float chainLenght;

        /// <summary>
        /// Timer for a quick rattling animation when placed
        /// </summary>
        public float placementAnimationTimer = 0f;

        public bool isPlacementPreview = false;
        public Color placementPreviewDrawColor;

        public IEnumerable<Point> AlternateAnchors
        {
            get
            {
                //If the chain is hanging straight down, there's no need to check for the end point to have a tile
                if (EndPoint.X == Anchor.X)
                    return null;

                return [EndPoint];
            }
        }

        public bool Update()
        {
            if (placementAnimationTimer > 0)
                placementAnimationTimer -= 1 / 20f;

            if (AlternateAnchors != null)
            {
                foreach (Point p in AlternateAnchors)
                    if (!RustyWulfrumChainsItem.TileValidForChain(p))
                        return false;
            }

            return RustyWulfrumChainsItem.TileValidForChain(Anchor);
        }

        public void OnPlace()
        {
            placementAnimationTimer = 1f;
        }


        #region Drawing
        public void CalculateSegmentPositions()
        {
            Vector2 worldStart = Anchor.ToWorldCoordinates();
            Vector2 worldEnd = EndPoint.ToWorldCoordinates();

            //Easy peasy when straight down
            if (Anchor.X == EndPoint.X)
            {
                chainLenght = Math.Abs(worldStart.Y - worldEnd.Y);
                segmentCount = (int)Math.Round(chainLenght / 16f) + 1;

                chainDrawPositions = new Vector2[segmentCount];
                for (int i = 0; i < segmentCount; i++)
                    chainDrawPositions[i] = worldStart + Vector2.UnitY * 16 * i;
            }
            //use verlet otherwise
            else
            {
                if (needVerletRecalculation)
                {
                    float straightLenght = worldStart.Distance(worldEnd);
                    float minimumExtraDroop = 26 - Utils.GetLerpValue(120f, 300f, straightLenght, true) * 18f;

                    float minChainLenght = Math.Max(straightLenght + minimumExtraDroop, 100);
                    float maxChainLenght = Math.Min(straightLenght * 4f, straightLenght + 350f);

                    float sagPercent = ChainSagValue / (float)(RustyWulfrumChainsItem.MAX_CHAIN_SAG_VARIANTS - 1f);

                    //larger chains get an exponent so theres more fine control over the low amount of sag
                    float sagExponent = 1f + 0.5f * Utils.GetLerpValue(200f, 400, minChainLenght, true);
                    sagPercent = MathF.Pow(sagPercent, sagExponent);

                    float usedLenght = MathHelper.Lerp(minChainLenght, maxChainLenght, sagPercent);
                    chainLenght = usedLenght;
                    segmentCount = (int)Math.Round(chainLenght / 16f);

                    chainSim.Clear();
                    chainSim.Add(new VerletPoint(worldStart, true));
                    for (int i = 1; i < segmentCount - 1; i++)
                        chainSim.Add(new VerletPoint(Vector2.Lerp(worldStart, worldEnd, i / (float)segmentCount) + Vector2.UnitY * (float)Math.Sin(i / (float)segmentCount) * sagPercent, false)); //Approximate curve, we will do it right afterwards
                    chainSim.Add(new VerletPoint(worldEnd, true));

                    remainingSimIterations = 40;
                }

                int iterations = 40;
                if (remainingSimIterations < 40)
                    iterations = 25;

                for (int i = 0; i < iterations; i++)
                    VerletPoint.SimpleSimulation(chainSim, 16, 10, 0.5f, remainingSimIterations == 40 ? 1f : 0.6f);

                chainDrawPositions = new Vector2[segmentCount];
                for (int i = 0; i < segmentCount; i++)
                    chainDrawPositions[i] = chainSim[i];

                remainingSimIterations--;
            }

            //Calculate random variants
            segmentVariants = new int[segmentCount];
            UnifiedRandom spineRandom = new UnifiedRandom(randomSeed);
            for (int i = 0; i < segmentCount; i++)
                segmentVariants[i] = spineRandom.Next(3);

            needVerletRecalculation = false;
        }

        public static bool drawAnchors = false;

        public void Draw()
        {
            //Don't reculate when drawing anchors, unless we really need to because we don't have positions at all
            if (chainLenght < 0 || chainDrawPositions == null || ((needVerletRecalculation || remainingSimIterations > 0) && !drawAnchors))
                CalculateSegmentPositions();

            if (segmentCount <= 1)
                return;

            if (drawAnchors)
            {
                DrawChainAnchors();
                return;
            }

            if (Texture == null)
                Texture = ModContent.Request<Texture2D>(TexturePath);
            Texture2D tex = Texture.Value;
            int segmentsToDraw = segmentCount - 1;

            //Done in 2 passes to draw the even and odd segments
            for (int i = 1; i < segmentsToDraw; i += 2)
                DrawSegment(tex, Main.spriteBatch, i);

            for (int i = 0; i < segmentsToDraw; i += 2)
                DrawSegment(tex, Main.spriteBatch, i);
        }

        private void DrawSegment(Texture2D tex, SpriteBatch spriteBatch, int i)
        {
            int segmentVariant = segmentVariants[i];

            Rectangle frame = new Rectangle(16 * segmentVariant, 0, 14, 24);
            if (i % 2 == 0)
            {
                frame.Y += 26;
                frame.Height = 26;
            }

            //Because the chain is real dark, theres an alt lighter one that can be tinted red with more cvisibility
            if (isPlacementPreview && placementPreviewDrawColor.B == 0)
                frame.X += 48;

            Vector2 position = chainDrawPositions[i];

            if (placementAnimationTimer > 0f)
                position += Main.rand.NextVector2Circular(4f, 4f) * MathF.Pow(placementAnimationTimer, 4);

            float rotation;
            if (i == segmentCount - 1)
                rotation = (chainDrawPositions[i] - chainDrawPositions[i - 1]).ToRotation() + MathHelper.PiOver2;
            else
                rotation = (chainDrawPositions[i + 1] - chainDrawPositions[i]).ToRotation() - MathHelper.PiOver2;

            Color drawColor;
            if (!isPlacementPreview)
                drawColor = Lighting.GetColor((int)position.X / 16, (int)position.Y / 16); //Lighting of the position of the chain segment
            else
                drawColor = placementPreviewDrawColor;

            position = new Vector2((int)position.X, (int)position.Y);
            Vector2 origin = new Vector2(7, 7);
            spriteBatch.Draw(tex, position - Main.screenPosition, frame, drawColor, rotation, origin, 1f, SpriteEffects.None, 0);
        }
        
        private void DrawChainAnchors()
        {
            if (AnchorTexture == null)
                AnchorTexture = ModContent.Request<Texture2D>(AnchorTexturePath);

            Texture2D tex = AnchorTexture.Value;
            Color drawColor = Lighting.GetColor(Anchor);
            Vector2 drawPos = Anchor.ToWorldCoordinates() - Main.screenPosition;
            //Platforms have the link a bit above
            if (Main.tileSolidTop[Main.tile[Anchor].TileType])
                drawPos.Y -= 2;
            else
                drawPos.Y += 2;
            drawPos = new Vector2((int)drawPos.X, (int)drawPos.Y);

            float drawRotation = (chainDrawPositions[0] - chainDrawPositions[1]).ToRotation() + MathHelper.PiOver2;

            Main.spriteBatch.Draw(tex, drawPos, null, drawColor, drawRotation, tex.Size() / 2f, 1f, SpriteEffects.None, 0);

            //Dont draw the anchor for hanging chains
            if (EndPoint.X == Anchor.X && (!Main.tile[EndPoint].HasTile || !Main.tileSolid[Main.tile[EndPoint].TileType]))
                return;

            drawColor = Lighting.GetColor(EndPoint);
            drawPos = EndPoint.ToWorldCoordinates() - Main.screenPosition;
            if (Main.tileSolidTop[Main.tile[EndPoint].TileType])
                drawPos.Y -= 2;
            else
                drawPos.Y += 2;
            drawPos = new Vector2((int)drawPos.X, (int)drawPos.Y);
            drawRotation = (chainDrawPositions[^1] - chainDrawPositions[^2]).ToRotation() + MathHelper.PiOver2;

            Main.spriteBatch.Draw(tex, drawPos, null, drawColor, drawRotation, tex.Size() / 2f, 1f, SpriteEffects.None, 0);
        }
        #endregion

        #region Breaking behavior
        public void OnRemove()
        {
            if (chainDrawPositions == null)
                CalculateSegmentPositions();

            var source = new EntitySource_TileBreak(Anchor.X, Anchor.Y);
            Vector2 position = chainDrawPositions[chainDrawPositions.Length / 2];

            if (Main.netMode == NetmodeID.Server)
                new WulfrumChainDestroyPacket(Anchor).Send(-1, -1, false);

            if (Main.netMode != NetmodeID.MultiplayerClient && chainDrawPositions.Count() > 1)
            {
                int itemType = ModContent.ItemType<RustyWulfrumChainsItem>();
                Item.NewItem(source, position, itemType);
            }

            if (!Main.dedServ && chainDrawPositions != null)
            {
                SoundEngine.PlaySound(SoundID.Tink, position);

                //Spawn broken links
                for (int i = segmentCount - 1; i >= 0; i--)
                {
                    if (Main.rand.NextBool())
                        continue;

                    int goreCount = Main.rand.Next(2);
                    for (int j = 0; j <= goreCount; j++)
                    {
                        Vector2 goreSpeed = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-1f, -0.4f));
                        int goreID = Gore.NewGore(source, chainDrawPositions[i], goreSpeed, CalamityFables.Instance.Find<ModGore>("RustyChainGore" + Main.rand.Next(1, 5).ToString()).Type);

                        float goreRotation;
                        if (i == 0)
                            goreRotation = (chainDrawPositions[i + 1] - chainDrawPositions[i]).ToRotation();
                        else
                            goreRotation = (chainDrawPositions[i] - chainDrawPositions[i - 1]).ToRotation();

                        Main.gore[goreID].rotation = goreRotation + Main.rand.NextFloat(-1f, 1f);
                        Main.gore[goreID].velocity.X *= 0.8f;
                    }
                }
            }
        }
        #endregion

        #region Saving 
        public static RustyWulfrumChain Deserialize(TagCompound tag)
        {
            RustyWulfrumChain chain = new RustyWulfrumChain();

            if (tag.TryGet("anchor", out Point start))
                chain.Anchor = start;
            else
                return null;

            if (tag.TryGet("endPoint", out Point end))
                chain.EndPoint = end;
            else
                return null;

            if (tag.TryGet("random", out int rngSeed))
                chain.randomSeed = rngSeed;
            if (tag.TryGet("chainSag", out int sag))
                chain.ChainSagValue = sag;

            return chain;
        }

        public TagCompound Serialize()
        {
            return new TagCompound
            {
                ["anchor"] = Anchor,
                ["endPoint"] = EndPoint,
                ["random"] = randomSeed,
                ["chainSag"] = ChainSagValue
            };
        }

        public void NetSerialize(BinaryWriter writer)
        {
            writer.Write(Anchor.X); writer.Write(Anchor.Y);
            writer.Write(EndPoint.X); writer.Write(EndPoint.Y);
            writer.Write(randomSeed);
            writer.Write(ChainSagValue);
        }

        public static RustyWulfrumChain NetDeserialize(BinaryReader reader)
        {
            RustyWulfrumChain chain = new RustyWulfrumChain();
            chain.Anchor = new Point(reader.ReadInt32(), reader.ReadInt32());
            chain.EndPoint = new Point(reader.ReadInt32(), reader.ReadInt32());

            chain.randomSeed = reader.ReadInt32();
            chain.ChainSagValue = reader.ReadInt32();

            return chain;
        }
        #endregion
    }
}