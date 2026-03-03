namespace CalamityFables.Particles
{
    public class PrimitiveStreak : Particle, IDrawPixelated //TODO these can get pretty laggy, look into how to fix possibly
    {
        public override string Texture => AssetDirectory.Invisible;
        public override bool UseCustomDraw => true;
        public DrawhookLayer layer => DrawhookLayer.AbovePlayer;

        #region Properties
        public float Gravity { get; set; } = 0.2f;
        public Vector2 Acceleration { get; set; } = new(-0.01f);
        public bool Collision { get; set; } = false;
        public bool FreakyMovement { get; set; } = false;

        public Color LightColor { get; set; } = Color.Black;
        public bool GlowAtTip { get; set; } = false;
        public bool Outline { get; set; } = false;

        public Func<Effect> TrailShader { get; set; }
        public ITrailTip TrailTip { get; set; }

        #endregion

        public delegate TrailColorFunction PrimitiveStreakColorFunction(Func<float, Color> lightColorFunc, float lifetimeCompletion, bool outline = false);
        public delegate TrailWidthFunction PrimitiveStreakWidthFunction(float lifetimeCompletion);

        protected readonly PrimitiveStreakColorFunction ColorFunction;
        protected readonly PrimitiveStreakWidthFunction WidthFunction;

        protected readonly float BaseWidth;
        protected readonly Color StartColor;
        protected readonly Color EndColor;

        protected int TrailPoints;
        protected List<Vector2> Positions;
        protected PrimitiveTrail Trail;

        protected bool DrawingOutline;
        protected float FreakyMovementCounter;

        public PrimitiveStreak(Vector2 position, Vector2 velocity, PrimitiveStreakColorFunction colorFunction, PrimitiveStreakWidthFunction widthFunction, int trailPoints, int lifetime)
        {
            Position = position;
            Velocity = velocity;
            ColorFunction = colorFunction;
            WidthFunction = widthFunction;
            TrailPoints = trailPoints;
            Lifetime = lifetime;

            // Get base values
            var baseColorFunction = ColorFunction(null, 0f);
            var baseWidthFunction = WidthFunction(0f);

            BaseWidth = baseWidthFunction(1f);
            StartColor = baseColorFunction(1f) with { A = 0 };
            EndColor = baseColorFunction(0f) with { A = 0 };
        }

        public PrimitiveStreak(Vector2 position, Vector2 velocity, TrailColorFunction colorFunction, TrailWidthFunction widthFunction, int trailPoints, int lifetime, TrailColorFunction outlineFunction = null, bool fadeColor = false, bool fadeWidth = true, bool lighting = true) 
            : this(position, velocity, ConvertColorFunction(colorFunction, outlineFunction, fadeColor, lighting), ConvertWidthFunction(widthFunction, fadeWidth), trailPoints, lifetime)
        {
            Outline = outlineFunction != null;
        }

        public PrimitiveStreak(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float startWidth, float endWidth, int trailPoints, int lifetime, Color? outlineStartColor = null, Color? outlineEndColor = null, bool fadeColor = false, bool fadeWidth = true, bool lighting = true)
            : this(position, velocity, f => Color.Lerp(endColor, startColor, f), f => MathHelper.Lerp(endWidth, startWidth, f), trailPoints, lifetime, outlineStartColor.HasValue && outlineEndColor.HasValue ? f => Color.Lerp(outlineEndColor.Value, outlineStartColor.Value, f) : null, fadeColor, fadeWidth, lighting)
        {
            Outline = outlineStartColor.HasValue && outlineEndColor.HasValue;
        }

        protected bool Colliding => Collision && Terraria.Collision.IsWorldPointSolid(Position, true);

        public override void Update()
        {
            // Apply gravity and acceleration
            Velocity.Y += Gravity;
            Velocity *= new Vector2(1f + Acceleration.X, 1f + Acceleration.Y);

            if (FreakyMovement)
            {
                if (FreakyMovementCounter == 0)
                    FreakyMovementCounter = Main.rand.NextFloat(MathHelper.TwoPi);

                FreakyMovementCounter += MathF.Pow(Main.rand.NextFloat(0f, 1f), 7);

                Velocity = Velocity.RotatedBy(MathF.Sin(FreakyMovementCounter) * MathF.Pow(Main.rand.NextFloat(0f, 1f), 4f) * (1.2f - Math.Min(Velocity.Length() / 4, 1f)));
            }

            if (Colliding)
            {
                Velocity *= 0;
                Gravity = 0;
                Acceleration = Vector2.Zero;
            }

            // Create light if color is not black
            if (LightColor != Color.Black)
                Lighting.AddLight(Position, LightColor.ToVector3() * (1f - MathF.Pow(LifetimeCompletion, 4)));

            // Update trail
            ManageTrail();
        }

        #region Prims
        protected void ManageTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 position = Position + Velocity;

            // Fill Trail Positions
            Positions ??= [.. Enumerable.Repeat(position, TrailPoints)];

            Positions.Add(position);
            while (Positions.Count > TrailPoints)
                    Positions.RemoveAt(0);

            Trail ??= new PrimitiveTrail(TrailPoints, f => WidthFunction(LifetimeCompletion)(f), f => ColorFunction(GetLightColor, LifetimeCompletion, DrawingOutline)(f), TrailTip);

            // No retrieval function
            Trail.SetPositionsSmart(Positions, position, (x, y) => [.. x]);
            Trail.NextPosition = position + Velocity;
        }

        protected Color GetLightColor(float progress)
        {
            // Find light position depending on progress and position cache
            int index = (int)MathHelper.Lerp(0, Positions.Count - 1, progress);
            Point lightPosition = Positions[index].ToSafeTileCoordinates();

            return Lighting.GetColor(lightPosition);
        }

        /// <summary>
        /// Converts the specified <see cref="TrailColorFunction"/> to a <see cref="PrimitiveStreakColorFunction"/>. Useful when the primitive streak specific type is not used.
        /// </summary>
        /// <param name="colorFunction"></param>
        /// <param name="outlineFunction"></param>
        /// <param name="fade"></param>
        /// <param name="lighting"></param>
        /// <returns></returns>
        public static PrimitiveStreakColorFunction ConvertColorFunction(TrailColorFunction colorFunction, TrailColorFunction outlineFunction = null, bool fade = false, bool lighting = true) => (lightColorFunc, lifetimeCompletion, outline) =>
        {
            // Create trail width function from given parameters
            return (progress) =>
            {
                Color color = outline && outlineFunction != null ? outlineFunction(progress) : colorFunction(progress);

                if (fade)
                    color *= 1f - MathF.Pow(Utils.GetLerpValue(0.5f, 1f, lifetimeCompletion, true), 2);

                if (lighting && lightColorFunc != null)
                    color = color.MultiplyRGBA(lightColorFunc(progress));

                return color;
            };
        };

        /// <summary>
        /// Converts the specified <see cref="TrailWidthFunction"/> to a <see cref="PrimitiveStreakWidthFunction"/>. Useful when the primitive streak specific type is not used.
        /// </summary>
        /// <param name="widthFunction"></param>
        /// <param name="fade"></param>
        /// <returns></returns>
        public static PrimitiveStreakWidthFunction ConvertWidthFunction(TrailWidthFunction widthFunction, bool fade = true) => (lifetimeCompletion) =>
        {
            // Create trail width function from given parameters
            return (progress) =>
            {
                float width = widthFunction(progress);

                if (fade)
                    width *= 1f - MathF.Pow(Utils.GetLerpValue(0.5f, 1f, lifetimeCompletion, true), 2);

                return width;
            };
        };

        #endregion

        /// <summary>
        /// Can be used in particles implementing this as a base to modify the shader.
        /// </summary>
        /// <param name="effect"></param>
        protected virtual void ExtraEffects(ref Effect effect) { }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = TrailShader?.Invoke();
            ExtraEffects(ref effect);

            // Draw outline
            if (Outline)
            {
                DrawingOutline = true;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = 2f * (i * MathHelper.PiOver2).ToRotationVector2();
                    Trail?.Render(effect, offset - Main.screenPosition);
                }
                DrawingOutline = false;
            }

            // Draw base trail
            Trail?.Render(effect, -Main.screenPosition);

            // Add a layer of bloom above trail
            if (GlowAtTip && !Colliding)
            {
                Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

                Vector2 position = (Position - Main.screenPosition) / 2f;

                // Scale depending on trail width over bloom size ratio
                float bloomScale = BaseWidth / bloom.Width;
                bloomScale *= 1f - MathF.Pow(Utils.GetLerpValue(0.5f, 1f, LifetimeCompletion, true), 2);

                // First dark layer
                spriteBatch.Draw(bloom, position, null, EndColor, 0, bloom.Size() / 2, bloomScale * 1f, SpriteEffects.None, 0);

                // Bright Core
                spriteBatch.Draw(bloom, position, null, StartColor, 0, bloom.Size() / 2, bloomScale * 0.5f, SpriteEffects.None, 0);

                // Final dark Layer
                spriteBatch.Draw(bloom, position, null, EndColor, 0, bloom.Size() / 2, bloomScale * 3f, SpriteEffects.None, 0);
            }
        }
    }
}