using System.Runtime.Serialization;
using static Terraria.GameContent.TextureAssets;

namespace CalamityFables.Core
{
    public abstract class RTParticle
    {
        /// <summary>
        /// The ID of the particle inside of the general particle handler's array. This is set automatically when the particle is created
        /// </summary>
        public int ID;

        /// <summary>
        /// The ID of the particle type as registered by the general particle handler. This is set automatically when the particle handler loads
        /// </summary>
        public int Type;

        /// <summary>
        /// The instance of <see cref="ParticleRenderTarget"/> that this particle is assigned to. The particle will be drawn using the instance.
        /// </summary>
        public ParticleRenderTarget AssignedTarget;

        /// <summary>
        /// The amount of frames this particle has existed for. You shouldn't have to touch this manually.
        /// </summary>
        public int Time;

        /// <summary>
        /// The maximum number of frames this particle will be active for. If this is set to zero, the particle will never expire due to age.
        /// </summary>
        public int Lifetime = 0;

        /// <summary>
        /// Provides a value between 0-1 that represents the completion of this particle's lifetime
        /// </summary>
        public float Progress => Lifetime > 0 ? Time / (float)Lifetime : 0;

        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Rotation;
        public float Scale;

        /// <summary>
        /// This particle's number of horizontal and vertical frames.
        /// </summary>
        public Point Frames = new(1, 1);

        /// <summary>
        /// This particle's current frame.
        /// </summary>
        public Point Frame = new(0, 0);

        /// <summary>
        /// if the particle has been killed. Helps avoid scenarios where a particle that's been already killed this frame dies again
        /// </summary>
        public bool Killed = false;

        /// <summary>
        /// Determines which layer this particle will be drawn to. <br/>
        /// The usage of this field depends on it's usage in the <see cref="ParticleRenderTarget"/> this particle is assigned to.
        /// </summary>
        public DrawhookLayer Layer { get; set; } = DrawhookLayer.AboveNPCs;

        /// <summary>
        /// The texture path override of the particle
        /// </summary>
        public virtual string Texture => "";

        /// <summary>
        /// The request mode for the texture autoload. leave as <see cref="AssetRequestMode.AsyncLoad"/> by default to improve load times, and only use <see cref="AssetRequestMode.ImmediateLoad"/> if you need the texture as soon as it spawns
        /// </summary>
        public virtual AssetRequestMode TextureRequestMode => AssetRequestMode.AsyncLoad;

        public Texture2D ParticleTexture => RTParticleHandler.GetTexture(Type);

        /// <summary>
        /// Called every frame, can be used to update the particle's behavior. <br/>
        /// By default, adds velocity to position and increments time
        /// </summary>
        public virtual void Update()
        {
            Time++;
            Position += Velocity;
        }

        /// <summary>
        /// Draws this particle to it's rendertarget. Always runs with default behavior if not overriden.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="basePosition"></param>
        /// <param name="baseColor"></param>
        public virtual void DrawParticle(SpriteBatch spriteBatch, Vector2 basePosition, Color baseColor)
        {
            Texture2D texture = ParticleTexture;
            Rectangle frame = texture.Frame(Frames.X, Frames.Y, Frame.X, Frame.Y);

            spriteBatch.Draw(texture, Position - basePosition, frame, Color.MultiplyRGB(baseColor), 0, frame.Size() / 2, Scale, 0, 0);
        }

        /// <summary>
        /// Used to remove this particle from the world.
        /// </summary>
        public void Kill()
        {
            RTParticleHandler.RemoveParticle(this);
            Killed = true;
        }
    }

    /// <summary>
    /// Handles custom render target behavior for particles. <para/>
    /// Particles can be assigned to instances of this object when they're spawned with <see cref="RTParticleHandler.SpawnParticle{T}(RTParticle, T)"/>. <br/>
    /// The assigned partcles will be automatically drawn to its assigned RT and the RT will be initialized and disposed as particles are spawned and killed.
    /// </summary>
    public abstract class ParticleRenderTarget
    {
        /// <summary>
        /// Determines if this <see cref="ParticleRenderTarget"/> is registered on loading. <para/>
        /// Registering on load makes an instance of this target accessible at all times. <br/>
        /// Registration should be turned off if this target is only assigned to and drawn manually.
        /// </summary>
        public virtual bool RegisterOnLoad => true;

        public bool AutoTarget = false;

        /// <summary>
        /// The number of ticks this <see cref="ParticleRenderTarget"/> will remain Initialized while not in use before being disposed.
        /// </summary>
        public virtual int AutoDisposeTime => 120;

