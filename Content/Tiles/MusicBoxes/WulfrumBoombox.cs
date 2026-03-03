using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.NPCs.Wulfrum;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.Utilities;

namespace CalamityFables.Content.Tiles.MusicBox
{
    public class WulfrumBoomboxItem : ModItem
    {
        public override string Texture => AssetDirectory.MusicBoxes + Name;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.CanGetPrefixes[Type] = false;
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;
            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(CalamityFables.Instance, "Sounds/Music/Nexus"), Type, ModContent.TileType<WulfrumBoombox>());
        }

        public override void SetDefaults()
        {
            Item.DefaultToMusicBox(ModContent.TileType<WulfrumBoombox>(), 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.MusicBox).AddIngredient<EnergyCore>().AddTile(TileID.WorkBenches).Register();
        }
    }

    public class WulfrumBoombox : ModTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.MusicBoxes + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Tungsten;
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);

            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleLineSkip = 6;

            //Zero padding so that there's no tile coordinate that falls naturally on 36
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(87, 92, 68), Language.GetText("ItemName.MusicBox"));

            FablesGeneralSystemHooks.SwitchMusicBox += CustomToggle;

            RegisterItemDrop(ModContent.ItemType<WulfrumBoomboxItem>());
        }

        //Custom toggle cuz our music box is 3 wide
        private bool CustomToggle(int i, int j)
        {
            Tile t = Main.tile[i, j];
            int left = i - ((t.TileFrameX / 16) % 3);
            int top = j - t.TileFrameY / 16;

            //Offset all tiles 
            int frameOffset = t.TileFrameX > 48 ? -1 : 1;
            for (int x = left; x < left + 3; x++)
                for (int y = top; y < top + 2; y++)
                {
                    Tile transformTile = Main.tile[x, y];
                    transformTile.TileFrameX = (short)(transformTile.TileFrameX + frameOffset * 96);

                    if (Wiring.running)
                        Wiring.SkipWire(x, y);
                }

            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<WulfrumBoomboxItem>();
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.tile[i, j].TileFrameX == 96)
                Main.SceneMetrics.ActiveMusicBox = WulfrumBunkerRaidScene.NexusMusic;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (drawData.tileFrameX >= 96)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.PostDrawTiles, true);

            #region Spawn music notes
            // This code spawns the music notes when the music box is open.
            if (Main.gamePaused || !Main.instance.IsActive || Lighting.UpdateEveryFrame && new FastRandom(Main.TileFrameSeed).WithModifier(i, j).Next(4) != 0)
            {
                return;
            }

            Tile tile = Main.tile[i, j];
            if (!TileDrawing.IsVisible(tile) || tile.TileFrameX < 96 || tile.TileFrameX % 48 != 16 || tile.TileFrameY != 0 || (int)Main.timeForVisualEffects % 7 != 0 || !Main.rand.NextBool(3))
            {
                return;
            }

            int MusicNote = Main.rand.Next(570, 573);
            Vector2 SpawnPosition = new Vector2(i * 16 + 8, j * 16 - 8);
            Vector2 NoteMovement = new Vector2(Main.WindForVisuals * 2f, -0.5f);
            NoteMovement.X *= Main.rand.NextFloat(0.5f, 1.5f);
            NoteMovement.Y *= Main.rand.NextFloat(0.5f, 1.5f);
            switch (MusicNote)
            {
                case 572:
                    SpawnPosition.X -= 8f;
                    break;
                case 571:
                    SpawnPosition.X -= 4f;
                    break;
            }

            Gore.NewGore(new EntitySource_TileUpdate(i, j), SpawnPosition, NoteMovement, MusicNote, 0.8f);
            #endregion
        }


        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            // Take the tile, check if it actually exists
            Point p = new Point(i, j);
            Tile t = Main.tile[p.X, p.Y];

            if (!TileDrawing.IsVisible(t))
                return;
            Color color = Lighting.GetColor(p);
            if (t.IsTileFullbright)
                color = Color.White;

            // Get the initial draw parameters
            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }

            //Frame offset by the activated offset
            Rectangle frame = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);
            if (t.TileFrameX >= 96)
                frame.X -= 96;

            int left = i - ((t.TileFrameX / 16) % 3);
            int top = j - t.TileFrameY / 16;

            Vector2 bopBase = new Vector2(left * 16 + 24, top * 16 + 20);
            Vector2 offsetFromBopBase = new Vector2(i * 16, j * 16) - bopBase;

            float animSpeed = 4.4f;

            float rotation = (float)Math.Sin(Main.GlobalTimeWrappedHourly * animSpeed * (float)(Math.PI * 2f)) * 0.1f;
            float offset = (float)Math.Sin(Main.GlobalTimeWrappedHourly * animSpeed * (float)(Math.PI));
            FastRandom randShake = new FastRandom(Main.TileFrameSeed).WithModifier(left, top);

            if (offset > 0f)
            {
                offset = 0f;
                rotation *= 0.4f;
            }

            Vector2 bopPosition = bopBase + new Vector2(0f, offset * 4f + 2f);

            float scale = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * animSpeed * (float)(Math.PI * 6)) * 0.03f;

            // Draw the main texture
            spriteBatch.Draw(texture, bopPosition + offsetFromBopBase.RotatedBy(rotation) * scale - Main.screenPosition, frame, color, rotation, Vector2.Zero, scale, 0, 0);

            Texture2D highlightTexture = null;
            Color highlightColor = Color.Transparent;
            Main.instance.TilesRenderer.GetTileOutlineInfo(i, j, t.TileType, ref color, ref highlightTexture, ref highlightColor);
            if (highlightTexture != null)
            {
                spriteBatch.Draw(highlightTexture, bopPosition + offsetFromBopBase.RotatedBy(rotation) * scale - Main.screenPosition, frame, highlightColor, rotation, Vector2.Zero, scale, 0, 0);
            }
        }
    }

}