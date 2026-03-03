using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Particles
{
    /// <summary>
    /// A primitive particle that forms a ring.
    /// </summary>
    public class PrimitiveRingParticle : Particle, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Invisible;
        public override bool SetLifetime => true;

        public DrawhookLayer layer => DrawLayer;
        protected PrimitiveClosedLoop Ring;

        public float Squish { get; set; } = 1;
        public bool NoLight { get; set; } = true;
        public DrawhookLayer DrawLayer { get; set; } = DrawhookLayer.AboveNPCs;

        #region Easings
        public EasingFunction RadiusEasing { get; set; } = PolyOutEasing;
        public bool InvertRadiusEasing { get; set; } = false;
        public float RadiusEasingDegree { get; set; } = 2f;
        public EasingFunction WidthEasing { get; set; } = LinearEasing;
        public bool InvertWidthEasing { get; set; } = false;
        public float WidthEasingDegree { get; set; } = 1f;
        public EasingFunction OpacityEasing { get; set; } = LinearEasing;
        public bool InvertOpacityEasing { get; set; } = false;
        public float OpacityEasingDegree { get; set; } = 1f;
        public float OpacityFadePoint { get; set; } = 0.75f;
        #endregion

        protected Entity AnchoredEntity;
        protected Vector2 AnchorOffset;
        protected float MaxOpacity;
        protected float Opacity;
        protected float MaxRadius;
        protected float MinRadius = 0;
        protected float Radius;
        protected float MaxWidth;
        protected float MinWidth = 0;
        protected float Width;

        /// <summary>
        /// Determines if opacity will be applied to the particle's primitives. <br/>
        /// Useful for particles that implementing this as a base.
        /// </summary>
        public virtual bool ApplyOpacity => true;

        /// <summary>
        /// Determines if the ring's far end will fade when squish is applied. <br/>
        /// Useful for particles that implementing this as a base.
        /// </summary>
        public virtual bool ApplyPerspectiveFade => true;

        public PrimitiveRingParticle(Vector2 position, Vector2 velocity, Color color, float maxRadius, float maxWidth, float opacity = 1f, int lifeTime = 30)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            MaxOpacity = opacity;
            MaxRadius = maxRadius;
            MaxWidth = maxWidth;
            Lifetime = lifeTime;
            Scale = 1;
            Rotation = 0;
        }

        public override void Update()
        {
            // Code specific to anchored entities
            if (AnchoredEntity != null)
            {
                // Check if the anchor is invalid and set the position
                if (!AnchoredEntity.active)
                {
                    Kill();
                    return;
                }
                Position = AnchoredEntity.Center + AnchorOffset;
            }

            // Easing all values
            float radiusProgress = InvertRadiusEasing ? 1f - LifetimeCompletion : LifetimeCompletion;
            Radius = (MaxRadius - MinRadius) * RadiusEasing(radiusProgress, RadiusEasingDegree) + MinRadius;

            float widthProgress = InvertWidthEasing ? 1f - LifetimeCompletion : LifetimeCompletion;
            Width = (MaxWidth - MinWidth) * WidthEasing(widthProgress, WidthEasingDegree) + MinWidth;

            float opacityProgress = Utils.GetLerpValue(InvertOpacityEasing ? 0f : 1f, OpacityFadePoint, LifetimeCompletion, true);
            opacityProgress = Math.Clamp(opacityProgress, 0f, 1f);
            Opacity = MaxOpacity * OpacityEasing(opacityProgress, OpacityEasingDegree);

            ManageRing();
        }

        /// <summary>
        /// Can be used in particles implementing this as a base to use unique shaders for the line.
        /// </summary>
        /// <param name="effect"></param>
        public virtual void ExtraEffects(ref Effect effect) { }

        #region Helper Methods
        /// <summary>
        /// Attaches the particle to an entity. Can be centered on the entity, or offset by its spawn position relative to the entity center.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="center"></param>
        public PrimitiveRingParticle Attach(Entity entity, bool center = false)
        {
            AnchoredEntity = entity;

            // Sets offset if center is false
            if (center)
                Position = entity.Center;

            AnchorOffset = Position - entity.Center;

            return this;
        }

        /// <summary>
        /// Applies rotation to the ring and squishes it. <br/>
        /// Makes the ring look 3d with the far end fading into the background, unless it's been disabled with <see cref="ApplyPerspectiveFade"/>.
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="squish"></param>
        public PrimitiveRingParticle Squash(float rotation, float squish)
        {
            Rotation = rotation;
            Squish = squish;

            return this;
        }

        /// <summary>
        /// Can be used to modify the style of this particles movement.
        /// </summary>
        /// <param name="radiusEasing"></param>
        /// <param name="radiusEasingDegree"></param>
        /// <param name="invertRadiusEasing"></param>
        /// <param name="widthEasing"></param>
        /// <param name="widthEasingDegree"></param>
        /// <param name="invertWidthEasing"></param>
        /// <param name="opacityEasing"></param>
        /// <param name="opacityEasingDegree"></param>
        /// <param name="invertOpacityEasing"></param>
        /// <param name="opacityFadePoint"></param>
        /// <returns></returns>
        public PrimitiveRingParticle ModifyEasings(EasingFunction radiusEasing = null, float radiusEasingDegree = 2f, bool invertRadiusEasing = false, 
                                                   EasingFunction widthEasing = null, float widthEasingDegree = 1f, bool invertWidthEasing = false,
                                                   EasingFunction opacityEasing = null, float opacityEasingDegree = 1f, bool invertOpacityEasing = false, float opacityFadePoint = 0.75f)
        {
            if (radiusEasing is not null)
                RadiusEasing = radiusEasing;
            RadiusEasingDegree = radiusEasingDegree;
            InvertRadiusEasing = invertRadiusEasing;

            if (widthEasing is not null)
                WidthEasing = widthEasing;
            WidthEasingDegree = widthEasingDegree;
            InvertWidthEasing = invertWidthEasing;

            if (opacityEasing is not null)
                OpacityEasing = opacityEasing;
            OpacityEasingDegree = opacityEasingDegree;
            InvertOpacityEasing = invertOpacityEasing;
            OpacityFadePoint = opacityFadePoint;

            return this;
        }
        #endregion

        #region Prims
        public void ManageRing()
        {
            if (Main.dedServ)
                return;

            // Update ring with parameters
            Ring ??= new PrimitiveClosedLoop(30, WidthFunction, ColorFunction);
            Ring.SetPositionsEllipse(Position, Radius, 0, Squish, Rotation);
        }

        protected virtual float WidthFunction(float completionRatio) => Width;

        protected virtual Color ColorFunction(float completionRatio)
        {
            Color color = Color.White;
            if (!NoLight)
                color = color.MultiplyRGB(Lighting.GetColor(Position.ToTileCoordinates()));

            if (ApplyPerspectiveFade)
            {
                // Fades at the far end of the ring to give it perspective
                float fadeFunction = 0.5f + 0.5f * MathF.Sin(MathHelper.Pi * (2 * completionRatio + 1));
                float perspectiveFade = MathHelper.Lerp(1, Squish, fadeFunction);
                color *= perspectiveFade;
            }

            if (ApplyOpacity)
                color *= Opacity;

            return color;
        }

        public virtual void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = null;
            ExtraEffects(ref effect);
            Ring?.Render(effect, -Main.screenPosition);
        }
        #endregion
    }
}