namespace CalamityFables.Content.Items.EarlyGameMisc
{
    // (Excuse the examplemod comments lmao)
    public class CloudInAMace : ModItem
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Cloud in a Mace");
            Tooltip.SetDefault("Shatters on tile impact, sending enemies high into the air");
            ItemID.Sets.ToolTipDamageMultiplier[Type] = 2f;

            if (Main.dedServ)
                return;
            for (int i = 1; i < 4; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("CloudMaceGore" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.knockBack = 5.5f;
            Item.width = 32;
            Item.height = 32;
            Item.damage = 20;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<CloudInAMaceProjectile>();
            Item.shootSpeed = 12f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: 3);
            Item.DamageType = DamageClass.Melee;
            Item.channel = true;
            Item.noMelee = true;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Mace).
                AddIngredient(ItemID.CloudinaBottle).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class CloudInAMaceProjectile : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public string ChainTexturePath => AssetDirectory.EarlyGameMisc + Name + "_Chain"; // The folder path to the flail chain sprite

        private enum AIState
        {
            Spinning,
            LaunchingForward,
            Retracting,
            UnusedState,
            ForcedRetracting,
            Ricochet,
            Dropping
        }

        // These properties wrap the usual ai and localAI arrays for cleaner and easier to understand code.
        private AIState CurrentAIState {
            get => (AIState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }
        public ref float StateTimer => ref Projectile.ai[1];
        public ref float CollisionCounter => ref Projectile.localAI[0];
        public ref float SpinningStateTimer => ref Projectile.localAI[1];
        public bool Shattered;


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cloud in a Mace");

            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            // Vanilla flails all use aiStyle 15, but the code isn't customizable so an adaption of that aiStyle is used in the AI method
        }

        // This AI code was adapted from vanilla code: Terraria.Projectile.AI_015_Flails() 
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            // Kill the projectile if the player dies or gets crowd controlled
            if (!player.active || player.dead || player.noItems || player.CCed || Vector2.Distance(Projectile.Center, player.Center) > 900f)
            {
                Projectile.Kill();
                return;
            }
            if (Main.myPlayer == Projectile.owner && Main.mapFullscreen)
            {
                Projectile.Kill();
                return;
            }

            Vector2 mountedCenter = player.MountedCenter;
            bool shouldOwnerHitCheck = false;
            int launchTimeLimit = 15;  // How much time the projectile can go before retracting (speed and shootTimer will set the flail's range)
            float launchSpeed = 18f; // How fast the projectile can move
            float maxLaunchLength = 800f; // How far the projectile's chain can stretch before being forced to retract when in launched state
            float retractAcceleration = 3f; // How quickly the projectile will accelerate back towards the player while retracting
            float maxRetractSpeed = 10f; // The max speed the projectile will have while retracting
            float forcedRetractAcceleration = 6f; // How quickly the projectile will accelerate back towards the player while being forced to retract
            float maxForcedRetractSpeed = 15f; // The max speed the projectile will have while being forced to retract
            int defaultHitCooldown = 10; // How often your flail hits when resting on the ground, or retracting
            int spinHitCooldown = 20; // How often your flail hits when spinning
            int movingHitCooldown = 10; // How often your flail hits when moving

            // Scaling these speeds and accelerations by the players meleeSpeed make the weapon more responsive if the player boosts their meleeSpeed
            float meleeSpeed = player.GetAttackSpeed(DamageClass.Melee);
            float meleeSpeedMultiplier = meleeSpeed;
            launchSpeed *= meleeSpeedMultiplier;
            retractAcceleration *= meleeSpeedMultiplier;
            maxRetractSpeed *= meleeSpeedMultiplier;
            forcedRetractAcceleration *= meleeSpeedMultiplier;
            maxForcedRetractSpeed *= meleeSpeedMultiplier;
            float launchRange = launchSpeed * launchTimeLimit;
            float maxDroppedRange = launchRange + 160f;
            Projectile.localNPCHitCooldown = defaultHitCooldown;

