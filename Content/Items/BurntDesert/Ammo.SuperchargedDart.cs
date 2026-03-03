using CalamityFables.Content.Dusts;
using CalamityFables.Particles;
using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Items.BurntDesert
{
    public class SuperchargedDart : ModItem
    {
        public override string Texture => AssetDirectory.DesertItems + Name;
        public static Asset<Texture2D> Glowmask;
        public static Asset<Texture2D> Outline;

        public static float ARC_DAMAGE_MULT = 1f;
        public static int ARC_MAX_RANGE = 320;
        public static int ARC_MAX_TARGETS = 3;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 7;
            Item.height = 14;
            Item.useTime = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.knockBack = 2;
            Item.value = Item.sellPrice(copper: 4);
            Item.rare = ItemRarityID.Blue;
            Item.shootSpeed = 2;
            Item.shoot = ModContent.ProjectileType<SuperchargedDartProjectile>();
            Item.ammo = AmmoID.Dart;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Glowmask ??= ModContent.Request<Texture2D>(Texture + "_Glowmask");

            Vector2 position = Item.Center - Main.screenPosition;
            position.Y -= Item.height / 2;

            spriteBatch.Draw(Glowmask.Value, position, null, Color.White, rotation, Glowmask.Size() / 2f, scale, 0, 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe(50).
                AddIngredient(ModContent.ItemType<Electrocells>()).
                AddRecipeGroup(FablesRecipes.AnyCopperBarGroup).
                Register();
        }
    }

    public class SuperchargedDartProjectile : ModProjectile
    {
        public override string Texture => AssetDirectory.DesertItems + "SuperchargedDart";

        public enum AIStates
        {
            Flying,
            Lingering
        }

        public AIStates State 
        { 
            get => (AIStates)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        public int Lifetime => (State == AIStates.Lingering ? 20 : 600) * Projectile.MaxUpdates;
        public float LifetimeCompletion => 1 - Projectile.timeLeft / (float)Lifetime;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            // Slow down quickly when lingering and fade out
            if (State == AIStates.Lingering)
            {
                Projectile.velocity *= 0.97f;
                Projectile.Opacity = Utils.GetLerpValue(1f, 0.5f, LifetimeCompletion, true);
            }
            else 
            {
                // AI 1 behavior
                if (Projectile.velocity != Vector2.Zero)
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                if (Projectile.velocity.Y < 16)
                    Projectile.velocity.Y += 0.1f;

                // Only spawn zappy dust when not lingering
                if (Main.rand.NextBool(15))
                {
                    Vector2 dustPostion = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f);
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * 3f + Main.rand.NextVector2Circular(2f, 2f);
                    int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                    Dust dust = Dust.NewDustPerfect(dustPostion, dusType, dustVelocity, Scale: Main.rand.NextFloat(0.3f, 0.5f));
                    dust.noGravity = true;
                }

                // Random arcs that go from the dart to the ground
                if (Main.rand.NextBool(30))
                    RandomLightingArc();
            }

            // Add light
            Vector3 lightColor = new(0.3f, 0.8f, 0.7f);
            Lighting.AddLight(Projectile.Center, lightColor * Projectile.Opacity);
        }

        // Zap nearby enemies on hit
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Zap(Projectile.velocity, target);

        public override bool OnTileCollide(Vector2 oldVelocity)
        {           
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

            // Zap nearby enemies. Pass in old velocity so the particle doesnt go sideways
            // Normal collision behavior doesn't occur if the zap effect triggered
            return !Zap(oldVelocity);
        }

        public override void OnKill(int timeLeft)
        {
            // Prevents particles after the projectile lingers
            if (timeLeft <= 0)
                return;

            // Some zappy dust on kill
            int dustAmount = Main.rand.Next(2, 4);
            for (int i = 0; i < dustAmount; i++)
            {
                Vector2 dustPostion = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f) + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(-5f, 5f);
                Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * 3f + Main.rand.NextVector2Circular(2f, 2f) * 1.4f;
                int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                Dust dust = Dust.NewDustPerfect(dustPostion, dusType, dustVelocity, Scale: Main.rand.NextFloat(0.3f, 0.9f));
                dust.noGravity = true;
            }
        }

        // Cannot deal damage while lingering
        public override bool? CanDamage() => State == AIStates.Lingering ? false : null;


        public bool Zap(Vector2 oldVelocity, NPC cannotHit = null)
        {
            // Find the closest targets
            bool ValidTargetForDummiesDelegate(NPC target, bool canBeChased) => target != cannotHit && target.type == NPCID.TargetDummy;
            bool ValidTargetDelegate(NPC target, bool canBeChased) => target != cannotHit && canBeChased;


            List<NPC> targets = Projectile.FindTargets(SuperchargedDart.ARC_MAX_RANGE, SuperchargedDart.ARC_MAX_TARGETS, true, cannotHit != null && cannotHit.type == NPCID.TargetDummy ? ValidTargetForDummiesDelegate : ValidTargetDelegate);

            // Return false if there are none
            if (targets.Count <= 0)
                return false;

            // Divides damage by the number of targets
            float damageMult = Math.Min(1f / targets.Count * SuperchargedDart.ARC_DAMAGE_MULT, 1f);
            int damage = (int)(Projectile.damage * damageMult);

            // Create a new electrocell projectile
            foreach (NPC target in targets)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<ElectrocellDischarge>(), damage, 6, Projectile.owner, target.whoAmI, Projectile.Center.X, Projectile.Center.Y);

            StartLingering(oldVelocity);
            return true;
        }

        public void StartLingering(Vector2 oldVelocity)
        {
            // Set state to lingering
            State = AIStates.Lingering;

            Projectile.velocity = oldVelocity * 0.1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;

            // Disable collision and normal behaviour
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.netUpdate = true;
        }

        public void RandomLightingArc()
        {
            // Select a random X position
            int randomX = Main.rand.Next(2, 8) * 16;
            randomX *= Main.rand.NextBool() ? -1 : 1;
            Vector2 endPosition = Projectile.Center + Vector2.UnitX * randomX;

            // Stop if the selected position is in a tile
            if (WorldGen.SolidTile(endPosition.ToSafeTileCoordinates()))
                return;

            // Find ground or ceiling. Stop here if either are too far
            int depth = FablesUtils.DepthFromPoint(endPosition);
            int height = FablesUtils.HeightFromPoint(endPosition);
            if (height > 6 && depth > 6)
                return;

            // Spawn zap particle between closest surface and projectile
            bool toFloor = depth <= height;
            endPosition += Vector2.UnitY * 16 * (toFloor ? depth : -height);
            Particle zap = new ElectricArcPrim(Projectile.Center, endPosition, Vector2.UnitY * Main.rand.NextFloat(20f, 50f) * (!toFloor ? 1 : -1), 4f);
            ParticleHandler.SpawnParticle(zap);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> texture = TextureAssets.Projectile[Type];
            SuperchargedDart.Glowmask ??= ModContent.Request<Texture2D>(Texture + "_Glowmask");
            SuperchargedDart.Outline ??= ModContent.Request<Texture2D>(Texture + "_Outline");

            // Recreating default draw position
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 origin = new Vector2(texture.Width() / 2, Projectile.height / 2);

            bool resetSpritebatch = ApplyShader(texture);

            // Draw base projectile
            Main.EntitySpriteDraw(texture.Value, position, null, lightColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0);

            // Get opacity of outline. The glowmask will have the inverse of this opacity
            // Set to zero when lingering so the glowmask is full opacity
            float outlineTransparency = State == AIStates.Lingering ? 0 : MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + Projectile.whoAmI) * 0.5f + 0.5f;

            // Glowmask
            Main.EntitySpriteDraw(SuperchargedDart.Glowmask.Value, position, null, Color.White * (1 - outlineTransparency) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0);

            // Multicolor outline
            if (State == AIStates.Flying)
            {
                Color glowColor = Color.Lerp(new Color(255, 239, 99), Color.White, 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f));
                origin.Y += 2;
                Main.EntitySpriteDraw(SuperchargedDart.Outline.Value, position, null, glowColor * outlineTransparency * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0);
            }

            if (resetSpritebatch)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            return false;
        }

        public bool ApplyShader(Asset<Texture2D> texture)
        {
            float fadeProbability = Utils.GetLerpValue(0.5f, 1f, LifetimeCompletion);

            if (State == AIStates.Lingering && Main.rand.NextFloat() > fadeProbability)
            {
                float electrificationOpacity = Utils.GetLerpValue(15, 13f, LifetimeCompletion, true);
                Rectangle frame = texture.Frame();

                var shader = GameShaders.Armor.GetShaderFromItemId(ModContent.ItemType<Electrocells>());
                Effect effect = shader.Shader;

                effect.Parameters["uColor"].SetValue(new Color(101, 241, 209));
                effect.Parameters["uSecondaryColor"].SetValue(new Color(177, 237, 59));
                effect.Parameters["uImageSize0"].SetValue(texture.Size());
                effect.Parameters["uSourceRect"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
                Main.instance.GraphicsDevice.Textures[1] = AssetDirectory.NoiseTextures.RGB.Value;
                effect.Parameters["uImageSize1"].SetValue(AssetDirectory.NoiseTextures.RGB.Size());
                effect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);

                //Gotta do that for the point sampling
                effect.Parameters["rgbNoise"]?.SetValue(AssetDirectory.NoiseTextures.RGB.Value);
                float electricity = Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly));
                effect.Parameters["glowStrenght"]?.SetValue(electricity + electrificationOpacity);
                effect.Parameters["displaceStrenght"]?.SetValue(electrificationOpacity * 3f);
                effect.Parameters["uOpacity"]?.SetValue(MathF.Pow(electrificationOpacity, 0.25f));

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

                return true;
            }
            return false;
        }
    }
}