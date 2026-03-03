using Terraria.DataStructures;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumMetalScrap")]
    public class WulfrumMetalScrap : ModItem
    {
        private int subID = 0; //Controls the in-world sprite for this item <- IS WHAT A SPIRIT MOD CODER WOULD SAY!
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void Load()
        {
            On_Item.CanFillEmptyAmmoSlot += AvoidDefaultingToAmmoSlot;
        }

        public override void Unload()
        {
            On_Item.CanFillEmptyAmmoSlot -= AvoidDefaultingToAmmoSlot;
        }

        private bool AvoidDefaultingToAmmoSlot(On_Item.orig_CanFillEmptyAmmoSlot orig, Item self)
        {
            if (self.type == Type)
                return false;

            return orig(self);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
            DisplayName.SetDefault("Wulfrum Metal Scrap");
        }

        public override void SetDefaults()
        {
            subID = 0;
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 80);
            Item.rare = ItemRarityID.White;
            Item.ammo = Item.type;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<WulfrumScrapTile>();
        }

        public override bool CanStackInWorld(Item item2)
        {
            //Become inventory sprite if merging with inventory sprite
            if ((item2.ModItem as WulfrumMetalScrap).subID == 0)
                subID = 0;

            return base.CanStackInWorld(item2);
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (source is EntitySource_Loot)
            {
                subID = Main.rand.Next(3) + 1;
                switch (subID)
                {
                    case 1:
                        Item.height = 20;
                        break;
                    case 2:
                        Item.height = 22;
                        break;
                    default:
                        Item.height = 28;
                        break;
                }
            }
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (subID == 0)
                return true;

            Texture2D tex = ModContent.Request<Texture2D>(Texture + "World" + (subID).ToString()).Value;
            spriteBatch.Draw(tex, Item.position - Main.screenPosition, null, lightColor, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class WulfrumScrapTile : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            HitSound = SoundID.Tink;
            Main.tileSolid[Type] = false;

            // Sand specific properties
            TileID.Sets.Falling[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tilePile[Type] = true;
            TileID.Sets.FallingBlockProjectile[Type] = new TileID.Sets.FallingBlockProjectileInfo(ModContent.ProjectileType<WulfrumScrapFalling>(), 0);
            TileID.Sets.CanPlaceNextToNonSolidTile[Type] = true;

            DustType = DustID.Tungsten;
            AddMapEntry(new Color(150, 168, 108));
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            yield return new Item(ModContent.ItemType<WulfrumMetalScrap>(), 1).OverrideTexture("Tiles/WulfrumScrapyard/RustyWulfrumMetalScrap");
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY += 2;
        }
    }

    public class WulfrumScrapFalling : ModProjectile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Falling Wulfrum Scrap");
            ProjectileID.Sets.FallingBlockDoesNotFallThroughPlatforms[Type] = true;
            ProjectileID.Sets.ForcePlateDetection[Type] = true;
            ProjectileID.Sets.FallingBlockTileItem[Type] = new(ModContent.TileType<WulfrumScrapTile>(), ModContent.ItemType<WulfrumMetalScrap>());
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.PlatinumCoinsFalling);
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            Vector2 center = Projectile.position + new Vector2(Projectile.width, Projectile.height) * 0.5f;
            Vector2 pos = center - new Vector2(width, height) * hitboxCenterFrac;

            //Copy vanilla code for projectile ID 10, because in vanilla only coin tiles collide with nonsolid falling tiles!

            Projectile.velocity = Collision.TileCollision(pos, Projectile.velocity, width, height, fallThrough, fallThrough);
            Projectile.velocity = Collision.AnyCollisionWithSpecificTiles(pos, Projectile.velocity, width, height, TileID.Sets.Falling, evenActuated: true);
            return false;
        }
    }
}
