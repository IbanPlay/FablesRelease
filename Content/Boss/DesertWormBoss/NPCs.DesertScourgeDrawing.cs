namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public partial class DesertScourge : ModNPC
    {
        #region Aesthetics
        public float mandiblesOpenness;
        public float mandibleJerkiness;
        public float MandiblesOpenness => mandiblesOpenness + mandibleJerkiness;
        public float VisualRotation => NPC.rotation + MathHelper.PiOver2;
        public bool initialRoarPlayed = false;
        #endregion

        #region Textures
        public override string Texture => AssetDirectory.DesertScourge + "DScourge_Head";
        public Asset<Texture2D> LeftMandibleTexture;
        public Asset<Texture2D> RightMandibleTexture;
        public override string BossHeadTexture => AssetDirectory.DesertScourge + "DesertScourgeMap";

        public void LoadTextures()
        {
            //Loads every texture to avoid flickering

            string[] filenamePrefixes = new string[] { "DScourge", "Skeleton/DScourgeSkin", "Skeleton/DScourgeSkeleton" };
            string[] filenameSuffixes = new string[] { "_Head", "_Body1", "_Body2", "_Body3", "_Body4", "_Tail" };

            foreach (string prefix in filenamePrefixes)
            {
                foreach (string suffix in filenameSuffixes)
                {
                    ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + prefix + suffix, AssetRequestMode.ImmediateLoad);
                }
            }
        }
        #endregion

        #region Map icon fading
        internal bool _hideMapIcon = false;
        public bool HideMapIcon { get => mapIconFade <= 0; set => _hideMapIcon = value; }
        public float mapIconFade = 1f;

        private void FadeMapIcon(NPC npc, ref byte alpha, ref float headScale, ref float rotation, ref SpriteEffects effects, ref int npcID, ref float x, ref float y)
        {
            if (npc.type == ModContent.NPCType<DesertScourge>())
            {
                float fade = (npc.ModNPC as DesertScourge).mapIconFade;
                if (fade < 1)
                    alpha = (byte)(alpha * fade);
            }
        }

        public void ManageMapIconOpacity(bool insideTiles, bool onlyInsideTopSurfaces)
        {
            HideMapIcon = false;
            if (insideTiles && !onlyInsideTopSurfaces)
                HideMapIcon = true;
            if (FablesUtils.FullyWalled(NPC.position, NPC.width, NPC.height))
                HideMapIcon = true;

            if (_hideMapIcon && mapIconFade > 0)
                mapIconFade -= 1 / (60f * 0.4f);

            else if (!_hideMapIcon && mapIconFade < 1)
                mapIconFade += 1 / (60f * 0.085f);

            mapIconFade = Math.Clamp(mapIconFade, 0f, 1f);
        }

        public override void BossHeadSlot(ref int index)
        {
            //Don't draw head icon if passive
            if (AIState == ActionState.UnnagroedMovement || AIState == ActionState.Despawning ||
                (AIState == ActionState.CutsceneFightStart && BecomePassiveAfterSpawnAnim))
            {
                index = -1;
                return;
            }

            //Don't draw the head icon while spawning or dying
            if (SubState == ActionState.CutsceneFightStart_CameraMagnetize ||
                SubState == ActionState.CutsceneFightStart_ThrowCreatureAndWait ||
                SubState == ActionState.CutsceneFightStart_EatCreature ||
                AIState == ActionState.CutsceneDeath)
            {
                index = -1;
                return;
            }

            if (!DrawStateTrackerSystem.drawingMap)
                return;

            //Hide map icon if in the floor
            if (HideMapIcon)
                index = -1;

            //Hide icon based if anticipation stage
            else if (SubState == ActionState.FastLunge_Anticipation || SubState == ActionState.LeviathanLunge_Anticipation || SubState == ActionState.ChungryLunge_Anticipation)
                index = -1;
        }
        #endregion

        public void TakeSegmentDamage(int segmentIndex, int damage, int direction = 0)
        {
            if (damage <= 0)
                return;
            if (segmentIndex < 0 || segmentIndex >= SegmentCount)
                return;
            //in ftw the scourge spawns with different preset damage values
            if (Main.getGoodWorld)
                return;

            float damageNeededToDownATier = NPC.lifeMax / 10f;
            if (segmentIndex == 0) //Head takes a lot more hits to fully decompose
                damageNeededToDownATier *= 1.35f;

            segmentDamage[segmentIndex] += damage * 1 / damageNeededToDownATier;
            if (segmentDamage[segmentIndex] > 3)
                segmentDamage[segmentIndex] = 3;

            //Recursion
            if (direction == 0)
            {
                if (segmentIndex == 0)
                    TakeSegmentDamage(segmentIndex - 1, damage, -7);
                else
                {
                    TakeSegmentDamage(segmentIndex + 1, (int)(damage * 0.75f), 3);
                    TakeSegmentDamage(segmentIndex - 1, (int)(damage * 0.75f), -3);
                }
            }

            else
            {
                int newIndex = segmentIndex + Math.Sign(direction);
                direction = Math.Abs(direction - 1) * Math.Sign(direction);
                if (direction == 0)
                    return;

                TakeSegmentDamage(newIndex, (int)(damage * 0.5f), direction);
            }
        }

        public void ManageMandibles(bool inGround, bool onlyInsidePlatforms)
        {
            mandibleJerkiness = MathHelper.Lerp(mandibleJerkiness, 0f, 0.1f);
            if (Math.Abs(mandibleJerkiness) < 0.01f)
                mandibleJerkiness = 0f;
            mandiblesOpenness = 0.1f * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);

            if (AIState == ActionState.CutsceneDeath || AIState == ActionState.CutsceneFightStart || AIState == ActionState.ElectroLunge)
                return;

            if (Main.rand.NextBool(55) && Math.Abs(mandibleJerkiness) < 0.1f && Math.Cos(Main.GlobalTimeWrappedHourly * 3f) > 0)
            {
                mandibleJerkiness += Main.rand.NextFloat(0.03f, 0.3f);

                if (!inGround || onlyInsidePlatforms)
                    SoundEngine.PlaySound(MandibleTwitchSound, NPC.Center);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (SubState != ActionState.CutsceneDeath_SegmentsBurn && SubState != ActionState.CutsceneDeath_ComedicPause)
            {
                for (int i = segmentPositions.Length - 1; i > 0; i--)
                {
                    GetSegmentTextureAndFrame(i, out Texture2D tex, out Rectangle frame);
                    DrawSegment(spriteBatch, screenPos,  Lighting.GetColor(SegmentPosition(i).ToTileCoordinates() - new Point(0, 1)), i, tex, frame);
                }

                GetSegmentTextureAndFrame(0, out Texture2D headTex, out Rectangle headFrame);
                DrawHead(spriteBatch, screenPos, drawColor, headTex, headFrame);
            }

            else
                DrawDyingScourge(spriteBatch, screenPos, drawColor);

            //Electro orb
            if ((SubState == ActionState.ElectroLunge_Jump || SubState == ActionState.ElectroLunge_Peek || SubState == ActionState.ElectroLunge_Backpedal) ||
                (SubState == ActionState.CutsceneDeath_Jump || SubState == ActionState.CutsceneDeath_Peek || SubState == ActionState.CutsceneDeath_Backpedal || SubState == ActionState.CutsceneDeath_SegmentsBurn))
            {
                Vector2 idealPosition = NPC.Center + NPC.rotation.ToRotationVector2() * 88f;
                float idealSize = 90f;
                float apparitionSize = 1f;

                if (SubState == ActionState.ElectroLunge_Peek || SubState == ActionState.CutsceneDeath_Peek)
                {
                    idealPosition = Vector2.Lerp(NPC.Center + NPC.rotation.ToRotationVector2() * 38f, idealPosition, FablesUtils.PolyInOutEasing(AttackTimer, 2));
                    apparitionSize = MathHelper.Lerp(0f, 1f, FablesUtils.PolyInOutEasing(AttackTimer, 2));
                }

                if (SubState == ActionState.ElectroLunge_Jump)
                    idealSize += 20f * AttackTimer;

                if (AIState == ActionState.CutsceneDeath)
                    idealSize *= 1.35f;

                if (SubState == ActionState.CutsceneDeath_SegmentsBurn)
                {
                    idealSize *= DeathAnimationElectroOrbProgress;
                }

                DrawElectroOrb(spriteBatch, screenPos, idealPosition, idealSize, apparitionSize);
            }

            return false;
        }


        public void GetSegmentTextureAndFrame(int index, out Texture2D tex, out Rectangle frame, string texturePathStart = "DScourge")
        {
            if (index == 0)
            {
                if (texturePathStart == "DScourge")
                    tex = TextureAssets.Npc[Type].Value;
                else
                    tex = ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + texturePathStart + "_Head").Value;
                frame = tex.Frame(1, 4, 0, (int)segmentDamage[0], 0, -2);
                return;
            }

            string variant = (2 + (index % 3)).ToString();
            string texturePath = AssetDirectory.DesertScourge + texturePathStart + "_Body" + variant;

            if (index == SegmentCount - 1)
                texturePath = AssetDirectory.DesertScourge + texturePathStart + "_Tail";
            if (index == 1)
                texturePath = AssetDirectory.DesertScourge + texturePathStart + "_Body1";

            tex = ModContent.Request<Texture2D>(texturePath).Value;
            frame = tex.Frame(1, 4, 0, (int)segmentDamage[index], 0, -2);
        }

        public void DrawSegment(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, int index, Texture2D tex, Rectangle frame)
        {
            Vector2 scale = Vector2.One * NPC.scale;

            if (SubState == ActionState.PreyBelch_Telegraph) //Body convulsions
            {
                float timeBeforeTimerStarts = 0.2f;
                float timeDuringWhichTheSegmentsUnglurg = 0f;
                float timerTime = 1f - timeBeforeTimerStarts - timeDuringWhichTheSegmentsUnglurg;

                float timer = Math.Clamp((AttackTimer - timeBeforeTimerStarts) / timerTime, 0f, 1f);

                float waveFulSegment = MathHelper.Lerp(SegmentCount, 1, (float)Math.Pow(timer, 0.2f));
                float distanceToWavefulSegment = (6f - Math.Clamp(Math.Abs(index - waveFulSegment), 0f, 6f)) / 6f;

                float extraWidth = (0.3f + timer * 0.2f) * (float)Math.Pow(distanceToWavefulSegment, 2f);

                if (AttackTimer > 1 - timeDuringWhichTheSegmentsUnglurg)
                    extraWidth *= 1 - (float)Math.Pow((AttackTimer - (1 - timeDuringWhichTheSegmentsUnglurg)) / timeDuringWhichTheSegmentsUnglurg, 3f);

                scale.X *= 1f + extraWidth;
            }

            spriteBatch.Draw(tex, segmentPositions[index] - screenPos, frame, drawColor, SegmentRotation(index) + MathHelper.PiOver2, frame.Size() / 2, scale, 0, 0);
        }

        public void DrawHead(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Texture2D tex, Rectangle frame)
        {
            LeftMandibleTexture = LeftMandibleTexture ?? ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_MandibleL");
            RightMandibleTexture = RightMandibleTexture ?? ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_MandibleR");

            Texture2D leftMandible = LeftMandibleTexture.Value;
            Texture2D rightMandible = RightMandibleTexture.Value;

            Vector2 mandibleLPosition = NPC.Center + new Vector2(-42, -19).RotatedBy(VisualRotation) * NPC.scale;
            Vector2 mandibleRPosition = NPC.Center + new Vector2(42, -19).RotatedBy(VisualRotation) * NPC.scale;

            Rectangle mandibleFrame = leftMandible.Frame(1, 4, 0, (int)segmentDamage[0], 0, -2);

            Vector2 mandibleLOrigin = new Vector2(31, 85);
            Vector2 mandibleROrigin = new Vector2(29, 85);


            spriteBatch.Draw(tex, NPC.Center - screenPos, frame, drawColor, VisualRotation, frame.Size() / 2, NPC.scale, 0, 0);

            Color mandibleLColor = Lighting.GetColor(mandibleLPosition.ToTileCoordinates());
            Color mandibleRColor = Lighting.GetColor(mandibleRPosition.ToTileCoordinates());

            spriteBatch.Draw(leftMandible, mandibleLPosition - screenPos, mandibleFrame, mandibleLColor, VisualRotation - MandiblesOpenness, mandibleLOrigin, NPC.scale, 0, 0);
            spriteBatch.Draw(rightMandible, mandibleRPosition - screenPos, mandibleFrame, mandibleRColor, VisualRotation + MandiblesOpenness, mandibleROrigin, NPC.scale, 0, 0);

            if (AIState == ActionState.ElectroLunge && (int)SubState >= (int)ActionState.ElectroLunge_Peek)
            {
                Color glowColor = Color.RoyalBlue * 1.4f;
                if (SubState == ActionState.ElectroLunge_Peek)
                    glowColor *= AttackTimer;

                Color eyesGlowColor = glowColor;
                glowColor *= Utils.GetLerpValue(1f, 0.6f, drawColor.GetBrightness(), true);

                DrawHeadGlow(spriteBatch, screenPos, glowColor, eyesGlowColor);
            }
        }

        public void DrawHeadGlow(SpriteBatch spriteBatch, Vector2 screenPos, Color glowColor, Color eyesGlowColor)
        {
            glowColor.A = 0;
            eyesGlowColor.A = 0;

            Texture2D ambientOcclusionGlow = ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_HeadGlowAO").Value;
            Texture2D headGlow = ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_HeadGlow").Value;
            Texture2D leftGlow = ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_MandibleLGlow").Value;
            Texture2D rightGlow = ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_MandibleRGlow").Value;

            Vector2 mandibleLPosition = NPC.Center + new Vector2(-42, -19).RotatedBy(VisualRotation) * NPC.scale;
            Vector2 mandibleRPosition = NPC.Center + new Vector2(42, -19).RotatedBy(VisualRotation) * NPC.scale;

            Rectangle headFrame = headGlow.Frame(1, 4, 0, (int)segmentDamage[0], 0, -2);
            Rectangle mandibleFrame = leftGlow.Frame(1, 4, 0, (int)segmentDamage[0], 0, -2);

            Vector2 mandibleLOrigin = new Vector2(31, 85);
            Vector2 mandibleROrigin = new Vector2(29, 85);

            spriteBatch.Draw(ambientOcclusionGlow, NPC.Center - screenPos, headFrame, eyesGlowColor, VisualRotation, headFrame.Size() / 2, NPC.scale, 0, 0);
            spriteBatch.Draw(headGlow, NPC.Center - screenPos, headFrame, glowColor, VisualRotation, headFrame.Size() / 2, NPC.scale, 0, 0);
            spriteBatch.Draw(leftGlow, mandibleLPosition - screenPos, mandibleFrame, glowColor, VisualRotation - MandiblesOpenness, mandibleLOrigin, NPC.scale, 0, 0);
            spriteBatch.Draw(rightGlow, mandibleRPosition - screenPos, mandibleFrame, glowColor, VisualRotation + MandiblesOpenness, mandibleROrigin, NPC.scale, 0, 0);
        }

        public void DrawHeadWithNoMandibles(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Texture2D tex, Rectangle frame)
        {
            spriteBatch.Draw(tex, NPC.Center - screenPos, frame, drawColor, VisualRotation, frame.Size() / 2, NPC.scale, 0, 0);
        }

        public void DrawMandiblesWithNoHead(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Texture2D tex, Rectangle frame)
        {
            LeftMandibleTexture = LeftMandibleTexture ?? ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_MandibleL");
            RightMandibleTexture = RightMandibleTexture ?? ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DScourge_MandibleR");

            Texture2D leftMandible = LeftMandibleTexture.Value;
            Texture2D rightMandible = RightMandibleTexture.Value;

            Vector2 mandibleLPosition = NPC.Center + new Vector2(-42, -19).RotatedBy(VisualRotation) * NPC.scale;
            Vector2 mandibleRPosition = NPC.Center + new Vector2(42, -19).RotatedBy(VisualRotation) * NPC.scale;

            Rectangle mandibleFrame = leftMandible.Frame(1, 4, 0, (int)segmentDamage[0], 0, -2);

            Vector2 mandibleLOrigin = new Vector2(31, 85);
            Vector2 mandibleROrigin = new Vector2(29, 85);

            Color mandibleLColor = Lighting.GetColor(mandibleLPosition.ToTileCoordinates());
            Color mandibleRColor = Lighting.GetColor(mandibleRPosition.ToTileCoordinates());

            spriteBatch.Draw(leftMandible, mandibleLPosition - screenPos, mandibleFrame, mandibleLColor, VisualRotation - MandiblesOpenness, mandibleLOrigin, NPC.scale, 0, 0);
            spriteBatch.Draw(rightMandible, mandibleRPosition - screenPos, mandibleFrame, mandibleRColor, VisualRotation + MandiblesOpenness, mandibleROrigin, NPC.scale, 0, 0);
        }


        public void DrawElectroOrb(SpriteBatch sb, Vector2 screenPos, Vector2 position, float size, float apparitionScale)
        {
            Effect effect = Scene["ElectroOrb"].GetShader().Shader;
            Texture2D pebbleNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "PebblesNoise").Value;
            Texture2D zapNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "LightningNoise").Value;

            Vector2 resolution = new Vector2(size * 0.5f);

            Texture2D ligjht = AssetDirectory.CommonTextures.BigBloomCircle.Value;
            Main.spriteBatch.Draw(ligjht, position - screenPos, null, Color.Black * 0.4f, 0, ligjht.Size() / 2f, 1.7f * size * apparitionScale / (float)ligjht.Width, SpriteEffects.None, 0f);

            Vector4 coreColor = new Vector4(0.9f, 1.0f, 1.2f, 0.9f);
            if (SubState == ActionState.ElectroLunge_Peek || SubState == ActionState.CutsceneDeath_Peek)
            {
                coreColor = Vector4.Lerp((coreColor * 0.2f) with { W = coreColor.W * 0.5f }, coreColor, (float)Math.Pow(AttackTimer, 1.8f));
            }

            Vector4 edgeColor = new Vector4(0.3f, 0.5f, 0.85f, 1);
            Vector4 zapColor = new Vector4(0.1f, 0.16f, 0.26f, 0.6f);

            if (AIState == ActionState.CutsceneDeath)
            {
                float lerpFactor = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f;

                coreColor = Vector4.Lerp(coreColor, new Vector4(1.3f, 0.7f, 0.2f, coreColor.W), lerpFactor);
                edgeColor = Vector4.Lerp(edgeColor, new Vector4(1f, 0.4f, 0.1f, 1f), lerpFactor);
            }

            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.24f);
            effect.Parameters["zapTexture"].SetValue(zapNoise);
            effect.Parameters["resolution"].SetValue(resolution);
            effect.Parameters["coreColor"].SetValue(coreColor);
            effect.Parameters["edgeColor"].SetValue(edgeColor);
            effect.Parameters["zapColor"].SetValue(zapColor);
            effect.Parameters["blowUpSize"].SetValue(0.4f);
            effect.Parameters["fresnelStrenght"].SetValue(7f);
            effect.Parameters["maxRadius"].SetValue((0.95f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly)) * apparitionScale);

            effect.Parameters["coreSolidRadius"].SetValue(0.6f);
            effect.Parameters["coreFadeRadius"].SetValue(0.2f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(pebbleNoise, position - screenPos, null, Color.White, 0, pebbleNoise.Size() / 2f, size / (float)pebbleNoise.Width, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Main.spriteBatch.Draw(bloom, position - screenPos, null, Color.RoyalBlue with { A = 0 }, 0, bloom.Size() / 2f, 1.6f * size * apparitionScale / (float)bloom.Width, SpriteEffects.None, 0f);

        }

        #region Boss checklist
        private static Asset<Texture2D> bossChecklistPortrait;

        public static void DrawBossChecklistPortrait(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            bossChecklistPortrait ??= ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "DesertScourgeBossChecklist");
            Texture2D tex = bossChecklistPortrait.Value;
            Vector2 drawPos = new Vector2(rect.Center.X, rect.Center.Y);
            spriteBatch.Draw(tex, drawPos, null, color, 0, tex.Size() / 2f, 1f, 0, 0);
        }
        #endregion
    }
}
