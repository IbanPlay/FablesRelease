using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Items.EarlyGameMisc;
using CalamityFables.Content.Items.Food;
using CalamityFables.Content.NPCs.Wulfrum;
using CalamityFables.Helpers;
using CalamityFables.Particles;
using Mono.Cecil;
using ReLogic.Utilities;
using System;
using System.Data.OleDb;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.NPCs.Desert
{
    [ReplacingCalamity("Stormlion")]
    public class Stormlion : ModNPC
    {
        public const int DEATH_BLAST_RADIUS = 85;

        public override string Texture => AssetDirectory.DesertNPCs + Name;
        public static Asset<Texture2D> ChargedTexture;
        public static Asset<Texture2D> OutlineTexture;

        public static readonly SoundStyle HitSound = new SoundStyle(SoundDirectory.Sounds + "StormlionHit", 3);
        public static readonly SoundStyle DeathSound = new SoundStyle(SoundDirectory.Sounds + "StormlionNonElectricDeath");
        public static readonly SoundStyle ElectricDeathSound = new SoundStyle(SoundDirectory.Sounds + "StormlionDeath");
        public static readonly SoundStyle AmbientSound = new SoundStyle(SoundDirectory.Sounds + "StormlionAmbient", 2) { Volume = 0.3f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, MaxInstances = 1 };
        public static readonly SoundStyle BurrowSound = new SoundStyle(SoundDirectory.Sounds + "StormlionBurrow");
        public static readonly SoundStyle ElectrifySound = new SoundStyle(SoundDirectory.Sounds + "StormlionThunderStrike");
        public static readonly SoundStyle ElectroArcSound = new SoundStyle(SoundDirectory.Sounds + "StormlionArc", 3) { SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest, MaxInstances = 2 };


        public Player target => Main.player[NPC.target];

        public List<Vector2> positionCache = new List<Vector2>();

        public bool aggravated;

        public bool Electrified
        {
            get => NPC.ai[0] == 1;
            set
            {
                float lastValue = NPC.ai[0];
                NPC.ai[0] = value ? 1 : 0;
                if (lastValue != NPC.ai[0])
                    NPC.netUpdate = true;
            }
        }

        public bool Burrowing
        {
            get => NPC.ai[1] >= 1;
            set => NPC.ai[1] = value ? 1 : 0;
        }

        public float BurrowTransitionProgress
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public ref float LeapCooldown => ref NPC.ai[2];
        public ref float SuperchargeDelay => ref NPC.ai[3];

        public static int BannerType;
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Stormlion", AssetDirectory.Banners, out AutoloadedBanner bannerTile);
            bannerTile.NPCType = Type;

            FablesNPC.ApplyCollisionEvent += SandSwimCollision;
            FablesNPC.ModifyCollisionParametersEvent += ShrinkHitbox;
            FablesNPC.DisableSlopesEvent += DisableSlopesWhenSwimming;
            FablesNPC.EditSpawnRateEvent += IncreaseSpawnRatesInStormyDeserts;
        }

        private bool DisableSlopesWhenSwimming(NPC npc) => npc.type == Type && (npc.ModNPC as Stormlion).Burrowing;
      
        private void ShrinkHitbox(NPC npc, ref Vector2 collisionPosition, ref int collisionWidth, ref int collisionHeight)
        {
            //Shave off the top
            if (npc.type == Type)
            {
                collisionHeight -= 20;
                collisionPosition.Y += 20;
            }
        }

        private bool SandSwimCollision(NPC npc, bool fall, Vector2 collisionPosition, int collisionWidth, int collisionHeight)
        {
            //Chance collision to match whats happening (ignore all sand tiles or ignore rolling cacti)
            if (npc.type == Type)
            {
                if ((npc.ModNPC as Stormlion).Burrowing)
                    npc.velocity = Collision.AdvancedTileCollision(FablesSets.ForAdvancedCollision.StormlionBurrowIgnore, collisionPosition, npc.velocity, collisionWidth, collisionHeight, fall);
                else
                    npc.velocity = Collision.AdvancedTileCollision(FablesSets.ForAdvancedCollision.StormlionRegularIgnore, collisionPosition, npc.velocity, collisionWidth, collisionHeight, fall);

                return true;
            }

            return false;
        }


        private void IncreaseSpawnRatesInStormyDeserts(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.ZoneDesert && !player.ZoneUndergroundDesert && Main.IsItStorming)
            {
                spawnRate = (int)(spawnRate * 0.75f);
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion");
            Main.npcFrameCount[Type] = 5;


            //Does this because the graveyards use fossils. Not related tho
            TileID.Sets.ForAdvancedCollision.ForSandshark[TileID.DesertFossil] = true;
            TileID.Sets.ForAdvancedCollision.ForSandshark[TileID.GoldBrick] = true;
            TileID.Sets.ForAdvancedCollision.ForSandshark[TileID.SandstoneBrick] = true;

            //Swim through tiles in the savanna
            if (CalamityFables.SpiritReforged != null && ModContent.TryFind("SpiritReforged/SavannaDirt", out ModTile dirt))
            {
                TileID.Sets.ForAdvancedCollision.ForSandshark[dirt.Type] = true;
                if (ModContent.TryFind("SpiritReforged/SavannaGrass", out ModTile grass))
                    TileID.Sets.ForAdvancedCollision.ForSandshark[grass.Type] = true;
                if (ModContent.TryFind("SpiritReforged/SavannaGrassCorrupt", out grass))
                    TileID.Sets.ForAdvancedCollision.ForSandshark[grass.Type] = true;
                if (ModContent.TryFind("SpiritReforged/SavannaGrassCrimson", out grass))
                    TileID.Sets.ForAdvancedCollision.ForSandshark[grass.Type] = true;
                if (ModContent.TryFind("SpiritReforged/SavannaGrassHallow", out grass))
                    TileID.Sets.ForAdvancedCollision.ForSandshark[grass.Type] = true;

            }
        }

        public override void SetDefaults()
        {
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 20;
            NPC.width = 62;
            NPC.height = 50;
            NPC.defense = 10;
            NPC.lifeMax = 120;
            NPC.knockBackResist = 0.2f;
            NPC.value = Item.buyPrice(0, 0, 3, 0);
            NPC.HitSound = HitSound;
            NPC.DeathSound = DeathSound;
            Banner = NPC.type;
            BannerItem = BannerType;
            NPC.soundDelay = Main.rand.Next(300, 600);

            NPC.direction = Main.rand.NextBool() ? 1 : -1;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundDesert,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.Rain,
                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.Stormlion")
            });
        }

        public override int SpawnNPC(int tileX, int tileY)
        {
            EntitySource_SpawnNPC source = new EntitySource_SpawnNPC();

            if (Main.rand.NextBool())
            {
                int babyCount = Main.rand.Next(1, 3);
                for (int i = 0; i < babyCount; i++)
                    NPC.NewNPC(source, tileX * 16 + (int)Main.rand.NextFloat(-16f, 16f), tileY * 16, ModContent.NPCType<StormlionLarva>());
            }

            int electrified = 0;
            if (Main.IsItStorming && tileY < Main.worldSurface && Main.rand.NextBool(3))
                electrified = 1;

            return NPC.NewNPC(source, tileX * 16, tileY * 16, NPC.type, 0, electrified, ai3: Main.rand.Next(100, 300));
        }

        public override void AI()
        {
            if (Electrified)
                NPC.DeathSound = ElectricDeathSound;

            NPC.TargetClosest(false);
            bool lineOfSight = Collision.CanHitLine(target.position, target.width, target.height, NPC.Top, 1, 1);
            bool broadLineOfSight = lineOfSight;

            //Extra check for line of sight seen from above to prevent small hills from making it so the stormlion loses LOS
            if (!broadLineOfSight && Collision.CanHitLine(NPC.Top, 1, 1, NPC.Center - Vector2.UnitY * 100f, 1, 1))
            {
                broadLineOfSight = Collision.CanHitLine(target.position, target.width, target.height, NPC.Center - Vector2.UnitY * 100f, 1, 1) ;
            }

            //Aggravated is just to know if it should consider itself having entered combat at all already
            if (!aggravated)
                aggravated = broadLineOfSight || NPC.life < NPC.lifeMax;

            float distanceToTarget = NPC.Distance(target.Center);
            float distanceToPlayerX = Math.Abs(target.Center.X - NPC.Center.X);

            //Tick down cooldowns
            if (LeapCooldown > 0)
                LeapCooldown -= 1 / 60f;

            //Animate into supercharge
            if (SuperchargeDelay < 0)
            {
                NPC.behindTiles = false;
                NPC.noGravity = false;

                float lastTimer = SuperchargeDelay;
                NPC.velocity.X *= 0.8f;
                //Charge when electrifying
                if (Math.Abs(NPC.velocity.X) < 0.1f || SuperchargeDelay > -1)
                {
                    NPC.velocity.X = 0f;
                    SuperchargeDelay += 1 / (60f * 1.7f);
                }

                if (lastTimer < -0.62f && SuperchargeDelay > -0.62f)
                {
                    SoundEngine.PlaySound(ElectrifySound, NPC.Center);
                    ElectrificationEffects();
                }

                if (SuperchargeDelay >= 0)
                {
                    Electrified = true;
                    SuperchargeDelay = 0;
                    NPC.netUpdate = true;
                }
            }

            else if (!Burrowing && BurrowTransitionProgress <= 0)
            {
                NPC.behindTiles = false;
                NPC.noGravity = false;

                float maxXSpeed = Electrified ? 6f : 4.5f;
                float acceleration = Electrified ? 0.04f : 0.03f;
                float jumpHeight = broadLineOfSight ? ( Electrified ? 10f : 8f ) : 7f; //Lower jump height if player isnt in LOS

                if (!broadLineOfSight)
                    maxXSpeed *= 0.6f;

                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

                //Change directions if close but not too close & line of sight
                if (distanceToPlayerX > 80 && distanceToPlayerX < 600 && broadLineOfSight)
                {
                    int direction = (target.Center.X - NPC.Center.X).NonZeroSign();
                    if (NPC.direction != direction)
                    {
                        NPC.direction = direction;
                        NPC.netSpam = 0;
                        NPC.netUpdate = true;
                    }
                }

                #region Horizontal Movement
                //If its already going faster than max speed, make the max speed ease from it, to let it do jumps that are faster than its max run speed
                if (Math.Abs(NPC.velocity.X) > maxXSpeed && NPC.velocity.Y != 0)
                    maxXSpeed = MathHelper.Lerp(Math.Abs(NPC.velocity.X), maxXSpeed, 0.1f);

                //When turning around, accelerate more as the NPC "skids" back in the right direction, and jump a little
                if (Math.Sign(NPC.velocity.X) != NPC.direction)
                {
                    acceleration *= 5f;
                    if (NPC.velocity.Y == 0 && Math.Abs(NPC.velocity.X) > maxXSpeed * 0.7f )
                        NPC.velocity.Y = -3;
                }

                NPC.velocity.X += acceleration * NPC.direction;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * maxXSpeed, 0.01f);
                NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxXSpeed, maxXSpeed);
                #endregion

                if (!Electrified && Main.IsItStorming)
                {
                    if (SuperchargeDelay > 0 && distanceToTarget < 1000)
                        SuperchargeDelay--;

                    if (SuperchargeDelay == 0 && NPC.position.Y / 16 < Main.worldSurface && NPC.velocity.Y == 0)
                    {
                        SuperchargeDelay = -1f;
                        NPC.velocity.X *= 0.6f;
                        NPC.netUpdate = true;
                    }
                }

                #region Pounce attack
                bool canPounce = lineOfSight && NPC.velocity.Y == 0 &&
                    distanceToTarget < 760 && distanceToPlayerX > 240 && //Close enough to player but not right up in their face either
                    target.Bottom.Y - 50 <= NPC.Bottom.Y &&              //Below the player (or at least close enough in elevation
                    Main.expertMode &&                                   //Only in expert
                    SuperchargeDelay >= 0;                               //If not supercharging                             
                    
                if (canPounce && LeapCooldown <= 0)
                {
                    Vector2 pounceVector = NPC.DirectionTo(target.Center - Vector2.UnitY * 14);
                    float pounceVerticality = Vector2.Dot(pounceVector, -Vector2.UnitY);
                    float horizontalLerper = Utils.GetLerpValue(0.6f, 0f, pounceVerticality, true); //Lerper for the horizontallness of a jump

                    float pounceSpeed = 10f + 4f * Utils.GetLerpValue(240, 400f, distanceToPlayerX, true);
                    pounceSpeed += horizontalLerper * 3f;

                    if (Electrified)
                        pounceSpeed *= 1.3f;


                    NPC.velocity = pounceVector * pounceSpeed;

                    //If the pounce is more horizontal, add extra Y velocity so it can still be a leap
                    NPC.velocity.Y -= horizontalLerper * 6f;


                    LeapCooldown = 2;
                    NPC.netUpdate = true;
                }
                #endregion

                //Jump if encountering a wall
                if ((NPC.collideX || NPC.oldPosition.X == NPC.position.X) && NPC.velocity.Y == 0 && SuperchargeDelay >= 0)
                {
                    NPC.velocity.Y = -jumpHeight;
                    LeapCooldown = Math.Max(LeapCooldown, 1);

                    //Swap directions if the player is on the other side / if we dont see the player and the npc was already agaisnt the wall
                    if ((target.Center.X - NPC.Center.X).NonZeroSign() != NPC.direction || (!broadLineOfSight && NPC.oldPosition.X == NPC.position.X))
                    {
                        NPC.direction *= -1;
                        NPC.velocity.X *= -1;

                        NPC.netUpdate = true;
                    }
                }

                // Jump if there's an gap ahead.
                if (NPC.velocity.Y == 0 && target.Top.Y < NPC.Bottom.Y && HoleAtPosition(NPC.Center.X + NPC.velocity.X * 4f) && SuperchargeDelay >= 0)
                {
                    NPC.velocity.Y = -jumpHeight;
                    LeapCooldown = Math.Max(LeapCooldown, 1);
                }


                #region Burrow
                //Wont burrow with LOS
                if (lineOfSight)
                    BurrowTransitionProgress = 0f;
                if (aggravated && !lineOfSight)
                    BurrowTransitionProgress -= 1 / 250f;

                //After enough time not having seen the player
                if (NPC.velocity.Y == 0 && CanBurrowInTiles() && BurrowTransitionProgress <= -1f && SuperchargeDelay >= 0)
                    BurrowTransitionProgress = 0.01f;
                #endregion

                //Tilt it during the jump
                NPC.spriteDirection = NPC.direction;
                NPC.rotation = -NPC.velocity.Y * 0.06f * NPC.direction;
                NPC.rotation = Math.Clamp(NPC.rotation, -0.4f, 0.4f);
            }

            //Transition into burrowing
            else if (!Burrowing && BurrowTransitionProgress > 0)
            {
                if (BurrowTransitionProgress > 0.5f)
                    NPC.behindTiles = true;

                float pastTransitionProgress = BurrowTransitionProgress;

                NPC.velocity.X *= 0.8f;
                if (Math.Abs(NPC.velocity.X) < 0.1f)
                {
                    NPC.velocity.X = 0f;
                    BurrowTransitionProgress += 1 / (60f * 0.7f);
                }

                if (pastTransitionProgress < 0.3f && BurrowTransitionProgress > 0.3f)
                    SoundEngine.PlaySound(BurrowSound, NPC.Center);

                NPC.spriteDirection = NPC.direction;
                NPC.rotation = -NPC.velocity.Y * 0.06f * NPC.direction;
                NPC.rotation = Math.Clamp(NPC.rotation, -0.4f, 0.4f);

                if (BurrowTransitionProgress >= 1)
                {
                    NPC.position.Y += 30;
                    NPC.velocity.Y = 2f;
                    BurrowTransitionProgress = 3;
                    LeapCooldown = -1;
                    NPC.netUpdate = true;
                }
            }

            else
            {
                Point npcTilePos = NPC.Center.ToTileCoordinates();
                bool canBurrow = WorldGen.SolidTile(npcTilePos);
                bool clearAbove = !WorldGen.SolidTile(npcTilePos - new Point(0, 2));
                bool clearAhead = !WorldGen.SolidTile((NPC.Center + NPC.velocity * 2f).ToTileCoordinates());

                NPC.behindTiles = true;
                NPC.noGravity = true;

                //Worm dig sound as it burrows
                if (NPC.soundDelay == 0)
                {
                    NPC.soundDelay = (int)Math.Clamp(distanceToTarget / 40f, 20,40);
                    SoundEngine.PlaySound(SoundID.WormDig with { Volume = 0.1f }, NPC.Center);
                }

                if (distanceToPlayerX > 200)
                    NPC.TargetClosest();

                if (canBurrow)
                {
                    float vSpeed = clearAbove ? 0.06f : 0.25f;

                    TelegraphSand(2f, 0.04f);

                    //Lunging out
                    if ((distanceToTarget < 200f && target.Bottom.Y < NPC.Bottom.Y && clearAhead) || LeapCooldown > 0)
                    {
                        if (LeapCooldown < 0)
                            NPC.velocity = NPC.DirectionTo(target.Center - Vector2.UnitY * 70f) * 13f;
                        LeapCooldown = 1f;
                    }

                    //just prowling 
                    else
                    {
                        NPC.velocity.X += NPC.direction * 0.15f;
                        NPC.velocity.Y += NPC.directionY * vSpeed;

                        float maxYSpeed = -5f;
                        if (NPC.velocity.Y < maxYSpeed)
                            maxYSpeed = MathHelper.Lerp(NPC.velocity.Y, maxYSpeed, 0.1f);


                        NPC.velocity.X = Math.Clamp(NPC.velocity.X, -7f, 7f);
                        NPC.velocity.Y = Math.Clamp(NPC.velocity.Y, maxYSpeed, 1f);

                        //if theres an empty space ahead, get a boost in speed
                        if (LeapCooldown < 0 && clearAhead)
                        {
                            NPC.velocity.X *= 1.6f;
                            if (NPC.directionY == -1)
                                NPC.velocity.Y -= 4f;
                        }
                    }
                }

                else
                {
                    //Continue the leap until we can emerge
                    if (LeapCooldown > 0)
                    {
                        if (CanEmerge())
                        {
                            BurrowTransitionProgress = 0f;
                            NPC.velocity.Y -= 0.2f;
                            LeapCooldown = 4f;
                        }
                    }
                    else
                    {
                        NPC.velocity.X += NPC.direction * 0.1f;
                        NPC.velocity.X = Math.Clamp(NPC.velocity.X, -5f, 5f);
                    }

                    //Tick down the progress (when it reaches 1 again it means its gonna be pushed to emerge when it can
                    BurrowTransitionProgress -= 1 / 60f;


                    NPC.velocity.Y += 0.3f;
                    if (NPC.velocity.Y > 10f)
                        NPC.velocity.Y = 10f;
                }

                //Emerge
                if (BurrowTransitionProgress <= 1f)
                {
                    if (CanEmerge())
                    {
                        BurrowTransitionProgress = 0f;
                        NPC.velocity.Y -= 0.2f;
                    }
                    else
                        BurrowTransitionProgress = 1f;

                }

                NPC.spriteDirection = NPC.direction;
                NPC.rotation = -NPC.velocity.Y * 0.06f * NPC.direction;
                NPC.rotation = Math.Clamp(NPC.rotation, -0.4f, 0.4f);
            }

            if (!Burrowing)
            {
                Vector3 glow = new Vector3(0.1f, 0.3f, 0.4f);
                if (Electrified)
                    glow = new Vector3(0.5f, 0.5f, 2f);
                Lighting.AddLight(NPC.Center, glow);
            }

            int dustChance = Electrified ? 5 : 10;
            if (Main.rand.NextBool(dustChance))
            {
                Dust d =Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(30f, 20f), 206, NPC.velocity * 0.1f, 200, Color.White, Main.rand.NextFloat(0.7f, 1.4f));
                d.noLightEmittence = !Main.rand.NextBool(6);
                d.noLight = true;
            }

            if (Electrified && Main.rand.NextBool(200))
                SpawnRandomLightningArc();

            positionCache.Add(NPC.Bottom);
            while (positionCache.Count > 20)
                positionCache.RemoveAt(0);

            for (int i = 0; i < positionCache.Count - 1; i++)
            {
                positionCache[i] = Vector2.Lerp(positionCache[i], positionCache[i + 1], 0.6f + (float)Math.Sin(Main.GlobalTimeWrappedHourly + i)* 0.3f);
            }

            if (NPC.soundDelay <= 0)
            {
                SoundEngine.PlaySound(AmbientSound, NPC.Center);
                NPC.soundDelay = Main.rand.Next(200, 600);
            }
        }

        /// <summary>
        /// Checks for if the ground under the stormlion is burrowable ground (Sand, fossils, etc)
        /// </summary>
        public bool CanBurrowInTiles()
        {
            Point NPCLeft = NPC.TopLeft.ToTileCoordinates();
            Point NPCRight = NPC.TopRight.ToTileCoordinates();

            for (int y = NPCLeft.Y; y < NPCLeft.Y + 10; y++)
            {
                for (int x = NPCLeft.X; x < NPCRight.X; x++)
                {
                    if (WorldGen.SolidTile(x, y) && !FablesSets.ForAdvancedCollision.StormlionBurrowIgnore[Main.tile[x, y].TileType])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the NPC is fully outside non-navigable tiles
        /// </summary>
        public bool CanEmerge()
        {
            Point NPCLeft = NPC.TopLeft.ToTileCoordinates();
            Point NPCBottom = NPC.BottomRight.ToTileCoordinates();

            for (int y = NPCLeft.Y; y < NPCBottom.Y; y++)
            {
                for (int x = NPCLeft.X; x < NPCBottom.X; x++)
                {
                    if (WorldGen.SolidTile(x, y) && !FablesSets.ForAdvancedCollision.StormlionRegularIgnore[Main.tile[x, y].TileType])
                        return false;
                }
            }

            return true;
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

        public void TelegraphSand(float dustSpeed, float dustProbability)
        {
            int x = (int)(NPC.Center.X / 16);
            int y = (int)(NPC.Center.Y / 16);
            int halfWidth = 4;

            for (int i = x - halfWidth; i < x + halfWidth; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Tile tile = Framing.GetTileSafely(i, y - j);
                    if ((!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType]) && tile.WallType == 0)
                    {
                        //probability more common the closer to the side we are
                        float sideness = (1 - Math.Abs(i - x) / (float)halfWidth);
                        float probability = dustProbability;

                        for (int d = 0; d < 10; d++)
                        {
                            if (Main.rand.NextFloat() < probability * (float)Math.Pow(sideness, 0.5f))
                            {
                                Vector2 dustPos = new Vector2(i, y - j) * 16f;
                                dustPos += Vector2.UnitX * Main.rand.NextFloat(16f) + Vector2.UnitY * 16f;

                                Dust dus = Dust.NewDustPerfect(dustPos, DustID.Sand, -Vector2.UnitY * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f), 0);
                                dus.noGravity = false;
                                dus.velocity = dus.velocity.RotatedByRandom(MathHelper.PiOver4);
                                dus.scale = Main.rand.NextFloat(0.6f, 2f) * sideness;
                            }
                        }
                        break;
                    }
                }
            }
        }

        public void ElectrificationEffects()
        {
            Electrified = true;
            Lighting.AddLight(NPC.Center, new Vector3(0.4f, 1.4f, 1.3f));

            //Small dust
            for (int i = 0; i < 20; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 dustPos = NPC.Center - Vector2.UnitY * 50f + offset * 20f;
                Dust d = Dust.NewDustPerfect(dustPos, 206, offset * Main.rand.NextFloat(2f, 10f), 200, Color.White, Main.rand.NextFloat(0.7f, 1.4f));
                d.noLightEmittence = !Main.rand.NextBool(6);
                d.noLight = true;
            }

            //Big dust
            for (int i = 0; i < 16; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 dustPos = NPC.Center - Vector2.UnitY * 50f + offset * 20f;
                Vector2 dustVel = offset * Main.rand.NextFloat(4f, 8f) - Vector2.UnitY * 5f;
                Dust d = Dust.NewDustPerfect(dustPos, 206, dustVel, 200, Color.White, Main.rand.NextFloat(1.3f, 2.4f));
                d.noLight = true;
            }

            for (int r = 0; r < 4; r++)
            {
                int randomX = Main.rand.Next(6, 20);
                if (Main.rand.NextBool())
                    randomX *= -1;

                randomX += (int)NPC.Center.X / 16;

                if (WorldGen.SolidTile(randomX, (int)NPC.Center.Y / 16 - 5))
                    continue;

                for (int j = -5; j < 5; j++)
                {
                    if (WorldGen.SolidTile(randomX, (int)NPC.Center.Y / 16 + j))
                    {

                        Vector2 zapOrigin = NPC.Center - Vector2.UnitY * 50f + Main.rand.NextVector2Circular(10f, 10f) ;
                        Particle zap = new ElectricArcPrim(zapOrigin, new Vector2(randomX * 16 + 8, ((int)NPC.Center.Y / 16 + j) * 16 ), -Vector2.UnitY * 60f, 4f);
                        ParticleHandler.SpawnParticle(zap);

                        break;
                    }
                }
            }

            Particle thunder = new ThunderboltPrim(NPC.Center - Vector2.UnitY * 800f, NPC.Center - Vector2.UnitY * 50f, true, 4, 2);
            ParticleHandler.SpawnParticle(thunder);
        }

        public void SpawnRandomLightningArc()
        {
            int randomX = Main.rand.Next(6, 20);
            if (Main.rand.NextBool())
                randomX *= -1;

            randomX += (int)NPC.Center.X / 16;

            if (WorldGen.SolidTile(randomX, (int)NPC.Center.Y / 16 - 5))
                return;

            for (int j = -5; j < 5; j++)
            {
                if (WorldGen.SolidTile(randomX, (int)NPC.Center.Y / 16 + j))
                {

                    Vector2 zapOrigin = NPC.Center - Vector2.UnitY * 50f + Main.rand.NextVector2Circular(10f, 10f);
                    Particle zap = new ElectricArcPrim(zapOrigin, new Vector2(randomX * 16 + 8, ((int)NPC.Center.Y / 16 + j) * 16), -Vector2.UnitY * 60f, 4f);
                    ParticleHandler.SpawnParticle(zap);

                    SoundEngine.PlaySound(ElectroArcSound, new Vector2(randomX * 16 + 8, ((int)NPC.Center.Y / 16 + j) * 16));

                    break;
                }
            }
        }

        public override bool? CanFallThroughPlatforms()
        {
            return target.Top.Y > NPC.Bottom.Y;
        }

        public override void FindFrame(int frameHeight)
        {
            float velocity = NPC.IsABestiaryIconDummy ? 2 : Math.Abs(NPC.velocity.X);
            NPC.frameCounter += 0.08 * velocity;

            //Extra anim speed when close to zero speed so it looks like its skidding to the other direction
            float minSkid = Math.Sign(NPC.velocity.X) != NPC.direction ? 2f : 1f;
            float skidPower = Utils.GetLerpValue(minSkid, 0.5f, velocity, true);
            NPC.frameCounter += skidPower * 0.3f;


            int yFrame = 0;
            int xFrame = 0;

            if (Burrowing)
            {
                yFrame = 2;

                xFrame = NPC.frame.X / 136;
                if (NPC.frameCounter >= 1f)
                {
                    xFrame++;
                    NPC.frameCounter = 0f;
                }

                if (xFrame >= 6)
                    xFrame = 0;
            }
            else if (SuperchargeDelay < 0)
            {
                float animationProgress = 1 + SuperchargeDelay;
                yFrame = 3 + (int)MathF.Floor(animationProgress * 1.99999f);
                xFrame = (int)MathF.Floor(animationProgress * 21.9999f) % 11;
            }
            else if (BurrowTransitionProgress > 0f)
            {
                yFrame = 1;
                xFrame = (int)MathF.Floor(BurrowTransitionProgress * 6.9999f);
            }
            else
            {
                yFrame = 0;

                xFrame = NPC.frame.X / 136;
                if (NPC.frameCounter >= 1f)
                {
                    xFrame++;
                    NPC.frameCounter = 0f;
                }

                //Stuck
                if (velocity == 0)
                    xFrame = 0;

                if (xFrame >= 8)
                    xFrame = 0;
            }

            NPC.frame = new Rectangle(xFrame * 136, yFrame * 204, 136, 204);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;
            drawColor = NPC.TintFromBuffAesthetic(drawColor);

            ChargedTexture ??= ModContent.Request<Texture2D>(Texture + "_Charged");
            OutlineTexture ??= ModContent.Request<Texture2D>(Texture + "_Outline");

            Vector2 offset = NPC.GfxOffY() - screenPos;
            SpriteEffects flip = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = new Vector2(74, 170);
            if (NPC.spriteDirection == 1)
                origin.X = NPC.frame.Width - origin.X;

            //Trail
            if (Electrified)
            {
                if (!Burrowing)
                {
                    for (int i = 0; i < positionCache.Count; i += 3)
                    {
                        Color trailColor = Color.Lerp(Color.DeepSkyBlue, Color.White, 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + i * 1.5f));
                        float opacity = (i / (float)positionCache.Count) * 0.4f;
                        spriteBatch.Draw(OutlineTexture.Value, positionCache[i] + offset, NPC.frame, trailColor * opacity, NPC.rotation, origin, NPC.scale, flip, 0);
                    }
                }

                Color glowColor = Color.Lerp(Color.DeepSkyBlue, Color.White, 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f));
                spriteBatch.Draw(OutlineTexture.Value, NPC.Bottom + offset, NPC.frame, glowColor, NPC.rotation, origin, NPC.scale, flip, 0);
            }


            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            if (Electrified)
                bodyTex = ChargedTexture.Value;

            bool applySuperchargeVisuals = SuperchargeDelay < 0f;
            float superchargeAnimationProgress = (1 + SuperchargeDelay) * 22f;

            float electrificationOpacity = Utils.GetLerpValue(15, 13f, superchargeAnimationProgress, true);

            if (applySuperchargeVisuals && electrificationOpacity > 0 && SuperchargeDelay >= -0.62f)
            {
                var shader = GameShaders.Armor.GetShaderFromItemId(ModContent.ItemType<Electrocells>());
                Effect effect = shader.Shader;

                effect.Parameters["uColor"].SetValue(new Color(101, 241, 209));
                effect.Parameters["uSecondaryColor"].SetValue(new Color(177, 237, 59));
                effect.Parameters["uImageSize0"].SetValue(bodyTex.Size());
                effect.Parameters["uSourceRect"].SetValue(new Vector4(NPC.frame.X, NPC.frame.Y, NPC.frame.Width, NPC.frame.Height));
                Main.instance.GraphicsDevice.Textures[1] = AssetDirectory.NoiseTextures.RGB.Value;
                effect.Parameters["uImageSize1"].SetValue(AssetDirectory.NoiseTextures.RGB.Size());
                effect.Parameters["uSaturation"].SetValue(NPC.whoAmI);
                effect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);

                //Gotta do that for the point sampling
                effect.Parameters["rgbNoise"]?.SetValue(AssetDirectory.NoiseTextures.RGB.Value);
                float electricity = Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly));
                effect.Parameters["glowStrenght"]?.SetValue(electricity + electrificationOpacity);
                effect.Parameters["displaceStrenght"]?.SetValue(electrificationOpacity * 3f);
                effect.Parameters["uOpacity"]?.SetValue(MathF.Pow(electrificationOpacity, 0.25f));

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            }

            spriteBatch.Draw(bodyTex, NPC.Bottom + offset, NPC.frame, drawColor, NPC.rotation, origin, NPC.scale, flip, 0);

            if (applySuperchargeVisuals)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            return false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (Main.IsItStorming && spawnInfo.Player.ZoneDesert && !spawnInfo.Player.ZoneUndergroundDesert)
            {
                if (Sandstorm.Happening && Main.hardMode)
                    return SpawnCondition.SandstormEvent.Chance * 0.4f;
                else
                    return 0.6f;
            }
            return SpawnCondition.DesertCave.Chance * 0.06f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0 && Electrified && Main.netMode != NetmodeID.MultiplayerClient)
            {

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile proj = Projectile.NewProjectileDirect(NPC.GetSource_Death(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DesertScourgeElectroblast>(), 15, 10, Main.myPlayer, ai2: DEATH_BLAST_RADIUS);

                }

                SoundEngine.PlaySound(DesertScourge.PreyBelchStormDebrisImpactSound, NPC.Center);
            }

            if (!Main.dedServ)
            {
                for (int k = 0; k < 5; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 3, hit.HitDirection, -1f, 0, default, 1f);
                }

                if (NPC.life <= 0)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        Dust dusty = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 256, 0, 0, 100);
                        dusty.scale = Main.rand.NextFloat(1.2f, 2.5f);
                        dusty.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 4f;
                        dusty.noLight = true;
                        dusty.noGravity = !Main.rand.NextBool(6);
                    }

                    int randomGoreCount = Main.rand.Next(0, 2);
                    for (int i = 0; i < randomGoreCount; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Stormlion_Gore" + Main.rand.Next(1, 5).ToString()).Type, 1f);
                    }
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            IItemDropRule normalRule = ItemDropRule.Common(ModContent.ItemType<Electrocells>(), 1, 1, 3);
            IItemDropRule superchargedDropRule = ItemDropRule.Common(ModContent.ItemType<Electrocells>(), 1, 4, 6);

            var dropSet = npcLoot.DefineConditionalDropSet(DropHelper.If((info) => (info.npc.ModNPC as Stormlion)?.Electrified == true));
            dropSet.Add(superchargedDropRule, true);
            dropSet.OnFailedConditions(normalRule);

            npcLoot.Add(ItemID.AntlionMandible, 1, 1, 2);
            npcLoot.Add(ModContent.ItemType<StormfriedIceCream>(), 30);
        }
    }
}
