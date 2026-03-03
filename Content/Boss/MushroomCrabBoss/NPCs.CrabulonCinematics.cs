using CalamityFables.Particles;
using Terraria.Graphics.Effects;
using Terraria.Localization;
using Terraria.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public partial class Crabulon : ModNPC, ICustomDeathMessages, IDrawOverTileMask, IIntroCardBoss
    {
        #region Music

        internal int previousMusic = 0;
        public static int DesperationMusic => SoundHandler.UseVanillaMusic ? MusicID.OtherworldlyEerie : MusicLoader.GetMusicSlot(CalamityFables.Instance, "Sounds/Music/MushroomCaves");


        private void SetMusic()
        {
            if (Main.gameMenu)
                return;

            NPC npc = Main.npc.Where(n => n.active && n.type == Type && n.Distance(Main.LocalPlayer.Center) < 5000f).OrderBy(n => n.Distance(Main.LocalPlayer.Center)).FirstOrDefault();
            if (npc == null || npc == default)
                return;

            Crabulon crab = npc.ModNPC as Crabulon;

            if (crab.AIState == ActionState.Despawning)
                return;

            //No music when dead
            if (crab.AIState == ActionState.Dead)
            {
                //Harsh cut
                Main.musicFade[DesperationMusic] = 0;
                Main.musicFade[Main.curMusic] = 0;
                Main.musicFade[Main.newMusic] = 0;
                return;
            }

            //Music for desperation phase
            if (crab.AIState == ActionState.Desperation)
            {
                //Desperation music
                if ((int)crab.SubState > (int)ActionState.Desperation_VineAttach)
                {
                    Main.newMusic = DesperationMusic;
                    Main.musicFade[DesperationMusic] = Math.Max(0.5f, Main.musicFade[DesperationMusic]);

                    if (crab.SubState == ActionState.Desperation_Stunned)
                        Main.musicFade[Main.newMusic] = Math.Min(Main.musicFade[Main.newMusic], 0.75f);
                }

                else
                {
                    //SILENCE
                    Main.musicFade[Main.curMusic] = 0;
                    Main.musicFade[Main.newMusic] = 0;
                }

                return;
            }

            //Mute the music so it starts right as Crabulon screams
            if (crab.AIState == ActionState.SpawningUp && (int)crab.SubState > (int)ActionState.SpawningUp && (int)crab.SubState < (int)ActionState.SpawningUp_Scream)
            {
                //if (previousMusic == -1)
                //{
                //    Main.music
                //}

                //Keep the music as whatever it was before the fight started
                if (Main.curMusic != Music && Main.curMusic != 0)
                    previousMusic = Main.curMusic;

                float volume = 0f;
                if (crab.SubState == ActionState.SpawningUp_Emerge)
                    volume = crab.AttackTimer;


                if (volume > 0)
                {
                    //Force the music to not change, and slowly quiet it
                    Main.newMusic = previousMusic;
                    Main.musicFade[previousMusic] = Math.Min(volume, Main.musicFade[previousMusic]);
                }

                //Flat out cut it out for the fight start 
                else
                {
                    Main.newMusic = 0;
                    Main.musicFade[previousMusic] = 0;
                }
            }

            //Regularly set the music
            else if (crab.Music != -1)
            {
                previousMusic = Main.curMusic;
                Main.newMusic = crab.Music;
                Main.musicFade[Main.newMusic] = 1;
            }
        }
        #endregion

        #region Spawning in
        public int screamParticleTimer = 0;
        public int screamParticleTypeCounter = 0;
        public int digupTimer = 0;
        public bool emergedYet = false;

        public bool SpawnAnimation()
        {
            if (SubState == ActionState.SpawningUp)
            {
                AttackTimer = 1f;
                SubState = ActionState.SpawningUp_Emerge;
                digupTimer = 0;
                emergedYet = !Collision.SolidCollision(CollisionBoxOrigin + Vector2.UnitX * (CollisionBoxWidth / 3f), (int)(CollisionBoxWidth / 3f), 15);

                CalamityFables.Instance.Logger.Debug("Crabulon spawn animation. Set behindtiles and donttakedamage");

                if (!WorldProgressionSystem.encounteredCrabulon)
                    WorldProgressionSystem.encounteredCrabulon = true;
            }

            if ((SubState == ActionState.SpawningUp_Wait || SubState == ActionState.SpawningUp_Scream) && CameraManager.PanMagnet.SetMagnetPositionAndImmunityForEveryone(Vector2.Lerp(VisualCenter, Main.LocalPlayer.Center, 0.5f), NPC.Center, 1300))
            {
                CameraManager.PanMagnet.PanProgress += 0.02f;
            }

            if (SubState == ActionState.SpawningUp_Emerge)
            {
                NPC.TargetClosest();
                ContinueMoving(true, 0f, true);

                NPC.velocity.Y *= (1 - AttackTimer);

                float timeLeft = 4f;
                if (NPC.velocity.Y >= 0)
                    timeLeft = 0.7f;
                AttackTimer -= 1 / (60f * timeLeft);

                if (!emergedYet && !Collision.SolidCollision(CollisionBoxOrigin + Vector2.UnitX * (CollisionBoxWidth / 3f), (int)(CollisionBoxWidth / 3f), 5))
                {
                    emergedYet = true;
                    SoundEngine.PlaySound(EmergeSound, NPC.Center);
                }

                digupTimer++;
                if (digupTimer % 40 == 0 && Collision.SolidCollision(CollisionBoxOrigin + Vector2.UnitX * (CollisionBoxWidth / 3f), (int)(CollisionBoxWidth / 3f), CollisionBoxHeight - 10))
                {
                    DigUpEffects(digupTimer / 40);
                    if (Main.LocalPlayer.WithinRange(NPC.Center, 1200))
                        CameraManager.Shake = Math.Max(CameraManager.Shake, (1 - AttackTimer) * 23f);
                    SoundEngine.PlaySound(ThumpDigSound with { Pitch = 0.5f * (1 - AttackTimer)}, NPC.Center);

                    NPC.position.Y -= 5;
                    NPC.velocity.Y -= 5f;
                }

                if (AttackTimer <= 0)
                {
                    AttackTimer = 1;
                    NPC.behindTiles = false;
                    SubState = ActionState.SpawningUp_Wait;
                }
            }

            else if (SubState == ActionState.SpawningUp_Wait)
            {
                ContinueMoving(true, 0f, true);
                AttackTimer -= 1 / (60f * 0.5f);


                if (AttackTimer <= 0)
                {
                    AttackTimer = 1;
                    SubState = ActionState.SpawningUp_Scream;

                    SoundEngine.PlaySound(SpawnScreamSound, NPC.Center);

                    //if we havent emerged yet, play the sound anyway but lower volume
                    if (!emergedYet)
                        SoundEngine.PlaySound(EmergeSound with { Volume = 0.4f }, NPC.Center);

                    //PUsh players away
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        if (player.active && !player.dead && player.WithinRange(NPC.Center, 400))
                            player.velocity += NPC.SafeDirectionTo(player.Center) * 10f;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Main.NewText(Language.GetTextValue("Announcement.HasAwoken", "Crabulon"), 171, 64, 255);
                }
            }

            else if (SubState == ActionState.SpawningUp_Scream)
            {
                ContinueMoving(true, 0f, true);

                //Eyes flicker random colors
                flickerOpacity = 1f;
                if (Main.rand.NextBool(2))
                    flickerFrom = Main.rand.Next(1, 5);

                //HUGEs screenshake
                if (Main.LocalPlayer.WithinRange(NPC.Center, 1000f))
                    CameraManager.Shake = (1 - AttackTimer) * 26f;

                //Spawn scream waves
                if (screamParticleTimer == 0)
                {

                    if (Main.LocalPlayer.WithinRange(NPC.Center, 1400))
                    {
                        if (screamParticleTypeCounter % 4 == 0)
                            ParticleHandler.SpawnParticle(new CircularScreamRoar(VisualCenter, Color.RoyalBlue, 0.5f));
                        else
                        {
                            ParticleHandler.SpawnParticle(new CircularScreamRoarNonAdditive(VisualCenter, Color.DarkBlue, 0.5f));
                            ParticleHandler.SpawnParticle(new CircularScreamRoarNonAdditive(VisualCenter, Color.Black, 0.55f));
                        }
                    }

                    screamParticleTypeCounter++;
                    screamParticleTimer = 7;
                }
                else
                    screamParticleTimer--;

                //Exhude out spores (:hot:)
                if (AttackTimer <= 0.9f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                        Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);

                        Vector2 smokeCenter = NPC.Center + gushDirection * 35f * Main.rand.NextFloat(0.2f, 0.55f);
                        Vector2 velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f);

                        Particle smoke = new SporeGas(smokeCenter, velocity, NPC.Center, 400f, smokeSize, 0.01f);
                        smoke.FrontLayer = false;
                        ParticleHandler.SpawnParticle(smoke);
                    }


                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(130f, 70f);
                        Vector2 dustVel = NPC.Center.SafeDirectionTo(dustPos) * Main.rand.NextFloat(0.5f, 1.6f) - Vector2.UnitY * 0.4f;

                        Dust d = Dust.NewDustPerfect(dustPos, DustID.MushroomSpray, dustVel);
                        d.scale = Main.rand.NextFloat(0.8f, 1.2f);

                        if (Main.rand.NextBool(5))
                            d.noGravity = false;
                    }
                }

                AttackTimer -= 1 / (60f * 1.7f);

                float motionMultiplier = Utils.GetLerpValue(1f, 0.8f, AttackTimer, true) * Utils.GetLerpValue(0f, 0.1f, AttackTimer, true);
                visualRotationExtra = (float)Math.Sin(AttackTimer * MathHelper.TwoPi * 3f) * 0.12f * motionMultiplier;
                visualOffsetExtra = Main.rand.NextVector2Circular(12f, 12f) * NPC.scale * motionMultiplier;

                if (AttackTimer <= 0)
                {
                    NPC.dontTakeDamage = false;
                    AttackTimer = 1;
                    SubState = ActionState.SpawningUp_Accelerate;
                }
            }

            else if (SubState == ActionState.SpawningUp_Accelerate)
            {
                ContinueMoving(true, 1 - AttackTimer, true);
                AttackTimer -= 1 / (60f * 0.5f);
            }

            return AttackTimer <= 0;
        }

        public void DigUpEffects(int rumbleCount)
        {
            //Search up for the surface when crawling out of the ground
            int direction = -1;
            Point tilePos = NPC.Center.ToTileCoordinates();

            //If we are inside solid tiles, search down instead
            if (!Main.tile[tilePos].IsTileSolid())
                direction = 1;

            //Search for ground
            int tries = 0;
            while (Main.tile[tilePos].IsTileSolid() || !Main.tile[tilePos + new Point(0, 1)].IsTileSolid())
            {
                tries++;
                if (tries > 20)
                    return;
                tilePos.Y += direction;
            }

            Vector2 worldPos = tilePos.ToWorldCoordinates();
            float radius = NPC.width / 2f * NPC.scale * 2.5f;

            Point topLeft = tilePos - new Point((int)(radius / 16f), 4);
            Point bottomRight = tilePos + new Point((int)(radius / 16f), 3);

            for (int i = topLeft.X; i <= bottomRight.X; i++)
            {
                for (int j = topLeft.Y; j <= bottomRight.Y; j++)
                {
                    float distance = Vector2.Distance(worldPos, new Vector2(i * 16 + 8, j * 16 + 8));
                    if (distance > radius)
                        continue;


                    Tile stompedTile = Framing.GetTileSafely(i, j);
                    Tile stompedTileAbove = Framing.GetTileSafely(i, j - 1);

                    if (!stompedTile.HasUnactuatedTile)
                        continue;

                    bool solidTileAbove = stompedTileAbove.HasUnactuatedTile && Main.tileSolid[stompedTileAbove.TileType];
                    bool solidTopTileAbove = stompedTileAbove.HasUnactuatedTile && (Main.tileSolid[stompedTileAbove.TileType] || Main.tileSolidTop[stompedTileAbove.TileType]);
                    bool isHalfSolid = Main.tileSolidTop[stompedTile.TileType];

                    if ((Main.tileSolid[stompedTile.TileType] && solidTileAbove) || (isHalfSolid && solidTopTileAbove))
                        continue;

                    if (rumbleCount >= 2)
                    {
                        if (!isHalfSolid && Main.tileSolid[stompedTile.TileType])
                        {
                            float scale = (float)Math.Pow(Utils.GetLerpValue(0f, radius, distance, true), 1.6f);
                            float heightScale = scale * Utils.GetLerpValue(radius, radius - 48f, distance, true);
                            ParticleHandler.SpawnParticle(new BouncingTileParticle(new Point(i, j), (int)(scale * 20), 20, 4f + 8f * heightScale));
                        }
                    }

                    int dustCount = WorldGen.KillTile_GetTileDustAmount(fail: true, stompedTile, i, j);

                    for (int k = 0; k < dustCount / 2; k++)
                    {
                        Dust tileBreakDust = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, stompedTile)];
                        tileBreakDust.velocity.Y -= 3f + (float)rumbleCount * 1.5f;
                        tileBreakDust.velocity.Y *= Main.rand.NextFloat();
                        tileBreakDust.velocity.Y *= 0.75f;
                        tileBreakDust.scale += (float)rumbleCount * 0.03f;
                    }

                    if (rumbleCount >= 2)
                    {
                        for (int m = 0; m < dustCount / 2 - 1; m++)
                        {
                            Dust tileBreakDust2 = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, stompedTile)];
                            tileBreakDust2.velocity.Y -= 1f + (float)rumbleCount;
                            tileBreakDust2.velocity.Y *= Main.rand.NextFloat();
                            tileBreakDust2.velocity.Y *= 0.75f;
                        }
                    }
                }
            }
        }
        #endregion

        #region Death
        public float deathHitTimer = 0;
        public void DeathRagdoll()
        {
            //Continously cleanse itself from any buffs and debuffs
            for (int i = 0; i < NPC.maxBuffs; i++)
                NPC.buffTime[i] = 0;

            flickerBackground = 5;
            flickerFrom = Main.rand.Next(1, 5);

            if (SubState == ActionState.Dead)
            {
                NPC.HitSound = FlimsyHitSound;
                desperationStartRotation = Main.rand.NextFloat(0.6f, 1f) * MathHelper.PiOver4 * lastHitDirection;

                if (!Main.dedServ)
                {
                    foreach (CrabulonLeg leg in Legs)
                    {
                        if (!leg.GoLimp())
                            continue;

                        leg.LimpingLeg.points[2].position.Y -= Main.rand.NextFloat(24f, 60f);

                        Vector2 legTip = leg.LimpingLeg.points[2].position;
                        Vector2 legVelocity = new Vector2((legTip.X - NPC.Center.X).NonZeroSign() * 40f + NPC.velocity.X * Main.rand.NextFloat(0.2f, 0.8f), -Main.rand.NextFloat(20f, 60f));

                        leg.LimpingLeg.points[2].oldPosition = leg.LimpingLeg.points[2].position - legVelocity.RotatedByRandom(0.4f);
                    }

                    foreach (CrabulonArm arm in Arms)
                        arm.GoLimp();

                    if (Rack.StringCount > 0)
                    {
                        SoundEngine.PlaySound(Items.DesertScourgeDrops.StormlionWhip.YankSound, NPC.Center);
                        if (Main.LocalPlayer.Distance(NPC.Center) < 1300)
                            CameraManager.Quake += 10;
                    }

                    while (Rack.StringCount > 0)
                        Rack.SnapString(true);

                    //This little thing is needed to make the ropes unextended again
                    Ropes.position.Y += 400;
                    Ropes.UpdatePositions();
                    Ropes.CutSkewers();
                    Ropes.SmoothenCurvesAgain(1400f / 1600f);
                }

                NPC.behindTiles = true;
                TopDownView = false;
                NPC.velocity.Y -= 5;
                ExtraMemory = 0;
                desperationStartRotation = Main.rand.NextFloat(0.6f, 1f) * MathHelper.PiOver4 * (NPC.Center.X - Main.player[NPC.FindClosestPlayer()].Center.X).NonZeroSign();

                SubState = ActionState.Dead_Limping;
            }

            AttackTimer += 1 / (60f * 2f);
            if (AttackTimer > 0.5f)
                NPC.dontTakeDamage = false;

            if (ExtraMemory == 0)
            {
                ChasingMovement(false, 0f, true);
                visualRotationExtra = (Main.rand.NextFloat(-0.05f, 0.05f) + desperationStartRotation) * (float)Math.Pow(AttackTimer, 3f);
            }

            if (deathHitTimer == 1)
                flickerOpacity = 1;

            visualRotation += desperationStartRotation * 0.16f;

            visualRotationExtra += lastHitDirection * 0.4f * PolyInOutEasing(deathHitTimer, 1.4f);
            deathHitTimer -= 0.05f;
            if (deathHitTimer < 0)
                deathHitTimer = 0;

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

            if (AttackTimer > 7.5f)
            {
                SoundEngine.PlaySound(NPC.HitSound, NPC.Center);
                SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);
                if (!Main.dedServ)
                    DeadHitEffect();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.StrikeInstantKill();
            }
        }

        public void DeadHitEffect()
        {
            SoundEngine.PlaySound(DesertWormBoss.DesertScourge.MandibleTwitchSound, NPC.Center);

            foreach (CrabulonLeg leg in Legs)
            {
                Vector2 legTip = leg.LimpingLeg.points[2].position;
                leg.LimpingLeg.points[2].oldPosition += Vector2.UnitY * Main.rand.NextFloat(3f, 20f) * 1f + Main.rand.NextFloat(-7f, 7f) * Vector2.UnitX;

                float distanceToAnchor = Math.Abs(legTip.X - leg.LimpingLeg.points[0].position.X);
                int directionToAnchor = (legTip.X - leg.LimpingLeg.points[0].position.X).NonZeroSign();

                //BOunce outwards if too close
                if (distanceToAnchor < 120f * NPC.scale)
                    leg.LimpingLeg.points[2].oldPosition += Vector2.UnitX * (legTip.X - VisualCenter.X).NonZeroSign() * Main.rand.NextFloat(6f, 16f);

                //Bounce inwards if too far
                if (distanceToAnchor > leg.maxLenght * 0.9f)
                    leg.LimpingLeg.points[2].oldPosition -= Vector2.UnitX * directionToAnchor * Main.rand.NextFloat(2f, 26f);
            }

            for (int i = 0; i < 16; i++)
            {
                Particle fiber = new MyceliumStrandParticle(VisualCenter + Main.rand.NextVector2Circular(112f, 35f) * NPC.scale);
                ParticleHandler.SpawnParticle(fiber);
                fiber.Velocity = fiber.Velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 1.6f);
                fiber.Scale *= Main.rand.NextFloat(0.8f, 1.3f);
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 sporePos = VisualCenter + Main.rand.NextVector2Circular(85f, 35f) * NPC.scale;
                Vector2 sporeVel = -Vector2.UnitY.RotatedByRandom(1.5f) * Main.rand.NextFloat(0.5f, 2f);
                SporeGas particle = new SporeGas(sporePos, sporeVel, VisualCenter, 100f, Main.rand.NextFloat(0.9f, 2.5f), 0.01f);
                particle.dustSpawnRate = 0f;
                particle.counter = 77;
                ParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(130f, 70f);
                Vector2 dustVel = NPC.Center.SafeDirectionTo(dustPos) * Main.rand.NextFloat(0.5f, 1.6f) - Vector2.UnitY * 0.4f;

                Dust d = Dust.NewDustPerfect(dustPos, DustID.MushroomSpray, dustVel);
                d.scale = Main.rand.NextFloat(0.8f, 1.2f);

                if (Main.rand.NextBool(5))
                    d.noGravity = false;
            }
        }
        #endregion

        #region Despawning & raving
        public bool PlayerKilledNear(float distanceTreshold)
        {
            return (NPC.target >= 0 && NPC.target < Main.maxPlayers && (Main.player[NPC.target].dead || !Main.player[NPC.target].active)) && Main.player[NPC.target].WithinRange(NPC.Center, distanceTreshold);
        }

        public void DespawnBehavior()
        {
            if (SubState == ActionState.Despawning)
            {
                AttackTimer = 1;

                //If crabulon has no targets, dance on their graves
                if (PlayerKilledNear(16f * 50f) && IsThereAStraightforwardPath(30, 444f, out bool falling))
                {
                    SubState = ActionState.Raving;
                    movementTarget = target.Center;
                }

                //IF the player is simply not in the correct biome but is otherwise close enough and alive
                else if (!HasNoValidTarget)
                {
                    SubState = ActionState.DespawningScurrySlowly;
                    ExtraMemory = 0;
                }

                //if no corpse nearby, just despawn but without the possibility to go back (signaled by extramemory = 1
                else
                {
                    SubState = ActionState.DespawningScurrySlowly;
                    ExtraMemory = 1;
                }

            }

            //Slow despawn at the start, there is a chance for the player to go back and make it undespawn
            if (SubState == ActionState.DespawningScurrySlowly)
            {
                TopDownView = false;
                lookingSideways = false;

                flickerBackground = 5;
                flickerFrom = 0;
                flickerOpacity = MathF.Sin(AttackTimer * 6f) * 0.5f + 0.5f;

                NPC.velocity.X *= 0.99f;
                NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, 3f, 0.02f);
                AttackTimer -= 1 / (60f * 2f);

                //If we no longer have a valid target (target died in the meanwhile, or got out of the range, or something like that, stop being able to un-despawn
                if (HasNoValidTarget)
                    ExtraMemory = 1;

                if (ExtraMemory == 0 && biomeDespawnTimer > 0)
                {
                    AIState = ActionState.Chasing;
                    return;
                }

                //Despawn for real
                if (AttackTimer <= 0)
                {
                    AttackTimer = 1;
                    if (NPC.velocity.Y < 0)
                        NPC.velocity.Y = 0;
                    SubState = ActionState.DespawningScurryFast;
                }
            }

            //Speedrun downwards after a while, won't un-despawn from that
            if (SubState == ActionState.DespawningScurryFast)
            {
                NPC.dontTakeDamage = true;
                topDownView = true;
                NPC.velocity.X *= 0.95f;
                NPC.velocity.Y += 0.6f * (float)Math.Pow(1 - AttackTimer, 0.2f);

                AttackTimer -= 1 / 60f;
                if (AttackTimer <= 0)
                    NPC.active = false;
            }

            //Fall down if we despawned from the desperation phase
            if (SubState == ActionState.DespawningDropDownDesperation)
            {
                NPC.velocity.Y += 0.5f + (1 - AttackTimer) * 2f;
                AttackTimer -= 1 / (60f * 1.8f);
                if (AttackTimer <= 0)
                    NPC.active = false;
            }
        }

        public void RaveOnACorpse()
        {
            if (SubState == ActionState.Raving)
            {
                AttackTimer = 1;
                //Pick a random dance on initialization
                SubState = (ActionState)Main.rand.Next((int)ActionState.Raving_Hardswap, (int)ActionState.Raving_Tapdance + 1);
                TopDownView = false;
                lookingSideways = false;
            }

            ContinueMoving(false, 1.4f);

            AttackTimer -= 1 / (60f * 7f);

            //Despawn immediately if a target comes back
            if (!HasNoValidTarget)
                AttackTimer = 0;

            if (AttackTimer <= 0)
            {
                AttackTimer = 1;
                SubState = ActionState.DespawningScurryFast;
            }    
        }

        public void RaveDance()
        {
            float raveStrenght = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(1f, 0.8f, AttackTimer, true));

            float danceSpeedMult = Main.getGoodWorld ? 3f : 1f;

            //Rave mode 1 (unsync dance)
            if (SubState == ActionState.Raving_Hardswap)
            {
                visualOffset.Y = 14f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 6.7f * danceSpeedMult) * 0.5 + 0.5);
                float oscillate = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f * danceSpeedMult);
                visualOffset.X = FablesUtils.PolyOutEasing(Math.Abs(oscillate), 1.4f) * 50f * Math.Sign(oscillate);
                visualRotation = oscillate * 0.1f;

                visualOffset *= NPC.scale;
            }
            //Rave mode 2 (circling)
            if (SubState == ActionState.Raving_Circling)
            {
                float oscillate = Main.GlobalTimeWrappedHourly * 8f * danceSpeedMult;
                visualOffset = oscillate.ToRotationVector2() * 42f * NPC.scale;
                visualOffset.Y *= 0.8f;
                visualRotation = visualOffset.X * 0.003f;
            }
            //Rave mode 3 (side cha cha)
            if (SubState == ActionState.Raving_SideChaCha)
            {
                float oscillate = Main.GlobalTimeWrappedHourly * 8f * danceSpeedMult;
                visualOffset = oscillate.ToRotationVector2() * 70f * NPC.scale;
                visualOffset.Y *= 0.5f;
                if (visualOffset.Y > 0)
                    visualOffset.Y *= -1;

                visualRotation = visualOffset.X * 0.001f;
            }
            //Rave mode 3 (synced up down)
            if (SubState == ActionState.Raving_Tapdance)
            {
                float oscillate = Main.GlobalTimeWrappedHourly * 12f * danceSpeedMult;

                visualOffset.X = (float)Math.Sin(oscillate * 0.5f) * 46f;
                visualOffset.Y = (float)Math.Sin(oscillate) * 30f;

                visualOffset.X *= NPC.scale;
                visualRotation = -visualOffset.X * 0.005f;
            }

            visualOffset *= raveStrenght;
            visualRotation *= raveStrenght;
        }
        #endregion

        #region Clentaminator easter egg
        public void CheckForClentaminator()
        {
            if (AIState == ActionState.SpawningUp || AIState == ActionState.ClentaminatedAway || AIState == ActionState.Dead)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ProjectileID.PureSpray && p.Hitbox.Intersects(NPC.Hitbox))
                {
                    SubState = ActionState.ClentaminatedAway;
                    NPC.netUpdate = true;
                    break;
                }
            }
        }

        public float originalClentScale = 1f;

        public void ClentaminatorDeath()
        {
            if (SubState == ActionState.ClentaminatedAway)
            {
                NPC.dontTakeDamage = true;
                NPC.life = 1;
                AttackTimer = 1;
                SubState = ActionState.ClentaminatedAway_DieHorribly;

                SoundEngine.PlaySound(ShriekSound with { Pitch = 0.6f }, NPC.Center);
                SoundEngine.PlaySound(ShriekSound with { Pitch = 0.2f }, NPC.Center);
                SoundEngine.PlaySound(ShriekSound with { Pitch = -0.44f }, NPC.Center);

                if (Main.netMode == NetmodeID.SinglePlayer)
                    WorldProgressionSystem.crabulonsDefeated++;
                else if (Main.netMode == NetmodeID.Server)
                    new IncreasedCrabulonDefeatCountPacket().Send();

                NPC.velocity *= 0.5f;
                screamParticleTimer = 0;
                originalClentScale = NPC.scale;
            }

            if (SubState == ActionState.ClentaminatedAway_DieHorribly)
            {
                NPC.velocity *= 0.95f;

                AttackTimer -= 1 / (60f * 4f);
                visualOffsetExtra = Main.rand.NextVector2Circular(16f, 16f) * (1 - AttackTimer);
                visualRotationExtra = Main.rand.NextFloat(-0.6f, 0.6f) * (1 - AttackTimer);

                if (AttackTimer < 0.5f)
                    NPC.scale = MathHelper.Lerp(originalClentScale, 0.4f, PolyInOutEasing(Utils.GetLerpValue(0.5f, 0, AttackTimer, true), 2f));

                if (CameraManager.PanMagnet.SetMagnetPositionAndImmunityForEveryone(Vector2.Lerp(VisualCenter, Main.LocalPlayer.Center, 0.3f), NPC.Center, 1300f))
                {
                    CameraManager.PanMagnet.PanProgress += 0.02f;
                    CameraManager.Shake = Math.Max(CameraManager.Shake, 30f * (1 - AttackTimer));
                }

                screamParticleTimer++;
                if (screamParticleTimer % 30 == 0)
                {
                    SoundEngine.PlaySound(ThumpDigSound, NPC.Center);
                    SoundEngine.PlaySound(TwitchSound, NPC.Center);

                    ParticleHandler.SpawnParticle(new CircularScreamRoarNonAdditive(VisualCenter, Color.Lerp(Color.RoyalBlue, Color.LimeGreen, 1 - AttackTimer), 0.5f));
                    ParticleHandler.SpawnParticle(new CircularScreamRoar(VisualCenter, Color.Lerp(Color.RoyalBlue, Color.LimeGreen, 1 - AttackTimer) * 0.6f, 0.5f));
                }



                for (int i = 0; i < 2; i++)
                {
                    float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                    Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);

                    Vector2 smokeCenter = NPC.Center + gushDirection * 35f * Main.rand.NextFloat(0.2f, 0.55f);
                    Vector2 velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f);

                    Particle smoke = new JungleSporeGas(smokeCenter, velocity, NPC.Center, 400f, smokeSize, 0.01f, (1 - AttackTimer * 0.5f) - Main.rand.NextFloat(0f, 0.5f));
                    smoke.FrontLayer = false;
                    ParticleHandler.SpawnParticle(smoke);
                }


                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(130f, 70f);
                    Vector2 dustVel = NPC.Center.SafeDirectionTo(dustPos) * Main.rand.NextFloat(0.5f, 1.6f) - Vector2.UnitY * 0.4f;

                    int dustType = Main.rand.NextFloat() < AttackTimer ? DustID.MushroomSpray : (Main.rand.NextBool() ? DustID.PureSpray : DustID.JungleSpore);

                    Dust d = Dust.NewDustPerfect(dustPos, dustType, dustVel);
                    d.scale = Main.rand.NextFloat(0.8f, 1.2f);

                    if (Main.rand.NextBool(5))
                        d.noGravity = false;
                }

                if (AttackTimer <= 0)
                {
                    NPC.active = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Item.NewItem(NPC.GetSource_Death(), NPC.Center, Vector2.Zero, ItemID.Mushroom);
                        NPC crab = NPC.NewNPCDirect(NPC.GetSource_Death(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.Crab);
                        crab.SpawnedFromStatue = true;
                        crab.GivenName = "Crabulon";

                        if (Main.dedServ)
                        {
                            new RenameNPCPacket(crab).Send();
                            new NPCFromStatuePacket(crab).Send();
                        }
                    }

                    ParticleHandler.SpawnParticle(new CircularPulseShine(VisualCenter, Color.GreenYellow, 4f));
                }
            }
        }
        #endregion

        #region Spawn card
        private bool playedIntroCard;
        public bool PlayedIntroCard {
            get => playedIntroCard;
            set => playedIntroCard = value;
        }

        public BossIntroCard GetIntroCard {
            get {
                return new BossIntroCard("Crabulon", (int)(1.9f * 60), NPC.Center.X < Main.LocalPlayer.Center.X,

                Color.Blue * 0.5f,
                Color.Lerp(Color.White, Color.Blue, 0.5f),
                new Color(0, 40, 255),
                Color.DodgerBlue
                )
                { music = MusicUsedInfo };
            }
        }

        public bool ShouldPlayIntroCard => SubState == ActionState.SpawningUp_Scream;

        public MusicTrackInfo MusicUsedInfo => SoundHandler.UseCalamityMusic ? 
            new MusicTrackInfo("1NF3S+@+!0N", "DM DOKURO") : SoundHandler.UseVanillaMusic?
            new MusicTrackInfo("Behold the Octoeye", "Jonathan Van Den Wijngaarden") :
            new MusicTrackInfo("Mycelium Marionette", "Salvati & Sbubby");

        #endregion

        public static float TrippyIntensity;
        public static float ShaderOpacity;

        public static bool ForceShaderActive;
        public static bool TrippinessAfflicted;

        private void SetScreenshaderEffect()
        {
            bool shouldShaderBeActive = ForceShaderActive;
            bool afflicted = TrippinessAfflicted;
            float minTrippyFade = 0f;

            if (!Main.gameMenu)
            {
                //If the player has the DOT, activate the shader and consider it trippy
                if (!afflicted && Main.LocalPlayer.HasBuff<CrabulonDOT>())
                {
                    afflicted = true;
                    shouldShaderBeActive = true;
                }

                //Check for crabulon if the shader isn't already being activated
                if (!shouldShaderBeActive && !afflicted)
                {
                    NPC npc = Main.npc.Where(n => n.active && n.type == Type && n.Distance(Main.LocalPlayer.Center) < 5000f).OrderBy(n => n.Distance(Main.LocalPlayer.Center)).FirstOrDefault();
                    if (npc != null && npc != default(NPC))
                    {
                        shouldShaderBeActive = true;

                        Crabulon crab = npc.ModNPC as Crabulon;
                        if (crab.AIState == ActionState.SpawningUp && (int)crab.SubState < (int)ActionState.SpawningUp_Scream)
                            shouldShaderBeActive = false;
                        else if (crab.AIState == ActionState.Dead)
                            shouldShaderBeActive = false;

                        if (crab.AIState == ActionState.Desperation && (int)crab.SubState > (int)ActionState.Desperation_CinematicWait)
                        {
                            float desperationPercent = (npc.life / (float)npc.lifeMax) / (float)Crabulon.DesperationPhaseTreshold;
                            minTrippyFade = (1 - desperationPercent) * 0.5f;
                        }
                    }
                }


                //Ramp up the intensity if afflicted by trippiness
                if (afflicted)
                {
                    TrippyIntensity += 0.02f;
                    if (TrippyIntensity > 1f)
                        TrippyIntensity = 1f;
                }

            }

            //Make the shader fade in and out
            if (shouldShaderBeActive)
            {
                ShaderOpacity += 0.08f;
                if (ShaderOpacity > 1f)
                    ShaderOpacity = 1f;
            }
            else
            {
                ShaderOpacity -= 0.02f;
                if (ShaderOpacity < 0f)
                    ShaderOpacity = 0f;
            }

            if (shouldShaderBeActive && !Scene["CalamityFables:CrabulonPsychosis"].IsActive())
                Scene.Activate("CalamityFables:CrabulonPsychosis");
            else if (!shouldShaderBeActive && Scene["CalamityFables:CrabulonPsychosis"].IsActive())
                Scene.Deactivate("CalamityFables:CrabulonPsychosis");

            //Tick down affliction visuals if its not being force activated / no dot
            if (!afflicted)
            {
                TrippyIntensity -= 0.02f;
                if (TrippyIntensity < minTrippyFade)
                    TrippyIntensity = minTrippyFade;
            }
        }
    }

    public class ScreenSporeGas : ScreenParticle
    {
        public override string Texture => AssetDirectory.Particles + "Smoke";

        public int counter;
        public float Spin;

        public override bool SetLifetime => false;
        public override int FrameVariants => 3;
        public override bool Important => true;


        public ScreenSporeGas(Vector2 position, Vector2 velocity, float scale, float rotationSpeed = 0f, float parallax = 0f)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            counter = Main.rand.Next(80);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Variant = Main.rand.Next(3);

            OriginalScreenPosition = Main.screenPosition;
            ParallaxRatio = parallax;
            WrapHorizontal = true;
        }

        public override void Update()
        {
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);

            //Fly up
            if (Velocity.Y > -2 && counter > 140)
                Velocity.Y -= 0.05f * Math.Min(1f, (counter - 140) / 60f);

            //Fade out
            if (counter > 100)
            {
                Scale += 0.01f;
                counter += 2;
            }
            else
            {
                Scale *= 0.985f;
                counter += 4;
            }
            //Dissapear
            if (counter >= 255)
                Kill();


            Lighting.AddLight(Position, Color.ToVector3() * 5.5f);

            //Slowly fade in
            float opacity = 0.1f + 0.15f * Math.Min(1f, counter / 90f);
            opacity *= 0.4f + 0.6f * (1 - ParallaxRatio);

            Color = new Color(30, 32, 176) * opacity;
            Color.A = (byte)(Math.Min(Color.A * 0.5f, 80)); //Color is always at least a bit glowy

            //Fade off in terms of opacity
            if (counter > 150)
                Color *= (float)Math.Pow(1 - (counter - 150) / 105f, 1.2f);

            //Slightly turn into the bg color
            Color backgroundColor = Lighting.GetColor(Position.ToTileCoordinates());
            Color = Color.Lerp(Color, Color.MultiplyRGBA(backgroundColor), 0.7f);

            //Start bright and fade away
            if (counter < 150)
            {
                byte oldA = Color.A;
                Color *= 1 + 2f * (float)Math.Pow(1 - counter / 150f, 4);
                Color.A = oldA;
            }
        }
    }

    [Serializable]
    public class IncreasedCrabulonDefeatCountPacket : Module
    {
        protected override void Receive() => WorldProgressionSystem.crabulonsDefeated++;
    }
}
