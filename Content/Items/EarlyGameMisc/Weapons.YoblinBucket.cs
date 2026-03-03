using Terraria.DataStructures;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class YoblinBucket : ModItem
    {
        public static readonly SoundStyle SummonYoblinSound = new SoundStyle("CalamityFables/Sounds/Yoblin/YoblinSummon", 6) { Volume = 0.6f, PitchVariance = 0.1f, MaxInstances = -1};
        public static readonly SoundStyle SummonYoblinRareSound = new SoundStyle("CalamityFables/Sounds/Yoblin/YoblinRare");
        public static readonly SoundStyle YoblinStepSound = new SoundStyle("CalamityFables/Sounds/Yoblin/YoblinStep", 4) { Volume = 0.5f, PitchVariance = 0.1f, MaxInstances = -1};
        public static readonly SoundStyle YoblinSplatSound = new SoundStyle("CalamityFables/Sounds/Yoblin/YoblinSplat") { Volume = 0.1f, MaxInstances = -1 };
        public static readonly SoundStyle YoblinScreamSound = new SoundStyle("CalamityFables/Sounds/Yoblin/YoblinScream") { Volume = 0.1f, PitchVariance = 0.3f, MaxInstances = -1 };
        public static readonly SoundStyle EmptyBucketSound = new SoundStyle("CalamityFables/Sounds/Yoblin/YoblinBucketEmpty") { PitchVariance = 0.3f, MaxInstances = -1 };

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetStaticDefaults()
        {
            FablesItem.CustomShimmerInteractionEvent.Add(Type, ShimmerIntoYoblinFountain);
        }

        public bool ShimmerIntoYoblinFountain(Item item)
        {
            Player spawnPlayer = Main.netMode == NetmodeID.SinglePlayer ? Main.LocalPlayer : item.GetNearestPlayer((p) => !p.DeadOrGhost);
            if (spawnPlayer != null)
                Projectile.NewProjectile(item.GetSource_FromThis(), item.Center, Vector2.Zero, ModContent.ProjectileType<ShimmerYoblinSpring>(), 2, 0, Main.myPlayer, item.stack * 150, spawnPlayer.whoAmI);

            item.stack = 0;
            item.type = ItemID.None;
            item.shimmerTime = 0f;
            item.active = false;
            

            if (Main.netMode == NetmodeID.SinglePlayer)
                Item.ShimmerEffect(item.Center);
            else
            {
                NetMessage.SendData(MessageID.ShimmerActions, -1, -1, null, 0, (int)item.Center.X, (int)item.Center.Y);
                NetMessage.SendData(MessageID.SyncItemsWithShimmer, -1, -1, null, item.whoAmI, 1f);
            }
            return false;
        }

        public override void SetDefaults()
        {
            Item.damage = 4;
            Item.ArmorPenetration = 4;

            Item.DamageType = DamageClass.Summon;
            Item.width = 20;
            Item.height = 20;
            Item.useTime = 4;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 1.1f;
            Item.value = Item.sellPrice(silver: 75);
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<YoblinBucketHoldout>();
            Item.autoReuse = true;
            Item.channel = true;
            Item.shootSpeed = 10f;
            Item.noUseGraphic = true;
            Item.useTurn = true;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, Main.rand.Next((int)SackOfInfiniteGunsProjectile.Style.Count));
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.EmptyBucket).
                AddIngredient(ItemID.FallenStar, 1).
                AddIngredient(ItemID.ClayBlock, 25).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }

    public class YoblinBucketHoldout : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public Player Owner => Main.player[Projectile.owner];

        public ref float SummonCooldown => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Owner.UpdateBasicHoldoutVariables(Projectile, 16, doItemRotation: false);
            Player player = Owner;
            Projectile.direction = Owner.direction;


            // Kill the projectile if the player dies or gets crowd controlled
            if (!player.active || player.dead || player.noItems || player.CCed || !player.channel || (player.whoAmI == Main.myPlayer && Main.mapFullscreen))
            {
                Projectile.Kill();
                return;
            }

            float animProgress = (SummonCooldown / 15f);
            animProgress = MathF.Sin(animProgress * 3f);

            Projectile.Center = new Vector2((14f + animProgress * 3) * player.direction, (9f - MathF.Pow(animProgress, 2f) * 6f) * player.gravDir) + player.MountedCenter;
            Projectile.rotation = (2.1f + animProgress * 0.9f) * player.direction;


            float armRotation = (-MathHelper.PiOver4 - animProgress * 0.2f) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            armRotation = (-MathHelper.PiOver4 * 1.6f - animProgress * 0.2f) * player.direction;
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation);



            if (SummonCooldown > 0)
                SummonCooldown--;
            else
            {
                SummonCooldown = 15;

                //Spawn a yoblin
                if (Owner.FrameStartMinionSlots() + 0.1f <= Owner.maxMinions)
                {
                    for (int i = 0; i < 14; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(3f, 5f), Main.rand.Next(139, 143), new Vector2(Owner.direction * Main.rand.NextFloat(1.4f, 3.2f), -2f) + Owner.velocity * 0.6f, 0, default, Main.rand.NextFloat(1f, 2f));
                        d.noGravity = true;
                    }

                    if (Main.myPlayer != Projectile.owner)
                        return;
                    int yobVariant = Main.rand.Next(5);
                    if (Main.rand.NextBool(100))
                        yobVariant = 5;

                    string ownerName = Owner.name.ToLower();
                    if (ownerName == "allan" || ownerName == "desmond")
                        yobVariant = 4;

                    int yob = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(Owner.direction * 14f, -6), ModContent.ProjectileType<Yoblin>(), Projectile.damage, Projectile.knockBack, Projectile.owner, yobVariant);
                    Main.projectile[yob].originalDamage = Projectile.originalDamage;
                    Main.projectile[yob].OriginalArmorPenetration = Projectile.OriginalArmorPenetration;
                    Main.projectile[yob].netUpdate = true;
                }
                else
                {
                    SoundEngine.PlaySound(YoblinBucket.EmptyBucketSound, Owner.position);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.direction = Owner.direction;
            Projectile.spriteDirection = Projectile.direction;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
            SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects);

            return false;
        }
    }

    public class YoblinBucketBuff : ModBuff
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;

        public override void Update(Player player, ref int buffIndex)
        {
            // Checks if the player with this buff has the minion
            // Renews the buff's time left and resets the player flag, removes the buff otherwise
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Yoblin>()] + player.ownedProjectileCounts[ModContent.ProjectileType<ShimmerYoblin>()] > 0)
            {
                int buffTime = 0;

                // Find yoblin with highest time left, that will be the time displayed on the buff icon
                foreach (Projectile projectile in Main.ActiveProjectiles)
                    if (projectile.ModProjectile is Yoblin && projectile.owner == player.whoAmI && projectile.timeLeft > buffTime)
                    {
                        buffTime = projectile.timeLeft;
                        if (projectile.timeLeft >= 3600)
                            break;
                    }

                player.buffTime[buffIndex] = buffTime;
                player.SetPlayerFlag(Name);
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }

    public class Yoblin : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "Yoblin";
        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            FablesSets.SubMinion[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.2f;
            Main.projFrames[Type] = 18;
            Main.projPet[Type] = true;
        }

        public int ColorVariant => (int)Projectile.ai[0];
        public bool Dead
        {
            get => Projectile.ai[1] == 1;
            set => Projectile.ai[1] = value ? 1 : 0;
        }
        public bool Flying
        {
            get => Projectile.ai[2] == 1;
            set
            {
                Projectile.ai[2] = value ? 1 : 0;
                Projectile.frame = Main.rand.Next(17);
            }
        }

        public ref float PirouetteTimer => ref Projectile.localAI[0];
        public bool Pirouetting
        {
            get => Projectile.localAI[1] == 1;
            set => Projectile.localAI[1] = value.ToInt();
        }
        public ref float PirouetteCooldown => ref Projectile.localAI[2];

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.VampireFrog);
            Projectile.aiStyle = -1;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.minionSlots = 0.1f;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.tileCollide = true;
            Projectile.localNPCHitCooldown = 13;
            Projectile.usesIDStaticNPCImmunity = false;
            Projectile.frame = Main.rand.Next(17);
            Projectile.timeLeft = 60 * 60;
        }

        public bool didSpawnEffects = false;

        public override bool MinionContactDamage() => !Dead && !Flying;
        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => (Dead || Flying) ? false : null;

        public override void AI()
        {
            if (!didSpawnEffects)
            {
                Owner.AddBuff(ModContent.BuffType<YoblinBucketBuff>(), 18000);
                SoundEngine.PlaySound(Main.rand.NextBool(5000) ? YoblinBucket.SummonYoblinRareSound : YoblinBucket.SummonYoblinSound, Projectile.Center);
                Projectile.hide = Projectile.whoAmI % 2 == 0;
                didSpawnEffects = true;
            }

            if (Projectile.shimmerWet)
                Projectile.ai[0] = 5;

            if (!Dead)
            {
                if (Owner.dead || !Owner.HasBuff<YoblinBucketBuff>())
                    Die();

                if (Flying)
                    GoFlying();
                else
                {
                    Projectile.tileCollide = true;
                    Projectile.shouldFallThrough = Owner.Bottom.Y - 12f > Projectile.Bottom.Y;
                    GoCrazyInsaneViolentInTheBasement();

                    if (Projectile.velocity.Y < 10)
                        Projectile.velocity.Y += Pirouetting ? 0.04f : 0.12f;
                }


                Projectile.frameCounter++;
                if (Projectile.frameCounter > (Flying ? 12 : 8))
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    Projectile.frame %= 18;
                }

                if (Pirouetting)
                    Projectile.frame = 10;

                if (Projectile.timeLeft < 30)
                    Die();
            }
            else
            {
                Projectile.Opacity = MathF.Min(1, Projectile.timeLeft / 80f);
                Projectile.velocity.X *= 0.97f;
                Projectile.tileCollide = false;
                Projectile.frame = 10;
                Projectile.rotation += MathF.Abs(Projectile.velocity.Y) * Projectile.velocity.X.NonZeroSign() * 0.16f;

                if (Projectile.velocity.Y < 10)
                    Projectile.velocity.Y += 0.57f;
            }

        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Projectile.hide)
                overPlayers.Add(index);
        }

        public void GoCrazyInsaneViolentInTheBasement()
        {
            int minionPos = Projectile.PerTypeMinionPos();

            float distanceToPlayer = Projectile.Distance(Owner.Center);
            float horizontalDistanceToPlayer = Math.Abs(Owner.Center.X - Projectile.Center.X);
            float verticalDistanceToPlayer = Math.Abs(Owner.Center.Y - Projectile.Center.Y);

            float offsetX = (minionPos / 2) * 26 * (minionPos % 2 - 0.5f);

            while (offsetX > 400)
                offsetX -= 399 * 2;
            while (offsetX < -400)
                offsetX += 399 * 2;



            Vector2 idealPosition = Owner.Center - Vector2.UnitX * Owner.direction * offsetX;
           
            int target = -1;
            float targettingRange = 700;
            Projectile.Minion_FindTargetInRange((int)targettingRange, ref target, true);
            NPC targetNPC = null;

            Projectile.shouldFallThrough = Owner.Bottom.Y - 12f > Projectile.Bottom.Y;
            float distanceToIdealRestPosition = Math.Abs(Projectile.Center.X - idealPosition.X);

            //Edge case mostly for when its spawned
            if (FablesUtils.FullSolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                Die();
                return;
            }

            //If not targetting anything, its allowed to go into flight mode to catch up
            if (target == -1)
            {
                //Die if too far away
                if (distanceToPlayer > 2000)
                {
                    Die(true);
                    return;
                }

                //Catch up to the player whle flying if too far
                else if (distanceToPlayer > 500 || verticalDistanceToPlayer > 300 || Owner.rocketDelay2 > 0)
                {
                    Flying = true;
                    Projectile.netUpdate = true;
                }
            }

            else
            {
                targetNPC = Main.npc[target];
                idealPosition = targetNPC.Center;
                Projectile.shouldFallThrough = targetNPC.Center.Y > Projectile.Bottom.Y;

                if (Projectile.IsInRangeOfMeOrMyOwner(targetNPC, targettingRange, out var _, out var _, out var _))
                {
                    if (Vector2.Distance(Projectile.Center, targetNPC.Center) < 30)
                    {
                        Projectile.velocity = Projectile.velocity.ClampMagnitude(10);
                        Projectile.netUpdate = true;
                    }

                    //Bounce on the enemies if they're in the air
                    Rectangle shortenedHitbox = targetNPC.Hitbox;
                    shortenedHitbox.Inflate(0, -8);
                    if (Projectile.Hitbox.Intersects(shortenedHitbox) && Projectile.velocity.Y >= 0f)
                    {
                        Projectile.velocity.Y = -10f;
                        Projectile.velocity.X += Math.Max(Math.Abs(Projectile.velocity.X * 0.2f), 5) * Projectile.velocity.X.NonZeroSign();
                        targetNPC.velocity.Y += 1f * targetNPC.knockBackResist;
                    }
                }

                idealPosition.X += 20 * (targetNPC.Center.X - Projectile.Center.X).NonZeroSign();
            }

            //Bounce
            bool canWaterBounce = !Projectile.shouldFallThrough && Projectile.velocity.Y >= 0 && Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height + 10);
            if (Projectile.velocity.Y == 0 || canWaterBounce)
            {
                PirouetteTimer = 0;
                Pirouetting = false;
                PirouetteCooldown++;

                float bounceStrenght = 9 + MathF.Sin(minionPos * 0.3f + Projectile.timeLeft) * 3;
                bounceStrenght += Utils.GetLerpValue(140, 300, distanceToIdealRestPosition, true) * 4f;

                //Check ahead for obstacles and increase the jump strenght if so
                if (bounceStrenght < 11)
                {
                    //Jump over obstacles
                    Point tilePosition = Projectile.Center.ToTileCoordinates();
                    tilePosition.X += Projectile.spriteDirection * 2;
                    tilePosition.X += (int)Projectile.velocity.X;

                    tilePosition.Y -= (int)(Utils.GetLerpValue(4, 7, bounceStrenght, true) * 2.9f);

                    for (int i = 0; i < 4; i++)
                    {
                        if (!WorldGen.InWorld(tilePosition.X, tilePosition.Y) || bounceStrenght > 11)
                            break;

                        Tile t = Main.tile[tilePosition];
                        if (WorldGen.SolidTile(t))
                            bounceStrenght += 2.5f;

                        tilePosition.Y--;
                    }
                }

                //Bounce less when the ideal position has been reached
                if (targetNPC == null && Owner.velocity.X == 0 && distanceToIdealRestPosition < 30)
                    bounceStrenght *= 0.7f + 0.3f * Utils.GetLerpValue(0f, 30f, distanceToIdealRestPosition, true);

                //Bounce higher to reach the target
                if (targetNPC != null)
                {
                    float enemyBounceStrenght = (float)Math.Sqrt(Math.Abs(Projectile.Center.Y - targetNPC.Center.Y) * 2f * 0.54f);
                    if (enemyBounceStrenght > 25)
                        enemyBounceStrenght = 25;
                    bounceStrenght = Math.Max(enemyBounceStrenght, bounceStrenght);
                }

                SoundEngine.PlaySound(YoblinBucket.YoblinStepSound with { Volume = 0.2f}, Projectile.Center);
                Projectile.velocity.Y = -bounceStrenght;

                if (Main.rand.NextBool(20) && PirouetteCooldown > 5)
                {
                    Pirouetting = true;
                    PirouetteCooldown = 0;
                    Projectile.velocity.Y *= 1.3f;
                    Projectile.netUpdate = true;
                }                
            }

            if (Projectile.velocity.Y < 10)
                Projectile.velocity.Y += 0.52f;

            //Go towards the target
            if (Math.Abs(Projectile.Center.X - idealPosition.X) > 10)
            {
                int direction = (idealPosition.X - Projectile.Center.X).NonZeroSign();
                float acceleration = 0.04f;

                if (Projectile.velocity.X.NonZeroSign() != direction)
                    acceleration *= 2f;

                float speed = Math.Max(Math.Abs(Owner.velocity.X), 6);
                if (targetNPC != null)
                    speed = 12;

                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, speed * direction, acceleration);
            }

            //Keep on bouncing
            else
            {
                Projectile.velocity.X *= 0.98f;
                if (Math.Abs(Projectile.velocity.X) < 0.1f)
                    Projectile.velocity.X = 0;
            }

            if (Pirouetting)
                PirouetteTimer += 1 / (60f * 0.7f);

            Projectile.rotation = Math.Clamp(Projectile.velocity.X * 0.04f * Utils.GetLerpValue(0, 5f, Projectile.velocity.Y), -0.3f, 0.3f);
        }

        public void GoFlying()
        {
            //Reset the count of bounces and other variables
            Pirouetting = false;
            Projectile.tileCollide = false;
            Projectile.shouldFallThrough = true;

            int target = -1;
            float targettingRange = 400;
            Projectile.Minion_FindTargetInRange((int)targettingRange, ref target, true);

            if (target != -1)
            {
                NPC targetNPC = Main.npc[target];
                Flying = false;
                Projectile.velocity = (targetNPC.Center - Projectile.Center) * 0.05f - Vector2.UnitY * 12f + Vector2.UnitX * Main.rand.NextFloat(-3f, 3f);
                Projectile.netUpdate = false;
                return;
            }

            int minionPos = Projectile.PerTypeMinionPos();

            //Ideal position is above the player
            Vector3 idealPosition3D = (Vector2.UnitX.RotatedBy(minionPos + Main.GlobalTimeWrappedHourly * 2f) * 50f).Vec3();
            Vector3 idealPosition3DoneFrameBefore = (Vector2.UnitX.RotatedBy(minionPos + Main.GlobalTimeWrappedHourly * 2f - 0.02f) * 50f).Vec3();
            Matrix threeDeeTransformation = Matrix.CreateRotationX(Main.GlobalTimeWrappedHourly * 0.95f + minionPos * 0.04f) * Matrix.CreateRotationY(Main.GlobalTimeWrappedHourly * 0.521f + minionPos * 0.02f);

            idealPosition3D = Vector3.Transform(idealPosition3D, threeDeeTransformation);
            idealPosition3DoneFrameBefore = Vector3.Transform(idealPosition3DoneFrameBefore, threeDeeTransformation);


            Projectile.hide = idealPosition3D.Z < 0;
            Vector2 idealPosition = idealPosition3D.Vec2() + Owner.MountedCenter + Owner.velocity;
            float distanceToIdealPosition = (Projectile.Center - idealPosition).Length();

            Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.085f;
            //Max velocity is either 4 or the speed of the player, so it never has trouble catching up
            float maxVelocity = Math.Max(Owner.velocity.Length(), 7);
            if (goalVelocity.Length() > maxVelocity)
                goalVelocity = goalVelocity.ClampMagnitude(maxVelocity);

                Projectile.velocity.X += goalVelocity.X * 0.1f;
                Projectile.velocity.Y += goalVelocity.Y * 0.06f;
                //A low lerp leads to curvier movement , which is nice when its hovering around its destination
                float lerpStrenght = 0.15f + Utils.GetLerpValue(100, 300, distanceToIdealPosition, true) * 0.07f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, lerpStrenght);
            

            Projectile.rotation = (idealPosition3D.Vec2() - idealPosition3DoneFrameBefore.Vec2()).ToRotation() + MathHelper.PiOver2;

            //Stop flying
            if (Projectile.WithinRange(Owner.Center, 200) && Owner.velocity.Y == 0f && Projectile.Bottom.Y <= Owner.Bottom.Y && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                Flying = false;
            //Teleport to the player if way too far
            if (distanceToIdealPosition > 2000)
                Die();
        }


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
                Die();
            else
                SoundEngine.PlaySound(YoblinBucket.YoblinSplatSound with { Volume = 0.5f }, Projectile.Center);
        }

        public void Die(bool instant = false)
        {
            if (instant)
            {
                Projectile.Kill();
                return;
            }

            if (Dead)
                return;

            Projectile.hide = true;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft = 100;
            Dead = true;
            SoundEngine.PlaySound(YoblinBucket.YoblinSplatSound, Projectile.position);
            SoundEngine.PlaySound(YoblinBucket.YoblinScreamSound, Projectile.position);

            Projectile.velocity = new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-12f, -3f));
            Projectile.netUpdate = true;
        }

        #region visuals & drawing
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.hide && ColorVariant != 5)
                lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(6, Main.projFrames[Type], ColorVariant, Projectile.frame, -2, -2);

            Vector2 drawPos = Projectile.Center;
            Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
            float scale = Projectile.scale;
            float rotation = Projectile.rotation;
            float extraRotation =0;

            if (Pirouetting)
                extraRotation += PirouetteTimer * MathHelper.TwoPi * Projectile.spriteDirection;
            rotation += extraRotation;

            Color color = Projectile.GetAlpha(lightColor);
            if (ColorVariant == 5)
            {
                color = Main.hslToRgb((Main.GlobalTimeWrappedHourly + Projectile.position.X * 0.0034f) % 1, 0.9f, 0.7f) * Projectile.Opacity;


                for (int i = ProjectileID.Sets.TrailCacheLength[Type] - 1; i >= 0; i--)
                {
                    float progression = 1 - i / (float)ProjectileID.Sets.TrailCacheLength[Type];
                    Color trailColor = color * 0.5f * (float)Math.Pow(progression, 2f);

                    Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, trailColor, Projectile.oldRot[i] + extraRotation, origin, scale, 0);
                }

            }

            Main.EntitySpriteDraw(texture, drawPos - Main.screenPosition, frame, color, rotation, origin, scale, 0);        
            return false;
        }
        #endregion
    }


    public class ShimmerYoblinSpring : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults() => base.SetStaticDefaults();

        public override void SetDefaults()
        {
            Projectile.hide = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true; 
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.ai[0] > 0)
            {
                Projectile.timeLeft = (int)Projectile.ai[0] * 6;
                Projectile.ai[0] = 0;
            }

            if (Projectile.timeLeft % 6 == 0)
            {
                if (Main.myPlayer == (int)Projectile.ai[1])
                {
                    int yob = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, -Vector2.UnitY.RotatedByRandom(0.8f) * Main.rand.NextFloat(7f, 20f), ModContent.ProjectileType<ShimmerYoblin>(), Projectile.damage, Projectile.knockBack, (int)Projectile.ai[1], 5);
                    Main.projectile[yob].originalDamage = Math.Max(Projectile.originalDamage, 1);
                    Main.projectile[yob].netUpdate = true;
                }

                Item.ShimmerEffect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f));
                CameraManager.Quake += 0.3f;
            }
        }
    }

    public class ShimmerYoblin : Yoblin
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.minionSlots = 0;
        }
    }
}