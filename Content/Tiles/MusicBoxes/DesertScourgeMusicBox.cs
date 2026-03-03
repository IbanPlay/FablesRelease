using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.Utilities;

namespace CalamityFables.Content.Tiles.MusicBox
{
    public class DesertScourgeMusicBoxItem : ModItem
    {
        public override string Texture => AssetDirectory.MusicBoxes + Name ;

        public static int prowlMusicID;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Music Box (The Dessicated Husk)");
            ItemID.Sets.CanGetPrefixes[Type] = false;
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;
            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(CalamityFables.Instance, "Sounds/Music/DesertScourge"), Type, ModContent.TileType<DesertScourgeMusicBox>());
            prowlMusicID = MusicLoader.GetMusicSlot(CalamityFables.Instance, "Sounds/Music/DesertScourgeProwl");
        }

        public override void SetDefaults()
        {
            Item.DefaultToMusicBox(ModContent.TileType<DesertScourgeMusicBox>(), 0);
        }
    }

    public class DesertScourgeMusicBox : ModTile
    {
        public override void Load()
        {
            FablesGeneralSystemHooks.SwitchMusicBox += ThreeStateMusicBox;
        }

        private bool ThreeStateMusicBox(int i, int j) => FablesUtils.MultiWayMusicBoxSwitch(Type, i, j, 3);

        public override string Texture => AssetDirectory.MusicBoxes + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleLineSkip = 2;
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(153, 122, 111), Language.GetText("ItemName.MusicBox"));
            RegisterItemDrop(ModContent.ItemType<DesertScourgeMusicBoxItem>());
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<DesertScourgeMusicBoxItem>();
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Tile t = Main.tile[i, j];
            if (t.TileFrameX >= 72)
                frameXOffset = 36 * ((int)(Main.timeForVisualEffects * 0.2f) % 6);
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.tile[i, j].TileFrameX >= 72)
                Main.SceneMetrics.ActiveMusicBox = DesertScourgeMusicBoxItem.prowlMusicID;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            // This code spawns the music notes when the music box is open.
            if (Main.gamePaused || !Main.instance.IsActive || Lighting.UpdateEveryFrame && new FastRandom(Main.TileFrameSeed).WithModifier(i, j).Next(4) != 0)
            {
                return;
            }

            Tile tile = Main.tile[i, j];

            if (!TileDrawing.IsVisible(tile) || tile.TileFrameX < 36 || tile.TileFrameX % 36 != 0 || tile.TileFrameY % 36 != 0 || (int)Main.timeForVisualEffects % 7 != 0 || !Main.rand.NextBool(3))
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
        }
    }

}