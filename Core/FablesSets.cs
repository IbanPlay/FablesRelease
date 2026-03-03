using CalamityFables.Core.DrawLayers;

namespace CalamityFables.Core
{
    public static class FablesSets
    {
        public static SetFactory ItemFactory = ItemID.Sets.Factory;//, "FablesItemSets");
        public static SetFactory NPCFactory = NPCID.Sets.Factory;//, "FablesNPCSets");
        public static SetFactory TilesFactory = TileID.Sets.Factory;//, "FablesTileSets");
        public static SetFactory WallFactory = WallID.Sets.Factory;//, "FablesWallSets");
        public static SetFactory ProjectileFactory = ProjectileID.Sets.Factory;//, "FablesWallSets");

        public static bool[] PlacesLikeGrassSeeds = TilesFactory.CreateBoolSet(false);
        public static bool[] CustomPaintedSprites = TilesFactory.CreateBoolSet(false);
        public static bool[] SwayingBanners = TilesFactory.CreateBoolSet(false);
        public static bool[] CustomContainer = TilesFactory.CreateBoolSet(false);
        public static bool[] WallMountedContainer = TilesFactory.CreateBoolSet(false);
        public static bool[] CustomDoor = TilesFactory.CreateBoolSet(false);
        public static bool[] ForceHouseWall = TilesFactory.CreateBoolSet(false);
        public static bool[] CustomPlaceSound = TilesFactory.CreateBoolSet(false);
        public static bool[] CustomFramingPostProcessing = TilesFactory.CreateBoolSet(false);
        public static bool[] CustomDrawOffset = TilesFactory.CreateBoolSet(false);
        public static bool[] AlwaysPreventTileBreakIfOnTopOfIt = TilesFactory.CreateBoolSet(false);
        public static bool[] ConduitTiles2xVertical = TilesFactory.CreateBoolSet(false);
        public static bool[] ConduitTiles2xHorizontal = TilesFactory.CreateBoolSet(false);
        public static bool[] ConduitTiles3xVertical = TilesFactory.CreateBoolSet(false);
        public static bool[] ConduitTiles3xHorizontal = TilesFactory.CreateBoolSet(false);
        public static bool[] TileCutIgnoreEverythingButThorns = TilesFactory.CreateBoolSet(true);

        /// <summary>
        /// Liquids are drawn behind this tile always, and players can place liquids inside. Use <see cref="FablesTile.MakeWaterIgnoreTilesEvent"/> together with it
        /// </summary>
        public static bool[] ActsAsAGrate = TilesFactory.CreateBoolSet(false);

        public static bool[] CustomPaintedWalls = WallFactory.CreateBoolSet(false);
        public static bool[] IndividuallyAnimatedWall = WallFactory.CreateBoolSet(false);

        public static bool[] IsElongatedTail = ItemFactory.CreateBoolSet(false);
        public static bool[] IsGogglesVanity = ItemFactory.CreateBoolSet(false);
        public static bool[] HasCustomHeldDrawing = ItemFactory.CreateBoolSet(false);
        public static bool[] WizardSetRobe = ItemFactory.CreateBoolSet(ItemID.AmethystRobe, ItemID.TopazRobe, ItemID.SapphireRobe, ItemID.EmeraldRobe, ItemID.AmberRobe, ItemID.RubyRobe, ItemID.DiamondRobe, ItemID.GypsyRobe);

        /// <summary>
        /// Temporary minions. Minions within this set cannot be targeted by Pontiff's Piper.
        /// </summary>
        public static bool[] SubMinion = ProjectileFactory.CreateBoolSet(false);
        public static bool[] WulfrumProjectiles = ProjectileFactory.CreateBoolSet(false);

        public static bool[] WulrumNPCs = NPCFactory.CreateBoolSet(false);
        public static bool[] NoJourneyStengthScaling = NPCFactory.CreateBoolSet(false);

        internal static readonly Dictionary<EquipType, Dictionary<int, ILongBackAccessory>> elongatedTailEquips = new();

        public static class ForAdvancedCollision
        {
            //Same as sandshark 
            public static bool[] StormlionBurrowIgnore = TilesFactory.CreateBoolSet(397, 398, 402, 399, 396, 400, 403, 401, 53, 112, 116, 234, 407, 404,
                TileID.DesertFossil, TileID.RollingCactus,
                TileID.Tin, TileID.Copper, TileID.Lead, TileID.Iron, TileID.Tungsten, TileID.Silver, TileID.Platinum, TileID.Gold);

            public static bool[] StormlionRegularIgnore = TilesFactory.CreateBoolSet(TileID.RollingCactus);

        }

        public static ILongBackAccessory GetLongBackAccessory(EquipType type, int slot) => elongatedTailEquips[type].TryGetValue(slot, out ILongBackAccessory acc) ? acc : null;
        public static void AddLongBackAccessory(int itemType, EquipType type, int slot, ILongBackAccessory accesory)
        {
            IsElongatedTail[itemType] = true;

            if (!elongatedTailEquips.ContainsKey(type))
                elongatedTailEquips[type] = new();
            elongatedTailEquips[type][slot] = accesory;
        }

        public static void PostSetupContent()
        {
            for (int i = 0; i < TileID.Sets.Conversion.Thorn.Length; i++)
            {
                TileCutIgnoreEverythingButThorns[i] = !TileID.Sets.Conversion.Thorn[i];
            }
        }
    }
}
