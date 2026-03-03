using CalamityFables.Content.Items.Cursed;
using CalamityFables.Particles;
using ReLogic.Utilities;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;
using static CalamityFables.Helpers.DropHelper;

namespace CalamityFables.Content.NPCs.Cursed
{
    public class Potlet : ModNPC
    {
        #region Setup and variables
        public const float SPAWN_RADIUS = 300f;


        public enum AIState
        {
            Spawning_Roll,
            Spawning_Stand,
            Walking,
            Death_Fall,
            Death_Ripple
        }


        public AIState CurrentState {
            get => (AIState)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }
        public ref float AITimer => ref NPC.ai[1];
        public ref float CurrentFrame => ref NPC.localAI[0];

        public bool Running => Math.Abs(NPC.velocity.X) > 3f;

        public bool OnTopOfTiles => FablesUtils.SolidCollisionFix(NPC.BottomLeft - Vector2.UnitY * 2, NPC.width, 4, true);

        public Player Target => Main.player[NPC.target];
        public SlotId rollSoundSlot;

        public override string Texture => AssetDirectory.CursedNPCs + Name;
        public static Rectangle ShellFrame = new Rectangle(386, 494, 32, 32);

        public override void Load()
        {
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Potlet");
            this.HideFromBestiary();
            Main.npcFrameCount[Type] = 8;
        }

