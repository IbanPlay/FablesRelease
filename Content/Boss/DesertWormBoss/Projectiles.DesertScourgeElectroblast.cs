using CalamityFables.Content.Dusts;
using CalamityFables.Content.NPCs.Desert;
using Terraria.DataStructures;
using Terraria.Localization;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class DesertScourgeElectroblast : ModProjectile, ICustomDeathMessages
    {
        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("a voltful blast");
        }

        public static int BlastTime => 28;
        public float Completion => (BlastTime - Projectile.timeLeft) / (float)BlastTime;

        public float BlastRadius => Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.width = 350;
            Projectile.height = 350;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = BlastTime;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => AABBvCircle(targetHitbox, Projectile.Center, BlastSize * BlastRadius);
        public override bool CanHitPlayer(Player target) => AABBvCircle(target.Hitbox, Projectile.Center, 0.9f * BlastSize * BlastRadius);

        public override void AI()
        {
            Color[] prettyColors = new Color[] { Color.DodgerBlue, Color.HotPink, Color.Orange };
            Lighting.AddLight(Projectile.Center, new Vector3(200, 210, 400) * 0.02f * (1 - Completion));

            if (Projectile.timeLeft == BlastTime)
            {
                //SoundEngine.PlaySound(SoundID.Item93 with { Volume = 1f }, Projectile.Center);
                //SoundEngine.PlaySound(SoundID.NPCHit44 with { Volume = 1f }, Projectile.Center);

                int tinyDustCount = (int)(10 + 30 * Utils.GetLerpValue(100, 400, BlastRadius * 2, true));

                for (int i = 0; i < tinyDustCount / 2; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(BlastRadius, BlastRadius);
                    Dust dus = Dust.NewDustPerfect(dustPos, 156, (dustPos - Projectile.Center) * 0.1f, 30);
                    dus.noGravity = true;
                    dus.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    dus.velocity = (dustPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                }

                for (int i = 0; i < tinyDustCount; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(BlastRadius, BlastRadius);
                    int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();
                    Dust dus = Dust.NewDustPerfect(dustPos, dusType, (dustPos - Projectile.Center) * 0.1f, Main.rand.Next(30, 60));
                    dus.noGravity = true;
                    dus.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    dus.velocity = (dustPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                }
            }

            if (Main.rand.NextBool(2) && Completion < 0.5f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(BlastRadius, BlastRadius) * BlastSize;
                    int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                    Dust dus = Dust.NewDustPerfect(dustPos, dusType, (dustPos - Projectile.Center) * 0.1f, 30);
                    dus.noGravity = true;
                    dus.scale = Main.rand.NextFloat(0.8f, 1.2f) * (1 - (Completion / 0.5f));

                    dus.velocity = (dustPos - Projectile.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 4f);

                    dus.customData = Main.rand.Next(prettyColors);
                }
            }

        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if (Completion > 0.8f)
                modifiers.IncomingDamageMultiplier *= 0.2f + 0.8f * (Completion - 0.8f) / 0.2f;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.velocity += (target.Center - Projectile.Center).SafeNormalize(-Vector2.UnitY) * (3f + 5f * Utils.GetLerpValue(100f, 400f, BlastRadius * 2, true));
        }

        public CurveSegment ExpandFast = new CurveSegment(PolyOutEasing, 0f, 0f, 1f, 3);
        public CurveSegment Unbounce = new CurveSegment(SineInEasing, 0.8f, 1f, -0.2f);
        public CurveSegment Shrink = new CurveSegment(SineInOutEasing, 0.95f, 0.8f, -0.8f);

        internal float BlastSize => PiecewiseAnimation(Completion, new CurveSegment[] { ExpandFast, Unbounce, Shrink });


        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = Scene["ElectroOrb"].GetShader().Shader;
            Texture2D pebbleNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "PebblesNoise").Value;
            Texture2D zapNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "LightningNoise").Value;
            Texture2D ligjht = AssetDirectory.CommonTextures.PixelBloomCircle.Value;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            float size = BlastSize * BlastRadius * 2;
            float coreRadiusPercent = Math.Max(20f / size, 0.2f);
            float colorMult = (float)Math.Pow(1 - Completion, 0.2f);
            float coreColorMult = (float)Math.Pow(1 - Completion, 0.05f);

            Vector2 resolution = new Vector2(BlastRadius);
            Main.spriteBatch.Draw(ligjht, Projectile.Center - Main.screenPosition, null, Color.Black * 0.4f * colorMult, 0, ligjht.Size() / 2f, 1.3f * size / (float)ligjht.Width, SpriteEffects.None, 0f);

            Vector4 coreColor = new Vector4(0.9f, 1.0f, 1.2f, 0.9f) * coreColorMult;
            Vector4 zapColor = new Vector4(0.1f, 0.16f, 0.26f, 1f) * coreColorMult;
            Vector4 edgeColor = new Vector4(0.3f, 0.5f, 0.85f, 1.0f) * colorMult;

            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 4.24f);
            effect.Parameters["zapTexture"].SetValue(zapNoise);
            effect.Parameters["resolution"].SetValue(resolution);
            effect.Parameters["coreColor"].SetValue(coreColor);
            effect.Parameters["edgeColor"].SetValue(edgeColor);
            effect.Parameters["zapColor"].SetValue(zapColor);
            effect.Parameters["blowUpSize"].SetValue(0.4f);
            effect.Parameters["maxRadius"].SetValue((0.95f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly)) * BlastSize);
            effect.Parameters["coreSolidRadius"].SetValue(coreRadiusPercent);
            effect.Parameters["coreFadeRadius"].SetValue(coreRadiusPercent * 0.25f);
            effect.Parameters["fresnelStrenght"].SetValue(7f + 7f * (float)Math.Pow(Completion, 4f));

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(pebbleNoise, Projectile.Center - Main.screenPosition, null, Color.White, 0, pebbleNoise.Size() / 2f, (BlastRadius * 2) / (float)pebbleNoise.Width, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, (Color.RoyalBlue with { A = 0 }) * coreColorMult, 0, bloom.Size() / 2f, 1.6f * size / (float)bloom.Width, SpriteEffects.None, 0f);

            return false;
        }

        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            customDeath.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.DesertScourgeElectroblast." + Main.rand.Next(1, 4).ToString()).ToNetworkText(player.name);
            return true;
        }
    }
}