        /// <summary>
        /// The current position at which this render target will be drawn.
        /// </summary>
        public virtual Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// The dimensions of this render target in pixels.
        /// </summary>
        public virtual Point Size { get; set; }

        /// <summary>
        /// The scale at which this render target will be drawn. <br/>
        /// Typically set to 2 to make anything drawn to this target pixelated.
        /// </summary>
        public virtual float Scale { get; set; } = 1f;

        public virtual Vector2 Origin { get; set; } = Vector2.Zero;

        public virtual float Opacity { get; set; } = 1f;

        public bool Initialized = false;
        public List<RTParticle> AssignedParticles;
        public int TimeSinceLastUpdate = 0;
        public bool NeedsManualParticleUpdate = false;

        private Dictionary<DrawhookLayer, int> ParticlesPerLayer;

        /// <summary>
        /// Initializes <see cref="AssignedParticles"/>, sets <see cref="Initialized"/> to true, and calls <see cref="Initialize"/>.
        /// </summary>
        public void InitializeRenderTarget()
        {
            AssignedParticles = [];
            ParticlesPerLayer = [];
            foreach (var kvp in ParticlesPerLayer)
                ParticlesPerLayer[kvp.Key] = 0;

            InitializeFields();
            Initialized = true;
            Initialize();
        }

        /// <summary>
        /// Ran on initialization. Can be used to update fields such as size, scale, and opacity. <br/>
        /// For particles registered on load, size must be updated. <br/>
        /// Assumes all targets with <see cref="RegisterOnLoad"/> set to true are fullscreen and sets their size to the screen size.
        /// </summary>
        public virtual void InitializeFields()
        {
            if (RegisterOnLoad)
                Size = Main.ScreenSize;
        }

        /// <summary>
        /// Clears <see cref="AssignedParticles"/>, sets <see cref="Initialized"/> to false, and calls <see cref="Dispose"/>.
        /// </summary>
        public void DisposeRenderTarget()
        {
            AssignedParticles = null;
            ParticlesPerLayer = null;
            Initialized = false;
            Dispose();
        }

        /// <summary>
        /// Used to Initialize the render targets managed by this <see cref="ParticleRenderTarget"/>.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Used to Dispose the render targets managed by this <see cref="ParticleRenderTarget"/>.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Called whenever the screen is resized. Re-initializes fullscreen targets to match screen size. <br/>
        /// Assumes all targets with <see cref="RegisterOnLoad"/> set to true are fullscreen and re-initializes them automatically.
        /// </summary>
        public virtual void ResizeTarget()
        {
            if (RegisterOnLoad)
            {
                // Does not clear assigned particles or particles per layer
                Dispose();
                InitializeFields();
                Initialize();
            }
        }

        public void SpawnParticle(RTParticle particle) => RTParticleHandler.SpawnParticle(particle, this);

        /// <summary>
        /// Don't use this to spawn particles!
        /// </summary>
        /// <param name="particle"></param>
        public void AddParticleToTarget(RTParticle particle)
        {
            AssignedParticles.Add(particle);

            if (!ParticlesPerLayer.ContainsKey(particle.Layer))
                ParticlesPerLayer[particle.Layer] = 0;

            ParticlesPerLayer[particle.Layer]++;
        }

        public void RemoveParticleFromTarget(RTParticle particle)
        {
            AssignedParticles.Remove(particle);

            //??? should never happen but just in case
            if (!ParticlesPerLayer.ContainsKey(particle.Layer))
                return;

            ParticlesPerLayer[particle.Layer]--;
        }

        public void GoUndead(DrawhookLayer drawLayerMask)
        {
            if (undead)
                return;
            undead = true;
            undeadDrawLayer = drawLayerMask;
            RTParticleHandler.UndeadTargetCount++;
        }

        public bool IsUndead => undead;

        private bool undead = false;
        private DrawhookLayer undeadDrawLayer = (DrawhookLayer)0;


        /// <summary>
        /// Used to determine if the render target should be drawn to the current layer. <br/>
        /// By default, prevents drawing if there are no particles to draw on the current layer. <br/>
        /// Should be overriden to return false for layers that will never be drawn.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public virtual bool ShouldDrawOnLayer(DrawhookLayer layer)
        {
            if (undead)
                return AssignedParticles.Count > 0 && (undeadDrawLayer & layer) != 0;

            return ParticlesPerLayer.ContainsKey(layer) && ParticlesPerLayer[layer] > 0;
        }

