using CalamityFables.Particles;
using ReLogic.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class CrabulonSporeBomb : ModNPC
    {
        public override string Texture => AssetDirectory.Crabulon + Name;

        public static readonly SoundStyle DeploySound = new SoundStyle(SoundDirectory.Crabulon + "CrabulonSporeOpen") { Volume = 0.6f, MaxInstances = 1 , SoundLimitBehavior = SoundLimitBehavior.IgnoreNew};
        public static readonly SoundStyle LoopSound = new SoundStyle(SoundDirectory.Crabulon + "CrabulonSporeGasLoop") { Volume = 0.2f, IsLooped = true};


        public ref float Timer => ref NPC.ai[0];
        public float AnchorRotation => NPC.ai[1] - MathHelper.TwoPi;
        public bool GroundAnchored => NPC.ai[1] != 0;

        //Takes 1.8 second to deploy
        public virtual float DeploymentTime => 1.8f * 60f;
        public bool Deployed => Timer > DeploymentTime;

        //Lasts 15 seconds, 5 seconds less depending on how damaged it is
        public virtual float ExpirationTime => (15f - (1 - NPC.life / (float)NPC.lifeMax) * 5f) * 60f ;

        public float GasExpansionTimer => 0.8f * 60f;
        public float GasExpansion => Math.Clamp((Timer - DeploymentTime) / GasExpansionTimer, 0f, 1f);

        public static Asset<Texture2D> ClosedBudTexture;
        public static Asset<Texture2D> OpenBudTexture;
        public static Asset<Texture2D> GroundBudTexture;
        public static Asset<Texture2D> GroundClosedBudTexture;
        public static Asset<Texture2D> GroundOpenBudTexture;
        internal PrimitiveClosedLoop FullChargeLoop;

        public static SlotId GasLoopSlot = SlotId.Invalid;
        public static float ClosestBudDistance = float.MaxValue;
        public void ResetClosestBudForSound() => ClosestBudDistance = float.MaxValue;

        public float budRotation = 0f;

        public virtual float GasRadiusMax => Crabulon.SporeInfestation_GasRadiusMax;
        public float GasRadius {
            get {
                if (!Deployed)
                    return 0f;
                return MathHelper.Lerp(Crabulon.SporeInfestation_GasRadiusMin, GasRadiusMax, GasExpansion);
            }
        } 
        
        //Gotta shrink down the effective radius of the effects because theres a lot of spillover
        public float EffectRadius => GasRadius * (GroundAnchored ? (140f / 170f) : (120f / 170f));

        public override void Load()
        {
            if (!Main.dedServ)
            {
                ClosedBudTexture = ModContent.Request<Texture2D>(Texture + "Closed");
                OpenBudTexture = ModContent.Request<Texture2D>(Texture + "Open");

                GroundBudTexture = ModContent.Request<Texture2D>(Texture + "_Ground");
                GroundClosedBudTexture = ModContent.Request<Texture2D>(Texture + "_GroundClosed");
                GroundOpenBudTexture = ModContent.Request<Texture2D>(Texture + "_GroundOpen");

                FablesGeneralSystemHooks.PreUpdateNPCsEvent += ResetClosestBudForSound;
            }

            On_NPC.NPCLoot_DropCommonLifeAndMana += DropSporeHearts;
        }

        private void DropSporeHearts(On_NPC.orig_NPCLoot_DropCommonLifeAndMana orig, NPC self, Player closestPlayer)
        {
            if (self.type == Type)
            {
                if ((closestPlayer.HasBuff<CrabulonDOT>() && closestPlayer.RollLuck(2) == 0) || closestPlayer.RollLuck(6) == 0 && Main.rand.NextBool())
                {
                    if (closestPlayer.statLife < closestPlayer.statLifeMax2)
                        Item.NewItem(self.GetSource_Loot(), (int)self.position.X, (int)self.position.Y, self.width, self.height, ModContent.ItemType<SporeHeart>());
                }

                if (closestPlayer.RollLuck(2) == 0 && closestPlayer.statMana < closestPlayer.statManaMax2)
                    Item.NewItem(self.GetSource_Loot(), (int)self.position.X, (int)self.position.Y, self.width, self.height, ItemID.Star);
            }
            else
                orig(self, closestPlayer);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore Bud");
            //NPCID.Sets.BossBestiaryPriority.Add(Type);
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.damage = 0;
            NPC.lifeMax = Crabulon.SporeInfestation_BudLifeMax;

            NPC.width = 40;
            NPC.height = 40;
            NPC.aiStyle = -1;
            AIType = -1;

            NPC.netAlways = true;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit13;
            NPC.DeathSound = SoundID.NPCDeath21;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = Crabulon.SporeInfestation_BudLifeMax;
        }

        public override void AI()
        {
            //Attach to the wall if the anchor tile is broken
            if (GroundAnchored && !SolidCollisionFix(NPC.position, NPC.width, NPC.height, true))
            {
                NPC.ai[1] = 0;
                SoundEngine.PlaySound(SoundID.GlommerBounce, NPC.Center);
            }

            NPC.scale = (float)Math.Pow(Math.Min(Timer / 12f, 1f), 0.4f) * PolyInOutEasing(Math.Min(1f, (ExpirationTime - Timer) / 50f), 2f);

            if (Timer == 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                    Dust d = Dust.NewDustPerfect(NPC.Center + direction * 12f, DustID.GemSapphire, direction * 1.4f, 0);
                    d.noGravity = true;
                }
            }

            if (Deployed)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && InGasRadius(player))
                    {
                        int buffIndex = player.FindBuffIndex(ModContent.BuffType<CrabulonDOT>());

                        //Give the player the minimum infliction time
                        if (buffIndex == -1)
                            player.AddBuff(ModContent.BuffType<CrabulonDOT>(), (int)(Crabulon.SporeInfestation_MinInflictionTime * 60));
                        //Increase to reach the max infliction time
                        else
                        {
                            float rampup = (Crabulon.SporeInfestation_MaxInflictionTime - Crabulon.SporeInfestation_MinInflictionTime) / Crabulon.SporeInfestation_InflictionRampupTime;

                            player.buffTime[buffIndex] = Math.Min(player.buffTime[buffIndex] + (int)(rampup), (int)(Crabulon.SporeInfestation_MaxInflictionTime * 60));
                        }
                    }
                }

                GasVisuals();

                if (GasLoopSlot == SlotId.Invalid || !SoundEngine.TryGetActiveSound(GasLoopSlot, out var gasLoopSound))
                {
                    GasLoopSlot = SoundEngine.PlaySound(LoopSound, NPC.Center);
                    SoundEngine.TryGetActiveSound(GasLoopSlot, out gasLoopSound);
                }
                float distanceToSound = NPC.DistanceSQ(Main.LocalPlayer.Center);
                if (ClosestBudDistance > distanceToSound && gasLoopSound != null)
                {
                    ClosestBudDistance = distanceToSound;
                    gasLoopSound.Position = NPC.Center;
                    gasLoopSound.Update();
                }

                SoundHandler.TrackSoundWithFade(GasLoopSlot, 32);
            }

            budRotation += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3) * 0.019f;

            bool previousDeploymentState = Deployed;
            Timer++;

            if (!previousDeploymentState && Deployed)
                Deploy();

            if (Timer > ExpirationTime)
            {
                NPC.active = false;
                return;
            }

            Lighting.AddLight(NPC.Center, new Color(30, 27, 176).ToVector3() * 3);
            if (Timer < 20f && !Main.dedServ)
            {
                FullChargeLoop = FullChargeLoop ?? new PrimitiveClosedLoop(50, AppearLoopWidthFunction, AppearLoopColorFunction);
                FullChargeLoop.SetPositionsCircle(NPC.Center, 30f - 20f * (float)Math.Pow(AppearFlashTimer, 2f));
            }
        }

        internal float AppearFlashTimer => Math.Max(0f, (20f - Timer) / 20f);

        internal float AppearLoopWidthFunction(float completionRatio)
        {
            float baseWidth = 6.5f * AppearFlashTimer;  //Width tapers off at the end
            baseWidth *= (1.3f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7) * 0.3f); //Width oscillates
            return baseWidth;
        }

        internal Color AppearLoopColorFunction(float completionRatio)
        {
            Color colorStartElectro = new Color(15, 46, 251);
            Color colorEndElectro = CommonColors.MushroomDeepBlue * 0.5f;

            Color color = Color.Lerp(colorStartElectro, colorEndElectro, 1 - AppearFlashTimer);
            return color;
        }

        public void Deploy()
        {
            SoundEngine.PlaySound(DeploySound, NPC.Center);

            Vector2 normal = AnchorRotation.ToRotationVector2();

            for (int i = 0; i < 60; i++)
            {
                float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                Vector2 smokeCenter;
                Vector2 velocity;
                Vector2 circleOrigin;

                if (GroundAnchored)
                {
                    Vector2 gushDirection = (i / 60f * MathHelper.Pi * 1.2f - MathHelper.Pi * 0.6f + AnchorRotation).ToRotationVector2();
                    smokeCenter = NPC.Center + normal * 20f + gushDirection * Crabulon.SporeInfestation_GasRadiusMax * Main.rand.NextFloat(0.2f, 0.55f);
                    velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f);
                    circleOrigin = NPC.Center + normal * Crabulon.SporeInfestation_GasRadiusMax * 0.18f;
                }
                else
                {
                    Vector2 gushDirection = (i / 60f * MathHelper.TwoPi).ToRotationVector2();
                    smokeCenter = NPC.Center + gushDirection * Crabulon.SporeInfestation_GasRadiusMax * Main.rand.NextFloat(0.2f, 0.55f);
                    velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f);
                    circleOrigin = NPC.Center;
                }

                Particle smoke = new SporeGas(smokeCenter, velocity, circleOrigin, Crabulon.SporeInfestation_GasRadiusMax, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 usedCenter = NPC.Center;
                if (GroundAnchored)
                    usedCenter += normal * Crabulon.SporeInfestation_GasRadiusMax * 0.25f;

                Vector2 dustPosition = usedCenter + Main.rand.NextVector2Circular(160, 160);

                Dust sparks = Dust.NewDustPerfect(dustPosition, ModContent.DustType<SporeBudDust>(), Vector2.Zero);
                sparks.velocity = Main.rand.NextVector2Circular(1f, 1f) - Vector2.UnitY;
                sparks.noGravity = true;
                sparks.customData = Color.RoyalBlue * 0.3f;
                sparks.rotation = MathHelper.PiOver4;
                sparks.scale = Main.rand.NextFloat(0.5f, 0.75f);
                sparks.color = Main.rand.NextBool() ? Color.RoyalBlue : Color.CornflowerBlue;
                sparks.alpha = Main.rand.Next(110);
            }
        }

        public bool InGasRadius(Player player)
        {
            if (!GroundAnchored)
                return AABBvCircle(player.Hitbox, NPC.Center, GasRadius);

            //https://media.discordapp.net/attachments/802291445360623686/1088717792083202068/image.png
            Vector2 pointingNormal = AnchorRotation.ToRotationVector2();
            Vector2 bigCircleCenter = NPC.Center + pointingNormal * GasRadius * 0.6f;

            //Check the large circle
            if (AABBvCircle(player.Hitbox, bigCircleCenter, GasRadius * 0.88f))
                return true;
            //Check the smaller circles
            for (int i = -1; i <= 1; i += 2)
            {
                if (AABBvCircle(player.Hitbox, NPC.Center + pointingNormal * GasRadius * 0.36f + pointingNormal.RotatedBy(MathHelper.PiOver2) * GasRadius * 0.5f * i, GasRadius * 0.5f))
                    return true;
            }

            return false;
        }

        public void GasVisuals()
        {
            bool spawnSmoke = GroundAnchored ? Main.rand.NextBool(2) : !Main.rand.NextBool(3);

            if (spawnSmoke)
            {
                for (int i = 0; i < 2; i++)
                {
                    float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                    Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                    if (GroundAnchored)
                        gushDirection = (Main.rand.NextFloat() * MathHelper.Pi * 1.2f - MathHelper.Pi * 0.6f + AnchorRotation).ToRotationVector2();

                    Vector2 smokeCenter = NPC.Center + gushDirection * EffectRadius * Main.rand.NextFloat(0.2f, 0.55f);
                    Vector2 velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f);

                    Vector2 origin = NPC.Center;
                    if (GroundAnchored)
                    {
                        Vector2 normal = AnchorRotation.ToRotationVector2();
                        origin += normal * GasRadius * 0.22f;
                        smokeCenter += normal * 20f;
                        velocity *= 1.1f;
                    }

                    Particle smoke = new SporeGas(smokeCenter, velocity, origin, EffectRadius, smokeSize, 0.01f);
                    ParticleHandler.SpawnParticle(smoke);
                }
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                if (GroundAnchored)
                    gushDirection = (Main.rand.NextFloat() * MathHelper.Pi * 1.2f - MathHelper.Pi * 0.6f + AnchorRotation).ToRotationVector2();

                Vector2 dustPosition = NPC.Center + gushDirection * EffectRadius * Main.rand.NextFloat(0.1f, 0.6f);
                Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.6f, 3.6f);
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.GlowingMushroom, velocity);
                d.noLightEmittence = true;
            }

            /*
            if (GroundAnchored)
            {
                Vector2 pointingNormal = AnchorRotation.ToRotationVector2();
                Vector2 bigCircleCenter = NPC.Center + pointingNormal * GasRadius * 0.6f;

                //Check the smaller circles
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = 0; j < 30; j++)
                    {
                        Dust.QuickDust((NPC.Center + pointingNormal * GasRadius * 0.36f + pointingNormal.RotatedBy(MathHelper.PiOver2) * GasRadius * 0.5f * i) + Vector2.UnitY.RotatedBy(j / 30f * MathHelper.TwoPi) * GasRadius * 0.5f, Color.Red);
                    }
                }


                for (int k = 0; k < 30; k++)
                {
                    Dust.QuickDust(bigCircleCenter + Vector2.UnitY.RotatedBy(k / 30f * MathHelper.TwoPi) * GasRadius * 0.88f, Color.Red);
                }

            }
            else
            {
                for (int j = 0; j < 40; j++)
                {
                    Dust.QuickDust(NPC.Center + Vector2.UnitY.RotatedBy(j / 40f * MathHelper.TwoPi) * GasRadius, Color.Red);
                }
            }
            */
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);

            if (!GroundAnchored)
            {
                Texture2D tex = TextureAssets.Npc[Type].Value;
                spriteBatch.Draw(tex, NPC.Center - screenPos, null, drawColor, budRotation, tex.Size() / 2f, (float)Math.Pow(NPC.scale, 0.5f), 0, 0);

                tex = Deployed ? OpenBudTexture.Value : ClosedBudTexture.Value;
                float size = 1f + 0.2f * (float)(0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 3f + NPC.whoAmI / 200f * 0.2f) + 0.5);
                spriteBatch.Draw(tex, NPC.Center - screenPos, null, drawColor, budRotation * 1.6f, tex.Size() / 2f, size * NPC.scale, 0, 0);
            }

            else
            {
                Vector2 basePos = NPC.Center - AnchorRotation.ToRotationVector2() * 10f;
                Texture2D tex = Deployed ? GroundOpenBudTexture.Value : GroundClosedBudTexture.Value;
                Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height);
                Vector2 size = new Vector2(1f + 0.2f * (float)(0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 3f + NPC.whoAmI / 200f * 0.2f) + 0.5));
                size.Y = 2 - size.X;

                spriteBatch.Draw(tex, basePos - screenPos, null, drawColor, AnchorRotation + MathHelper.PiOver2 + budRotation * 0.1f, origin, size * NPC.scale, 0, 0);
                spriteBatch.Draw(GroundBudTexture.Value, basePos - screenPos, null, drawColor, AnchorRotation + MathHelper.PiOver2, GroundBudTexture.Value.Size() / 2f, (float)Math.Pow(NPC.scale, 0.5f), 0, 0);
            }


            if (AppearFlashTimer > 0f)
            {
                Effect effect = AssetDirectory.PrimShaders.IntensifiedTextureMap;
                effect.Parameters["repeats"].SetValue(4);
                effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                FullChargeLoop?.Render(effect, -Main.screenPosition);
            }

            return false;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool CheckActive()
        {
            //Doesn't despawn if crabulon is alive
            if (Main.npc.Any(n => n.type == ModContent.NPCType<Crabulon>() && n.active))
                return false;
            return base.CheckActive();
        }
    }

    public class CrabulonThrownSporeBomb : CrabulonSporeBomb
    {
        public override string Texture => AssetDirectory.Crabulon + "CrabulonSporeBomb";

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.lifeMax = Crabulon.SporeBomb_BudLifeMax;
        }

        public override float DeploymentTime => 0.2f * 60f;
        public override float GasRadiusMax => GroundAnchored ? Crabulon.SporeBomb_GasRadiusMax : Crabulon.SporeInfestation_GasRadiusMax;

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = Crabulon.SporeBomb_BudLifeMax;
        }
    }

    public class SporeBudDust : ModDust
    {

        public override string Texture => AssetDirectory.Visible;

        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return Color.White;
        }

        public override bool Update(Dust dust)
        {
            //update position / rotation
            if (!dust.noGravity)
                dust.velocity.Y += 0.1f;
            else
            {
                dust.velocity.Y -= 0.05f;
                if (dust.velocity.Y < -5f)
                    dust.velocity.Y = -5;

                dust.velocity *= 0.985f;
            }

            dust.position += dust.velocity;

            if (dust.alpha < 80)
                dust.alpha += 6;
            else
                dust.alpha += 2;


            if (dust.alpha > 150)
            {
                dust.active = false;
            }

            if (dust.customData != null && dust.customData is Color color)
                dust.color = Color.Lerp(dust.color, color, 0.03f);

            dust.scale *= 0.96f;

            if (dust.active)
                NoitaBloomLayer.bloomedDust.Add(new BloomInfo(Color.DodgerBlue, dust.position, dust.scale * 0.7f, dust.alpha));

            return false;
        }
    }

}
