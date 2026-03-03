using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.Utilities;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public partial class Crabulon : ModNPC, ICustomDeathMessages, IDrawOverTileMask
    {
        public VerletNet HangingMycelium;
        public List<CrabulonProp> Props;
        public List<CrabulonArm> Arms;
        public CrabulonPupeteeringRack Rack;
        public CrabulonCables Ropes;

        public static Asset<Texture2D> WebSprite;

        #region Map Icon
        public static int deadEyesMapIcon = 0;

        public static int hairyMapIcon = 0;
        public static int hairyDeadEyesMapIcon = 0;

        public static int shroomyMapIcon = 0;
        public static int shroomyDeadEyesMapIcon = 0;

        public static int mushroomCavesMapIcon = 0;


        public void LoadMapIcons()
        {
            deadEyesMapIcon = Mod.AddBossHeadTexture(AssetDirectory.Crabulon + "CrabulonMapDead");

            hairyMapIcon = Mod.AddBossHeadTexture(AssetDirectory.Crabulon + "CrabulonHairyMap");
            hairyDeadEyesMapIcon = Mod.AddBossHeadTexture(AssetDirectory.Crabulon + "CrabulonHairyMapDead");
            shroomyMapIcon = Mod.AddBossHeadTexture(AssetDirectory.Crabulon + "CrabulonShroomyMap");
            shroomyDeadEyesMapIcon = Mod.AddBossHeadTexture(AssetDirectory.Crabulon + "CrabulonShroomyMapDead");

            mushroomCavesMapIcon = Mod.AddBossHeadTexture(AssetDirectory.Crabulon + "CaveHorrorMap");
        }

        //Modifies the health bar
        public override void BossHeadSlot(ref int index)
        {
            if (chosenSkinIndices[0] == 1)
                index = hairyMapIcon;
            else if (chosenSkinIndices[0] == 2)
                index = shroomyMapIcon;

            if (AIState == ActionState.Desperation)
                index = mushroomCavesMapIcon;
            else if (AIState == ActionState.Dead || (AIState == ActionState.SpawningUp && (int)SubState < (int)ActionState.SpawningUp_Scream))
                index = -1;
        }

        //Modifies the map drawing
        private void ModifyMapIcon(NPC npc, ref byte alpha, ref float headScale, ref float rotation, ref SpriteEffects effects, ref int npcID, ref float x, ref float y)
        {
            if (npc.type == ModContent.NPCType<Crabulon>() && npc.ModNPC is Crabulon crab)
            {
                int deadMapIconType = deadEyesMapIcon;
                if (crab.chosenSkinIndices[0] == 1)
                    deadMapIconType = hairyDeadEyesMapIcon;
                else if (crab.chosenSkinIndices[0] == 2)
                    deadMapIconType = shroomyDeadEyesMapIcon;


                if (crab.AIState == ActionState.Desperation || crab.SubState == ActionState.HuskDrop_Stunned)
                    npcID = deadMapIconType;
            }
        }

        #endregion

        #region Props
        public void UpdateProps()
        {
            //this should never happen, as the props are initialized in setdefaults when crabulon's skin is applied, but just in case
            if (Props == null)
            {
                Props = new List<CrabulonProp>();
                BodySkins[chosenSkinIndices[0]].Decorator(this, Props);
            }

            foreach (CrabulonProp prop in Props)
                prop.Update();
        }
        #endregion

        #region Arms
        public void UpdateArms()
        {
            if (Arms == null)
                InitializeArms();
            foreach (CrabulonArm arm in Arms)
                arm.Update();
        }

        public void SetClawOpenness(float value)
        {
            if (Main.netMode == NetmodeID.Server || Arms == null || Arms.Count < 2)
                return;
            Arms[1].clawOpennessOverride = value;
        }

        public void InitializeArms()
        {
            Arms = new List<CrabulonArm>();

            Arms.Add(new CrabulonArm(this, new Vector2(55, 14), new Vector2(19, 67), 30.5f, 59.5f,
                0.07f, 1.04f, ArmSkins[chosenSkinIndices[6]])
            { bendFlip = true });

            Arms.Add(new CrabulonArm(this, new Vector2(-65, 4), new Vector2(-35, 56), 59.5f, 75.2f,
                0.07f, 1.04f, ViolinArmSkins[chosenSkinIndices[5]]));
        }
        #endregion

        public bool HideAllStrings => (AIState == ActionState.Desperation && (int)SubState > (int)ActionState.Desperation_CinematicWait) || SubState == ActionState.DespawningDropDownDesperation|| AIState == ActionState.DebugDisplay;
        public float GlobalStringOpacityMutliplier {
            get {
                if (AIState == ActionState.Despawning && (int)SubState > (int)ActionState.DespawningScurrySlowly)
                    return AttackTimer;
                if (SubState == ActionState.SpawningUp_Emerge)
                    return 1 - AttackTimer;
                if (SubState == ActionState.ClentaminatedAway_DieHorribly)
                    return Utils.GetLerpValue(0.7f, 1f, AttackTimer, true);
                return 1f;
            }
        }

        public static Color PrimsLightColor;
        public float VineWidth(float progress) => 6f;
        public Color HangingMyceliumColor(float progress) => PrimsLightColor;

        public float visualRotation;
        public float visualRotationExtra;
        public float rotationVelocity;
        public Vector2 visualOffset;
        public Vector2 visualOffsetExtra;
        public Vector2 visualOffsetVelocity;
        public float kineticOffset;
        public float kineticOffsetVelocity;
        public Vector2 VisualCenter => NPC.Center + visualOffset - Vector2.UnitY * 10f * NPC.scale;

        public Asset<Texture2D> ForwardsSprite;
        public bool TopDownView {
            get => topDownView;
            set {
                //Remove invalid leg attach points
                if (!value && topDownView && !Main.dedServ)
                {
                    foreach (CrabulonLeg leg in Legs)
                        if (!leg.grabPosition.HasValue || !Main.tile[leg.grabPosition.Value.ToTileCoordinates()].IsTileSolidOrPlatform())
                        {
                            leg.ReleaseGrip();
                        }
                }

                topDownView = value;
            }
        }

        public Asset<Texture2D> FissureSprite;

        private bool topDownView = false;
        public Asset<Texture2D> TopDownSprite;
        public Asset<Texture2D> FissureTopDownSprite;

        public bool lookingSideways = false;
        public Asset<Texture2D> SidewaysSprite;
        public Asset<Texture2D> FissureSidewaysSprite;
        public float spriteSizeMultiplier = 1f;


        public void HandlePureVisualStuff()
        {
            Vector2 oldVisualOffset = visualOffset;
            visualRotation = NPC.velocity.X * 0.04f;
            if (TopDownView)
                visualRotation = 0;

            Vector2 averageLeftLegs = Vector2.Lerp(Legs[0].legTip, Legs[1].legTip, 0.5f);
            Vector2 averageRightLegs = Vector2.Lerp(Legs[2].legTip, Legs[3].legTip, 0.5f);

            if (averageLeftLegs.X < averageRightLegs.X && !TopDownView)
            {
                visualRotation += averageLeftLegs.AngleTo(averageRightLegs) * 0.25f;
            }

            visualRotation += visualRotationExtra;


            visualOffset = Vector2.Zero;
            visualOffset.Y += 9f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5 + 0.5);

            if (SubState == ActionState.HuskDrop_Stunned)
                visualOffset.Y *= Math.Min((1 - AttackTimer) * 2f, 1f);

            //When its in desperation, it no longer breathes
            if (AIState == ActionState.Desperation || AIState == ActionState.Dead)
                visualOffset.Y = 0;

            //Otherwise, twitch your claw at random
            if (Main.rand.NextBool(150) && Arms != null && 
                AIState != ActionState.SpawningUp && 
                AIState != ActionState.Dead && 
                AIState != ActionState.Desperation &&
                AIState != ActionState.Slam && 
                AIState != ActionState.Snip &&
                AIState != ActionState.HuskDrop &&
                AIState != ActionState.ClentaminatedAway &&
                !Arms[1].limping)
                Arms[1].ClackClaw();

            visualOffset.Y += kineticOffset;
            visualOffset += visualOffsetExtra;

            kineticOffsetVelocity = MathHelper.Lerp(kineticOffsetVelocity, -kineticOffset + 1f, 0.1f);
            kineticOffset += kineticOffsetVelocity;

            if (AIState == ActionState.Raving)
            {
                RaveDance();
                visualOffsetVelocity = visualOffset - oldVisualOffset;
            }

            if (TopDownView)
                visualRotation += NPC.rotation;
            else
                NPC.rotation = 0f;

            if (Main.rand.NextBool(17) && AIState != ActionState.DebugDisplay && AIState != ActionState.SpawningUp)
            {
                Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(NPC.width, NPC.height) - Vector2.UnitY * 34f, DustID.GlowingMushroom, -Vector2.UnitY + NPC.velocity);
                d.noLightEmittence = true;
            }

            UpdateProps();
            UpdateArms();

            if (SlingshotAttached)
                ManageSlingshotVisuals();

            if (Rack == null)
                Rack = new CrabulonPupeteeringRack(this);
            Rack.Update();

            if (Ropes == null)
                Ropes = new CrabulonCables(this);
            Ropes.Update();

            if (AIState != ActionState.HuskDrop && AIState != ActionState.Desperation && AIState != ActionState.Dead)
                Ropes.Reset();

            visualOffsetExtra = Vector2.Zero;
            visualRotationExtra = 0f;

            UpdateFlicker();
        }

        #region Flicker stuff
        public void UpdateFlicker()
        {
            flickerOpacity -= 1 / (60f * (flickerTickDownTimeOverride ?? 0.3f));
            if (flickerOpacity < 0)
                flickerOpacity = 0;

            flickerTickDownTimeOverride = null;
        }

        public int flickerFrom = 0;
        public int flickerBackground = 0;
        public float? flickerTickDownTimeOverride = null;

        public int effectFlicker = 0;
        public int flickerTimer = 0;
        public float flickerOpacity = 0f;
        public float FlickerOpacity => (float)Math.Pow(flickerOpacity, 2f);

        public int EffectFlicker {
            get {
                return effectFlicker;
            }
            set {
                if (flickerTimer == 0)
                    effectFlicker = value;
            }
        }

        public void RefreshFlicker()
        {
            flickerOpacity = 1f;
        }

        public void FlickerBetween(float chanceForFirstColor, int firstColor = 0, int secondColor = 4)
        {
            if (Main.rand.NextFloat() < chanceForFirstColor)
                EffectFlicker = firstColor;
            else
                EffectFlicker = secondColor;
            flickerOpacity = 1f;
        }

        #endregion

        public bool Limp_DanglingSimulationMode => SubState == ActionState.HuskDrop_Chase || SubState == ActionState.Desperation_Chase;
        public bool Limp_FallingSimulationMode => SubState == ActionState.HuskDrop_Drop || SubState == ActionState.Desperation_Drop || SubState == ActionState.DespawningDropDownDesperation;
        public bool Limp_RagdollSimulationMode => SubState == ActionState.HuskDrop_Stunned || SubState == ActionState.Desperation_Stunned || SubState == ActionState.Desperation_CinematicWait || SubState == ActionState.Desperation_VineAttach || AIState == ActionState.Dead;
        public bool Limp_CollidingWithTiles => (Limp_FallingSimulationMode || Limp_RagdollSimulationMode) && SubState != ActionState.Dead_InternalGoreSimulation ;

        #region Slingshot stuff
        public List<float> slingshotLegAttachOffsets = new List<float>();
        public VerletNet slingshotWeb;
        public Color slingshotBaseColor;
        public float slingshotLenght;
        public float originalSlingshotLenght;

        public float SlingshotStringWidth(float progress) => 9f;
        public Color SlingshotStringColor(float progress)
        {
            return Color.Lerp(slingshotBaseColor, Lighting.GetColor(target.Center.ToTileCoordinates()), 1 - progress);
        }

        public void SlingshotCallback(int chainIndex, ref float chainScroll)
        {
            if (SubState == ActionState.Slingshot_SpitAndWait)
                chainScroll = (float)Math.Pow(1 - AttackTimer * 2f, 2f) * -0.2f;
        }

        public void ManageSlingshotVisuals()
        {
            if (slingshotLegAttachOffsets == null)
                slingshotLegAttachOffsets = new List<float>();

            if (slingshotWeb == null)
                slingshotWeb = new VerletNet();
            if (slingshotWeb.extremities.Count == 0)
            {
                slingshotWeb.AddChain(new VerletPoint(target.Center, true), new VerletPoint(NPC.Center, true), 15, SlingshotStringWidth, SlingshotStringColor, primResolution: 3f);
                originalSlingshotLenght = NPC.Center.Distance(target.Center);
            }

            slingshotWeb.extremities[1].position = NPC.Center;
            slingshotWeb.extremities[0].position = target.Center;

            slingshotLenght = NPC.Center.Distance(target.Center);
            foreach (VerletStick stick in slingshotWeb.segments)
                stick.lenght = slingshotLenght / 14f * 0.7f;

            float gravity = SubState == ActionState.Slingshot_Reel_in ? 0f : 0.3f;
            slingshotWeb.Update(4, gravity);

            if (SubState != ActionState.Slingshot_SpitAndWait)
            {
                int i = 0;
                foreach (CrabulonLeg leg in Legs)
                {
                    if (slingshotLegAttachOffsets.Count < i + 1)
                        slingshotLegAttachOffsets.Add(Main.rand.NextFloat(-1f, 1f));

                    Vector2 idealLegTipPosition = VisualCenter + NPC.DirectionTo(target.Center) * (210f + slingshotLegAttachOffsets[i] * 21f) * NPC.scale;

                    if (SubState == ActionState.Slingshot_JumpSlowmo)
                        idealLegTipPosition = Vector2.Lerp(leg.legTip, idealLegTipPosition, (1 - AttackTimer) * 0.2f);

                    leg.legTipGraphic = idealLegTipPosition;
                    leg.CalculateKnee();
                    leg.legTip = leg.legTipGraphic;
                    leg.strideTimer = 1;
                    leg.latchedOn = false;
                    leg.previousGrabPosition = null;
                    leg.grabPosition = idealLegTipPosition;

                    i++;
                }
            }
        }
        #endregion

        #region Drawing
        public enum ArmDrawLayer
        {
            BehindBody,
            BehindFaceProps,
            AboveBody
        }

        public bool DrawFlip => lookingSideways && NPC.direction == 1 && !topDownView;

        #region Fissure shader
        public static readonly float[] GradientMapBrightnesses = new float[]
            {
                0f, 
                0.2f,
                0.37f,
                0.45f,
                0.72f,
                0.87f,
                1,
                0,
                0,
                0
            };

        public static readonly Vector3[] GradientMapColors = new Vector3[]
            {
                FablesUtils.Vector3FromHex(0x191856),
                FablesUtils.Vector3FromHex(0x47409F),
                FablesUtils.Vector3FromHex(0x7DA1F5),
                FablesUtils.Vector3FromHex(0x35469C),
                FablesUtils.Vector3FromHex(0x5B5FB0),
                FablesUtils.Vector3FromHex(0x66A8F1),
                FablesUtils.Vector3FromHex(0x191856),
                Vector3.Zero,
                Vector3.Zero,
                Vector3.Zero
            };

        public Vector3 GetFissureColorMultiplier()
        {
            if (SubState == ActionState.SporeBomb_ChargeBomb || SubState == ActionState.SporeBomb_SpawnOnSelfCharge)
                return Vector3.One + Vector3.One * 4f * MathF.Pow(1 - AttackTimer, 0.5f);

            if (SubState == ActionState.HuskDrop_Stunned)
                return Vector3.One * (1f - 0.8f * AttackTimer);

            if (SubState == ActionState.SpawningUp_Scream)
                return Vector3.One + Vector3.One * MathF.Sign(AttackTimer * MathHelper.Pi);

            if (SubState == ActionState.ClentaminatedAway_DieHorribly)
                return new Vector3(1f, 1f + (1 - AttackTimer) * 3f, 1f - (1 - AttackTimer) * 0.5f);

            return Vector3.One;
        }
        #endregion

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Legs == null && !NPC.IsABestiaryIconDummy)
                return false;

            if (NPC.IsABestiaryIconDummy)
            {
                UpdateVisualsForBestiary();
                drawColor = drawingBossChecklistDummy ? bossChecklistColor : Color.White;
            }
            else
                drawingBossChecklistDummy = false; //Failsafe

            //This is to avoid crabulon drawing at full brightness for the very first frame of his existence , before he could get updated
            if (SubState == ActionState.SpawningUp)
                return false;

            drawColor = NPC.TintFromBuffAesthetic(drawColor);

            //Failsafe
            ForwardsSprite = ForwardsSprite ?? TextureAssets.Npc[Type];
            TopDownSprite = TopDownSprite ?? ModContent.Request<Texture2D>(Texture + "TopDown");

            Texture2D bodyTex = ForwardsSprite.Value;
            Texture2D fissureTex = FissureSprite.Value;

            if (TopDownView)
            {
                bodyTex = TopDownSprite.Value;
                fissureTex = FissureTopDownSprite.Value;
            }
            else if (lookingSideways)
            {
                bodyTex = SidewaysSprite.Value;
                fissureTex = FissureSidewaysSprite.Value;
            }

            if (AIState == ActionState.Despawning && (int)SubState > (int)ActionState.DespawningScurrySlowly)
                drawColor *= AttackTimer;
            else if (SubState == ActionState.SpawningUp_Emerge)
                drawColor *= 1 - AttackTimer;

            DrawCrabulon(spriteBatch, screenPos, drawColor, bodyTex, fissureTex);

            if (SubState == ActionState.ClentaminatedAway_DieHorribly)
            {
                Color overlayColor = (Color.YellowGreen * 0.6f) with { A = 0 } * (1 - AttackTimer);
                DrawCrabulon(spriteBatch, screenPos + Main.rand.NextVector2Circular(10f, 10f) * (1 - AttackTimer) * NPC.scale, overlayColor, bodyTex, fissureTex, false, true);

                float disintegrationProgress = Utils.GetLerpValue(0.5f, 0f, AttackTimer, true);

                Effect effect = Scene["BasicTint"].GetShader().Shader;
                effect.Parameters["uOpacity"].SetValue((float)Math.Pow(disintegrationProgress, 2f));
                effect.Parameters["uSaturation"].SetValue(1f);
                effect.Parameters["uColor"].SetValue(Color.Lerp(Color.GreenYellow, Color.White, (float)Math.Pow(disintegrationProgress, 2f)));

                FlipShadersOnOff(spriteBatch, effect, true);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = Vector2.UnitY.RotatedBy(i / 4f * MathHelper.TwoPi ) * 1.5f * disintegrationProgress;
                    DrawCrabulon(spriteBatch, screenPos + offset, Color.White, bodyTex, fissureTex, false, false);
                }
                FlipShadersOnOff(spriteBatch, null, false);
            }

            return false;
        }

        public void DrawCrabulon(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Texture2D bodyTex, Texture2D fissureTex, bool drawStrings = true, bool drawFissure = true)
        {
            //Back legs (Alternatively, all legs when in top down view) (alternatively the front leg which is behind crabulon when sideways)
            foreach (CrabulonLeg leg in Legs)
            {
                if (!leg.frontPair || TopDownView || (lookingSideways && leg.Direction == NPC.direction))
                    leg.Draw(spriteBatch, screenPos, drawColor);
            }

            //Large ropes from husk drop
            if (Ropes != null && drawStrings)
                Ropes.Render(spriteBatch, screenPos, drawColor);

            //Slingshot hook
            if (SlingshotAttached && slingshotWeb != null && drawStrings)
            {
                slingshotBaseColor = drawColor;
                WebSprite ??= ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumWeb");
                slingshotWeb.Render(WebSprite.Value, -screenPos, 0f, SlingshotCallback, slingshotLenght / originalSlingshotLenght);
            }

            //Arm bg
            if (Arms != null)
                foreach (CrabulonArm arm in Arms)
                    arm.Draw(spriteBatch, screenPos, drawColor, ArmDrawLayer.BehindBody);

            DrawBody(spriteBatch, screenPos, drawColor, bodyTex, fissureTex, drawFissure);

            //Body Decorations
            if (Props != null)
                foreach (CrabulonProp prop in Props)
                    if (!prop.faceProp)
                        prop.Draw(spriteBatch, screenPos, drawColor);

            //Puppeteering strings for the front legs
            if (Rack != null && drawStrings && (!HideAllStrings && AIState != ActionState.Dead && AIState != ActionState.Desperation))
                Rack.DrawFrontStrings(screenPos, drawColor);

            //Front legs
            foreach (CrabulonLeg leg in Legs)
            {
                if (leg.frontPair && !TopDownView && (!lookingSideways || leg.Direction != NPC.direction))
                    leg.Draw(spriteBatch, screenPos, drawColor);
            }

            //Forearms when facing striaght ahead
            if (Arms != null)
                foreach (CrabulonArm arm in Arms)
                    arm.Draw(spriteBatch, screenPos, drawColor, ArmDrawLayer.BehindFaceProps);

            //Face decorations
            if (Props != null)
            {
                foreach (CrabulonProp prop in Props)
                    if (prop.faceProp)
                        prop.Draw(spriteBatch, screenPos, drawColor);

                //Lens flare on eyes when spawning
                if (SubState == ActionState.SpawningUp_Scream)
                {
                    Texture2D bloom = AssetDirectory.CommonTextures.PixelBloomCircle.Value;
                    Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;

                    float lensFlareSizeMultiplier = MathF.Sin(AttackTimer * 15f) * 0.3f + 0.8f;
                    lensFlareSizeMultiplier *= MathF.Pow(MathF.Sin(AttackTimer * MathHelper.Pi), 2f) + 1f;

                    int eyeCount = 0;

                    foreach (CrabulonProp prop in Props)
                    {
                        //Eyes are the only face props that use flicker
                        if (prop.faceProp && prop.useFlicker)
                        {
                            Vector2 offsetToEye;

                            switch (chosenSkinIndices[0])
                            {
                                //crabby
                                default:
                                    offsetToEye = new Vector2(-16, -10);
                                    if (prop.flipped)
                                        offsetToEye.X *= -1;
                                    break;
                                //shroomy
                                case 2:
                                    offsetToEye = new Vector2(-5, -12);
                                    if (eyeCount == 1)
                                        offsetToEye.X *= -1;
                                    break;
                                //hairy
                                case 1:
                                    offsetToEye = new Vector2(-12, -11);
                                    if (eyeCount == 1)
                                        offsetToEye.X *= -1;
                                    break;
                            }

                            float propRotation = prop.Anchor.AngleTo(prop.position);
                            propRotation = Math.Clamp(prop.GravityAngle.AngleBetween(propRotation), -prop.maxAngleDeviation, prop.maxAngleDeviation);

                            offsetToEye = offsetToEye.RotatedBy(propRotation);

                            Vector2 lensFlarePosition = prop.Anchor + offsetToEye * NPC.scale;
                            Color lensflareColor = Color.RoyalBlue with { A = 0 };

                            spriteBatch.Draw(bloom, lensFlarePosition - Main.screenPosition, null, lensflareColor * 0.2f, 0, bloom.Size() / 2f,NPC.scale * lensFlareSizeMultiplier * 0.45f, SpriteEffects.None, 0);
                            spriteBatch.Draw(bloom, lensFlarePosition - Main.screenPosition, null, lensflareColor * 0.6f, 0, bloom.Size() / 2f, NPC.scale * lensFlareSizeMultiplier * 0.15f, SpriteEffects.None, 0);

                            //Draws many layer of lens flare ontop of the eyes, scaling with velocity
                            Vector2 squishy = new Vector2(0.46f, 1.8f);
                            spriteBatch.Draw(lensFlare, lensFlarePosition - Main.screenPosition, null, lensflareColor * 0.3f, MathHelper.PiOver2, lensFlare.Size() / 2f, squishy * NPC.scale * lensFlareSizeMultiplier, SpriteEffects.None, 0);
                            spriteBatch.Draw(lensFlare, lensFlarePosition - Main.screenPosition, null, lensflareColor * 0.4f, MathHelper.PiOver2, lensFlare.Size() / 2f, squishy * NPC.scale * lensFlareSizeMultiplier * 0.5f, SpriteEffects.None, 0);
                            spriteBatch.Draw(lensFlare, lensFlarePosition - Main.screenPosition, null, Color.White, 0, lensFlare.Size() / 2f, squishy * lensFlareSizeMultiplier * NPC.scale * 0.3f, SpriteEffects.None, 0);

                            eyeCount++;
                        }
                    }
                }
            }

            //Arms
            if (Arms != null)
                foreach (CrabulonArm arm in Arms)
                    arm.Draw(spriteBatch, screenPos, drawColor, ArmDrawLayer.AboveBody);
        }

        public void DrawBody(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Texture2D bodyTex, Texture2D fissureTex,  bool drawFissure)
        {

            SpriteEffects flip = SpriteEffects.None;
            if (DrawFlip)
                flip = SpriteEffects.FlipHorizontally;

            //Body
            spriteBatch.Draw(bodyTex, VisualCenter - screenPos, null, drawColor, visualRotation, bodyTex.Size() / 2, NPC.scale * spriteSizeMultiplier, flip, 0);

            if (drawFissure)
            {
                //Fissure
                Effect effect = Scene["CrabulonFissure"].GetShader().Shader;
                effect.Parameters["brightnessShift"].SetValue(Main.GlobalTimeWrappedHourly + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.2f);
                effect.Parameters["segments"].SetValue(7);
                effect.Parameters["gradientScaleMultiplier"].SetValue(0.5f);
                effect.Parameters["lightColor"].SetValue(Color.Lerp(Color.Blue, drawColor, 0.75f).ToVector4());
                effect.Parameters["brightnessStep"].SetValue(0.1f);

                effect.Parameters["colors"].SetValue(GradientMapColors);
                effect.Parameters["brightnesses"].SetValue(GradientMapBrightnesses);
                effect.Parameters["globalMultiplyColor"].SetValue(GetFissureColorMultiplier());

                effect.Parameters["noiseTexture"].SetValue(Main.Assets.Request<Texture2D>("Images/Misc/noise").Value);
                effect.Parameters["noiseScale"].SetValue(new Vector2(0.33f, 0.1f));
                effect.Parameters["noiseScroll"].SetValue(Main.GlobalTimeWrappedHourly * -1f);
                effect.Parameters["noiseColor"].SetValue(new Vector3(3f, 4f, 12f));
                effect.Parameters["noiseTreshold"].SetValue(0.5f);
                effect.Parameters["noisePower"].SetValue(4f);

                effect.Parameters["depthColor"].SetValue(new Vector3(0.1f, 0.2f, 0.3f));

                FlipShadersOnOff(spriteBatch, effect, true);
                spriteBatch.Draw(fissureTex, VisualCenter - screenPos, null, drawColor, visualRotation, fissureTex.Size() / 2, NPC.scale * spriteSizeMultiplier, flip, 0);
                FlipShadersOnOff(spriteBatch, null, false);
            }
        }

        public void FlipShadersOnOff(SpriteBatch spriteBatch, Effect effect, bool immediate)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                RasterizerState priorRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
                Rectangle priorScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
                spriteBatch.End();
                spriteBatch.GraphicsDevice.RasterizerState = priorRasterizer;
                spriteBatch.GraphicsDevice.ScissorRectangle = priorScissorRectangle;
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, priorRasterizer, effect, Main.UIScaleMatrix);
            }
            else
            {
                spriteBatch.End();
                SpriteSortMode sortMode = SpriteSortMode.Deferred;
                if (immediate)
                    sortMode = SpriteSortMode.Immediate;
                spriteBatch.Begin(sortMode, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.Transform);
            }
        }

        private void DrawCrabulonStrings()
        {
            bool spriteBatchOn = false;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && npc.type == Type)
                {
                    Crabulon crab = (Crabulon)npc.ModNPC;

                    if (crab.HideAllStrings)
                        continue;

                    if (!spriteBatchOn)
                    {
                        spriteBatchOn = true;
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                    }

                    if (crab.Rack != null)
                        crab.Rack?.Draw(Main.screenPosition, Lighting.GetColor(npc.Center.ToTileCoordinates()));
                }
            }

            if (spriteBatchOn)
                Main.spriteBatch.End();
        }

        public bool MaskDrawActive => SubState == ActionState.HuskDrop_Stunned;
        public Vector2 crackPosition;
        public float crackRotation;
        public bool UsesNonsolidMask => false;

        public void DrawOverMask(SpriteBatch spriteBatch, bool solidLayer)
        {
            Texture2D woosh = ModContent.Request<Texture2D>(AssetDirectory.Noise + "ChromaBurst").Value;
            float wooshProgress = (float)Math.Pow(Math.Min((1 - AttackTimer) / 0.2f, 1f), 0.5f);
            Color wooshColor = new Color(30, 40, 255, 0) * (1 - wooshProgress);
            spriteBatch.Draw(woosh, crackPosition - Main.screenPosition, null, wooshColor, crackRotation, woosh.Size() / 2f, 0.4f * wooshProgress, 0, 0);

            Texture2D cracks = ModContent.Request<Texture2D>(AssetDirectory.Noise + "RadialMudCrackConcentric").Value;
            Color crackColor = new Color(30, 40, 255, 0);
            crackColor *= (float)Math.Pow(AttackTimer, 3f);

            for (int i = 0; i < 3; i++)
                spriteBatch.Draw(cracks, crackPosition - Main.screenPosition, null, crackColor, crackRotation, cracks.Size() / 2f, 2f, 0, 0);
        }
        #endregion

        #region Bestiary and boss checklist
        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
                UpdateVisualsForBestiary();
        }

        public void AnimateForBestiary()
        {
            visualOffset.Y = 9f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5 + 0.5);
            visualRotation = 0.04f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.3f);
        }

        public void UpdateVisualsForBestiary()
        {
            if (Legs == null)
                InitializeLimbs();

            foreach (CrabulonLeg leg in Legs)
            {
                leg.legOriginGraphic = leg.GetLegOrigin() + visualOffset;

                //Bestiary crab has legs at full extension cuz theyrre hardly visible anyways
                if (!drawingBossChecklistDummy)
                    leg.legTipGraphic = leg.legOriginGraphic + leg.baseRotation.ToRotationVector2() * leg.maxLenght * 0.8f;
                else
                    leg.legTipGraphic = leg.legOriginGraphic + leg.baseRotation.ToRotationVector2() * leg.maxLenght * 0.56f - Vector2.UnitY * 30f;

                leg.legKnee = FablesUtils.InverseKinematic(leg.legOriginGraphic, leg.legTipGraphic, leg.ForelegLenght, leg.LegLenght, !leg.leftSet);
            }

            UpdateArms();
            UpdateProps();
        }

        private static NPC _bossChecklistDummy;
        private static bool drawingBossChecklistDummy = false;
        private static Color bossChecklistColor;

        public static void DrawBossChecklistPortrait(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            int prevCrabsDefeated = WorldProgressionSystem.crabulonsDefeated;
            WorldProgressionSystem.crabulonsDefeated = 0;
            if (_bossChecklistDummy == null)
            {
                _bossChecklistDummy = new NPC();
                _bossChecklistDummy.IsABestiaryIconDummy = true;
                _bossChecklistDummy.SetDefaults(ModContent.NPCType<Crabulon>());
            }
            WorldProgressionSystem.crabulonsDefeated = prevCrabsDefeated;

            RasterizerState priorRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
            Rectangle priorScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
            bool scissorEnabled = priorRasterizer.ScissorTestEnable;

            Vector2 topLeft = new Vector2(rect.X, rect.Y);
            Vector2 bottomRight = topLeft + new Vector2(rect.Width, rect.Height);
            topLeft = Vector2.Transform(topLeft, Main.UIScaleMatrix);
            bottomRight = Vector2.Transform(bottomRight, Main.UIScaleMatrix);
            Rectangle clippingRectangle = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));


            spriteBatch.End();
            spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
            spriteBatch.GraphicsDevice.ScissorRectangle = clippingRectangle;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, priorRasterizer, null, Main.UIScaleMatrix);

            Vector2 center = rect.Center.ToVector2();
            center.X -= 60f;
            center.Y -= 60f;

            drawingBossChecklistDummy = true;
            bossChecklistColor = color;

            Main.instance.DrawNPCDirect(spriteBatch, _bossChecklistDummy, false, -center);

            spriteBatch.End();
            spriteBatch.GraphicsDevice.RasterizerState = priorRasterizer;
            spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable = scissorEnabled;
            spriteBatch.GraphicsDevice.ScissorRectangle = priorScissorRectangle;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, priorRasterizer, null, Main.UIScaleMatrix);


            drawingBossChecklistDummy = false;
        }
        #endregion
    }
}
