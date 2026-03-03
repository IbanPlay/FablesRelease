using CalamityFables.Content.Items.Food;
using CalamityFables.Content.Items.Wulfrum;
using System.IO;
using Terraria.GameContent.Bestiary;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    [ReplacingCalamity("WulfrumHovercraft")]
    public class WulfrumMagnetizer : ModNPC, ISuperchargable
    {
        public override string Texture => AssetDirectory.WulfrumNPC + Name;
        public static readonly SoundStyle FireScrap = new(SoundDirectory.Wulfrum + "WulfrumScrewdriverScrewHit");
        public static readonly SoundStyle ScrapHit = new(SoundDirectory.Wulfrum + "WulfrumScrewdriverThud");

        public int ScrapProjectileDamage
        {
            get
            {
                int baseDamage = 10;
                baseDamage = (int)(baseDamage* Main.GameModeInfo.EnemyDamageMultiplier);
                if (Main.GameModeInfo.EnemyDamageMultiplier >= 3)
                    baseDamage = (int)(baseDamage * 0.75f);
                return baseDamage;
            }
        }

        public Player target => Main.player[NPC.target];
        public ref float TimeUntilNextAction => ref NPC.ai[0];
        public ref float ActionTimer => ref NPC.ai[1];
        public ref float HoverHeight => ref NPC.ai[2];

        public ref float MagnetRotation => ref NPC.localAI[0];
        public ref float MagnetRetraction => ref NPC.localAI[1];
        public static float MagnetArmLength = 40;

        public bool IsSupercharged {
            get {
                return NPC.ai[3] > 0;
            }
            set {
                NPC.ai[3] = value ? 1 : 0;
            }
        }
        public int yFrame;

        public NPC caughtRoller = default;
        public bool firedARollerAlready = false;
        public bool hasARoller => caughtRoller != default && caughtRoller != null;

        public bool Aggroed => IsSupercharged || NPC.life < NPC.lifeMax || hasARoller || AttachedDebris.Count > 0;


        #region debris struct
        public struct MagnetizedDebris
        {
            public int variant;
            public Rectangle frame;
            public float fadeIn;
            public float rotation;

            public float random_telegraphInitialRotationDisplacement;
            public float random_telegraphDiscoMovement;
            public float random_telegraphApparitionPower;
            public float random_telegraphOpacity;
            public float random_telegraphLenghtMult;

            public float angle;
            public Vector2 originalOffset;
            public float timeItTakesToReachDestination;

            public MagnetizedDebris(float maxAngleFromDown, float minDistance, float maxDistance)
            {
                fadeIn = 0f;
                variant = Main.rand.Next(6);
                switch (variant)
                {
                    case 0:
                        frame = new Rectangle(0, 0, 12, 16);
                        break;
                    case 1:
                        frame = new Rectangle(16, 2, 10, 12);
                        break;
                    case 2:
                        frame = new Rectangle(30, 0, 14, 14);
                        break;
                    case 3:
                        frame = new Rectangle(0, 18, 18, 14);
                        break;
                    case 4:
                        frame = new Rectangle(22, 16, 10, 14);
                        break;
                    case 5:
                    default:
                        frame = new Rectangle(36, 18, 16, 14);
                        break;

                }

                Vector2 distanceFromCenter = Vector2.UnitY * Main.rand.NextFloat(minDistance, maxDistance);
                angle = Main.rand.NextFloat(-maxAngleFromDown, maxAngleFromDown);
                originalOffset = distanceFromCenter.RotatedBy(angle);

                rotation = Main.rand.NextFloat() * MathHelper.TwoPi;
                timeItTakesToReachDestination = Main.rand.NextFloat(30f, 40f);

                random_telegraphInitialRotationDisplacement = Main.rand.NextFloat(MathHelper.TwoPi);
                random_telegraphDiscoMovement = Main.rand.NextFloat(0.6f, 1.4f);
                random_telegraphApparitionPower = Main.rand.NextFloat(0.2f, 0.4f);
                random_telegraphOpacity = Main.rand.NextFloat(0.7f, 1f);
                random_telegraphLenghtMult = Main.rand.NextFloat(0.9f, 1.15f);

            }
        }

        public List<MagnetizedDebris> AttachedDebris;
        #endregion


        public static int BannerType;
        public static AutoloadedBanner bannerTile;
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Wulfrum Magnetizer", AssetDirectory.WulfrumBanners, out bannerTile);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Magnetizer");
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
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumMagnetizerGore" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            caughtRoller = default;
            AttachedDebris = new List<MagnetizedDebris>();
            NPC.noGravity = true;
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 13;
            NPC.width = 42;
            NPC.height = 37;
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

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.WulfrumMagnetizer")
            });
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(caughtRoller == default ? -1 : caughtRoller.whoAmI);
            writer.Write((byte)firedARollerAlready.ToInt());
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int caughtRollerIndex = reader.ReadInt32();
            caughtRoller = caughtRollerIndex >= 0 ? Main.npc[caughtRollerIndex] : default;

            firedARollerAlready = reader.ReadByte() != 0;
        }

        public override void AI()
        {
            if (HoverHeight == 0)
                HoverHeight = Main.rand.NextFloat(0.1f, 4f);

            //Prevent the on-spawn "stuck" issue by forcing it to have an initial direction
            if (NPC.direction == 0)
            {
                NPC.TargetClosest(false);
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
            }

            //Face the player
            NPC.TargetClosest(false);
            if (Aggroed || (Math.Abs(target.Center.X - NPC.Center.X) < 800 && Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height)))
            {
                NPC.spriteDirection = (target.Center.X - NPC.Center.X).NonZeroSign();
            }
            else
                NPC.spriteDirection = NPC.direction;


            bool lineOfSight = Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height);

            Vector2 movementTarget = target.Center;
            //Chance directions if close but not too close and if line of sight (or aggroed)

            float distanceToTargetX = Math.Abs(movementTarget.X - NPC.Center.X);
            float minDistanceToChangeDirections = (NPC.life < NPC.lifeMax || hasARoller) ? 10 : 50;

            if (distanceToTargetX > minDistanceToChangeDirections && (Aggroed || (distanceToTargetX < 780 && lineOfSight)))
            {
                int direction = (movementTarget.X - NPC.Center.X).NonZeroSign();
                if (NPC.direction != direction)
                {
                    NPC.direction = direction;
                    NPC.netSpam = 0;
                    NPC.netUpdate = true;
                }
            }

            //This state is the basic state where it just hovers around (Either its got no debris, or its "action timer" is zero which is basically saying its not reading up the attack
            if (AttachedDebris.Count == 0 || ActionTimer == 0)
            {
                //Accelerates forward
                float maxXSpeed = 3f;
                if (NPC.direction != NPC.spriteDirection) //Moving away from where its facing is slower
                    maxXSpeed *= 0.7f;

                if (AttachedDebris.Count > 0) //If it has debris, it moves faster
                    maxXSpeed *= 1.5f;

                //Accelerate yadda yadda
                if ((NPC.direction == -1 && NPC.velocity.X > -maxXSpeed) || (NPC.direction == 1 && NPC.velocity.X < maxXSpeed))
                {
                    float speedToGoAt = 0.04f;

                    if (NPC.velocity.X * NPC.direction < -maxXSpeed) //Accelerate faster if going in teh wrong direction lol
                        speedToGoAt *= 1.5f;
                    if (NPC.velocity.X * NPC.direction < 0)
                        speedToGoAt *= 1.5f;


                    NPC.velocity.X += speedToGoAt * NPC.direction;
                    NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxXSpeed, maxXSpeed);
                }
            }


            int tilesToFloat = 10 + (int)HoverHeight;
            if (AttachedDebris.Count > 0 && ActionTimer > 0) //Float higher when readying up an attack 
                tilesToFloat += 3;

            if (AttachedDebris.Count > 0 && target.Bottom.Y < NPC.Top.Y)
                tilesToFloat += 5;

            if (target.Bottom.Y < NPC.Top.Y && target.velocity.Y == 0)
                tilesToFloat += 10;

            if (!lineOfSight && NPC.Bottom.Y < target.Top.Y - 30)
            {
                tilesToFloat = Math.Min(tilesToFloat, 5);
            }

            HoverMovement(movementTarget, tilesToFloat, 2.3f, 1.4f);

            //Charge up towards grabbing debris
            if (AttachedDebris.Count == 0)
            {
                //If its charging up
                if (TimeUntilNextAction < 60)
                {
                    //Random timer until it grabs debris
                    TimeUntilNextAction += Main.rand.NextFloat(0.4f, 1f);

                    //Retract the magnet back in (happens after it attacks)
                    MagnetRetraction = MathHelper.Lerp(MagnetRetraction, 0f, 0.12f);
                    if (MagnetRetraction < 0.04f)
                        MagnetRetraction = 0f;
                }

                //If its done charging up and is about to grab debris
                else
                {
                    //Slow down
                    NPC.velocity.X *= 0.9f;

                    //Make the magnet lower
                    ActionTimer += 1 / (60f * 1.2f);
                    MagnetRetraction = PolyInOutEasing(ActionTimer, 1);

                    //Once the magnet is fully lowered, reset its state and charge up a bunch of debris
                    if (ActionTimer >= 1)
                    {
                        ActionTimer = 0;
                        TimeUntilNextAction = 0;

                        int debris = Main.rand.Next(3, 6);
                        for (int i = 0; i < debris; i++)
                        {
                            AttachedDebris.Add(new(MathHelper.PiOver4 * 1.2f, 150f, 260f));
                        }

                        if (Main.netMode == NetmodeID.Server)
                            new SyncWulfrumMagnetizerScrapPacket(NPC, AttachedDebris).Send(runLocally:false);

                        caughtRoller = default;
                        firedARollerAlready = false;
                        NPC.netUpdate = true;
                    }
                }
            }

            //If its got debris (charge up towards shooting them at the player)
            else
            {
                Vector2 magnetDisplacement = Vector2.UnitY.RotatedBy(NPC.rotation + MagnetRotation) * MathHelper.Lerp(14f, MagnetArmLength, MagnetRetraction);

                //it can carge up 2/3rds of the way until shooting b ut will only complete the final third if its above its target, to avoid it launching an attack when below the player
                if (TimeUntilNextAction < 60 || NPC.Bottom.Y < target.Top.Y)
                    TimeUntilNextAction += 0.5f;

                //Update all the debris (make em fade into full opacity and make them spin around
                for (int i = 0; i < AttachedDebris.Count; i++)
                {
                    MagnetizedDebris debris = AttachedDebris[i];

                    debris.fadeIn = MathHelper.Lerp(debris.fadeIn, 1f, 0.05f);
                    if (debris.fadeIn > 0.97f)
                        debris.fadeIn = 1;

                    if (debris.timeItTakesToReachDestination > TimeUntilNextAction)
                        debris.rotation += debris.angle.NonZeroSign() * 0.02f;

                    AttachedDebris[i] = debris;
                }

                bool hasAMagnetFriend = MagnetizeRollers();

                //Attacks slower if the target is brand ambassador
                float timeToAttack = (!IsSupercharged  && NPC.life == NPC.lifeMax && target.npcTypeNoAggro[Type]) ? 280 : 90; 

                //if its ready to attack
                if (TimeUntilNextAction >= timeToAttack)
                {
                    float telegraphTime = Main.expertMode ? 1.4f : 2f;

                    NPC.velocity.X *= 0.97f; //Slow down when telegraphing
                    ActionTimer += 1 / (60f * telegraphTime);

                    if (ActionTimer >= 1)
                    {
                        bool explosivePellets = ActionTimer >= 10; //This only happens if a mortar's shell manually sets it to 10 by hitting it

                        TimeUntilNextAction = 0;
                        ActionTimer = 0;

                        //Shoot out every scrap fragment as a projectile
                        foreach (MagnetizedDebris debris in AttachedDebris)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + magnetDisplacement, Vector2.UnitY.RotatedBy(debris.angle) * 10f, ModContent.ProjectileType<WulfrumScrapProjectile>(), ScrapProjectileDamage / 2, 8, Main.myPlayer, debris.variant, explosivePellets ? 1f : 0f);
                            }
                        }

                        if (hasAMagnetFriend)
                        {
                            caughtRoller.Roller_ForceDash();
                            caughtRoller = default;
                        }


                        SoundEngine.PlaySound(FireScrap, NPC.Center);
                        AttachedDebris.Clear(); //Gotta remember to clear the debris
                        NPC.velocity.Y -= 3f; //Make the magnetizer hop upwards to show recoil
                    }
                }
            }

            //Bounce on walls
            if (NPC.collideX)
            {
                NPC.direction *= -1;
                NPC.velocity.X *= (Aggroed && lineOfSight) ? -0.6f : -1;
            }

            if (NPC.collideY && target.Bottom.Y < NPC.Top.Y)
                NPC.velocity.Y = 5;

            //Lil dust coming from its thrusters
            if (Math.Abs(NPC.velocity.X) > 0.5f && NPC.direction == NPC.velocity.X.NonZeroSign() && Main.rand.NextBool(4))
            {
                Vector2 dustPosition = NPC.Center - Vector2.UnitX.RotatedBy(NPC.rotation) * NPC.direction * 22f + Vector2.UnitY.RotatedBy(NPC.rotation) * Main.rand.NextFloat() * 9f;
                Dust chust = Dust.NewDustPerfect(dustPosition, 15, -NPC.velocity * Main.rand.NextFloat(0.6f, 1f), 150, Scale: Main.rand.NextFloat(1f, 1.4f));
                chust.noGravity = true;
            }

            Lighting.AddLight(NPC.Center, CommonColors.WulfrumBlue.ToVector3() * ((float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.3f + 0.7f));
            //Tilt in the direction of the movement
            NPC.rotation = Math.Clamp(NPC.velocity.X * 0.04f, -MathHelper.PiOver4, MathHelper.PiOver4);
            MagnetRotation = MathHelper.Lerp(MagnetRotation, NPC.rotation, 0.1f);
        }

        public void HoverMovement(Vector2 movementTarget, int hoverHeight, float maxDownwardsSpeed, float maxUpwardsSpeed)
        {
            int npcFrontTileX = (int)(NPC.Center.X / 16f) + NPC.direction * 1;
            int npcCenterTileY = (int)(NPC.Center.Y / 16f);
            bool noGroundToHoverOn = true;
            bool rightAboveGround = false;
            bool ignorePlatforms = CanFallThroughPlatforms().HasValue ? CanFallThroughPlatforms().Value : true;

            for (int y = npcCenterTileY; y < npcCenterTileY + hoverHeight; y++)
            {
                if ((Main.tile[npcFrontTileX, y].HasUnactuatedTile && Main.tileSolid[Main.tile[npcFrontTileX, y].TileType]) || Main.tile[npcFrontTileX, y].LiquidAmount > 0)
                {
                    if (TileID.Sets.Platforms[Main.tile[npcFrontTileX, y].TileType] && ignorePlatforms)
                        continue;

                    if (y <= npcCenterTileY + 7)
                        rightAboveGround = true;

                    noGroundToHoverOn = false;
                    break;
                }
            }

            //Fall down if no ground
            if (noGroundToHoverOn)
                NPC.velocity.Y += 0.1f;
            //Float up if yes ground
            else if (!noGroundToHoverOn || rightAboveGround || (NPC.collideX || NPC.velocity.X == 0) || (movementTarget.Y - NPC.Center.Y < 0 && NPC.velocity.Y > 0))
                NPC.velocity.Y -= 0.035f;

            NPC.velocity.Y = Math.Clamp(NPC.velocity.Y, -maxDownwardsSpeed, maxUpwardsSpeed);
        }

        public bool MagnetizeRollers()
        {
            Vector2 magnetDisplacement = Vector2.UnitY.RotatedBy(NPC.rotation + MagnetRotation) * MathHelper.Lerp(14f, MagnetArmLength, MagnetRetraction);

            if (caughtRoller == default || caughtRoller == null)
            {
                if (!firedARollerAlready)
                {
                    caughtRoller = Main.npc.Where(n => n.active && n.type == ModContent.NPCType<WulfrumRoller>() && n.ai[0] == 0 && n.ai[3] <= 0 &&
                         NPC.Distance(n.Center) < 300 &&
                         (NPC.Center + magnetDisplacement).Y < n.Center.Y &&
                         Math.Abs(FablesUtils.AngleBetween(n.AngleFrom(NPC.Center), MathHelper.PiOver2)) < MathHelper.PiOver2)
                        .OrderBy(n => NPC.Distance(n.Center))
                        .FirstOrDefault();
                    NPC.netUpdate = true;
                }
            }

            else if (!caughtRoller.active || caughtRoller.Distance(NPC.Center) > 310)
            {
                caughtRoller = default;
                NPC.netUpdate = true;
            }

            if (caughtRoller == default || caughtRoller == null)
                return false;

            //Get the strenght of the magnetization based on how close the roller is
            Vector2 magnetizationPosition = (NPC.Center + magnetDisplacement) + Vector2.UnitY.RotatedBy(NPC.rotation + MagnetRotation) * 13f;
            float distanceToMagnet = caughtRoller.Distance(magnetizationPosition);
            float magnetizationStrenght = Utils.GetLerpValue(0f, 15f, TimeUntilNextAction, true) * (float)Math.Pow((float)Math.Clamp(1 - distanceToMagnet / 300f, 0f, 1f), 2f);

            //Move the roller towards the magnet
            caughtRoller.Center = Vector2.Lerp(caughtRoller.Center, magnetizationPosition, 0.05f * magnetizationStrenght);
            caughtRoller.Center = caughtRoller.Center.MoveTowards(magnetizationPosition, 2.4f + 3f * magnetizationStrenght);


            //Stick the roller onto the magnet if close enough
            if (caughtRoller.Distance(magnetizationPosition) < 5f)
                caughtRoller.Center = magnetizationPosition;

            //Do particles to link them both if not
            else
            {
                Vector2 dustPosition = caughtRoller.Center + caughtRoller.DirectionTo(NPC.Center).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1f, 1f) * caughtRoller.width / 2f;

                Dust chust = Dust.NewDustPerfect(dustPosition, 15, dustPosition.DirectionTo(magnetizationPosition) * Main.rand.NextFloat(0.6f, 1f), 150, Scale: Main.rand.NextFloat(1f, 1.4f));
                chust.noGravity = true;
            }

            //Drop the roller straight on the players head if its above them in expert
            if (Main.expertMode && ActionTimer == 0 && caughtRoller.Distance(magnetizationPosition) < 15f && caughtRoller.Bottom.Y < target.Top.Y - 40 && Math.Abs(caughtRoller.Center.X - target.Center.X) < 10f)
            {
                caughtRoller.Center = magnetizationPosition;
                caughtRoller.velocity.Y = 3;
                caughtRoller.ai[2] = 1;
                caughtRoller.ai[3] = 0f;

                SoundEngine.PlaySound(WulfrumGrappler.HandGrabSound, caughtRoller.Center);

                caughtRoller = default;
                firedARollerAlready = true;
                NPC.netUpdate = true;

                return false;
            }


            float rotationPower = magnetizationStrenght * 0.18f * NPC.direction;

            //If were not about to dash or anything, prevent the roller from dashing or being blue
            if (ActionTimer == 0)
            {
                caughtRoller.Roller_PreventDashes();
            }

            //Make the roller start glowing
            else
            {
                caughtRoller.Roller_StartToGlow(ActionTimer);
                rotationPower *= 1f + ActionTimer * 0.5f;
            }

            //Prevent the roller from moving or doing anything else really.
            caughtRoller.Roller_BeMagnetized(rotationPower);
            caughtRoller.rotation = NPC.rotation + MagnetRotation;

            return true;
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

            //if on the bestiary, keep its magnet extended and swinging, alongside the body
            if (NPC.IsABestiaryIconDummy)
            {
                MagnetRetraction = 0.9f + 0.1f * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.7f) * 0.5f + 0.5f);
                MagnetRotation = 0 + 0.3f * MathHelper.PiOver4 * ((float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f + 0.5f);

                NPC.rotation = 0.06f * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.1f) * 0.5f + 0.5f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);
            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;

            Texture2D magnetTex = ModContent.Request<Texture2D>(Texture + "_Magnet").Value;
            Texture2D backArmTex = ModContent.Request<Texture2D>(Texture + "_BackThruster").Value;
            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            Texture2D frontArmTex = ModContent.Request<Texture2D>(Texture + "_FrontThruster").Value;

            Vector2 gfxOffY = NPC.GfxOffY() + Vector2.UnitY;
            Rectangle frame = new Rectangle(0, yFrame * bodyTex.Height / 4, bodyTex.Width, bodyTex.Height / 4 - 2);
            SpriteEffects flip = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Vector2 magnetOrigin = new Vector2(magnetTex.Width / 2, magnetTex.Height);
            Vector2 magnetDisplacement = Vector2.UnitY.RotatedBy(NPC.rotation + MagnetRotation) * MathHelper.Lerp(14f, MagnetArmLength, MagnetRetraction);

            Main.spriteBatch.Draw(magnetTex, NPC.Center + magnetDisplacement + gfxOffY - screenPos, null, drawColor, MagnetRotation + NPC.rotation, magnetOrigin, NPC.scale, flip, 0f);

            //Draw the debris
            if (AttachedDebris.Count > 0)
            {
                Texture2D debrisTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumScrapProjectiles").Value;

                foreach (MagnetizedDebris debris in AttachedDebris)
                {
                    float debrisTravelCompletion = 1 - Math.Clamp(TimeUntilNextAction / debris.timeItTakesToReachDestination, 0f, 1f);
                    debrisTravelCompletion = (float)Math.Pow(debrisTravelCompletion, 0.5f);

                    Vector2 debrisOffset = debris.originalOffset.SafeNormalize(Vector2.UnitY).RotatedBy(NPC.rotation);
                    float debrisDistance = Math.Max(debris.originalOffset.Length() * debrisTravelCompletion, 8f);


                    debrisOffset *= debrisDistance;
                    debrisOffset -= Vector2.UnitY.RotatedBy(NPC.rotation + MagnetRotation) * 4f;

                    Color debrisColor = drawColor * debris.fadeIn;
                    Main.spriteBatch.Draw(debrisTex, NPC.Center + magnetDisplacement + debrisOffset + gfxOffY - screenPos, debris.frame, debrisColor, debris.rotation + NPC.rotation + MagnetRotation, debris.frame.Size() / 2f, NPC.scale, 0f, 0f);
                }
            }

            //Draw the swag as fuck telegraph lines
            if (AttachedDebris.Count > 0 && ActionTimer > 0)
            {
                Texture2D empty = ModContent.Request<Texture2D>(AssetDirectory.Invisible).Value;
                float completion = (float)Math.Pow(ActionTimer, 0.65f);

                //Setup the laser sights effect.
                Effect laserScopeEffect = Scene["PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.005f);

                float lineOpacity = (float)Math.Pow(ActionTimer, 0.7f);//Opacity increases as the attack is about to happen

                laserScopeEffect.Parameters["laserWidth"].SetValue(0.004f - 0.001f * (float)Math.Pow(ActionTimer, 1.5f));

                laserScopeEffect.Parameters["color"].SetValue(CommonColors.WulfrumGreen.ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Lerp(CommonColors.WulfrumBlue, Color.Black, 0.8f).ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.05f + (1 - completion) * 0.2f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(1f);


                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);

                foreach (MagnetizedDebris debris in AttachedDebris)
                {
                    float projectileAngle = -debris.angle - MathHelper.PiOver2; //The real angle
                    //Disco
                    projectileAngle += (float)Math.Sin(debris.random_telegraphInitialRotationDisplacement + (float)Math.Pow(1 - ActionTimer, 3.1f) * MathHelper.PiOver2 * debris.random_telegraphDiscoMovement) * (float)Math.Pow(1 - ActionTimer, 3.1f) * MathHelper.PiOver2;

                    float size = debris.random_telegraphLenghtMult;

                    laserScopeEffect.Parameters["laserLightStrenght"].SetValue(0.9f + debris.random_telegraphOpacity * 0.1f);
                    laserScopeEffect.Parameters["mainOpacity"].SetValue(lineOpacity * debris.random_telegraphOpacity);
                    laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(400f * size * 0.35f));
                    laserScopeEffect.Parameters["laserAngle"].SetValue(projectileAngle);

                    Main.spriteBatch.Draw(empty, NPC.Center + magnetDisplacement + gfxOffY - screenPos, null, Color.White, 0, empty.Size() / 2f, 500f * size, 0, 0);

                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            }

            Main.spriteBatch.Draw(backArmTex, NPC.Center + new Vector2(12 * NPC.spriteDirection, 8).RotatedBy(NPC.rotation) + gfxOffY - screenPos, null, drawColor, NPC.rotation * 3f, backArmTex.Size() / 2f, NPC.scale, flip, 0f);
            Main.spriteBatch.Draw(bodyTex, NPC.Center + gfxOffY - screenPos, frame, drawColor, NPC.rotation, frame.Size() / 2f, NPC.scale, flip, 0f);
            Main.spriteBatch.Draw(frontArmTex, NPC.Center + new Vector2(-14 * NPC.spriteDirection, 8).RotatedBy(NPC.rotation) + gfxOffY - screenPos, null, drawColor, NPC.rotation * 3f, frontArmTex.Size() / 2f, NPC.scale, flip, 0f);
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
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumMagnetizerGore" + i.ToString()).Type, 1f);
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
            npcLoot.Add(ModContent.ItemType<WulfrumBrandCereal>(), WulfrumBrandCereal.DroprateInt);
        }
    }

    [Serializable]
    public class SyncWulfrumMagnetizerScrapPacket : Module
    {
        int npc;
        int debrisCount;

        float[] angles;
        Vector2[] offsets;
        float[] collectionTime;

        public SyncWulfrumMagnetizerScrapPacket(NPC npc, List<WulfrumMagnetizer.MagnetizedDebris> debris)
        {
            this.npc = npc.whoAmI;
            debrisCount = debris.Count;

            angles = new float[debrisCount];
            offsets = new Vector2[debrisCount];
            collectionTime = new float[debrisCount];

            int i = 0;
            foreach (WulfrumMagnetizer.MagnetizedDebris scrap in debris)
            {
                angles[i] = scrap.angle;
                offsets[i] = scrap.originalOffset;
                collectionTime[i] = scrap.timeItTakesToReachDestination;
                i++;
            }
        }

        protected override void Receive()
        {
            if (Main.npc[npc].ModNPC != null && Main.npc[npc].ModNPC is WulfrumMagnetizer magnetizer)
            {
                magnetizer.AttachedDebris.Clear();
                for (int i = 0; i < debrisCount; i++)
                {
                    WulfrumMagnetizer.MagnetizedDebris debris = new WulfrumMagnetizer.MagnetizedDebris(1f, 1f, 1f);
                    debris.angle = angles[i];
                    debris.originalOffset = offsets[i];
                    debris.timeItTakesToReachDestination = collectionTime[i];
                    magnetizer.AttachedDebris.Add(debris);
                }
            }
        }
    }

    public class WulfrumScrapProjectile : ModProjectile
    {
        public override string Texture => AssetDirectory.WulfrumNPC + Name + "s";
        public int Variant => (int)Projectile.ai[0];
        public bool Explosive => Projectile.ai[1] == 1;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scrap");
            FablesSets.WulfrumProjectiles[Type] = true;
            ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
            Projectile.rotation = Main.rand.Next() * MathHelper.TwoPi;
        }

        public override void AI()
        {
            if (Explosive)
                CooldownSlot = 5; //5 is unique and therefore wont conflict with explosion iframes

            Projectile.rotation += Projectile.velocity.X.NonZeroSign() * 0.04f;

            if (Projectile.timeLeft < 150)
                Projectile.velocity.Y += 0.15f;

            if (Projectile.timeLeft < 190 && Main.rand.NextBool(2))
            {
                Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                Dust chust = Dust.NewDustPerfect(dustCenter, 15, -Projectile.velocity * Main.rand.NextFloat(0.6f, 1f), Scale: Main.rand.NextFloat(1f, 1.4f));
                chust.noGravity = true;

                if (!Main.rand.NextBool(5))
                    chust.noLightEmittence = true;
            }
        }


        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame;
            switch (Variant)
            {
                case 0:
                    frame = new Rectangle(0, 0, 12, 16);
                    break;
                case 1:
                    frame = new Rectangle(16, 2, 10, 12);
                    break;
                case 2:
                    frame = new Rectangle(30, 0, 14, 14);
                    break;
                case 3:
                    frame = new Rectangle(0, 18, 18, 14);
                    break;
                case 4:
                    frame = new Rectangle(22, 16, 10, 14);
                    break;
                case 5:
                default:
                    frame = new Rectangle(36, 18, 16, 14);
                    break;

            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0);
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Explosive)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    new KillServerProjectilePacket(Projectile).Send(runLocally: false);
                Projectile.Kill();
                target.immuneTime = 0;
            }
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if (Explosive)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
                {
                    info.Damage = 1;
                    info.SourceDamage = 1;
                };
            }
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            SoundEngine.PlaySound(WulfrumMagnetizer.ScrapHit, Projectile.Center);
            if (Main.netMode != NetmodeID.Server)
            {
                string goreVariant;
                switch (Variant)
                {
                    case 0:
                        goreVariant = "10";
                        break;
                    case 1:
                        goreVariant = "7";
                        break;
                    case 2:
                        goreVariant = "9";
                        break;
                    case 3:
                        goreVariant = "6";
                        break;
                    case 4:
                        goreVariant = "5";
                        break;
                    case 5:
                    default:
                        goreVariant = "8";
                        break;

                }

                Gore gore = Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.Center - Vector2.UnitY * 5f, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 7f), Mod.Find<ModGore>("WulfrumEnemyGore" + goreVariant).Type);
                gore.timeLeft = 20;
            }

            if (Explosive)
            {
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.position);
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile kaboom = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center - Vector2.UnitY * 10, Vector2.Zero, ModContent.ProjectileType<WulfrumMortarExplosion>(), (int)(Projectile.damage * 1.4f), 8, Main.myPlayer);

                    kaboom.position = kaboom.Center;
                    int newWidth = (int)(kaboom.width * (0.4f));
                    kaboom.width = kaboom.height = newWidth;
                    kaboom.Center = kaboom.position;

                    kaboom.friendly = false;
                }
            }
        }
    }

}

