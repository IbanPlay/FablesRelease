using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using CalamityFables.Content.Tiles.WulfrumScrapyard;
using Terraria.DataStructures;
using Terraria.Localization;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum
{
    public class WulfrumRotorLandfill : ModTile, IForegroundTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + "WulfrumLandfill";

        public virtual int Variant => 1;

        public bool IsForeground(int i, int j) => false && (((i % 7) * (j % 91) + (i * j)) % 4 == 0);
        public int RotationDirection(int i, int j, int oneInXGearsRotate = 5)
        {
            if ((i + j) % (oneInXGearsRotate * 2) == 0)
                return 1;

            else if ((i + j) % (oneInXGearsRotate * 2) == 0)
                return -1;

            return 0;
        }

        public Rectangle VariantFrame(int i, int j)
        {
            return new Rectangle(0, 270 + Variant * 108, 106, 106);
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            RegisterItemDrop(ItemType<WulfrumLandfillItem>());
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


        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (!IsForeground(i, j))
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.BehindTiles);
            else
                ForegroundManager.AddForegroundDrawingPoint(i, j);
        }


        public void DrawRotor(int x, int y, SpriteBatch spriteBatch, float opacity = 1)
        {
            Tile trackTile = Main.tile[x, y];

            //if (trackTile.IsHalfBlock || trackTile.Slope != SlopeType.Solid)
            //    return;

            Color color = Lighting.GetColor(x, y);
            Tile t = Main.tile[x, y];
            if (t.IsTileFullbright)
                color = Color.White;
            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }


            Rectangle frame = VariantFrame(x, y);
            Vector2 drawPosition = new Vector2(x * 16 - Main.screenPosition.X, y * 16 - Main.screenPosition.Y) + Vector2.One * 8;

            float rotationOffset = (x + y * x) % MathHelper.TwoPi;
            int rotationDirection = RotationDirection(x, y, 5);
            float rotation = rotationOffset + rotationDirection * Main.GlobalTimeWrappedHourly * 0.8f;

            spriteBatch.Draw(texture, drawPosition, frame, color * opacity, rotation, frame.Size() / 2, 1f, SpriteEffects.None, 0f);
        }

        public void ForegroundDraw(int x, int y, SpriteBatch spriteBatch)
        {
            float opacity = 0.5f + 0.5f * Utils.GetLerpValue(30f, 100f, Main.LocalPlayer.Distance(new Vector2(x, y) * 16 + Vector2.One * 8f), true);
            DrawRotor(x, y, spriteBatch, opacity);
        }

        public void DrawSpecialLayer(int x, int y, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            DrawRotor(x, y, spriteBatch);
        }
    }


    public class WulfrumBigRotorLandfill : WulfrumRotorLandfill, IForegroundTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + "WulfrumLandfill";
        public override int Variant => 0;
    }


    public class WulfrumRotorItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Rotor Piece");
            Tooltip.SetDefault("'A small rusty cog is lodged firmly inside the debris'");
            Item.ResearchUnlockCount = 10;
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
            Item.createTile = TileType<WulfrumRotorLandfill>();
            Item.rare = 1;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddIngredient<WulfrumLandfillItem>(1).
                AddRecipeGroup(RecipeGroupID.IronBar, 4).
                AddCondition(Condition.InGraveyard).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    public class WulfrumBigRotorItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Large Wulfrum Rotor Piece");
            Tooltip.SetDefault("'A large rusty cog is lodged firmly inside the debris'");
            Item.ResearchUnlockCount = 10;
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
            Item.createTile = TileType<WulfrumBigRotorLandfill>();
            Item.rare = 1;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddIngredient<WulfrumLandfillItem>(1).
                AddRecipeGroup(RecipeGroupID.IronBar, 6).
                AddCondition(Condition.InGraveyard).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }
}