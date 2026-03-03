using CalamityFables.Particles;
using System.Runtime.Serialization;
using Terraria.ModLoader.Core;

namespace CalamityFables.Core
{
    public class ParticleHandler : ModSystem
    {
        internal static List<Particle> particles;
        //List containing the particles to delete
        private static List<Particle> particlesToKill;
        //List containing the particles to add
        private static List<Particle> particlesToSpawn;

        //Static list for details concerning every particle type
        internal static Dictionary<Type, int> particleTypes;
        internal static Dictionary<int, Asset<Texture2D>> particleTextures;
        private static List<Particle> particleInstances;
        //Lists used when drawing particles batched
        private static List<Particle> batchedAlphaBlendParticles;
        private static List<Particle> batchedNonPremultipliedParticles;
        private static List<Particle> batchedAdditiveBlendParticles;

        private static bool updatingParticles = false;

        public static void LoadModParticleInstances(Mod mod)
        {
            Type baseParticleType = typeof(Particle);
            foreach (Type type in AssemblyManager.GetLoadableTypes(mod.Code))
            {
                if (type.IsSubclassOf(baseParticleType) && !type.IsAbstract && type != baseParticleType)
                {
                    int ID = particleTypes.Count; //Get the ID of the particle
                    particleTypes[type] = ID;

                    Particle instance = (Particle)FormatterServices.GetUninitializedObject(type);
                    particleInstances.Add(instance);

                    string texturePath = type.Namespace.Replace('.', '/') + "/" + type.Name;
                    if (instance.Texture != "")
                        texturePath = instance.Texture;
                    particleTextures[ID] = ModContent.Request<Texture2D>(texturePath);
                }
            }
        }

        public override void Load()
        {
            if (Main.dedServ)
                return;

            On_Main.DrawInfernoRings += DrawForegroundParticles;
            FablesDrawLayers.DrawThingsAboveSolidTilesEvent += DrawOverTileParticles;

            particles = new List<Particle>();
            particlesToKill = new List<Particle>();
            particlesToSpawn = new List<Particle>();
            particleTypes = new Dictionary<Type, int>();
            particleTextures = new Dictionary<int, Asset<Texture2D>>();
            particleInstances = new List<Particle>();

            batchedAlphaBlendParticles = new List<Particle>();
            batchedNonPremultipliedParticles = new List<Particle>();
            batchedAdditiveBlendParticles = new List<Particle>();

            LoadModParticleInstances(CalamityFables.Instance);
        }


        public override void Unload()
        {
            if (Main.dedServ)
                return;

            Terraria.On_Main.DrawInfernoRings -= DrawForegroundParticles;

            particles = null;
            particlesToKill = null;
            particlesToSpawn = null;
            particleTypes = null;
            particleTextures = null;
            particleInstances = null;
            batchedAlphaBlendParticles = null;
            batchedNonPremultipliedParticles = null;
            batchedAdditiveBlendParticles = null;
        }

        public override void PostUpdateEverything()
        {
            if (!Main.dedServ)
                Update();
        }
        private static void DrawForegroundParticles(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            DrawAllParticles(Main.spriteBatch, true);
            orig(self);
        }


        private void DrawOverTileParticles()
        {
            DrawAllParticles(Main.spriteBatch, false);
        }



        /// <summary>
        /// Spawns the particle instance provided into the world. If the particle limit is reached but the particle is marked as important, it will try to replace a non important particle.
        /// </summary>
        public static void SpawnParticle(Particle particle)
        {
            // Don't queue particles if the game is paused.
            // This precedent is established with how Dust instances are created.
            //Don't spawn particles if on the server side either, or if the particles dict is somehow null
            if (Main.gamePaused || Main.dedServ || particles == null)
                return;

            if (updatingParticles)
            {
                particlesToSpawn.Add(particle);
                particle.Type = particleTypes[particle.GetType()];
                return;
            }

            //if (particles.Count >= CalamityFablesConfig.Instance.ParticleLimit && !particle.Important)
            //   return;

            particles.Add(particle);
            particle.active = true;
            particle.Type = particleTypes[particle.GetType()];
        }

        public static void Update()
        {
            updatingParticles = true;

            foreach (Particle particle in particles)
            {
                if (particle == null)
                    continue;
                particle.Position += particle.Velocity;
                particle.Time++;
                particle.Update();

                particle.Center = particle.Position;
                particle.velocity = particle.Velocity;

                if (particle.Time >= particle.Lifetime && particle.SetLifetime)
                    particle.active = false;
            }
            updatingParticles = false;

            //Clear out particles whose time is up
            particles.RemoveAll(particle => (particle.Time >= particle.Lifetime && particle.SetLifetime) || particlesToKill.Contains(particle));
            foreach (Particle particle in particlesToKill)
                particle.active = false;
            particlesToKill.Clear();

            particles.AddRange(particlesToSpawn);
            particlesToSpawn.Clear();
        }

        public static void RemoveParticle(Particle particle)
        {
            particlesToKill.Add(particle);
        }

        public static void DrawAllParticles(SpriteBatch sb, bool frontLayer)
        {
            if (particles.Count == 0)
                return;

            if (frontLayer)
                sb.End();

            //Batch the particles to avoid constant restarting of the spritebatch
            foreach (Particle particle in particles)
            {
                if (particle == null || particle.FrontLayer != frontLayer)
                    continue;

                if (particle.UseAdditiveBlend)
                    batchedAdditiveBlendParticles.Add(particle);
                else if (particle.UsesNonPremultipliedAlpha)
                    batchedNonPremultipliedParticles.Add(particle);
                else
                    batchedAlphaBlendParticles.Add(particle);
            }

            DrawParticleBatch(sb, batchedAlphaBlendParticles, BlendState.AlphaBlend);
            DrawParticleBatch(sb, batchedNonPremultipliedParticles, BlendState.NonPremultiplied);
            DrawParticleBatch(sb, batchedAdditiveBlendParticles, BlendState.Additive);

            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            batchedAlphaBlendParticles.Clear();
            batchedNonPremultipliedParticles.Clear();
            batchedAdditiveBlendParticles.Clear();

            if (frontLayer)
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }

        public static void DrawParticleBatch(SpriteBatch sb, List<Particle> batch, BlendState blend)
        {
            if (batch.Count <= 0)
                return;

            sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (Particle particle in batch)
            {
                if (particle.UseCustomDraw)
                    particle.CustomDraw(sb, Main.screenPosition);
                else
                {
                    Rectangle frame = particleTextures[particle.Type].Frame(1, particle.FrameVariants, 0, particle.Variant);
                    sb.Draw(particleTextures[particle.Type].Value, particle.Position - Main.screenPosition, frame, particle.Color, particle.Rotation, frame.Size() * 0.5f, particle.Scale, SpriteEffects.None, 0f);
                }
            }

            sb.End();
        }

        /// <summary>
        /// Gives you the amount of particle slots that are available. Useful when you need multiple particles at once to make an effect and dont want it to be only halfway drawn due to a lack of particle slots
        /// </summary>
        /// <returns></returns>
        public static int FreeSpacesAvailable()
        {
            //Safety check
            if (Main.dedServ || particles == null)
                return 0;

            return FablesConfig.Instance.ParticleLimit - particles.Count();
        }

        /// <summary>
        /// Gives you the texture of the particle type. Useful for custom drawing
        /// </summary>
        public static Texture2D GetTexture(int type) => particleTextures[type].Value;

        private static string noteToEveryone = "This particle system was inspired by spirit mod's own particle system, with permission granted by Yuyutsu. Love you spirit mod! -Iban";
    }
}
