namespace CalamityFables.Core
{
    public class TooltipParticleData
    {
        public Vector2 position;
        public Vector2 velocity;
        public float rotation;
        public float rotationSpeed;
        public float scale;
        public float baseScale;

        public Color color;

        public int time;
        public int lifeTime;
        public int TimeLeft => lifeTime - time;

        public TooltipParticleData(Vector2 position, Vector2 velocity, float rotation, float rotationSpeed, float scale, Color color, int lifeTime, ParticleScaleMultiplier scaleMultiplier = null)
        {
            this.time = 0;
            this.lifeTime = lifeTime;
            this.rotation = rotation;
            this.rotationSpeed = rotationSpeed;
            this.position = position;
            this.velocity = velocity;
            this.color = color;

            this.scaleMultiplier = scaleMultiplier;
            if (this.scaleMultiplier == null)
                this.scaleMultiplier = ConstantScale;

            this.baseScale = scale;
            this.scale = scale * scaleMultiplier(this);
        }

        public TooltipParticleData(DrawableTooltipLine line, Vector2 velocity, float rotation, float rotationSpeed, float scale, Color color, int lifeTime, ParticleScaleMultiplier scaleMultiplier = null)
        {
            Vector2 textSize = line.Font.MeasureString(line.Text);
            Vector2 position = Main.rand.NextVector2FromRectangle(new Rectangle(-6, (int)(textSize.Y * 0.25f), (int)textSize.X + 12, (int)(textSize.Y * 0.5f)));

            this.time = 0;
            this.lifeTime = lifeTime;
            this.rotation = rotation;
            this.rotationSpeed = rotationSpeed;
            this.position = position;
            this.velocity = velocity;
            this.color = color;

            this.scaleMultiplier = scaleMultiplier;
            if (this.scaleMultiplier == null)
                this.scaleMultiplier = ConstantScale;

            this.baseScale = scale;
            this.scale = scale * scaleMultiplier(this);
        }

        public static void SimulateParticles(List<TooltipParticleData> particles)
        {
            // Update any active particles.
            for (int i = 0; i < particles.Count; i++)
            {
                TooltipParticleData particle = particles[i];
                particle.position += particle.velocity;

                particle.scale = particle.baseScale * particle.scaleMultiplier(particle);

                // Increase the rotation and time.
                particle.rotation += particle.rotationSpeed;
                particle.time++;
            }

            // Remove any sparkles that have existed long enough.
            particles.RemoveAll((TooltipParticleData s) => s.time >= s.lifeTime);
        }

        public static void DrawParticles(List<TooltipParticleData> particles, Texture2D texture, DrawableTooltipLine line)
        {
            Vector2 linePosition = new Vector2(line.X, line.Y);
            foreach (TooltipParticleData particle in particles)
                Main.spriteBatch.Draw(texture, linePosition + particle.position, null, particle.color, particle.rotation, texture.Size() * 0.5f, particle.scale, SpriteEffects.None, 0f);
        }

        public delegate float ParticleScaleMultiplier(TooltipParticleData particle);
        public ParticleScaleMultiplier scaleMultiplier;

        #region Delegate presets
        public static float ConstantScale(TooltipParticleData particle) => 1f;
        public static float GrowShrinkScale(TooltipParticleData particle)
        {
            // Grow rapidly
            if (particle.time <= 20)
                return particle.time / 20f;

            // Shrink rapidly.
            if (particle.TimeLeft <= 20)
                return particle.TimeLeft / 20f;

            return 1f;
        }
        #endregion
    }
}
