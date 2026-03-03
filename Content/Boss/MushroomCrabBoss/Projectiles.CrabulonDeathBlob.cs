using CalamityFables.Particles;
using ReLogic.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    /*
    public class CrabulonDeathBlob : ModProjectile
    {
        public override string Texture => AssetDirectory.Crabulon + "Gores/" + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ooze");
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 660;
            Projectile.tileCollide = true;
            Projectile.scale = Main.rand.NextFloat(0.9f, 1.1f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity.X *= 0.3f;
            return false;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;
            Projectile.velocity.X *= 0.987f;

            if (Main.rand.NextBool(27))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 15f) - Vector2.UnitY * 14f, DustID.GlowingMushroom, -Vector2.UnitY);
                d.noLightEmittence = true;
            }

            Lighting.AddLight(Projectile.Center, CommonColors.MushroomDeepBlue.ToVector3() * 0.8f);
        }

        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 squish = new Vector2(1f - MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + Projectile.whoAmI) * 0.1f, 1f + MathF.Sin(Main.GlobalTimeWrappedHourly *3f + 0.3f + Projectile.whoAmI) * 0.1f);

            //Fissure
            Effect effect = Scene["CrabulonFissure"].GetShader().Shader;
            effect.Parameters["brightnessShift"].SetValue(Main.GlobalTimeWrappedHourly + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.2f + Projectile.whoAmI * 0.17f);
            effect.Parameters["segments"].SetValue(7);
            effect.Parameters["gradientScaleMultiplier"].SetValue(0.5f);
            effect.Parameters["lightColor"].SetValue(Color.Lerp(Color.Blue, lightColor, 0.75f).ToVector4());
            effect.Parameters["brightnessStep"].SetValue(0.1f);

            effect.Parameters["colors"].SetValue(Crabulon.GradientMapColors);
            effect.Parameters["brightnesses"].SetValue(Crabulon.GradientMapBrightnesses);
            effect.Parameters["globalMultiplyColor"].SetValue(Vector3.One);

            effect.Parameters["noiseTexture"].SetValue(Main.Assets.Request<Texture2D>("Images/Misc/noise").Value);
            effect.Parameters["noiseScale"].SetValue(new Vector2(0.33f, 0.1f));
            effect.Parameters["noiseScroll"].SetValue(Main.GlobalTimeWrappedHourly * -1f);
            effect.Parameters["noiseColor"].SetValue(new Vector3(3f, 4f, 12f));
            effect.Parameters["noiseTreshold"].SetValue(0.5f);
            effect.Parameters["noisePower"].SetValue(4f);
            effect.Parameters["depthColor"].SetValue(new Vector3(0.1f, 0.2f, 0.3f));

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.Transform);

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height - 4);
            Main.EntitySpriteDraw(tex, Projectile.Bottom - Main.screenPosition, null, lightColor, 0f, origin, Projectile.scale * squish, 0, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            tex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "Gores/" + Name + "Outline").Value;
            Main.EntitySpriteDraw(tex, Projectile.Bottom - Main.screenPosition, null, lightColor * 0.5f, 0f, origin, Projectile.scale * squish, 0, 0);
            
            tex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "Gores/" + Name + "Fresnel").Value;
            Color glowColor = Color.RoyalBlue with { A = 0 } * 0.5f;
            Main.EntitySpriteDraw(tex, Projectile.Bottom - Main.screenPosition, null, glowColor, 0f, origin, Projectile.scale * squish, 0, 0);


            return false;
        }
    
    }
    */
}
