using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.NPCs.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    //using IDrawPixelated on the item class itself to do a call to the manager is beyond preposterous, but it works!
    //Replacing battey for convenience for players
    [ReplacingCalamity("WulfrumBattery")]
    public class WireSpoolItem : ModItem, IMultiAnchorPlaceable, IDrawPixelated 
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        public DrawhookLayer layer => DrawhookLayer.BehindTiles;

        public static readonly SoundStyle PlaceSound = new(SoundDirectory.Tiles + "WirePlace") { PitchVariance = 0.3f };
        public static readonly SoundStyle ShiftSound = new(SoundDirectory.Tiles + "WireShift", 3) { MaxInstances = 0 };
        public static readonly SoundStyle BreakSound = SoundID.DrumTamaSnare with { Pitch = 0.3f};

        public static ScrapyardWire previewDummy;
        public static ExtraPlaceableManager<ScrapyardWire> wireManager;

        public const int MAX_WIRE_CHAIN_SAG_VARIANTS = 10;

        public override void Load()
        {
            previewDummy = new ScrapyardWire();
            previewDummy.isPlacementPreview = true;

            wireManager = new ExtraPlaceableManager<ScrapyardWire>("scrapyardWires", "wire", ScrapyardWire.Deserialize, ScrapyardWire.NetDeserialize, new Vector2(600f, 600f), restartSpritebatch:false);
            FablesDrawLayers.DrawThingsAboveSolidTilesEvent += DrawChainAnchors;
            PixelatedDrawingLayer.AddToPixelQueueEvent += AddToDrawingQueue;
        }

        //Adding this by using the interface on the item itself to refer to its manager... wth. BUT IT WORKS !!!
        private IDrawPixelated AddToDrawingQueue() => this;

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            ScrapyardWire.drawAnchors = false;
            wireManager.DrawExtraPlaceables();

            var fablesPlayer = Main.LocalPlayer.Fables();
            if (fablesPlayer.multiAnchorPlaceCount == 0 || fablesPlayer.multiAnchorItem.type != Type)
                return;

            //Don't bother if hovering over the same tile we have already
            Point tileTarget = Main.LocalPlayer.TileTarget();
            if (fablesPlayer.multiAnchorPlaceCount == 1 && fablesPlayer.multiAnchorPlacePoints[0] == tileTarget || !fablesPlayer.multiAnchorPlacePoints[0].ToWorldCoordinates().WithinRange(tileTarget.ToWorldCoordinates(), 1000))
                return;

            if (previewDummy == null)
            {
                previewDummy = new ScrapyardWire();
                previewDummy.isPlacementPreview = true;
                UpdatePreview(fablesPlayer.multiAnchorPlaceCount, fablesPlayer.multiAnchorPlacePoints, true);
            }

            Color invalidColor = Color.Red * 0.45f;
            Color validColor = ScrapyardWire.DefaultWireColor * 1.5f;
            Color usedColor = validColor;

            //Just dont draw it if its too far
            float distanceToPlayer = (new Vector2(tileTarget.X, tileTarget.Y) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer > 15)
                return;

            if (!CheckForWirePlacingConditions(tileTarget))
                usedColor = invalidColor;

            previewDummy.placementPreviewDrawColor = usedColor;
            previewDummy.Draw();
        }

        public void DrawChainAnchors()
        {
            //only show anchors when the wire spool is held
            if (Main.LocalPlayer.HeldItem.type != Type)
                return;

            ScrapyardWire.drawAnchors = true; 
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            wireManager.DrawExtraPlaceables();
            Main.spriteBatch.End();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wire Spool");
            Tooltip.SetDefault("Can be placed on solid tiles and lamps\n" +
                "Press Up/Down to adjust the wire curvature\n" +
                "'What are these, some kind of Calamity Cables??'");
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

        public static bool TileValidForWire(Point pos, bool surroundedCheck = false)
        {
            if (!WorldGen.InWorld(pos.X, pos.Y, 2))
                return false;

            if (Main.netMode == NetmodeID.MultiplayerClient && Main.sectionManager != null && !Main.sectionManager.TileLoaded(pos.X, pos.Y))
                return true;

            Tile t = Main.tile[pos];
            //Can only be placed on tiles!
            if (!t.HasTile)
                return false;

            //Solid tiles are gucci
            if (Main.tileSolid[t.TileType])
                return true;

            if (surroundedCheck)
                return false;

            if (t.TileType == TileID.Lamps || t.TileType == TileID.HangingLanterns || t.TileType == TileID.Lampposts)
                return true;

            //Can be attached to lamps
            ModTile modTile = TileLoader.GetTile(t.TileType);
            if (modTile != null)
            {
                if (modTile.AdjTiles.Contains(TileID.Lamps) || modTile.AdjTiles.Contains(TileID.HangingLanterns))
                    return true;
            }

            return false;
        }

        public bool CheckForWirePlacingConditions(Point position)
        {
            if (!WorldGen.InWorld(position.X, position.Y, 5))
                return false;

            int multiAnchorPlaceCount = Main.LocalPlayer.Fables().multiAnchorPlaceCount;

            //Player reach check
            float distanceToPlayer = (new Vector2(position.X, position.Y) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer > (multiAnchorPlaceCount == 1 ? 15 : Player.tileRangeX))
                return false;

            //Trying to place a second one, do a distance check
            if (multiAnchorPlaceCount == 1)
            {
                Point currentAnchor = Main.LocalPlayer.Fables().multiAnchorPlacePoints[0];
                float wireLenght = Vector2.Distance(currentAnchor.ToWorldCoordinates(), position.ToWorldCoordinates());
                if (wireLenght < 40f || wireLenght > 500f)
                    return false;
            }

            if (!TileValidForWire(position))
                return false;

            //Cant place fully surrounded
            if (TileValidForWire(position + new Point(-1, 0), true) &&
                TileValidForWire(position + new Point(1, 0), true) &&
                TileValidForWire(position + new Point(0, -1), true) &&
                TileValidForWire(position + new Point(0, 1), true))
                return false;

            //Can't overlap chains
            if (wireManager.GetExtraPlaceable(position.X, position.Y) != null)
                return false;
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return true;

            Point tileTarget = player.TileTarget();
            if (!CheckForWirePlacingConditions(tileTarget))
                return false;

            if (player.MultiAnchorPlace(Item, tileTarget, out List<Point> anchors))
            {
                //Do the placement in SP , its straighfoforward
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    ScrapyardWire newWire = new ScrapyardWire();
                    newWire.Anchor = anchors[0];
                    newWire.EndPoint = tileTarget;
                    newWire.WireSagValue = Player.FlexibleWandCycleOffset.ModulusPositive(MAX_WIRE_CHAIN_SAG_VARIANTS);
                    wireManager.TryPlaceNewObject(newWire);

                    SoundEngine.PlaySound(PlaceSound, player.Center);
                }
                else
                    new ScrapyardWirePlacementPacket(anchors[0], anchors[1], Player.FlexibleWandCycleOffset.ModulusPositive(MAX_WIRE_CHAIN_SAG_VARIANTS), Type).Send(-1, -1, false);
            }
            //Mid placement
            else
                SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.3f, Pitch = 0.7f }, player.Center);

            return true;
        }

        public void DrawPreview(int anchorCount, List<Point> existingAnchors)
        {
            return;
        }

        public void UpdatePreview(int anchorCount, List<Point> existingAnchors, bool movedTileTarget)
        {
            Point tileTarget = Main.LocalPlayer.TileTarget();

            //After placing down the thing
            if (anchorCount == 0)
                return;

            //Don't play a sound if its gonna be invisible when drawing anyways!
            float distanceToPlayer = (new Vector2(tileTarget.X, tileTarget.Y) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer <= 15)
            {
                float pitch = Utils.GetLerpValue(400f, 80f, existingAnchors[0].ToWorldCoordinates().Distance(tileTarget.ToWorldCoordinates()), true);
                pitch -= (Player.FlexibleWandCycleOffset.ModulusPositive(MAX_WIRE_CHAIN_SAG_VARIANTS)) / (float)(MAX_WIRE_CHAIN_SAG_VARIANTS - 1f);
                SoundEngine.PlaySound(ShiftSound with { Pitch = pitch });
            }

            
            previewDummy.Anchor = existingAnchors[0];
            previewDummy.EndPoint = tileTarget;
            previewDummy.WireSagValue = Player.FlexibleWandCycleOffset.ModulusPositive(MAX_WIRE_CHAIN_SAG_VARIANTS);
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddRecipeGroup(FablesRecipes.AnyCopperBarGroup, 2).
                AddIngredient(ItemID.Chain).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    [Serializable]
    public class ScrapyardWirePlacementPacket : Module
    {
        public Point positionStart;
        public Point positionEnd;
        public int wireSagValue;
        public int dropItem;

        public ScrapyardWirePlacementPacket(Point placeStart, Point placeEnd, int chainSagValue, int dropItem)
        {
            this.positionStart = placeStart;
            this.positionEnd = placeEnd;
            this.wireSagValue = chainSagValue;
            this.dropItem = dropItem;
        }

        protected override void Receive()
        {
            ScrapyardWire newWire = new ScrapyardWire();
            newWire.Anchor = positionStart;
            newWire.EndPoint = positionEnd;
            newWire.WireSagValue = wireSagValue;

            if (Main.netMode == NetmodeID.Server)
            {
                //Edgecase check in case theres already an unsynced chain serverside
                //If we coldnt place it, we drop the item back again
                if (!WireSpoolItem.wireManager.TryPlaceNewObject(newWire))
                {
                    Item.NewItem(new EntitySource_TileBreak(positionStart.X, positionStart.Y), positionStart.X * 16, positionStart.Y * 16, 32, 32, dropItem, 1, noBroadcast: true);
                    return;
                }

                Send(-1, -1, false);
            }
            else
            {
                //Play sound
                SoundEngine.PlaySound(WireSpoolItem.PlaceSound, positionStart.ToWorldCoordinates());
                WireSpoolItem.wireManager.TryPlaceNewObject(newWire);
            }
        }
    }


    [Serializable]
    public class ScrapyardWireDestroyPacket : Module
    {
        public Point anchor;

        public ScrapyardWireDestroyPacket(Point anchor)
        {
            this.anchor = anchor;
        }

        protected override void Receive()
        {
            var wire = WireSpoolItem.wireManager.GetExtraPlaceable(anchor.X, anchor.Y);
            if (wire != null)
                WireSpoolItem.wireManager.RemovePlaceable(wire);
        }
    }

    public class ScrapyardWire : IExtraPlaceable
    {
        public string AnchorTexturePath => AssetDirectory.WulfrumScrapyard + "WireConnectionIndicator";
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
        private PrimitiveTrail trailRenderer;

        private int _wireSagValue;
        /// <summary>
        /// Value to calculate how much the wire sags down
        /// </summary>
        public int WireSagValue
        {
            get => _wireSagValue;
            set
            {
                if (_wireSagValue != value)
                    needVerletRecalculation = true;
                _wireSagValue = value;
            }
        }

        public bool isPlacementPreview = false;
        public Color placementPreviewDrawColor;
        public IEnumerable<Point> AlternateAnchors => [EndPoint];

        public bool Update()
        {
            if (AlternateAnchors != null)
            {
                foreach (Point p in AlternateAnchors)
                    if (!WireSpoolItem.TileValidForWire(p))
                        return false;
            }

            return WireSpoolItem.TileValidForWire(Anchor);
        }

        #region Drawing
        private float wireLenght;

        public void CalculateSegmentPositions()
        {
            Vector2 worldStart = Anchor.ToWorldCoordinates();
            Vector2 worldEnd = EndPoint.ToWorldCoordinates();

            if (needVerletRecalculation)
            {
                float straightLenght = worldStart.Distance(worldEnd);

                float minChainLenght = straightLenght + 6f;
                float maxChainLenght = straightLenght + 160f;

                float sagPercent = WireSagValue / (float)(RustyWulfrumChainsItem.MAX_CHAIN_SAG_VARIANTS - 1f);

                //larger chains get an exponent so theres more fine control over the low amount of sag
                sagPercent = MathF.Pow(sagPercent, 1.5f);

                wireLenght = MathHelper.Lerp(minChainLenght, maxChainLenght, sagPercent);
                trailRenderer = null;

                //Compared to the chains that actually rely on segment drawing, this is just kinda arbitrary
                float segmentCount = (int)Math.Round(wireLenght / 16f);

                chainSim.Clear();
                chainSim.Add(new VerletPoint(worldStart, true));
                for (int i = 1; i < segmentCount - 1; i++)
                    chainSim.Add(new VerletPoint(Vector2.Lerp(worldStart, worldEnd, i / (float)segmentCount) + Vector2.UnitY * (float)Math.Sin(i / (float)segmentCount) * sagPercent, false)); //Approximate curve, we will do it right afterwards
                chainSim.Add(new VerletPoint(worldEnd, true));

                remainingSimIterations = 40;
            }

            int iterations = 40;
            Vector2 gravity = Vector2.UnitY * 0.15f;

            if (remainingSimIterations < 40)
                iterations = 25;
            if (remainingSimIterations == 0)
            {
                Point centerPoint = chainSim[chainSim.Count / 2].position.ToTileCoordinates();
                Tile tileInbetween = Main.tile[centerPoint];
                iterations = WallID.Sets.AllowsWind[tileInbetween.WallType] ? 1 : 0;

                if (iterations == 1)
                {
                    gravity.X += Main.instance.TilesRenderer.GetWindCycle(centerPoint.X, centerPoint.Y, WindHelper.grassWindCounter) * 0.1f;
                }
            }

            for (int i = 0; i < iterations; i++)
                VerletPoint.SimpleSimulation(chainSim, 16, 10, gravity, remainingSimIterations == 40 ? 1f : 0.6f);

            remainingSimIterations--;
            if (remainingSimIterations < 0)
                remainingSimIterations = 0;
            needVerletRecalculation = false;
        }

        public static bool drawAnchors = false;

        public void Draw()
        {
            if (drawAnchors)
            {
                DrawChainAnchors();
                return;
            }

            //Draw the thing
            if (wireLenght == 0)
                needVerletRecalculation = true;
            CalculateSegmentPositions();

            trailRenderer ??= new PrimitiveTrail((int)(wireLenght / 10f), WireWidth, WireColor);
            trailRenderer?.SetPositions(chainSim.Select(x => x.position), FablesUtils.SmoothBezierPointRetreivalFunction);
            drawColor = DefaultWireColor;
            trailRenderer?.Render(null, -Main.screenPosition);
        }

        public static Color DefaultWireColor => new Color(136, 50, 55);
        public static Color DefaultWireShadowColor => new Color(44, 8, 27);

        public float WireWidth(float progress) => 1.05f;
        private Color drawColor;
        public Color WireColor(float progress)
        {
            if (isPlacementPreview)
                return placementPreviewDrawColor;

            int wireIndex = Math.Clamp((int)(progress * trailRenderer.maxPointCount - 1), 0, trailRenderer.maxPointCount - 1);
            Vector2 positionAlongWire = trailRenderer.Positions[wireIndex];

            return drawColor.MultiplyRGB(Lighting.GetColor(positionAlongWire.ToTileCoordinates()));
        }

        private void DrawChainAnchors()
        {
            if (AnchorTexture == null)
                AnchorTexture = ModContent.Request<Texture2D>(AnchorTexturePath);

            Texture2D tex = AnchorTexture.Value;
            Color drawColor = Color.White * CommonColors.WulfrumLightMultiplier;
            Vector2 drawPos = Anchor.ToWorldCoordinates() - Main.screenPosition;
            drawPos = new Vector2((int)drawPos.X, (int)drawPos.Y);
            Main.spriteBatch.Draw(tex, drawPos, null, drawColor, 0f, tex.Size() / 2f, 1f, SpriteEffects.None, 0);

            drawPos = EndPoint.ToWorldCoordinates() - Main.screenPosition;
            drawPos = new Vector2((int)drawPos.X, (int)drawPos.Y);
            Main.spriteBatch.Draw(tex, drawPos, null, drawColor, 0f, tex.Size() / 2f, 1f, SpriteEffects.None, 0);
        }
        #endregion

        #region Breaking behavior
        public void OnRemove()
        {
            Vector2 position = Vector2.Lerp(Anchor.ToWorldCoordinates(), EndPoint.ToWorldCoordinates(), 0.5f);

            if (Main.netMode == NetmodeID.Server)
                new ScrapyardWireDestroyPacket(Anchor).Send(-1, -1, false);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var source = new EntitySource_TileBreak(Anchor.X, Anchor.Y);
                int itemType = ModContent.ItemType<WireSpoolItem>();
                Item.NewItem(source, position, itemType);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(WireSpoolItem.BreakSound, position);
            }
        }
        #endregion

        #region Saving 
        public static ScrapyardWire Deserialize(TagCompound tag)
        {
            ScrapyardWire wire = new ScrapyardWire();

            if (tag.TryGet("anchor", out Point start))
                wire.Anchor = start;
            else
                return null;

            if (tag.TryGet("endPoint", out Point end))
                wire.EndPoint = end;
            else
                return null;

            if (tag.TryGet("sag", out int sag))
                wire.WireSagValue = sag;

            return wire;
        }

        public TagCompound Serialize()
        {
            return new TagCompound
            {
                ["anchor"] = Anchor,
                ["endPoint"] = EndPoint,
                ["sag"] = WireSagValue
            };
        }

        public void NetSerialize(BinaryWriter writer)
        {
            writer.Write(Anchor.X); writer.Write(Anchor.Y);
            writer.Write(EndPoint.X); writer.Write(EndPoint.Y);
            writer.Write(WireSagValue);
        }

        public static ScrapyardWire NetDeserialize(BinaryReader reader)
        {
            ScrapyardWire wire = new ScrapyardWire();
            wire.Anchor = new Point(reader.ReadInt32(), reader.ReadInt32());
            wire.EndPoint = new Point(reader.ReadInt32(), reader.ReadInt32());
            wire.WireSagValue = reader.ReadInt32();
            return wire;
        }
        #endregion
    }
}