        /// <summary>
        /// Used to draw this<see cref="ParticleRenderTarget"/>. Called automatically if this target is registered on loading. Otherwise, it should be called for manual drawing. <br/>
        /// The target's position, size, scale, and origin are fields, rather than parameters for this method.
        /// </summary>
        /// <param name="spritebatch"></param>
        /// <param name="layer"></param>
        /// <param name="source"></param>
        public abstract void DrawRenderTarget(SpriteBatch spritebatch, DrawhookLayer layer = DrawhookLayer.AbovePlayer, Rectangle? source = null);

        /// <summary>
        /// Kills all assigned particles and disposes of this <see cref="ParticleRenderTarget"/>.
        /// </summary>
        public void ClearAndDisposeRenderTarget()
        {
            // Kill all particles
            for (int i = AssignedParticles.Count - 1; i >= 0; i--)
                AssignedParticles[i].Kill();

            // Dispose of target
            DisposeRenderTarget();
        }

        public delegate void ParticleActionDelegate(RTParticle particle);
        /// <summary>
        /// Runs before the particle from this target gets updated <br/>
        /// Use this event to set values right before particles are updated, for example if you want to impart a velocity boost onto them
        /// </summary>
        public event ParticleActionDelegate PreUpdateParticlesEvent;
        public void PreUpdateParticle(RTParticle particle) => PreUpdateParticlesEvent?.Invoke(particle);

        /// <summary>
        /// Runs after all particles have been updated <br/>
        /// Use this to reset values you used for <see cref="PreUpdateParticlesEvent"/>
        /// </summary>
        public event Action PostUpdateParticlesEvent;
        public void PostUpdate() => PostUpdateParticlesEvent?.Invoke();

        public void ManuallyUpdateTarget()
        {
            if (!NeedsManualParticleUpdate)
                return;

            TimeSinceLastUpdate = 0;

            for (int i = AssignedParticles.Count - 1; i >= 0; i--)
            {
                var particle = AssignedParticles[i];

                //How???
                if (particle is null)
                {
                    AssignedParticles.RemoveAt(i);
                    continue;
                }

                PreUpdateParticle(particle);
                if (particle.Killed)
                    continue;
                particle.Update();
                if (particle.Killed)
                    continue;

                if (particle.Lifetime > 0 && particle.Time > particle.Lifetime)
                    particle.Kill();
            }

            PostUpdate();
        }
    }

    /// <summary>
    /// System that both handles the simulation of particles on a rendertarget, and the auto-disposal of rendertargets, while at the same time handling automatically instanced rendertargets
    /// </summary>
    public class RTParticleHandler : ModSystem
    {
        internal static List<RTParticle> RTParticles;
        internal static Dictionary<Type, int> RTParticleTypes;
        internal static Dictionary<int, Asset<Texture2D>> RTParticleTextures;

        internal static Dictionary<Type, ParticleRenderTarget> AutoTargets;
        internal static List<ParticleRenderTarget> ManualRenderTargets;

        internal static int UndeadTargetCount = 0;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTargets;
            FablesDrawLayers.DrawBehindDustEvent += DrawAbovePlayers;
            FablesDrawLayers.DrawAboveProjectilesEvent += DrawAboveProjectiles;
            FablesDrawLayers.DrawThingsAboveNPCsEvent += DrawAboveNPCs;
            FablesDrawLayers.DrawThingsAboveSolidTilesEvent += DrawAboveTiles;
            FablesDrawLayers.DrawThingsBehindSolidTilesEvent += DrawBehindTiles;

            RTParticles = [];
            RTParticleTypes = [];
            RTParticleTextures = [];
            AutoTargets = [];
            ManualRenderTargets = [];
            UndeadTargetCount = 0;

            RegisterParticlesAndTargets(CalamityFables.Instance);
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            RTParticles = null;
            RTParticleTextures = null;

            DisposeRenderTargets();

            AutoTargets = null;
            ManualRenderTargets = null;
        }

