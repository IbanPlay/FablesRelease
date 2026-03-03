namespace CalamityFables.Particles
{
    /// <summary>
    /// A primitive particle that connects between two points and can be anchored to entities.
    /// </summary>
    public class ConnectingLineParticle : Particle, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Invisible;
        public override bool SetLifetime => true;

        public DrawhookLayer layer => DrawhookLayer.AboveTiles;
        private PrimitiveTrail Line;
        public override bool Important => Needed;

        public List<Entity> AnchoredEntities { get; set; }
        public bool NoLight { get; set; } = true;
        public bool Needed { get; set; } = false;

        protected List<Vector2> Positions;
        protected float MaxOpacity;
        protected float Width;
        protected float Opacity;

        /// <summary>
        /// Determines if opacity will be applied to the particle's primitives. <br/>
        /// Useful for particles that implementing this as a base.
        /// </summary>
        public virtual bool ApplyOpacity => true;

        public ConnectingLineParticle(Color color, float width, int lifeTime, float opacity, params List<Vector2> positions)
        {
            Positions = positions;
            Position = positions[0];
            Color = color;
            MaxOpacity = opacity;
            Width = width;
            Lifetime = lifeTime;
            FrontLayer = false;
            Scale = 1;
            Rotation = 0;
        }

        public override void Update()
        {
            // Code specific to anchored entities
            if (AnchoredEntities != null)
            {
                // Clear positions
                Positions.Clear();

                // Check if any of the anchors is invalid and set the positions to each entity's center
                foreach (Entity entity in AnchoredEntities)
                {
                    if (entity is null || !entity.active)
                    {
                        Kill();
                        return;
                    }
                    Positions.Add(entity.Center);
                }
            }

            Position = Positions[0];
            Opacity = MaxOpacity * MathHelper.Lerp(1, 0, LifetimeCompletion);

            ManageLine();
        }

        /// <summary>
        /// Can be used in particles implementing this as a base to use unique shaders for the line.
        /// </summary>
        /// <param name="effect"></param>
        public virtual void ExtraEffects(ref Effect effect) { }

        /// <summary>
        /// Attaches particle positions to entities.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="center"></param>
        public void Attach(params List<Entity> entities) => AnchoredEntities = entities;

        #region Prims
        public void ManageLine()
        {
            if (Main.dedServ)
                return;

            // Update line with positions
            Line ??= new PrimitiveTrail(Positions.Count, WidthFunction, ColorFunction);
            Line.SetPositionsSmart(Positions, position);
        }

        public virtual float WidthFunction(float completionRatio) => Width;

        public virtual Color ColorFunction(float completionRatio)
        {
            Color color = Color.White;

            if (ApplyOpacity)
                color *= Opacity;

            if (NoLight)
                return color;

            Color lighting = Lighting.GetColor(Position.ToTileCoordinates());
            return color.MultiplyRGB(lighting);
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = null;
            ExtraEffects(ref effect);
            Line?.Render(effect, -Main.screenPosition);
        }
        #endregion
    }
}