        public override void SetDefaults()
        {
            NPC.damage = 0;
            NPC.width = 24;
            NPC.height = 24;
            NPC.defense = 5;
            NPC.lifeMax = 50;
            NPC.knockBackResist = 0.04f;
            NPC.value = Item.buyPrice(0, 1, 0, 0);
            
            NPC.lavaImmune = true;
            //Lifeform analyzer
            NPC.rarity = 4;
        }
        #endregion


        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            //Can't be hit when spawning (only for the first hit is it allowed)
            if (CurrentState == AIState.Spawning_Stand || (CurrentState == AIState.Spawning_Roll && (projectile.minion || NPC.life < NPC.lifeMax)))
                return false;
            return base.CanBeHitByProjectile(projectile);
        }

        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            //Cant be hovered over
            if (CurrentState == AIState.Spawning_Roll && AITimer == 0)
                boundingBox = new Rectangle(0, 0, 0, 0);
            else
                boundingBox.Inflate(-(82 - 24) / 2, 0);
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            //nigh impervious at the start
            if (CurrentState == AIState.Spawning_Roll)
                modifiers.SetMaxDamage(1);
        }

        public int previousTarget;

        public override void AI()
        {
            //Keep track of closest player
            if (CurrentState != AIState.Death_Fall && CurrentState != AIState.Death_Ripple)
                NPC.TargetClosest(false);

            NPC.gfxOffY = 0;
            float animationSpeed = 1f;
            bool playerWithinRange = Target.WithinRange(NPC.Center, 200) || NPC.life < NPC.lifeMax;


            switch (CurrentState)
            {
                //This includes the state when just pretending to be a pot
                case AIState.Spawning_Roll:

                    //Stuck as a pot
                    if (AITimer == 0)
                    {
                        NPC.direction = (NPC.Center.X - Target.Center.X).NonZeroSign();

                        //If the player is too far away, stop the rest of the AI and dont roll out
                        if (!playerWithinRange)
                            break;
                    }

                    float speed = (float)Math.Sin(MathHelper.PiOver2 * AITimer + MathHelper.PiOver2); //Ease out

                    //Sluggishly roll away from the player
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * speed * 2.2f, 0.1f);

                    //Plays the rolling sfx
                    if (!Main.dedServ)
                    {
                        if (!SoundEngine.TryGetActiveSound(rollSoundSlot, out var activeRollSound))
                        {
                            rollSoundSlot = SoundEngine.PlaySound(PeculiarPot.RollingSound, NPC.Center);
                            SoundEngine.TryGetActiveSound(rollSoundSlot, out activeRollSound);
                        }

                        if (activeRollSound != null)
                        {
                            activeRollSound.Volume = (float)Math.Sin(MathHelper.Pi * AITimer);
                            activeRollSound.Position = NPC.Center;
                            SoundHandler.TrackSound(rollSoundSlot);
                        }
                    }

                    //Transition into standing anim
                    if (AITimer >= 1)
                    {
                        NPC.velocity.X = 0;
                        AITimer = 0;
                        CurrentState = AIState.Spawning_Stand;
                    }

                    break;
                case AIState.Spawning_Stand:

                    //Stand up and grow
                    int desiredHeight = (int)Math.Min(MathHelper.Lerp(24, 50, AITimer), 50);

                    NPC.position.Y += NPC.height;
                    NPC.height = desiredHeight;
                    NPC.position.Y -= NPC.height;
                    animationSpeed = 0.7f;

                    if (AITimer >= 1)
                    {
                        AITimer = 0;
                        CurrentState = AIState.Walking;
                        NPC.direction = (NPC.Center.X - Target.Center.X).NonZeroSign();
                    }

                    break;

                case AIState.Walking:
                    animationSpeed = 0.6f;

                    NPC.spriteDirection = NPC.direction;
                    float stepSpeed = 0.3f;
                    Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref stepSpeed, ref NPC.gfxOffY);
                    Vector4 results = Collision.WalkDownSlope(NPC.position, NPC.velocity, NPC.width, NPC.height, 0.3f);
                    NPC.position = new Vector2(results.X, results.Y);
                    NPC.velocity = new Vector2(results.Z, results.W);


                    bool bumpedFoot = FablesUtils.SolidCollisionFix(NPC.position + Vector2.UnitX * (NPC.direction == -1 ? -2 : NPC.width), 2, NPC.height, false);
                    bool bumpedHead = FablesUtils.SolidCollisionFix(NPC.position - Vector2.UnitY * 16, NPC.width, 16, false);

                    if (bumpedFoot)
                    {
                        //Turn around if we cant jump
                        if (bumpedHead)
                        {
                            NPC.direction *= -1;
                            NPC.velocity.X *= -0.4f;
                        }
                        else if (OnTopOfTiles && NPC.velocity.Y <= 0)
                        {
                            NPC.velocity.Y = -10;
                        }
                    }

                    float distanceToPlayer = NPC.Distance(Target.Center);

                    float xSpeed = (2f + Utils.GetLerpValue(500f, 350f, distanceToPlayer, true) * 3f) * Math.Min(1f, AITimer);
                    NPC.velocity.X =  MathHelper.Lerp(NPC.velocity.X, xSpeed * NPC.direction, 0.1f);
                    NPC.velocity.Y += 0.2f;
                    break;

                //Play an animation and then start falling
                case AIState.Death_Fall:
                    animationSpeed = 0.6f;
                    NPC.noGravity = true;
                    NPC.height = 20; //Shrink NPC


                    //Start falling after the animation has played
                    if (AITimer >= 1f)
                        NPC.velocity.Y += 0.3f;
                    //Slow down heavily otherwise
                    else
                        NPC.velocity *= 0.1f;

                    //Transition into the death ripple after hitting the floor
                    if (OnTopOfTiles)
                    {
                        AITimer = 0;
                        CurrentState = AIState.Death_Ripple;
                    }

                    break;

                //Just play an animation and despawn when its over
                case AIState.Death_Ripple:
                default:
                    animationSpeed = 0.6f;
                    NPC.velocity = Vector2.Zero;
                    if (AITimer >= 1)
                        NPC.active = false;
                    break;
            }



            previousTarget = NPC.target;

            if (CurrentState != AIState.Spawning_Roll || AITimer > 0 || playerWithinRange)
                AITimer += 1 / (animationSpeed * 60f);
        }

        public override bool CheckDead()
        {
            if (CurrentState == AIState.Death_Fall || CurrentState == AIState.Death_Ripple)
                return false;

            NPC.life = 1;
            NPC.dontTakeDamage = true;
            CurrentState = AIState.Death_Fall;
            AITimer = 0;
            CurrentFrame = 0;
            NPC.netUpdate = true;

            if (NPC.ai[2] != 0 && NPC.ai[3] != 0)
            {
                (TileEntity.ByPosition[new Point16((int)NPC.ai[2], (int)NPC.ai[3])] as ModTileEntity).Kill((int)NPC.ai[2], (int)NPC.ai[3]);
                if (Main.netMode == NetmodeID.Server)
                    new KillTileEntityPacket(new Point16((int)NPC.ai[2], (int)NPC.ai[3])).Send(runLocally: false);
            }

            //Crack!
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(PeculiarPot.BreakSound, NPC.Center);
                CameraManager.Shake += 4f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int item = Item.NewItem(NPC.GetSource_Death(), NPC.Hitbox, ModContent.ItemType<PeculiarPot>());
                Main.item[item].velocity += Main.rand.NextVector2CircularEdge(8f, 4f);
            }

            //Never dies on its own
            return false;
        }


        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Width = 82;
            NPC.frame.Height = 64;

            switch (CurrentState)
            {
                case AIState.Spawning_Roll:
                    NPC.frameCounter = 0;
                    NPC.frame.X = 0;
                    NPC.frame.Y = 0;
                    break;

                case AIState.Spawning_Stand:
                    NPC.frame.X = 0;
                    int standFrame = (int)Math.Min(AITimer * 8f, 7);
                    NPC.frame.Y = (NPC.frame.Height + 2) * standFrame;
                    break;

                case AIState.Walking:


                    NPC.frameCounter++;
                    int maxFrame;
                    int frameTime = 4;

                    if (Running)
                    {
                        NPC.frame.X = (NPC.frame.Width + 2) * 2;
                        maxFrame = 5;

                        frameTime = 4;
                        frameTime -= (int)(Utils.GetLerpValue(3f, 6f, Math.Abs(NPC.velocity.X), true)) * 2;
                    }   
                    //Walk
                    else
                    {
                        NPC.frame.X = (NPC.frame.Width + 2) * 1;
                        maxFrame = 8;
                    }

                    if (NPC.frameCounter > frameTime)
                    {
                        NPC.frameCounter = 0;
                        CurrentFrame++;
                    }

                    CurrentFrame %= maxFrame;
                    NPC.frame.Y = (NPC.frame.Height + 2) * (int)CurrentFrame;

                    break;

                case AIState.Death_Fall:

                    NPC.frame.X = (NPC.frame.Width + 2) * 3;
                    int deathFrame = (int)Math.Min(AITimer * 6f, 6);
                    NPC.frame.Y = (NPC.frame.Height + 2) * deathFrame;
                    break;

                case AIState.Death_Ripple:

                    NPC.frame.X = (NPC.frame.Width + 2) * 4;
                    int rippleFrame = (int)Math.Min(AITimer * 5f, 4);
                    NPC.frame.Y = (NPC.frame.Height + 2) * rippleFrame;
                    break;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Draw the body.
            Texture2D texture = TextureAssets.Npc[Type].Value;
            Vector2 drawPosition = NPC.Bottom - screenPos + NPC.GfxOffY();
            SpriteEffects direction = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            float drawRotation = NPC.rotation;
            float potExtraRotation = MathHelper.Pi;

            if (CurrentState == AIState.Spawning_Roll)
                drawRotation -= (1 - AITimer) * MathHelper.Pi * NPC.direction;


            switch (CurrentState)
            {
                case AIState.Death_Fall:

                    //Shake
                    if (AITimer < 1f)
                        drawPosition += Main.rand.NextVector2Circular(4f, 4f) * (1 - AITimer);

                    drawPosition.Y += 12f;
                    drawRotation = 0;
                    break;

                case AIState.Death_Ripple:
                    drawPosition.Y += 8f;
                    drawRotation = 0;
                    break;
            }

            //We have to set the true width here otherwise it interferes with the hover text thing
            Rectangle frame = NPC.frame;

            if ((int)CurrentState > (int)AIState.Spawning_Roll)
                Main.spriteBatch.Draw(texture, drawPosition, frame, NPC.GetAlpha(drawColor), drawRotation, new Vector2(frame.Width * 0.5f, frame.Height), NPC.scale, direction, 0f);

            switch(CurrentState)
            {
                case AIState.Spawning_Roll:
                    drawPosition = NPC.Bottom - Vector2.UnitY * 15f - screenPos + NPC.GfxOffY();
                    break;
                case AIState.Spawning_Stand:

                    drawPosition = NPC.Bottom - Vector2.UnitY * 15f - screenPos + NPC.GfxOffY();
                    drawPosition.Y -= (float)Math.Pow(Math.Min(AITimer, 1f), 0.63f) * 34f;
                    drawPosition.Y += (float)Math.Sin(Utils.GetLerpValue(0.4f, 0.7f, AITimer, true) * MathHelper.Pi) * 4f;
                    break;
                default:
                    drawPosition = NPC.Bottom - Vector2.UnitY * 49f - screenPos + NPC.GfxOffY();

                    //Run
                    if (Running)
                    {

                        float animOffset = -0.3f;
                        float animCycle = CurrentFrame / 5f * MathHelper.Pi;

                        drawPosition.X -= (float)Math.Sin(animCycle + 0.5f + animOffset) * 2f * NPC.spriteDirection;
                        drawPosition.Y -= (float)Math.Sin(animCycle + 0.5f + animOffset) * 6f;
                        potExtraRotation -= (float)Math.Sin(animCycle + animOffset) * 0.2f * NPC.direction;
                    }

                    //No run
                    else
                    {
                        float animOffset = -0.3f;
                        float animCycle = CurrentFrame / 8f * MathHelper.Pi;

                        drawPosition.X -= (float)Math.Sin(animCycle + 0.5f + animOffset) * 2f * NPC.spriteDirection;
                        drawPosition.Y -= (float)Math.Sin(animCycle + 0.5f + animOffset) * 4f;
                        potExtraRotation -= (float)Math.Sin(animCycle + animOffset) * 0.2f * NPC.direction ;
                    }
                    break;
            }

            if ((int)CurrentState < (int)AIState.Death_Fall)
                Main.spriteBatch.Draw(texture, drawPosition, ShellFrame, NPC.GetAlpha(drawColor), drawRotation + potExtraRotation, ShellFrame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0f);

            return false;
        }


        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(PeculiarPot.ChipSound, NPC.Center);

            return;
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = NPC.Center + Main.rand.NextVector2Circular(6f, 6f) - Vector2.UnitY * 8f;
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 0.7f + NPC.velocity * 0.6f;
                if (velocity.Y > 0)
                    velocity.Y *= 0.5f;

                Gore.NewGoreDirect(NPC.GetSource_Misc("Wah"), position, velocity, Mod.Find<ModGore>("PeculiarPot_Gore" + Main.rand.Next(1, 4).ToString()).Type);
            }

            for (int i = 0; i < 7; i++)
            {
                Dust.NewDustPerfect(NPC.Center - Vector2.UnitY * 8f + Main.rand.NextVector2Circular(20f, 20f), DustID.DesertPot, Main.rand.NextVector2Circular(4f, 4f));
            }
        }
    }
}
