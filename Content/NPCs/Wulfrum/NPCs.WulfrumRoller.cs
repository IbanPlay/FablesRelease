using CalamityFables.Content.Items.Food;
using CalamityFables.Content.Items.Wulfrum;
using System.IO;
using Terraria.GameContent.Bestiary;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    [ReplacingCalamity("WulfrumGyrator")]
    public class WulfrumRoller : ModNPC, ISuperchargable
    {
        public override string Texture => AssetDirectory.WulfrumNPC + Name;

        public static readonly SoundStyle GearClick = new(SoundDirectory.Wulfrum + "WulfrumRollerGearClick") { Volume = 0.6f, MaxInstances = 0 };
        public static readonly SoundStyle JumpUp = new(SoundDirectory.Wulfrum + "WulfrumRollerJump") { Volume = 0.6f, MaxInstances = 1, Identifier = "Roller", SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        public Player target => Main.player[NPC.target];
        public ref float AIState => ref NPC.ai[0];
        public ref float ChargeTimer => ref NPC.ai[1];
        public bool IsCharging {
            get {
                return NPC.ai[2] != 0;
            }
            set {
                NPC.ai[2] = value ? 1 : 0;
            }
        }
        public bool IsBeingMagnetized {
            get => NPC.ai[3] > 0f;
            set => NPC.ai[3] = value ? 1f : 0f;
        }


        public NPC RoverToCharge = default;


        public ref float CogRotation => ref NPC.localAI[0];
        public ref float CogSoundDelay => ref NPC.localAI[1];
        public ref float CogRotationOverride => ref NPC.localAI[2];

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
            BannerType = BannerLoader.LoadBanner(Name, "Wulfrum Roller", AssetDirectory.WulfrumBanners, out bannerTile);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Roller");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1

            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
            Main.npcFrameCount[Type] = 2;
            FablesSets.WulrumNPCs[Type] = true;
            bannerTile.NPCType = Type;

            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumRollerGore" + i.ToString()).Type] = true;
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

            //NPC.behindTiles = true; <= I don't know why this is here, this is making them draw behind doors, so ill remove it. Hopefully no issues arise?
            Banner = NPC.type;
            BannerItem = BannerType;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            if (Main.masterMode)
                NPC.damage = (int)(NPC.damage * 0.75f);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(RoverToCharge == default ? -1 : RoverToCharge.whoAmI);
            writer.Write((byte)_supercharged.ToInt());
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int roverToChargeIndex = reader.ReadInt32();
            RoverToCharge = roverToChargeIndex >= 0 ? Main.npc[roverToChargeIndex] : default;

            _supercharged = reader.ReadByte() != 0;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.WulfrumRoller")
            });
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (IsBeingMagnetized && projectile.type == ModContent.ProjectileType<WulfrumMortarExplosion>())
                return false;
            return null;
        }

        public override void AI()
        {
            //Prevent the on-spawn "stuck" issue by forcing it to have an initial direction
            if (NPC.direction == 0)
            {
                NPC.TargetClosest(false);
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
            }


            //Idle movement
            if (AIState == 0)
            {
                NPC.noGravity = false;
                NPC.TargetClosest(false);
                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

                NPC.ai[3] -= 0.5f;

                bool immobilized = IsBeingMagnetized;
                bool outToHelpAFriend = false;

                //Decide on where to go. By default, the closest players center, but if helping out a rover, it will instead try to follow it
                Vector2 movementTarget = target.Center;

                if (!immobilized)
                {
                    outToHelpAFriend = ComboLogic(out Vector2 newTarget, out immobilized);
                    if (outToHelpAFriend)
                        movementTarget = newTarget;
                }


                //If not immobilized (aka not directly charging the rover)
                if (!immobilized)
                {
                    //Reset its knockback resistance (The charge and immobilization grant it full kb resistance)
                    NPC.knockBackResist = 0.15f;

                    //Change directions if close enough but not too close enough
                    float distanceToTargetX = Math.Abs(movementTarget.X - NPC.Center.X);
                    if (distanceToTargetX > 80 && (Aggroed || (distanceToTargetX < 600 && Collision.CanHitLine(movementTarget - NPC.Hitbox.Size() / 2, NPC.width, NPC.height, NPC.position, NPC.width, NPC.height))))
                    {
                        int direction = (movementTarget.X - NPC.Center.X).NonZeroSign();
                        if (NPC.direction != direction)
                        {
                            NPC.direction = direction;
                            NPC.netSpam = 0;
                            NPC.netUpdate = true;
                        }
                    }

                    //Accelerates forward (faster if damaged)
                    float maxXSpeed = NPC.life == NPC.lifeMax ? 4f : 6f;

                    //Simply accelerate in the direction of the movement
                    if (!IsCharging)
                    {
                        NPC.velocity.X += 0.05f * NPC.direction;
                        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * maxXSpeed, 0.01f);
                        NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxXSpeed, maxXSpeed);
                    }

                    //If charging, we cant just clamp the velocity like usual
                    else
                    {
                        Dust chust = Dust.NewDustPerfect(NPC.Top + Vector2.UnitY * NPC.height * Main.rand.NextFloat(), 178, -NPC.velocity * 0.4f * Main.rand.NextFloat(0.6f, 1f), Scale: Main.rand.NextFloat(0.5f, 1f));
                        chust.noGravity = true;

                        //Simply slow down by lerping the actual velocity towards the wanted max X speed, but not clamping it
                        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * maxXSpeed, 0.01f);

                        //Stop charging if the speed is low enough or if bumping a wall
                        if (NPC.collideX || Math.Abs(NPC.velocity.X) < maxXSpeed)
                            IsCharging = false;
                    }

                    //Jump if encountering a wall
                    if (NPC.collideX && NPC.velocity.Y == 0)
                    {
                        SoundEngine.PlaySound(JumpUp, NPC.Center);
                        NPC.velocity.Y = -8;

                        //Swap directions if the player is on the other side
                        if ((movementTarget.X - NPC.Center.X).NonZeroSign() != NPC.direction)
                            NPC.direction *= -1;
                        NPC.netUpdate = true;
                    }

                    // Jump if there's an gap ahead.
                    if (NPC.collideY && NPC.velocity.Y == 0f && (movementTarget.Y - target.height / 2) < NPC.Bottom.Y && HoleAtPosition(NPC.Center.X + NPC.velocity.X * 4f))
                    {
                        SoundEngine.PlaySound(JumpUp, NPC.Center);
                        NPC.velocity.Y = -8;
                        NPC.netUpdate = true;
                    }

                    //Head tilts in the direction of the velocity
                    NPC.rotation = NPC.velocity.X * 0.058f;
                    NPC.rotation = MathHelper.Clamp(NPC.rotation, -MathHelper.PiOver4 * 0.5f, MathHelper.PiOver4 * 0.5f);

                    //In expert mode, charge at the player (not if theres a rover to be charged)
                    if (!outToHelpAFriend && (Main.expertMode || Main.getGoodWorld || IsSupercharged))
                    {
                        //Only charge if line of sight with the player
                        if (Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height))
                            ChargeTimer += Main.getGoodWorld ? 5f : 1f;

                        //Slower attacks if the player has brand embassador
                        float chargeTime = (!Aggroed && target.GetPlayerFlag("WulfrumAmbassador")) ? 10f : 5f;

                        if (ChargeTimer > chargeTime * 60f)
                        {
                            ChargeTimer = 1f;
                            AIState = 1;
                            IsCharging = false;
                            NPC.knockBackResist = 0f;
                            NPC.netUpdate = true;
                        }
                    }
                }
            }

            //Charge-dash
            else if (AIState == 1)
            {
                NPC.noGravity = true;
                NPC.rotation = MathHelper.Lerp(NPC.rotation, 0f, 0.04f);
                NPC.knockBackResist *= 0.9f; //Increase the knockback resistance

                //Start by slowing down
                if (!IsCharging)
                {
                    if (Math.Abs(NPC.velocity.X) > 0.5f)
                        NPC.velocity *= 0.9f;

                    else
                        IsCharging = true;
                }

                //If slowed down sufficiently
                else
                {
                    //Chargeup in place
                    ChargeTimer -= 1 / (60f * 0.5f);
                    NPC.velocity = Vector2.Zero;

                    //And dash when finished charging
                    if (ChargeTimer <= 0)
                    {
                        //Aim at the player
                        Vector2 speed = (target.Center - NPC.Center) * 0.15f;

                        //Allow it to dash down only if the player is below the roller and in clear line of sight
                        float maxDownSpeed = -2;
                        float minXSpeed = 3.7f;
                        if (Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height) && NPC.Bottom.Y < target.Top.Y)
                        {
                            maxDownSpeed = 8;
                            minXSpeed = 0f;
                        }

                        //Clamp the velocity
                        speed.X = Math.Clamp(Math.Abs(speed.X) * 1.2f, minXSpeed, 12f) * Math.Sign(speed.X);
                        speed.Y = Math.Clamp(speed.Y, -9, maxDownSpeed);
                        NPC.velocity = speed;

                        //Reset back to rolling
                        AIState = 0;

                        //Sounds and visuals
                        SoundEngine.PlaySound(JumpUp, NPC.Center);
                        for (int i = 0; i < 14; i++)
                        {
                            Dust chust = Dust.NewDustPerfect(NPC.Center, 178, Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.6f, 1f), Scale: Main.rand.NextFloat(1f, 2.2f));
                            chust.noGravity = true;
                        }
                        NPC.netUpdate = true;
                    }
                }
            }

            if (IsBeingMagnetized)
            {
                NPC.velocity *= 0.9f;
                if (NPC.velocity.Length() < 1f)
                    NPC.velocity = Vector2.Zero;
            }

            //Make the cog spin and play sounds
            AnimateCog();

            //Dust particles on the floor when it goes fast
            if (Math.Abs(NPC.velocity.X) > 4.5f && Main.rand.NextBool() && NPC.collideY)
            {
                WorldGen.KillTile((int)(NPC.Center.X / 16), (int)((NPC.position.Y + NPC.height) / 16), true, true, true);
            }
        }

        public bool ComboLogic(out Vector2 movementTarget, out bool immobilized)
        {
            bool comboing = false;
            movementTarget = Vector2.Zero;
            immobilized = false;

            //If not currently charging a rover
            if (RoverToCharge == default)
            {
                //Find the closest rover to boost
                int otherNearbyNPCs = 0;
                List<NPC> nearbyRovers = new List<NPC>();

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];

                    if (n.active && !n.friendly && !n.CountsAsACritter && NPC.Distance(n.Center) < 400)
                    {
                        otherNearbyNPCs++;

                        if (n.type == ModContent.NPCType<WulfrumRover>())
                        {
                            //Do not count rovers as nearby NPCs
                            otherNearbyNPCs--;

                            if (n.localAI[0] == 0)
                                nearbyRovers.Add(n);
                        }

                        //Do not count yourself as a nearby NPC
                        else if (NPC.whoAmI == n.whoAmI)
                            otherNearbyNPCs--;
                    }
                }

                //Pick the closest rover to help out if any
                if (otherNearbyNPCs > 0)
                {
                    RoverToCharge = nearbyRovers.OrderBy(n => NPC.Distance(n.Center)).FirstOrDefault();
                    NPC.netUpdate = true;
                }
            }

            //If theres a rover to charge
            if (RoverToCharge != default)
            {
                //If the rover died, break out of the combo
                if (!RoverToCharge.active || NPC.Distance(RoverToCharge.Center) > 400)
                {
                    RoverToCharge.Rover_StopBeingCharged();
                    RoverToCharge = default;
                    NPC.netUpdate = true;
                    return false;
                }

                //If theres no other enemy nearby, break out of the combo
                NPC nearbyAttacker = Main.npc.Where(n => n.active && !n.friendly && !n.CountsAsACritter && n.whoAmI != NPC.whoAmI && n.type != NPCID.TargetDummy && n.type != ModContent.NPCType<WulfrumRover>() && NPC.Distance(n.Center) < 500)
                .FirstOrDefault();
                if (nearbyAttacker == default)
                {
                    RoverToCharge.Rover_StopBeingCharged();
                    RoverToCharge = default;
                    NPC.netUpdate = true;
                    return false;
                }

                movementTarget = RoverToCharge.Center; //Moves toward the rover
                comboing = true;
                RoverToCharge.Rover_StartingRollerCombo(); //Makes the rover realize its got a combo partner

                //Hop ONTOP of the rover if close enough
                Vector2 theExactSpot = RoverToCharge.Center - Vector2.UnitY * 33f;
                if (NPC.Distance(theExactSpot) < 60)
                {
                    immobilized = true;
                    NPC.velocity = Vector2.Zero;

                    NPC.Center = Vector2.Lerp(NPC.Center, theExactSpot, 0.1f);
                    if (NPC.Distance(theExactSpot) < 5f)
                    {
                        NPC.knockBackResist = 0f; //Gain full kb resist
                        NPC.Center = theExactSpot + Main.rand.NextVector2Circular(1f, 1f); //Shake around ontop of the rover

                        RoverToCharge.Rover_GetChargedByRoller(2f); //Make the rover realize its being charged up and electrify the shield
                        NPC.netUpdate = true;
                    }
                }
            }

            return comboing;
        }



        public void AnimateCog()
        {
            float cogMovement = (AIState == 0 ? NPC.velocity.X * 0.029f : 0.1f * NPC.direction);

            if (IsBeingMagnetized)
                cogMovement = CogRotationOverride;

            if (RoverToCharge != default && NPC.Distance(RoverToCharge.Center - Vector2.UnitY * 33f) <= 5)
                cogMovement = RoverToCharge.direction * -0.12f;

            CogRotation += cogMovement;
            if (CogSoundDelay <= 0)
            {
                SoundEngine.PlaySound(GearClick, NPC.Center);
                CogSoundDelay = 1f;
            }
            CogSoundDelay -= Math.Abs(cogMovement);
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

        public override bool? CanFallThroughPlatforms()
        {
            return target.Top.Y > NPC.Bottom.Y;
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
                CogRotation += 0.04f;
        }

        //Cant hit the player when charging a rover
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return RoverToCharge == default || NPC.Distance(RoverToCharge.Center - Vector2.UnitY * 33) >= 5;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);
            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;

            Texture2D headTex = TextureAssets.Npc[Type].Value;
            Texture2D cogTex = ModContent.Request<Texture2D>(Texture + "_Gear").Value;

            Rectangle greenHeadFrame = new Rectangle(0, 0, headTex.Width, headTex.Height / 2 - 2);
            Rectangle blueHeadFrame = new Rectangle(0, headTex.Height / 2, headTex.Width, headTex.Height / 2 - 2);

            Vector2 gfxOffY = NPC.GfxOffY() + Vector2.UnitY * (float)Math.Pow(Terraria.Utils.GetLerpValue(3f, 5f, Math.Abs(NPC.velocity.X), true), 1.5f) * 5f;
            Vector2 gfxOffYHead = NPC.GfxOffY() + Vector2.UnitY * (float)Math.Pow(Terraria.Utils.GetLerpValue(3.45f, 5.45f, Math.Abs(NPC.velocity.X), true), 1.5f) * 5f;

            if (Math.Abs(NPC.velocity.X) > 3)
            {
                float glowOpacity = Utils.GetLerpValue(3f, 6f, Math.Abs(NPC.velocity.X), true);

                for (int i = 0; i < NPC.oldPos.Length; i++)
                {
                    float progress = 1 - i / (float)NPC.oldPos.Length;
                    float gearRotation = CogRotation - (1 - progress) * NPC.direction * 0.3f;
                    float afterImageSize = NPC.scale * (float)Math.Pow(progress, 0.7f);

                    Vector2 drawPos = (NPC.oldPos[i] - screenPos) + gfxOffY + cogTex.Size() / 2f;
                    Color color = CommonColors.WulfrumGreen * glowOpacity * (float)Math.Pow(progress, 2f);
                    Main.EntitySpriteDraw(cogTex, drawPos, null, color with
                    {
                        A = 0
                    }, gearRotation, cogTex.Size() / 2f, afterImageSize, SpriteEffects.None, 0);
                }
            }

            Main.spriteBatch.Draw(cogTex, NPC.Center + gfxOffY - screenPos, null, drawColor, CogRotation, cogTex.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(headTex, NPC.Center + gfxOffYHead - screenPos, greenHeadFrame, drawColor, NPC.rotation, greenHeadFrame.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);

            if ((AIState == 1 || IsBeingMagnetized) && IsCharging)
            {
                float blueOpacity = Utils.GetLerpValue(1f, 0.75f, ChargeTimer, true);
                Texture2D cogOutline = ModContent.Request<Texture2D>(Texture + "_GearOutline").Value;
                Main.spriteBatch.Draw(cogOutline, NPC.Center + gfxOffY - screenPos, null, CommonColors.WulfrumBlue * blueOpacity, CogRotation, cogOutline.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);

                blueOpacity = Utils.GetLerpValue(1f, 0.4f, ChargeTimer, true);
                Main.spriteBatch.Draw(headTex, NPC.Center + gfxOffYHead - screenPos, blueHeadFrame, drawColor * blueOpacity, NPC.rotation, blueHeadFrame.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);
            }

            return false;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo) => WulfrumCollaborationHelper.WulfrumGoonSpawnChance(spawnInfo);

        public override void OnKill()
        {
            //Make the rover realize its no longer paired with a patrtner
            if (RoverToCharge != default && RoverToCharge.active)
            {
                RoverToCharge.Rover_StopBeingCharged();
            }
        }

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
                    for (int k = 0; k < 20; k++)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, 3, hit.HitDirection, -1f, 0, default, 1f);
                    }

                    bool splitHead = Main.rand.NextBool();
                    bool splitGear = Main.rand.NextBool();
                    int randomGoreCount = Main.rand.Next(0, 2);


                    for (int i = 0; i < randomGoreCount; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumEnemyGore" + Main.rand.Next(1, 11).ToString()).Type, 1f);
                    }

                    if (splitGear)
                    {
                        for (int i = 1; i < 3; i++)
                            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumRollerGore" + i.ToString()).Type, 1f);
                    }
                    else
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumRollerGore0").Type, 1f);

                    if (splitHead)
                    {
                        for (int i = 4; i < 6; i++)
                            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumRollerGore" + i.ToString()).Type, 1f);
                    }
                    else
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumRollerGore3").Type, 1f);

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
}
