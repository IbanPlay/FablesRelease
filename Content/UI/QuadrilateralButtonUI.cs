using ReLogic.Graphics;
using Terraria.UI;
using Terraria.UI.Chat;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.UI
{
    /// <summary>
    /// A button with a text label that has wobbly vertices. Added to a textbox by using <see cref="TextboxInfo.AddButton"/>
    /// </summary>
    public class QuadrilateralButtonUI : UIElement
    {
        public bool CloseUIOnClick { get; set; }
        public Action clickEvent;

        #region Colors
        /// <summary>
        /// Solid color the button will display with when not hovered
        /// </summary>
        public Color MainColor { get; set; }
        /// <summary>
        /// Solid color the button will display with when hovered
        /// </summary>
        public Color HoveredMainColor { get; set; }
        /// <summary>
        /// Color of the "decal" that appears when the button is hovered. Decal is an extra quadrilateral that draws masked ontop of the base button and applies its color over it
        /// </summary>
        public Color HoveredDecalColor { get; set; }
        /// <summary>
        /// Color of the outline that draws around the button when hovered
        /// </summary>
        public Color HoveredOutlineColor { get; set; }
        #endregion

        /// <summary>
        /// Offsets added to the base rectangular vertices
        /// </summary>
        public ShiftingVertex[] vertexShifts;
        public ShiftingVertex[] decalVertexShifts;

        /// <summary>
        /// Cached rectangular vertices. Re-calculated when <see cref="_verticesCalculated"/> is set to false
        /// </summary>
        private List<Vector2> _vertices;
        private bool _verticesCalculated = false;

        private Vector2 _origin;
        private float _rotation;

        /// <summary>
        /// Center of the button
        /// </summary>
        public Vector2 Origin { get => _origin; set { _origin = value; _verticesCalculated = false; } }
        /// <summary>
        /// Button's rotation, based on the bottom edge of the parent textbox
        /// </summary>
        public float Rotation { get => _rotation; set { _rotation = value; _verticesCalculated = false; } }
        /// <summary>
        /// Size of the button's rectangular base (before adding in the displacement), based on the dimensions of the text label + some extra padding
        /// </summary>
        public Vector2 Dimensions {
            get {

                float height = labelDimensions.Y + 20f;
                float width = Math.Max(labelDimensions.X + 16f, 90f);

                return new Vector2(width, height) * CoolDialogueUI.uiScale;

            }
        }


        private float _opacity;
        /// <summary>
        /// Opacity of the button. Buttons start at zero opacity and quickly get more opaque when the text in the textbox has finished scrolling, or instantly become fully opaque when the textbox is clicked. <br/>
        /// Some actions are blocked when the button is at zero opacity, like clicking or hovering on it
        /// </summary>
        public float Opacity {
            get => _opacity;
            set => _opacity = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Maximum lenght of the vertices's displacement from their base dimensions
        /// </summary>
        public float Wobbliness { get; set; }
        /// <summary>
        /// Speed at which the vertex offsets change from one offset to the other. If set to 1, there will be 1 displacement per second
        /// </summary>
        public float WobbleSpeedMultiplier { get; set; }
        /// <summary>
        /// Speed multiplier ontop of the base <see cref="WobbleSpeedMultiplier"/> applied when hovering over the button
        /// </summary>
        public float HoveredWobbleSpeedMultiplier { get; set; }

        /// <summary>
        /// Maximum lenght of the decal's displacement from the button's base dimensions (Decal shows up when the button is hovered)
        /// </summary>
        public float DecalWobbliness { get; set; }
        /// <summary>
        /// Speed at which the decal's vertex offsets change from one offset to the other. If set to 1, there will be 1 displacement per second
        /// </summary>
        public float DecalWobbleSpeed { get; set; }
        /// <summary>
        /// Constantly increasing timer that's used to make the hovered outline oscillate slightly. Hardly noticeable for the button's outlines though
        /// </summary>
        public float OutlineThicknessTimer { get; set; }

        internal PrimitiveQuadrilateral drawer;
        internal PrimitiveQuadrilateral decalDrawer;
        internal PrimitivePolygonOutline outlineDrawer;

        //Text stuff
        public string _label;
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                if (!Main.dedServ)
                    labelDimensions = ChatManager.GetStringSize(FontAssets.MouseText.Value, _label, Vector2.One) * labelScale;
            }
        }

        public float labelScale = 1f;
        public CharacterDisplacementDelegate labelDisplacement;
        public Vector2 labelDimensions;
        public Color labelColor = Color.White;

        /// <summary>
        /// Base vertices of the button, a perfect rectangle with the specified <see cref="Dimensions"/>. Only regenerated when needed.<br/>
        /// The player doesnt see these vertices and instead sees <see cref="DisplacedVertices"/>, which are obtained by combining these vertices with the vertex vertex displacements to get a funky shifting look 
        /// </summary>
        public List<Vector2> Vertices {
            get {
                if (_verticesCalculated)
                    return _vertices;

                _vertices = new List<Vector2>();

                Vector2 unitVectorX = (Vector2.UnitX * Dimensions.X * 0.5f).RotatedBy(Rotation);
                Vector2 unitVectorY = (Vector2.UnitY * Dimensions.Y * 0.5f).RotatedBy(Rotation);

                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        _vertices.Add(Origin + unitVectorX * j + unitVectorY * i);
                    }
                }

                _verticesCalculated = true;
                return _vertices;
            }
        }

        /// <summary>
        /// Vertices of the button as they appear to the player. Obtained by adding the random vertex shifts to the constant rectangular vertices of the button
        /// </summary>
        public List<Vector2> DisplacedVertices {
            get {
                List<Vector2> displacedVertices = new List<Vector2>();

                for (int i = 0; i < 4; i++)
                {
                    displacedVertices.Add(Vertices[i] + vertexShifts[i].Displacement * CoolDialogueUI.uiScale);
                }

                return displacedVertices;
            }
        }

        /// <summary>
        /// Vertices that make up the on-hover outline, upscaled from <see cref="DisplacedVertices"/>
        /// </summary>
        public List<Vector2> OutlineVertices {
            get {
                List<Vector2> outlineVertices = DisplacedVertices;
                Vector2 cache = outlineVertices[3];
                outlineVertices[3] = outlineVertices[2];
                outlineVertices[2] = cache;

                return UpscalePolygonByCombiningPerpendiculars(outlineVertices, (4.5f + (float)Math.Sin(OutlineThicknessTimer - 0.8f) * 0.04f) * CoolDialogueUI.uiScale);
            }
        }

        /// <summary>
        /// Vertices that make up the decal, obtained by displacing the already once displaced vertices
        /// </summary>
        public List<Vector2> DecalVertices {
            get {
                List<Vector2> decalVertices = DisplacedVertices;

                for (int i = 0; i < decalVertices.Count; i++)
                {
                    decalVertices[i] = decalVertices[i] + decalVertexShifts[i].Displacement * CoolDialogueUI.uiScale;
                }

                return decalVertices;
            }
        }

        /// <summary>
        /// Should the player be able to see the decals? True when hovering over the button and the button's opacity is at 1 <br/>
        /// Also controls wether or not the button can be interacted with
        /// </summary>
        public bool ShouldDecalBeVisible => Opacity >= 1 && IsMouseHovering;


        public QuadrilateralButtonUI()
        {
            OutlineThicknessTimer = 0f;

            Wobbliness = 9f;
            WobbleSpeedMultiplier = 1f;
            HoveredWobbleSpeedMultiplier = 3f;

            DecalWobbleSpeed = 3f;
            DecalWobbliness = 20f;

            vertexShifts = new ShiftingVertex[4];
            decalVertexShifts = new ShiftingVertex[4];

            for (int i = 0; i < 4; i++)
            {
                vertexShifts[i] = new ShiftingVertex(Wobbliness);
                decalVertexShifts[i] = new ShiftingVertex(DecalWobbliness);
            }
        }

        public override void OnActivate()
        {
            OutlineThicknessTimer = 0;
            Vertices.Clear();
            _verticesCalculated = false;
        }

        public override void OnDeactivate()
        {
            OutlineThicknessTimer = 0;
            Vertices.Clear();
            _verticesCalculated = false;
        }

        public override void Update(GameTime gameTime)
        {
            //Augment the timer
            OutlineThicknessTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Calculate vertex shifts
            UpdateShiftingVertices(gameTime);

            //Set the vertices on the things
            UpdatePrimitives();
        }

        /// <summary>
        /// Goes over every shifting vertice and updates its timer, so they can constantly move around to their next intended destination. Sets a new target for the vertice's wobble target if they reached the current one
        /// </summary>
        public void UpdateShiftingVertices(GameTime gameTime)
        {
            //Update the vertex shifts for the button itself
            for (int i = 0; i < 4; i++)
            {
                ShiftingVertex vertex = vertexShifts[i];
                float extraMultiplier = IsMouseHovering ? HoveredWobbleSpeedMultiplier : 1f;
                vertex.time += (float)gameTime.ElapsedGameTime.TotalSeconds * WobbleSpeedMultiplier * extraMultiplier;

                if (vertex.time > 1f)
                {
                    vertex.displacement = vertex.nextDisplacement;
                    vertex.nextDisplacement = Main.rand.NextVector2Circular(Wobbliness, Wobbliness);
                    vertex.time = vertex.time % 1;
                }

                vertexShifts[i] = vertex;
            }

            //Update the vertex shifts for the button's decal if hovered
            if (ShouldDecalBeVisible)
            {
                for (int i = 0; i < 4; i++)
                {
                    ShiftingVertex vertex = decalVertexShifts[i];
                    vertex.time += (float)gameTime.ElapsedGameTime.TotalSeconds * DecalWobbleSpeed;

                    if (vertex.time > 1f)
                    {
                        vertex.displacement = vertex.nextDisplacement;
                        vertex.nextDisplacement = Main.rand.NextVector2Circular(DecalWobbliness, DecalWobbliness);
                        vertex.time = vertex.time % 1;
                    }

                    decalVertexShifts[i] = vertex;
                }
            }
        }

        /// <summary>
        /// Updates the primitive renderers to use the vertex positions we calculated
        /// </summary>
        public void UpdatePrimitives()
        {
            if (drawer == null)
                drawer = new PrimitiveQuadrilateral(MainColor);
            if (decalDrawer == null)
                decalDrawer = new PrimitiveQuadrilateral(HoveredDecalColor);
            if (outlineDrawer == null)
                outlineDrawer = new PrimitivePolygonOutline(4, 3f, HoveredOutlineColor, UpscalePolygonByCombiningPerpendiculars);

            outlineDrawer.outlineThickness = 3f * CoolDialogueUI.uiScale;

            drawer.Vertices = DisplacedVertices.ToArray();

            if (ShouldDecalBeVisible)
            {
                drawer.color = HoveredMainColor * Opacity;

                decalDrawer.Vertices = DecalVertices.ToArray();
                outlineDrawer.Vertices = OutlineVertices.ToArray();
            }

            else
                drawer.color = MainColor * Opacity;
        }

        #region Drawing
        /// <summary>
        /// Draws both the main button and the outline if hovered over
        /// </summary>
        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            if (OutlineThicknessTimer == 0 || drawer == null)
                return;

            drawer.RenderWithView(Matrix.Identity, null, Matrix.CreateTranslation(Vector3.Zero));

            if (ShouldDecalBeVisible)
                outlineDrawer.RenderWithView(Matrix.Identity, null, Matrix.CreateTranslation(Vector3.Zero));
        }

        /// <summary>
        /// Draws the decal for the button, if its visible
        /// </summary>
        public void DrawDecal(SpriteBatch spriteBatch)
        {
            if (!ShouldDecalBeVisible || decalDrawer == null)
                return;

            decalDrawer.RenderWithView(Matrix.Identity, null, Matrix.CreateTranslation(Vector3.Zero));
        }

        /// <summary>
        /// Renders the button's text label
        /// </summary>
        public void DrawLabel(SpriteBatch spriteBatch)
        {
            Vector2 setenceOrigin = Origin - (new Vector2(labelDimensions.X * 0.5f, labelDimensions.Y * 0.5f).RotatedBy(Rotation) - Vector2.UnitY.RotatedBy(Rotation) * 5f) * CoolDialogueUI.uiScale;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            for (int i = 0; i < Label.Length; i++)
            {
                Vector2 displacement = labelDisplacement(i);
                displacement = displacement.RotatedBy(Rotation) * labelScale;

                DrawBorderStringEightWay(spriteBatch, font, Label[i].ToString(), setenceOrigin + displacement, labelColor * Opacity, Color.Black * Opacity, Rotation, labelScale * CoolDialogueUI.uiScale);
                setenceOrigin += Vector2.UnitX.RotatedBy(Rotation) * ChatManager.GetStringSize(font, Label[i].ToString(), Vector2.One).X * labelScale * CoolDialogueUI.uiScale;
            }
        }
        #endregion

        public override void MouseOver(UIMouseEvent evt)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            base.MouseOver(evt);
        }

        public override bool ContainsPoint(Vector2 point)
        {
            if (Opacity < 0.5f)
                return false;

            List<Vector2> vert = DisplacedVertices;
            Vector2 cache = vert[2];
            vert[2] = vert[3];
            vert[3] = cache;

            return FablesUtils.InsidePolygon(point * Main.UIScale, vert);
        }

        /*
        public void SetClickEvent(TextboxInfo nextTextbox, bool closeOnClick = false)
        {
            CloseUIOnClick = closeOnClick;
            clickEvent = delegate () { CoolDialogueUIManager.theUI.SetTextbox(nextTextbox); };
        }

        public void SetClickEvent(IEnumerable<TextboxInfo> nextTextboxPool, bool closeOnClick = false)
        {
            CloseUIOnClick = closeOnClick;
            clickEvent = delegate () { CoolDialogueUIManager.theUI.SetTextbox(Main.rand.Next(nextTextboxPool)); };
        }
        */

        public override void Recalculate()
        {
            //Recalculates it
            Label = _label;
            _verticesCalculated = false;
        }
    }
}
