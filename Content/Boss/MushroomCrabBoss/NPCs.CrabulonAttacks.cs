using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Particles;
using Terraria.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public partial class Crabulon : ModNPC, ICustomDeathMessages, IDrawOverTileMask
    {
        #region Attack selection
        public void SelectNextAttack()
        {
            //Transition from attack to idle
            if (AIState != ActionState.Chasing)
            {
                SubState = ActionState.Chasing;
                NPC.dontTakeDamage = false; //Failsafe

                float timeUntilNextAttack = 3f - DifficultyScale * 0.35f;
                timeUntilNextAttack *= 0.7f + 0.3f * NPC.life / (float)NPC.lifeMax;

                float minimumAttackTime = Main.getGoodWorld ? 0.05f : 1f;
                timeUntilNextAttack = Math.Max(timeUntilNextAttack, minimumAttackTime);
                AttackTimer = timeUntilNextAttack;
            }

            //Pick a new attack
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                WeightedRandom<ActionState> attackPool = new WeightedRandom<ActionState>(Main.rand);
                float distanceToTarget = NPC.Distance(target.Center);

                bool previouslyMelee = PreviousState == ActionState.Slam || PreviousState == ActionState.Snip || PreviousState == ActionState.Slingshot || (int)NPC.localAI[0] == (int)ActionState.SporeBomb_SpawnOnSelfCharge;

                int sporebudType = ModContent.NPCType<CrabulonSporeBomb>();
                int sporeMortarType = ModContent.NPCType<CrabulonThrownSporeBomb>();
                int sporeBombCount = Main.npc.Where(n => n.active && n.type == sporebudType || n.type == sporeMortarType && n.DistanceSQ(NPC.Center) < 3000 * 3000).Count();

                float playerAlignedWithCrab = Utils.GetLerpValue(100f, 30f, Math.Abs(target.Center.X - NPC.Center.X), true);

                //Lower chance of doubling up on melee attacks, otherwise prioritize if the player is close enough
                float meleeAttackMultiplier = previouslyMelee ? 0.7f : 1 + Utils.GetLerpValue(300f, 0f, distanceToTarget, true) * 0.5f;

                float sporeMinesMultiplier = (float)Math.Pow(Utils.GetLerpValue(4f, 1f, sporeBombCount, true), 1.3f) * 1.1f; //Spore mines are not incentivized to be spawned if crabulon already got enough
                attackPool.Add(ActionState.SporeMines, sporeMinesMultiplier * 0.7f + 0.3f * (Utils.GetLerpValue(300, 0, distanceToTarget, true)));
                
                if (playerAlignedWithCrab > 0 || sporeBombCount <= 3)
                    attackPool.Add(ActionState.SporeBomb, 1f);
                attackPool.Add(ActionState.HuskDrop, 0.8f);

                if (distanceToTarget > 300f || (playerAlignedWithCrab > 0f && FloorHeight - target.position.Y > 200f))
                    attackPool.Add(ActionState.Charge, 0.6f + Utils.GetLerpValue(120f, 600f, distanceToTarget, true) * 0.65f + playerAlignedWithCrab * 0.6f);

                //claw slam and claw snip have different activation ranges vertically & horizontally
                if (distanceToTarget < 350f && target.Bottom.Y - FloorHeight > -350f)
                    attackPool.Add(ActionState.Slam, 1.4f * meleeAttackMultiplier);

                if (distanceToTarget < 500f && target.Bottom.Y - FloorHeight > -250f)
                        attackPool.Add(ActionState.Snip, 1.4f * meleeAttackMultiplier);
            

                //Increased chances the further away the player is
                if (distanceToTarget > 350f)
                    attackPool.Add(ActionState.Slingshot, 0.6f + Utils.GetLerpValue(300f, 900f, distanceToTarget, true) * 1.3f);

                ActionState potentialNewState = ActionState.Chasing;
                //Reduce the probabily for it to repeat an attack
                for (int i = 0; i < attackPool.elements.Count; i++)
                {
                    if (attackPool.elements[i].Item1 == PreviousState)
                        attackPool.elements[i] = new Tuple<ActionState, double>(PreviousState, attackPool.elements[i].Item2 * 0.1f);
                }

                if (attackPool.elements.Count > 0)
                    potentialNewState = attackPool.Get();

                SubState = potentialNewState;
                PreviousState = potentialNewState;
                AttackTimer = 0;
                NPC.netUpdate = true;
            }
        }
        #endregion

        #region Movement
        public int CollisionBoxWidth => 60;
        public int CollisionBoxHeight => NPC.height + CollisionBoxYOffset;
        public int CollisionBoxYOffset => AIState == ActionState.Desperation ? 0 : AIState == ActionState.Dead ? (int)(-NPC.height / 5.2f * NPC.scale) : (int)(90 * NPC.scale);
        public float FloorHeight => NPC.Bottom.Y + CollisionBoxYOffset;

        public Vector2 FloorPosition => NPC.Bottom + Vector2.UnitY * CollisionBoxYOffset;

        public Vector2 CollisionBoxOrigin => NPC.Top - Vector2.UnitX * (float)(CollisionBoxWidth / 2);

        public Rectangle CollisionBox {
            get {
                Vector2 collisionBoxOrigin = NPC.Bottom - Vector2.UnitX * (float)(CollisionBoxWidth / 2) - Vector2.UnitY * CollisionBoxHeight;
                return new Rectangle((int)collisionBoxOrigin.X, (int)collisionBoxOrigin.Y, CollisionBoxWidth, CollisionBoxHeight);
            }
        }

        public float kineticForce;

        public bool IdleMotion()
        {
            //Decide what action to take
            if (SubState == ActionState.Chasing)
            {
                NPC.TargetClosest();
                float distanceToTarget = NPC.Distance(target.Center);

                //Just quickly chase the player if too far
                if (distanceToTarget >= 2100)
                    SubState = ActionState.Chasing_WallFastCrawl;

                else
                {
                    bool straightforwardPath = IsThereAStraightforwardPath(30, 444f, out bool falling);
                    if (!straightforwardPath)
                    {
                        // Simply falling down until we reach the ground, while trying to go towards the player
                        if (falling && NPC.Center.Y < target.Top.Y)
                            SubState = ActionState.Chasing_FallingDown;
                        else
                            SubState = ActionState.Chasing_WallCrawl;
                    }
                    else
                        SubState = ActionState.Chasing_Scuttle;
                }
            }

            float attackTimerMultiplier = 1f;

            //Crawling on the walls straight to the player
            if (SubState == ActionState.Chasing_WallFastCrawl || SubState == ActionState.Chasing_WallCrawl)
            {
                attackTimerMultiplier = CrawlingMovement(true, SubState == ActionState.Chasing_WallFastCrawl ? 9f : 4f, SubState == ActionState.Chasing_WallFastCrawl ? 10f : 20f);

                float distanceToTarget = NPC.Distance(target.Center);
                //Slown down if close enough
                if (distanceToTarget < 500f && SubState != ActionState.Chasing_WallCrawl)
                    SubState = ActionState.Chasing_WallCrawl;

                float pathCheckFrequency = AttackTimer % 0.5f;
                if (pathCheckFrequency > 0 && pathCheckFrequency - attackTimerMultiplier / 60f <= 0)
                {
                    if (IsThereAStraightforwardPath(30, 444f, out bool falling))
                        SubState = ActionState.Chasing_Scuttle;
                }
            }

            else if (SubState == ActionState.Chasing_Scuttle || SubState == ActionState.Chasing_FallingDown)
            {
                //TODO move slower at the start of the fight
                attackTimerMultiplier = ChasingMovement();

                float pathCheckFrequency = AttackTimer % 1f;
                if (pathCheckFrequency > 0 && pathCheckFrequency - attackTimerMultiplier / 60f <= 0)
                {
                    if (!IsThereAStraightforwardPath(30, 444f, out bool falling))
                        SubState = ActionState.Chasing_WallCrawl;
                }
            }

            //Above normal, charge up attacks faster if the player is too close; or too far
            if (DifficultyScale > 0)
            {
                float distanceToPlayer = NPC.Distance(target.Center);
                attackTimerMultiplier *= 1f + ((float)Math.Pow(Utils.GetLerpValue(300f, 100f, distanceToPlayer, true), 1.5f) + Utils.GetLerpValue(650f, 900f, distanceToPlayer, true)) * 1.5f;

            }

            lookingSideways = false;
            AttackTimer -= attackTimerMultiplier / 60f;
            return AttackTimer <= 0f;
        }

        public bool IsThereAStraightforwardPath(int freeFallDistanceCheck, float extraLeeway, out bool freeFall)
        {
            freeFall = !GroundCheck(NPC.Top.ToTileCoordinates(), CollisionBoxWidth / 32, freeFallDistanceCheck, out Point ground);

            if (freeFall)
                return false;

            int gravityState = GravityState(out _, out _, out _);

            //If crabulon is currently inside tiles and below the player, we go up to find a valid tile
            if (gravityState <= 0)
            {
                float verticalDistanceToPlayer = Math.Max(64, FloorHeight - (target.Bottom.Y - 4));
                bool foundGroundAbove = false;

                for (int i = 0; i < verticalDistanceToPlayer / 16f; i++)
                {
                    Tile t = Main.tile[ground + new Point(0, -i)];
                    if (!t.HasUnactuatedTile || t.IsHalfBlock || !Main.tileSolid[t.TileType] || TileID.Sets.Platforms[t.TileType])
                    {
                        ground += new Point(0, -i);
                        foundGroundAbove = true;
                        break;
                    }
                }

                if (!foundGroundAbove)
                    return false;
            }


            //Make sure the ground we found is navigable
            int maxIterations = 50;
            ground -= new Point(0, 1);
            ground = AStarPathfinding.OffsetUntilNavigable(ground, new Point(0, 1), CrabulonCrawlPathfind, ref maxIterations);
            if (maxIterations < 0)
                return false;

            maxIterations = 34;
            //Try to find navigable ground below the player
            Point pathfindingEnd = AStarPathfinding.OffsetUntilNavigable(target.Center.ToTileCoordinates(), new Point(0, 1), CrabulonCrawlPathfind, ref maxIterations);

            //If theres no floor under the player then give up
            if (maxIterations < 0)
                return false;

            //If we managed to find a good starting point and a good ending point, we then proceed to simulate pathfinding between the two.
            //If we can find a path to the target whose lenght is shorter than a straight line from start to end + some varying leeway
            //Then we know there exists an "easy straightforward path", but if it differs too much, we simply just wallcrawl
            return AStarPathfinding.IsThereAPath(ground, pathfindingEnd, CrabulonStride, CrabulonCrawlPathfind, extraLeeway);
        }

        public bool GroundCheck(Point origin, int checkHalfWidth, int maxDistance, out Point groundPos)
        {
            for (int i = 0; i < maxDistance; i++)
            {
                for (int y = 0; y <= checkHalfWidth; y = (y < 1 ? -y + 1 : -y))
                {
                    Tile t = Main.tile[origin + new Point(y, i)];
                    if (!t.HasUnactuatedTile)
                        continue;

                    if (Main.tileSolid[t.TileType] || (Main.tileSolidTop[t.TileType] && t.TileFrameY == 0))
                    {
                        groundPos = origin + new Point(y, i);
                        return true;
                    }
                }
            }

            groundPos = Point.Zero;
            return false;
        }

        public int GravityState(out bool touchingFloor, out bool insideSolids, out bool acceptTopSurfaces)
        {
            Rectangle targetHitbox = NPC.GetTargetData().Hitbox;
            acceptTopSurfaces = FloorHeight >= (float)targetHitbox.Bottom - 6; //Accept platforms if you aren't above the player's 

            insideSolids = SolidCollisionFix(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight, acceptTopSurfaces);
            bool upperBodyInSolids = SolidCollisionFix(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight - 4, acceptTopSurfaces);
            touchingFloor = insideSolids && !upperBodyInSolids;

            //Dust.QuickBox(CollisionBoxOrigin, CollisionBoxOrigin + new Vector2(CollisionBoxWidth, CollisionBoxHeight), 30, Color.Red, null);

            if (touchingFloor)
                return 0;
            //Rise up if we're below the player's floor height
            if (upperBodyInSolids && FloorHeight > target.Bottom.Y - 4 && AIState != ActionState.Desperation)
                return -1;
            return 1;
        }

        public void ContinueMoving(bool retarget = true, float speedMultiplier = 1f, bool tryToStopCrawling = false)
        {
            if (TopDownView)
            {
                CrawlingMovement(retarget, 4f * speedMultiplier, 20f);
                if (tryToStopCrawling && IsThereAStraightforwardPath(30, 444f, out bool falling))
                    TopDownView = false;
            }
            else
                ChasingMovement(retarget, speedMultiplier);
        }

        public float ChasingMovement(bool retarget = true, float walkMultiplier = 1f, bool onlyVerticalMovement = false, bool cannotClimbUp = false)
        {
            if (retarget)
                NPC.TargetClosest(true);

            TopDownView = false;

            NPC.noTileCollide = true;
            NPC.noGravity = true;
            GravityState(out bool onFloor, out bool insideSolids, out bool acceptTopSurfaces);
            float attackRechargeMultiplier = 1f;
            float riseSpeed = -0.4f;
            float minRiseSpeed = -8f;
            float gravity = 0.4f;
            float jumpVelocity = 8f;
            bool alwaysRise = false;
            float heightAbovePlayerToRiseTo = target.Bottom.Y - 4;

            if ((int)SubState >= (int)ActionState.SpawningUp && (int)SubState <= (int)ActionState.SpawningUp_Scream)
            {
                alwaysRise = true;
                heightAbovePlayerToRiseTo = target.Bottom.Y - 230;
            }

            if (SubState == ActionState.Desperation_CinematicWait)
                gravity = (float)Math.Pow(1 - AttackTimer, 0.7f) * 0.8f;
            else if (AIState == ActionState.Desperation)
                gravity *= 0.5f;
            else if (AIState == ActionState.Dead)
                gravity = (float)Math.Pow(Math.Min(AttackTimer, 1), 0.7f) * 0.8f;

            // Look at player

            float distanceToPlayer = Math.Abs(NPC.Center.X - target.Center.X);

            //Walk towards the player
            if (!onlyVerticalMovement)
            {
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
                float walkSpeed = (3f + Utils.GetLerpValue(300f, 600f, distanceToPlayer, true) * 4f) * Utils.GetLerpValue(0f, 60f, distanceToPlayer, true) * walkMultiplier;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, walkSpeed * NPC.direction, 1 / 20f);
            }

            //Fall down if above player
            if (distanceToPlayer < 380f && FloorHeight < target.Bottom.Y - 30 && AIState != ActionState.Desperation && !alwaysRise)
                NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + gravity, 0.001f, 16f);

            else if (onFloor)
                NPC.velocity.Y = 0f;

            else if (insideSolids) //Rise up if the whole body is in solid and below the player
            {
                if (FloorHeight > heightAbovePlayerToRiseTo && !cannotClimbUp)
                {
                    NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + riseSpeed, minRiseSpeed, 0f);
                    attackRechargeMultiplier *= 0.5f;
                }
                else
                {
                    NPC.velocity.Y = 0;
                }
            }
            //Fall down
            else
            {
                NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + gravity, -jumpVelocity, 16f);
                //Attacks recharge slower when in the air, especially when jumping
                attackRechargeMultiplier *= NPC.velocity.Y < 0 ? 0.3f : 0.7f;
            }

            if (NPC.velocity.Y <= 2)
            {
                if (NPC.velocity.Y <= 0 && kineticForce > 30f && Main.LocalPlayer.Distance(NPC.Center) < 2000f)
                {
                    float force = (Utils.GetLerpValue(30, 50, kineticForce, true) * 0.5f + 0.5f);
                    float visualForce = force * Utils.GetLerpValue(2000, 1500f, Main.LocalPlayer.Distance(NPC.Center), true);
                    CameraManager.Quake += 30f * visualForce;
                    CameraManager.AddCameraEffect(new DirectionalCameraTug(Vector2.UnitY * visualForce * 30f, 2f, 20));
                    SoundEngine.PlaySound(LightSlamSound with { Volume = visualForce }, NPC.Center);

                    kineticOffset = 40f;
                    kineticOffsetVelocity = force * 3f;

                    foreach (Player player in Main.player)
                    {
                        if (player.active && !player.dead && player.velocity.Y == 0 && player.Distance(NPC.Center) < 300f)
                        {
                            player.velocity.Y = -force * 7f;
                            player.velocity.X += (player.Center.X - NPC.Center.X).NonZeroSign() * force * 7f;
                        }
                    }
                }

                kineticForce = 0;
            }
            else
                kineticForce += 1f;

            return attackRechargeMultiplier;
        }

        public float CrawlingMovement(bool retarget, float speed, float accelerationSpeed)
        {
            float attackTimerMultiplier = 1f;
            NPC.TargetClosest(retarget);
            //NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(target.Center) * speed, 1 / accelerationSpeed);

            NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(target.Center) * speed, 1 / accelerationSpeed);

            float idealRotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;

            //initialize the rotation if we went into top down view mode
            if (!TopDownView)
                NPC.rotation = idealRotation;
            else
                NPC.rotation = NPC.rotation.AngleTowards(idealRotation, 0.06f + 0.2f * Utils.GetLerpValue(2f, 5f, NPC.velocity.Length(), true));


            TopDownView = true;
            //Attacks recharge faster when quickly chasing the player
            if (SubState == ActionState.Chasing_WallFastCrawl)
                attackTimerMultiplier = 1.1f;
            return attackTimerMultiplier;
        }


        public static readonly List<AStarNeighbour> CrabulonStride = AStarNeighbour.BigOmniStride(3, 4);

        public static bool CrabulonCrawlPathfind(Point p, Point? from, out bool universallyUnnavigable)
        {
            universallyUnnavigable = true;

            Tile t = Main.tile[p];
            bool solidTile = Main.tileSolid[t.TileType];
            bool platform = TileID.Sets.Platforms[t.TileType];

            //Can't navigate inside solid tiles
            if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
                return false;

            universallyUnnavigable = false;

            //Can navigate on half tiles and platforms just fine
            if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
                return true;

            for (int i = -1; i <= 1; i++)
                for (int j = 0; j <= 1; j++)
                {
                    //Only cardinal directions here
                    if (j * i != 0 || (j == 0 && i == 0))
                        continue;

                    //IF a neighboring tile is solid we can go on it
                    Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                        return true;
                }

            //Can fall straight down just fine
            if (from != null && p.X == from.Value.X && p.Y == from.Value.Y + 1)
                return true;

            return false;
        }
        #endregion

        #region Spore mines
        public bool SporeMineAttack()
        {
            if (SubState == ActionState.SporeMines)
            {
                NPC.TargetClosest();
                SubState = ActionState.SporeMines_Telegraph;
                AttackTimer = 1f;
                NPC.velocity.X *= 0.5f;
                SoundEngine.PlaySound(SporeMinefieldShakeSound, NPC.Center);
            }

            float walkMultiplier = 0.1f;
            float timeInterval = 0f;

            if (SubState == ActionState.SporeMines_Telegraph)
            {
                NPC.velocity.X *= 0.95f;
                timeInterval = 1 / (60f * 0.9f);
                AttackTimer -= timeInterval;
                visualRotationExtra = (float)Math.Sin(AttackTimer * MathHelper.TwoPi * 2f) * 0.2f;
                visualOffsetExtra = Vector2.UnitX * visualRotationExtra * 60f + Vector2.UnitY * (1 - AttackTimer) * 10f;

                if (Main.rand.NextBool(5))
                {
                    Vector2 dustRotation = -Vector2.UnitY.RotatedBy(visualRotation + Main.rand.NextFloatDirection() * MathHelper.PiOver2 * 0.8f);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 actualDustRotation = dustRotation.RotatedByRandom(0.1f);
                        Dust.NewDustPerfect(VisualCenter + actualDustRotation * Main.rand.NextFloat(42f, 50f) * NPC.scale, DustID.GlowingMushroom, actualDustRotation * Main.rand.NextFloat(7f));
                    }
                }

                if (Main.rand.NextFloat() > AttackTimer && Main.rand.NextBool(4))
                {
                    Vector2 spawnPosition = VisualCenter - (Vector2.UnitY * 7f + Main.rand.NextVector2Circular(35f, 16f)) * NPC.scale;

                    SporeGas smoke = new SporeGas(spawnPosition, spawnPosition.SafeDirectionFrom(VisualCenter) * Main.rand.NextFloat(0.3f, 3f), NPC.Center, 56f * NPC.scale, Main.rand.NextFloat(3.5f, 3.6f), 0.01f);
                    smoke.dustSpawnRate = 0.03f;
                    ParticleHandler.SpawnParticle(smoke);
                }

                //Spawn the things
                if (AttackTimer <= 0)
                {
                    SoundEngine.PlaySound(SporeMinefieldDeploySound, NPC.Center);
                    SpawnSporeMines((int)(3 + DifficultyScale * 0.5f));
                    SubState = ActionState.SporeMines_Recovery;
                    AttackTimer = 1;
                    kineticOffset = 8;
                    kineticOffsetVelocity = -4;
                }
            }

            else if (SubState == ActionState.SporeMines_Recovery)
            {
                visualRotationExtra = (float)Math.Sin(AttackTimer * MathHelper.TwoPi * 2f) * 0.2f * (float)Math.Pow(AttackTimer, 0.5f);
                visualOffsetExtra = Vector2.UnitX * visualRotationExtra * 60f * (AttackTimer);

                timeInterval = 1 / (60f * 0.5f);
                AttackTimer -= timeInterval;
                walkMultiplier = (1 - AttackTimer) * 0.9f + 0.1f;

                if (Arms != null)
                {
                    foreach (CrabulonArm arm in Arms)
                    {
                        arm.springinessOverride = 1f;
                        arm.accelerationOverride = 0.2f;
                    }
                }
            }


            if (Arms != null)
            {
                foreach (CrabulonArm arm in Arms)
                {
                    float lerp = SubState == ActionState.SporeMines_Telegraph ? (1 - AttackTimer) : AttackTimer;
                    arm.contractionOverride = MathHelper.Lerp(arm.IdealContraction, 0.6f, lerp);
                }
            }

            ContinueMoving(false, walkMultiplier, AttackTimer % 0.1f > 0f && AttackTimer % 0.1f - timeInterval <= 0f);
            return AttackTimer <= 0;
        }

        public void SpawnSporeMines(int mineCount)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            //Spawn the first mine inbetween crabulon and the player
            List<Vector2> spawnCandidates = new List<Vector2>();

            int iterations = 0;
            Vector2 direction = NPC.SafeDirectionTo(target.Center);
            int flipCount = (int)(mineCount / 2);

            while (spawnCandidates.Count < mineCount && iterations < 1000)
            {
                Vector2 offset = direction.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(400f, 2000f) + Main.rand.NextVector2Circular(500f, 500f);
                if (flipCount > 0 && Main.rand.NextBool(6))
                {
                    offset *= -1;
                    flipCount--;
                }

                Vector2 origin = NPC.Center + offset;

                ValidateSporeMinePosition(origin.ToSafeTileCoordinates(), spawnCandidates, 380f, 100f, 800f, 9, 160f, 1.1f);
                iterations++;
            }

            foreach (Vector2 minePosition in spawnCandidates)
                NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)minePosition.X, (int)minePosition.Y, ModContent.NPCType<CrabulonSporeBomb>());
        }

        public bool ValidateSporeMinePosition(Point potentialPosition, List<Vector2> usedPositions, float minDistanceToCrab, float minDistanceToPlayer, float maxDistanceToPlayer, int maxGroundDistance = 9, float minDistanceBetweenMines = 100f, float maxAstarDistanceMultiplier = 2f)
        {
            Tile tile = Main.tile[potentialPosition];
            Vector2 worldPos = potentialPosition.ToWorldCoordinates();
            minDistanceToCrab *= minDistanceToCrab;
            minDistanceToPlayer *= minDistanceToPlayer;
            maxDistanceToPlayer *= maxDistanceToPlayer;

            //Can't spawn too close to the player or crabulon
            if (worldPos.DistanceSQ(NPC.Center) < minDistanceToCrab || worldPos.DistanceSQ(target.Center) < minDistanceToPlayer)
                return false;

            //Can't spawn too far to the player
            if (worldPos.DistanceSQ(target.Center) > maxDistanceToPlayer)
                return false;

            //Can't spawn inside a tile
            if (tile.IsTileSolid())
                return false;

            //Check if there's floor below to avoid spawning them too high up into the ceiling
            bool notTooHighUp = false;
            for (int i = 1; i < maxGroundDistance; i++)
            {
                Tile t = Main.tile[potentialPosition.X, potentialPosition.Y + i];
                if (t.IsTileSolidGround())
                {
                    notTooHighUp = true;
                    //If there's floor RIGHT below, raise it up a tile
                    if (i == 1 && !Main.tile[potentialPosition.X, potentialPosition.Y - 1].IsTileSolid())
                        potentialPosition.Y -= 1;

                    break;
                }
            }
            if (!notTooHighUp)
                return false;

            //Check if not too close to the other positions
            minDistanceBetweenMines *= minDistanceBetweenMines;
            foreach (Vector2 p in usedPositions)
            {
                if (p.DistanceSQ(worldPos) < minDistanceBetweenMines)
                    return false;
            }

            //Check if reachable through a* pathfinding
            bool reachable = AStarPathfinding.IsThereAPath(potentialPosition, target.Center.ToSafeTileCoordinates(), AStarNeighbour.BasicCardinalOrdinal, AStarPathfinding.AirNavigable, worldPos.Distance(target.Center) * (maxAstarDistanceMultiplier - 1));
            if (!reachable)
                return false;

            usedPositions.Add(worldPos);
            return true;
        }
        #endregion

        #region Spore bomb mortar
        public bool SporeBombAttack()
        {
            if (SubState == ActionState.SporeBomb)
            {
                NPC.TargetClosest();
                SubState = ActionState.SporeBomb_ChargeBomb;
                AttackTimer = 1f;
                NPC.velocity.X *= 0.5f;
                SoundEngine.PlaySound(SporeMortarChargeSound, NPC.Center);

                if (Math.Abs(NPC.Center.X - target.Center.X) < 100f && target.Top.Y > NPC.Top.Y - 250)
                {
                    SubState = ActionState.SporeBomb_SpawnOnSelfCharge;
                    NPC.localAI[0] = (int)ActionState.SporeBomb_SpawnOnSelfCharge;
                }
            }

            float walkMultiplier = 0.1f;
            float targetDirection = target.velocity.X == 0 ? (target.Center.X - NPC.Center.X).NonZeroSign() : target.velocity.X.NonZeroSign();
            Vector2 targetPosition = target.Center + Vector2.UnitX * Math.Clamp(Math.Abs(target.velocity.X) * 170f, 240f, 360f) * targetDirection;

            flickerTickDownTimeOverride = 1.5f;

            if (SubState == ActionState.SporeBomb_ChargeBomb)
            {
                NPC.velocity.X *= 0.97f;
                AttackTimer -= 1 / (60f * 0.5f);

                Vector2 arcVel = GetArcVel(NPC.Top, targetPosition, 0.4f, 100f, 400f, heightAboveTarget: 60f);
                float arcRotation = (arcVel.ToRotation() + MathHelper.PiOver2) * 0.36f;

                visualRotationExtra = arcRotation * (1 - AttackTimer);

                visualOffsetExtra = -arcVel.SafeNormalize(Vector2.UnitY) * 60f * (1 - AttackTimer);
                visualOffsetExtra.Y *= 0.8f;

                if (Main.rand.NextBool(10))
                {
                    flickerOpacity = 1f;
                    flickerFrom = Main.rand.Next(1, 4);
                }
                    

                //Fire the bomb
                if (AttackTimer <= 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Top, arcVel, ModContent.ProjectileType<CrabulonSporeSeed>(), 0, 0, Main.myPlayer, targetPosition.X, target.whoAmI);

                    Vector2 top = VisualCenter + (visualRotation - MathHelper.PiOver2).ToRotationVector2() * 65f * NPC.scale;
                    for (int i = 0; i < 26; i++)
                    {
                        Vector2 dustPosition = top + Main.rand.NextVector2Circular(10f, 10f);
                        Dust d = Dust.NewDustPerfect(dustPosition, DustID.MushroomSpray, arcVel.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.4f, 2.4f));
                        d.noGravity = true;
                        d.scale = Main.rand.NextFloat(0.8f, 1.3f);
                    }


                    SoundEngine.PlaySound(SporeMortarFireSound, NPC.Center);
                    SubState = ActionState.SporeBomb_Recovery;
                    AttackTimer = 1;
                    kineticOffset = -14;
                    kineticOffsetVelocity = -4;
                }
            }


            if (SubState == ActionState.SporeBomb_SpawnOnSelfCharge)
            {
                NPC.velocity.X *= 0.97f;

                float chargeupTime = Math.Max(0.3f, 0.8f - 0.15f * DifficultyScale);
                AttackTimer -= 1 / (60f * chargeupTime);

                visualRotationExtra = Main.rand.NextFloat(-0.6f, 0.6f) * (1 - AttackTimer);
                visualOffsetExtra.Y = 40f * NPC.scale * (1 - AttackTimer);

                flickerBackground = 5;
                if (Main.rand.NextBool(10))
                {
                    flickerOpacity = 1f;
                    flickerFrom = Main.rand.Next(1, 4);
                }

                //Fire the bomb
                if (AttackTimer <= 0)
                {
                    ParticleHandler.SpawnParticle(new CircularScreamRoar(VisualCenter, Color.Blue, 0.8f));
                    SoundEngine.PlaySound(ShriekSound, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Top, -Vector2.UnitY * 5f, ModContent.ProjectileType<CrabulonSporeSeed>(), 0, 0, Main.myPlayer, targetPosition.X, target.whoAmI);


                    SubState = ActionState.SporeBomb_Recovery;
                    AttackTimer = 1;
                    kineticOffset = -14;
                    kineticOffsetVelocity = -4;
                }
            }


            if (SubState == ActionState.SporeBomb_Recovery)
            {
                visualRotationExtra = (float)Math.Sin(AttackTimer * MathHelper.TwoPi * 2f) * 0.2f * (float)Math.Pow(AttackTimer, 0.5f);
                visualOffsetExtra = Vector2.UnitX * visualRotationExtra * 60f * (AttackTimer);

                AttackTimer -= 1 / (60f * 0.5f);
                walkMultiplier = (1 - AttackTimer) * 0.9f + 0.1f;

                if (Arms != null)
                {
                    foreach (CrabulonArm arm in Arms)
                    {
                        arm.springinessOverride = 1f;
                        arm.accelerationOverride = 0.2f;
                    }
                }
            }

            if (Arms != null)
            {
                foreach (CrabulonArm arm in Arms)
                {
                    float lerp = SubState == ActionState.SporeBomb_ChargeBomb ? (1 - AttackTimer) : AttackTimer;
                    arm.contractionOverride = MathHelper.Lerp(arm.IdealContraction, 0.6f, lerp);
                }
            }

            ChasingMovement(false, walkMultiplier);
            return AttackTimer <= 0;
        }
        #endregion

        #region Husk Drop
        public bool HuskDrop()
        {
            if (SubState == ActionState.HuskDrop)
            {
                SubState = ActionState.HuskDrop_VineAttach;
                AttackTimer = 1f;
                NPC.TargetClosest();

                if (!Main.dedServ)
                    Ropes.AddSkewer();
            }

            //Recoil on crab's body when skewered
            if (!Main.dedServ && Ropes.stabTimer > 0f)
            {
                visualRotationExtra = Ropes.stabTimer / 15f * 0.24f * Ropes.stabDirection;
                visualOffsetExtra = Ropes.stabTimer / 15f * Main.rand.NextVector2Circular(1f, 1f) * 4f;

                Vector2 stabDir = Ropes.attachPointOffsets[Ropes.cableCount - 1].DirectionFrom(Ropes.attachPointOffsets[Ropes.cableCount - 1] * 1.4f - Vector2.UnitY * 1400f);
                visualOffsetExtra += stabDir * 10f * Ropes.stabTimer / 15f;
            }

            if (SubState == ActionState.HuskDrop_VineAttach)
            {
                float telegraphDuration = 1.5f - 0.3f * DifficultyScale;
                telegraphDuration = Math.Max(telegraphDuration, 0.1f);
                AttackTimer -= 1 / (60f * telegraphDuration);

                //Continue moving while still checking 10 times for if we can just go walk normally
                ContinueMoving(true, AttackTimer, AttackTimer % 0.1f > 0 && AttackTimer % 0.1f - 1 / (60f * telegraphDuration) <= 0);

                //Slow to a halt
                NPC.velocity.X *= 0.9f + 0.1f * AttackTimer;
                if (AttackTimer < 0.2f)
                    NPC.velocity.X = 0;

                //Add skewers faster and faster
                if (!Main.dedServ && Ropes.cableCount < Math.Pow(1 - AttackTimer, 2f) * 5f)
                    Ropes.AddSkewer();

                if (AttackTimer <= 0f)
                {
                    //Make the limbs go limp
                    if (!Main.dedServ)
                    {
                        foreach (CrabulonLeg leg in Legs)
                            leg.GoLimp();
                        foreach (CrabulonArm arm in Arms)
                            arm.GoLimp();
                    }

                    TopDownView = false;
                    AttackTimer = 1f;
                    SubState = ActionState.HuskDrop_ReelUp;
                }
            }

            else if (SubState == ActionState.HuskDrop_ReelUp)
            {
                //Slowly rise up. Nothing major
                NPC.velocity.X = 0f;
                NPC.velocity.Y -= 0.2f;

                float attackTime = 1f - DifficultyScale * 0.34f;
                attackTime = Math.Max(attackTime, 0.1f);
                AttackTimer -= 1 / (60f * attackTime);

                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1f;
                    SubState = ActionState.HuskDrop_Chase;
                }
            }

            else if (SubState == ActionState.HuskDrop_Chase)
            {
                float targetX = target.Center.X + target.velocity.X * 12.3f;
                float goalX = MathHelper.Lerp(NPC.Center.X, targetX, (1 - AttackTimer) * 0.2f + 0.04f);
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, goalX - NPC.Center.X, 0.1f + (float)Math.Pow(1 - AttackTimer, 3f));

                float targetHeight = target.Center.Y - 350f + (float)Math.Sin(AttackTimer * MathHelper.TwoPi) * 30f;
                NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, MathHelper.Lerp(NPC.Center.Y, targetHeight, 0.03f + 0.1f * (float)Math.Pow(1 - AttackTimer, 3f)) - NPC.Center.Y, 0.12f);

                if (!Main.dedServ)
                    Ropes.position = Vector2.Lerp(Ropes.position, new Vector2(targetX, targetHeight) - Vector2.UnitY * 1600f, 0.05f);

                float step = 1 / (60f * 2f);
                AttackTimer -= step;

                int skullCount = 5;
                float interval = (AttackTimer % (1 / (float)skullCount));
                if (DifficultyScale >= 2f && Main.netMode != NetmodeID.MultiplayerClient && interval > 0 && interval - step <= 0 && AttackTimer > 0.2f)
                {
                    Vector2 projectilePosition = NPC.Center;
                    int iterations = 0;
                    while (iterations < 35)
                    {
                        Tile t = Main.tile[projectilePosition.ToTileCoordinates()];
                        if (t.IsTileSolid())
                            break;

                        projectilePosition.Y -= 16;
                        iterations++;
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromAI(), projectilePosition, Vector2.UnitY * 0.3f, ModContent.ProjectileType<SporedSkeleton>(), (int)(HuskDrop_FallingSkullDamage * DamageMultiplier), 1f, Main.myPlayer, target.Center.Y);
                }

                if (AttackTimer <= 0f)
                {
                    //This little thing is needed to make the ropes unextended again
                    if (!Main.dedServ)
                    {
                        Ropes.position.Y += 400;
                        Ropes.UpdatePositions();
                        Ropes.CutSkewers();
                        Ropes.SmoothenCurvesAgain(1400f / 1600f);
                    }

                    SoundEngine.PlaySound(GrappleDetachSound, NPC.Center);

                    AttackTimer = 1f;
                    SubState = ActionState.HuskDrop_Drop;
                    NPC.velocity.Y = -4;
                    NPC.velocity.X = 0;
                    ExtraMemory = -1f;
                    oldPosition = target.Center;

                    movementTarget = new Vector2(NPC.Center.X, target.Center.Y);
                    while (movementTarget.Y > NPC.Center.Y)
                    {
                        Dust.QuickDust(movementTarget.ToTileCoordinates(), Color.Red);
                        if (Main.tile[movementTarget.ToTileCoordinates()].IsTileSolid())
                            movementTarget.Y -= 16;
                        else
                            break;
                    }

                    Vector2 fallPosition = movementTarget - NPC.Size * 0.5f;
                    bool canFallThroughPlatforms = movementTarget.Y + NPC.height * 0.5f < target.Bottom.Y;

                    //Crawl down if there is immediate collision
                    while (SolidCollisionFix(fallPosition, NPC.width, NPC.height, !canFallThroughPlatforms))
                    {
                        movementTarget.Y += 16;
                        fallPosition = movementTarget - NPC.Size * 0.5f;
                        canFallThroughPlatforms = movementTarget.Y + NPC.height * 0.5f < target.Bottom.Y;

                        if (movementTarget.Y > oldPosition.Y)
                        {
                            movementTarget.Y = oldPosition.Y;
                            break;
                        }
                    }
                }
            }

            else if (SubState == ActionState.HuskDrop_Drop)
            {
                if (ExtraMemory != 1f && NPC.velocity.Y > 0 && (
                    //(!SolidCollisionFix(NPC.position, NPC.width, NPC.height, false) || //If we are no longer inside of tiles
                    NPC.Center.Y > movementTarget.Y || //If we reached the height hte player was at before
                    NPC.Center.Y > oldPosition.Y)) //Fallback, if we went below where the player used to be
                    ExtraMemory = 1f;

                bool canFallThroughPlatforms = NPC.Bottom.Y < target.Bottom.Y;
                bool hasCollided = (ExtraMemory == 1f && SolidCollisionFix(NPC.position + NPC.velocity, NPC.width, NPC.height / 2, !canFallThroughPlatforms)); //|| Math.Abs(collisionDifference.X) > 0f || Math.Abs(collisionDifference.Y) > 0f;
                NPC.velocity.Y += 0.5f + (1 - AttackTimer) * 2f;

                if (hasCollided)
                {
                    //Re-enable tile collision just in case
                    NPC.velocity.Y = 0;

                    if (!Main.dedServ && Main.LocalPlayer.Distance(NPC.Center) < 700)
                        CameraManager.Quake += 27f;
                    SoundEngine.PlaySound(SlamSound, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient && DifficultyScale > 0)
                    {
                        float shockwaveWidth = 24f + 6f * DifficultyScale;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitY * 12f, Vector2.UnitY * 5f, ModContent.ProjectileType<CrabulonStomp>(), (int)(HuskDrop_ShockwaveDamage * DamageMultiplier), 1f, Main.myPlayer, 0, shockwaveWidth);
                    }

                    crackPosition = VisualCenter + Vector2.UnitY * 28f;
                    crackRotation = Main.rand.NextFloat() * MathHelper.TwoPi;

                    AttackTimer = 1f;
                    SubState = ActionState.HuskDrop_Stunned;
                }

                AttackTimer -= 1 / (60f * 2f);
            }

            else if (SubState == ActionState.HuskDrop_Stunned)
            {
                visualOffsetExtra.Y = (1 - (float)Math.Pow(Math.Min((1 - AttackTimer) / 0.125f, 1f), 3f)) * -26f + 10f * AttackTimer;

                NPC.behindTiles = true;
                NPC.velocity.X = 0;
                AttackTimer -= 1 / (60f * 2.5f);
                visualRotationExtra += 0.4f * Math.Min(1f, (AttackTimer) / 0.5f);

                if (AttackTimer < 0.5f)
                {
                    if (AttackTimer + 1 / (60f * 2.5f) >= 0.5f)
                        SoundEngine.PlaySound(GetupSound, NPC.Center);

                    if (!Main.dedServ)
                    {
                        foreach (CrabulonLeg leg in Legs)
                        {
                            leg.LimpingLeg.points[2].locked = true;
                            if (Main.rand.NextBool(40) || leg.LimpingLeg.points[0].position.Distance(leg.LimpingLeg.points[2].position) > leg.maxLenght * 0.95f)
                                leg.UnLimp();
                        }
                    }

                    ChasingMovement(true, 0f);
                    NPC.velocity.Y *= (1 - AttackTimer);
                }

                flickerBackground = 5;
                flickerFrom = 4;

                if (Main.rand.NextBool((int)(10 + 30 * AttackTimer)) && AttackTimer < 0.8f && flickerOpacity < (0.2f + (1 - AttackTimer) * 0.4f))
                    flickerOpacity = Main.rand.NextFloat(0.8f, 1f);
                else if (AttackTimer > 0.8f)
                    flickerOpacity = 0f;

                if (!Main.dedServ)
                {
                    //In case the arm "broke" we have to put it back in place
                    if (AttackTimer >= 0.55f && AttackTimer < 0.65f)
                    {
                        float progress = 1 - (AttackTimer - 0.55f) / 0.1f;
                        foreach (CrabulonArm arm in Arms)
                        {
                            arm.updateLimpElbow = false;
                            Vector2 idealElbow = InverseKinematic(arm.Anchor, arm.armTip, arm.forearmLength * NPC.scale, arm.armLenght * NPC.scale, arm.bendFlip);
                            arm.elbow = Vector2.Lerp(arm.LimpingArm.points[1].position, idealElbow, progress);
                        }
                    }

                    if (AttackTimer < 0.55f)
                    {
                        foreach (CrabulonArm arm in Arms)
                            arm.UnLimp();
                    }

                    foreach (CrabulonArm arm in Arms)
                    {
                        arm.accelerationOverride = (1 - AttackTimer) * 0.01f;
                        if (AttackTimer < 0.4f)
                            arm.accelerationOverride = MathHelper.Lerp(arm.accelerationOverride.Value, 0.2f, 1 - AttackTimer / 0.4f);
                        if (AttackTimer < 0.1f)
                            arm.accelerationOverride = MathHelper.Lerp(arm.accelerationOverride.Value, arm.acceleration, 1 - AttackTimer / 0.1f);

                        arm.springinessOverride = 0.8f;
                        arm.contractionOverride = arm.IdealContraction * (0.6f + 0.4f * (1 - AttackTimer - 0.55f) / 0.45f);
                    }
                }

                if (AttackTimer <= 0f && !Main.dedServ)
                {
                    flickerOpacity = 1;
                    flickerFrom = 3;

                    NPC.behindTiles = false;
                    foreach (CrabulonLeg leg in Legs)
                        leg.UnLimp();

                    foreach (CrabulonArm arm in Arms)
                        arm.UnLimp();
                }
            }

            return AttackTimer <= 0f;
        }
        #endregion

        #region Charge
        public bool ChargeAttack()
        {
            if (SubState == ActionState.Charge)
            {
                SubState = ActionState.Charge_Screech;
                NPC.TargetClosest();
                SoundEngine.PlaySound(ShriekSound, NPC.Center);
                AttackTimer = 1;
                NPC.velocity.X *= 0.5f;

                ExtraMemory = 1f + 0.25f * Utils.GetLerpValue(400, 280, target.Distance(NPC.Center), true);
            }

            if (SubState == ActionState.Charge_Screech)
            {
                Vector2 direction = NPC.Center.DirectionTo(target.Center);

                AttackTimer -= 1 / (60f * 0.7f * ExtraMemory);

                //Dont move horizontally but keep doing the vertical movement
                if (!TopDownView)
                    ContinueMoving(false, 0f);
                else
                {
                    NPC.velocity *= 0.98f;
                    NPC.velocity = NPC.velocity.ToRotation().AngleTowards(direction.ToRotation(), 0.02f + (1 - AttackTimer) * 0.2f).ToRotationVector2() * NPC.velocity.Length();
                }

                visualOffsetExtra -= direction * (float)Math.Sin((1 - AttackTimer) * MathHelper.Pi * 0.8f) * 45f * NPC.scale;
                visualOffsetExtra += Main.rand.NextVector2Circular(6f, 6f) * (1 - AttackTimer);

                //Update goal position up until a point
                if (AttackTimer >= 0.3f)
                    goalPosition = target.Center + direction * 170f;

                if (AttackTimer <= 0f)
                {
                    //Align downwards
                    if (!TopDownView)
                        NPC.Center += Vector2.UnitY * 14f * NPC.scale;
                    TopDownView = true;

                    SubState = ActionState.Charge_DashForwards;
                    AttackTimer = 1;
                    NPC.velocity = direction * 25f;
                    NPC.rotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;
                    oldPosition = NPC.Center;

                    if (DifficultyScale > 0)
                        SoundEngine.PlaySound(DashExpertSound, NPC.Center);
                    else
                        SoundEngine.PlaySound(DashSound, NPC.Center);

                    if (Main.LocalPlayer.Distance(NPC.Center) < 1000)
                        CameraManager.Shake += 20;

                    if (Main.netMode != NetmodeID.MultiplayerClient && DifficultyScale >= 2)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<LingeringSporeGas>(), 0, 1, Main.myPlayer, (int)(Charge_TrailLingerTime * 60), 44f, NPC.whoAmI);
                    }

                    ExtraMemory = -1;
                }
            }

            if (SubState == ActionState.Charge_DashForwards)
            {
                //- (0.5f - 0.5f * Vector2.Dot(NPC.DirectionTo(target.Center), NPC.velocity.SafeNormalize(Vector2.UnitX))) * 0.6f
                float dashTime = 0.8f;
                AttackTimer -= 1 / (60f * dashTime);
                NPC.rotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;

                if (AttackTimer <= 0.4f)
                    NPC.velocity *= 0.98f;

                if (AttackTimer <= 0.1f)
                    NPC.velocity *= 0.9f;

                if (!Main.rand.NextBool(3))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust d = Dust.NewDustPerfect(VisualCenter + Main.rand.NextVector2Circular(100f, 100f) * NPC.scale, DustID.MushroomSpray, -NPC.velocity.RotatedByRandom(0.3f) * 0.2f * Main.rand.NextFloat(0.2f, 1f), 0);

                        d.scale *= Main.rand.NextFloat(0.8f, 1.2f);

                        if (Main.rand.NextBool(5))
                        {
                            d.velocity *= -1;
                            d.type = DustID.MushroomTorch;
                            d.noGravity = true;
                        }
                    }
                }

                if (Main.rand.NextBool(10))
                {
                    flickerBackground = Main.rand.Next(1, 5);
                    flickerFrom = flickerBackground;
                    flickerOpacity = 1f;
                }
                

                if (Vector2.Dot(goalPosition.DirectionTo(oldPosition), goalPosition.DirectionTo(NPC.Center)) < -0.2f)
                    AttackTimer = 0f;

                if (AttackTimer <= 0f)
                {
                    SubState = ActionState.Charge_Recovery;
                    AttackTimer = 1f;
                    NPC.velocity *= 0.6f;
                }
            }

            if (SubState == ActionState.Charge_Recovery)
            {
                AttackTimer -= 1 / (60f * 0.3f);
                ContinueMoving(true, 0.5f + (1 - AttackTimer) * 0.5f);
            }

            return AttackTimer <= 0;
        }
        #endregion

        #region Slam Claw
        public bool ClawSlam()
        {
            if (SubState == ActionState.Slam)
            {
                NPC.TargetClosest();
                SubState = ActionState.Slam_ReadyClaw;
                SoundEngine.PlaySound(ClawSlamTelegraphSound, NPC.Center);
                AttackTimer = 1;
                NPC.velocity.X *= 0.7f;
                TopDownView = false;
                lookingSideways = true;
                ExtraMemory = 0;
                goalPosition = Vector2.Zero;
            }

            flickerTickDownTimeOverride = 1.2f;

            if (SubState == ActionState.Slam_ReadyClaw)
            {
                ChasingMovement(false, Math.Max(0f, AttackTimer - 0.6f) * 0.5f, cannotClimbUp : AttackTimer < 0.8f);

                flickerOpacity = 1 - AttackTimer;
                if (Main.rand.NextBool(16) || AttackTimer == 1)
                    flickerFrom = Main.rand.Next(3, 5);

                //Scuffed but this keeps the direction the same
                if (goalPosition == Vector2.Zero)
                    goalPosition.X = NPC.direction;
                else
                    NPC.direction = (int)goalPosition.X;

                //Walking away from the player
                if (ExtraMemory <= 0 && Math.Abs(NPC.Center.X - target.Center.X) < 90f)
                {
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (NPC.Center.X - target.Center.X).NonZeroSign() * 10f, 0.15f);
                    ExtraMemory = -1;
                }

                else if (ExtraMemory > 0 || (ExtraMemory == 0 && Math.Abs(NPC.Center.X - target.Center.X) > 190f))
                {
                    if (ExtraMemory == 0)
                        ExtraMemory = 1f + Utils.GetLerpValue(290f, 400f, Math.Abs(NPC.Center.X - target.Center.X), true);

                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, -(NPC.Center.X - target.Center.X).NonZeroSign() * (6f + (ExtraMemory - 1) * 6f), 0.15f);
                }

                else if (AttackTimer < 0.6f)
                    NPC.velocity.X *= 0.7f;

                //Clamp vertical mobility to avoid insane repositionings
                if (NPC.velocity.Y < -1.2f)
                    NPC.velocity.Y = -1.2f;

                float attackSpeed = 0.75f + Utils.GetLerpValue(300f, 100f, FloorPosition.Distance(target.Center), true) * 0.15f;
                AttackTimer -= 1 / (60f * attackSpeed);
                visualRotationExtra = NPC.direction * (1 - AttackTimer) * -0.3f;

                visualOffsetExtra.X = (1 - AttackTimer) * (NPC.Center.X - target.Center.X).NonZeroSign() * 40f;
                visualOffsetExtra.Y = PiecewiseAnimation(1 - AttackTimer, new CurveSegment[] { new(SineBumpEasing, 0f, 0f, 20f), new(SineOutEasing, 0.5f, 0f, -35f) });

                visualOffsetExtra.Y *= NPC.scale;

                SetClawOpenness(MathF.Pow((1 - AttackTimer), 1.4f) * 0.8f);

                if (AttackTimer <= 0f)
                {
                    if (ExtraMemory <= 0)
                        NPC.velocity.X += NPC.direction * 3.2f;

                    if (NPC.velocity.Y < 0)
                        NPC.velocity.Y = 0;

                    AttackTimer = 1f;
                    SubState = ActionState.Slam_SlamDown;
                    SoundEngine.PlaySound(ClawSlamSound, NPC.Center);

                    float effectStrenght = Utils.GetLerpValue(300f, 0f, Main.LocalPlayer.Distance(FloorPosition + Vector2.UnitX * NPC.direction * 200f), true);
                    if (effectStrenght > 0f)
                        CameraManager.AddCameraEffect(new SnappyDirectionalCameraTug(Vector2.UnitY * 30f * effectStrenght, 15, 0.6f, 0.4f, CircOutEasing, 1f, PolyOutEasing, 2f));

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X + NPC.direction * 240f, FloorHeight), Vector2.Zero, ModContent.ProjectileType<CrabulonStomp>(), 0, 0, Main.myPlayer, 0, 17f);
                }
            }

            else if (SubState == ActionState.Slam_SlamDown)
            {
                ChasingMovement(false, onlyVerticalMovement: true, cannotClimbUp : true);
                visualRotationExtra = NPC.direction * (float)Math.Pow(AttackTimer, 0.6f) * 0.4f;
                visualOffsetExtra = Vector2.UnitY * AttackTimer * 42f * NPC.scale;


                SetClawOpenness(0f);

                AttackTimer -= 1 / (60f * 0.5f);
                NPC.velocity.X *= 0.98f;

                if (AttackTimer <= 0)
                    lookingSideways = false;
            }

            return AttackTimer <= 0;
        }
        #endregion

        #region Snip Claw
        public bool ClawSnip()
        {
            if (SubState == ActionState.Snip)
            {
                NPC.TargetClosest();
                SubState = ActionState.Snip_ReadyClaw;
                SoundEngine.PlaySound(ClawSnipTelegraphSound, NPC.Center);
                SoundEngine.PlaySound(ClawClickSound, NPC.Center);
                AttackTimer = 1;
                NPC.velocity.X *= 0.8f;
                TopDownView = false;
                lookingSideways = true;
                ExtraMemory = 0;
                goalPosition = Vector2.Zero;
            }

            flickerTickDownTimeOverride = 1.2f;

            if (SubState == ActionState.Snip_ReadyClaw)
            {
                ChasingMovement(false, Math.Max(0f, AttackTimer - 0.6f) * 0.5f, cannotClimbUp: AttackTimer < 0.8f);
                //Scuffed but this keeps the direction the same
                if (goalPosition == Vector2.Zero)
                    goalPosition.X = NPC.direction;
                else
                    NPC.direction = (int)goalPosition.X;

                //If the player is too close, or if we were already backing away, back away.
                if (ExtraMemory <= 0 && Math.Abs(NPC.Center.X - target.Center.X) < 90f)
                {
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (NPC.Center.X - target.Center.X).NonZeroSign() * 14f, 0.15f);
                    ExtraMemory = -1;
                }
                else if (AttackTimer < 0.6f)
                    NPC.velocity.X *= 0.7f;

                if (NPC.velocity.Y < -2)
                    NPC.velocity.Y = -2;

                flickerOpacity = 1 - AttackTimer;
                flickerFrom = 2;

                SetClawOpenness(MathF.Pow(1 - AttackTimer, 2f) * 0.7f);

                AttackTimer -= 1 / (60f * 0.75f);

                visualRotationExtra = NPC.direction * (1 - AttackTimer) * 0.42f;
                visualOffsetExtra.X = (1 - AttackTimer) * (NPC.Center.X - target.Center.X).NonZeroSign() * 40f;
                visualOffsetExtra.Y = PiecewiseAnimation(1 - AttackTimer, new CurveSegment[] { new(SineBumpEasing, 0f, 0f, -20f), new(SineOutEasing, 0.3f, 0f, 30f) });
                visualOffsetExtra.Y += MathF.Pow(1 - AttackTimer, 0.5f) * CollisionBoxYOffset * 0.4f;

                if (AttackTimer <= 0)
                {
                    if (ExtraMemory <= 0)
                        NPC.velocity.X = NPC.direction * 45f;

                    if (NPC.velocity.Y < 0)
                        NPC.velocity.Y = 0;

                    //Aim past the player if the player is in front of crabby
                    if ((target.Center.X - NPC.Center.X) * goalPosition.X > 0)
                        goalPosition = target.Center + goalPosition * 150f;
                    else
                        goalPosition = NPC.Center + goalPosition * 50f;


                    ExtraMemory = 0f;
                    AttackTimer = 1f;
                    SubState = ActionState.Snip_ThrustForwards;
                    SoundEngine.PlaySound(ClawSnipSound, NPC.Center);
                    SoundEngine.PlaySound(ClawClackSound, NPC.Center);
                }
            }

            else if (SubState == ActionState.Snip_ThrustForwards)
            {
                ChasingMovement(false, onlyVerticalMovement: true, cannotClimbUp: true);
                AttackTimer -= 1 / (60f * 0.5f);

                //Stay fast
                if (ExtraMemory == 0f)
                {
                    NPC.velocity.X *= 0.97f;
                    if ((NPC.Center.X - goalPosition.X) * NPC.velocity.X.NonZeroSign() <= 0 || AttackTimer < 0.5f)
                        ExtraMemory = 1f;
                }
                //Slow fast
                else
                    NPC.velocity.X *= 0.9f;

                if (Arms != null)
                    foreach (CrabulonArm arm in Arms)
                    {
                        arm.springinessOverride = 0.0f;
                        arm.accelerationOverride = 0.2f;
                    }

                if (NPC.velocity.Y < -1)
                    NPC.velocity.Y = -1;

                SetClawOpenness(-0.1f);
                visualOffsetExtra.X = NPC.velocity.X.NonZeroSign() * (float)Math.Pow(AttackTimer, 2f) * 35f * NPC.scale;
                visualOffsetExtra.Y = MathF.Pow(Math.Max(0, AttackTimer), 0.2f) * CollisionBoxYOffset * 0.4f;

                if (AttackTimer <= 0)
                {
                    lookingSideways = false;
                    if (Arms != null)
                        foreach (CrabulonArm arm in Arms)
                            arm.UnloadSprings();
                }
            }

            return AttackTimer <= 0;
        }
        #endregion

        #region Slingshot
        public bool SlingshotAttached => AIState == ActionState.Slingshot && SubState != ActionState.Slingshot && SubState != ActionState.Slingshot_Overshoot && ((int)SubState > (int)ActionState.Slingshot_SpitAndWait || AttackTimer < 0.5f);

        public bool Slingshot()
        {
            if (SubState == ActionState.Slingshot)
            {
                slingshotLegAttachOffsets.Clear();
                NPC.TargetClosest();
                SubState = ActionState.Slingshot_SpitAndWait;
                AttackTimer = 1;
                NPC.velocity.X *= 0.5f;
                if (!TopDownView)
                    lookingSideways = true;
                ExtraMemory = 0;
                goalPosition = Vector2.Zero;
            }

            if (SubState == ActionState.Slingshot_SpitAndWait)
            {
                float walkspeed = SlingshotAttached ? 0f : 0.3f;
                ContinueMoving(false, walkspeed, false);


                float previousAttackTimer = AttackTimer;
                AttackTimer -= 1 / (60f * 0.55f);

                if (AttackTimer >= 0.5f)
                {
                    visualRotationExtra = Main.rand.NextFloat(-1f, 1f) * 0.4f * (1 - AttackTimer);
                    visualOffsetExtra.Y = (float)Math.Sin((1 - AttackTimer) * MathHelper.TwoPi) * -21f * NPC.scale;
                }

                else
                {
                    //Spit
                    if (previousAttackTimer >= 0.5f)
                    {
                        SoundEngine.PlaySound(GrappleSound, NPC.Center);
                        Vector2 normal = NPC.DirectionTo(target.Center);

                        //Dust along web
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 dustPosition = Vector2.Lerp(NPC.Center, target.Center, Main.rand.NextFloat(0f, 0.97f));
                            dustPosition += Main.rand.NextVector2Circular(10f, 10f);
                            Vector2 dustVelocity = NPC.Center.DirectionTo(target.Center) * Main.rand.NextFloat(1f, 3f);
                            int dustType = DustID.MushroomSpray;

                            if (Main.rand.NextBool(5))
                            {
                                dustVelocity *= -1;
                                dustType = DustID.MothronEgg;
                            }
                            Dust d = Dust.NewDustPerfect(dustPosition, dustType, dustVelocity, Scale: Main.rand.NextFloat(0.7f, 1f));
                            if (dustType == DustID.MothronEgg)
                                d.noGravity = true;
                        }

                        //Blood gushing out as the player is skewered
                        for (int d = 0; d < 23; d++)
                        {
                            Vector2 direction = normal.RotatedByRandom(0.2f);
                            Vector2 bloodPosition = target.Center + direction * Main.rand.NextFloat(0f, 18f);
                            int dustType = (!ChildSafety.Disabled || Main.rand.NextBool(4)) ? DustID.GlowingMushroom : DustID.Blood;
                            Dust.NewDustPerfect(bloodPosition, dustType, direction * Main.rand.NextFloat(5f, 12f), Scale: Main.rand.NextFloat(0.8f, 1.7f));
                        }

                        if (Main.myPlayer == target.whoAmI)
                            CameraManager.AddCameraEffect(new DirectionalCameraTug(normal * 8f, 3f, 20, uniqueIdentity: "crabulonSlingshot"));

                        //Push the player away
                        if (target.Distance(NPC.Center) < 600f)
                            target.velocity += normal * 7f;
                        else
                        {
                            target.velocity *= 0.6f;
                            target.velocity -= normal * 7f;
                        }
                    }

                    visualOffsetExtra = NPC.DirectionTo(target.Center) * AttackTimer * 82f;
                    visualOffsetExtra.Y += (float)Math.Sin(AttackTimer * MathHelper.TwoPi) * 14f * NPC.scale;
                }

                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1f;
                    SubState = ActionState.Slingshot_JumpSlowmo;
                    lookingSideways = false;
                    TopDownView = true;
                    NPC.velocity.Y = -8f;
                }
            }

            else if (SubState == ActionState.Slingshot_JumpSlowmo)
            {
                AttackTimer -= 1 / (60f * 0.4f);
                NPC.velocity.Y *= 0.91f;
                NPC.rotation = NPC.AngleTo(target.Center) - MathHelper.PiOver2;

                if (!Main.dedServ)
                {
                    foreach (CrabulonArm arm in Arms)
                    {
                        arm.springinessOverride = 1f;
                        arm.accelerationOverride = 0.2f;
                    }
                }

                visualOffsetExtra = -NPC.DirectionTo(target.Center) * 65f * NPC.scale * (float)Math.Sin(AttackTimer * MathHelper.Pi);

                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1f;
                    SubState = ActionState.Slingshot_Reel_in;
                    NPC.velocity = NPC.DirectionTo(target.Center) * 3f;

                    GrappleSoundSlot = SoundEngine.PlaySound(GrappleLoopSound, NPC.Center);
                    SoundHandler.TrackSound(GrappleSoundSlot);
                }
            }

            else if (SubState == ActionState.Slingshot_Reel_in)
            {
                float angleToTarget = NPC.AngleTo(target.Center);
                NPC.rotation = angleToTarget - MathHelper.PiOver2;
                NPC.velocity = NPC.velocity.ToRotation().AngleLerp(angleToTarget, 0.05f + (1 - AttackTimer) * 0.3f).ToRotationVector2() * NPC.velocity.Length();
                NPC.velocity += NPC.DirectionTo(target.Center) * (1f + (1 - AttackTimer) * 0.5f);

                Vector2 distance = NPC.Center - target.Center;
                distance.Y *= 2f;

                AttackTimer -= 1 / (60f * 1.5f);

                if (!Main.dedServ)
                {
                    foreach (CrabulonArm arm in Arms)
                    {
                        arm.springinessOverride = 1f;
                        arm.accelerationOverride = 0.2f;
                    }
                }

                if (AttackTimer <= 0)
                    slingshotWeb?.Clear();

                if (distance.Length() < 100f)
                {
                    slingshotWeb?.Clear();
                    AttackTimer = 1f;
                    SubState = ActionState.Slingshot_Overshoot;
                }
            }

            else if (SubState == ActionState.Slingshot_Overshoot)
            {
                //Hold period for crabulon while waiting for a new attack in mp
                if (AttackTimer <= -1)
                    return false;

                AttackTimer -= 1 / (60f * 0.4f);
                Vector2 normalizedVelocity = NPC.velocity.SafeNormalize(Vector2.UnitY);
                float dotVertical = Vector2.Dot(normalizedVelocity, Vector2.UnitY);

                //If crabulon is approaching the player from the 90° angle above them, quickly go sideways
                if (dotVertical > 0.5f)
                    NPC.velocity.X *= 1.04f;
                //If approaching from the bottom 90° angle, accelerate vertically
                else if (dotVertical < -0.5f)
                    NPC.velocity.Y *= 1.02f;
                //If approaching the player from the side 90° angle
                else
                {
                    //If going down, slow a bit
                    if (NPC.velocity.Y > 0f)
                        NPC.velocity.Y *= 0.9f;
                    NPC.velocity.X *= 1.03f;
                }

                float distance = NPC.Distance(target.Center);
                float dotTarget = Vector2.Dot(normalizedVelocity, NPC.DirectionTo(target.Center));

                //Main.NewText(dotTarget.ToString());

                //If we went past the player, end the overshoot early
                if (distance > 100f && dotTarget < -0.1f)
                    AttackTimer = 0f;

                //Also end the overshoot if the player is too far
                else if (distance > 400f)
                    AttackTimer = 0f;

                if (AttackTimer <= 0f)
                {
                    SoundEngine.PlaySound(GrappleDetachSound, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        AttackTimer = 1f;
                        SubState = Main.rand.NextBool() ? ActionState.Slam : ActionState.Snip;
                        NPC.netUpdate = true;
                    }
                    else
                        AttackTimer = -1;

                    NPC.velocity.X *= 0.2f;
                    if (FloorHeight > target.Bottom.Y)
                        NPC.velocity.Y -= 8f;
                }
            }

            if (SlingshotAttached)
            {
                target.GetModPlayer<FablesPlayer>().MoveSpeedModifier *= 0.5f;
                if (target.velocity.Length() > 6f)
                    target.velocity *= 0.9f;
            }

            if ((int)SubState > (int)ActionState.Slingshot_JumpSlowmo)
            {
                if (!SoundEngine.TryGetActiveSound(GrappleSoundSlot, out var loopSound))
                {
                    GrappleSoundSlot = SoundEngine.PlaySound(GrappleLoopSound, NPC.Center);
                }
                if (SoundEngine.TryGetActiveSound(GrappleSoundSlot, out loopSound))
                {
                    loopSound.Position = NPC.Center;
                    loopSound.Pitch = Utils.GetLerpValue(533f, 0f, NPC.Distance(target.Center), true);
                }

                SoundHandler.TrackSound(GrappleSoundSlot);
            }

            return AttackTimer <= 0;
        }
        #endregion

        #region Desperation
        public float desperationStartRotation;
        public float desperationTug;

        public void DesperationSlams()
        {
            //Recoil on crab's body when skewered
            if (!Main.dedServ && Ropes.stabTimer > 0f)
            {
                visualRotationExtra = Ropes.stabTimer / 15f * 0.24f * Ropes.stabDirection;
                visualOffsetExtra = Ropes.stabTimer / 15f * Main.rand.NextVector2Circular(1f, 1f) * 4f;

                Vector2 stabDir = Ropes.attachPointOffsets[Ropes.cableCount - 1].DirectionFrom(Ropes.attachPointOffsets[Ropes.cableCount - 1] * 1.4f - Vector2.UnitY * 1400f);
                visualOffsetExtra += stabDir * 10f * Ropes.stabTimer / 15f;
            }

            //Set flicker
            flickerBackground = 5;
            flickerFrom = Main.rand.Next(5);
            flickerTickDownTimeOverride = 1.5f;

            //Goalposition.X is used to keep track of the amount of successive slams
            bool bigSlam = goalPosition.X > 3;
            float pauseDuration = !WorldProgressionSystem.DefeatedCrabulon? 4.5f : 2f;

            if (((SubState == ActionState.Desperation_CinematicWait && AttackTimer <= 0.4f) || SubState == ActionState.Desperation_VineAttach) && Main.LocalPlayer.WithinRange(NPC.Center, 1300))
            {
                Vector2 positionOffset = SubState == ActionState.Desperation_VineAttach ? Vector2.Zero : Vector2.UnitY * AttackTimer * 300f;
                int gasCount = SubState == ActionState.Desperation_VineAttach ? 5 : (int)((0.4f - AttackTimer) / 0.4f * 5);
                Vector2 gasVelocityBoost = SubState == ActionState.Desperation_CinematicWait ? Vector2.Zero : -Vector2.UnitY * Main.rand.NextFloat(2f, 5f) * MathF.Pow(AttackTimer, 2f);

                for (int i = 0; i < gasCount; i++)
                {
                    Vector2 gasSpawnPos = new Vector2(Main.rand.NextFloat(0f, Main.screenWidth), Main.screenHeight + Main.rand.NextFloat(-30f, 200f)) + Main.screenPosition;
                    gasSpawnPos += positionOffset;

                    float gasParallax = Main.rand.NextFloat(0f, 0.8f);
                    float gasSize = Main.rand.NextFloat(5f, 8f) + (0.8f - gasParallax) * 3f;
                    Vector2 gasVelocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 6f) + Vector2.UnitX * Main.rand.NextFloat(0f, 1f) + gasVelocityBoost;

                    Particle smokeGas = new ScreenSporeGas(gasSpawnPos, gasVelocity, gasSize, 0.01f, gasParallax);
                    ParticleHandler.SpawnParticle(smokeGas);
                }

                for (int i = 0; i < 3; i++)
                {
                    Vector2 dustSpawnPos = new Vector2(Main.rand.NextFloat(0f, Main.screenWidth), Main.screenHeight + Main.rand.NextFloat(-70f, 0f)) + Main.screenPosition;
                    dustSpawnPos += positionOffset;
                    Vector2 dustVel = -Vector2.UnitY * Main.rand.NextFloat(1f, 6f) + Vector2.UnitX * Main.rand.NextFloat(-1f, 1f) + gasVelocityBoost;

                    Dust d = Dust.NewDustPerfect(dustSpawnPos, DustID.MushroomSpray, dustVel);
                    d.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    d.noLightEmittence = true;
                    if (Main.rand.NextBool(5))
                        d.noGravity = false;
                }
            }

            if (SubState == ActionState.Desperation)
            {
                desperationStartRotation = Main.rand.NextFloat(0.6f, 1f) * MathHelper.PiOver4 * lastHitDirection;

                SoundEngine.PlaySound(Items.DesertScourgeDrops.StormlionWhip.YankSound, NPC.Center);

                if (!Main.dedServ)
                {
                    foreach (CrabulonLeg leg in Legs)
                    {
                        leg.GoLimp();
                        leg.LimpingLeg.points[2].position.Y -= Main.rand.NextFloat(24f, 60f);

                        Vector2 legTip = leg.LimpingLeg.points[2].position;
                        Vector2 legVelocity = new Vector2((legTip.X - NPC.Center.X).NonZeroSign() * 60f + NPC.velocity.X * Main.rand.NextFloat(0.2f, 0.8f), -Main.rand.NextFloat(20f, 60f));

                        leg.LimpingLeg.points[2].oldPosition = leg.LimpingLeg.points[2].position - legVelocity.RotatedByRandom(0.4f);
                    }

                    foreach (CrabulonArm arm in Arms)
                        arm.GoLimp();

                    Ropes.Reset();

                    while (Rack.strings.chains.Count > 0)
                        Rack.SnapString(true);
                }

                NPC.HitSound = FlimsyHitSound;
                NPC.dontTakeDamage = true;
                NPC.behindTiles = true;
                TopDownView = false;
                AttackTimer = 1f;
                SubState = ActionState.Desperation_CinematicWait;
                ExtraMemory = 0;
                NPC.velocity.Y = -3;
                NPC.position.Y -= 17f;

                int bombType = ModContent.NPCType<CrabulonSporeBomb>();
                int thrownBombType = ModContent.NPCType<CrabulonThrownSporeBomb>();
                float despawnTime = ModContent.GetInstance<CrabulonSporeBomb>().ExpirationTime - 60;

                //Despawn all spore mines
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && (npc.type == bombType || npc.type == thrownBombType) && npc.ai[0] < despawnTime)
                        npc.ai[0] = despawnTime;
                }
            }

            if (SubState == ActionState.Desperation_CinematicWait)
            {
                visualOffsetExtra.Y = 10f;

                if (ExtraMemory == 0)
                {
                    ChasingMovement(false, 0f, true);
                    visualRotationExtra = (Main.rand.NextFloat(-0.05f, 0.05f) + desperationStartRotation) * (float)Math.Pow(AttackTimer, 3f);
                }

                if (NPC.velocity.Y > 12)
                    NPC.velocity.Y = 12;

                NPC.velocity.X *= 0.97f;
                if (ExtraMemory == 1)
                    NPC.velocity.X *= 0.85f;

                if (NPC.velocity.Y == 0 && ExtraMemory == 0)
                {
                    ExtraMemory = 1;
                    SoundEngine.PlaySound(LightSlamSound, NPC.Center);
                    CameraManager.Quake += 30;
                }

                AttackTimer -= 1 / (60f * pauseDuration);

                if (CameraManager.PanMagnet.SetMagnetPositionAndImmunityForEveryone(VisualCenter, NPC.Center, 1300))
                {
                    CameraManager.PanMagnet.PanProgress += 0.02f;

                    if (AttackTimer <= 0.4f)
                    {
                        SoundEngine.PlaySound(HorrorScreamSound);
                        CameraManager.Shake = (1 - AttackTimer) * 20f;
                    }
                }


                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1f;
                    SubState = ActionState.Desperation_VineAttach;

                    if (!SoundEngine.TryGetActiveSound(HorrorSoundSlot, out var activeSound))
                    {
                        HorrorSoundSlot = SoundEngine.PlaySound(HorrorSound, NPC.Center, delegate (ActiveSound s) {
                            s.Position = NPC.Center;
                            if (!NPC.active || AIState != ActionState.Desperation)
                                return false;
                            return true;
                        });
                    }

                    if (SoundEngine.TryGetActiveSound(HorrorSoundSlot, out activeSound))
                        activeSound.Volume = 0f;
                }
            }

            if (SubState == ActionState.Desperation_VineAttach)
            {
                AttackTimer -= 1 / (60f * 1f);

                NPC.velocity = Vector2.Zero;


                if (SoundEngine.TryGetActiveSound(HorrorSoundSlot, out var activeSound))
                    activeSound.Volume = (1 - AttackTimer) * 0.5f;

                //Add skewers faster and faster
                if (!Main.dedServ && Ropes.cableCount < Math.Pow(1 - AttackTimer, 2f) * 5f)
                    Ropes.AddSkewer();

                if (CameraManager.PanMagnet.SetMagnetPositionAndImmunityForEveryone(VisualCenter, NPC.Center, 1300))
                {
                    CameraManager.PanMagnet.PanProgress += 0.02f;
                    CameraManager.Shake = MathF.Pow(Math.Max(AttackTimer, 0), 0.5f) * 20f;
                }


                if (AttackTimer <= 0f)
                {
                    NPC.dontTakeDamage = false;
                    AttackTimer = 1f;
                    SubState = ActionState.Desperation_ReelUp;
                    goalPosition = Vector2.Zero;
                }
            }

            float horrorSoundVolumeMultiplier = Utils.GetLerpValue(DesperationPhaseTreshold, 0f, NPC.life / (float)NPC.lifeMax, true) * 0.5f + 0.5f;

            if (SubState == ActionState.Desperation_ReelUp)
            {
                //Slowly rise up. Nothing major
                NPC.velocity.X = 0f;
                NPC.velocity.Y -= 0.2f + Utils.GetLerpValue(DesperationPhaseTreshold, 0f, NPC.life / (float)NPC.lifeMax, true) * 0.15f;

                float attackTime = 0.5f - Utils.GetLerpValue(DesperationPhaseTreshold, 0f, NPC.life / (float)NPC.lifeMax, true) * 0.3f;
                AttackTimer -= 1 / (60f * attackTime);

                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1f;
                    SubState = ActionState.Desperation_Chase;
                    //We use goalposition.X to keep track of the amount of times it did the attack in succession
                    goalPosition.X++;
                }
            }

            //Chase the player around, faster and faster as the health drops
            else if (SubState == ActionState.Desperation_Chase)
            {
                float targetX = target.Center.X + target.velocity.X * 12.3f;
                float goalX = MathHelper.Lerp(NPC.Center.X, targetX, (1 - AttackTimer) * 0.2f + 0.04f);
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, goalX - NPC.Center.X, 0.1f + (float)Math.Pow(1 - AttackTimer, 3f));

                float targetHeight = target.Center.Y - 350f + (float)Math.Sin(AttackTimer * MathHelper.TwoPi) * 30f;
                if (bigSlam)
                    targetHeight -= 60f;

                NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, MathHelper.Lerp(NPC.Center.Y, targetHeight, 0.03f + 0.1f * (float)Math.Pow(1 - AttackTimer, 3f)) - NPC.Center.Y, 0.12f);

                if (!Main.dedServ)
                {
                    Ropes.position = Vector2.Lerp(Ropes.position, new Vector2(targetX, targetHeight) - Vector2.UnitY * 2600f, 0.05f);
                    Ropes.positionExtra = Vector2.Zero;
                }

                float attackTime = 0.7f - Utils.GetLerpValue(DesperationPhaseTreshold, 0f, NPC.life / (float)NPC.lifeMax, true) * 0.3f;
                float step = 1 / (60f * attackTime);
                AttackTimer -= step;

                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1f;
                    SubState = ActionState.Desperation_Drop;
                    NPC.velocity.Y = -4;
                    NPC.velocity.X = 0;
                    ExtraMemory = -1f;
                    oldPosition = target.Center;

                    //Find the floor at the position of the npc but the height of the player
                    //Crawl up tiles if there is a hill in the way, for example
                    movementTarget = new Vector2(NPC.Center.X, target.Center.Y);
                    while (movementTarget.Y > NPC.Center.Y)
                    {
                        Dust.QuickDust(movementTarget.ToTileCoordinates(), Color.Red);
                        if (Main.tile[movementTarget.ToTileCoordinates()].IsTileSolid())
                            movementTarget.Y -= 16;
                        else
                            break;
                    }

                    Vector2 fallPosition = movementTarget - NPC.Size * 0.5f;
                    bool canFallThroughPlatforms = movementTarget.Y + NPC.height * 0.5f < target.Bottom.Y;

                    //Crawl down if there is immediate collision
                    while (SolidCollisionFix(fallPosition, NPC.width, NPC.height, !canFallThroughPlatforms))
                    {
                        movementTarget.Y += 16;
                        fallPosition = movementTarget - NPC.Size * 0.5f;
                        canFallThroughPlatforms = movementTarget.Y + NPC.height * 0.5f < target.Bottom.Y;

                        if (movementTarget.Y > oldPosition.Y)
                        {
                            movementTarget.Y = oldPosition.Y;
                            break;
                        }    
                    }    

                }
            }

            else if (SubState == ActionState.Desperation_Drop)
            {
                if (ExtraMemory != 1f && NPC.velocity.Y > 0 && (NPC.Center.Y > movementTarget.Y || NPC.Center.Y > oldPosition.Y))
                    ExtraMemory = 1f;

                bool canFallThroughPlatforms = NPC.Bottom.Y < target.Bottom.Y;
                bool hasCollided = (ExtraMemory == 1f && SolidCollisionFix(NPC.position + NPC.velocity, NPC.width, NPC.height / 2, !canFallThroughPlatforms)); //|| Math.Abs(collisionDifference.X) > 0f || Math.Abs(collisionDifference.Y) > 0f;
                NPC.velocity.Y += 0.5f + (1 - AttackTimer) * 2f;

                if (bigSlam)
                    NPC.velocity.Y += 0.5f;

                if (hasCollided)
                {
                    //Re-enable tile collision just in case
                    NPC.velocity.Y = 0;

                    if (!Main.dedServ && Main.LocalPlayer.Distance(NPC.Center) < 700)
                        CameraManager.Quake += 27f + (bigSlam ? 10 : 0);
                    SoundEngine.PlaySound(SlamSound, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient && DifficultyScale > 0)
                    {
                        float shockwaveWidth = 34;
                        if (bigSlam)
                            shockwaveWidth += 8;

                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitY * 12f, Vector2.UnitY * 5f, ModContent.ProjectileType<CrabulonStomp>(), (int)(HuskDrop_ShockwaveDamage * DamageMultiplier), 1f, Main.myPlayer, 0, shockwaveWidth);
                    }

                    crackPosition = VisualCenter + Vector2.UnitY * 28f;
                    crackRotation = Main.rand.NextFloat() * MathHelper.TwoPi;

                    AttackTimer = 1f;
                    flickerOpacity = 1f;

                    if (!bigSlam)
                    {
                        SubState = ActionState.Desperation_ReelUp;
                        if (goalPosition.X == 2)
                            DesperationCorpseFall(5);
                    }

                    else
                    {
                        SubState = ActionState.Desperation_Stunned;
                        DesperationCorpseFall(7);
                    }
                }

                AttackTimer -= 1 / (60f * 2f);
                if (AttackTimer <= 0)
                {
                    AttackTimer = 1;
                    SubState = ActionState.Desperation_ReelUp;
                }
            }

            else if (SubState == ActionState.Desperation_Stunned)
            {
                horrorSoundVolumeMultiplier *= 1 - MathF.Sin(AttackTimer * MathHelper.Pi);

                NPC.velocity.X = 0;
                AttackTimer -= 1 / (60f * 3.5f);
                visualRotationExtra += 0.4f;

                float progress = 1 - AttackTimer;

                if ((progress >= 0.3f && progress < 0.45f) || (progress >= 0.6f && progress < 0.7f) || progress > 0.9f)
                {
                    float tug = 0;
                    if (progress < 0.45f)
                        tug = Utils.GetLerpValue(0.3f, 0.45f, 1 - AttackTimer, true);
                    else if (progress < 0.7f)
                        tug = Utils.GetLerpValue(0.6f, 0.7f, 1 - AttackTimer, true);
                    desperationTug = DesperationStunTugMotionArc(tug);

                    if (progress > 0.9f)
                        desperationTug = MathF.Pow(Utils.GetLerpValue(0.9f, 1f, progress, true), 0.6f) * 2.7f;
                }


                if (AttackTimer <= 0f)
                {
                    desperationTug = 0f;
                    SubState = ActionState.Desperation_ReelUp;
                    goalPosition = Vector2.Zero;
                }
            }


            if ((int)SubState > (int)ActionState.Desperation_VineAttach)
            {
                 if (SoundEngine.TryGetActiveSound(HorrorSoundSlot, out var horrorSoud))
                    horrorSoud.Volume = horrorSoundVolumeMultiplier;
                 else
                    HorrorSoundSlot = SoundEngine.PlaySound(HorrorSound, NPC.Center, delegate (ActiveSound s) {
                        s.Position = NPC.Center;
                        if (!NPC.active || AIState != ActionState.Desperation)
                            return false;
                        return true;
                    });
            }
                
        }

        public float DesperationStunTugMotionArc(float tug) => PiecewiseAnimation(tug, new CurveSegment[] { new CurveSegment(SineOutEasing, 0f, 0.9f, 0.1f), new CurveSegment(PolyOutEasing, 0.4f, 1f, -1f, 3f) });

        public void DesperationCorpseFall(int projectileCount)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //MFW Unique AI. What has calamity: Fables of a world's mending came down to.
                Point slamOrigin = NPC.Center.ToTileCoordinates();
                slamOrigin.Y = target.Center.ToTileCoordinates().Y - 5;

                List<Point> topography = SirNautilus.GetCeilingTopography_Rockfall(slamOrigin, 90, 67, 40);
                Vector2 projectilePosition = NPC.Center;


                int iterations = 0;
                while (iterations < 35)
                {
                    Tile t = Main.tile[projectilePosition.ToTileCoordinates()];
                    if (t.IsTileSolid())
                        break;

                    projectilePosition.Y -= 16;
                    iterations++;
                }

                int spacing = 5;
                int randomOffset = target.Center.ToTileCoordinates().X % spacing; //The spacing is always perfetly aligned with the player to make it a bit more fair with the quick boulders
                topography.RemoveAll(p => (p.X - randomOffset) % spacing != 0);

                //Remember the boulder RIGHT above the player
                Point rightAbovePlayer = topography.Find(p => Math.Abs(p.X - target.Center.ToTileCoordinates().X) <= spacing / 2);

                topography = SirNautilus.EnsureEscapeOptions(topography, rightAbovePlayer, 6);


                topography.Remove(rightAbovePlayer);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), rightAbovePlayer.ToWorldCoordinates(), Vector2.UnitY * 0.3f, ModContent.ProjectileType<SporedSkeleton>(), (int)(HuskDrop_FallingSkullDamage * DamageMultiplier), 1f, Main.myPlayer, target.Center.Y);


                int bouldersFalling = Math.Min(projectileCount, topography.Count);

                for (int i = 0; i < bouldersFalling; i++)
                {
                    Point tileTarget = topography[Main.rand.Next(topography.Count)];
                    topography.Remove(tileTarget);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), tileTarget.ToWorldCoordinates() - Vector2.UnitY * Main.rand.NextFloat(0f, 100f), Vector2.UnitY * 0.3f, ModContent.ProjectileType<SporedSkeleton>(), (int)(HuskDrop_FallingSkullDamage * DamageMultiplier), 1f, Main.myPlayer, target.Center.Y);
                }
            }
        }
        #endregion
    }
}
