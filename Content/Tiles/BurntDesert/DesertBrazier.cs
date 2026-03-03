using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.Graves;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class DesertBrazier : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Desert Brazier");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<DesertBrazierTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Torch).
                AddIngredient(ItemID.SandstoneBrick, 5).
                AddTile(TileID.WorkBenches).
                Register();
        }

        public override bool? UseItem(Player player)
        {
            Tile tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);

            if (tile.HasTile && tile.TileType == TileType<SandstonePillarTile>() && player.IsInTileInteractionRange(Player.tileTargetX, Player.tileTargetY, TileReachCheckSettings.Simple) )
            {
                if (SandstonePillarTile.CanPlaceBrasier(Player.tileTargetX, Player.tileTargetY))
                {
                    SoundEngine.PlaySound(SoundID.Dig, new Vector2(Player.tileTargetX, Player.tileTargetY) * 16);
                    Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX += 1;
                    return true;
                }
            }
            return null;
        }
    }

    public class DesertBrazierSpectral : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spectral Desert Brazier");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<DesertBrazierTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
            Item.placeStyle = 1;
        }

        public override bool? UseItem(Player player)
        {
            Tile tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);

            if (tile.HasTile && tile.TileType == TileType<SandstonePillarTile>() && player.IsInTileInteractionRange(Player.tileTargetX, Player.tileTargetY, TileReachCheckSettings.Simple))
            {
                if (SandstonePillarTile.CanPlaceBrasier(Player.tileTargetX, Player.tileTargetY))
                {
                    SoundEngine.PlaySound(SoundID.Dig, new Vector2(Player.tileTargetX, Player.tileTargetY) * 16);
                    Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX += 2;
                    return true;
                }
            }
            return null;
        }
    }

    public class DesertBrazierTile : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public static Asset<Texture2D> FlameTex;

        public override void Load()
        {
            FablesGeneralSystemHooks.ModifyTilePreviewDrawEvent += ShowAsValidOnPillars;
        }

        private void ShowAsValidOnPillars(TileObjectPreviewData preview, ref Vector2 position)
        {
            if (preview.Type != Type)
                return;

            Point origin = new Point(preview.Coordinates.X + 1, preview.Coordinates.Y + 2);
            Tile tile = Framing.GetTileSafely(origin);
            if (tile.HasTile && tile.TileType == TileType<SandstonePillarTile>())
            {
                if (SandstonePillarTile.CanPlaceBrasier(origin.X, origin.Y))
                {
                    for (int i = 0; i < preview.Size.X; i++)
                        for (int j = 0; j < preview.Size.Y; j++)
                            preview[i, j] = 1;
                }
            }
        }

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = true;
            Main.tileLighted[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Origin = new Point16(1, 2);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16 };
            TileObjectData.newTile.WaterDeath = true;
            TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.StyleLineSkip = 5;
            TileObjectData.newTile.StyleWrapLimit = 2;

            TileObjectData.addTile(Type);
            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.Sand;

            SandstonePillarTile.BrasierType = Type;

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Brazier");
            AddMapEntry(new Color(204, 190, 99), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameX < 54)
            {
                if (Main.tile[i, j].TileFrameY < 54)
                {
                    r = 1f;
                    g = 0.95f;
                    b = 0.65f;

                    if (SirNautilus.SignathionVisualInfluence < 1)
                    {
                        Color color = CommonColors.DesertMirageBlue;
                        r = MathHelper.Lerp(r, color.R / 255f, SirNautilus.TorchLightColorShift);
                        g = MathHelper.Lerp(g, color.G / 255f, SirNautilus.TorchLightColorShift);
                        b = MathHelper.Lerp(b, color.B / 255f, SirNautilus.TorchLightColorShift);
                    }
                }
                else
                {
                    Color color = CommonColors.DesertMirageBlue;
                    r = color.R / 255f;
                    g = color.G / 255f;
                    b = color.B / 255f;
                }
            }
        }

        public override void HitWire(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            int topX = i - tile.TileFrameX % 54 / 18;
            int topY = j - tile.TileFrameY % 54 / 18;

            short frameAdjustment = (short)(tile.TileFrameX >= 54 ? -54 : 54);

            for (int x = topX; x < topX + 3; x++)
            {
                for (int y = topY; y < topY + 3; y++)
                {
                    Main.tile[x, y].TileFrameX += frameAdjustment;

                    if (Wiring.running)
                    {
                        Wiring.SkipWire(x, y);
                    }
                }
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendTileSquare(-1, topX, topY, 3, 3);
        }


        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.gamePaused || !Main.instance.IsActive || Lighting.UpdateEveryFrame && !Main.rand.NextBool(4))
                return;
            Tile tile = Main.tile[i, j];
            if (!TileDrawing.IsVisible(tile))
                return;

            if (!Main.rand.NextBool(20) || tile.TileFrameX != 18 || tile.TileFrameY % 54 >= 36)
                return;

            bool ghostly = tile.TileFrameY >= 54 || SirNautilus.SignathionVisualInfluence == 0;

            if (!ghostly)
            {
                var dust = Dust.NewDustDirect(new Vector2(i * 16 - 4, j * 16 + 2), 12, 4, DustID.Torch, 0f, 0f, 100, default, 1f);
                dust.noGravity = !Main.rand.NextBool(3);
                dust.velocity *= 0.3f;
                dust.velocity.Y = dust.velocity.Y - 1.5f;
            }
            else
            {
                Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(new Rectangle(i * 16 - 4, j * 16, 24, 16)), 
                    DustType<SpectralWaterDustEmbers>(), -Vector2.UnitY, 15, Color.White, Main.rand.NextFloat(0.6f, 1.3f));

            }
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = 2;
            if (tileFrameY < 54 && SirNautilus.SignathionVisualInfluence == 0)
                tileFrameY += 54;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (++frameCounter > 6)
            {
                frameCounter = 0;
                frame = ++frame % 4;
            }
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            var tile = Main.tile[i, j];

            //On fire
            if (tile.TileFrameX < 54)
            {
                frameXOffset = Main.tileFrame[type] * 54;
            }
            //Turned off
            else
                frameXOffset = 54 * 3;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            if (!TileDrawing.IsVisible(tile))
                return;

            int width = 16;
            int offsetY = 0;
            int height = 16;
            short frameX = tile.TileFrameX;
            short frameY = tile.TileFrameY;

            int frameXOffset = 0;
            int frameYOffset = 0;

            TileLoader.SetDrawPositions(i, j, ref width, ref offsetY, ref height, ref frameX, ref frameY);
            TileLoader.SetAnimationFrame(Type, i, j, ref frameXOffset, ref frameYOffset);
            frameX += (short)frameXOffset;

            FlameTex ??= Request<Texture2D>(Texture + "Flame");
            Vector2 drawOffset = FablesUtils.TileDrawOffset();

            spriteBatch.Draw(FlameTex.Value, new Vector2(i * 16, j * 16 + offsetY) - drawOffset
                , new Rectangle(frameX, frameY, width, height), Color.White with { A = 0} * 0.3f, 0f, default, 1f, 0, 0f);

        }

        public override void MouseOver(int i, int j)
        {
            if (Main.tile[i, j].TileFrameY < 54)
                SpectralWater.MouseOverIcon(i, j);
        }
    }
}