            switch (CurrentAIState)
            {
                case AIState.Spinning:
                    {
                        shouldOwnerHitCheck = true;
                        if (Projectile.owner == Main.myPlayer)
                        {
                            Vector2 unitVectorTowardsMouse = mountedCenter.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * player.direction);
                            player.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
                            if (!player.channel) // If the player releases then change to moving forward mode
                            {
                                CurrentAIState = AIState.LaunchingForward;
                                StateTimer = 0f;
                                Projectile.velocity = unitVectorTowardsMouse * launchSpeed + player.velocity;
                                Projectile.Center = mountedCenter;
                                Projectile.netUpdate = true;
                                Projectile.ResetLocalNPCHitImmunity();
                                Projectile.localNPCHitCooldown = movingHitCooldown;
                                break;
                            }
                        }
                        SpinningStateTimer += 1f;
                        // This line creates a unit vector that is constantly rotated around the player. 10f controls how fast the projectile visually spins around the player
                        Vector2 offsetFromPlayer = new Vector2(player.direction).RotatedBy((float)Math.PI * 10f * (SpinningStateTimer / 60f) * player.direction);

                        offsetFromPlayer.Y *= 0.8f;
                        if (offsetFromPlayer.Y * player.gravDir > 0f)
                        {
                            offsetFromPlayer.Y *= 0.5f;
                        }
                        Projectile.Center = mountedCenter + offsetFromPlayer * 30f;
                        Projectile.velocity = Vector2.Zero;
                        Projectile.localNPCHitCooldown = spinHitCooldown; // set the hit speed to the spinning hit speed
                        break;
                    }
                case AIState.LaunchingForward:
                    {
                        bool shouldSwitchToRetracting = StateTimer++ >= launchTimeLimit;
                        shouldSwitchToRetracting |= Projectile.Distance(mountedCenter) >= maxLaunchLength;
                        if (player.controlUseItem) // If the player clicks, transition to the Dropping state
                        {
                            CurrentAIState = AIState.Dropping;
                            StateTimer = 0f;
                            Projectile.netUpdate = true;
                            Projectile.velocity *= 0.2f;
                            //DetachFlail(); //DETACH STYLE

                            break;
                        }
                        if (shouldSwitchToRetracting)
                        {
                            CurrentAIState = AIState.Retracting;
                            StateTimer = 0f;
                            Projectile.netUpdate = true;
                            Projectile.velocity *= 0.3f;

                            //DetachFlail(); //DETACH STYLE
                        }
                        player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
                        Projectile.localNPCHitCooldown = movingHitCooldown;
                        break;
                    }
                case AIState.Retracting:
                    {
                        Vector2 unitVectorTowardsPlayer = Projectile.DirectionTo(mountedCenter).SafeNormalize(Vector2.Zero);
                        if (Projectile.Distance(mountedCenter) <= maxRetractSpeed)
                        {
                            Projectile.Kill(); // Kill the projectile once it is close enough to the player
                            return;
                        }
                        if (player.controlUseItem) // If the player clicks, transition to the Dropping state
                        {
                            CurrentAIState = AIState.Dropping;
                            StateTimer = 0f;
                            Projectile.netUpdate = true;
                            Projectile.velocity *= 0.2f;
                        } //DELETE FOR DETACH STYLE
                        else
                        {
                            Projectile.velocity *= 0.98f;
                            Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsPlayer * maxRetractSpeed, retractAcceleration);
                            player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
                        }
                        break;
                    }
                case AIState.ForcedRetracting:
                    {
                        Projectile.tileCollide = false;
                        Vector2 unitVectorTowardsPlayer = Projectile.DirectionTo(mountedCenter).SafeNormalize(Vector2.Zero);
                        if (Projectile.Distance(mountedCenter) <= maxForcedRetractSpeed)
                        {
                            Projectile.Kill(); // Kill the projectile once it is close enough to the player
                            return;
                        }
                        Projectile.velocity *= 0.98f;
                        Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsPlayer * maxForcedRetractSpeed, forcedRetractAcceleration);
                        Vector2 target = Projectile.Center + Projectile.velocity;
                        Vector2 value = mountedCenter.DirectionFrom(target).SafeNormalize(Vector2.Zero);
                        if (Vector2.Dot(unitVectorTowardsPlayer, value) < 0f)
                        {
                            Projectile.Kill(); // Kill projectile if it will pass the player
                            return;
                        }
                        player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
                        break;
                    }
                case AIState.Dropping:
                    if (!player.controlUseItem || Projectile.Distance(mountedCenter) > maxDroppedRange)
                    {
                        CurrentAIState = AIState.ForcedRetracting;
                        StateTimer = 0f;
                        Projectile.netUpdate = true;
                    }
                    else
                    {
                        Projectile.velocity.Y += 0.8f;
                        Projectile.velocity.X *= 0.95f;
                        player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
                    }
                    break;
            }

            Projectile.direction = Projectile.velocity.X.NonZeroSign();
            Projectile.ownerHitCheck = shouldOwnerHitCheck; // This prevents attempting to damage enemies without line of sight to the player. The custom Colliding code for spinning makes this necessary.


            Vector2 vectorTowardsPlayer = Projectile.DirectionTo(mountedCenter).SafeNormalize(Vector2.Zero);
            Projectile.rotation = vectorTowardsPlayer.ToRotation() + MathHelper.PiOver2;
            if (CurrentAIState == AIState.Dropping)
                Projectile.rotation += Projectile.velocity.ToRotation() * Projectile.direction * 0.1f; //just gives a little tilt to the projectile when dropping.
            //(Looks a bit janky when going left, though. Shouldn't be much of an issue, but feel free to look into it if you want.)

            Projectile.timeLeft = 2; // Makes sure the flail doesn't die (good when the flail is resting on the ground)
            player.heldProj = Projectile.whoAmI;
            player.SetDummyItemTime(2); //Add a delay so the player can't button mash the flail
            player.itemRotation = Projectile.DirectionFrom(mountedCenter).ToRotation();
            if (Projectile.Center.X < mountedCenter.X)
            {
                player.itemRotation += (float)Math.PI;
            }
            player.itemRotation = MathHelper.WrapAngle(player.itemRotation);

            if (!Shattered && Main.rand.NextBool(2))
            {
                Vector2 dustOffset = Main.rand.NextVector2Circular(8f, 8f);

                Dust cloudDust = Dust.NewDustPerfect(Projectile.Center + dustOffset, DustID.Cloud, dustOffset * 0.2f, 100, default(Color), Main.rand.NextFloat(0.7f, 1.2f));
                cloudDust.velocity *= 0.5f;

            }
        }

        public void Shatter()
        {
            Projectile.alpha = 255; //Hides the projectile
            Shattered = true;
            CurrentAIState = AIState.ForcedRetracting;
            Projectile.netUpdate = true;

            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC target = Main.npc[i];
                    if (target.active && target.knockBackResist > 0f && !target.townNPC)
                    {
                        if (FablesUtils.AABBvCircle(target.Hitbox, Projectile.Center, 100f))
                        {
                            Vector2 knockback = new Vector2(Math.Clamp(target.Center.X - Projectile.Center.X, -2, 2), -12);
                            knockback *= (float)Math.Pow(target.knockBackResist, 0.2f);

                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                new KnockbackNPCPacket(target.whoAmI, knockback).Send(runLocally: false);
                            else
                                target.velocity += knockback;
                            target.GetGlobalNPC<FallDamageNPC>().ApplyFallDamage(target, 60, 5);
                        }
                    }
                }

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && (!player.noKnockback || i == Projectile.owner))
                    {
                        if (FablesUtils.AABBvCircle(player.Hitbox, Projectile.Center, 70f))
                        {
                            Vector2 knockback = new Vector2(Math.Clamp(player.Center.X - Projectile.Center.X, -6, 6), -16 * player.gravDir);


                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                new KnockbackPlayerPacket(player.whoAmI, knockback).Send(runLocally: false);

                            else
                            {
                                if (player.velocity.Y <= -7 * player.gravDir)
                                    knockback.Y *= 0.6f;
                                if (player.gravDir == 1)
                                    player.velocity.Y = Math.Min(player.velocity.Y, 4);
                                player.velocity += knockback;
                                player.fallStart = (int)(player.Bottom.Y / 16);
                            }
                        }
                    }
                }
            }

            if (Main.dedServ)
                return;

            if (Main.LocalPlayer.WithinRange(Projectile.Center, 800))
                CameraManager.Quake += 2;

            SoundEngine.PlaySound(SoundID.Shatter, Projectile.position);
            SoundEngine.PlaySound(SoundID.DoubleJump, Projectile.position);

            //Glass shatters
            for (int i = 0; i < Main.rand.Next(3, 4); i++)
            {
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.position, new Vector2(Main.rand.Next(-2, 3), Main.rand.Next(-2, 3)), Mod.Find<ModGore>("CloudMaceGore" + Main.rand.Next(1, 4).ToString()).Type);
                gore.timeLeft = 18;
            }

            //Cloud sprites
            for (int i = 0; i < Main.rand.Next(4, 7); i++)
            {
                Vector2 cloudOffset = Main.rand.NextVector2Circular(10f, 10f);
                if (cloudOffset.Y > 4)
                    cloudOffset.Y *= -1;

                Vector2 cloudSpeed = (cloudOffset * 0.2f) with { Y = cloudOffset.Y * 0.05f };

                Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position + cloudOffset, cloudSpeed, Main.rand.Next(11, 13));
            }

            //dusty
            for (int i = 0; i < 10; i++)
            {
                Vector2 dustOffset = Main.rand.NextVector2Circular(40f, 40f);
                if (dustOffset.Y > 0)
                    dustOffset.Y *= -1;

                Dust cloudDust = Dust.NewDustPerfect(Projectile.Center + dustOffset, DustID.Cloud, dustOffset * 0.2f, 100, default(Color), 1.5f);
                cloudDust.velocity *= 0.5f;
            }

            //Ring of smaller dust
            for (int i = 0; i < 14; i++)
            {
                Vector2 dustOffset = Vector2.UnitY.RotatedBy(i / 14f * MathHelper.TwoPi);
                Dust.NewDustPerfect(Projectile.Center + dustOffset * 10f, DustID.Cloud, dustOffset * Main.rand.NextFloat(0.2f, 0.7f), 100, default(Color), 1f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!Shattered && CurrentAIState != AIState.Spinning) //bandaid fix for the weird terraria slope flail bug. TRY TO FIND THE ACTUAL FIX, since that was done in a patch.
                Shatter();
            int defaultLocalNPCHitCooldown = 10;
            int impactIntensity = 0;
            Vector2 velocity = Projectile.velocity;
            float bounceFactor = 0.2f;
            if (CurrentAIState == AIState.LaunchingForward || CurrentAIState == AIState.Ricochet)
                bounceFactor = 0.4f;

            if (CurrentAIState == AIState.Dropping)
                bounceFactor = 0f;

            if (oldVelocity.X != Projectile.velocity.X)
            {
                if (Math.Abs(oldVelocity.X) > 4f)
                    impactIntensity = 1;

                Projectile.velocity.X = (0f - oldVelocity.X) * bounceFactor;
                CollisionCounter += 1f;
            }

            if (oldVelocity.Y != Projectile.velocity.Y)
            {
                if (Math.Abs(oldVelocity.Y) > 4f)
                    impactIntensity = 1;

                Projectile.velocity.Y = (0f - oldVelocity.Y) * bounceFactor;
                CollisionCounter += 1f;
            }

            // If in the Launched state, spawn sparks
            if (CurrentAIState == AIState.LaunchingForward)
            {
                CurrentAIState = AIState.Ricochet;
                Projectile.localNPCHitCooldown = defaultLocalNPCHitCooldown;
                Projectile.netUpdate = true;
                Point scanAreaStart = Projectile.TopLeft.ToTileCoordinates();
                Point scanAreaEnd = Projectile.BottomRight.ToTileCoordinates();
                impactIntensity = 2;
                Projectile.CreateImpactExplosion(2, Projectile.Center, ref scanAreaStart, ref scanAreaEnd, Projectile.width, out bool causedShockwaves);
                Projectile.CreateImpactExplosion2_FlailTileCollision(Projectile.Center, causedShockwaves, velocity);
                Projectile.position -= velocity;
            }

            // Here the tiles spawn dust indicating they've been hit
            if (impactIntensity > 0)
            {
                Projectile.netUpdate = true;
                for (int i = 0; i < impactIntensity; i++)
                {
                    Collision.HitTiles(Projectile.position, velocity, Projectile.width, Projectile.height);
                }

                SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            }

            // Force retraction if stuck on tiles while retracting
            if (CurrentAIState != AIState.UnusedState && CurrentAIState != AIState.Spinning && CurrentAIState != AIState.Ricochet && CurrentAIState != AIState.Dropping && CollisionCounter >= 10f)
            {
                CurrentAIState = AIState.ForcedRetracting;
                Projectile.netUpdate = true;
            }

            // tModLoader currently does not provide the wetVelocity parameter, this code should make the flail bounce back faster when colliding with tiles underwater.
            //if (Projectile.wet)
            //	wetVelocity = Projectile.velocity;

            return false;
        }

        public override bool? CanDamage()
        {
            // Flails in spin mode won't damage enemies within the first 12 ticks. Visually this delays the first hit until the player swings the flail around for a full spin before damaging anything.
            if ((CurrentAIState == AIState.Spinning && SpinningStateTimer <= 12f) || Shattered)
                return false;
            return base.CanDamage();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Flails do special collision logic that serves to hit anything within an ellipse centered on the player when the flail is spinning around the player. For example, the projectile rotating around the player won't actually hit a bee if it is directly on the player usually, but this code ensures that the bee is hit. This code makes hitting enemies while spinning more consistant and not reliant of the actual position of the flail projectile.
            if (CurrentAIState == AIState.Spinning)
            {
                Vector2 mountedCenter = Main.player[Projectile.owner].MountedCenter;
                Vector2 shortestVectorFromPlayerToTarget = targetHitbox.ClosestPointInRect(mountedCenter) - mountedCenter;
                shortestVectorFromPlayerToTarget.Y /= 0.8f; // Makes the hit area an ellipse. Vertical hit distance is smaller due to this math.
                float hitRadius = 55f; // The length of the semi-major radius of the ellipse (the long end)
                return shortestVectorFromPlayerToTarget.Length() <= hitRadius;
            }
            // Regular collision logic happens otherwise.
            return base.Colliding(projHitbox, targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Flails do 20% more damage while spinning
            if (CurrentAIState == AIState.Spinning)
                modifiers.SourceDamage *= 1.2f;

            // Flails do 100% more damage while launched or retracting. This is the damage the item tooltip for flails aim to match, as this is the most common mode of attack. This is why the item has ItemID.Sets.ToolTipDamageMultiplier[Type] = 2f;
            if (CurrentAIState == AIState.LaunchingForward || CurrentAIState == AIState.Retracting)
                modifiers.SourceDamage *= 2f;

            // The hitDirection is always set to hit away from the player, even if the flail damages the npc while returning
            modifiers.HitDirectionOverride = (Main.player[Projectile.owner].Center.X < target.Center.X) ? 1 : (-1);

            // Knockback is only 25% as powerful when in spin mode
            if (CurrentAIState == AIState.Spinning)
                modifiers.Knockback *= 0.25f;
            // Knockback is only 50% as powerful when in drop down mode
            if (CurrentAIState == AIState.Dropping)
                modifiers.Knockback *= 0.5f;
        }

        // PreDraw is used to draw a chain and trail before the projectile is drawn normally.
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 playerArmPosition = Main.GetPlayerArmPosition(Projectile);

            // This fixes a vanilla GetPlayerArmPosition bug causing the chain to draw incorrectly when stepping up slopes. The flail itself still draws incorrectly due to another similar bug. This should be removed once the vanilla bug is fixed.
            playerArmPosition.Y -= Main.player[Projectile.owner].gfxOffY;

            Asset<Texture2D> chainTexture = ModContent.Request<Texture2D>(ChainTexturePath);
            //Asset<Texture2D> chainTextureExtra = ModContent.Request<Texture2D>(ChainTextureExtraPath); // This texture and related code is optional and used for a unique effect

            Rectangle? chainSourceRectangle = null;
            // Drippler Crippler customizes sourceRectangle to cycle through sprite frames: sourceRectangle = asset.Frame(1, 6);
            float chainHeightAdjustment = 0f; // Use this to adjust the chain overlap. 

            Vector2 chainOrigin = chainSourceRectangle.HasValue ? (chainSourceRectangle.Value.Size() / 2f) : (chainTexture.Size() / 2f);
            Vector2 chainDrawPosition = Projectile.Center;
            Vector2 vectorFromProjectileToPlayerArms = playerArmPosition.MoveTowards(chainDrawPosition, 4f) - chainDrawPosition;
            Vector2 unitVectorFromProjectileToPlayerArms = vectorFromProjectileToPlayerArms.SafeNormalize(Vector2.Zero);
            float chainSegmentLength = (chainSourceRectangle.HasValue ? chainSourceRectangle.Value.Height : chainTexture.Height()) + chainHeightAdjustment;
            if (chainSegmentLength == 0)
                chainSegmentLength = 10; // When the chain texture is being loaded, the height is 0 which would cause infinite loops.
            float chainRotation = unitVectorFromProjectileToPlayerArms.ToRotation() + MathHelper.PiOver2;
            int chainCount = 0;
            float chainLengthRemainingToDraw = vectorFromProjectileToPlayerArms.Length() + chainSegmentLength / 2f;

            // This while loop draws the chain texture from the projectile to the player, looping to draw the chain texture along the path
            while (chainLengthRemainingToDraw > 0f)
            {
                // This code gets the lighting at the current tile coordinates
                Color chainDrawColor = Lighting.GetColor((int)chainDrawPosition.X / 16, (int)(chainDrawPosition.Y / 16f));

                // Flaming Mace and Drippler Crippler use code here to draw custom sprite frames with custom lighting.
                // Cycling through frames: sourceRectangle = asset.Frame(1, 6, 0, chainCount % 6);
                // This example shows how Flaming Mace works. It checks chainCount and changes chainTexture and draw color at different values

                var chainTextureToDraw = chainTexture;
                /*if (chainCount >= 4) {
                    // Use normal chainTexture and lighting, no changes
                }
                else if (chainCount >= 2) {
                    // Near to the ball, we draw a custom chain texture and slightly make it glow if unlit.
                    chainTextureToDraw = chainTexture;
                    byte minValue = 140;
                    if (chainDrawColor.R < minValue)
                        chainDrawColor.R = minValue;

                    if (chainDrawColor.G < minValue)
                        chainDrawColor.G = minValue;

                    if (chainDrawColor.B < minValue)
                        chainDrawColor.B = minValue;
                }
                else {
                    // Close to the ball, we draw a custom chain texture and draw it at full brightness glow.
                    chainTextureToDraw = chainTexture;
                    chainDrawColor = Color.White;
                }*/

                // Here, we draw the chain texture at the coordinates
                Main.spriteBatch.Draw(chainTextureToDraw.Value, chainDrawPosition - Main.screenPosition, chainSourceRectangle, chainDrawColor, chainRotation, chainOrigin, 1f, SpriteEffects.None, 0f);

                // chainDrawPosition is advanced along the vector back to the player by the chainSegmentLength
                chainDrawPosition += unitVectorFromProjectileToPlayerArms * chainSegmentLength;
                chainCount++;
                chainLengthRemainingToDraw -= chainSegmentLength;
            }

            // Add a motion trail when moving forward, like most flails do (don't add trail if already hit a tile)
            if (CurrentAIState == AIState.LaunchingForward)
            {
                Texture2D projectileTexture = TextureAssets.Projectile[Projectile.type].Value;
                Vector2 drawOrigin = new Vector2(projectileTexture.Width * 0.5f, Projectile.height * 0.5f);
                SpriteEffects spriteEffects = SpriteEffects.None;
                if (Projectile.spriteDirection == -1)
                    spriteEffects = SpriteEffects.FlipHorizontally;
                for (int k = 0; k < Projectile.oldPos.Length && k < StateTimer; k++)
                {
                    Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
                    Color color = Projectile.GetAlpha(lightColor) * ((float)(Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                    Main.spriteBatch.Draw(projectileTexture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale - k / (float)Projectile.oldPos.Length / 3, spriteEffects, 0f);
                }
            }
            return true;
        }
    }
    //just here for posteriety
    /*public class CloudInAFlailBounce : ModProjectile
    {
        public override string Texture => AssetDirectory.Assets + "Invisible";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cloud Bounce"); // The English name of the projectile
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
        }

        public override void SetDefaults()
        {
            Projectile.width = 75;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 1; //exists for only one frame
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;

            Projectile.localNPCHitCooldown = 20;
        }

        /*public Vector2 LinePositive => Projectile.Center + ((Projectile.rotation + 0.3f).ToRotationVector2() * 80);
        public Vector2 LineNegative => Projectile.Center - ((Projectile.rotation + 0.3f).ToRotationVector2() * 80);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float lmao = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, target, 40, ref lmao); //I think I technically don't need this but whateverit's here anyways
        }* /

        public override void AI()
        {
            for (int i = 0; i < Main.maxNPCs; i++) //doesn't work on players, but that'd honestly be super funny. Figure it out later, maybe?
            {
                NPC target = Main.npc[i];

                if (target.active && target.knockBackResist != 0f && !target.townNPC && target.Distance(Projectile.Center) <= 80)
                {
                    float lmao = 0f;
                    if (Collision.CheckAABBvLineCollision(target.Hitbox.TopLeft(), target.Hitbox.Size(), Projectile.Center, target.Center, 10, ref lmao))
                    {
                        target.velocity.Y -= 10 * (float)Math.Sqrt(target.knockBackResist);
                        target.GetGlobalNPC<CalamityFallDamageNPC>().ApplyFallDamage(target, 16, 5f);
                    }
                }
            }
            for (int i = 0; i < Main.rand.Next(4, 5); i++)
            {
                Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position, new Vector2(Main.rand.Next(-2, 3), 0f), Main.rand.Next(11, 13));
            }
        }

        public override bool? CanDamage()
        {
            return false; //damage not allowed
        }
    }*/
}