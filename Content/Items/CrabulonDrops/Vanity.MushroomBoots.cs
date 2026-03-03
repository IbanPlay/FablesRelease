using Terraria.Graphics.Renderers;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class MushroomBoots : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mushroom Boots");
            Tooltip.SetDefault("Leaves a trail of mushrooms in your wake");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.vanity = true;
            Item.width = 32;
            Item.height = 32;
        }

        public override void UpdateEquip(Player player) => UpdateVanity(player);

        public override void UpdateVanity(Player player)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            player.DoBootsEffect(DoBootsEffect_PlaceMushroomsOnTiles);
        }


        public bool DoBootsEffect_PlaceMushroomsOnTiles(Player player, int X, int Y)
        {
            float speedChance = Utils.GetLerpValue(8f, 0f, Math.Abs(player.velocity.X), true);
            if (Main.rand.NextFloat() < speedChance)
                return false;

            Tile tile = Main.tile[X, Y + 1];
            if (!tile.HasTile || tile.LiquidAmount > 0 || !WorldGen.SolidTileAllowBottomSlope(X, Y + 1))
                return false;


            MushroomBootParticle mushroomParticle = _particlePool.RequestParticle();

            Vector2 spawnPosition = new Vector2(X * 16 + 8, Y * 16 + 16) + Main.rand.NextFloatDirection() * Vector2.UnitX * 9f;
            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture + "Shrooms", AssetRequestMode.ImmediateLoad);
            Asset<Texture2D> glowTexture = ModContent.Request<Texture2D>(Texture + "Shrooms_Glow", AssetRequestMode.ImmediateLoad);

            float particleLifetime = Main.rand.NextFloat(60f, 90f);
            mushroomParticle.SetBasicInfo(texture, texture.Value.Frame(1, 3, 0, Main.rand.Next(3), -2, -2), Vector2.Zero, spawnPosition);
            mushroomParticle.SetTypeInfo(particleLifetime);

            float scale = Main.rand.NextFloat() * 0.8f + 0.67f;
            Vector2 squish = Vector2.One * Main.rand.NextFloat(1f, 1.2f);
            squish.X = 2 - squish.X;


            mushroomParticle._glowmaskTexture = glowTexture;
            mushroomParticle.FadeOutNormalizedTime = 0.4f;
            mushroomParticle.ScaleVelocity = new Vector2(0f, 1f) * scale * 0.3f;
            mushroomParticle.ScaleAcceleration = squish * scale * (-1f / 60f) / particleLifetime;
            mushroomParticle.Scale = squish * scale;
            mushroomParticle.velocityRotation = Math.Min(player.velocity.X, 12f) * Main.rand.NextFloat(0.12f, 0.2f);
            mushroomParticle.Rotation = Main.rand.NextFloat(-0.05f, 0.05f);


            Main.ParticleSystem_World_BehindPlayers.Add(mushroomParticle);


            return true;
        }

        internal static ParticlePool<MushroomBootParticle> _particlePool = new ParticlePool<MushroomBootParticle>(200, GetNewMushroomBootParticle);
        private static MushroomBootParticle GetNewMushroomBootParticle() => new MushroomBootParticle();
    }

    public class MushroomBootParticle : ABasicParticle
    {
        public float FadeOutNormalizedTime = 1f;
        private float _timeTolive;
        private float _timeSinceSpawn;
        public Asset<Texture2D> _glowmaskTexture;

        public float velocityRotation = 0;

        public override void FetchFromPool()
        {
            base.FetchFromPool();
            FadeOutNormalizedTime = 1f;
            _timeTolive = 0f;
            _timeSinceSpawn = 0f;
        }

        public override void SetBasicInfo(Asset<Texture2D> textureAsset, Rectangle? frame, Vector2 initialVelocity, Vector2 initialLocalPosition)
        {
            base.SetBasicInfo(textureAsset, frame, initialVelocity, initialLocalPosition);
            _origin = new Vector2(_frame.Width / 2, _frame.Height - 2);
        }

        public void SetTypeInfo(float timeToLive)
        {
            _timeTolive = timeToLive;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            base.Update(ref settings);
            _timeSinceSpawn += 1f;
            if (_timeSinceSpawn >= _timeTolive)
                ShouldBeRemovedFromRenderer = true;

            if (_timeTolive / _timeTolive > 0.3f && ScaleVelocity.Y > 0)
                ScaleVelocity.Y *= -0.04f;

            Lighting.AddLight(LocalPosition, Color.RoyalBlue.ToVector3() * 1f * (1 - _timeSinceSpawn / _timeTolive));
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            float fadeOut = Utils.GetLerpValue(1f, FadeOutNormalizedTime, _timeSinceSpawn / _timeTolive, true);
            float rotation = Rotation + velocityRotation * (float)Math.Pow(Utils.GetLerpValue(0.25f, 0, _timeSinceSpawn / _timeTolive, true), 0.5f);

            Color color = Lighting.GetColor(LocalPosition.ToTileCoordinates()) * fadeOut;
            spritebatch.Draw(_texture.Value, settings.AnchorPosition + LocalPosition, _frame, color, rotation, _origin, Scale, 0, 0);

            color = new Color((int)(250 * fadeOut), (int)(250 * fadeOut), 250, 200) * fadeOut;
            spritebatch.Draw(_glowmaskTexture.Value, settings.AnchorPosition + LocalPosition, _frame, color, rotation, _origin, Scale, 0, 0);

            color = Color.RoyalBlue * Utils.GetLerpValue(0.25f, 0f, _timeSinceSpawn / _timeTolive, true);
            spritebatch.Draw(_glowmaskTexture.Value, settings.AnchorPosition + LocalPosition, _frame, color with { A = 0 }, rotation, _origin, Scale * 1.06f, 0, 0);
        }
    }
}