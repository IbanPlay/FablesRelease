namespace CalamityFables.Content.Dusts
{
    public class ElectroDust : ModDust
    {
        public override string Texture => AssetDirectory.Visible;

        public override void OnSpawn(Dust dust)
        {
            dust.scale = Main.rand.NextFloat(0.9f, 1.2f);
            dust.noLight = true;
            dust.noLightEmittence = false;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return Color.White;
        }

        public override bool Update(Dust dust)
        {
            //update position / rotation
            if (!dust.noGravity)
                dust.velocity.Y += 0.1f;
            else
            {
                dust.velocity.Y -= 0.05f;
                if (dust.velocity.Y < -5f)
                    dust.velocity.Y = -5;

                dust.velocity *= 0.985f;
            }

            dust.position += dust.velocity;
            dust.rotation += dust.velocity.Y + dust.velocity.X;

            if (dust.alpha < 80)
                dust.alpha += 6;
            else
                dust.alpha += 2;


            if (dust.alpha > 150)
            {
                dust.active = false;
            }

            if (dust.customData != null && dust.customData is Color)
                dust.color = Color.Lerp(dust.color, (Color)dust.customData, 0.01f);

            dust.scale *= 0.96f;

            if (!dust.noLightEmittence)
            {
                float strength = dust.scale * 1.4f;
                if (strength > 1f)
                {
                    strength = 1f;
                }
                Lighting.AddLight(dust.position, dust.color.ToVector3() * strength * 0.2f);
            }

            if (dust.active)
                NoitaBloomLayer.bloomedDust.Add(new BloomInfo(Color.DodgerBlue, dust.position, dust.scale, dust.alpha));

            return false;
        }
    }

    public class ElectroDustUnstable : ElectroDust
    {
        public override bool Update(Dust dust)
        {
            dust.velocity += Main.rand.NextVector2Circular(1.5f, 1.5f) * Utils.GetLerpValue(0f, 150f, dust.alpha, true);

            return base.Update(dust);
        }
    }
}