        private static void RegisterParticlesAndTargets(Mod mod)
        {
            // Loads each particle type
            Type baseParticleType = typeof(RTParticle);
            Type baseTargetType = typeof(ParticleRenderTarget);

            foreach (Type type in mod.Code.GetTypes())
            {
                if (type.IsAbstract)
                    continue;

                if (type.IsSubclassOf(baseParticleType) && type != baseParticleType)
                {
                    // Fill type ID dictionary
                    int ID = RTParticleTypes.Count;
                    RTParticleTypes[type] = ID;

                    // Fill textures dictionary
                    RTParticle instance = (RTParticle)FormatterServices.GetUninitializedObject(type);

                    string texturePath = type.Namespace.Replace('.', '/') + "/" + type.Name;
                    if (instance.Texture != "")
                        texturePath = instance.Texture;
                    RTParticleTextures[ID] = ModContent.Request<Texture2D>(texturePath, instance.TextureRequestMode);
                }

                // Fill dictionary with new instances of each render target
                if (type.IsSubclassOf(baseTargetType) && type != baseTargetType)
                {
                    // Initialize target using parameterless constructor, then check if it should be registered on load
                    ParticleRenderTarget target = (ParticleRenderTarget)Activator.CreateInstance(type);
                    if (!target.RegisterOnLoad)
                        continue;

                    target.AutoTarget = true;
                    AutoTargets[type] = target;
                }
            }
        }

        public override void PreUpdateItems() 
        {
            if (Main.dedServ)
                return;

            //Main.NewText(RenderTargets.Count + ", " + RTParticles.Count);

            // Check for active particles
            if (RTParticles is not null)
            {
                // Update all active particles
                for (int i = RTParticles.Count - 1; i >= 0; i--)
                {
                    var particle = RTParticles[i];

                    //How???
                    if (particle is null)
                    {
                        RTParticles.RemoveAt(i);
                        continue;
                    }

                    if (particle.AssignedTarget.NeedsManualParticleUpdate)
                        continue;

                    particle.AssignedTarget?.PreUpdateParticle(particle);
                    if (particle.Killed)
                        continue;

                    particle.Update();
                    if (particle.Killed)
                        continue;

                    // Update time since last update for particle's render target
                    if (particle.AssignedTarget is not null)
                        particle.AssignedTarget.TimeSinceLastUpdate = 0;

                    if (particle.Lifetime > 0 && particle.Time > particle.Lifetime)
                        particle.Kill();
                }
            }

            // Get all active render targets
            List<ParticleRenderTarget> activeRTs = [];

            if (AutoTargets != null && AutoTargets.Count > 0)
                activeRTs.AddRange(AutoTargets.Values.Where(Rt => Rt is not null && Rt.Initialized)); 
            if (ManualRenderTargets != null && ManualRenderTargets.Count > 0)
                activeRTs.AddRange(ManualRenderTargets);

            // Dispose inactive render targets
            foreach (var renderTarget in activeRTs)
            {
                if (!renderTarget.NeedsManualParticleUpdate)
                    renderTarget.PostUpdate();

                //Undead targets will not get any more particles added to them while theyre dead, so just clear em instantly
                if (renderTarget.IsUndead && renderTarget.AssignedParticles.Count == 0)
                    renderTarget.TimeSinceLastUpdate = renderTarget.AutoDisposeTime;

                // Dispose of render target if it has been inactive for too long
                if (renderTarget.TimeSinceLastUpdate++ >= renderTarget.AutoDisposeTime)
                {
                    // Remove target if its a manual target
                    ManualRenderTargets.Remove(renderTarget);
                    if (renderTarget.IsUndead)
                        UndeadTargetCount--;
                    renderTarget.DisposeRenderTarget();
                    continue;
                }
            }
        }


        /// <summary>
        /// Spawns a new <see cref="RTParticle"/>. <para/>
        /// Assigns the particle to a render target. The target can be one registered on loading or one initialized and drawn manually. <br/>
        /// If a type is specified, the <see cref="ParticleRenderTarget"/> in <see cref="AutoTargets"/> of that type will be used. <br/>
        /// If a manualTarget is specified, that target will be added to <see cref="ManualRenderTargets"/> and must be drawn manually.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="particle"></param>
        /// <param name="manualTarget"></param>
        public static void SpawnParticle<T>(RTParticle particle, T manualTarget = null) where T : ParticleRenderTarget
        {
            // Don't spawn particles if the game is paused.
            // This precedent is established with how Dust instances are created.
            // Don't spawn particles if on the server side either, or if the particles dict is somehow null
            if ((Main.gamePaused && manualTarget == null) || Main.dedServ || RTParticles is null)
                return;

            // Initialize target
            ParticleRenderTarget renderTarget = manualTarget is null ? GetInitializedTarget<T>() : GetInitializedTarget(manualTarget);

            // Add to active particles list
            RTParticles.Add(particle);
            particle.Type = RTParticleTypes[particle.GetType()];

            // Assign particle to render target and vice versa
            renderTarget?.AddParticleToTarget(particle);
            particle.AssignedTarget = renderTarget;
        }

