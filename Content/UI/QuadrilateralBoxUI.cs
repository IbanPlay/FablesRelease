using ReLogic.Utilities;
using Terraria.UI;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.UI
{
    /// <summary>
    /// Helper struct that stores two vertices and lerps between the two, used to endlessly shift the vertices making up textboxes and button outlines
    /// </summary>
    public struct ShiftingVertex
    {
        public float time;
        public Vector2 displacement;
        public Vector2 nextDisplacement;

        public ShiftingVertex(float displacementLenght)
        {
            this.displacement = Main.rand.NextVector2Circular(displacementLenght, displacementLenght);
            this.nextDisplacement = Main.rand.NextVector2Circular(displacementLenght, displacementLenght);
            time = Main.rand.NextFloat(0.3f);
        }

        public Vector2 Displacement => Vector2.Lerp(displacement, nextDisplacement, PolyInOutEasing(time % 1, 4));
    }

    /// <summary>
    /// A box with text inside that has wobbly vertices. Doesnt have any rendering code on its own, instead being used as a base class for <see cref="QuadrilateralBoxWithPortraitUI"/>
    /// </summary>
    public class QuadrilateralBoxUI : UIElement
    {
        #region Colors
        public Color OutlineColor { get; set; }
        public Color HoveredOutlineColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color HoveredBackgroundColor { get; set; }
        public Color HoveredDoubleOutlineColor { get; set; }
        #endregion

        /// <summary>
        /// Offsets added to the base trapezoidal vertices
        /// </summary>
        public ShiftingVertex[] vertexShifts;

        private Vector2 _origin;
        private Vector2 _dimensions;
        private float _rotation;

        private List<Vector2> _vertices;
        private bool _verticesCalculated = false;

        /// <summary>
        /// Center of the textbox
        /// </summary>
        public Vector2 Origin { get => _origin; set { _origin = value; _verticesCalculated = false; } }
        /// <summary>
        /// Size of the button's rectangular base (before being turned trapezoidal and adding in the displacement to the vertices)
        /// </summary>
        public Vector2 Dimensions {
            get => _dimensions * CoolDialogueUI.uiScale; 
            set {
                _verticesCalculated = false;
                _dimensions = value;
            }
        }

        /// <summary>
        /// Textbox's rotation. Rotates depending on the player's world position relative to the npc who's talking
        /// </summary>
        public float Rotation { get => _rotation; set { _rotation = value; _verticesCalculated = false; } }

        /// <summary>
        /// Maximum lenght of the vertices's displacement from their base dimensions
        /// </summary>
        public float Wobbliness { get; set; }
        /// <summary>
        /// Speed at which the vertex offsets change from one offset to the other. If set to 1, there will be 1 displacement per second
        /// </summary>
        public float WobbleSpeedMultiplier { get; set; }

        /// <summary>
        /// Timer that keeps increasing when the box is on screen. primarily used to stop the box from rendering on the first frame of its existence, and to make the outline thickness oscillate
        /// </summary>
        public float Timer { get; set; }


        internal PrimitiveQuadrilateral drawer;
        internal PrimitivePolygonOutline outlineDrawerSquared;

        private PolygonUpscalingAlgorithm _outlineUpscaling;
        /// <summary>
        /// Algorithm used to upscale the outline from the base vertices. Defaults to <see cref="UpscalePolygonFromCenter"/>, but you may wish to use a neater looking one <br/>
        /// <see cref="CooldownRackUI"/> instead sets <see cref="UpscalePolygonByCombiningPerpendiculars"/> when initializing the textbox
        /// </summary>
        public PolygonUpscalingAlgorithm OutlineUpscalingAlgorithm { get => _outlineUpscaling ?? UpscalePolygonFromCenter; set => _outlineUpscaling = value; }

        public bool UsesDoubleOutline { get; set; }
        public float DoubleOutlineDistance { get; set; }
        public float DoubleOutlineThickness { get; set; }

        #region Setence stuff
        public bool AutoAdjustSizeToSetence { get; set; }
        internal bool adjustedSize = false;

        internal AwesomeSentence _setence;
        public AwesomeSentence Setence {
            get => _setence;
            set {
                _setence = value;
                adjustedSize = false;
            }
        }

        public float SetenceTimer { get; set; }
        internal SlotId SpeechSlot;
        public float SetenceSoundTimer { get; set; }
        #endregion
        public bool HoverInteractionAble => Setence == null || SetenceTimer >= Setence.maxProgress && CoolDialogueUIManager.theUI.clickEvent != null;

        /// <summary>
        /// Icon that appears in the bottom left corner of the textbox when the character has finished talking in this textbox and there is an event that is set to happen when clicking on it
        /// </summary>
        public Asset<Texture2D> DialogueOverIcon { get; set; }

        /// <summary>
        /// Which side of the talking NPC the player is on. <see cref="SideWeight"/> lerps towards it ovr time, creating the smooth side transition when switching sides
        /// </summary>
        public int PlayerSide { get; set; }
        /// <summary>
        /// Weight 
        /// </summary>
        public float SideWeight { get; set; }
        /// <summary>
        /// How scaled up the side opposite to the player is, creating the cool trapezoid shape
        /// </summary>
        public float OtherSideScaleUp { get; set; }
        /// <summary>
        /// Timer used for the box to appear, so it doesnt suddenly pop into existence. Full expansion lasts 0.6 seconds
        /// </summary>
        public float ExpansionTimer { get; set; }

        /// <summary>
        /// Base vertices of the box, a trapezoid with the specified <see cref="Dimensions"/> and one side skewed to be larger according to <see cref="OtherSideScaleUp"/>. <br/>
        /// The player doesnt see these vertices and instead sees <see cref="DisplacedVertices"/>, which are obtained by combining these vertices with the vertex vertex displacements to get a funky shifting look  <br/>
        ///  Only regenerated when needed.
        /// </summary>
        public List<Vector2> Vertices {
            get {
                if (_verticesCalculated)
                    return _vertices;

                _vertices = new List<Vector2>();

                Vector2 unitVectorX = (Vector2.UnitX * Dimensions.X * 0.5f).RotatedBy(Rotation);
                Vector2 unitVectorY = (Vector2.UnitY * Dimensions.Y * 0.5f).RotatedBy(Rotation);

                //Textbox expanding horizontally and vertically when opened
                unitVectorX *= 0.5f + 0.5f * EaseInOutBack(Utils.GetLerpValue(0f, 0.7f, SineInOutEasing(ExpansionTimer, 1), true), 1);
                unitVectorY *= 0.3f + 0.7f * EaseInOutBack(Utils.GetLerpValue(0.3f, 1f, SineInOutEasing(ExpansionTimer, 1), true), 1);

                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        float t = Utils.GetLerpValue(-j, j, SideWeight, true);

                        float multiplier = MathHelper.Lerp(1f, OtherSideScaleUp, t);
                        //Distort the bottom half a bit more than the top half
                        if (i == 1)
                            multiplier *= 1.1f;

                        _vertices.Add(Origin + unitVectorX * j + unitVectorY * i * multiplier);
                    }
                }

                _verticesCalculated = true;
                return _vertices;
            }
        }

        /// <summary>
        /// Vertices of the textbox as they appear to the player. Obtained by adding the random vertex shifts to the constant trapezoidal vertices of the textbox
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
        /// Vertices of the textbox's extra outline around the main box. Obtained by upscaling the regular vertices by using the specified <see cref="OutlineUpscalingAlgorithm"/>
        /// </summary>
        public List<Vector2> OutlineVertices {
            get {
                List<Vector2> outlineVertices = DisplacedVertices;
                Vector2 cache = outlineVertices[3];
                outlineVertices[3] = outlineVertices[2];
                outlineVertices[2] = cache;

                return OutlineUpscalingAlgorithm(outlineVertices, (DoubleOutlineDistance + (float)Math.Sin(Timer - 0.8f) * 0.04f) * CoolDialogueUI.uiScale);
            }
        }

        public QuadrilateralBoxUI()
        {
            Timer = 0f;
            Wobbliness = 0f;
            WobbleSpeedMultiplier = 1f;
            OtherSideScaleUp = 1f;
            vertexShifts = new ShiftingVertex[4];
            for (int i = 0; i < 4; i++)
            {
                vertexShifts[i] = new ShiftingVertex(Wobbliness);
            }
        }

        public override void OnActivate()
        {
            SetenceTimer = 0;
            ExpansionTimer = 0;
            Vertices.Clear();
            _verticesCalculated = false;
            Timer = 0;
        }

        public override void OnDeactivate()
        {
            Timer = 0;
            SetenceTimer = 0;
            ExpansionTimer = 0;
            Vertices.Clear();
            _verticesCalculated = false;
        }

        public override void Update(GameTime gameTime)
        {
            //Augment the timer
            Timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Textbox height needs to extend down to be able to handle tall setences with multiple lines
            if (Setence != null && AutoAdjustSizeToSetence && !adjustedSize)
            {
                Dimensions = new Vector2(_dimensions.X, Math.Max(60, Setence.GetTotalHeight() * 0.9f + 30f));
                adjustedSize = true;
            }

            //Expand the box
            if (ExpansionTimer < 1f)
            {
                ExpansionTimer += 1 / (60f * 0.6f);
                if (ExpansionTimer > 1f)
                    ExpansionTimer = 1f;
            }

            //Slide the ui's weight towards the players side
            if (SideWeight != PlayerSide)
            {
                SideWeight = MathHelper.Lerp(SideWeight, PlayerSide, 0.2f);
                if (Math.Abs(SideWeight - PlayerSide) < 0.01f)
                    SideWeight = PlayerSide;
            }


            //Write the setence out
            UpdateSetence(gameTime);

            //Calculate vertex shifts
            UpdateShiftingVertices(gameTime);

            //Set the vertices on the things
            UpdatePrimitives();

            base.Update(gameTime);
        }

        /// <summary>
        /// Scrolls the setence by increasing a timer, playing the sound bits as we go through the dialogue (based on the specified scroll speed of the text and such)
        /// </summary>
        /// <param name="gameTime"></param>
        public void UpdateSetence(GameTime gameTime)
        {
            if (Setence != null && ExpansionTimer >= 1)
            {
                //Talky
                if (SetenceTimer < Setence.maxProgress && Setence.voice != null)
                {
                    SetenceSoundTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    float currentSnippetDelay = Setence.GetDelayAtProgress(SetenceTimer);

                    //"Celeste style" voice bytes, aka continous syllables, 
                    if (currentSnippetDelay < 0.13f)
                    {
                        if (!SoundEngine.TryGetActiveSound(SpeechSlot, out var activeSound) || !activeSound.IsPlaying)
                        {
                            SoundStyle voiceVariant = Setence.voice.regularSyllables;
                            if (Main.rand.NextBool(6) || SetenceTimer + SetenceSoundTimer >= Setence.maxProgress || Setence.GetDelayAtProgress(SetenceTimer + currentSnippetDelay) >= 0.13f)
                                voiceVariant = Setence.voice.endSyllables;

                            SpeechSlot = SoundEngine.PlaySound(voiceVariant);
                        }
                    }

                    //"Undertale style" voice byes, aka one character per syllable
                    else
                    {
                        if (SetenceSoundTimer <= 0 && Setence.GetLetterAtProgress(SetenceTimer) != " ")
                        {
                            SoundEngine.PlaySound(Setence.voice.endSyllables);
                            SetenceSoundTimer = currentSnippetDelay;
                        }
                    }
                }

                SetenceTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (Setence.maxProgress < SetenceTimer)
                    SetenceTimer = Setence.maxProgress;
            }
        }
       
        /// <summary>
        /// Goes over every shifting vertice and updates its timer, so they can constantly move around to their next intended destination. Sets a new target for the vertice's wobble target if they reached the current one
        /// </summary>
        public void UpdateShiftingVertices(GameTime gameTime)
        {
            for (int i = 0; i < 4; i++)
            {
                ShiftingVertex vertex = vertexShifts[i];
                vertex.time += (float)gameTime.ElapsedGameTime.TotalSeconds * WobbleSpeedMultiplier;

                if (vertex.time > 1f)
                {
                    vertex.displacement = vertex.nextDisplacement;
                    vertex.nextDisplacement = Main.rand.NextVector2Circular(Wobbliness, Wobbliness);
                    vertex.time = vertex.time % 1;
                }

                vertexShifts[i] = vertex;
            }
        }

        /// <summary>
        /// Updates the primitive renderers to use the vertex positions we calculated
        /// </summary>
        public void UpdatePrimitives()
        {
            if (drawer != null)
                drawer.Vertices = DisplacedVertices.ToArray();

            if (outlineDrawerSquared != null && UsesDoubleOutline)
            {
                outlineDrawerSquared.Vertices = OutlineVertices.ToArray();
                outlineDrawerSquared.outlineThickness = (DoubleOutlineThickness + (0.5f + 0.5f * (float)Math.Sin(Timer - 0.3f)) * 4f) * CoolDialogueUI.uiScale;
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            if (HoverInteractionAble)
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.4f });

            base.MouseOver(evt);
        }

        public override bool ContainsPoint(Vector2 point)
        {
            List<Vector2> vert = DisplacedVertices;
            Vector2 cache = vert[2];
            vert[2] = vert[3];
            vert[3] = cache;

            return FablesUtils.InsidePolygon(point * Main.UIScale, vert);
        }
    }
}
