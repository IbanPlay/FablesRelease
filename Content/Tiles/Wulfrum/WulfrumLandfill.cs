using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using CalamityFables.Content.Tiles.WulfrumScrapyard;
using Terraria.Localization;
using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.Wulfrum
{
    public class WulfrumLandfill : ModTile
    {
        public Asset<Texture2D> GlowTexture;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                GlowTexture = ModContent.Request<Texture2D>(AssetDirectory.WulfrumTiles + Name + "_Glow");
            }

            FablesGeneralSystemHooks.PostSetupContentEvent += SetTileMerge;
        }

        public void SetTileMerge()
        {
            FablesUtils.SetMerge(Type, ModContent.TileType<WulfrumRotorLandfill>());
            FablesUtils.SetMerge(Type, ModContent.TileType<WulfrumBigRotorLandfill>());
            FablesUtils.SetMerge(ModContent.TileType<WulfrumRotorLandfill>(), ModContent.TileType<WulfrumBigRotorLandfill>());
        }

        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            Main.tileMergeDirt[Type] = true;

            Main.tileSolid[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = true;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Landfill");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
        }
        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.rand.NextBool(50) && (!Main.tile[i, j - 1].HasTile || !Main.tileSolid[Main.tile[i, j - 1].TileType]))
            {
                Vector2 pos = new Vector2(i, j) * 16;
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(-6, 0)), 44, new Vector2(Main.rand.NextFloat(-0.02f, 0.02f), -Main.rand.NextFloat(0.05f, 0.18f)), 0, new Color(0.2f, 0.2f, 0.25f, 0f), Main.rand.NextFloat(0.25f, 0.5f));
            }
        }

        public override bool HasWalkDust() => true;
        public override void WalkDust(ref int dustType, ref bool makeDust, ref Color color)
        {
            makeDust = Main.rand.NextBool(4);
            color.A = 133;
        }
        public override void FloorVisuals(Player player)
        {
            base.FloorVisuals(player);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (GlowTexture is null)
                return;

            int xPos = Main.tile[i, j].TileFrameX;
            int yPos = Main.tile[i, j].TileFrameY;
            Tile trackTile = Main.tile[i, j];

            if (trackTile.IsHalfBlock || trackTile.Slope != SlopeType.Solid)
                return;

            TileDirection? direction = GiveDirection(i, j);
            if (direction == null || BroadTop.Contains(direction.Value))
                return;

            bool hasGlow = ((i % 13) + (xPos % 25) * (yPos % 12)) % 8 == 2;
            if (!hasGlow)
                return;

            Texture2D tex = GlowTexture.Value;

            Texture2D bloomTex = AssetDirectory.CommonTextures.BloomCircle.Value;
            float opacityShift = (float)Math.Pow(Math.Cos(-Main.GlobalTimeWrappedHourly * 1.36f + i * 0.12f + j * 0.3f), 4f);

            Color lightColor = Color.White * opacityShift;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 epicoOffset = new Vector2(((i % 13) + (j % 5) + (yPos % 24)) % 10, ((xPos % 13) + (yPos % 5) + (j % 24)) % 10);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffset - Vector2.One * 5 + epicoOffset;

            int frameVariant = ((yPos % 19) - (j % 17) * 22).ModulusPositive(3);
            Rectangle frame = new Rectangle(0, frameVariant * tex.Height / 3, tex.Width, tex.Height / 3 - 2);

            lightColor.A = 0;
            spriteBatch.Draw(tex, drawPosition, frame, lightColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            float sizeShift = 0.10f * (float)Math.Pow(opacityShift, 4f);
            Color bloomColor = ((frameVariant == 0 ? CommonColors.WulfrumGreen : CommonColors.WulfrumBlue) * opacityShift) with { A = 0 };
            spriteBatch.Draw(bloomTex, drawPosition + Vector2.One * 3f, null, bloomColor * 0.1f, 0f, bloomTex.Size() / 2, sizeShift, SpriteEffects.None, 0f);

        }
    }

    public class WulfrumLandfillItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Landfill");
            Tooltip.SetDefault("'The scrap has been compacted too much for it to be extracted back...'");

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
            Item.createTile = ModContent.TileType<WulfrumLandfill>();
            Item.rare = 1;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(10).
                AddIngredient<WulfrumMetalScrap>(1).
                AddTile<BunkerWorkshop>().
                DisableDecraft().
                Register();
        }
    }

}