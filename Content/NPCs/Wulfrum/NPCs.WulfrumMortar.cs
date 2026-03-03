using CalamityFables.Content.Items.Food;
using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Particles;
using Terraria.GameContent.Bestiary;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    public class WulfrumMortar : ModNPC, ISuperchargable
    {
        public override string Texture => AssetDirectory.WulfrumNPC + Name;
        public static readonly SoundStyle ReadyMortar = new(SoundDirectory.Wulfrum + "WulfrumMortarReadyup");
        public static readonly SoundStyle FireMortar = new(SoundDirectory.Wulfrum + "WulfrumMortarFire");

        public Player target => Main.player[NPC.target];
        public ref float AIState => ref NPC.ai[0];
        public ref float BackupDistance => ref NPC.ai[1];
        public ref float PrepareFireTimer => ref NPC.ai[2];

        public ref float CannonRotation => ref NPC.localAI[0];
        public float CannonMinHeightPreference => (NPC.ai[1] % 1) * 140 + 200;

        public bool IsSupercharged {
            get {
                return NPC.ai[3] > 0;
            }
            set {
                NPC.ai[3] = value ? 1 : 0;
            }
        }
        public bool Aggroed => IsSupercharged || NPC.life < NPC.lifeMax;


        public int yFrame;
        public float hoverPulseTimer;

        public static int BannerType;
        public static AutoloadedBanner bannerTile;
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Wulfrum Mortar", AssetDirectory.WulfrumBanners, out bannerTile);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Mortar");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1

            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
            Main.npcFrameCount[Type] = 4;
            FablesSets.WulrumNPCs[Type] = true;
            bannerTile.NPCType = Type;

            if (Main.dedServ)
                return;
            for (int i = 0; i < 4; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumMortarGore" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.noGravity = true;
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 14;
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

                // TODO
                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.WulfrumMortar")
            });
        }


        public override void AI()
        {
            //Prevent the on-spawn "stuck" issue by forcing it to have an initial direction
            if (NPC.direction == 0)
            {
                NPC.TargetClosest(false);
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
                NPC.spriteDirection = NPC.direction;
            }

            //initialize the backup distance (Random number so they can not overlap when together)
            if (BackupDistance == 0)
                BackupDistance = Main.rand.NextFloat(40, 120);

            //Face the player if close and having line of sight
            NPC.TargetClosest(false);
            if (Aggroed || (Math.Abs(target.Center.X - NPC.Center.X) < 600 && Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height)))
            {
                NPC.spriteDirection = (target.Center.X - NPC.Center.X).NonZeroSign();
            }

            //Find the closest rover to hide near
            NPC nearbyRover = Main.npc.Where(n => n.active && n.type == ModContent.NPCType<WulfrumRover>() && NPC.Distance(n.Center) < 600)
                .OrderBy(n => NPC.Distance(n.Center))
                .FirstOrDefault();

            //If theres no rovers to target, just decide to move a specific distance from the player. Else, move a specific distance from the rover
            Vector2 movementTarget;
            if (nearbyRover == default)
                movementTarget = target.Center + Vector2.UnitX * (NPC.Center.X - target.Center.X).NonZeroSign() * (200 + BackupDistance);
            else
                movementTarget = nearbyRover.Center - Vector2.UnitX * nearbyRover.direction * BackupDistance;

            //Chance directions towards the movement target
            float distanceToTargetX = Math.Abs(movementTarget.X - NPC.Center.X);
            if (Aggroed || (distanceToTargetX < 600 && Collision.CanHitLine(movementTarget - NPC.Hitbox.Size() / 2, NPC.width, NPC.height, NPC.position, NPC.width, NPC.height)))
            {
                int direction = (movementTarget.X - NPC.Center.X).NonZeroSign();
                if (NPC.direction != direction)
                {
                    NPC.direction = direction;
                    NPC.netSpam = 0;
                    NPC.netUpdate = true;
                }
            }

            //If close enough to its ideal position / Charging up an attack, stop moving
            if (distanceToTargetX < 20 || AIState > 0)
            {
                NPC.velocity.X *= 0.9f;
                if (Math.Abs(NPC.velocity.X) < 0.1)
                    NPC.velocity.X = 0;

                //if not already charging up an attack, increase the timer of it wanting to fire
                if (AIState == 0)
                    PrepareFireTimer++;
            }

            else if (Math.Abs(target.Center.X - NPC.Center.X) < 700)
            {
                //If targetting the player / Being too far from the rover, increase the timer of it wanting to fire but less than if it was close to the movement target
                if ((nearbyRover == default || nearbyRover.Distance(NPC.Center) > 400))
                    PrepareFireTimer += 0.3f;

                //If damaged AND also close enough, increase the timer as well, but less 
                else if (NPC.life < NPC.lifeMax || NPC.Distance(target.Center) < 700)
                    PrepareFireTimer += 0.2f;

                //Accelerates forward towards its ideal movement target
                float maxXSpeed = 3f;
                if (NPC.direction != NPC.spriteDirection) //Going backwards makes it go slower - This helps melee players to catch up
                    maxXSpeed *= 0.7f;

                NPC.velocity.X += 0.02f * NPC.direction;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * maxXSpeed, 0.01f);
                NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxXSpeed, maxXSpeed);
            }


            //If ready enough to fire. Decide to fire
            if (AIState == 0)
            {
                //Takes longer to fire if the player has brand ambassador
                float prepareToFireDuration = (!Aggroed && target.npcTypeNoAggro[Type]) ? 180 : 90;

                if (PrepareFireTimer > prepareToFireDuration)
                {
                    AIState = 1;
                    PrepareFireTimer = 0;
                }
            }


            //Max aim distance
            Vector2 mortarTarget = target.Center;
            if (Math.Abs(NPC.Center.X - mortarTarget.X) > 700)
                mortarTarget.X = NPC.Center.X + 700 * Math.Sign(mortarTarget.X - NPC.Center.X);

            //Calculate arc velocity for its projectile towards the player. Make the cannon match this rotation
            Vector2 arcVel = GetArcVel(NPC.Center - Vector2.UnitY * 10, mortarTarget, 0.15f, CannonMinHeightPreference, CannonMinHeightPreference + 60);
            CannonRotation = arcVel.ToRotation();


            //Float up and down if chasing
            if (AIState == 0)
            {
                int tilesToFloat = 4;
                if (Collision.CanHitLine(movementTarget - NPC.Hitbox.Size() / 2, NPC.width, NPC.height, NPC.position, NPC.width, NPC.height))
                    tilesToFloat = 7;
                HoverMovement(movementTarget, tilesToFloat, 2.3f, 1.4f);

                NPC.gfxOffY += 0.1f;
                if (NPC.gfxOffY > 0)
                    NPC.gfxOffY = 0;
            }

            else //If readying to fire
            {
                //Sink down
                NPC.velocity.Y += 0.1f;
                if (NPC.velocity.Y > 3)
                    NPC.velocity.Y = 3;

                //Sink into the ground a bit
                NPC.gfxOffY += 0.2f;
                if (NPC.gfxOffY > 5)
                    NPC.gfxOffY = 5;

                //Charge up its cannon
                float timeToChargeCannon = Main.expertMode ? 0.8f : 1.25f;
                if (Math.Abs(PrepareFireTimer + 0.7f - timeToChargeCannon) < 1 / (60f * timeToChargeCannon))
                    SoundEngine.PlaySound(ReadyMortar, NPC.Center);
                PrepareFireTimer += 1 / (60f * timeToChargeCannon);

                //Fire (lol)
                if (PrepareFireTimer >= 1)
                {
                    SoundEngine.PlaySound(FireMortar, NPC.Center);
                    PrepareFireTimer = 0;
                    AIState = 0;
                    NPC.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Deals 2x contact damage by using the NPC's contact damage directly (since projectiles deal 2x damage)
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center - Vector2.UnitY * 10, arcVel, ModContent.ProjectileType<WulfrumMortarShell>(), NPC.damage, 5, Main.myPlayer);
                    }
                }
            }


            //Glow
            Lighting.AddLight(NPC.Center, CommonColors.WulfrumBlue.ToVector3() * ((float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.3f + 0.7f));
            //Tilt in the direction of the movement
            NPC.rotation = Math.Clamp(NPC.velocity.X * 0.04f, -MathHelper.PiOver4, MathHelper.PiOver4);

            //Make lil pulse particles behind itself
            hoverPulseTimer += 1 / (60f * 0.15f);
            if (hoverPulseTimer >= 1)
            {
                hoverPulseTimer = 0;
                ParticleHandler.SpawnParticle(new WulfrumMortarPulseParticle(NPC.Bottom - Vector2.UnitY.RotatedBy(NPC.rotation) * 3f, Vector2.UnitY.RotatedBy(NPC.rotation) * 1.3f, 1.2f, Color.White, 20));
            }
        }

        public void HoverMovement(Vector2 movementTarget, int hoverHeight, float maxDownwardsSpeed, float maxUpwardsSpeed)
        {
            float distanceToTargetX = Math.Abs(movementTarget.X - NPC.Center.X);

            //Ymovement
            int npcFrontTileX = (int)(NPC.Center.X / 16f) + NPC.direction * 1;
            int npcCenterTileY = (int)(NPC.Center.Y / 16f);
            bool noGroundToHoverOn = true;
            bool rightAboveGround = false;
            bool ignorePlatforms = CanFallThroughPlatforms().HasValue ? CanFallThroughPlatforms().Value : true;

            //Check beneath the mortar to check if theres any solid ground beneath
            for (int y = npcCenterTileY; y < npcCenterTileY + hoverHeight; y++)
            {
                if ((Main.tile[npcFrontTileX, y].HasUnactuatedTile && Main.tileSolid[Main.tile[npcFrontTileX, y].TileType]) || Main.tile[npcFrontTileX, y].LiquidAmount > 0)
                {
                    if (TileID.Sets.Platforms[Main.tile[npcFrontTileX, y].TileType] && ignorePlatforms)
                        continue;

                    if (y <= npcCenterTileY + 2)
                        rightAboveGround = true;

                    noGroundToHoverOn = false;
                    break;
                }
            }

            //Stop hovering up if above the player (Frankly not useful since the rover will always try to get away from the player
            //This is a holdover from the vanilla code where for exmaple, dripplers will want to stop hovering and instead come down to attack the player 
            //Im just too lazy to remove it and also it may cause unexpected issues
            if (distanceToTargetX < 20)
                noGroundToHoverOn = true;

            //Fall down if no ground
            if (noGroundToHoverOn && !rightAboveGround)
                NPC.velocity.Y += 0.1f;

            //Float up if yes ground / If agaisnt a wall
            else if (rightAboveGround || (NPC.collideX) || (movementTarget.Y - NPC.Center.Y < 0 && NPC.velocity.Y > 0))
                NPC.velocity.Y -= 0.3f;

            //TLDR: Hovers , but less strongly than the hovering motion that takes tiles into account.
            //Depending on how different its current Y velocity is compared to the ideal Y velocity it wants to reach, it will change its vertical speed faster
            if (distanceToTargetX > 20)
            {
                NPC.directionY = (movementTarget.Y - NPC.Center.Y).NonZeroSign();

                float maxSpeed = 1.5f;
                if (NPC.directionY == -1 && NPC.velocity.Y > -maxSpeed)
                {
                    NPC.velocity.Y -= 0.04f;
                    if (NPC.velocity.Y > maxSpeed)
                        NPC.velocity.Y -= 0.05f;

                    else if (NPC.velocity.Y > 0f)
                        NPC.velocity.Y += 0.03f;

                    if (NPC.velocity.Y < -maxSpeed)
                        NPC.velocity.Y = -maxSpeed;
                }
                else if (NPC.directionY == 1 && NPC.velocity.Y < maxSpeed)
                {
                    NPC.velocity.Y += 0.04f;
                    if (NPC.velocity.Y < -maxSpeed)
                        NPC.velocity.Y += 0.05f;

                    else if (NPC.velocity.Y < 0f)
                        NPC.velocity.Y -= 0.03f;

                    if (NPC.velocity.Y > maxSpeed)
                        NPC.velocity.Y = maxSpeed;
                }
            }

            //Clamp da velocity
            NPC.velocity.Y = Math.Clamp(NPC.velocity.Y, -maxDownwardsSpeed, maxUpwardsSpeed);
        }



        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool? CanFallThroughPlatforms() => target.Top.Y > NPC.Bottom.Y;

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter += 0.1;
            if (NPC.frameCounter > 1)
            {
                NPC.frameCounter = 0;
                yFrame += 1;
                if (yFrame >= Main.npcFrameCount[Type])
                    yFrame = 0;
            }

            //Floats up and down and have its cannon rotate around in the bestiary
            if (NPC.IsABestiaryIconDummy)
            {
                NPC.gfxOffY = ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 4) * 0.5f + 0.5f) * 4f;
                CannonRotation = -MathHelper.PiOver2 * 0.7f + MathHelper.PiOver4 * ((float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f + 0.5f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);
            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;

            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            Vector2 gfxOffY = NPC.GfxOffY() + Vector2.UnitY;
            Rectangle frame = new Rectangle(0, yFrame * bodyTex.Height / 4, bodyTex.Width, bodyTex.Height / 4 - 2);
            SpriteEffects flip = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.spriteBatch.Draw(bodyTex, NPC.Center + gfxOffY - screenPos, frame, drawColor, NPC.rotation, frame.Size() / 2f, NPC.scale, flip, 0f);


            Texture2D cannonTex = ModContent.Request<Texture2D>(Texture + "Cannon").Value;
            Vector2 cannonRotationPoint = NPC.Center + gfxOffY + new Vector2(-2 * NPC.spriteDirection, -12).RotatedBy(NPC.rotation);
            Vector2 origin = cannonTex.Size();
            float gunRotation = CannonRotation + (MathHelper.Pi - MathHelper.PiOver4);

            if (flip == SpriteEffects.FlipHorizontally)
            {
                gunRotation -= MathHelper.PiOver2;
                origin.X = 0;
            }

            Main.spriteBatch.Draw(cannonTex, cannonRotationPoint - screenPos, null, drawColor, gunRotation, origin, NPC.scale, flip, 0f);



            return false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => WulfrumCollaborationHelper.WulfrumGoonSpawnChance(spawnInfo);

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                for (int k = 0; k < 5; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 3, hit.HitDirection, -1f, 0, default, 1f);
                }
                if (NPC.life <= 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumMortarGore" + i.ToString()).Type, 1f);
                    }

                    int randomGoreCount = Main.rand.Next(0, 3);
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
            npcLoot.AddIf(info => (info.npc.ModNPC as ISuperchargable).IsSupercharged, ModContent.ItemType<EnergyCore>());
            npcLoot.Add(ItemID.Grenade, 2, 3, 5);
            npcLoot.Add(ModContent.ItemType<WulfrumBrandCereal>(), WulfrumBrandCereal.DroprateInt);
        }
    }


    public class WulfrumMortarShell : ModProjectile
    {
        public override string Texture => AssetDirectory.WulfrumNPC + Name;
        public ref float SizeModifier => ref Projectile.ai[0];
        public ref float NoFriendlyFire => ref Projectile.ai[1];
        public ref float SeekTimer => ref Projectile.localAI[1];

        public NPC magnetToSeek = default;

        public static int SeekTime = 30;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gyro Shell");
            FablesSets.WulfrumProjectiles[Type] = true;
            ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            magnetToSeek = default;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 420;
            CooldownSlot = 5; //5 is unique
        }

        public override void AI()
        {
            if (Projectile.timeLeft > 395)
            {
                Projectile.scale = 0.4f + 0.6f * (float)Math.Pow(1 - (Projectile.timeLeft - 395) / 25f, 0.7f);
            }

            Lighting.AddLight(Projectile.Center, CommonColors.WulfrumGreen.ToVector3() * 0.4f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.velocity.Y += 0.15f;

            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 5; i++)
                {
                    Dust fuck = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-6f, 6f), 43, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.4f), 100, CommonColors.WulfrumGreen, 3f);
                    fuck.noGravity = true;
                }
                Projectile.localAI[0] = 1f;
            }


            if (Main.rand.NextBool(4))
            {
                Dust chust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), 178, -Projectile.velocity * 0.4f * Main.rand.NextFloat(0.6f, 1f), Scale: Main.rand.NextFloat(0.5f, 1f));
                chust.noGravity = true;
                chust.velocity *= 0.5f;
                chust.velocity += Projectile.velocity * 0.1f;
            }

            //Find any magnetizer nearby if you havent found one already
            if (Projectile.timeLeft < 405 && (magnetToSeek == default || magnetToSeek == null) && SeekTimer == 0)
            {
                magnetToSeek = Main.npc.Where(n => n.active
                && n.type == ModContent.NPCType<WulfrumMagnetizer>()
                && (Projectile.Distance(n.Center) < 250)
                && (n.ai[0] >= 40)
                && (n.ModNPC as WulfrumMagnetizer).AttachedDebris.Count > 0)
                    .OrderBy(n => Projectile.Distance(n.Center))
                    .FirstOrDefault();
            }

            //If a magnet was found
            if (magnetToSeek != default)
            {
                //Check if its still active lol
                if (!magnetToSeek.active)
                {
                    magnetToSeek = default;
                    return;
                }

                SeekTimer++;

                //First phase : slow down and aim at the magnetizer
                if (SeekTimer < SeekTime)
                {
                    Projectile.velocity *= 0.92f;
                    Projectile.velocity = (Projectile.Center.DirectionTo(magnetToSeek.Center).ToRotation().AngleLerp(Projectile.velocity.ToRotation(), 0.12f)).ToRotationVector2() * Projectile.velocity.Length();
                }

                //Second phase: accelerate towards the magnetizer after the telegraph is over
                else
                {
                    float distanceToMagnet = Projectile.Distance(magnetToSeek.Center);
                    Projectile.velocity += Projectile.DirectionTo(magnetToSeek.Center) * (Utils.GetLerpValue(450f, 100f, distanceToMagnet, true) * 0.5f + 0.2f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(magnetToSeek.Center) * Projectile.velocity.Length() * 1.2f, 0.05f);

                    //If close enough to the magnet, kill yourself :zap: NOW!, prevent it from hurting other enemies (including magnetron) (magnetron has immunity to it already but yeah
                    if (distanceToMagnet < 30)
                    {
                        Projectile.timeLeft = 0;
                        NoFriendlyFire = 1f;

                        //Guarantee the magnetron to fire projectiles by setting its actiontimer to 10 (it fires when actiontimer reaches 1, but makes the projectiles explode if they are at 10)
                        magnetToSeek.Magnetizer_GetExplodedByMortar();
                    }
                }

            }


        }


        //Instantly kills itself when contacting the player, and also sets the player immunity to zero so the ctual explosion can hit
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                new KillServerProjectilePacket(Projectile).Send(runLocally: false);
            Projectile.Kill();
            target.immuneTime = 0;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
            {
                info.Damage = 1;
                info.SourceDamage = 1;
            };
        }

        //Boom
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.position);
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile kaboom = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center - Vector2.UnitY * 10, Vector2.Zero, ModContent.ProjectileType<WulfrumMortarExplosion>(), Projectile.damage, 8, Main.myPlayer);

                if (SizeModifier != 0)
                {
                    kaboom.position = kaboom.Center;
                    int newWidth = (int)(kaboom.width * (1 + SizeModifier));
                    kaboom.width = kaboom.height = newWidth;
                    kaboom.Center = kaboom.position;
                }

                if (NoFriendlyFire == 1)
                {
                    kaboom.friendly = false;
                }
            }
        }
    }

    public class WulfrumMortarExplosion : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public Vector2 offset;
        public Vector2 offsetOuter;
        List<Particle> innerSmoke;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosion");
            FablesSets.WulfrumProjectiles[Type] = true;
            ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.hide = true;
            Projectile.width = 140;
            Projectile.height = 140;
            Projectile.hostile = true;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;

            offset = new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat());
            offsetOuter = new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat());

        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override void AI()
        {
            //Do a little smoking. We initialize our own custom list of particles instead of using the particle handler because we want to draw it ourselves behind the blast circle.

            if (!Main.dedServ)
            {
                if (innerSmoke == null)
                {
                    innerSmoke = new List<Particle>();

                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 particlePosition = Main.rand.NextVector2Circular(Projectile.width * 0.15f, Projectile.width * 0.15f);
                        Particle smoke = new SmokeParticle(Projectile.Center + particlePosition, particlePosition.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 4f), Color.Gold, Color.OrangeRed, Main.rand.NextFloat(1f, 1.2f), 1f, Main.rand.Next(22, 28));
                        smoke.Type = ParticleHandler.particleTypes[smoke.GetType()];

                        innerSmoke.Add(smoke);
                    }
                }

                else
                {
                    foreach (Particle particle in innerSmoke)
                    {
                        particle.Update();
                        particle.Position += particle.Velocity;
                        particle.Time++;
                    }

                    innerSmoke.RemoveAll(n => n.Time >= n.Lifetime);
                }
            }



            if (Projectile.timeLeft == 30)
            {
                for (int i = 0; i < 15; i++)
                {
                    Vector2 direction = Main.rand.NextVector2CircularEdge(Projectile.width / 2, Projectile.width / 2);

                    Dust bust = Dust.NewDustPerfect(Projectile.Center + direction, DustID.Torch, direction * Main.rand.NextFloat(0.01f, 0.2f), Scale: 2f);
                    bust.noGravity = true;
                }

                for (int i = 0; i < 6; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.width * 0.42f);

                    if (direction.Length() < Projectile.width * 0.3f)
                        direction = direction.SafeNormalize(Vector2.UnitY) * Projectile.width * 0.42f;

                    Particle streak = new BlastStreak(Projectile.Center, direction, Projectile.width * 0.42f, Color.Gold, Color.OrangeRed, Color.Gold * 0.3f, 0.4f, 12, 3f);
                    ParticleHandler.SpawnParticle(streak);
                }

            }

            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = 0;

            Lighting.AddLight(Projectile.Center, CommonColors.WulfrumGreen.ToVector3() * 0.4f);

        }

        public override bool CanHitPlayer(Player target) => Projectile.timeLeft > 25;
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.timeLeft <= 25 || target.type == ModContent.NPCType<WulfrumMagnetizer>()) //Cant hit magnetizers
                return false;

            return base.CanHitNPC(target);

        }

        //Shake the player and sends them flying
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (target.whoAmI == Main.myPlayer)
                CameraManager.Shake += 4;

            if (!target.noKnockback)
                target.velocity = (target.Center - Projectile.Center).SafeNormalize(-Vector2.UnitY) * 8;
        }

        //Modify the damage scaling to match the difficulty
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!FablesSets.WulrumNPCs[target.type])
                modifiers.FinalDamage *= 3;
            else
                modifiers.FinalDamage *= 1.5f;
        }

        //Make the hitbox circular
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!AABBvCircle(targetHitbox, Projectile.Center, Projectile.width / 2f))
                return false;

            if (!Collision.CanHit(projHitbox.TopLeft(), projHitbox.Width, projHitbox.Height, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height))
                return Collision.CanHit(projHitbox.TopLeft(), projHitbox.Width, projHitbox.Height, targetHitbox.TopLeft() - Vector2.UnitY * (targetHitbox.Height * 0.5f + 16), targetHitbox.Width, targetHitbox.Height);

            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Draw the cached smoke
            if (innerSmoke != null)
                foreach (Particle smoke in innerSmoke)
                {
                    Texture2D smokeTex = ParticleHandler.particleTextures[smoke.Type].Value;

                    Rectangle frame = smokeTex.Frame(1, smoke.FrameVariants, 0, smoke.Variant);
                    Main.spriteBatch.Draw(smokeTex, smoke.Position - Main.screenPosition, frame, smoke.Color, smoke.Rotation, frame.Size() * 0.5f, smoke.Scale, SpriteEffects.None, 0f);
                }

            //TLDR : draw the blast

            float dissipateProgress = Projectile.timeLeft / 30f;
            Texture2D noiseTex = ModContent.Request<Texture2D>(AssetDirectory.Noise + "PerlinNoise").Value;
            Vector2 scale = Vector2.One * (Projectile.width / (float)noiseTex.Height) * (float)Math.Pow(1 - dissipateProgress, 0.07f);

            Effect blastEffect = Scene["GyroMortarBlast"].GetShader().Shader;

            Color innerBlastColor = Color.Lerp(CommonColors.WulfrumBlue, CommonColors.WulfrumGreen, 1 - dissipateProgress) with
            {
                A = 222
            };
            Color outerBlastColor = Color.Lerp(Color.Gold, Color.OrangeRed, 1 - dissipateProgress);

            float innerBlastTreshold = 1 - (Projectile.timeLeft - 5f) / 25f;
            float outerBlastTreshold = (float)Math.Pow(1 - dissipateProgress, 2);

            Vector2 innerOffset = offset with
            {
                X = offset.X + (float)Math.Pow(dissipateProgress, 2) * 0.3f
            };
            Vector2 outerOffset = offset with
            {
                X = offsetOuter.X - (float)Math.Pow(dissipateProgress, 2) * 0.3f
            };

            float innerResolution = Projectile.width / 2;
            float outerResolution = Projectile.width / 2 * 1.1f;


            if (Projectile.timeLeft < 20)
                outerBlastColor *= Projectile.timeLeft / 20f;

            blastEffect.Parameters["noiseScale"].SetValue(0.5f + dissipateProgress * 0.3f);

            blastEffect.Parameters["edgeFadeDistance"].SetValue(0.05f);
            blastEffect.Parameters["edgeFadePower"].SetValue(3f);
            blastEffect.Parameters["shapeFadeTreshold"].SetValue(0.1f * (1 - outerBlastTreshold));
            blastEffect.Parameters["shapeFadePower"].SetValue(1f);

            blastEffect.Parameters["fresnelDistance"].SetValue(0.3f);
            blastEffect.Parameters["fresnelStrenght"].SetValue(1.6f);
            blastEffect.Parameters["fresnelOpacity"].SetValue(0.25f);

            blastEffect.Parameters["offset"].SetValue(innerOffset);
            blastEffect.Parameters["resolution"].SetValue(innerResolution);
            blastEffect.Parameters["treshold"].SetValue(innerBlastTreshold);
            blastEffect.Parameters["blastColor"].SetValue(innerBlastColor.ToVector4());



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, blastEffect, Main.GameViewMatrix.TransformationMatrix);

            //Middle green
            Main.EntitySpriteDraw(noiseTex, Projectile.Center - Main.screenPosition, null, Color.White, 0, noiseTex.Size() / 2f, scale, SpriteEffects.None, 0);

            blastEffect.Parameters["offset"].SetValue(outerOffset);
            blastEffect.Parameters["resolution"].SetValue(outerResolution);
            blastEffect.Parameters["treshold"].SetValue(outerBlastTreshold);
            blastEffect.Parameters["blastColor"].SetValue(outerBlastColor.ToVector4() with
            {
                W = outerBlastColor.A / 255f
            });

            Main.EntitySpriteDraw(noiseTex, Projectile.Center - Main.screenPosition, null, Color.White, 0, noiseTex.Size() / 2f, scale * 1.05f, SpriteEffects.None, 0);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);


            return false;
        }
    }
}

