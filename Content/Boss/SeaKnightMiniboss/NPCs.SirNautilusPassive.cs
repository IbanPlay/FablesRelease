using CalamityFables.Content.UI;
using static Terraria.ModLoader.ModContent;
using CalamityFables.Particles;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public class SirNautilusPassive : ModNPC
    {
        public int yFrame;
        public int yawnCooldown;
        public int waitTime;
        public int onomatopeiaTimer;

        public override string Texture => AssetDirectory.SirNautilus + Name;

        public bool hovered = false;

        public Animation CurrentAnimation {
            get {
                //If we have a non default dialogue anim, play it
                Animation dialogueAnim = DialogueAnimation;
                if (dialogueAnim != Animation.None)
                {
                    NPC.ai[3] = (int)dialogueAnim;

                    if (_previousAnimation != dialogueAnim)
                    {
                        yFrame = 0;
                        NPC.frameCounter = 0;
                        _previousAnimation = dialogueAnim;
                    }

                    return dialogueAnim;
                }

                return (Animation)NPC.ai[3];
            }

            set {
                NPC.ai[3] = (int)value;
            }
        }

        internal Animation _previousAnimation;

        public enum Animation
        {
            None,
            Curious,
            Sleeping,
            Yawning,
            Laughing,
            Rattled,
            Interested,
            Shocked,
            Angry,
            Ukuleling
        }

        public Animation DialogueAnimation {
            get {
                if (!CoolDialogueUIManager.Active)
                    return Animation.None;

                DialoguePortrait portrait = CoolDialogueUIManager.CurrentPortrait;
                if (portrait is null)
                    return Animation.None;

                string portraitPath = portrait.portraitPath.Replace(AssetDirectory.SirNautilusDialogue, "");

                if (portraitPath == "Bored")
                    return Animation.Sleeping;

                if (portraitPath == "Angry" || portraitPath == "AngryHands")
                    return Animation.Angry;

                if (portraitPath == "Laughing")
                    return Animation.Laughing;

                if (portraitPath == "Curious")
                    return Animation.Curious;

                if (portraitPath == "Surprised" || portraitPath == "StarStruck" || portraitPath == "Shocked")
                    return Animation.Interested;

                if (portraitPath == "SurprisedHands" || portraitPath == "StarStruckHands")
                    return Animation.Shocked;

                return Animation.None;
            }
        }

        public override void Load()
        {
            FablesNPC.EditSpawnRateEvent += LowerSpawnRatesWhenInChamber;
        }

        private void LowerSpawnRatesWhenInChamber(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (NPC.AnyNPCs(Type))
            {
                Rectangle safetyRect = PointOfInterestMarkerSystem.NautilusChamberWorldRectangle;
                safetyRect.Inflate(700, 400);

                if (safetyRect.Intersects(player.Hitbox))
                {
                    spawnRate *= 5;
                    maxSpawns = 0;
                }
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sir Nautilus");
            Main.npcFrameCount[Type] = 16;
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.friendly = true;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.width = 40;
            NPC.height = 64;
            NPC.aiStyle = -1;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 200;
            NPC.HitSound = SirNautilus.HitSound;
            NPC.DeathSound = SirNautilus.DeathSound;
            NPC.knockBackResist = 0;
            NPC.netAlways = true;

            NPC.frame = new Rectangle(0, 0, 52, 68);
        }

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && NPC.AnyNPCs(ModContent.NPCType<SirNautilus>()))
            {
                NPC.active = false;
                return;
            }

            //CoolDialogueUIManager.ShowDialogueMenu();
            if (!Main.dedServ)
            {
                if (CurrentAnimation == Animation.None)
                {
                    if (Main.rand.NextBool(150) && yawnCooldown <= 0)
                    {
                        CurrentAnimation = Animation.Yawning;
                        yawnCooldown = 60 * 18;
                    }

                    yawnCooldown--;
                    if (yawnCooldown < 0)
                        yawnCooldown = 0;

                    waitTime++;

                    //Ukulele faster if the player is sleeping or sitting
                    if (Main.LocalPlayer.sitting.isSitting || Main.LocalPlayer.sleeping.isSleeping)
                        waitTime = Math.Max(waitTime, 60 * 20);

                    if (waitTime > 60 * 25)
                        CurrentAnimation = Animation.Ukuleling;
                }

                //Nautilus needs to wait 5 seconsd to ukulele again
                else
                    waitTime = Math.Min(waitTime, 60 * 20);

                #region Onomatopeias
                Vector2 baseParticlePosition = NPC.Center - Vector2.UnitY * 10f + Vector2.UnitX * 20;

                if (CurrentAnimation == Animation.Laughing)
                {
                    if (onomatopeiaTimer < 100 && onomatopeiaTimer % 25 == 0)
                    {
                        ParticleHandler.SpawnParticle(new NautilusOnomatopeiaParticle(baseParticlePosition, Vector2.UnitX, onomatopeiaTimer == 0 ? 0 : 1));
                    }

                    onomatopeiaTimer++;
                }

                else if (CurrentAnimation == Animation.Sleeping)
                {
                    onomatopeiaTimer++;
                    if (onomatopeiaTimer > 40)
                    {
                        onomatopeiaTimer = 0;
                        ParticleHandler.SpawnParticle(new NautilusOnomatopeiaParticle(baseParticlePosition + Vector2.UnitX * 5f, Vector2.UnitX * 0.4f - Vector2.UnitY * 0.2f, 2));
                    }
                }

                else if (CurrentAnimation == Animation.Ukuleling)
                {
                    onomatopeiaTimer = 0;
                    if (NPC.frameCounter == 0 && (NPC.frame.Y / 68) % 4 == 2)
                    {
                        ParticleHandler.SpawnParticle(new NautilusOnomatopeiaParticle(baseParticlePosition + Vector2.UnitX * 5f + Vector2.UnitY * 14f, new Vector2(0.8f, -0.1f), 3));
                    }
                }

                else
                    onomatopeiaTimer = 0;
                #endregion
            }

            Rectangle hoverRectangle = new Rectangle((int)NPC.Bottom.X - NPC.frame.Width / 2, (int)NPC.Bottom.Y - NPC.frame.Height, NPC.frame.Width, NPC.frame.Height);
            if (hoverRectangle.Contains(Main.MouseWorld.ToPoint()) && (Main.LocalPlayer.Distance(NPC.Center) < 400))
            {
                hovered = true;
                Main.LocalPlayer.cursorItemIconID = -1;
            }
            else
                hovered = false;

            if (!Main.dedServ && !Main.LocalPlayer.DeadOrGhost && !Main.mapFullscreen && hovered && Main.mouseRight && Main.mouseRightRelease)
            {
                CoolDialogueUIManager.DialogueHandler = GetInstance<SirNautilusDialogue>();
                CoolDialogueUIManager.theUI.anchorNPC = NPC;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[Type].Value;
            Rectangle frame = NPC.frame;
            drawColor = NPC.TintFromBuffAesthetic(drawColor);

            //Just in case
            if (NPC.frame.Height > 100 || NPC.frame.Width > 100)
            {
                FindFrame(68);
                frame = NPC.frame;
            }

            ///While it is hidden in the bestiary, this is done so dragonlens can draw it correctly
            if (NPC.IsABestiaryIconDummy)
                frame = new Rectangle(0, 0, 52, 68);

            Vector2 scale = new Vector2(NPC.scale);
            Vector2 position = NPC.Bottom + Vector2.UnitX * 6f + Vector2.UnitY * 2f;
            Vector2 origin = new Vector2(frame.Width / 2, frame.Height);

            Main.EntitySpriteDraw(tex, position - screenPos, frame, drawColor, NPC.rotation, origin, scale, 0, 0);

            if (hovered)
            {
                tex = Request<Texture2D>(AssetDirectory.SirNautilus + "NautilusTextBubble").Value;
                Color color = Color.Lerp(Color.White, Color.White * 0.6f, 0.5f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f);

                position += -Vector2.UnitY * NPC.height * 0.75f - Vector2.UnitX * 35f;
                position += -Vector2.UnitY * 8f * (float)Math.Sin(((Main.GlobalTimeWrappedHourly * 1.3f) % 1) * MathHelper.Pi * 0.9f);

                scale.Y *= 1f + 0.15f * (float)Math.Sin(((Main.GlobalTimeWrappedHourly * 1.3f) % 1) * MathHelper.Pi * 0.9f);
                scale.X *= 1f - 0.15f * (float)Math.Sin(((Main.GlobalTimeWrappedHourly * 1.3f) % 1) * MathHelper.Pi * 0.9f);


                Main.EntitySpriteDraw(tex, position - screenPos, null, color, 0, tex.Size() / 2f, scale, SpriteEffects.FlipHorizontally, 0);
            }

            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            int xFrame = 0;

            int frameWidth = 52;
            frameHeight = 68;

            Animation currentAnimation = CurrentAnimation;
            bool returnToNormal = DialogueAnimation == Animation.None;

            //If sleeping and the player is talking to nautilus, and he isn't sleeping, wake him up!
            if (currentAnimation == Animation.Sleeping && returnToNormal && CoolDialogueUIManager.Active && CoolDialogueUIManager.theUI.anchorNPC == NPC)
            {
                CurrentAnimation = Animation.Rattled;
            }

            //Fall frame
            if (NPC.velocity.Y != 0)
            {
                xFrame = 0;
                yFrame = 1;
            }
            else
            {
                switch (currentAnimation)
                {
                    case Animation.None:
                        xFrame = 0;
                        yFrame = 0;
                        break;
                    case Animation.Sleeping:
                        xFrame = 0;
                        yFrame = 2;
                        break;
                    case Animation.Ukuleling:
                        xFrame = 8;

                        //Roughly in time with Sealed Chamber's BPM
                        NPC.frameCounter += 1 / (60f * 0.15f);
                        if (NPC.frameCounter > 1)
                        {
                            NPC.frameCounter = 0;
                            yFrame++;
                        }
                        yFrame %= 16;
                        break;

                    case Animation.Curious:
                        xFrame = 1;

                        //Anim goes up till frame 3
                        if (yFrame < 3)
                        {
                            NPC.frameCounter += 1 / (60f * 0.1f);
                            if (NPC.frameCounter > 1)
                            {
                                NPC.frameCounter = 0;
                                yFrame++;
                            }
                        }

                        //Failsafe?
                        if (yFrame > 3)
                            yFrame = 0;

                        if (returnToNormal)
                            CurrentAnimation = Animation.None;
                        break;

                    case Animation.Yawning:
                        xFrame = 2;

                        NPC.frameCounter += 1 / (60f * 0.1f);
                        if (NPC.frameCounter > 1)
                        {
                            NPC.frameCounter = 0;
                            yFrame++;
                        }

                        //Stop yawning
                        if (yFrame >= 11)
                        {
                            yFrame = 10;
                            CurrentAnimation = Animation.None;
                        }
                        break;

                    case Animation.Laughing:
                        xFrame = 3;

                        NPC.frameCounter += 1 / (60f * 0.08f);
                        if (NPC.frameCounter > 1)
                        {
                            NPC.frameCounter = 0;
                            yFrame++;
                        }
                        yFrame %= 4;

                        if (returnToNormal)
                            CurrentAnimation = Animation.None;

                        break;

                    case Animation.Rattled:
                        xFrame = 4;

                        //Stops at frame 3, until it goes back to normal
                        if (returnToNormal || yFrame < 2)
                        {
                            //Snap to the return frame
                            if (returnToNormal && yFrame <= 2)
                            {
                                yFrame = 3;
                                NPC.frameCounter = 0;
                            }


                            //Frame 2 is quicker than the others
                            if (yFrame == 1)
                                NPC.frameCounter += 1 / (60f * 0.05f);
                            else
                                NPC.frameCounter += 1 / (60f * 0.1f);


                            if (NPC.frameCounter > 1)
                            {
                                NPC.frameCounter = 0;
                                yFrame++;
                            }

                            //Stop the anim
                            if (yFrame > 3)
                            {
                                yFrame = 3;
                                CurrentAnimation = Animation.None;
                            }
                        }
                        break;

                    case Animation.Interested:
                        xFrame = 5;

                        //Stops at frame 2, until it goes back to normal
                        if (returnToNormal || yFrame < 1)
                        {
                            //Snap to the return frame
                            if (returnToNormal && yFrame <= 1)
                            {
                                yFrame = 2;
                                NPC.frameCounter = 0;
                            }


                            NPC.frameCounter += 1 / (60f * 0.1f);
                            if (NPC.frameCounter > 1)
                            {
                                NPC.frameCounter = 0;
                                yFrame++;
                            }

                            //Stop the anim
                            if (yFrame > 3)
                            {
                                yFrame = 3;
                                CurrentAnimation = Animation.None;
                            }
                        }
                        break;

                    case Animation.Shocked:
                        xFrame = 6;

                        //Stops at frame 2, until it goes back to normal
                        if (returnToNormal || yFrame < 2)
                        {
                            //Snap to the return frame
                            if (returnToNormal && yFrame <= 2)
                            {
                                yFrame = 3;
                                NPC.frameCounter = 0;
                            }


                            NPC.frameCounter += 1 / (60f * 0.1f);
                            if (NPC.frameCounter > 1)
                            {
                                NPC.frameCounter = 0;
                                yFrame++;
                            }

                            //Stop the anim
                            if (yFrame > 5)
                            {
                                yFrame = 5;
                                CurrentAnimation = Animation.None;
                            }
                        }
                        break;

                    case Animation.Angry:
                        xFrame = 7;

                        //Snap to the return frame
                        if (returnToNormal && yFrame <= 4)
                        {
                            yFrame = 5;
                            NPC.frameCounter = 0;
                        }


                        NPC.frameCounter += 1 / (60f * 0.1f);
                        if (NPC.frameCounter > 1)
                        {
                            NPC.frameCounter = 0;
                            yFrame++;
                        }

                        //Loop the anim if we don't want to go back to normal yet
                        if (!returnToNormal && yFrame > 4)
                            yFrame = 2;

                        //Stop the anim
                        if (yFrame > 5)
                        {
                            yFrame = 5;
                            CurrentAnimation = Animation.None;
                        }

                        break;
                }
            }


            NPC.frame = new Rectangle(xFrame * (frameWidth + 2), yFrame * (frameHeight + 2), frameWidth, frameHeight);
        }

        public override bool? CanFallThroughPlatforms()
        {
            return true;
        }
    }


    public class NautilusOnomatopeiaParticle : Particle
    {
        public override string Texture => AssetDirectory.SirNautilus + "NautilusOnomatopeias";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public Rectangle Frame;
        public float offsetOscillation;
        public bool fadeIn = false;

        public NautilusOnomatopeiaParticle(Vector2 position, Vector2 velocity, int variant)
        {
            Position = position;
            Velocity = velocity;
            Color = Color.White;
            Scale = 1f;
            Lifetime = 80;

            if (variant == 2)
            {
                fadeIn = true;
                Lifetime = 100;
            }

            //Random notes
            if (variant == 3)
                variant += Main.rand.Next(4);
            Variant = variant;

            Frame = new Rectangle(0, 16 * Variant, 22, 14);

            //The last note is 1px taller
            if (variant == 6)
                Frame.Height += 2;

            offsetOscillation = Main.rand.NextFloat(MathHelper.TwoPi) * 0.2f;
        }

        public override void Update()
        {
            float oscillationStrenght = 1.2f;
            if (Variant == 2)
                oscillationStrenght *= 0.3f;
            if (Variant > 2)
                oscillationStrenght *= 0.8f;

            Position.Y += MathF.Sin(-Time * 0.1f + offsetOscillation) * oscillationStrenght;

            Color = Lighting.GetColor(Position.ToTileCoordinates());
            if (Color.GetBrightness() > 0.5f)
                Color = Color.White;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D dustTexture = ParticleTexture;
            Color drawColor = Color;
            Vector2 drawScale = Vector2.One * Scale;

            if (LifetimeCompletion > 0.7f)
            {
                drawScale.Y *= MathF.Pow(Utils.GetLerpValue(1f, 0.7f, LifetimeCompletion, true), 0.5f);
            }

            if (fadeIn && LifetimeCompletion < 0.2f)
                drawColor *= Utils.GetLerpValue(0f, 0.2f, LifetimeCompletion, true);

            spriteBatch.Draw(dustTexture, Position - basePosition, Frame, drawColor, Rotation, Frame.Size() / 2f, drawScale, SpriteEffects.None, 0);
        }
    }
}