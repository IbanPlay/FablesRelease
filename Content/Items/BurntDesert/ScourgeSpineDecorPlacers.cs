using CalamityFables.Content.Tiles.BurntDesert;
using Terraria.DataStructures;

namespace CalamityFables.Content.Items.BurntDesert
{
    public abstract class ScourgeSpineDecorationItem : ModItem, IMultiAnchorPlaceable
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Places down a decorative scourge fossil\n" +
                "Press Up/Down to adjust the spine curvature");

            previewDummy = new ScourgeSpineDecor();
            previewDummy.isPlacementPreview = true;
        }

        public static ScourgeSpineDecor previewDummy;



        public override string Texture => AssetDirectory.BurntDesert + Name;
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
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = null;
            Item.maxStack = 9999;
            Item.autoReuse = true;
        }

        public abstract bool HasHead { get; }

        public override bool ConsumeItem(Player player)
        {
            return player.Fables().justPlacedMultiAnchorItem;
        }

        public bool CheckForSpinePlacingConditions(Point position, out bool hangingInTheAir)
        {
            hangingInTheAir = false;
            if (!WorldGen.InWorld(position.X, position.Y))
                return false;

            Tile tile = Framing.GetTileSafely(position);
            Tile tileAbove = Framing.GetTileSafely(position - new Point(0, 1));

            bool validGroundAnchor = tile.HasTile && Main.tileSolid[tile.TileType] &&!Main.tileFrameImportant[tile.TileType] && !Main.tileSolidTop[tile.TileType];
            if (validGroundAnchor && tileAbove.HasTile && (!Main.tileFrameImportant[tileAbove.TileType] || Main.tileSolid[tileAbove.TileType]))
                validGroundAnchor = false;

            int multiAnchorPlaceCount = Main.LocalPlayer.Fables().multiAnchorPlaceCount;

            //First placement is limited to the ground
            if (multiAnchorPlaceCount == 0)
            {
                if (!validGroundAnchor)
                    return false;
            }
            else
            {
                if (tile.HasTile)
                {
                    //Can't place head tiles inside the ground
                    if (HasHead)
                        return false;
                    //If we're inside a tile and not a head, we gotta make sure its a valid ground tile
                    else if (!validGroundAnchor)
                        return false;
                }
                //Otherwise we can hang in the air
                else
                    hangingInTheAir = true;
            }

            //Reach check
            float distanceToPlayer = (new Vector2(position.X, position.Y) * 16 - Main.LocalPlayer.Center).Length() / 16f;
            if (distanceToPlayer > (multiAnchorPlaceCount == 1 ? 15 : Player.tileRangeX))
                return false;

            if (multiAnchorPlaceCount == 1)
            {
                Point currentAnchor = Main.LocalPlayer.Fables().multiAnchorPlacePoints[0];
                int distanceToOriginAnchorX = Math.Abs(position.X - currentAnchor.X);
                if ((distanceToOriginAnchorX <= 4 && !hangingInTheAir) || distanceToOriginAnchorX > 30)
                    return false;

                int distanceToOriginAnchorY = Math.Abs(position.Y - currentAnchor.Y);
                if (distanceToOriginAnchorY > (hangingInTheAir ? 15 : 10))
                    return false;
            }

            //Can't overlap TEs
            if (TileEntity.ByPosition.ContainsKey(position.ToPoint16()))
                return false;
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return true;

            Point tileTarget = player.TileTarget();
            if (!CheckForSpinePlacingConditions(tileTarget, out bool hangingInTheAir))
                return false;

            if (player.Fables().multiAnchorPlaceCount == 1)
            {
                //Extra check
                if (previewDummy != null)
                {
                    UpdatePreview(1, player.Fables().multiAnchorPlacePoints, false);
                    previewDummy.CalculateSegmentPositions();
                    if (previewDummy.segmentCount <= 1)
                        return false;
                }
            }

            if (player.MultiAnchorPlace(Item, tileTarget, out List<Point> anchors))
            {
                //Do the placement in SP , its straighfoforward
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    FablesWorld.PlaceScourgeSkeleton(anchors[0], anchors[1], HasHead, Player.FlexibleWandCycleOffset, previewDummy.randomSeed);
                    SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, player.Center);
                }
                else
                    new ScourgeSpinePlacementPacket(anchors[0], anchors[1], HasHead, Player.FlexibleWandCycleOffset, previewDummy.randomSeed, Type).Send(-1, -1, false);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt with { Volume = 0.3f, Pitch = 0.7f }, player.Center);
            }


            return true;
        }

        public void DrawPreview(int anchorCount, List<Point> existingAnchors)
        {
            //Don't b other if hovering over the same tile we have already
            Point tileTarget = Main.LocalPlayer.TileTarget();
            if (anchorCount == 1 && existingAnchors[0] == tileTarget || !existingAnchors[0].ToWorldCoordinates().WithinRange(tileTarget.ToWorldCoordinates(), 1200))
                return;

            if (previewDummy == null)
            {
                previewDummy = new ScourgeSpineDecor();
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

            if (!CheckForSpinePlacingConditions(tileTarget, out bool hangingInTheAir))
                usedColor = invalidColor;

            previewDummy.hasTail = !HasHead && hangingInTheAir;
            previewDummy.placementPreviewDrawColor = usedColor;
            previewDummy.DrawBehindTiles(Main.spriteBatch);
        }

        public void UpdatePreview(int anchorCount, List<Point> existingAnchors, bool movedTileTarget)
        {
            Point tileTarget = Main.LocalPlayer.TileTarget();
            previewDummy.spineLength = -1;
            previewDummy.hasHead = HasHead;

            //Only reset the seed if we moved targets
            if (movedTileTarget)
                previewDummy.randomSeed = WorldGen.genRand.Next(int.MaxValue - 23);
            previewDummy.ControlPoints.Clear();

            //After placing down the thing
            if (anchorCount == 0)
                return;

            previewDummy.Position = existingAnchors[0].ToPoint16();
            previewDummy.SetControlPoints(tileTarget, Player.FlexibleWandCycleOffset);
        }
    }

    [Serializable]
    public class ScourgeSpinePlacementPacket : Module
    {
        public Point positionStart;
        public Point positionEnd;
        public bool head;
        public int cycleOffset;
        public int rand;

        public int dropItem;

        public ScourgeSpinePlacementPacket(Point placeStart, Point placeEnd, bool head, int cycleOffset, int random, int dropItem)
        {
            this.positionStart = placeStart;
            this.positionEnd = placeEnd;
            this.head = head;
            this.cycleOffset = cycleOffset;
            this.rand = random;

            this.dropItem = dropItem;
        }

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                //Edgecase check in case theres already an unsynced tile entity serverside
                //If we coldnt place it, we drop the item back again
                if (TileEntity.ByPosition.ContainsKey(positionStart.ToPoint16()))
                {
                    Item.NewItem(new EntitySource_TileBreak(positionStart.X, positionStart.Y), positionStart.X * 16, positionStart.Y * 16, 32, 32, dropItem, 1, noBroadcast: true);
                    return;
                }

                if (FablesWorld.PlaceScourgeSkeleton(positionStart, positionEnd, head, cycleOffset, rand))
                {
                    ScourgeSpineDecor placedSpine = TileEntity.ByPosition[new Point16(positionStart.X, positionStart.Y)] as ScourgeSpineDecor;
                    NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, placedSpine.ID, positionStart.X, positionStart.Y);
                    Send(-1, -1, false);
                }
            }
            else
            {
                //Play sound
                SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, positionStart.ToWorldCoordinates());
            }
        }
    }

    public class ScourgeSpineHeadPlacer : ScourgeSpineDecorationItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Sea Scourge Fossil (Head)");
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Boulder, 1)
                .AddIngredient(ItemID.AntlionMandible, 2)
                .AddIngredient(ItemID.DesertFossil, 10)
                .AddTile(TileID.HeavyWorkBench)
                .Register();
        }

        public override bool HasHead => true;
    }

    public class ScourgeSpinePlacer : ScourgeSpineDecorationItem
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Sea Scourge Fossil (Spine)");
        }

        public override void AddRecipes()
        {
            CreateRecipe()
               .AddIngredient(ItemID.DesertFossil, 3)
               .AddTile(TileID.HeavyWorkBench)
               .Register();
        }

        public override bool HasHead => false;
    }
}