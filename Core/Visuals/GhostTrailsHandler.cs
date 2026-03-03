using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    //Made after seeing spirit's interesting way to deal with lingering trails
    public class GhostTrailsHandler : ModSystem
    {
        internal static List<GhostTrail> trails;
        internal static Dictionary<IDisposable, int> disposableTrails;
        private static readonly List<IDisposable> clearList = new List<IDisposable>();

        public override void Load()
        {
            RenderTargetsManager.DrawToRenderTargetsEvent += SortGhostTrails;

            //Hooking on the diff layers
            FablesDrawLayers.DrawBehindDustEvent += DrawTrailsAbovePlayer;
            FablesDrawLayers.DrawThingsAboveNPCsEvent += DrawTrailsAboveNPCs;
            FablesDrawLayers.DrawThingsAboveSolidTilesEvent += DrawTrailsAboveTiles;
            FablesDrawLayers.DrawThingsBehindSolidTilesEvent += DrawTrailsBehindTiles;

            trails = new List<GhostTrail>();
            disposableTrails = new();
        }


        public override void Unload()
        {
            foreach (GhostTrail trail in trails)
                trail.trail.Dispose();
            foreach (IDisposable disposable in disposableTrails.Keys)
                disposable.Dispose();

            trails = null;
            disposableTrails = null;
        }

        public override void PostUpdateEverything()
        {
            if (!Main.dedServ)
                UpdateTrails();
        }

        /// <summary>
        /// Spawns the particle instance provided into the world. If the particle limit is reached but the particle is marked as important, it will try to replace a non important particle.
        /// </summary>
        public static void LogNewTrail(GhostTrail trail)
        {
            //Don't spawn trails if on the server side either, or if the particles dict is somehow null
            if (Main.dedServ || trails == null)
                return;

            trails.Add(trail);
        }

        public static void LogDisposable(IDisposable trail)
        {
            //Don't care if on the server side 
            if (Main.dedServ || disposableTrails == null)
                return;
            disposableTrails[trail] = 120;
        }

        public static void UpdateTrails()
        {
            for (int i = 0; i < trails.Count; i++)
            {
                GhostTrail trail = trails[i];

                trail.Decay();
                if (trail.Dead)
                {
                    trails.RemoveAt(i);
                    i--;
                }
            }

            clearList.Clear();
            foreach (IDisposable disposable in disposableTrails.Keys)
            {
                disposableTrails[disposable]--;
                if (disposableTrails[disposable] < 0)
                {
                    disposable.Dispose();
                    clearList.Add(disposable);
                }
            }
            foreach (IDisposable disposable in clearList)
                disposableTrails.Remove(disposable);
        }


        public static List<GhostTrail> trailsAbovePlayers = new List<GhostTrail>();
        public static List<GhostTrail> trailsAboveNPCs = new List<GhostTrail>();
        public static List<GhostTrail> trailsAboveTiles = new List<GhostTrail>();
        public static List<GhostTrail> trailsBehindTiles = new List<GhostTrail>();

        private void SortGhostTrails()
        {
            trailsAbovePlayers.Clear();
            trailsAboveNPCs.Clear();
            trailsAboveTiles.Clear();
            trailsBehindTiles.Clear();

            foreach (GhostTrail trail in trails)
            {
                //Pixelated trails are handled by the pixelated prim layer
                if (trail.Pixelated)
                    continue;

                switch (trail.DrawLayer)
                {
                    case DrawhookLayer.AbovePlayer:
                        trailsAbovePlayers.Add(trail);
                        break;
                    case DrawhookLayer.AboveNPCs:
                        trailsAboveNPCs.Add(trail);
                        break;
                    case DrawhookLayer.AboveTiles:
                        trailsAboveTiles.Add(trail);
                        break;
                    case DrawhookLayer.BehindTiles:
                        trailsBehindTiles.Add(trail);
                        break;
                }    
            }
        }

        private void DrawTrailsAbovePlayer()
        {
            if (trailsAbovePlayers.Count() == 0)
                return;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (GhostTrail trail in trailsAbovePlayers)
                trail.Draw();
            Main.spriteBatch.End();
        }


        private void DrawTrailsAboveNPCs()
        {
            if (trailsAboveNPCs.Count() == 0)
                return;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (GhostTrail trail in trailsAboveNPCs)
                trail.Draw();
            Main.spriteBatch.End();
        }

        private void DrawTrailsAboveTiles()
        {
            if (trailsAboveTiles.Count() == 0)
                return;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (GhostTrail trail in trailsAboveTiles)
                trail.Draw();
            Main.spriteBatch.End();
        }

        private void DrawTrailsBehindTiles()
        {
            if (trailsBehindTiles.Count() == 0)
                return;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (GhostTrail trail in trailsBehindTiles)
                trail.Draw();
            Main.spriteBatch.End();
        }
    }

    public delegate void SetEffectsParameterDelegate(Effect effect, float dissaparition);

    public class GhostTrail : IDrawPixelated
    {
        public bool Dead { get; set; } = false;
        public Entity AttachedEntity { get; set; } = null;
        public int TrailMaxLenght { get; set; } = 9999;
        public bool ShrinkTrailLenght { get; set; } = false;
        public bool ShrinkTrailWidth { get; set; } = true;
        public bool FadeTrailOpacity { get; set; } = true;
        public float ShrinkTime { get; set; } = 1f;
        public string ShaderName { get; set; } = "";

        internal PrimitiveTrail trail;
        internal float timeLeft = 1f;
        internal List<Vector2> cache;

        public DrawhookLayer DrawLayer { get; set; } = DrawhookLayer.AboveNPCs;
        public bool Pixelated { get; set; } = false;
        public bool ShoulDrawPixelated => Pixelated;
        public DrawhookLayer layer => DrawLayer;


        TrailWidthFunction realWidthFunction;
        TrailColorFunction realColorFunction;
        TrailPointRetrievalFunction pointRetrievalFunction;
        SetEffectsParameterDelegate effectParametersDelegate;

        public virtual float ShrinkingWidthFunction(float completion)
        {
            float baseWidth = realWidthFunction(completion);
            if (ShrinkTrailWidth)
                baseWidth *= timeLeft;
            return baseWidth;
        }

        public virtual Color FadingColorFunction(float completion)
        {
            Color baseColor = realColorFunction(completion);
            if (FadeTrailOpacity)
                baseColor *= timeLeft;
            return baseColor;
        }

        internal void NoEffectParameters(Effect effect, float completion) { }

        public GhostTrail(List<Vector2> trailCache, PrimitiveTrail trailToClone, float duration = 0.3f, Entity attachedEntity = null, string shaderName = "", SetEffectsParameterDelegate effectParams = null, TrailPointRetrievalFunction pointRetrieval = null, int? maxPoints = null)
        {

            if (maxPoints == null)
                maxPoints = trailCache.Count;
            if (effectParams == null)
                effectParams = NoEffectParameters;
            if (pointRetrieval == null)
                pointRetrieval = RigidPointRetreivalFunction;

            cache = trailCache;
            realColorFunction = trailToClone.trailColorFunction;
            realWidthFunction = trailToClone.trailWidthFunction;
            trail = new PrimitiveTrail(trailToClone.maxPointCount, ShrinkingWidthFunction, FadingColorFunction, trailToClone.tip);
            trail.SetPositionsSmart(trailCache, attachedEntity != null ? attachedEntity.Center : Vector2.Zero, pointRetrievalFunction);

            TrailMaxLenght = maxPoints.Value;
            ShrinkTime = duration;

            pointRetrievalFunction = pointRetrieval;
            AttachedEntity = attachedEntity;
            ShaderName = shaderName;
            effectParametersDelegate = effectParams;
        }

        public void Decay()
        {
            //Shrink the trail along its time
            if (timeLeft > 0)
                timeLeft -= 1 / (60f * ShrinkTime);
            else
            {
                Dead = true;
                timeLeft = 0;
                return;
            }

            //Clears the attached entity in case it expires
            if (AttachedEntity != null && !AttachedEntity.active)
                AttachedEntity = null;

            //Update the trail's position based on the entity it is attached to
            if (AttachedEntity != null)
                cache.Add(AttachedEntity.Center);

            //Keep the trail at the max size (Alternatively, make it shrink)
            while (cache.Count > TrailMaxLenght)
                cache.RemoveAt(0);
            if (ShrinkTrailLenght && cache.Count > 2)
                cache.RemoveAt(0);

            trail.SetPositionsSmart(cache, AttachedEntity != null ? AttachedEntity.Center : Vector2.Zero, pointRetrievalFunction);
            if (AttachedEntity != null)
                trail.NextPosition = AttachedEntity.Center + AttachedEntity.velocity;
        }

        /// <summary>
        /// How the trail is drawn to the screen
        /// </summary>
        public virtual void Draw()
        {
            Effect effect = null;
            if (ShaderName != "")
            {
                effect = Scene[ShaderName].GetShader().Shader;
                effectParametersDelegate(effect, timeLeft);
            }

            trail?.Render(effect, -Main.screenPosition);
        }

        public void DrawPixelated(SpriteBatch spriteBatch) => Draw();
    }
}
