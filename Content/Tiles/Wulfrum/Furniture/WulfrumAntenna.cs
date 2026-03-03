using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumAntenna : ModTile, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public static int ForkAntennaType;

        public override void Load()
        {
            //Bubble block-esque placement
            Terraria.On_Player.PlaceThing_Tiles_BlockPlacementForAssortedThings += NonsolidConnectionForAntennae;
            FablesGeneralSystemHooks.PostSetupContentEvent += SetTileMerge;
        }


        private bool NonsolidConnectionForAntennae(Terraria.On_Player.orig_PlaceThing_Tiles_BlockPlacementForAssortedThings orig, Player self, bool canPlace)
        {
            if (self.inventory[self.selectedItem].createTile == Type)
            {
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if (i * j == 0 && i != j && ValidConnection(Main.tile[Player.tileTargetX + i, Player.tileTargetY + j]))
                        {
                            canPlace = true;
                            break;
                        }
            }

            return orig(self, canPlace);
        }

        public static bool ValidConnection(Tile tile) => tile.HasTile || tile.WallType > 0;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.DynastyWood;
            HitSound = SoundID.Item126;
            Main.tileBlockLight[Type] = false;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileNoAttach[Type] = true;

            AddMapEntry(CommonColors.WulfrumPipeworksBrown);
            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public void SetTileMerge()
        {
            for (int i = 0; i < Main.tileMerge[Type].Length; i++)
                Main.tileMerge[Type][i] = !Main.tileNoAttach[i] && Main.tileSolid[i];
        }

        public static bool MergeWithForkAntenna(Tile myTile, Tile otherTile) => otherTile.TileType == ForkAntennaType && otherTile.TileFrameY != 0 && (otherTile.TileFrameX % 54) == 18;

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            if (!FablesUtils.BetterGemsparkFraming(i, j, resetFrame, MergeWithForkAntenna))
                return false;

            if (MergeWithForkAntenna(Main.tile[i, j], Framing.GetTileSafely(i, j - 1)))
            {
                Main.tile[i, j].TileFrameX = 18 * 5;
                Main.tile[i, j].TileFrameY = (short)(18 * Main.tile[i, j].TileFrameNumber);
                return false;
            }

            return true;
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            if (FablesUtils.GetSmoothTileRNG(new Point(i, j)) < 0.4f)
                frameYOffset += Terraria.GameContent.TextureAssets.Tile[type].Value.Height / 2;
        }
    }

    public class WulfrumAntennaItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Antenna");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumAntenna>());
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(5).
                AddIngredient<WulfrumHullItem>().
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }
}