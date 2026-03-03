using CalamityFables.Content.Items.Food;
using CalamityFables.Content.Items.Wulfrum;
using ReLogic.Utilities;
using System.IO;
using Terraria.GameContent.Bestiary;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    [ReplacingCalamity("WulfrumRover")]
    public class WulfrumRover : ModNPC, ISuperchargable
    {
        public override string Texture => AssetDirectory.WulfrumNPC + Name;
        public static readonly SoundStyle ShieldOn = new(SoundDirectory.Wulfrum + "WulfrumRoverShieldActivation");
        public static readonly SoundStyle ShieldHit = new(SoundDirectory.Wulfrum + "RoverDriveHit") { Volume = 0.6f };
        public static readonly SoundStyle ShieldOff = new(SoundDirectory.Wulfrum + "WulfrumRoverShieldDeactivation");
        public static readonly SoundStyle Shutdown = new(SoundDirectory.Wulfrum + "WulfrumShutdown");
        public static readonly SoundStyle TreadsLoop = new(SoundDirectory.Wulfrum + "WulfrumRoverTreadsLoop") { IsLooped = true, MaxInstances = 0, PlayOnlyIfFocused = true };

        public SlotId TreadsLoopSlot;

        public Player target => Main.player[NPC.target];
        public float ShieldDeploymentProgress {
            get {
                return Math.Clamp(NPC.ai[0], 0, 1);
            }
            set {
                NPC.ai[0] = value;
            }
        }
        public ref float BehaviorChangeTimer => ref NPC.ai[1];
        public ref float BehaviorChangeDirection => ref NPC.ai[2];
        public ref float ShieldBoostTimer => ref NPC.ai[3];
        public bool IsRollerChargingTheShield => NPC.localAI[0] > 1;

        public float ShieldWidth => 300 + Math.Clamp(ShieldBoostTimer, 0, 1) * 200;
        public Rectangle ShieldHitbox {
            get {
                Vector2 squish = ShieldSquish * ShieldWidth;
                return new Rectangle((int)(NPC.Center.X - squish.X / 2), (int)(NPC.Center.Y - squish.Y / 2), (int)squish.X, (int)squish.Y);
            }
        }


        public int yFrame;
        public float cogFrame;

        private bool _supercharged = false;
        public bool IsSupercharged {
            get => _supercharged;
            set {
                if (_supercharged != value)
                {
                    _supercharged = value;
                    NPC.netUpdate = true;
                }
            }
        }
        public bool Aggroed => IsSupercharged || NPC.life < NPC.lifeMax;


        public static int BannerType;
        public static AutoloadedBanner bannerTile;
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Wulfrum Rover", AssetDirectory.WulfrumBanners, out bannerTile);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Rover");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1

            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
            Main.npcFrameCount[Type] = 8;
            FablesSets.WulrumNPCs[Type] = true;
            bannerTile.NPCType = Type;

            if (Main.dedServ)
                return;
            for (int i = 0; i < 3; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumRoverGore" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 13;
            NPC.width = 42;
            NPC.height = 42;
            NPC.defense = 2;
            NPC.lifeMax = 40;
            NPC.knockBackResist = 0.15f;
            NPC.value = Item.buyPrice(0, 0, 1, 15);
            NPC.HitSound = SoundDirectory.CommonSounds.WulfrumNPCHitSound;
            NPC.DeathSound = SoundDirectory.CommonSounds.WulfrumNPCDeathSound;
            Banner = NPC.type;
            BannerItem = BannerType;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            if (Main.masterMode)
                NPC.damage = (int)(NPC.damage * 0.75f);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.WulfrumRover")
            });
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_supercharged.ToInt());
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _supercharged = reader.ReadByte() != 0;
        }

        public bool ProjectileCollidesWithWallsOfShield(Projectile proj, out bool velocityDoesntAffectMovement)
        {
            Rectangle mainHitbox = ShieldHitbox;
            velocityDoesntAffectMovement = false;
            if (mainHitbox.Contains(proj.Center.ToPoint()))
                return false;

            //If the projectile doesnt move based on its velocity, its therefore unreliable to use it for dot products, so we just pretend it always is going outside the shield
            velocityDoesntAffectMovement = (proj.aiStyle == 4 || proj.aiStyle == 38 || proj.aiStyle == 84 || proj.aiStyle == 148 || (proj.aiStyle == 7 && proj.ai[0] == 2f) || ((proj.type == 440 || proj.type == 449 || proj.type == 606) && proj.ai[1] == 1f) || (proj.aiStyle == 93 && proj.ai[0] < 0f) || proj.type == 540 || proj.type == 756 || proj.type == 818 || proj.type == 856 || proj.type == 961 || proj.type == 933 || ProjectileID.Sets.IsAGolfBall[proj.type]);
            velocityDoesntAffectMovement = velocityDoesntAffectMovement || !ProjectileLoader.ShouldUpdatePosition(proj) || proj.velocity == Vector2.Zero;

            Vector2 normalizedVelocity = proj.velocity.SafeNormalize(Vector2.UnitX);

            if (proj.tileCollide)
            {
                Vector2 collisionPoint = mainHitbox.GetCollisionPoint(proj.Center, normalizedVelocity);
                if (!Collision.CanHitLine(proj.position, proj.width, proj.height, collisionPoint, 1, 1))
                    return false;
            }

            //This is all very complicated but yah. TLDR is that we check if the projectile collides with the walls or is about to cross over the wall next frame.
            //The dot product is basically, if its 0, the projectile moves perpendicular to the wall. If it is 1, it moves directly agaisnt the wall. If it is -1 it moves outside of the wall. Any angles are between -1 and 1
            if (!velocityDoesntAffectMovement)
            {
                int wallThickness = (int)Math.Clamp(proj.velocity.Length() * (1 + proj.extraUpdates) * 2f, 4f, Math.Max(4f, mainHitbox.Width / 18));

                float dotLeft = Vector2.Dot(Vector2.UnitX, normalizedVelocity);
                if (dotLeft > 0)
                {
                    Rectangle leftWall = new Rectangle(mainHitbox.Left, mainHitbox.Top, wallThickness, mainHitbox.Height);
                    if (proj.Colliding(proj.Hitbox, leftWall))
                        return true;

                    if (LinesIntersect(proj.Center, proj.Center + proj.velocity * (proj.extraUpdates + 1), mainHitbox.TopLeft(), mainHitbox.BottomLeft()))
                        return true;
                }

                float dotRight = Vector2.Dot(-Vector2.UnitX, normalizedVelocity);
                if (dotRight > 0)
                {
                    Rectangle rightWall = new Rectangle(mainHitbox.Right - wallThickness, mainHitbox.Top, wallThickness, mainHitbox.Height);
                    if (proj.Colliding(proj.Hitbox, rightWall))
                        return true;

                    if (LinesIntersect(proj.Center, proj.Center + proj.velocity * (proj.extraUpdates + 1), mainHitbox.TopRight(), mainHitbox.BottomRight()))
                        return true;
                }

                float dotTop = Vector2.Dot(Vector2.UnitY, normalizedVelocity);
                if (dotTop > 0)
                {
                    Rectangle topWall = new Rectangle(mainHitbox.Left, mainHitbox.Top, mainHitbox.Width, wallThickness);
                    if (proj.Colliding(proj.Hitbox, topWall))
                        return true;

                    if (LinesIntersect(proj.Center, proj.Center + proj.velocity * (proj.extraUpdates + 1), mainHitbox.TopLeft(), mainHitbox.TopRight()))
                        return true;
                }

                float dotBottom = Vector2.Dot(-Vector2.UnitY, normalizedVelocity);
                if (dotBottom > 0)
                {
                    Rectangle bottomWall = new Rectangle(mainHitbox.Left, mainHitbox.Bottom - wallThickness, mainHitbox.Width, wallThickness);
                    if (proj.Colliding(proj.Hitbox, bottomWall))
                        return true;

                    if (LinesIntersect(proj.Center, proj.Center + proj.velocity * (proj.extraUpdates + 1), mainHitbox.BottomLeft(), mainHitbox.BottomRight()))
                        return true;
                }

                return false;
            }

            //If we don't care about which way the projectile is going we just check if it collides with any of the walls
            else
            {
                int wallThickness = (int)Math.Clamp(proj.velocity.Length() * (1 + proj.extraUpdates) * 2f, 4f, Math.Max(4f, mainHitbox.Width / 18));
                Rectangle leftWall = new Rectangle(mainHitbox.Left - wallThickness, mainHitbox.Top, wallThickness, mainHitbox.Height);
                Rectangle rightWall = new Rectangle(mainHitbox.Right, mainHitbox.Top, wallThickness, mainHitbox.Height);
                Rectangle topWall = new Rectangle(mainHitbox.Left, mainHitbox.Top - wallThickness, mainHitbox.Width, wallThickness);
                Rectangle bottomWall = new Rectangle(mainHitbox.Left, mainHitbox.Bottom, mainHitbox.Width, wallThickness);


                return proj.Colliding(proj.Hitbox, leftWall) ||
                   proj.Colliding(proj.Hitbox, rightWall) ||
                   proj.Colliding(proj.Hitbox, topWall) ||
                   proj.Colliding(proj.Hitbox, bottomWall);
            }
        }

        public override void AI()
        {
            //Handle tread sounds
            if (!SoundEngine.TryGetActiveSound(TreadsLoopSlot, out var vrrrSound) && NPC.velocity.X != 0)
            {
                TreadsLoopSlot = SoundEngine.PlaySound(TreadsLoop with
                {
                    Pitch = 0.2f
                }, NPC.Center);
                if (SoundEngine.TryGetActiveSound(TreadsLoopSlot, out var brrrrSound))
                {
                    brrrrSound.Volume = Math.Abs(NPC.velocity.X) / 3f;
                }
            }
            else if (vrrrSound != null)
            {
                vrrrSound.Position = NPC.Center;
                vrrrSound.Volume = Math.Abs(NPC.velocity.X) / 3f;
            }

            SoundHandler.TrackSound(TreadsLoopSlot);

            //Prevent the on-spawn "stuck" issue by forcing it to have an initial direction
            if (NPC.direction == 0)
            {
                NPC.TargetClosest(false);
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
            }


            //If the shield is deployed at alll
            if (ShieldDeploymentProgress > 0)
            {
                NPC.TargetClosest(true);
                NPC.knockBackResist = 0;

                //Slow down fast
                NPC.velocity.X *= 0.9f;
                if (Math.Abs(NPC.velocity.X) < 1)
                    NPC.velocity.X = 0;

                //Check all projectiles to see if theyre colliding
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.friendly && !proj.hostile && proj.penetrate >= 0 && ProjectileCollidesWithWallsOfShield(proj, out bool velocityIgnored))
                    {
                        proj.penetrate = Math.Max(0, proj.penetrate - 4);
                        if (proj.penetrate == 0)
                        {
                            int updateCount = proj.extraUpdates;
                            while (updateCount >= 0)
                            {
                                proj.AI();
                                if (!velocityIgnored)
                                    proj.position += proj.velocity;
                                updateCount--;
                                if (ShieldHitbox.Intersects(proj.Hitbox))
                                    break;
                            }

                            SoundEngine.PlaySound(ShieldHit, proj.Center);
                            proj.Kill();
                        }
                    }
                }

                //Shield electrification (This only happens with the roller when it manually increases the timer past 1 in its combo)
                if (ShieldBoostTimer >= 2)
                {
                    //Spam dust
                    for (int i = 0; i < 60; i++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(ShieldHitbox.TopLeft(), ShieldHitbox.Width, ShieldHitbox.Height, 226)];
                        dust.noGravity = true;
                        dust.velocity = -Vector2.UnitY;
                    }

                    //Apply electrified to the players (Could be a custom debuff if too strong)
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        if (player.active && !player.dead && ShieldHitbox.Intersects(player.Hitbox))
                        {
                            player.AddBuff(BuffID.Electrified, 65);
                        }
                    }
                    ShieldBoostTimer = 1;
                }
            }

            else
            {
                NPC.TargetClosest(false);
                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

                NPC.knockBackResist = 0.15f; //Reset KB resistance (The shield form gives it full kb immunity)

                //Change directions if close but not too close & line of sight
                float distanceToPlayerX = Math.Abs(target.Center.X - NPC.Center.X);

                bool canTargetPlayer = Aggroed;
                if (!target.GetPlayerFlag("WulfrumAmbassador") && distanceToPlayerX < 600)
                    canTargetPlayer |= Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height);

                if (distanceToPlayerX > 80 && canTargetPlayer)
                {
                    int direction = (target.Center.X - NPC.Center.X).NonZeroSign();
                    if (NPC.direction != direction)
                    {
                        NPC.direction = direction;
                        NPC.netSpam = 0;
                        NPC.netUpdate = true;
                    }
                }


                //Accelerates forward
                float maxXSpeed = 3f;
                NPC.velocity.X += 0.02f * NPC.direction;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * maxXSpeed, 0.01f);
                NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxXSpeed, maxXSpeed);

                //The rover can jump higher if it has line of sight
                float jumpHeight = Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height) ? 4f : 8f;

                //Jump if encountering a wall
                if (NPC.collideX && NPC.velocity.Y == 0)
                {
                    NPC.velocity.Y = -jumpHeight;

                    //Swap directions if the player is on the other side
                    if ((target.Center.X - NPC.Center.X).NonZeroSign() != NPC.direction)
                        NPC.direction *= -1;
                }

                // Jump if there's an gap ahead.
                if (NPC.collideY && NPC.velocity.Y == 0f && target.Top.Y < NPC.Bottom.Y && HoleAtPosition(NPC.Center.X + NPC.velocity.X * 4f))
                {
                    NPC.velocity.Y = -jumpHeight;
                }

                //Kick up some particles from the ground
                if (NPC.collideY && NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 1f && Main.rand.NextBool(6))
                {
                    Vector2 dustPos = NPC.Bottom - Vector2.UnitX * Main.rand.NextFloat() * NPC.width / 2 * NPC.direction;
                    Dust.NewDustPerfect(dustPos, DustID.Smoke, new Vector2(-NPC.direction * Main.rand.NextFloat(0.5f, 1f), Main.rand.NextFloat(-1f, -0.3f)), 130, default, 1.4f);
                }
            }


            //Reset the shield size boost if no roller is helping out
            if (!IsRollerChargingTheShield)
            {
                ShieldBoostTimer = MathHelper.Lerp(ShieldBoostTimer, 0f, 0.03f);
                if (ShieldBoostTimer < 0.01f)
                    ShieldBoostTimer = 0;
            }

            //If currently transitionning between states, go in either direction
            if (ShieldDeploymentProgress > 0 && ShieldDeploymentProgress < 1)
            {
                ShieldDeploymentProgress += 1 / (60f * 0.6f) * BehaviorChangeDirection;

                if (ShieldDeploymentProgress == 0 || ShieldDeploymentProgress == 1)
                {
                    BehaviorChangeDirection *= -1;
                    BehaviorChangeTimer = 0;
                }
            }

            //If the shield isnt transitionning between states and the rover is on the ground/Moving slowly enough
            else if (NPC.collideY || NPC.velocity.Length() <= 0.1)
            {
                bool nearbyNPCsToDefend = false;

                //Searches for enemies further away if the shield is fully deployed
                int scanDistance = ShieldDeploymentProgress < 1 ? (int)(ShieldWidth + 130) : (int)(ShieldWidth + 250);

                //Check if any NPCs would fit into its shield. (Todo : Make a NPC set of NPCs that cant activate the shield)
                Rectangle potentialShieldHitbox = new Rectangle((int)(NPC.Center.X - scanDistance / 2), (int)(NPC.Center.Y - scanDistance / 2), scanDistance, scanDistance);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (ShouldDefendNPC(npc, potentialShieldHitbox))
                    {
                        nearbyNPCsToDefend = true;
                        break;
                    }
                }

                //If theres no NPCs
                if (!nearbyNPCsToDefend && NPC.Distance(target.Center) < 1300)
                {
                    //Shield deployed. Spending 1.6 seconds with no NPCs nearby makes the shield retract
                    if (ShieldDeploymentProgress == 1)
                        BehaviorChangeTimer += 1 / (60f * 1.6f);


                    //Reset the values
                    else
                    {
                        BehaviorChangeTimer = 0f;
                        ShieldDeploymentProgress = 0;
                    }

                }
                else
                {
                    //Shield not deployed. Decides to charge up if an enemy goes through for 0.6 secs
                    if (ShieldDeploymentProgress == 0)
                        BehaviorChangeTimer += 1 / (60f * 0.6f);

                    //Reset the values
                    else
                    {
                        BehaviorChangeTimer = 0f;
                        ShieldDeploymentProgress = 1;
                    }
                }

                //If its decided enough to change its behavior, switch up its current mode
                if (BehaviorChangeTimer > 1)
                {
                    SoundEngine.PlaySound(ShieldDeploymentProgress == 1 ? ShieldOff : ShieldOn, NPC.Center);

                    BehaviorChangeDirection = ShieldDeploymentProgress == 1 ? -1 : 1;
                    BehaviorChangeTimer = 0;
                    ShieldDeploymentProgress += BehaviorChangeDirection * 0.01f;
                    NPC.netUpdate = true;
                }
            }

            //LocalAI is set by the roller to make the rover realize its got a partner. Lower it by a bit just in case a glitch happens to avoid the rover getting stuck like that
            NPC.localAI[0] -= 0.1f;
            if (NPC.localAI[0] < 0)
                NPC.localAI[0] = 0;
        }

        public static bool ShouldDefendNPC(NPC npc, Rectangle potentialShieldHitbox)
        {
            return npc.active &&
                !npc.friendly &&
                !npc.CountsAsACritter &&
                npc.type != NPCID.TargetDummy &&
                npc.type != ModContent.NPCType<WulfrumRover>() &&
                npc.type != ModContent.NPCType<WulfrumNexus>() &&
                potentialShieldHitbox.Intersects(npc.Hitbox);
        }

        private bool HoleAtPosition(float xPosition)
        {
            int tileWidth = NPC.width / 16;
            xPosition = (int)(xPosition / 16f) - tileWidth;
            if (NPC.velocity.X > 0)
                xPosition += tileWidth;

            int tileY = (int)((NPC.position.Y + NPC.height) / 16f);
            for (int y = tileY; y < tileY + 2; y++)
            {
                for (int x = (int)xPosition; x < xPosition + tileWidth; x++)
                {
                    if (Main.tile[x, y].HasTile)
                        return false;
                }
            }

            return true;
        }

        //Can't hit the player while bunkered down
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return ShieldDeploymentProgress == 0;
        }

        public override bool? CanFallThroughPlatforms()
        {
            return target.Top.Y > NPC.Bottom.Y;
        }

        public override void FindFrame(int frameHeight)
        {
            float velocity = NPC.IsABestiaryIconDummy ? 2 : Math.Abs(NPC.velocity.X);
            NPC.frameCounter += 0.1 * velocity;

            bool shielding = ShieldDeploymentProgress > 0;

            if (NPC.frameCounter > 1 && (!shielding || yFrame != 6))
            {
                NPC.frameCounter = 0;
                yFrame += 1;
                if (yFrame >= Main.npcFrameCount[Type])
                    yFrame = 0;
            }

            cogFrame += 0.04f;
            if (ShieldDeploymentProgress > 0)
                cogFrame += ShieldDeploymentProgress * 0.06f;

            //Cog spins faster if the rover is comboing with a roller
            if (IsRollerChargingTheShield)
                cogFrame += 0.03f;
        }

        public CurveSegment WidthUp = new CurveSegment(SineOutEasing, 0f, 0f, 1.2f);
        public CurveSegment WidthUnBounce = new CurveSegment(SineInEasing, 0.25f, 1.2f, -0.2f);
        public CurveSegment WidthStay = new CurveSegment(LinearEasing, 0.4f, 1f, 0f);

        public CurveSegment HeightStayLow = new CurveSegment(LinearEasing, 0f, 0.1f, 0f);
        public CurveSegment HeightUp = new CurveSegment(SineInOutEasing, 0.4f, 0.1f, 1f);
        public CurveSegment HeightUnBounce = new CurveSegment(SineInEasing, 0.55f, 1.1f, -0.1f);
        public CurveSegment HeightStayUp = new CurveSegment(LinearEasing, 0.75f, 1f, 0);
        internal Vector2 ShieldSquish => new Vector2(PiecewiseAnimation(ShieldDeploymentProgress, new CurveSegment[] { WidthUp, WidthUnBounce, WidthStay }), PiecewiseAnimation(ShieldDeploymentProgress, new CurveSegment[] { HeightStayLow, HeightUp, HeightUnBounce, HeightStayUp }));


        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);
            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;

            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            Vector2 gfxOffY = NPC.GfxOffY() + Vector2.UnitY;
            Rectangle frame = new Rectangle(0, yFrame * bodyTex.Height / 8, bodyTex.Width / 2 - 2, bodyTex.Height / 8 - 2);
            SpriteEffects flip = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (flip == SpriteEffects.FlipHorizontally)
                frame.X += 2;

            Rectangle frameBlue = frame with
            {
                X = frame.X + bodyTex.Width / 2
            };
            float blueOpacity = ShieldDeploymentProgress;

            Main.spriteBatch.Draw(bodyTex, NPC.Center + gfxOffY - screenPos, frame, drawColor, NPC.rotation, frame.Size() / 2f, NPC.scale, flip, 0f);
            Main.spriteBatch.Draw(bodyTex, NPC.Center + gfxOffY - screenPos, frameBlue, drawColor * blueOpacity, NPC.rotation, frameBlue.Size() / 2f, NPC.scale, flip, 0f);


            Texture2D gearTex = ModContent.Request<Texture2D>(Texture + "_Gear").Value;
            Rectangle gearFrame = new Rectangle(0, (int)((cogFrame % 1) * 4) * gearTex.Height / 4, gearTex.Width, gearTex.Height / 4 - 2);
            Vector2 bodyBob = Vector2.UnitY * 2 * ((yFrame == 4 || yFrame == 5) ? 1 : 0);
            Main.spriteBatch.Draw(gearTex, NPC.Center + gfxOffY + bodyBob - screenPos, gearFrame, drawColor, NPC.rotation, gearFrame.Size() / 2f, NPC.scale, flip, 0f);


            //TODO make this draw above other enemies
            Texture2D noiseTex = ModContent.Request<Texture2D>(AssetDirectory.Noise + "TechyNoise").Value;
            Vector2 scale = Vector2.One * (ShieldWidth / (float)noiseTex.Height) * ShieldSquish;
            Effect shieldEffect = Scene["WulfrumRoverShield"].GetShader().Shader;

            Color baseColor = CommonColors.WulfrumBlue with
            {
                A = (byte)(122 + 100 * Utils.GetLerpValue(0.5f, 0f, ShieldDeploymentProgress, true) + 100f * (float)Math.Pow(Utils.GetLerpValue(1.5f, 2f, ShieldBoostTimer, true), 5))
            };
            Color outlineColor = CommonColors.WulfrumGreen with
            {
                A = 255
            };
            if (IsRollerChargingTheShield)
                outlineColor = CommonColors.WulfrumBlue;

            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["outlineThickness"].SetValue(2);
            shieldEffect.Parameters["outlineBlendStrength"].SetValue(1.5f);
            shieldEffect.Parameters["centerFadeStrentgh"].SetValue(1.2f + 6f * Utils.GetLerpValue(0.5f, 0f, ShieldDeploymentProgress, true));

            shieldEffect.Parameters["noiseFadeStrength"].SetValue(0.4f);
            shieldEffect.Parameters["noiseOpacity"].SetValue(1f);

            shieldEffect.Parameters["baseTintColor"].SetValue(baseColor.ToVector4());
            shieldEffect.Parameters["outlineColor"].SetValue(outlineColor.ToVector4());

            shieldEffect.Parameters["spriteResolution"].SetValue(scale * noiseTex.Size() * 0.5f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(noiseTex, NPC.Center + gfxOffY - screenPos, null, drawColor, 0, noiseTex.Size() / 2f, scale, SpriteEffects.None, 0f);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);


            return false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => WulfrumCollaborationHelper.WulfrumGoonSpawnChance(spawnInfo);

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                if (SoundEngine.TryGetActiveSound(TreadsLoopSlot, out var vrrrSound))
                {
                    vrrrSound.Stop();
                }


                for (int k = 0; k < 5; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 3, hit.HitDirection, -1f, 0, default, 1f);
                }
                if (NPC.life <= 0)
                {

                    if (ShieldDeploymentProgress > 0)
                    {

                        SoundEngine.PlaySound(Shutdown, NPC.Center);

                        for (int i = 0; i < 40; i++)
                        {
                            Dust dust = Main.dust[Dust.NewDust(ShieldHitbox.TopLeft(), ShieldHitbox.Width, ShieldHitbox.Height, 226)];
                            dust.noGravity = true;
                            dust.velocity = Vector2.Zero;
                        }
                    }

                    for (int k = 0; k < 20; k++)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, 3, hit.HitDirection, -1f, 0, default, 1f);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumRoverGore" + i.ToString()).Type, 1f);
                    }


                    int randomGoreCount = Main.rand.Next(0, 2);
                    for (int i = 0; i < randomGoreCount; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumEnemyGore" + Main.rand.Next(1, 11).ToString()).Type, 1f);
                    }

                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ModContent.ItemType<WulfrumMetalScrap>(), 1, 1, 2);
            npcLoot.Add(ModContent.ItemType<RoverDrive>(), new Fraction(1, 10));
            npcLoot.AddIf(info => (info.npc.ModNPC as ISuperchargable).IsSupercharged, ModContent.ItemType<EnergyCore>());
            npcLoot.Add(ModContent.ItemType<WulfrumBrandCereal>(), WulfrumBrandCereal.DroprateInt);
        }
    }
}
