using ReLogic.Utilities;
using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class WulfrumFidgetGrappler : FidgetToyUI
    {
        public static readonly SoundStyle ScreamStartSound = new SoundStyle("CalamityFables/Sounds/Fidget/WulfrumGrapplerScreamStart") { PlayOnlyIfFocused = true, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };
        public static readonly SoundStyle ScreamLoopSound = new SoundStyle("CalamityFables/Sounds/Fidget/WulfrumGrapplerScreamLoop") { IsLooped = true, PlayOnlyIfFocused = true };
        public SlotId loopSlotID;
        public SlotId screamSlotID;
        public float timeSpentSpinning = 0f;
        public float timeSpentSpinningBuffer = 0f;

        public bool outsideTheScreen;
        public float consecutiveHits;
        public float consecutiveOuchs;
        public float lastHighScore;

        public List<VerletPoint> segments;
        public const int segmentCount = 10;
        public const float segmentDistance = 15f;
        public Vector2 GrapplerPos => segments[segmentCount - 1].position;

        public PrimitiveTrail armTrail;

        public override InterfaceScaleType Scale => InterfaceScaleType.None;

        public WulfrumFidgetGrappler()
        {
            InitializeSegments();
        }

        public void InitializeSegments()
        {
            segments = new List<VerletPoint>(segmentCount);
            for (int i = 0; i < segmentCount; i++)
            {
                VerletPoint segment = new VerletPoint(Main.MouseScreen + Vector2.UnitY * segmentDistance * i);
                segments.Add(segment);
            }

            segments[0].locked = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (segments is null)
                InitializeSegments();

            segments[0].oldPosition = segments[0].position;
            segments[0].position = Main.MouseScreen;

            segments = VerletPoint.SimpleSimulation(segments, segmentDistance, 30, 0.4f);

            if (armTrail is null)
                armTrail = new PrimitiveTrail(segmentCount, ArmWidth, ArmColor);
            armTrail.SetPositions(segments.Select(x => x.position), FablesUtils.SmoothBezierPointRetreivalFunction);

            float spinSpeed = segments[segmentCount - 1].position.Distance(segments[segmentCount - 1].oldPosition);
            bool loopPlaying = SoundEngine.TryGetActiveSound(loopSlotID, out var loopSound) && loopSound.IsPlaying;
            bool screamStartPlaying = SoundEngine.TryGetActiveSound(screamSlotID, out var screamSound) && screamSound.IsPlaying;

            if (spinSpeed > 15f)
            {
                timeSpentSpinningBuffer = 1f;

                if (!loopPlaying)
                {
                    loopSlotID = SoundEngine.PlaySound(ScreamLoopSound);
                    loopPlaying = SoundEngine.TryGetActiveSound(loopSlotID, out loopSound);
                    if (loopPlaying)
                    {
                        loopSound.Volume = 0f;
                        loopSound.Update();
                    }
                }


                else
                {
                    loopSound.Volume += 1 / (60f * 0.4f) * 0.3f;
                    if (loopSound.Volume > 0.3f)
                        loopSound.Volume = 0.3f;

                    float pitchRandomness = Math.Clamp((spinSpeed - 20f) / 60f, 0f, 1f) * Main.rand.NextFloat(-0.05f, 0.05f);
                    float screamPitch = 0.4f * (spinSpeed - 15f) / 100f + pitchRandomness;

                    screamPitch = Math.Min(screamPitch, 0.9f);

                    loopSound.Sound.Pitch = screamPitch;
                    loopSound.Update();
                }
            }
            else
            {
                if (loopPlaying)
                {
                    loopSound.Volume -= 0.01f;
                    if (loopSound.Volume <= 0f)
                        loopSound.Stop();

                    loopSound.Update();
                }

                timeSpentSpinningBuffer -= 1 / 30f;
            }

            SoundHandler.TrackSoundWithFade(loopSlotID);

            if (timeSpentSpinningBuffer > 0f)
                timeSpentSpinning += 1 / 60f;
            else
                timeSpentSpinning = 0f;

            bool previousOutsideTheScreenState = outsideTheScreen;
            outsideTheScreen = !new Rectangle(0, 0, Main.screenWidth, Main.screenHeight).Contains(segments[segmentCount - 1].position.ToPoint());

            if (!previousOutsideTheScreenState && outsideTheScreen)
            {
                SoundEngine.PlaySound(NPCs.Wulfrum.WulfrumGrappler.HopSound with { MaxInstances = 0, Pitch = consecutiveHits / 46f });

                if (consecutiveOuchs < 15)
                    SoundEngine.PlaySound(NPCs.Wulfrum.WulfrumGrappler.AngryOuch with { MaxInstances = 2, Pitch = consecutiveOuchs / 15f * 0.3f, Volume = 1f - (consecutiveOuchs / 15f) });

                consecutiveHits = (float)Math.Ceiling(consecutiveHits) + 1;
                consecutiveOuchs = (float)Math.Ceiling(consecutiveOuchs) + 1;
                lastHighScore = consecutiveHits;

                CameraManager.Quake = Math.Max(consecutiveHits * 9f, CameraManager.Quake);
            }

            consecutiveHits -= 1 / (60f * 0.1f);
            if (consecutiveHits < lastHighScore - 3f)
            {
                consecutiveHits -= 0.6f;
                CameraManager.Quake *= 0.9f;
            }

            if (consecutiveOuchs < lastHighScore - 3f)
            {
                consecutiveOuchs -= 0.6f;
            }

            consecutiveHits = MathHelper.Clamp(consecutiveHits, 0, 46);
            consecutiveOuchs = MathHelper.Clamp(consecutiveOuchs - 0.03f, 5f, 15f);
        }

        public float ArmWidth(float progress) => 2f;
        public Color ArmColor(float progress) => Color.DarkSlateGray;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D bodyTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumGrappler").Value;
            Texture2D handTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumGrappler_Hand").Value;
            Texture2D antiBloom = AssetDirectory.CommonTextures.BigBloomCircle.Value;

            Vector2 position = segments[0].position;

            //Draw anti bloom
            spriteBatch.Draw(antiBloom, GrapplerPos, null, Color.Black * 0.2f, 0, antiBloom.Size() / 2f, 0.5f, SpriteEffects.None, 0);
            spriteBatch.Draw(antiBloom, GrapplerPos, null, Color.Black * 0.1f, 0, antiBloom.Size() / 2f, 0.7f, SpriteEffects.None, 0);

            bool cachedViewMatrixPrims = RenderTargetsManager.NoViewMatrixPrims;
            RenderTargetsManager.NoViewMatrixPrims = true;
            armTrail?.Render(null, Vector2.Zero);
            RenderTargetsManager.NoViewMatrixPrims = cachedViewMatrixPrims;

            float centrifuge = Utils.GetLerpValue(10f, 90f, segments[0].position.Distance(segments[0].oldPosition), true);
            Vector2 centrifugalSquish = new Vector2(1 - centrifuge * 0.36f, 1 + centrifuge * 1.2f);

            Rectangle bodyFrame = bodyTex.Frame(1, 6, 0, 0, 0, -2);
            float bodyRotation = segments[segmentCount - 1].position.AngleTo(segments[segmentCount - 2].position) + MathHelper.PiOver2;
            Vector2 bodyOrigin = new Vector2(bodyFrame.Width / 2, 8);
            spriteBatch.Draw(bodyTex, GrapplerPos, bodyFrame, Color.White, bodyRotation, bodyOrigin, centrifugalSquish, SpriteEffects.None, 0);


            Rectangle handFrame = handTex.Frame(1, 6, 0, 5, 0, -2);
            float handRotation = segments[1].position.AngleTo(segments[0].position) + MathHelper.PiOver2;
            spriteBatch.Draw(handTex, segments[0].position, handFrame, Color.White, handRotation, handFrame.Size() / 2f, 1f, SpriteEffects.None, 0);
        }
    }
}