using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Bestiary;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    #region Tile
    public class RustyWulfrumElevatorBaseItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Derelict Wulfrum Elevator Base");
            Tooltip.SetDefault("Connects with elevator rails and elevator stations");
            Item.ResearchUnlockCount = 1;

            ItemID.Sets.ShimmerTransformToItem[Type] = ItemType<WulfrumElevatorBaseItem>();
            ItemID.Sets.ShimmerTransformToItem[ItemType<WulfrumElevatorBaseItem>()] = Type;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<RustyWulfrumElevatorBase>());
            Item.maxStack = 9999;
            Item.value = 0;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<DullPlatingItem>(20).
                AddIngredient<EnergyCore>().
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    public class RustyWulfrumElevatorBase : ModTile, ICustomLayerTile
    {
        public static Asset<Texture2D> ButtonHighlights;

        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void Load()
        {
            if (!Main.dedServ)
                ButtonHighlights = Request<Texture2D>(AssetDirectory.WulfrumScrapyard + "RustyWulfrumElevator_Highlights");
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileTable[Type] = true;
            Main.tileSolidTop[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Width = 5;
            TileObjectData.newTile.Origin = new Point16(2, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };

            //TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(pylonHook.PlacementPreviewHook_CheckIfCanPlace, 1, 0, true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(GetInstance<RustyWulfrumElevatorController>().Hook_AfterPlacement, -1, 0, false);

            //Attach from top of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 5, 0);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            //Attach from edge of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 2, 0);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);
            //Attach from edge of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 2, 0);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            TileObjectData.addTile(Type);
            //Counts as "door"
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AddMapEntry(CommonColors.WulfrumMetalDark);
            FablesSets.ForceHouseWall[Type] = true;
            RustyWulfrumElevatorRail.ElevatorBaseType = Type;
            RustyWulfrumElevatorController._elevatorBaseType = Type;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
        public override bool RightClick(int i, int j) => WulfrumElevatorController.StationInteraction(i, j);

        public override void MouseOver(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX % 36 != 0 || tile.TileFrameX >= 18 * 3)
                return;

            WulfrumElevatorController controller = WulfrumElevatorController.GetControllerAtPosition(i, j, out int floor);
            if (controller is null)
                return;

            int direction = (tile.TileFrameX - 36) / -36;
            bool canInteract = controller.CanInteractAtFloor(floor, direction);
            if (!canInteract)
                return;

            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            if (direction == 0)
                direction = floor > controller.PlatformAnchorStation ? 1 : -1;

            player.cursorItemIconID = direction == 1 ? ItemType<RustyWulfrumElevatorHoverIndicatorUp>() : ItemType<RustyWulfrumElevatorHoverIndicatorDown>();
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];
            if (!t.IsTileInvisible)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.AboveTiles, true);
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y);

            Texture2D drawTexture = TextureAssets.Tile[Type].Value;
            if (tile.TileColor != 0)
            {
                Texture2D paintedTexture = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, tile.TileColor);
                drawTexture = paintedTexture ?? drawTexture;
            }

            Color drawColor = Lighting.GetColor(i, j);
            if (tile.IsTileFullbright)
                drawColor = Color.White;

            int rectStartY = tile.TileFrameY + 38;
            if (tile.TileFrameY > 0)
                rectStartY -= 2;
            spriteBatch.Draw(drawTexture, drawOffset - Vector2.UnitY * 2, new Rectangle(tile.TileFrameX, rectStartY, 18, 18), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            DrawHoverIcons(i, j, drawOffset, spriteBatch);
        }

        public void DrawHoverIcons(int i, int j, Vector2 drawPosition, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            int frameX = tile.TileFrameX;

            //Offset necessary so that the arrow on the left side draws over the left-middle row
            if (frameX < 36)
            {
                frameX -= 18;
                drawPosition.X -= 16;
                i -= 1;
            }

            if (frameX % 36 != 0 || frameX >= 18 * 3 || tile.TileFrameY == 0)
                return;

            Vector2 mouseVect = Main.LocalPlayer.MouseWorld();
            Rectangle selectionRect = new Rectangle(i * 16, (j - tile.TileFrameY / 18) * 16, 16, 32);
            if (!selectionRect.Contains((int)mouseVect.X, (int)mouseVect.Y))
                return;

            if (!Main.LocalPlayer.InInteractionRange(i, j, TileReachCheckSettings.Simple))
                return;

            WulfrumElevatorController controller = WulfrumElevatorController.GetControllerAtPosition(i, j, out int floor);
            if (controller is null)
                return;
            int direction = (frameX - 36) / -36;
            bool canInteract = controller.CanInteractAtFloor(floor, direction);
            if (!canInteract)
                return;

            Texture2D highlight = ButtonHighlights.Value;
            drawPosition += new Vector2(8, -10);
            if (direction != 0)
            {
                drawPosition.Y += 2;
                drawPosition.X += 5 * direction;
            }

            Rectangle frame = direction == 0 ? new Rectangle(12, 0, 16, 18) : new Rectangle(0 + (direction < 0 ? 30 : 0), 0, 10, 14);
            Color color = Main.OurFavoriteColor * Lighting.GetColor(i, j).GetBrightness();

            spriteBatch.Draw(highlight, drawPosition, frame, color, 0f, new Vector2(frame.Width / 2f, 0), 1f, SpriteEffects.None, 0.0f);
        }

        public override void HitWire(int i, int j) => WulfrumElevatorController.StationWireInteraction(i, j);
    }
    #endregion

    #region TE
    public class RustyWulfrumElevatorController : WulfrumElevatorController
    {
        public static new int _elevatorBaseType;
        public static new int _elevatorStationType;
        public static new int _elevatorRailType;
        public static new int _platformType;

        public override int PlatformType => _platformType;
        public override int ElevatorBaseType => _elevatorBaseType;
        public override int ElevatorRailType => _elevatorRailType;
        public override int ElevatorStationType => _elevatorStationType;


        //Dummies use the same thing to only register player hitboxes once
        public override void Load()
        {
            //Avoid double clearboxes
        }
    }
    #endregion

    public class RustyWulfrumElevatorPlatform : WulfrumElevatorPlatform, IMovingSurface
    {
        public static readonly SoundStyle PlatformMoveSound = new(SoundDirectory.Wulfrum + "WulfrumIndustrialElevator", 6);
        public static readonly SoundStyle PlatformHurrySound = new(SoundDirectory.Wulfrum + "WulfrumIndustrialElevatorFast");
        public override SoundStyle ClickingSound => RustyWulfrumChainsItem.ShiftSound with { Identifier = "RustyWulfElevator", SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        public override int RailType => RustyWulfrumElevatorController._elevatorRailType;
        public override int StationType => RustyWulfrumElevatorController._elevatorStationType;
        public override int BaseType => RustyWulfrumElevatorController._elevatorBaseType;

        public override string Texture => AssetDirectory.WulfrumScrapyard + "RustyWulfrumElevator";

        public override void SafeStaticDefaults() => RustyWulfrumElevatorController._platformType = Type;

        public override void SafeAI()
        {
            //Less light than the default one, and flickering
            Lighting.AddLight(NPC.Center, new Vector3(0.1f, 0.6f, 0.4f) * Main.rand.NextFloat(0.8f, 1f));

            if (previousFloor != -1 && previousFloor != TravelToFloor)
                SoundEngine.PlaySound(FastTravel ? PlatformHurrySound : PlatformMoveSound, NPC.Center);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[Type].Value;
            Rectangle frame = tex.Frame(1, 3, 0, NPC.frame.Y);
            if (FastTravel && false)
                drawColor *= 0.56f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);

            spriteBatch.Draw(tex, NPC.Center - screenPos, frame, drawColor, 0, new Vector2(tex.Width / 2, 0), 1, 0, 0);
            return false;
        }
    }

    [Serializable]
    public class WulfrumElevatorPlatformPacket : Module
    {
        byte whoAmI;
        int platformIndex;

        int fromFloor;
        int toFloor;
        bool fastTravel;
        bool playArrivalSound;

        public WulfrumElevatorPlatformPacket(NPC npc, bool playArrivalSound = false)
        {
            whoAmI = (byte)Main.myPlayer;
            platformIndex = npc.whoAmI;

            fromFloor = (int)npc.ai[0];
            toFloor = (int)npc.ai[1];
            fastTravel = npc.ai[2] == 1;
            this.playArrivalSound = playArrivalSound;
        }

        protected override void Receive()
        {
            NPC platform = Main.npc[platformIndex];
            platform.ai[0] = fromFloor;
            platform.ai[1] = toFloor;
            platform.ai[2] = fastTravel ? 1 : 0;

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
            else if (playArrivalSound)
                SoundEngine.PlaySound(WulfrumAcrobaticsPack.GrabSound, platform.Center);
        }
    }

    [Serializable]
    public class SyncWulfrumElevatorStationCount : Module
    {
        int tileEntityID;
        int stations;

        public SyncWulfrumElevatorStationCount(TileEntity te, int stationsCount)
        {
            tileEntityID = te.ID;
            stations = stationsCount;
        }

        protected override void Receive()
        {
            if (!TileEntity.ByID.TryGetValue(tileEntityID, out TileEntity elevatorController) || elevatorController is not RustyWulfrumElevatorController wulfrumElevator)
                return;
            wulfrumElevator.stations = stations;
        }
    }

    #region Hover items
    internal class RustyWulfrumElevatorHoverIndicatorUp : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("");
            Tooltip.SetDefault("You shouldn't be seeing this");
            Item.ResearchUnlockCount = 0;
            ItemID.Sets.Deprecated[Type] = true;
            ItemID.Sets.ItemsThatShouldNotBeInInventory[Type] = true;
        }
    }

    internal class RustyWulfrumElevatorHoverIndicatorDown : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("");
            Tooltip.SetDefault("You shouldn't be seeing this");
            Item.ResearchUnlockCount = 0;
            ItemID.Sets.Deprecated[Type] = true;
            ItemID.Sets.ItemsThatShouldNotBeInInventory[Type] = true;
        }
    }
    #endregion
}
