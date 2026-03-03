using CalamityFables.Content.Tiles.WulfrumScrapyard;
using Terraria.DataStructures;
using Terraria.Localization;
using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.Wulfrum
{
    public class WulfrumSmokingPipes : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + "WulfrumPipes";

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            Main.tileMergeDirt[Type] = true;

            Main.tileMerge[Type][ModContent.TileType<WulfrumPipes>()] = true;

            Main.tileSolid[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = true;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Pipe");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (!closer)
                return;

            TileAlignment? alignment = GiveAlignment(i, j);
            if (!alignment.HasValue || alignment == TileAlignment.Single)
                return;

            if (alignment == TileAlignment.Vertical)
            {
                Tile tileAbove = Main.tile[i, j - 1];
                if ((!tileAbove.HasTile || !Main.tileSolid[tileAbove.TileType]))
                {
                    ProduceSmoke(i, j, -Vector2.UnitY);
                }
            }
            else
            {
                TileDirection? direction = GiveExtremityAlignment(i, j);
                if (!direction.HasValue || (direction != TileDirection.Left && direction != TileDirection.Right))
                    return;

                bool right = direction == TileDirection.Right;
                Tile sideTile = Main.tile[i + (right ? 1 : -1), j];
                if ((!sideTile.HasTile || !Main.tileSolid[sideTile.TileType]))
                    ProduceSmoke(i, j, right ? Vector2.UnitX : -Vector2.UnitX);
            }
        }

        public static void ProduceSmoke(int i, int j, Vector2 smokeDirection)
        {
            Vector2 position = new Vector2(i, j) * 16f + Vector2.One * 8;
            Vector2 perpendicular = smokeDirection.RotatedBy(MathHelper.PiOver2);

            if (Main.rand.NextBool(10))
            {
                Vector2 dustPosition = position + smokeDirection * 8 + perpendicular * Main.rand.NextFloat(-6f, 6f);
                Dust must = Dust.NewDustPerfect(dustPosition, DustID.Smoke, smokeDirection * Main.rand.NextFloat(0.4f, 1.4f), Scale: Main.rand.NextFloat(0.7f, 2.3f));
                must.rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                must.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 smokePosition = new Vector2(i * 16, j * 16) - Vector2.One * 4f + smokeDirection * 9f;
                Vector2 smokeVelocity = Vector2.Zero;
                if (Main.WindForVisuals < 0f)
                    smokeVelocity.X = 0f - Main.WindForVisuals;

                int[] goreIDs = new int[] { GoreID.ChimneySmoke1, GoreID.ChimneySmoke2, GoreID.ChimneySmoke3 };
                int smokeType = Main.rand.Next(goreIDs);

                if (Main.rand.NextBool(4))
                    Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.2f + 0.2f);
                else if (Main.rand.NextBool(2))
                    Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.3f + 0.3f);
                else
                    Gore.NewGore(new EntitySource_TileUpdate(i, j), smokePosition, smokeVelocity, smokeType, Main.rand.NextFloat() * 0.4f + 0.4f);
            }
        }

    }

    public class WulfrumSmokingPipesItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumTiles + "WulfrumPipesSmokingItem";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Smoking Wulfrum Pipes");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<WulfrumSmokingPipes>();
            Item.rare = 1;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddIngredient<WulfrumPipesItem>(1).
                AddTile(ModContent.TileType<BunkerWorkshop>()).
                AddCondition(Condition.InGraveyard).
                Register();
        }
    }

}