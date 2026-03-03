using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class IndustrialWulfrumTubeLightItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Industrial Wulfrum Tube Light");
            Item.ResearchUnlockCount = 1;

            ItemID.Sets.ShimmerTransformToItem[Type] = ItemType<WulfrumTubeLightItem>();
            ItemID.Sets.ShimmerTransformToItem[ItemType<WulfrumTubeLightItem>()] = Type;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<IndustrialWulfrumTubeLight>());
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<DullPlatingItem>(2).
                AddIngredient(ItemID.Torch).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    public class IndustrialWulfrumTubeLight : WulfrumTubeLight, ICustomTileDrawOffset
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override int DropType => ItemType<IndustrialWulfrumTubeLightItem>();

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            FablesSets.CustomDrawOffset[Type] = true;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer && Main.tile[i,j].TileFrameY == 0 && Main.rand.NextBool(250))
            {
                Tile t = Main.tile[i, j];
                float minX = 6f;
                float maxX = 10f;

                //Horizontal ceiling lamps
                if (t.TileFrameY == 0)
                {
                    Tile leftTile = Framing.GetTileSafely(i - 1, j);
                    Tile rightTile = Framing.GetTileSafely(i + 1, j);
                    if (leftTile.HasTile && leftTile.TileType == Type)
                        minX = 0;
                    if (rightTile.HasTile && rightTile.TileType == Type)
                        maxX = 16;
                }

                Vector2 dustPos = new Vector2(i, j) * 16; 
                Dust d =Dust.NewDustPerfect(dustPos + new Vector2(Main.rand.NextFloat(minX, maxX), 8), 74, Vector2.UnitY * Main.rand.NextFloat(0.5f, 1f), 255, new Color(1f, 0.8f, 0.25f, 0f), Main.rand.NextFloat(0.25f, 0.5f));
                d.noGravity = false;
                d.noLight = true;
                d.noLightEmittence = true;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            base.PostDraw(i, j, spriteBatch);
            Tile tile = Main.tile[i, j];
            if (tile.TileFrameY != 0)
                return;

            int xOffset = 0;
            int yOffset = 0;
            AnimateIndividualTile(Type, i, j, ref xOffset, ref yOffset);

            int xPos = tile.TileFrameX + xOffset;
            int yPos = 78;

            Texture2D glowRay = TextureAssets.Tile[Type].Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            Color drawColour = GetDrawColour(i, j, Color.White) * CommonColors.WulfrumLightMultiplier;
            drawOffset.Y += 8;

            Vector2 drawScale = Vector2.One;
            drawScale.Y *= 1.2f + MathF.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.2f;

            Main.spriteBatch.Draw(glowRay, drawOffset, new Rectangle(xPos, yPos, 18, 33), drawColour with { A = 0 } * 0.7f, 0.0f, Vector2.Zero, drawScale, SpriteEffects.None, 0.0f);
        }
    }
}
