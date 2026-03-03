using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Core
{
    public class BossRelicLoader
    {
        public static Mod Mod => CalamityFables.Instance;

        public static int LoadBossRelic(string bossName, string bossDisplayName, string texturePath)
        {
            //Load the relic item but cache it first
            AutoloadedBossRelicItem relicItem = new AutoloadedBossRelicItem(bossName, bossDisplayName, texturePath);
            Mod.AddContent(relicItem);

            //Load the tile using the item's tile type
            AutoloadedBossRelic relicTile = new AutoloadedBossRelic(bossName, relicItem.Type, texturePath);
            Mod.AddContent(relicTile);

            //Set the relic item's type to be the one of the relic tile (so it can properly place it)
            relicItem.TileType = relicTile.Type;

            //Return the relic item's type so that npcs can drop it
            return relicItem.Type;
        }
    }

    [Autoload(false)]
    public class AutoloadedBossRelic : ModTile, ICustomLayerTile
    {
        public const int FrameWidth = 18 * 3;
        public const int FrameHeight = 18 * 4;
        public override string Texture => AssetDirectory.Tiles + "RelicPedestal";
        public override string Name => InternalName != "" ? InternalName : base.Name;

        public string InternalName;
        protected readonly int ItemType;
        protected readonly string TexturePath;

        internal static Dictionary<int, Asset<Texture2D>> RelicAssets;

        public AutoloadedBossRelic(string NPCname, int dropType, string path = null)
        {
            InternalName = NPCname + "RelicTile";
            ItemType = dropType;
            TexturePath = path;
        }

        public override void SetStaticDefaults()
        {
            Main.tileShine[Type] = 400; // Responsible for golden particles
            Main.tileFrameImportant[Type] = true; // Any multitile requires this
            TileID.Sets.InteractibleByNPCs[Type] = true; // Town NPCs will palm their hand at this tile

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4); // Relics are 3x4
            TileObjectData.newTile.LavaDeath = false; // Does not break when lava touches it
            TileObjectData.newTile.DrawYOffset = 2; // So the tile sinks into the ground
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft; // Player faces to the left
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.StyleMultiplier = 2;

            // Register an alternate tile data with flipped direction
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; // Player faces to the right
            TileObjectData.addAlternate(1);

            // Register the tile data itself
            TileObjectData.addTile(Type);

            // Register map name and color
            // "MapObject.Relic" refers to the translation key for the vanilla "Relic" text
            AddMapEntry(new Color(233, 207, 94), Language.GetText("MapObject.Relic"));
        }

        public override bool CreateDust(int i, int j, ref int type) => false;

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (drawData.tileFrameX % FrameWidth == 0 && drawData.tileFrameY % FrameHeight == 0)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.PostDrawTiles, true);
        }

        public Asset<Texture2D> GetRelicTexture()
        {
            if (RelicAssets == null)
                RelicAssets = new Dictionary<int, Asset<Texture2D>>();

            if (RelicAssets.TryGetValue(Type, out var asset))
                return asset;

            Asset<Texture2D> newAsset = ModContent.Request<Texture2D>(TexturePath + Name);
            RelicAssets.Add(Type, newAsset);
            return newAsset;
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            // Take the tile, check if it actually exists
            Point p = new Point(i, j);
            Tile tile = Main.tile[p.X, p.Y];
            if (tile == null || !tile.HasTile)
            {
                return;
            }

            // Get the initial draw parameters
            Texture2D texture = GetRelicTexture().Value;

            int frameY = tile.TileFrameX / FrameWidth; // Picks the frame on the sheet based on the placeStyle of the item
            Rectangle frame = texture.Frame(1, 1, 0, frameY);

            Vector2 origin = frame.Size() / 2f;
            Vector2 worldPos = p.ToWorldCoordinates(24f, 64f);

            Color color = Lighting.GetColor(p.X, p.Y);

            bool direction = tile.TileFrameY / FrameHeight != 0; // This is related to the alternate tile data we registered before
            SpriteEffects effects = direction ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Some math magic to make it smoothly move up and down over time
            float offset = (float)Math.Sin(Main.GlobalTimeWrappedHourly * (float)(Math.PI * 2f) / 5f);
            Vector2 drawPos = worldPos - Main.screenPosition + new Vector2(0f, -40f) + new Vector2(0f, offset * 4f);

            // Draw the main texture
            spriteBatch.Draw(texture, drawPos, frame, color, 0f, origin, 1f, effects, 0f);

            // Draw the periodic glow effect
            float scale = (float)Math.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi / 2f) * 0.3f + 0.7f;
            Color effectColor = color;
            effectColor.A = 0;
            effectColor = effectColor * 0.1f * scale;
            for (int h = 0; h < 6; h++)
            {
                spriteBatch.Draw(texture, drawPos + (MathHelper.TwoPi * h / 6f).ToRotationVector2() * (6f + offset * 2f), frame, effectColor, 0f, origin, 1f, effects, 0f);
            }
        }
    }

    [Autoload(false)]
    public class AutoloadedBossRelicItem : ModItem
    {
        public string InternalName = "";
        public string Itemname;
        public int TileType;

        private readonly string TexturePath;

        protected override bool CloneNewInstances => true;

        public override string Name => InternalName != "" ? InternalName : base.Name;
        public override string Texture => string.IsNullOrEmpty(TexturePath) ? AssetDirectory.DebugSquare : TexturePath + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault(Itemname ?? "ERROR");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType, 0);

            Item.width = 30;
            Item.height = 40;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Master;
            Item.master = true; // This makes sure that "Master" displays in the tooltip, as the rarity only changes the item name color
            Item.value = Item.buyPrice(0, 5);
        }

        public AutoloadedBossRelicItem(string NPCName, string NPCDisplayName, string texturePath)
        {
            InternalName = NPCName + "RelicItem";
            Itemname = NPCDisplayName + " Relic";
            TexturePath = texturePath;
        }
    }
}
