using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Items.CrabulonDrops;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.Utilities;
using CalamityFables.Particles;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class PsychedelicFogMachineItem : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psychedelic Fog Machine");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<PsychedelicFogMachine>(), 0);
            Item.accessory = true;
            Item.vanity = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<MyceliumMoldItem>(25).AddIngredient(ItemID.Glass, 10).AddTile(TileID.Anvils).Register();
        }

        public override void UpdateVanity(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Crabulon.TrippinessAfflicted = true;
                Crabulon.ForceShaderActive = true;
            }
        }
    }

    public class PsychedelicFogMachine : ModTile
    {
        public static Asset<Texture2D> Glowmask;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                Glowmask = ModContent.Request<Texture2D>(Texture + "_Glow");
            }
            FablesGeneralSystemHooks.ResetNearbyTileEffectsEvent += ResetFlags;
        }

        private void ResetFlags()
        {
            Crabulon.TrippinessAfflicted = false;
            Crabulon.ForceShaderActive = false;
        }

        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            Main.tileLighted[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = new int[]{16, 16, 18};
            TileObjectData.newTile.Origin = new Point16(0, 2);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(178, 184, 181));
            RegisterItemDrop(ModContent.ItemType<PsychedelicFogMachineItem>());
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {

            Tile t = Main.tile[i, j];
            if (t.TileFrameX >= 72)
            {
                r += 0.05f;
                g += 0.05f;
                b += 1.9f;
            }
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<PsychedelicFogMachineItem>();
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override bool RightClick(int i, int j)
        {
            FlipState(i, j);
            return true;
        }
        public override void HitWire(int i, int j)
        {
            FlipState(i, j);
        }


        public void FlipState(int i, int j)
        {
            SoundEngine.PlaySound(SoundID.MenuTick, new Vector2(i, j) * 16);

            int tileLeftX = i - ((Main.tile[i, j].TileFrameX / 18) % 2);
            int tileTopY = j - Main.tile[i, j].TileFrameY / 18;

            for (int k = tileLeftX; k < tileLeftX + 2; k++)
            {
                for (int l = tileTopY; l < tileTopY + 3; l++)
                {
                    Tile tile = Main.tile[k, l];
                    if (!tile.HasTile)
                        continue;

                    tile.TileFrameX += 36;
                    if (tile.TileFrameX >= 36 * 3)
                        tile.TileFrameX -= 36 * 3;
                }
            }

            if (Wiring.running)
            {
                Wiring.SkipWire(tileLeftX, tileTopY);
                Wiring.SkipWire(tileLeftX, tileTopY + 1);
                Wiring.SkipWire(tileLeftX, tileTopY + 2);
                Wiring.SkipWire(tileLeftX + 1, tileTopY);
                Wiring.SkipWire(tileLeftX + 1, tileTopY + 1);
                Wiring.SkipWire(tileLeftX + 1, tileTopY + 2);
            }

            NetMessage.SendTileSquare(-1, tileLeftX, tileTopY, 2, 3);
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Tile t = Main.tile[i, j];
            if (t.TileFrameX >= 72)
                frameXOffset = 36 * ((int)(Main.timeForVisualEffects * 0.4f) % 7);
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer && Main.tile[i, j].TileFrameX >= 36)
            {
                Crabulon.ForceShaderActive = true;

                if (Main.tile[i, j].TileFrameX >= 72)
                {
                    Crabulon.TrippinessAfflicted = true;
                }
            }
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            // Gas it up when in 3rd mode
            if (Main.gamePaused || !Main.instance.IsActive || Lighting.UpdateEveryFrame && new FastRandom(Main.TileFrameSeed).WithModifier(i, j).Next(4) != 0)
            {
                return;
            }

            Tile tile = Main.tile[i, j];

            if (!TileDrawing.IsVisible(tile) || tile.TileFrameX < 36 || tile.TileFrameX % 36 != 0 || tile.TileFrameY != 0 || (int)Main.timeForVisualEffects % 7 != 0 || !Main.rand.NextBool(3))
                return;

            Rectangle tileRect = new Rectangle(i * 16, j * 16, 20, 26);

                Vector2 position = Main.rand.NextVector2FromRectangle(tileRect);
                Dust.NewDustPerfect(position, DustID.GlowingMushroom, Main.rand.NextVector2Circular(3f, 3f), Scale: Main.rand.NextFloat(0.7f, 1.1f));
            

            if (Main.rand.NextBool(2))
            {
                position = Main.rand.NextVector2FromRectangle(tileRect);
                float smokeSize = Main.rand.NextFloat(1.6f, 2.2f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.2f, 0.3f) - Vector2.UnitY * 0.6f;

                Particle smoke = new SporeGas(position, velocity, position, 50, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i,j];
            if (!TileDrawing.IsVisible(tile))
                return;

            Vector2 offScreen = new Vector2(Main.offScreenRange);
            if (Main.drawToScreen)
            {
                offScreen = Vector2.Zero;
            }

            Texture2D tex = Glowmask.Value;
            Vector2 pos = new Vector2(i, j) * 16 - Main.screenPosition + offScreen;

            int frameX = tile.TileFrameX;
            int frameY = tile.TileFrameY;
            int frameXOffset = 0;
            AnimateIndividualTile(0, i, j, ref frameXOffset, ref frameY);
            frameX += frameXOffset;
            spriteBatch.Draw(tex, pos, new Rectangle(frameX, frameY, 16, 16), Color.White);
        }
    }

}