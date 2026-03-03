namespace CalamityFables.Particles
{
    public class NeurotoxinMist : ExplosionSmoke
    {
        public float dustSpawnRate;

        public NeurotoxinMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float rotationSpeed = 0, bool lighted = false, float dustSpawnRate = 1 / 16f) : base(position, velocity, colorFire, colorFade, scale, rotationSpeed, lighted)
        {
            this.dustSpawnRate = dustSpawnRate;
        }

        public NeurotoxinMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, Color colorEnd, float scale, float rotationSpeed = 0, bool lighted = false, float dustSpawnRate = 1 / 16f) : base(position, velocity, colorFire, colorFade, colorEnd, scale, rotationSpeed, lighted)
        {
            this.dustSpawnRate = dustSpawnRate;
        }

        public override void Update()
        {
            base.Update();

            // Spawn chance. Decreases the longer the particle exists
            float spawnChance = dustSpawnRate * MathHelper.Lerp(1f, 0.1f, alpha / 255f);

            // Spawn glowy dust
            if (Main.rand.NextFloat() < spawnChance)
            {
                Vector2 particlePosition = Position + Main.rand.NextVector2Circular(10f, 10f) * Scale;
                Vector2 particleVelocity = Main.rand.NextVector2Circular(1f, 1f);

                Particle glowies = new BloomParticle(particlePosition, particleVelocity, ColorFire, ColorEnd * 0.25f, Main.rand.NextFloat(0.75f, 1f), Main.rand.NextFloat(0.8f, 1f)) { Pixelated = true };
                ParticleHandler.SpawnParticle(glowies);
            }
        }
    }

    public class RefreshingMist : ExplosionSmoke
    {
        public float dustSpawnRate;

        public RefreshingMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float rotationSpeed = 0, bool lighted = false, float dustSpawnRate = 1 / 16f) : base(position, velocity, colorFire, colorFade, scale, rotationSpeed, lighted)
        {
            this.dustSpawnRate = dustSpawnRate;
        }

        public RefreshingMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, Color colorEnd, float scale, float rotationSpeed = 0, bool lighted = false, float dustSpawnRate = 1 / 16f) : base(position, velocity, colorFire, colorFade, colorEnd, scale, rotationSpeed, lighted)
        {
            this.dustSpawnRate = dustSpawnRate;
        }

        public override void Update()
        {
            base.Update();

            // Spawn chance. Decreases the longer the particle exists
            float spawnChance = dustSpawnRate * MathHelper.Lerp(1f, 0.1f, alpha / 255f);

            // Spawn healing dust
            if (Main.rand.NextFloat() < spawnChance)
            {
                Vector2 dustPosition = Position + Main.rand.NextVector2Circular(10f, 10f) * Scale;
                Vector2 dustVelocity = Main.rand.NextVector2Circular(1f, 1f);
                Color dustColor = Color.Lerp(Color * (255 / (float)Color.A), Main.rand.NextBool() ? Color.MediumSpringGreen : Color.Turquoise, 0.7f);
                float dustScale = Main.rand.NextFloat(1.5f, 2.75f);

                Dust sparks = Dust.NewDustPerfect(dustPosition, ModContent.DustType<BlowpipeHealingMistDust>(), dustVelocity, Main.rand.Next(110), dustColor, dustScale);
                sparks.noGravity = true;
                sparks.customData = ColorFade;
            }
        }
    }
}

#region Old Versions
    /*
    public class NeurotoxinMist : LingeringExplosionSmoke
    {
        public NeurotoxinMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float rotationSpeed = 0, bool lighted = false) : base(position, velocity, colorFire, colorFade, scale, rotationSpeed, lighted)
        {
        }

        public NeurotoxinMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, Color colorEnd, float scale, float rotationSpeed = 0, bool lighted = false) : base(position, velocity, colorFire, colorFade, colorEnd, scale, rotationSpeed, lighted)
        {
        }

        public override void Update()
        {
            base.Update();

            if (Main.rand.NextFloat() < (1 / 16f) * (0.3f + 0.7f * (1 - (base.alpha / 255f))))
            {
                Dust sparks = Dust.NewDustPerfect(Position + Main.rand.NextVector2Circular(10f, 10f) * Scale, ModContent.DustType<BlowpipeNeurotoxinDust>(), Main.rand.NextVector2CircularEdge(10f, 10f), newColor: Color.DodgerBlue);
                sparks.velocity = Main.rand.NextVector2Circular(1f, 1f);
                sparks.noGravity = true;
                sparks.customData = Color.DodgerBlue;
                sparks.scale = Main.rand.NextFloat(0.5f, 0.75f);
                Color unAlphaColor = Color.Lerp(Color * (255 / (float)Color.A), Color.Turquoise, 0.7f);
                sparks.color = Color.Lerp(Color.White, unAlphaColor, 0.5f + 0.5f * Main.rand.NextFloat());
                sparks.alpha = Main.rand.Next(110);
            }

        }
    }

    public class RefreshingMist : LingeringExplosionSmoke
    {
        public float dustSpawnRate;

        public RefreshingMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float rotationSpeed = 0, bool lighted = false, float dustSpawnRate = 1 / 16f) : base(position, velocity, colorFire, colorFade, scale, rotationSpeed, lighted)
        {
            this.dustSpawnRate = dustSpawnRate;
        }

        public RefreshingMist(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, Color colorEnd, float scale, float rotationSpeed = 0, bool lighted = false, float dustSpawnRate = 1 / 16f) : base(position, velocity, colorFire, colorFade, colorEnd, scale, rotationSpeed, lighted)
        {
            this.dustSpawnRate = dustSpawnRate;
        }

        public override void Update()
        {
            base.Update();

            if (Main.rand.NextFloat() < dustSpawnRate * (0.3f + 0.7f * (1 - (base.alpha / 255f))))
            {
                Dust sparks = Dust.NewDustPerfect(Position + Main.rand.NextVector2Circular(10f, 10f) * Scale, ModContent.DustType<BlowpipeHealingMistDust>(), Main.rand.NextVector2CircularEdge(10f, 10f));
                sparks.velocity = Main.rand.NextVector2Circular(1f, 1f);
                sparks.noGravity = true;
                sparks.customData = Color.DodgerBlue;
                sparks.scale = Main.rand.NextFloat(1.5f, 2.75f);
                Color unAlphaColor = Color.Lerp(Color * (255 / (float)Color.A), Main.rand.NextBool() ? Color.MediumSpringGreen : Color.Turquoise, 0.7f);
                sparks.color = Color.Lerp(Color.White, unAlphaColor, 0.5f + 0.5f * Main.rand.NextFloat());
                sparks.alpha = Main.rand.Next(110);
            }
        }
    }
    */
#endregion