        public static ParticleRenderTarget GetInitializedTarget<T>(T manualTarget = null) where T : ParticleRenderTarget
        {
            // Determine if a manual target is being used, the instance cannot already exist in the RenderTargets dict
            bool usingManualTarget = manualTarget != null && !AutoTargets.ContainsValue(manualTarget);
            ParticleRenderTarget renderTarget = usingManualTarget ? manualTarget : AutoTargets[typeof(T)];

            // Initialize the RT if it hasnt been already
            if (renderTarget is not null && !renderTarget.Initialized)
            {
                renderTarget.InitializeRenderTarget();

                // Add it to the manuatRT list if it has been determined to be one
                if (usingManualTarget)
                    ManualRenderTargets.Add(renderTarget);
            }

            return renderTarget;
        }

        /// <summary>
        /// Sets the given particle to inactive and unassigns it from it's render target.
        /// </summary>
        /// <param name="particle"></param>
        public static void RemoveParticle(RTParticle particle)
        {
            RTParticles.Remove(particle);
            particle.AssignedTarget?.RemoveParticleFromTarget(particle);
        }

        public static void ClearActiveParticles()
        {
            RTParticles.Clear();
            DisposeRenderTargets();
        }

        public static void DisposeRenderTargets()
        {
            // First, batch all active targets
            List<ParticleRenderTarget> activeRTs = [];

            if (AutoTargets != null && AutoTargets.Count > 0)
                activeRTs.AddRange(AutoTargets.Values.Where(Rt => Rt is not null && Rt.Initialized));
            if (ManualRenderTargets != null && ManualRenderTargets.Count > 0)
                activeRTs.AddRange(ManualRenderTargets);

            // Do nothing if none of the RTs are active
            if (activeRTs.Count <= 0)
                return;

            // Dipose every active target
            foreach (var renderTarget in activeRTs)
                renderTarget.DisposeRenderTarget();

            UndeadTargetCount = 0;
        }

        private void DrawAbovePlayers() => DrawAutoTargetsAndUndeadTargets(DrawhookLayer.AbovePlayer, Main.spriteBatch);
        private void DrawAboveProjectiles() => DrawAutoTargetsAndUndeadTargets(DrawhookLayer.AboveProjectiles, Main.spriteBatch);
        private void DrawAboveNPCs() => DrawAutoTargetsAndUndeadTargets(DrawhookLayer.AboveNPCs, Main.spriteBatch);
        private void DrawAboveTiles() => DrawAutoTargetsAndUndeadTargets(DrawhookLayer.AboveTiles, Main.spriteBatch);
        private void DrawBehindTiles() => DrawAutoTargetsAndUndeadTargets(DrawhookLayer.BehindTiles, Main.spriteBatch);

        private static void DrawAutoTargetsAndUndeadTargets(DrawhookLayer layer, SpriteBatch spriteBatch)
        {
            if ((AutoTargets is null || AutoTargets.Count <= 0) && UndeadTargetCount == 0)
                return;

            // First, batch all active targets by their layer
            // Does not include manual targets since they're in their own list
            List<ParticleRenderTarget> batchedRTs = [];
            if (AutoTargets is not null && AutoTargets.Count > 0)
                batchedRTs.AddRange(AutoTargets.Values.Where(rt => rt is not null && rt.Initialized && rt.ShouldDrawOnLayer(layer)));
            if (UndeadTargetCount > 0)
                batchedRTs.AddRange(ManualRenderTargets.Where(rt => rt is not null && rt.IsUndead && rt.ShouldDrawOnLayer(layer)));

            // Do nothing if there are no RTs to draw
            if (batchedRTs.Count <= 0)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw batched RTs
            foreach (var renderTarget in batchedRTs)
                renderTarget.DrawRenderTarget(Main.spriteBatch, layer, null);

            spriteBatch.End();
        }

        private void ResizeRenderTargets()
        {
            if (AutoTargets is null || AutoTargets.Count <= 0)
                return;

            // Resize each auto target
            foreach (var renderTarget in AutoTargets.Values)
            {
                if (renderTarget is null || !renderTarget.Initialized)
                    continue;

                renderTarget.ResizeTarget();
            }
        }

        /// <summary>
        /// Provides the texture associated with this particle type.
        /// </summary>
        public static Texture2D GetTexture(int type) => RTParticleTextures[type].Value;
    }
}