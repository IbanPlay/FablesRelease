using CalamityFables.Content.Projectiles;
using Terraria.Utilities;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public partial class SirNautilus : ModNPC
    {
        #region Movement utilities
        /// <summary>
        /// Jump to a point in a jump-like arc with no care for tile collision
        /// </summary>
        /// <param name="pos">The position to jump towards</param>
        /// <param name="oldPos">The position the jump was started at</param>
        /// <param name="speed">The speed of the jump. 1 means that the jump all takes place in one frame. 0.1 means it takes place in 10 frames</param>
        /// <param name="spins">How many 360° spins does nautilus during his hop</param>
        /// <returns></returns>
        private bool JumpToPos(Vector2 pos, Vector2 oldPos, float speed, float spins = 0)
        {
            if (AttackTimer > 0)
            {
                Vector2 dir = NPC.DirectionTo(pos);

                float progress = 1 - AttackTimer;

                //Get the position along the jump curve nautilus should go at
                Vector2 targetedPosition = Vector2.Lerp(oldPos, pos, -((float)Math.Cos(MathHelper.Pi * progress) - 1) / 2f);
                float bumpStrenght = Utils.GetLerpValue(100f, 400f, Math.Abs(pos.X - oldPos.X)) * 20f;
                targetedPosition -= dir.RotatedBy(MathHelper.PiOver2 * Math.Sign(dir.X)) * (float)Math.Sin(progress * 3.14f) * bumpStrenght;

                //Move towards the position you should be in
                NPC.velocity = targetedPosition - NPC.Center;
                NPC.rotation = progress * MathHelper.TwoPi * spins * Math.Sign(pos.X - oldPos.X);

                AttackTimer -= speed;
            }

            if (AttackTimer <= 0)
                NPC.rotation = 0f;

            return AttackTimer <= 0;
        }

        /// <summary>
        /// Dash to a position with a slight telegraph where nautilus backs off, before instantly teleporting to the position with no care for tile collision
        /// </summary>
        /// <param name="pos">The position to teleport to</param>
        /// <param name="oldPos">The position the teleportation was started at</param>
        /// <param name="speed">The speed of the teleportation. 1 means that the dash all takes place in one frame. 0.1 means it takes place in 10 frames</param>
        /// <param name="percentOfTheAnimationTelegraphing">How much of the animation is spent telegraphing (aka "bouncing" backwards</param>
        /// <param name="telegraphMidPoint">How much of the telegraph animation time is spent going backwards</param>
        /// <param name="teleportationOffset">Offset between the teleportation position and where nautilus will actually reappear after the dash</param>
        /// <param name="postTeleportationSpeedPercent">How much of the distance between the teleportation target and nautilus should be turned into velocity after the dash</param>
        /// <param name="backwardsDistance">How far back should nautilus move during the telegraph</param>
        /// <returns></returns>
        private bool DashToPos(Vector2 pos, Vector2 oldPos, float speed, float percentOfTheAnimationTelegraphing, float telegraphMidPoint, Vector2 teleportationOffset, float postTeleportationSpeedPercent, float backwardsDistance = 20f)
        {
            if (AttackTimer > 0)
            {
                Vector2 dir = (pos.X - oldPos.X).NonZeroSign() * Vector2.UnitX; ;

                float progress = 1 - AttackTimer;


                if (progress < percentOfTheAnimationTelegraphing)
                {
                    //Scale the progress to only be about the duration of the telegraph
                    progress /= percentOfTheAnimationTelegraphing;
                    if (progress < telegraphMidPoint)
                        progress = (progress / telegraphMidPoint) * 0.5f;
                    else
                        progress = 0.5f + (progress - telegraphMidPoint) / (1 - telegraphMidPoint) * 0.5f;

                    NPC.Center = oldPos - dir * (float)Math.Sin(progress * MathHelper.Pi) * backwardsDistance;
                }

                else
                {
                    //Instantly teleport
                    if (progress - percentOfTheAnimationTelegraphing < speed)
                    {
                        //Instantly teleport
                        NPC.Center = pos - teleportationOffset;
                        NPC.velocity = (pos - NPC.Center) * postTeleportationSpeedPercent;

                        Vector2 dustOrigin = NPC.Center + 2f * NPC.velocity;
                        for (int i = -1; i <= 1; i += 2)
                        {
                            int dustCount = Main.rand.Next(6, 15);
                            for (int j = 0; j <= dustCount; j++)
                            {
                                Vector2 dustPos = dustOrigin + Main.rand.NextVector2Circular(0f, 15f) + (Main.rand.NextBool() ? -1 : 1) * Vector2.UnitY * (float)Math.Pow(Main.rand.NextFloat(0f, 1f), 0.3f) * NPC.height * 0.5f;

                                Dust smoke = Dust.NewDustPerfect(dustPos, 31, dir * i * Main.rand.NextFloat(2f, 5f), Scale: Main.rand.NextFloat(0.95f, 1.3f));
                                smoke.noGravity = true;
                            }

                            dustOrigin = oldPos;
                        }
                    }

                    else
                        NPC.velocity *= 0.9f;
                }

                AttackTimer -= speed;
            }

            return AttackTimer <= 0;
        }

        public void WalkBehavior()
        {
            NPC.TargetClosest(true);

            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
            NPC.rotation = 0f;
            AttackTimer -= 0.03f;

            NPC.noTileCollide = false;
            NPC.noGravity = false;

            float walkSpeed = 1f;
            //Walk faster if the player is far
            if (Patience > 0.2f)
                walkSpeed += Utils.GetLerpValue(0.2f, 1f, Patience, true) * 9f;

            //Face the player
            float playerLocation = NPC.Center.X - Target.Center.X;
            NPC.direction = playerLocation < 0 ? 1 : -1;
            float distanceToPlayer = Math.Abs(NPC.Center.X - Target.Center.X);

            //Get some distance
            if (distanceToPlayer < 110f && SubState == ActionState.SlowWalk)
                SubState = ActionState.SlowWalkAway;
            //Draw near
            if (distanceToPlayer > 140f && SubState == ActionState.SlowWalkAway)
                SubState = ActionState.SlowWalk;


            if (SubState == ActionState.SlowWalkAway)
            {
                walkSpeed *= 3;
                NPC.velocity.X = (NPC.velocity.X * 20f - walkSpeed * NPC.direction) / 21f;
            }
            else
            {
                NPC.velocity.X = (NPC.velocity.X * 20f + walkSpeed * NPC.direction) / 21f;
            }

            //Jump if encountering a wall
            if (NPC.velocity.Y == 0 && NPC.collideX)
                NPC.velocity.Y = -8;

            ManageTridentReapparition();

            if (Collision.SolidCollision(NPC.BottomLeft, NPC.width, 2) && NPC.velocity.Y >= 0 && NPC.Bottom.Y >= Target.Bottom.Y)
                NPC.velocity.Y = 0;
        }
        #endregion

        #region Trident throw
        public float GetTilePreference(int tileX, int centerX, int areaWidth) => 0.4f + 0.6f * (float)Math.Pow(1 - Math.Abs(tileX - centerX) / (areaWidth / 2f), 0.3f);

        public void TridentThrowAttack()
        {
            //Initialization 
            if (SubState == ActionState.TridentThrow)
            {
                NPC.TargetClosest(true);

                if (!NPC.HasValidTarget)
                {
                    AIState = ActionState.SlowWalk;
                    return;
                }

                Stamina -= 0.5f;
                NPC.velocity = Vector2.Zero;
                NPC.noGravity = true;

                int minumumRangeForDirectThrow = 120;
                int maximumRangeForCeilingThrow = 650;
                float distanceToTarget = NPC.Distance(Target.Center);

                //Choose the variant of the attack based on the distance to the player (Player too close = always uses the ceiling throw, player too far = always uses the direct throw)
                SubState = ActionState.TridentThrow_AtPlayer;
                movementTarget = Target.Center;

                if (Main.rand.NextFloat() > 0.7f * Utils.GetLerpValue(minumumRangeForDirectThrow, maximumRangeForCeilingThrow, distanceToTarget, false) || true)
                {
                    SubState = ActionState.TridentThrow_AtCeiling;

                    //Check for a ceiling tile that is valid and above the player.
                    int lookupWidth = 6 - (int)DifficultyScale * 1; //Narrower range of precision in higher difficulties
                    int ceilingLookupOffset = (int)MathHelper.Clamp((Target.velocity.X * 3), -8, 8);

                    WeightedRandom<Point> ceilingTopography = GetCeilingTopography_TridentThrow(Target.BottomLeft.ToTileCoordinates(), lookupWidth, 32, ceilingLookupOffset);

                    //If there is absolutely no ceiling, just throw the trident straight at the player
                    if (ceilingTopography.elements.Count == 0)
                        SubState = ActionState.TridentThrow_AtPlayer;

                    else
                        movementTarget = ceilingTopography.Get().ToWorldCoordinates();
                }
            }

            //Attack (Most of the code here is the same between both alternative versions
            if (SubState == ActionState.TridentThrow_AtPlayer || SubState == ActionState.TridentThrow_AtCeiling)
            {
                //Retarget a bit if throwing at player
                if (SubState == ActionState.TridentThrow_AtPlayer)
                {
                    float retargetingStrenght = DifficultyScale * 0.05f + 0.15f;
                    movementTarget = Vector2.Lerp(movementTarget, Target.Center, retargetingStrenght * AttackTimer);
                }

                //Also retarget with tiles
                else if (DifficultyScale > 0 && NPC.Distance(Target.Center) > 600)
                {
                    int lookupWidth = 6 - (int)DifficultyScale * 1; //Narrower range of precision in higher difficulties
                    int ceilingLookupOffset = (int)MathHelper.Clamp((Target.velocity.X * 2f), -8, 8);

                    WeightedRandom<Point> ceilingTopography = GetCeilingTopography_TridentThrow(Target.BottomLeft.ToTileCoordinates(), lookupWidth, 32, ceilingLookupOffset);

                    if (ceilingTopography.elements.Count > 0)
                        movementTarget = ceilingTopography.Get().ToWorldCoordinates();
                }

                //Ceiling hit comes out faster, since it will take time for the rock to fall
                float attackSpeed = 1 / (60f * (SubState == ActionState.TridentThrow_AtPlayer ? 0.4f : 0.25f));

                //Comes out slower if the palyer is right on nautie
                if (SubState == ActionState.TridentThrow_AtCeiling && NPC.Distance(Target.Center) < 50)
                    attackSpeed *= 1.4f;

                AttackTimer -= attackSpeed;

                NPC.direction = NPC.DirectionTo(movementTarget).X.NonZeroSign();
                NPC.velocity *= 0.86f;

                if (AttackTimer <= 0)
                {
                    //throw Trident
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float ai0 = SubState == ActionState.TridentThrow_AtCeiling ? movementTarget.X : DifficultyScale >= 3 ? Target.whoAmI : -1;
                        float ai1 = SubState == ActionState.TridentThrow_AtCeiling ? movementTarget.Y : 0f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.DirectionTo(movementTarget) * 10f, ModContent.ProjectileType<NautilusTrident>(), TridentThrow_DirectDamage / 2, 1, Main.myPlayer, ai0, ai1);

                    }

                    tridentReapparitionTimer = TridentReapparitionTime;

                    SoundEngine.PlaySound(TridentThrow, NPC.Center);
                    AttackTimer = 1;
                    SubState = ActionState.TridentThrow_RecoveryAnim;
                    NPC.noGravity = false;
                }
            }

            else if (SubState == ActionState.TridentThrow_RecoveryAnim)
            {
                //AttackTimer -= 0.05f;
                AttackTimer -= 1 / (60f * 0.6f);
            }
        }

        //https://media.discordapp.net/attachments/802291445360623686/1031684059006509136/unknown.png
        //Visual explanation
        public WeightedRandom<Point> GetCeilingTopography_TridentThrow(Point searchOrigin, int lookupWidth, int maxHeight, int ceilingLookupOffset)
        {
            WeightedRandom<Point> ceilingTopography = new WeightedRandom<Point>();
            int halfLookupWidth = lookupWidth / 2;

            int furthestEdgeDirection = Math.Sign(ceilingLookupOffset);
            if (furthestEdgeDirection == 0)
                furthestEdgeDirection = 1;

            int areaFurthestEdgeX = searchOrigin.X + ceilingLookupOffset + halfLookupWidth * furthestEdgeDirection;

            int direction = Math.Sign(areaFurthestEdgeX - searchOrigin.X);
            int bonkPointX = searchOrigin.X;
            int currentPositionX = searchOrigin.X;

            bool hasBonkedIntoAWall = false;

            //Try to move towards the furthest part of the zone up until we either reach it or hit a wall
            while (currentPositionX != areaFurthestEdgeX + direction)
            {
                bool clearTileUnder = false;

                for (int j = 0; j < maxHeight; j++)
                {
                    Point pos = new Point(0, Target.BottomLeft.ToTileCoordinates().Y) + new Point(currentPositionX, -j);
                    Tile tile = Framing.GetTileSafely(pos);

                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !TileID.Sets.Platforms[tile.TileType])
                    {
                        if (clearTileUnder)
                        {
                            break;
                        }
                    }
                    else
                        clearTileUnder = true;

                    //If we reached the top of the lookup and couldnt find a ceiling, we bonked into a wall.
                    if (j == maxHeight - 1)
                    {
                        bonkPointX = currentPositionX - direction;
                        hasBonkedIntoAWall = true;
                    }
                }

                currentPositionX += direction;

                //If we hit a wall, stop going
                if (hasBonkedIntoAWall)
                    break;

                //If we reached the end of the lookup aka we got to the edge with no walls, just save that as the "bonk point" even if we didnt bonk into anything
                if (currentPositionX == areaFurthestEdgeX + direction)
                    bonkPointX = areaFurthestEdgeX;
            }

            //Now we reached the furthest edge of the boulder fall zone.
            //We then try to look in the other direction to check if all is clear

            //First, we check the center of the area and its width (this is for probability calculation
            int areaCenterX = areaFurthestEdgeX - halfLookupWidth * direction;
            int areaWidth = lookupWidth;

            //If we bonked into a wall, we use the center as : either the wall itself, or the centerof the zone, whichever is closer to the player
            if (hasBonkedIntoAWall)
            {
                if (direction == 1)
                    areaCenterX = Math.Min(bonkPointX, areaCenterX);
                else
                    areaCenterX = Math.Max(bonkPointX, areaCenterX);

                //If we bonked further than the center of the area, we use the width that's peeking out.
                //If we bonked before reaching the center of the area, we use half the width of the area.
                int howManyTilesLeftUntilFurthestEdgeWhenIBonked = Math.Abs(areaFurthestEdgeX - bonkPointX);
                areaWidth = Math.Max(lookupWidth / 2, lookupWidth - howManyTilesLeftUntilFurthestEdgeWhenIBonked);
            }

            direction *= -1;
            hasBonkedIntoAWall = false; //Reset this to check if we bonk into a wall on the other side
            List<Point> ceilingTiles = new List<Point>();


            for (int i = 0; i < areaWidth; i++)
            {
                bool clearTileUnder = false;

                for (int j = 0; j < maxHeight; j++)
                {
                    Point pos = new Point(0, Target.BottomLeft.ToTileCoordinates().Y) + new Point(bonkPointX + i * direction, -j);
                    Tile tile = Framing.GetTileSafely(pos);

                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !TileID.Sets.Platforms[tile.TileType])
                    {
                        if (clearTileUnder)
                        {
                            ceilingTiles.Add(pos);
                            break;
                        }
                    }
                    else
                        clearTileUnder = true;

                    //If we reached the top of the lookup and couldnt find a ceiling, we bonked into a wall.
                    if (j == maxHeight - 1)
                    {
                        hasBonkedIntoAWall = true;
                        break;
                    }
                }

                //If we hit a wall, stop going
                if (hasBonkedIntoAWall)
                    break;
            }

            foreach (Point ceilingNode in ceilingTiles)
            {
                ceilingTopography.Add(ceilingNode, GetTilePreference(ceilingNode.X, areaCenterX, areaWidth));
            }

            return ceilingTopography;
        }

        #endregion

        #region Double swipe attack
        #region Variables about timers and angles
        public float DoubleSwipe_SwingReach(int swing) => swing <= 1 ? 98f : 160f;

        public float DoubleSwipe_CurrentSwingReach => DoubleSwipe_SwingReach(CurrentSlashVariant);
        public float DoubleSwipe_CurrentSwingTelegraphLenght => DoubleSwipe_CurrentSwingBaseTelegraphLenght + ExtraMemory;
        public float DoubleSwipe_CurrentSwingEndLag {
            get {
                //How much of the telegraph-less timer is the end lag(Aka, if the telegraph is 0.5 in lenght and the end lag is 0.1, the telegraph-less time is 0.5, and so the end lag is 20% of that period)
                float percentOfTelegraphlessPeriod = DoubleSwipe_CurrentSwingBaseEndLag / (1 - DoubleSwipe_CurrentSwingBaseTelegraphLenght);

                float newTelegraphlessPeriod = 1 - DoubleSwipe_CurrentSwingTelegraphLenght;
                return newTelegraphlessPeriod * percentOfTelegraphlessPeriod;
            }
        }


        public float DoubleSwipe_CurrentSwingBaseTelegraphLenght => CurrentSlashVariant <= 1 ? 0.5f : 0.45f;
        public float DoubleSwipe_CurrentSwingBaseEndLag => CurrentSlashVariant <= 1 ? 0.08f : 0.2f;



        /// <summary>
        /// Gets the variant of the slash attack that is being performed
        /// </summary>
        public int CurrentSlashVariant => SubState == ActionState.DoubleSwipe_FirstSwipe ? 1 : SubState == ActionState.DoubleSwipe_SecondSwipe ? 2 : 0;

        /// <summary>
        /// Gets the slash attack that is currently being done, without the telegraph period and the end lag
        /// </summary>
        public int ActiveSlash {
            get {
                int currentSwing = CurrentSlashVariant;
                if (currentSwing == 0)
                    return 0;

                float progress = (1 - AttackTimer - DoubleSwipe_CurrentSwingTelegraphLenght) / CurrentSwingLenght;
                if (progress < 0 || progress > 1)
                    return 0;

                return currentSwing;
            }
        }

        /// <summary>
        /// Gets the lenght of the current swing animation, without the end lag
        /// </summary>
        public float CurrentSwingLenght => 1 - DoubleSwipe_CurrentSwingTelegraphLenght - DoubleSwipe_CurrentSwingEndLag;

        public float CurrentSlashTimer {
            get {
                int currentSwing = CurrentSlashVariant;
                if (currentSwing == 0)
                    return -1;
                return Math.Max(1 - AttackTimer - DoubleSwipe_CurrentSwingTelegraphLenght, 0f);
            }
        }

        public float SlashTelegraphCompletion => MathHelper.Clamp((1 - AttackTimer) / DoubleSwipe_CurrentSwingTelegraphLenght, 0, 1);
        public float SlashAttackCompletion => MathHelper.Clamp((1 - AttackTimer - DoubleSwipe_CurrentSwingTelegraphLenght) / (1 - DoubleSwipe_CurrentSwingTelegraphLenght), 0, 1);
        public float ActiveSlashCompletion => MathHelper.Clamp((1 - AttackTimer - DoubleSwipe_CurrentSwingTelegraphLenght) / (1 - DoubleSwipe_CurrentSwingTelegraphLenght - DoubleSwipe_CurrentSwingEndLag), 0, 1);



        public List<Vector2> GetSlashPoints(int pointCount)
        {
            bool secondSlash = CurrentSlashVariant == 2;
            Vector2 originOfCurve;

            if (secondSlash)
                originOfCurve = NPC.Center + Vector2.UnitX * 17f * NPC.spriteDirection;
            else
                originOfCurve = NPC.Center - Vector2.UnitX * 15f * NPC.spriteDirection - Vector2.UnitY * 3f;

            List<Vector2> points = new List<Vector2>();

            for (int i = 0; i < pointCount; i++)
            {
                float progress = i / (float)(pointCount - 1);
                //Second slash swings in the opposite direction
                if (secondSlash)
                    progress = 1 - progress;
                //Also reverse them if nautilus faces right
                if (NPC.spriteDirection == 1)
                    progress = 1 - progress;

                float halfAngle = secondSlash ? MathHelper.PiOver2 * 0.9f : MathHelper.PiOver2;
                float angle = MathHelper.Lerp(-halfAngle, halfAngle, progress) + (NPC.spriteDirection == 1 ? MathHelper.Pi : 0f);

                Vector2 point = angle.ToRotationVector2() * DoubleSwipe_CurrentSwingReach;

                if (secondSlash)
                    point.Y *= 0.35f;

                points.Add(originOfCurve + point);
            }

            return points;
        }
        #endregion

        public void DoubleSlashAttack()
        {
            //Initialize the attack
            if (SubState == ActionState.DoubleSwipe)
            {
                //Callout post
                CuteSpeechSystem.Speak(NPC.Center, 1, RegularSpeech, RegularTone);
                NPC.TargetClosest(true);

                if (!NPC.HasValidTarget || NPC.Distance(Target.Center) > 1200)
                {
                    AIState = ActionState.SlowWalk;
                    return;
                }

                Stamina -= 0.5f;
                NPC.collideX = false;
                NPC.collideY = false;
                NPC.velocity = Vector2.Zero;

                oldPosition = NPC.Center;
                movementTarget = Target.Center;

                ExtraMemory = 0f;
                //If in reach of his first swipe, skip to the swipe, and add some extra telegraph time
                if (NPC.Distance(Target.Center) < DoubleSwipe_SwingReach(1) * 0.7f)
                {
                    SubState = ActionState.DoubleSwipe_FirstSwipe;
                    ExtraMemory = 0.2f; //<- indicates how many extra % the telegraph takes
                }

                //Else, get to a point where the target is in reach
                else
                {
                    SubState = ActionState.DoubleSwipe_CloseDistance1;

                    //Target right in front of the player
                    NPC.noTileCollide = true;
                    NPC.noGravity = true;
                }
            }

            //Close the distance to the player by either leaping at them or dashing straight in front of them
            if (SubState == ActionState.DoubleSwipe_CloseDistance1)
            {
                float animSpeed = 1 / (60f * (5 / 6f));
                float backBounce = 10f;
                float telegraphPercent = 0.67f;
                float telegraphTimeDuringWhichPositionRecalculationIsPossible = 0.7f;

                //In death, nautilus skips the first half of the telegraph and opts to instantly teleport to the player instead, forcing better reflexes.
                //we adjust the anim speed to compensate for that, as the rest of the dash would take way too long if it lasted the whole duration of the regular telegraph + dash
                if (DifficultyScale >= 3)
                {
                    animSpeed *= (1 / (1 - telegraphPercent));
                    telegraphPercent = 0f;
                }

                if (AttackTimer > (1 - telegraphPercent))
                {
                    Vector2 idealPosition = Target.Center;

                    //Retarget the Y position of the player only during a percentage of the telegraph, so players can jump in early to dodge
                    if (AttackTimer > (1 - telegraphPercent * telegraphTimeDuringWhichPositionRecalculationIsPossible))
                        movementTarget.Y = idealPosition.Y;

                    //However, always retarget the X position
                    movementTarget.X = idealPosition.X;

                    //Don't bounce back if the player is litterally above nautilus, looks weird and unnatural
                    backBounce *= Utils.GetLerpValue(0, 80, Math.Abs(movementTarget.X - NPC.Center.X), true);
                    NPC.direction = (movementTarget.X - oldPosition.X).NonZeroSign();
                }

                //Teleport to the player right outside of the hit range (And the dashes momentum should be able to carry nautilus through the rest
                Vector2 teleportDirection = Vector2.UnitX * (movementTarget.X - oldPosition.X).NonZeroSign();
                Vector2 displacementAfterTeleportation = teleportDirection * DoubleSwipe_SwingReach(1) * 1.2f - Vector2.UnitX * Target.velocity.X * 6f;

                //If the player is standing still, nautilus won't get as much extra speed coming out of his dash than if the player is moving
                float velocityConservation = 0.1f + 0.12f * Utils.GetLerpValue(0f, 6f, Math.Abs(Target.velocity.X), true);

                //Do the dash
                bool movementDone = DashToPos(movementTarget, oldPosition, animSpeed, telegraphPercent, 0.7f, displacementAfterTeleportation, velocityConservation, backBounce);

                //Slow down nautilus if he's about to dash too close to the player. We don't want him to dash so fast he ends up behind the player
                if (Math.Abs(NPC.velocity.X + NPC.Center.X - (Target.Center.X + Target.velocity.X * 2f)) < 0.2f * DoubleSwipe_SwingReach(1))
                    NPC.velocity.X *= 0.6f;

                if (movementDone)
                {
                    AttackTimer = 1;
                    SubState = ActionState.DoubleSwipe_FirstSwipe;
                }
            }

            else if (SubState == ActionState.DoubleSwipe_FirstSwipe)
            {
                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

                float attackSpeed = 1 / (60f * 0.55f);
                if (ExtraMemory > 0)
                {
                    float telegraphUnit = 0.55f / DoubleSwipe_CurrentSwingBaseTelegraphLenght;
                    attackSpeed = 1 / (60f * (0.55f + ExtraMemory * telegraphUnit));
                }

                AttackTimer -= attackSpeed;
                NPC.noTileCollide = false;

                float speed = NPC.velocity.Length();
                float distanceToTarget = NPC.Distance(Target.Center);

                //Slow down (slow down even more if in range anyways
                if (distanceToTarget < DoubleSwipe_CurrentSwingReach * 0.5f)
                    NPC.velocity *= 0.3f;
                else
                    NPC.velocity *= 0.6f;

                if (Math.Abs(NPC.Center.X - Target.Center.X) < DoubleSwipe_CurrentSwingReach * 0.3f)
                    NPC.velocity.X *= 0;


                //Swing sound when the swing starts
                if (CurrentSlashTimer <= attackSpeed && CurrentSlashTimer > 0)
                    SoundEngine.PlaySound(TridentSwing, NPC.Center);

                if (slashShinePosition != Vector2.Zero && Main.rand.NextBool(3))
                {
                    Dust blingDust = Dust.NewDustPerfect(slashShinePosition, DustID.GoldCoin, Vector2.Zero, Scale: Main.rand.NextFloat(0.95f, 1.5f));
                    blingDust.noGravity = true;
                    blingDust.scale *= 2f;
                }

                if (AttackTimer <= 0)
                {
                    //End the combo straight away on normal mode
                    if (DifficultyScale <= 1)
                        return;

                    AttackTimer = 1;
                    NPC.noTileCollide = true;
                    NPC.noGravity = true;

                    SubState = ActionState.DoubleSwipe_CloseDistance2;
                    oldPosition = NPC.Center;
                    movementTarget.X = Target.Center.X;
                    //Recorrect vertically up to 50 pixels
                    movementTarget.Y += MathHelper.Clamp(Target.Center.Y - NPC.Center.Y, -400f, 500f);

                    NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign();
                    NPC.velocity.Y = 0;

                    //TODO check if target still even alive. Have a method thats like "checkdespawn" or something.
                }
            }

            else if (SubState == ActionState.DoubleSwipe_CloseDistance2)
            {
                //Attack has more telegraph time in rev , so we stretch the timer a bit
                float animSpeed = DifficultyScale <= 3 ? 1 / (60f * 0.3f) : 1 / (60f * 0.2f);
                Vector2 teleportDirection = Vector2.UnitX * (movementTarget.X - oldPosition.X).NonZeroSign();
                Vector2 displacementAfterTeleportation = teleportDirection * DoubleSwipe_SwingReach(2) * 1.4f - Vector2.UnitX * Target.velocity.X * 4f;

                bool movementDone;

                //The second dash has no bouncy back telegraph this time. Prepare thyselfff!
                movementDone = DashToPos(movementTarget, oldPosition, animSpeed, 0f, 0f, displacementAfterTeleportation, 0.15f);

                //Slow down nautilus if he's about to dash too close to the player. We don't want him to dash so fast he ends up behind the player
                if (Math.Abs(NPC.velocity.X + NPC.Center.X - (Target.Center.X + Target.velocity.X * 2f)) < 0.34f * DoubleSwipe_SwingReach(2))
                    NPC.velocity.X *= 0.35f;

                if (movementDone)
                {
                    AttackTimer = 1;
                    SubState = ActionState.DoubleSwipe_SecondSwipe;
                    ExtraMemory = 0f;
                }
            }

            else if (SubState == ActionState.DoubleSwipe_SecondSwipe)
            {
                NPC.noTileCollide = false;
                float attackSpeed = 1 / (60f * 0.56f);
                AttackTimer -= attackSpeed;

                //The dash kinda seems to shoot downwards??
                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

                float speed = NPC.velocity.Length();
                float distanceToTarget = NPC.Distance(Target.Center);

                //Slow down (slow down even more if in range anyways
                if (distanceToTarget < DoubleSwipe_CurrentSwingReach * 0.7f)
                    NPC.velocity *= 0.46f;
                else
                    NPC.velocity *= 0.76f;

                if (Math.Abs(NPC.Center.X - Target.Center.X) < DoubleSwipe_CurrentSwingReach * 0.4f)
                    NPC.velocity.X *= 0;

                if (slashShinePosition != Vector2.Zero && Main.rand.NextBool(3))
                {
                    Dust blingDust = Dust.NewDustPerfect(slashShinePosition, DustID.GoldCoin, Vector2.Zero, Scale: Main.rand.NextFloat(0.95f, 1.5f));
                    blingDust.noGravity = true;
                    blingDust.scale *= 2f;
                }

                //Swing sound when the swing starts
                if (CurrentSlashTimer <= attackSpeed && CurrentSlashTimer > 0)
                    SoundEngine.PlaySound(TridentSwing, NPC.Center);
            }
        }
        #endregion

        #region Jump slam
        public void JumpSlamAttack()
        {
            //Initialize the attack
            if (SubState == ActionState.JumpSlam)
            {
                //Callout post
                CuteSpeechSystem.Speak(NPC.Center, 2, RegularSpeech, RegularTone);


                NPC.TargetClosest(true);

                if (!NPC.HasValidTarget || NPC.Distance(Target.Center) > 1200)
                {
                    AIState = ActionState.SlowWalk;
                    return;
                }

                Stamina -= 1f;
                SubState = ActionState.JumpSlam_Jumping;
                NPC.noTileCollide = true;
                NPC.collideX = false;
                NPC.collideY = false;
                NPC.noGravity = true;
                NPC.velocity = Vector2.Zero;

                oldPosition = NPC.Center;

                //Decide to jump to a point between himself and the player, 200 to 300 tiles above the midway height between his and the target's 
                //If said midway point is still not high enough , pick a closer distance
                float baselineJumpHeight = MathHelper.Lerp(NPC.Center.Y, Target.Center.Y, 0.5f);
                if ((NPC.Center.Y - Target.Center.Y) / 2f > 200)
                    baselineJumpHeight = Target.Center.Y + 100;

                movementTarget = new Vector2(Target.Center.X, baselineJumpHeight);
                movementTarget.X += (NPC.Center.X - Target.Center.X) * (0.05f + 0.3f * Main.rand.NextFloat());
                movementTarget.Y -= Main.rand.NextFloat(200f, 300f);
            }

            //Jump in the air until the target is reached
            if (SubState == ActionState.JumpSlam_Jumping)
            {
                float jumpSpeed = DifficultyScale >= 3 ? 0.06f : DifficultyScale >= 2 ? Utils.GetLerpValue(1f, 0.5f, AttackTimer, true) * 0.02f + 0.03f : 0.03f;

                if (JumpToPos(movementTarget, oldPosition, jumpSpeed, 1f))
                {
                    NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.UnitX * (Target.Center.X - NPC.Center.X) * 0.05f, 0.5f);
                    SubState = ActionState.JumpSlam_Holding;
                    AttackTimer = 1f;

                    //Retarget again
                    TargetBestGroundslamTarget();
                }
            }

            //Hold and aim at the target
            else if (SubState == ActionState.JumpSlam_Holding)
            {
                NPC.velocity *= 0.9f;
                AttackTimer -= 1f / 26f;

                //Attack timer at 0.2f = lock rotation
                //Attack timer at zero = slam

                if (AttackTimer > 0.2f)
                {
                    float rotationStrenght = 1 - (AttackTimer - 0.2f) / 0.8f;
                    NPC.rotation = Utils.AngleLerp(NPC.rotation, NPC.Center.AngleTo(Target.Center + Target.velocity * 10f) - MathHelper.PiOver2, 0.05f + 0.3f * rotationStrenght);
                    NPC.rotation = Math.Clamp(NPC.rotation, -MathHelper.PiOver4, MathHelper.PiOver4);
                }

                else
                {
                    //Dive
                    if (AttackTimer <= 0)
                    {
                        SubState = ActionState.JumpSlam_Diving;
                        NPC.velocity = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 17f;
                        AttackTimer = 1;

                        ExtraMemory = 0f;
                    }
                }
            }

            else if (SubState == ActionState.JumpSlam_Diving)
            {
                //Collid with the floor again if not stuck, or if below the point you started the jump
                if (!Collision.SolidCollision(NPC.position, NPC.width, NPC.height, false) || NPC.Center.Y > oldPosition.Y)
                    ExtraMemory = 1f;

                bool canFallThroughPlatforms = CanFallThroughPlatforms().Value;
                bool hasCollided = (ExtraMemory == 1f && Collision.SolidCollision(NPC.position + NPC.velocity, NPC.width, NPC.height, !canFallThroughPlatforms)); //|| Math.Abs(collisionDifference.X) > 0f || Math.Abs(collisionDifference.Y) > 0f;

                if (!Main.rand.NextBool(3))
                {
                    Dust zust = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(9f, 9f), 43, -NPC.velocity * Main.rand.NextFloat(0.05f, 0.3f), 200, Color.Turquoise, Main.rand.NextFloat(1f, 1.5f));
                    zust.noGravity = true;
                }

                if (hasCollided)
                {
                    //Re-enable tile collision just in case
                    NPC.noTileCollide = false;
                    NPC.velocity.Y = 0;

                    if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height, !canFallThroughPlatforms))
                        NPC.velocity.Y -= 5;

                    if (!Main.dedServ && Main.LocalPlayer.Distance(NPC.Center) < 500)
                        CameraManager.Shake += 7f;

                    //Shockwave
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient && DifficultyScale > 0)
                    {
                        float shockwaveWidth = DifficultyScale >= 2 ? 24 : 16;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitX * NPC.velocity.X + Vector2.UnitY * 22f, Vector2.UnitY * 5f, ModContent.ProjectileType<GroundStomp>(), JumpSlam_SlamDamage / 2, 1f, Main.myPlayer, 0, shockwaveWidth);

                    }
                    SubState = ActionState.JumpSlam_Recovery;
                    AttackTimer = 1;
                    NPC.velocity.X *= 0.38f;
                }

                AttackTimer -= 1 / 30f;

                if (AttackTimer > 0.7f)
                    NPC.velocity *= 1.02f;

                if (AttackTimer < 0.2f)
                {
                    NPC.velocity *= 0.8f;
                    NPC.velocity.X += 0.2f;
                }

                if (AttackTimer <= 0)
                    NPC.noGravity = false;
            }

            if (SubState == ActionState.JumpSlam_Recovery)
            {
                NPC.noTileCollide = false;
                NPC.noGravity = false;
                NPC.velocity *= 0.9f;
                NPC.rotation = 0f;
                AttackTimer -= 1 / 30f;
            }
        }

        public void TargetBestGroundslamTarget()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                NPC.target = 0;
                return;
            }

            float bestScore = float.MaxValue;
            int bestTarget = 0;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead && !Main.player[i].ghost)
                {
                    float score = MathHelper.Max(Vector2.Dot(Vector2.UnitY, NPC.DirectionTo(Main.player[i].Center)), 0) * -400f + NPC.Distance(Main.player[i].Center);

                    if (score < bestScore)
                    {
                        bestTarget = i;
                        bestScore = score;
                    }
                }
            }

            NPC.target = bestTarget;

            if (NPC.target != NPC.oldTarget)
                NPC.netUpdate = true;
        }
        #endregion

        #region Cyclone dash
        public void StartSpinDash()
        {
            NPC.velocity = Vector2.Zero;
            SubState = ActionState.TridentSpin_Windup;

            NPC.direction = NPC.DirectionTo(Target.Center).X.NonZeroSign();

            //Additionally, teleport up to 120 blocks above or below nautie's current Y position to match the players, if there is space 
            if (Math.Abs(NPC.Center.Y - Target.Center.Y) > 50)
            {
                float displacement = Math.Clamp(Target.Bottom.Y - NPC.Bottom.Y, -350, 350) - 4f;
                NPC.position.Y += displacement;
                //Dust
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 220);
                    dust.noGravity = true;
                    dust.velocity.Y = MathHelper.Clamp(-displacement * 0.2f, -6f, 6f);
                }
            }

            SoundEngine.PlaySound(CycloneStart, NPC.Center);
        }

        public void TridentSpin()
        {
            if (SubState == ActionState.TridentSpin)
            {
                NPC.TargetClosest(true);
                if (!NPC.HasValidTarget)
                {
                    AIState = ActionState.SlowWalk;
                    return;
                }

                Stamina -= 1f;

                if (Target.velocity.Y != 0)
                    SubState = ActionState.TridentSpin_WaitingAtTheStartOfTheAttackForThePlayerToHitTheFloor;

                else
                    StartSpinDash();
            }

            if (SubState == ActionState.TridentSpin_WaitingAtTheStartOfTheAttackForThePlayerToHitTheFloor)
            {
                ManageTridentReapparition();
                AttackTimer -= 1 / (60f * 0.25f);

                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
                NPC.rotation = 0f;

                NPC.direction = NPC.DirectionTo(Target.Center).X.NonZeroSign();
                NPC.velocity.X *= 0.2f;

                if (Collision.SolidCollision(NPC.BottomLeft, NPC.width, 2) && NPC.velocity.Y >= 0 && NPC.Bottom.Y >= Target.Bottom.Y)
                    NPC.velocity.Y = 0;

                if (Target.velocity.Y == 0 || AttackTimer <= 0)
                {
                    StartSpinDash();
                    AttackTimer = 1f;
                }
            }

            if (SubState == ActionState.TridentSpin_Windup)
            {
                float attackSpeed = 1 / (60f * 0.8f);
                AttackTimer -= attackSpeed;

                //Slow fall
                NPC.velocity.Y = Math.Clamp(NPC.velocity.Y, 0f, 0.1f * Math.Max(0f, AttackTimer));

                if (AttackTimer <= 0)
                {
                    //Target the player and then a few more..
                    int extraDashDistanceInTiles = 6;
                    Vector2 dashUnit = Vector2.UnitX * NPC.direction * 8f;
                    Vector2 extraDash = Vector2.Zero;
                    movementTarget = new Vector2(Target.position.X, NPC.position.Y);


                    //If the ideal movement target is clear, teleport there simply AND move forward a few extra tiles if able to
                    bool targetClear = !Collision.SolidCollision(movementTarget, Target.width, NPC.height, false) || !Collision.SolidCollision(movementTarget - Vector2.UnitY * 16f, Target.width, NPC.height, false);
                    if (targetClear)
                    {
                        for (int i = 1; i < extraDashDistanceInTiles * 2; i++)
                        {
                            if (Collision.CanHitLine(movementTarget, NPC.width, Target.height, movementTarget + dashUnit * i, NPC.width, Target.height))
                            {
                                extraDash += dashUnit;
                            }
                            else
                                break;
                        }
                        movementTarget += extraDash;
                    }

                    //If going at the X coordinates of the player but at the Y coordinates of nautilus lands him in a wall,
                    // -For example, if nautilus is at the bottom of a hill and the player is ontop of it, which would land nautie inside the hill
                    //Nautie can only travel up until he meets a wall
                    else
                    {
                        bool alreadyInAWall = Collision.SolidCollision(NPC.position, NPC.width, NPC.height, false);
                        int travelSteps = (int)Math.Abs(NPC.position.X - movementTarget.X) / 8;

                        Vector2 dashDistance = Vector2.Zero;
                        int stepUpCooldown = 0;

                        for (int i = 0; i < travelSteps; i++)
                        {
                            if (!alreadyInAWall && Collision.SolidCollision(NPC.position + dashDistance + dashUnit, NPC.width, NPC.height, false))
                            {
                                //If we can't move up anyways, stop calculating
                                if (stepUpCooldown > 0)
                                    break;

                                //Else, we check if theres free ground Just above nautie and if it is, he can slightly retarget up and down
                                if (Collision.SolidCollision(NPC.position - Vector2.UnitY * 9f + dashDistance + dashUnit, NPC.width, NPC.height, false))
                                {
                                    NPC.position -= Vector2.UnitY * 9f;
                                }

                                else if (Collision.SolidCollision(NPC.position - Vector2.UnitY * 17f + dashDistance + dashUnit, NPC.width, NPC.height, false))
                                {
                                    stepUpCooldown = 6;
                                    NPC.position -= Vector2.UnitY * 9f;
                                }
                            }

                            else if (alreadyInAWall && !Collision.SolidCollision(NPC.position + dashDistance + dashUnit, NPC.width, NPC.height, false))
                                alreadyInAWall = false;

                            dashDistance += dashUnit;
                            stepUpCooldown--;
                        }

                        movementTarget = NPC.position + dashDistance;
                    }

                    oldPosition = NPC.position;
                    SoundEngine.PlaySound(CycloneCharge, NPC.Center);

                    SubState = ActionState.TridentSpin_DashStart;
                    AttackTimer = 1;
                }
            }

            else if (SubState == ActionState.TridentSpin_DashStart)
            {
                NPC.noGravity = true;
                NPC.noTileCollide = true;

                AttackTimer -= 0.1f;

                if (Math.Abs(NPC.position.X - movementTarget.X) < 100)
                    AttackTimer = 0;

                NPC.velocity.X += NPC.direction;
                NPC.velocity.X *= 1.06f;

                if (AttackTimer <= 0)
                {
                    AttackTimer = 1;
                    SubState = ActionState.TridentSpin_Recovery;

                    float dashHitboxHeight = NPC.height + 40;
                    CycloneDashVisuals(dashHitboxHeight);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), movementTarget + Vector2.UnitY * NPC.height / 2 - Vector2.UnitX * (movementTarget - NPC.position).X / 2, Vector2.Zero, ModContent.ProjectileType<NautilusDashSlice>(), TridentSpin_DashDamage / 2, 2, Main.myPlayer, (movementTarget - NPC.position).X, dashHitboxHeight);
                    }

                    cycloneDashOrigin = NPC.Center;
                    NPC.position = movementTarget;
                    NPC.velocity = Vector2.UnitX * 10f * NPC.direction;
                    NPC.noTileCollide = false;
                    NPC.noGravity = false;

                    if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(movementTarget) < 500f)
                        CameraManager.Shake += 5f;
                }
            }

            else if (SubState == ActionState.TridentSpin_Recovery)
            {
                NPC.noGravity = false;
                NPC.noTileCollide = false;

                float recoverySpeed = 1 / (60f * 0.4f);
                AttackTimer -= recoverySpeed;

                NPC.velocity *= 0.95f;

                if (AttackTimer > 0.5f && !Main.rand.NextBool(3))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 dustPosition = NPC.Center + Vector2.UnitY * (Main.rand.NextBool() ? 1 : -1) * (float)Math.Pow(Main.rand.NextFloat(), 0.3 * 0.3f) * (NPC.height / 2 + 20) * (float)Math.Pow(AttackTimer, 0.5f);

                        Dust zust = Dust.NewDustPerfect(dustPosition, 43, Vector2.Zero, 100, Color.Gold, Main.rand.NextFloat(0.5f, 1f));
                        zust.noGravity = true;
                        zust.velocity = -Vector2.UnitX * 4f * NPC.direction * AttackTimer;

                        if (Main.rand.NextBool(4))
                        {
                            zust.scale *= 2f;
                            zust.velocity.X *= -1;
                        }
                    }
                }

                if (AttackTimer <= 0)
                {
                    AIState = ActionState.TridentThrow;
                    AttackTimer = 1;
                }
            }
        }

        public void CycloneDashVisuals(float dashHitboxHeight)
        {
            for (int i = 0; i < 40; i++)
            {
                Vector2 start = movementTarget with { X = NPC.Center.X } + Vector2.UnitX * NPC.spriteDirection * 46f;
                Vector2 end = movementTarget + Vector2.UnitY * NPC.height / 2;

                float howCloseToTheEnd = (float)Math.Pow(Main.rand.NextFloat(0.3f, 1f), 0.7f);
                Vector2 dustPosition = Vector2.Lerp(start, end, howCloseToTheEnd);

                dustPosition += Vector2.UnitY * (Main.rand.NextBool() ? 1 : -1) * (float)Math.Pow(Main.rand.NextFloat(), 0.1f + howCloseToTheEnd * 0.3f) * dashHitboxHeight / 2;

                Dust zust = Dust.NewDustPerfect(dustPosition, 43, Vector2.Zero, 0, Color.Gold, Main.rand.NextFloat(0.5f, 1f));
                zust.noGravity = true;
                zust.velocity = -Vector2.UnitX * 4f * NPC.direction;
                zust.noLightEmittence = true;

                if (Main.rand.NextBool(4))
                {
                    zust.scale *= 2f;
                    zust.velocity.X *= -1;
                }
            }

            //Kick up dust
            for (int i = 0; i < (int)Math.Abs(NPC.Center.X - movementTarget.X) / 16; i++)
            {
                int tileX = (int)NPC.Center.X / 16 + i * (int)(NPC.Center.X - movementTarget.X).NonZeroSign() * -1;
                int j = (int)NPC.Bottom.Y / 16;

                Tile stompedTile = Framing.GetTileSafely(tileX, j);
                if (!stompedTile.HasUnactuatedTile || !(Main.tileSolid[stompedTile.TileType] || Main.tileSolidTop[stompedTile.TileType]))
                    continue;

                Tile stompedTileAbove = Framing.GetTileSafely(tileX, j - 1);
                if (stompedTileAbove.HasUnactuatedTile && (Main.tileSolid[stompedTileAbove.TileType] || Main.tileSolidTop[stompedTileAbove.TileType]))
                    continue;

                int dustCount = WorldGen.KillTile_GetTileDustAmount(fail: true, stompedTile, tileX, j);

                float progression = i / (float)(Math.Abs(NPC.Center.X - movementTarget.X) / 16f);
                dustCount = (int)(dustCount * (float)Math.Pow(progression, 0.4f));

                for (int k = 0; k < dustCount / 2; k++)
                {
                    Dust tileBreakDust = Main.dust[WorldGen.KillTile_MakeTileDust(tileX, j, stompedTile)];
                    tileBreakDust.velocity.Y -= 3f;
                    tileBreakDust.velocity.Y *= Main.rand.NextFloat();
                    tileBreakDust.velocity.Y *= 0.75f;
                    tileBreakDust.velocity.X += -NPC.spriteDirection * 4f;
                }
            }
        }
        #endregion
    }
}
