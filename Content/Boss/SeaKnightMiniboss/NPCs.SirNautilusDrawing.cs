using Terraria.Graphics.Effects;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public partial class SirNautilus : ModNPC
    {
        #region Textures and frames
        public override string Texture => AssetDirectory.SirNautilus + Name;
        public string SignathionTexture => AssetDirectory.SirNautilus + "Signathion";

        //Sheet yourself
        public static Asset<Texture2D> NautilusSheet;
        public static Asset<Texture2D> NautilusTridentSheet;

        public static Asset<Texture2D> NautilusRidingSheet;
        public static Asset<Texture2D> NautilusRidingOverSheet;

        public static Asset<Texture2D> SignathionSheet1;
        public static Asset<Texture2D> SignathionSheet2;
        public static Asset<Texture2D> SignathionSheet3;

        private int xFrame;
        private int yFrame;

        private float frameCounterSig;
        private int xFrameSig;
        private int yFrameSig;

        static int nautieFrameWidth = 214;
        static int nautieFrameHeight = 148;

        static int nautieRiderFrameWidth = 130;
        static int nautieRiderFrameHeight = 152;

        static int signathionFrameWidth = 330;
        static int signathionFrameHeight = 190;

        public Texture2D UsedSignathionSheet => xFrameSig > 11 ? SignathionSheet3.Value : xFrameSig > 5 ? SignathionSheet2.Value : SignathionSheet1.Value;
        public int signathionSheetXOffset => (int)(xFrameSig / 6) * (signathionFrameWidth + 2) * 6;
        #endregion

        #region Other visual variables
        public static int TridentReapparitionTime = 90;
        public int tridentReapparitionTimer;

        public Vector2 slashShinePosition;

        public PrimitiveTrail CycloneDashTrailDrawer;
        public Vector2 cycloneDashOrigin;

        public List<Vector2> cycloneDashPoints;
        public List<Vector2> cycloneDashPoints2;
        public List<Vector2> cycloneDashPoints3;

        public float usedRotationOffset;
        public List<float> cycloneDashRotations;

        public PrimitiveTrail SlamTrailDrawer;
        public List<Vector2> slamCache;
        public float slamStretch;

        public int sigBarDissapearCounter;

        public float InvincibilitySpectreVisualsStrenght;
        public float SignathionFadeOpacity = 0f;
        #endregion

        #region Find frame
        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                FindNautilusFrame();
                return;
            }

            if (InACutscene)
            {
                //The initial wait is animated like normal
                if (SubState == ActionState.CutsceneDismountSig || SubState == ActionState.CutsceneDismountSig_InitialWait || SubState == ActionState.CutsceneFightStart_SignathionScream)
                {
                    FindSignathionFrame();
                    FindRidingNautilusFrame();
                }

                //When jumping off/on, signathions dummy is animated like normal, but nautie is manually animated
                if (SubState == ActionState.CutsceneDismountSig_NautilusJumpingOff || SubState == ActionState.CutsceneFightStart_JumpOnSignathion)
                    FindSignathionFrame();

                if (SubState == ActionState.CutsceneFightStart_InitialPose)
                    FindSignathionFrame();

                if (AIState == ActionState.CutsceneDeath)
                {
                    xFrame = 0;
                    yFrame = 0;
                }

                NPC.frame = new Rectangle((nautieFrameWidth + 2) * xFrame, (nautieFrameHeight + 2) * yFrame, nautieFrameWidth, nautieFrameHeight);
                return;
            }

            if (IsSignathionPresent)
            {
                FindSignathionFrame();
                FindRidingNautilusFrame();
                return;
            }

            FindNautilusFrame();
        }

        public void FindNautilusFrame()
        {

            #region Movement
            if (AIState == ActionState.SlowWalk)
            {
                if ((Math.Abs(NPC.velocity.X) > 0.3f || NPC.IsABestiaryIconDummy))
                {
                    xFrame = 1;
                    yFrame %= 10;

                    int walkAnimLength = 10;
                    float walkAnimSpeed = 0.15f + 0.24f * Utils.GetLerpValue(1f, 4f, Math.Abs(NPC.velocity.X), true);

                    NPC.frameCounter += walkAnimSpeed;
                    if (NPC.frameCounter >= 1)
                    {
                        int walkDirection = NPC.direction * Math.Sign(NPC.velocity.X) < 0 ? -1 : 1;

                        NPC.frameCounter = 0;
                        yFrame += walkDirection;

                        if (yFrame >= walkAnimLength)
                            yFrame = 0;

                        if (yFrame < 0)
                            yFrame = walkAnimLength - 1;
                    }
                }

                else
                {
                    xFrame = 0;
                    yFrame = 0;
                }
            }
            #endregion

            #region Jump slam
            else if (SubState == ActionState.JumpSlam_Jumping)
            {
                xFrame = 2;
                yFrame = 0;
            }

            else if (SubState == ActionState.JumpSlam_Holding)
            {
                xFrame = 2;
                yFrame = 1 + (int)((1 - AttackTimer) * 4);
                if (yFrame > 3)
                    yFrame = 3;
            }

            else if (SubState == ActionState.JumpSlam_Diving)
            {
                xFrame = 2;
                yFrame = 3;
            }
            #endregion

            #region Trident throw
            else if (SubState == ActionState.TridentThrow_AtPlayer || SubState == ActionState.TridentThrow_AtCeiling)
            {
                xFrame = 3;
                yFrame = (int)(5 * (1 - AttackTimer));
                if (yFrame > 4)
                    yFrame = 4;
            }

            else if (SubState == ActionState.TridentThrow_RecoveryAnim)
            {
                xFrame = 4;
                yFrame = (int)(7 * (1 - AttackTimer));
                if (yFrame > 6)
                    yFrame = 6;
            }
            #endregion

            #region Cyclone Dash
            else if (SubState == ActionState.TridentSpin_Windup)
            {
                xFrame = 7;
                yFrame = (int)(10 * (1 - AttackTimer));
                if (yFrame > 10)
                    yFrame = 10;
            }

            else if (SubState == ActionState.TridentSpin_DashStart || SubState == ActionState.TridentSpin_Recovery)
            {
                xFrame = 8;

                if (SubState == ActionState.TridentSpin_DashStart)
                    yFrame = Math.Min(3, (int)(3 * (1 - AttackTimer)));

                else
                    yFrame = Math.Min(8, 2 + (int)(6 * (1 - AttackTimer)));
            }
            #endregion

            #region Double Slash
            else if (SubState == ActionState.DoubleSwipe_CloseDistance1)
            {
                xFrame = 5;
                yFrame = (int)(7 * (1 - AttackTimer));

                if (yFrame > 5)
                    yFrame = 5;
            }

            else if (SubState == ActionState.DoubleSwipe_FirstSwipe)
            {
                xFrame = 6;

                float telegraphLenght = DoubleSwipe_CurrentSwingTelegraphLenght;
                if (telegraphLenght > 1 - AttackTimer)
                {
                    yFrame = (int)(3 * SlashTelegraphCompletion);
                    if (yFrame > 2)
                        yFrame = 2;
                }
                else
                {
                    yFrame = 3 + (int)(3 * (float)Math.Pow(SlashAttackCompletion, 1.2f));
                    if (yFrame > 5)
                        yFrame = 5;
                }
            }

            else if (SubState == ActionState.DoubleSwipe_CloseDistance2)
            {
                xFrame = 6;
                yFrame = 6;
            }

            else if (SubState == ActionState.DoubleSwipe_SecondSwipe)
            {
                xFrame = 6;

                float telegraphLenght = DoubleSwipe_CurrentSwingTelegraphLenght;
                if (telegraphLenght > 1 - AttackTimer)
                {
                    yFrame = 6 + (int)(4 * SlashTelegraphCompletion);
                    if (yFrame > 8)
                        yFrame = 8;
                }

                else
                {
                    yFrame = 9 + (int)(3 * (float)Math.Pow(SlashAttackCompletion, 1.4f));
                    if (yFrame > 11)
                        yFrame = 11;
                }
            }
            #endregion

            else if (!InACutscene)
            {
                xFrame = 0;
                yFrame = 0;
            }

            NPC.frame = new Rectangle((nautieFrameWidth + 2) * xFrame, (nautieFrameHeight + 2) * yFrame, nautieFrameWidth, nautieFrameHeight);
        }

        public void FindSignathionFrame()
        {
            bool recoveryIdleAnim = SubState == ActionState.SpecterBolts_Recovery ||
                SubState == ActionState.Charge_Recovery ||
                (SubState == ActionState.Charge_TridentThrow && ExtraMemory == 0) ||
                SubState == ActionState.CutsceneFightStart_InitialPose || SubState == ActionState.CutsceneFightStart_JumpOnSignathion ||
                AIState == ActionState.CutsceneDismountSig;

            bool movingAction = (SubState == ActionState.Charge_GetReady && movementTarget != Vector2.Zero);

            bool usesSimpleYAnimation = false;
            int maxYFrame = 0;
            int oldSigYFrame = yFrameSig;

            if (AIState == ActionState.SlowWalk || recoveryIdleAnim || movingAction)
            {
                //Walk animation
                if ((Math.Abs(NPC.velocity.X) > 0.3f || NPC.IsABestiaryIconDummy) && !recoveryIdleAnim)
                {
                    xFrameSig = 1;

                    int walkAnimLength = 8;
                    float walkAnimSpeed = 0.12f + 0.16f * Utils.GetLerpValue(1f, 4f, Math.Abs(NPC.velocity.X), true);

                    frameCounterSig += walkAnimSpeed;
                    if (frameCounterSig >= 1)
                    {
                        int walkDirection = NPC.direction * Math.Sign(NPC.velocity.X) < 0 ? -1 : 1;

                        frameCounterSig = 0;
                        yFrameSig += walkDirection;

                        if (yFrameSig >= walkAnimLength)
                            yFrameSig = 0;

                        if (yFrameSig < 0)
                            yFrameSig = walkAnimLength - 1;

                        if ((yFrameSig == 3 || yFrameSig == 7) && Collision.SolidCollision(CollisionBoxOrigin + Vector2.UnitY * CollisionBoxHeight, CollisionBoxWidth, 4, CanFallThroughPlatforms().Value))
                            SoundEngine.PlaySound(SignathionStep, NPC.Bottom);
                    }
                }

                //Idle animation
                else
                {
                    xFrameSig = 0;
                    frameCounterSig += 0.1f;

                    if (frameCounterSig >= 1)
                    {
                        frameCounterSig = 0;
                        yFrameSig += 1;
                    }

                    yFrameSig = yFrameSig % 7;
                }
            }

            #region Charge
            else if (SubState == ActionState.Charge_GetReady && movementTarget == Vector2.Zero)
            {
                //8 frames in 0.7 seconds aka 8 / 0.7 frames per second (The anim lasts 9 frames, but the 9th frame is actually a transitory first run frame
                xFrameSig = 2;
                frameCounterSig += 0.21f;

                if (frameCounterSig >= 1)
                {
                    frameCounterSig = 0;
                    yFrameSig += 1;

                    if (yFrameSig >= 8)
                        yFrameSig = 7;
                }
            }

            else if (SubState == ActionState.Charge_Run)
            {
                //This is because the final frame of the chargeup actually starts on a fake first frame for the other frame
                if (xFrameSig != 2)
                    xFrameSig = 3;

                frameCounterSig += 0.18f + 0.05f * DifficultyScale; //11 FPS. 

                if (frameCounterSig >= 1)
                {
                    if (xFrameSig == 2)
                        yFrameSig = 1;
                    else
                        yFrameSig++;

                    frameCounterSig = 0;
                    xFrameSig = 3;

                    if (yFrameSig >= 8)
                        yFrameSig = 0;

                    if ((yFrameSig == 0 || yFrameSig == 4) && Collision.SolidCollision(CollisionBoxOrigin + Vector2.UnitY * CollisionBoxHeight, CollisionBoxWidth, 4, CanFallThroughPlatforms().Value))
                        SoundEngine.PlaySound(SignathionStep, NPC.Center);
                }
            }

            else if (SubState == ActionState.Charge_UpSlice || SubState == ActionState.Charge_TridentThrow && ExtraMemory != 0)
            {
                xFrameSig = 4;
                usesSimpleYAnimation = true;
                maxYFrame = 3;
            }
            #endregion

            #region Tail swipe
            else if (SubState == ActionState.TailSwipe_Telegraph)
            {
                xFrameSig = 5;
                usesSimpleYAnimation = true;
                maxYFrame = 4;
            }

            else if (SubState == ActionState.TailSwipe_Swipe)
            {
                xFrameSig = 6;
                usesSimpleYAnimation = true;
                maxYFrame = 5;
            }

            else if (SubState == ActionState.TailSwipe_Recovery)
            {
                xFrameSig = 7;
                usesSimpleYAnimation = true;
                maxYFrame = 4;
            }
            #endregion

            #region Spectral Bolts
            else if (SubState == ActionState.SpecterBolts_Chargeup)
            {
                xFrameSig = ExtraMemory == 1 ? 10 : 8;
                usesSimpleYAnimation = true;
                maxYFrame = ExtraMemory == 1 ? 6 : 4;
            }

            else if (SubState == ActionState.SpecterBolts_FireBlasts)
            {
                xFrameSig = 9;
                usesSimpleYAnimation = true;
                maxYFrame = 6;
            }

            else if (SubState == ActionState.SpecterBolts_FireShotgun)
            {
                xFrameSig = 11;
                usesSimpleYAnimation = true;
                maxYFrame = 2;
            }
            #endregion

            #region Rockfall
            else if (SubState == ActionState.Rockfall_RepeatedStomps)
            {
                xFrameSig = 12;

                float completion = ((1 - AttackTimer) / (1 / (float)RockfallRepeats)) % 1;
                bool backLegStomp = (int)((1 - AttackTimer) / (1 / (float)RockfallRepeats)) == 1;

                int yFrameInAnimation = (int)(completion * 12);
                if (yFrameInAnimation > 11)
                    yFrameInAnimation = 11;

                yFrameSig = yFrameInAnimation;

                //Switch to the second column
                if (yFrameInAnimation > 5)
                {
                    yFrameSig -= 6;
                    xFrameSig++;
                }

                //Switch to the other stomp leg
                if (backLegStomp)
                    xFrameSig += 2;
            }
            #endregion

            else if (SubState == ActionState.CutsceneFightStart_SignathionScream)
            {
                xFrameSig = 4;
                yFrameSig = (int)(((float)Math.Pow(1 - AttackTimer, 2.5f)) * (4));
                if (yFrameSig > 3)
                    yFrameSig = 3;
            }

            else if (!InACutscene)
            {
                xFrameSig = 0;
                yFrameSig = 0;
            }

            if (usesSimpleYAnimation && maxYFrame > 0)
            {
                yFrameSig = (int)((1 - AttackTimer) * (maxYFrame + 1));
                if (yFrameSig > maxYFrame)
                    yFrameSig = maxYFrame;

                if (SubState == ActionState.TailSwipe_Recovery && oldSigYFrame == 2 && yFrameSig == 3)
                    SoundEngine.PlaySound(SignathionStep, NPC.Bottom);

            }

            NPC.frame = new Rectangle((signathionFrameWidth + 2) * xFrameSig, (signathionFrameHeight + 2) * yFrameSig, signathionFrameWidth, signathionFrameHeight);
        }

        public void FindRidingNautilusFrame()
        {
            if (AIState == ActionState.SlowWalk ||
                AIState == ActionState.Rockfall ||
                (SubState == ActionState.Charge_GetReady && movementTarget != Vector2.Zero) ||
                (SubState == ActionState.Charge_Recovery && ExtraMemory == 0) ||
                SubState == ActionState.TailSwipe_Telegraph ||
                SubState == ActionState.SpecterBolts_Chargeup || SubState == ActionState.SpecterBolts_Recovery ||

                SubState == ActionState.CutsceneFightStart_SignathionScream ||
                SubState == ActionState.CutsceneDismountSig_InitialWait
                )
            {
                xFrame = 0;
                yFrame = 0;
            }

            #region Charge
            else if (SubState == ActionState.Charge_GetReady && yFrameSig > 4)
            {
                xFrame = 4;
                yFrame = yFrameSig - 5;
            }

            else if (SubState == ActionState.Charge_Run && xFrameSig == 2)
            {
                xFrame = 4;
                yFrame = 3;
            }

            else if (SubState == ActionState.Charge_Run)
            {
                xFrame = 5;
                yFrame = yFrameSig % 4;
            }

            else if (SubState == ActionState.Charge_UpSlice)
            {
                xFrame = 6;
                yFrame = yFrameSig;
            }

            else if (SubState == ActionState.Charge_TridentThrow)
            {
                xFrame = 2;
                yFrame = (int)Math.Min(3, (1 - AttackTimer) * 4);
            }

            else if (SubState == ActionState.Charge_Recovery) //&& ExtraMemory != 0 implied from the initial if
            {
                xFrame = 2;
                yFrame = 4 + (int)Math.Min(3, (1 - AttackTimer) * 5);
            }

            #endregion

            #region Tail swipe
            else if (SubState == ActionState.TailSwipe_Swipe)
            {
                xFrame = 3;
                yFrame = yFrameSig;
            }

            else if (SubState == ActionState.TailSwipe_Recovery && yFrameSig <= 1)
            {
                xFrame = 3;
                yFrame = yFrameSig + 6;
            }
            #endregion

            #region Specter bolts
            else if (SubState == ActionState.SpecterBolts_FireBlasts)
            {
                xFrame = 1;
                yFrame = 0;
            }

            else if (SubState == ActionState.SpecterBolts_FireShotgun && yFrameSig <= 1)
            {
                xFrame = 1;
                yFrame = 0;
            }
            #endregion

            else if (!InACutscene)
            {
                xFrame = 0;
                yFrame = 0;
            }
        }

        public Vector2 GetRidingNautilusOffset()
        {
            Vector2 offset = new Vector2(-16, -28);

            #region the ugliest switch statement ever because im litterally just hardcoding frame offsets
            switch (xFrameSig)
            {
                case 0: //Idle
                    switch (yFrameSig)
                    {
                        case 1:
                        case 2:
                        case 3:
                            offset.Y += 2;
                            break;
                        case 5:
                        case 6:
                            offset.Y -= 2;
                            break;
                    }
                    break;

                case 1: // Walk
                    switch (yFrameSig)
                    {
                        case 1:
                        case 2:
                        case 4:
                        case 5:
                        case 6:
                            offset.Y -= 2;
                            break;
                    }
                    break;

                case 2: //Run pre
                    switch (yFrameSig)
                    {
                        case 1:
                        case 2:
                            offset.Y -= 2;
                            break;
                        case 4:
                            offset += new Vector2(-10, 2);
                            break;
                    }
                    break;

                case 5: //Tail swipe pre
                    switch (yFrameSig)
                    {
                        case 0:
                            offset += new Vector2(2, -2);
                            break;
                        case 1:
                            offset += new Vector2(2, 2);
                            break;
                        case 2:
                        case 3:
                            offset += new Vector2(0, -2);
                            break;
                    }
                    break;

                case 7: //Tail swipe post
                    switch (yFrameSig)
                    {
                        case 2:
                            offset += new Vector2(10, 0);
                            break;
                        case 3:
                            offset += new Vector2(4, 0);
                            break;
                        case 4:
                            offset += new Vector2(-2, -2);
                            break;
                    }
                    break;

                case 8: //Spectral rifle charge
                    switch (yFrameSig)
                    {
                        case 0:
                            offset += new Vector2(0, -2);
                            break;
                        case 1:
                            offset += new Vector2(4, -2);
                            break;
                        case 2:
                            offset += new Vector2(8, -4);
                            break;
                        case 3:
                        case 4:
                            offset += new Vector2(10, -4);
                            break;
                    }
                    break;

                case 9: //Spectral rifle shoot
                    switch (yFrameSig)
                    {
                        case 0:
                            offset += new Vector2(-8, 0);
                            break;
                        case 1:
                            offset += new Vector2(-20, 6);
                            break;
                        case 2:
                            offset += new Vector2(-16, 6);
                            break;
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                            offset += new Vector2(-14, 4);
                            break;
                    }
                    break;

                case 10: //Spectral shotgun charge
                    switch (yFrameSig)
                    {
                        case 0:
                            offset += new Vector2(0, -2);
                            break;
                        case 1:
                            offset += new Vector2(6, -2);
                            break;
                        case 2:
                            offset += new Vector2(10, -6);
                            break;
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                            offset += new Vector2(12, -6);
                            break;
                    }
                    break;

                case 11: //Spectral shotgun fire
                    switch (yFrameSig)
                    {
                        case 0:
                            offset += new Vector2(-22, 4);
                            break;
                        case 1:
                            offset += new Vector2(-16, 6);
                            break;
                        case 2:
                            offset += new Vector2(-4, 0);
                            break;
                    }
                    break;

                case 12: //Both rockfall stomps
                case 13:
                case 14:
                case 15:

                    int linearYFrame = yFrameSig;
                    if (xFrameSig % 2 == 1)
                        linearYFrame += 6;

                    switch (linearYFrame)
                    {
                        case 0:
                        case 3:
                        case 9:
                        case 11:
                            offset += new Vector2(0, -6);
                            break;
                        case 2:
                            offset += new Vector2(0, 2);
                            break;
                        case 4:
                        case 5:
                            offset += new Vector2(2, -8);
                            break;
                        case 6:
                        case 10:
                            offset += new Vector2(2, -6);
                            break;
                        case 7:
                            offset += new Vector2(4, 22);
                            break;
                        case 8:
                            offset += new Vector2(4, 20);
                            break;
                    }
                    break;
            }
            #endregion

            //Sig's "heads up" anim is normally used in cfonjuction with the trident slice or throw anim that need no offsets.
            //The start cutscene doesnt do that, so we manually put the frames
            if (SubState == ActionState.CutsceneFightStart_SignathionScream)
            {
                switch (yFrameSig)
                {
                    case 0:
                        offset += new Vector2(2, 0);
                        break;
                    case 1:
                        offset += new Vector2(2, 2);
                        break;
                    case 2:
                        offset += new Vector2(-2, 0);
                        break;
                }
            }

            offset.X *= NPC.spriteDirection;
            return offset;
        }
        #endregion

        #region Primitive trails
        private void UpdateCaches()
        {
            if (slamCache == null)
            {
                slamCache = new List<Vector2>();
                for (int i = 0; i < 15; i++)
                {
                    slamCache.Add(NPC.Bottom - (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 10);
                }
            }
            slamCache.Add(NPC.Bottom - (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 10);

            while (slamCache.Count > 15)
            {
                slamCache.RemoveAt(0);
            }
        }

        private void UpdateTrails()
        {
            SlamTrailDrawer = SlamTrailDrawer ?? new PrimitiveTrail(15, factor =>
            {
                return (factor * 7f + 9f) * (1f - 0.4f * Utils.GetLerpValue(3f, 10f, slamStretch, true));
            }
            , factor =>
            {
                Color trailColor = Color.Lerp(Color.Turquoise, Color.DeepSkyBlue, factor) * factor;

                if (SubState == ActionState.JumpSlam_Recovery)
                    trailColor *= (float)Math.Pow(AttackTimer, 3f);

                return trailColor * 0.8f;
            },
            new TriangularTip(1));

            SlamTrailDrawer.NextPosition = NPC.Bottom;
            SlamTrailDrawer.SetPositions(slamCache);

            if (SubState == ActionState.JumpSlam_Diving)
                slamStretch = NPC.velocity.Length();

            GenerateCycloneSpinTrail();
        }

        public float CycloneTrailWidth(float factor)
        {
            int rotationCount = cycloneDashRotations.Count;
            float closestRotation = cycloneDashRotations[Math.Clamp((int)(rotationCount * factor), 0, rotationCount - 1)] + usedRotationOffset;

            float baseWidth = (5f + 9f * (float)Math.Pow(factor, 3f)) * (float)Math.Pow(AttackTimer, 0.4f);

            baseWidth *= (0.8f + 0.2f * (float)Math.Cos(closestRotation));

            return baseWidth;
        }

        public Color CycloneTrailColor(float factor)
        {
            float baseOpacity = (float)Math.Pow(factor, 1.2f) * (float)Math.Pow(AttackTimer, 0.6f) * 0.7f;

            int rotationCount = cycloneDashRotations.Count;
            float closestRotation = cycloneDashRotations[Math.Clamp((int)(rotationCount * factor), 0, rotationCount - 1)] + usedRotationOffset;

            Color baseColor = Color.Lerp(Color.Yellow, Color.OrangeRed, 0.5f + 0.5f * (float)Math.Sin(closestRotation));
            baseColor *= 0.7f + 0.3f * (float)Math.Sin(closestRotation);

            return baseColor * baseOpacity;
        }

        public void GenerateCycloneSpinTrail()
        {
            int trailLenght = 80;
            CycloneDashTrailDrawer = CycloneDashTrailDrawer ?? new PrimitiveTrail(trailLenght,
                CycloneTrailWidth,
                CycloneTrailColor);

            Vector2 start = cycloneDashOrigin;
            Vector2 end = start with { X = NPC.Center.X };

            float trailSpirals = Utils.GetLerpValue(0, 170, Math.Abs(start.X - end.X));
            trailSpirals *= (0.7f + 0.3f * AttackTimer);

            if (cycloneDashPoints == null)
                cycloneDashPoints = new List<Vector2>();
            cycloneDashPoints.Clear();


            if (cycloneDashRotations == null)
                cycloneDashRotations = new List<float>();
            cycloneDashRotations.Clear();


            float currentAngle = (float)Math.Pow(AttackTimer, 2f) * MathHelper.TwoPi * 0.5f;

            for (int i = 0; i < trailLenght; i++)
            {
                float progress = 1 - i / (float)trailLenght;
                float distanceFromCenter = 20f + 14f * (float)Math.Pow(progress, 0.1f);

                if (progress > AttackTimer)
                    distanceFromCenter += Math.Min(1f, (progress - AttackTimer) / 0.1f) * 30f;

                //float angle = (float)Math.Pow(progress, 1.6f) * MathHelper.TwoPi * trailSpirals + AttackTimer * MathHelper.TwoPi;
                cycloneDashRotations.Add(currentAngle);
                cycloneDashPoints.Add(Vector2.Lerp(start, end, i / (float)trailLenght) + new Vector2(-36f * NPC.spriteDirection, (float)Math.Sin(currentAngle) * distanceFromCenter));

                currentAngle += 0.1f * Math.Min(1f, progress * 20f);
            }

            CycloneDashTrailDrawer.Positions = cycloneDashPoints.ToArray();
        }
        #endregion

        #region Drawing  
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor, 0.4f, 0.1f);

            if (NPC.IsABestiaryIconDummy)
            {
                //Bestiary only displays nautilus, but boss checklist should display both
                if (!drawingBossChecklistDummy)
                {
                    DrawNautilus(spriteBatch, screenPos, drawColor);
                    return false;
                }
                else
                    drawColor = bossChecklistColor;
            }
            else
                drawingBossChecklistDummy = false; //Failsafe

            float sizeMult = NPC.hide ? 0.5f : 1f;

            if (InACutscene)
            {
                if (AIState == ActionState.CutsceneFightStart)
                    MountSignathionCinematicsDrawing(spriteBatch, screenPos, drawColor, sizeMult);

                if (AIState == ActionState.CutsceneDismountSig)
                    DismountSignathionCinematicsDrawing(spriteBatch, screenPos, drawColor, sizeMult);

                if (AIState == ActionState.CutsceneDeath)
                {
                    DrawNautilusesRagdollBones(spriteBatch, screenPos, drawColor, sizeMult);
                    if (SubState == ActionState.CutsceneDeath_ReformBones)
                        DrawNautiluseDeathShine(spriteBatch, screenPos, drawColor, sizeMult);
                }
            }

            else if (IsSignathionPresent)
            {
                NautilusRidingSheet = NautilusRidingSheet ?? ModContent.Request<Texture2D>(Texture + "_Riding");
                NautilusRidingOverSheet = NautilusRidingOverSheet ?? ModContent.Request<Texture2D>(Texture + "_RidingOver");

                Rectangle ridingNautilusFrame = new Rectangle(xFrame * (nautieRiderFrameWidth + 2), yFrame * (nautieRiderFrameHeight + 2), nautieRiderFrameWidth, nautieRiderFrameHeight);
                Vector2 offset = GetRidingNautilusOffset();

                DrawRidingNautilus(screenPos, drawColor, NautilusRidingSheet.Value, ridingNautilusFrame, offset, sizeMult);
                DrawSignathion(spriteBatch, screenPos, drawColor, NPC.Bottom, sizeMult);
                DrawRidingNautilus(screenPos, drawColor, NautilusRidingOverSheet.Value, ridingNautilusFrame, offset, sizeMult);

                if (SubState == ActionState.Charge_TridentThrow)
                {
                    Vector2 kidNamedShoulder = NPC.Center + NPC.GfxOffY();
                    Vector2 shoulderOffset;
                    switch (yFrame)
                    {
                        case 0:
                            shoulderOffset = Vector2.UnitX * 7f;
                            break;
                        case 1:
                            shoulderOffset = Vector2.UnitX * 7f - Vector2.UnitY * 2f;
                            break;
                        case 2:
                            shoulderOffset = Vector2.UnitX * 8f;
                            break;
                        case 3:
                        case 4:
                        default:
                            shoulderOffset = Vector2.UnitX * 11f;
                            break;
                    }

                    kidNamedShoulder += (shoulderOffset with { X = shoulderOffset.X * NPC.spriteDirection }) * NPC.scale;
                    Vector2 kidNamedShoulderButStatic = NPC.Center + NPC.GfxOffY() + Vector2.UnitX * 8f * NPC.spriteDirection * NPC.scale;

                    offset += new Vector2(0, -30f);
                    kidNamedShoulder += offset;
                    kidNamedShoulderButStatic += offset;

                    DrawNautilusTridentThrowTelegraph(spriteBatch, screenPos, drawColor, kidNamedShoulder, kidNamedShoulderButStatic, sizeMult);
                }
            }

            else
            {
                if (SubState == ActionState.JumpSlam_Diving || SubState == ActionState.JumpSlam_Recovery)
                    DrawNautilusSlamTrail(spriteBatch, screenPos, drawColor, sizeMult);

                if (SubState == ActionState.TridentSpin_Recovery)
                    DrawNautilusCycloneTrail(spriteBatch, screenPos, drawColor, sizeMult);

                DrawNautilus(spriteBatch, screenPos, drawColor, sizeMult);

                if (SubState == ActionState.TridentThrow_AtPlayer || SubState == ActionState.TridentThrow_AtCeiling)
                {
                    Vector2 kidNamedShoulder = NPC.Center + NPC.GfxOffY();
                    Vector2 shoulderOffset;
                    switch (yFrame)
                    {
                        case 0:
                            shoulderOffset = Vector2.UnitX * 7f;
                            break;
                        case 1:
                            shoulderOffset = Vector2.UnitX * 7f - Vector2.UnitY * 2f;
                            break;
                        case 2:
                            shoulderOffset = Vector2.UnitX * 8f;
                            break;
                        case 3:
                        case 4:
                        default:
                            shoulderOffset = Vector2.UnitX * 11f;
                            break;
                    }

                    kidNamedShoulder += (shoulderOffset with { X = shoulderOffset.X * NPC.spriteDirection }) * NPC.scale;
                    Vector2 kidNamedShoulderButStatic = NPC.Center + NPC.GfxOffY() + Vector2.UnitX * 8f * NPC.spriteDirection * NPC.scale;

                    DrawNautilusTridentThrowTelegraph(spriteBatch, screenPos, drawColor, kidNamedShoulder, kidNamedShoulderButStatic, sizeMult);
                }

                if (ActiveSlash != 0)
                    DrawNautilusTridentLensFlare(spriteBatch, screenPos, drawColor, sizeMult);
                else
                    slashShinePosition = Vector2.Zero;
            }
            return false;
        }

        public void DrawSignathion(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Vector2 position = default, float sizeMultiplier = 1f)
        {
            if (position == default)
                position = NPC.Bottom;

            SignathionSheet1 = SignathionSheet1 ?? ModContent.Request<Texture2D>(SignathionTexture + "1");
            SignathionSheet2 = SignathionSheet2 ?? ModContent.Request<Texture2D>(SignathionTexture + "2");
            SignathionSheet3 = SignathionSheet3 ?? ModContent.Request<Texture2D>(SignathionTexture + "3");

            Texture2D signathion = UsedSignathionSheet;

            Rectangle frame = new Rectangle(xFrameSig * (signathionFrameWidth + 2), yFrameSig * (signathionFrameHeight + 2), signathionFrameWidth, signathionFrameHeight);
            frame.X -= signathionSheetXOffset; //Account for multiple sheeets

            Vector2 origin = frame.Size();
            origin.X /= 2;


            Effect ghostlyEffect = Scene["SignathionGhostlyAura"].GetShader().Shader;

            ghostlyEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.06f);
            ghostlyEffect.Parameters["sourceFrame"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
            ghostlyEffect.Parameters["texSize"].SetValue(signathion.Size());
            ghostlyEffect.Parameters["noiseTex"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TurbulentNoise").Value);
            ghostlyEffect.Parameters["lightColor"].SetValue(drawColor);

            float sineAlternation = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly);
            float effectOpacityTotal = Math.Max((0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.2f)) - 0.3f, 0f) * (1 / 0.7f);

            float fadeStrenght = (8f + 2f * sineAlternation * (1 - InvincibilitySpectreVisualsStrenght)) - (6f + sineAlternation) * InvincibilitySpectreVisualsStrenght;

            ghostlyEffect.Parameters["noiseFadeStrenght"].SetValue(fadeStrenght);
            ghostlyEffect.Parameters["upwardsGradientOpacity"].SetValue(0.3f);
            ghostlyEffect.Parameters["upwardsGradientStrenght"].SetValue(7f - (5.5f + sineAlternation) * InvincibilitySpectreVisualsStrenght);

            ghostlyEffect.Parameters["glowColor"].SetValue(new Vector4(0f, 0.5f, 0.8f + 0.2f * sineAlternation, 1f));
            ghostlyEffect.Parameters["outlineOpacity"].SetValue(InvincibilitySpectreVisualsStrenght * sineAlternation * 0.7f);
            ghostlyEffect.Parameters["opacity"].SetValue(1f);
            ghostlyEffect.Parameters["maxEffectOpacity"].SetValue((0.5f + 0.3f * InvincibilitySpectreVisualsStrenght) * (MathHelper.Lerp(effectOpacityTotal, 1f, InvincibilitySpectreVisualsStrenght)));

            ghostlyEffect.Parameters["vanishCompletion"].SetValue(SignathionFadeOpacity);
            ghostlyEffect.Parameters["vanishingNoise"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "CertifiedCrustyNoise").Value);

            Matrix matrix = Main.GameViewMatrix.TransformationMatrix;
            if (NPC.IsABestiaryIconDummy)
                matrix = Main.UIScaleMatrix;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, ghostlyEffect, matrix);

            Main.EntitySpriteDraw(signathion, (position - screenPos + Vector2.UnitY * 2) * sizeMultiplier, frame, drawColor, 0f, origin, NPC.scale * sizeMultiplier, NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
        }

        public void DrawRidingNautilus(Vector2 screenPos, Color drawColor, Texture2D texture, Rectangle frame, Vector2 offset, float sizeMultiplier = 1f)
        {
            Vector2 origin = frame.Size();
            origin.X /= 2;

            Main.EntitySpriteDraw(texture, (NPC.Bottom - screenPos + Vector2.UnitY * 2 + offset) * sizeMultiplier, frame, drawColor, 0f, origin, NPC.scale * sizeMultiplier, NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }


        public void DrawNautilus(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMultiplier = 1f)
        {
            NautilusSheet = NautilusSheet ?? ModContent.Request<Texture2D>(Texture);
            Texture2D tex = NautilusSheet.Value;

            bool flipped = NPC.spriteDirection > 0;

            Rectangle frame = new Rectangle(xFrame * (nautieFrameWidth + 2), yFrame * (nautieFrameHeight + 2), nautieFrameWidth, nautieFrameHeight);
            Vector2 origin = new Vector2(58, 126);
            if (flipped)
                origin.X = frame.Width - origin.X;

            if ((SubState == ActionState.CutsceneFightStart_JumpOnSignathion || SubState == ActionState.CutsceneDismountSig_NautilusJumpingOff) && xFrame == 2)
                origin.Y -= 20;

            if (xFrame < 2)
                DrawNautilusesTrident(spriteBatch, screenPos, drawColor, frame, origin, sizeMultiplier);

            Vector2 scale = new Vector2(NPC.scale);
            if (SubState == ActionState.JumpSlam_Diving)
            {
                scale.Y *= Math.Max(1.3f - (float)Math.Pow(AttackTimer, 5f), 1f);
                scale.X *= 1 - 0.3f * (float)Math.Pow(1 - AttackTimer, 0.7f);
            }

            Main.EntitySpriteDraw(tex, (NPC.Bottom + NPC.GfxOffY() - screenPos + Vector2.UnitY * 2) * sizeMultiplier, frame, drawColor, NPC.rotation, origin, scale * sizeMultiplier, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        public void DrawNautilusesTrident(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Rectangle frame, Vector2 origin, float sizeMultiplier = 1f)
        {
            bool flipped = NPC.spriteDirection > 0;

            NautilusTridentSheet = NautilusTridentSheet ?? ModContent.Request<Texture2D>(Texture + "_TridentSheet");
            Texture2D tex = NautilusTridentSheet.Value;
            bool coolTrident = tridentReapparitionTimer > 0;
            Vector2 tridentOffset = Vector2.Zero;

            if (coolTrident)
            {
                tridentOffset = -Vector2.UnitY * 56f * (float)Math.Pow(tridentReapparitionTimer / (float)TridentReapparitionTime, 3.5f);

                Effect reapparification = Scene["NautilusTridentApparification"].GetShader().Shader;
                reapparification.Parameters["completion"].SetValue(1 - tridentReapparitionTimer / (float)TridentReapparitionTime);
                reapparification.Parameters["sourceFrame"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
                reapparification.Parameters["texSize"].SetValue(tex.Size());
                reapparification.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "LargeCracksNoise").Value);
                reapparification.Parameters["lightColor"].SetValue(drawColor);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, reapparification, Main.GameViewMatrix.TransformationMatrix);
            }

            Main.EntitySpriteDraw(tex, (NPC.Bottom + NPC.GfxOffY() + tridentOffset - screenPos + Vector2.UnitY * 2) * sizeMultiplier, frame, drawColor, NPC.rotation, origin, NPC.scale * sizeMultiplier, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

            if (coolTrident)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            }
        }

        public void ManageTridentReapparition()
        {
            if (tridentReapparitionTimer > 0)
            {
                tridentReapparitionTimer--;

                if (tridentReapparitionTimer < TridentReapparitionTime * 0.8f && tridentReapparitionTimer > TridentReapparitionTime * 0.45f && Main.rand.NextBool())
                {
                    Vector2 tridentBottom = NPC.Center + new Vector2(NPC.spriteDirection * -14f, 23f);
                    float upwardsProgression = Utils.GetLerpValue(TridentReapparitionTime * 0.8f, TridentReapparitionTime * 0.45f, tridentReapparitionTimer, true);

                    Vector2 dustPosition = tridentBottom - Vector2.UnitY * upwardsProgression * 56f;
                    dustPosition += Main.rand.NextVector2Circular(8f, 20f);

                    Color dustColor = Main.rand.NextBool(3) ? Color.DeepSkyBlue : Color.Turquoise;

                    Dust glow = Dust.NewDustPerfect(dustPosition, 43, -Vector2.UnitY * Main.rand.NextFloat(1f, 2f) + NPC.velocity * 0.4f, 100, dustColor, Main.rand.NextFloat(0.57f, 1f));
                    glow.noGravity = true;
                }
            }
        }

        public void DrawNautilusTridentThrowTelegraph(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Vector2 shoulder, Vector2 staticShoulder, float sizeMultiplier = 1f)
        {
            Texture2D tridentTex = ModContent.Request<Texture2D>(AssetDirectory.SirNautilus + "NautilusTrident").Value;
            Texture2D armTex = ModContent.Request<Texture2D>(Texture + "_HandUp").Value;
            bool flipped = NPC.spriteDirection > 0;

            Vector2 armOrigin = new Vector2(armTex.Width / 2f, armTex.Height - 5);

            Vector2 throwDirection = NPC.DirectionTo(movementTarget);

            float armRotation = throwDirection.ToRotation();
            armRotation += MathHelper.PiOver4 * (0.5f + 0.5f * (float)Math.Pow(1 - AttackTimer, 1.6f)) * NPC.spriteDirection;

            Vector2 handPosition = staticShoulder + armRotation.ToRotationVector2() * 14f;
            float tridentRotation = throwDirection.ToRotation() + MathHelper.PiOver4 * 0.2f * (float)Math.Pow(1 - AttackTimer, 3.6f) * NPC.spriteDirection;
            handPosition -= tridentRotation.ToRotationVector2() * (float)Math.Pow(1 - AttackTimer, 1.6f) * 9.5f;

            armRotation += (flipped ? MathHelper.Pi : 0);

            Main.EntitySpriteDraw(tridentTex, (handPosition - screenPos) * sizeMultiplier, null, drawColor, tridentRotation, tridentTex.Size() / 2f, NPC.scale * sizeMultiplier, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(armTex, (shoulder - screenPos) * sizeMultiplier, null, drawColor, armRotation, armOrigin, NPC.scale * sizeMultiplier, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        public void DrawNautilusSlamTrail(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMultiplier = 1f)
        {
            Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(4f);

            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
            effect.Parameters["streakScale"].SetValue(1f);

            SlamTrailDrawer?.Render(effect, -Main.screenPosition);
        }

        public void DrawNautilusCycloneTrail(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMultiplier = 1f)
        {
            if (CycloneDashTrailDrawer == null)
                return;

            Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(4f);

            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
            effect.Parameters["streakScale"].SetValue(1f);

            CycloneDashTrailDrawer.Positions = cycloneDashPoints.ToArray();
            usedRotationOffset = MathHelper.TwoPi * 2 / 3f;
            CycloneDashTrailDrawer.Render(effect, -Main.screenPosition);
        }

        public void DrawNautilusTridentLensFlare(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMultiplier = 1f)
        {
            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            Texture2D bloommmmm = AssetDirectory.CommonTextures.BloomCircle.Value;

            float shineOpacity = 0.8f * (float)Math.Sin(ActiveSlashCompletion * MathHelper.Pi);

            List<Vector2> slashPoints = GetSlashPoints(60); //They all go from top (0) to bottom (60) now
            if (ActiveSlash == 2)
                slashPoints.Reverse();

            float progress;
            if (ActiveSlash == 1)
                progress = 0.5f - 0.5f * (float)Math.Pow(SlashAttackCompletion, 0.6f);
            else
                progress = 0.4f + 0.5f * (float)Math.Pow(SlashAttackCompletion, 0.5f);


            Vector2 point = slashPoints[(int)(progress * 60)];

            float unscale = (float)Math.Pow(ActiveSlashCompletion, 2f);
            Vector2 lensFlareScale = NPC.scale * new Vector2(0.2f - 0.1f * unscale, 0.7f + 0.3f * unscale);
            lensFlareScale *= 2;

            shineOpacity *= 1 - unscale;

            slashShinePosition = point;
            Main.EntitySpriteDraw(bloommmmm, (point - screenPos) * sizeMultiplier, null, Color.Goldenrod with { A = 0 } * shineOpacity * 0.3f, MathHelper.PiOver2, bloommmmm.Size() / 2, NPC.scale * 0.3f * sizeMultiplier, 0, 0);

            Main.EntitySpriteDraw(lensFlare, (point - screenPos) * sizeMultiplier, null, Color.Gold with { A = 0 } * shineOpacity * 0.4f, MathHelper.PiOver2, lensFlare.Size() / 2, lensFlareScale * 1.3f * sizeMultiplier, 0, 0);
            Main.EntitySpriteDraw(lensFlare, (point - screenPos) * sizeMultiplier, null, Color.White with { A = 0 } * shineOpacity, MathHelper.PiOver2, lensFlare.Size() / 2, lensFlareScale * sizeMultiplier, 0, 0);

        }
        #endregion

        #region Boss checklist portrait
        private static NPC _bossChecklistDummy;
        private static bool drawingBossChecklistDummy = false;
        private static Color bossChecklistColor;

        public static void DrawBossChecklistPortrait(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            if (_bossChecklistDummy == null)
            {
                _bossChecklistDummy = new NPC();
                _bossChecklistDummy.IsABestiaryIconDummy = true;
                _bossChecklistDummy.SetDefaults(ModContent.NPCType<SirNautilus>());
            }

            Vector2 center = rect.Center.ToVector2();
            center.X -= 90f;
            center.Y -= 30f;


            drawingBossChecklistDummy = true;
            bossChecklistColor = color;

            Main.instance.DrawNPCDirect(spriteBatch, _bossChecklistDummy, false, -center);

            drawingBossChecklistDummy = false;

        }
        #endregion

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            position.X -= 18f * scale;
            if (Main.LocalPlayer.gravDir == -1f)
            {
                position.Y -= Main.screenPosition.Y;
                position.Y = Main.screenPosition.Y + (float)Main.screenHeight - position.Y;
            }

            Texture2D background = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarBack").Value;
            Texture2D texOver = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarFront").Value;

            position.X += texOver.Width / 2;

            float nautilusHealthProgress = Math.Clamp((float)NPC.life / (NPC.lifeMax * OneVOneLifePercent), 0f, 1f);
            float sigHealthProgress = Math.Clamp((float)(NPC.life - NPC.lifeMax * OneVOneLifePercent) / (float)(NPC.lifeMax - NPC.lifeMax * OneVOneLifePercent), 0f, 1f);

            if (IsSignathionPresent)
                sigBarDissapearCounter = 100;
            else if (sigBarDissapearCounter > 0)
                sigBarDissapearCounter--;

            Color sigColor = sigHealthProgress > 0.5f ?
                Color.Lerp(Color.Yellow, Color.LimeGreen, sigHealthProgress * 2 - 1) :
                Color.Lerp(Color.Red, Color.Yellow, sigHealthProgress * 2);
            sigColor *= (sigBarDissapearCounter / 100f);

            Color nautieColor = nautilusHealthProgress > 0.5f ?
                Color.Lerp(Color.MediumTurquoise, Color.MediumSpringGreen, nautilusHealthProgress * 2 - 1) :
                Color.Lerp(Color.DodgerBlue, Color.MediumTurquoise, nautilusHealthProgress * 2);

            nautieColor *= Math.Clamp((1 - sigBarDissapearCounter / 40f), 0f, 1f);

            if (!IsSignathionPresent)
            {
                Rectangle frame = new Rectangle(0, 0, 2 + (int)(nautilusHealthProgress * (texOver.Width - 2)), texOver.Height);
                Main.spriteBatch.Draw(background, position - Main.screenPosition, null, nautieColor, 0, background.Size() / 2, 1, 0, 0);
                Main.spriteBatch.Draw(texOver, position - Main.screenPosition, frame, nautieColor, 0, background.Size() / 2, 1, 0, 0);
            }

            if (sigBarDissapearCounter > 0 && IsSignathionPresent)
            {
                Rectangle frame = new Rectangle(0, 0, 4 + (int)(sigHealthProgress * (texOver.Width - 4)), texOver.Height);
                Main.spriteBatch.Draw(background, position - Main.screenPosition, null, sigColor, 0, background.Size() / 2, 1, 0, 0);
                Main.spriteBatch.Draw(texOver, position - Main.screenPosition, frame, sigColor, 0, background.Size() / 2, 1, 0, 0);
            }

            return false;
        }
    }



    //public interface IFiniteStateAttack
    //{
    //Initialization behavior
    //Stamina
    //States it can switch to
    //Selection percentage based on a delegate
    //Contact damage
    //Behavior
    //Gravity & tilecollide
    //Framing
    //Drawing

    //}
}
