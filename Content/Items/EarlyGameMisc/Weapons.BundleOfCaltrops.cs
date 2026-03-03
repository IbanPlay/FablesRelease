namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class BundleOfCaltrops : ModItem
    {
        public static readonly SoundStyle SlapSound = new("CalamityFables/Sounds/BundleOfCaltropsHit");
        public static readonly SoundStyle ReleaseSound = new("CalamityFables/Sounds/BundleOfCaltropsRelease");

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += DropFromGoblinThieves;
        }

        private void DropFromGoblinThieves(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.GoblinThief)
            {
                npcloot.Add(Type, new Fraction(1, 40));
            }
        }

        public static float CHARGEGAINEDPERHIT = 0.6f;
        public static float SECONDSTOFULLYCHARGE = 4.5f;
        public static float SECONDSTOFIRSTCHARGE = 1.5f;
        public static int MINIMUMCALTROPS = 3;
        public static int MAXIMUMCALTROPS = 10;
        public static float CALTROPDAMAGEMULTIPLIER = 0.2f;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Bundle of Caltrops");
            Tooltip.SetDefault("Charges up caltrops as the flail spins, release to launch\n" +
					           "Dealing damage with the flail charges caltrops more quickly");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.knockBack = 1.5f;
            Item.width = 32;
            Item.height = 32;
            Item.damage = 24;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<BundleOfCaltropsProjectile>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 22);
            Item.DamageType = DamageClass.Melee;
            Item.channel = true;
            Item.noMelee = true;
        }


        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SpikyBall, 50).
                AddIngredient(ItemID.Cobweb, 50).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class BundleOfCaltropsProjectile : ModProjectile
    {
        #region Textures
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public string NeckTexturePath => AssetDirectory.EarlyGameMisc + Name + "Neck1";
        public string NeckEndTexturePath => AssetDirectory.EarlyGameMisc + Name + "Neck2";
        public string OpenNetTexturePath => AssetDirectory.EarlyGameMisc + Name + "Open";

        public static Asset<Texture2D> NeckAsset;
        public static Asset<Texture2D> NeckEndAsset;
        public static Asset<Texture2D> OpenNetAsset;

        public static Asset<Texture2D> NeckSilouetteAsset;
        public static Asset<Texture2D> NeckEndSilouetteAsset;
        public static Asset<Texture2D> NetSilouetteAsset;
        #endregion

        private enum AIState
        {
            Spinning,
            OpenedNet
        }

        private AIState CurrentAIState {
            get => (AIState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }
        public ref float StateTimer => ref Projectile.ai[1];
        public ref float CollisionCounter => ref Projectile.localAI[0];
        public ref float SpinningStateTimer => ref Projectile.localAI[1];

        public float SpinupBoost => Math.Clamp(SpinningStateTimer, 0f, 60f * BundleOfCaltrops.SECONDSTOFULLYCHARGE) / (60f * BundleOfCaltrops.SECONDSTOFULLYCHARGE);

        public bool fullChargeSFXPlayed = false;
        public float glowTimer = 0f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bundle of Caltrops");
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

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
            glowTimer--;

            switch (CurrentAIState)
            {
                case AIState.Spinning:
                    {
                        if (Projectile.owner == Main.myPlayer)
                        {
                            if (StateTimer > BundleOfCaltrops.SECONDSTOFULLYCHARGE * 60 && !fullChargeSFXPlayed)
                            {
                                glowTimer = 30;
                                fullChargeSFXPlayed = true;
                                SoundEngine.PlaySound(SoundID.MaxMana);
                            }

                            Vector2 unitVectorTowardsMouse = mountedCenter.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * player.direction);
                            player.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));

                            if (!player.channel) // If the player releases then release caltrops
                            {
                                CurrentAIState = AIState.OpenedNet;
                                SoundEngine.PlaySound(SoundID.DD2_GoblinBomberThrow, Projectile.Center);

                                Projectile.velocity = unitVectorTowardsMouse;
                                Projectile.Center = mountedCenter + Projectile.velocity * 10f;
                                Projectile.netUpdate = true;

                                ReleaseCaltrops();
                                StateTimer = 1f;
                                break;
                            }
                        }

                        StateTimer += 1f;
                        SpinningStateTimer += 1f;
                        Vector2 offsetFromPlayer = new Vector2(player.direction).RotatedBy((float)Math.PI * (6f + 1f * SpinupBoost) * (SpinningStateTimer / 60f) * player.direction);

                        offsetFromPlayer.Y *= 0.8f;
                        if (offsetFromPlayer.Y * player.gravDir > 0f)
                        {
                            offsetFromPlayer.Y *= 0.5f;
                        }
                        Projectile.Center = mountedCenter + offsetFromPlayer * (33f + 4f * SpinupBoost);

                        //Spawn dsut
                        Vector2 dustOffset = Main.rand.NextVector2Circular(8f, 8f);
                        Dust cloudDust = Dust.NewDustPerfect(Projectile.Center + offsetFromPlayer * Main.rand.NextFloat(5f, 17f) + dustOffset, 31, dustOffset * 0.2f, 100, default(Color), Main.rand.NextFloat(0.7f, 1.2f));
                        cloudDust.velocity *= 0.5f;
                        cloudDust.velocity = offsetFromPlayer.RotatedBy(MathHelper.PiOver2 * player.direction);

                        //Play a sound as we spin
                        Projectile.soundDelay--;
                        if (Projectile.soundDelay < 0)
                        {
                            Projectile.soundDelay = 40 - (int)(7 * SpinupBoost);
                            SoundEngine.PlaySound(SoundID.DD2_GoblinBomberThrow, Projectile.Center);
                        }

                        break;
                    }

                case AIState.OpenedNet:
                    {
                        if (StateTimer <= 0)
                        {
                            Projectile.Kill();
                            return;
                        }

                        StateTimer -= 1 / (60f * 0.3f);

                        Projectile.Center = player.MountedCenter + Projectile.velocity * 10f;
                        player.ChangeDir((player.Center.X < Projectile.Center.X) ? 1 : (-1));
                        break;
                    }
            }

            Projectile.direction = Projectile.velocity.X.NonZeroSign();
            Vector2 vectorTowardsPlayer = Projectile.DirectionTo(mountedCenter).SafeNormalize(Vector2.Zero);
            Projectile.rotation = vectorTowardsPlayer.ToRotation() + MathHelper.PiOver2;

            FablesUtils.GenericManipulatePlayerVariablesForHoldouts(Projectile);
        }

        public void ReleaseCaltrops()
        {
            int caltropType = ModContent.ProjectileType<Caltrop>();
            float chargeProgress = Utils.GetLerpValue(BundleOfCaltrops.SECONDSTOFIRSTCHARGE, BundleOfCaltrops.SECONDSTOFULLYCHARGE, StateTimer / 60f, true);

            SoundEngine.PlaySound(BundleOfCaltrops.ReleaseSound with { Volume = 0.4f + chargeProgress * 0.3f }, Projectile.Center);

            if (chargeProgress == 0 || Main.myPlayer != Projectile.owner)
                return;

            int caltropCount = BundleOfCaltrops.MINIMUMCALTROPS + (int)(chargeProgress * (BundleOfCaltrops.MAXIMUMCALTROPS - BundleOfCaltrops.MINIMUMCALTROPS));
            for (int i = 0; i < caltropCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.7f) * Main.rand.NextFloat(6f, 12f), caltropType, (int)(Projectile.damage * BundleOfCaltrops.CALTROPDAMAGEMULTIPLIER), 1, Projectile.owner, ai2: Main.MouseWorld.Y );
            }
        }

        public override bool? CanDamage()
        {
            // Flails in spin mode won't damage enemies within the first 12 ticks. Visually this delays the first hit until the player swings the flail around for a full spin before damaging anything.
            if ((CurrentAIState == AIState.Spinning && SpinningStateTimer <= 12f) || CurrentAIState == AIState.OpenedNet)
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
                float hitRadius = 85f + SpinupBoost * 10f; // The length of the semi-major radius of the ellipse (the long end)
                return shortestVectorFromPlayerToTarget.Length() <= hitRadius;
            }

            return base.Colliding(projHitbox, targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // The hitDirection is always set to hit away from the player
            modifiers.HitDirectionOverride = (Main.player[Projectile.owner].Center.X < target.Center.X) ? 1 : (-1);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            StateTimer += BundleOfCaltrops.CHARGEGAINEDPERHIT * 60f;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(BundleOfCaltrops.SlapSound with { Volume = 0.35f, PitchVariance = 0.3f, MaxInstances = 0 }, Projectile.Center);
        }

        // PreDraw is used to draw a chain and trail before the projectile is drawn normally.
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 playerArmPosition = Main.player[Projectile.owner].Center;
            playerArmPosition.Y -= Main.player[Projectile.owner].gfxOffY;

            //Load textures
            NeckAsset = NeckAsset ?? ModContent.Request<Texture2D>(NeckTexturePath);
            NeckEndAsset = NeckEndAsset ?? ModContent.Request<Texture2D>(NeckEndTexturePath);
            OpenNetAsset = OpenNetAsset ?? ModContent.Request<Texture2D>(OpenNetTexturePath);

            NeckSilouetteAsset = NeckSilouetteAsset ?? ModContent.Request<Texture2D>(NeckTexturePath + "_Silouette");
            NeckEndSilouetteAsset = NeckEndSilouetteAsset ?? ModContent.Request<Texture2D>(NeckEndTexturePath + "_Silouette");
            NetSilouetteAsset = NetSilouetteAsset ?? ModContent.Request<Texture2D>(Texture + "_Silouette");

            if (CurrentAIState == AIState.Spinning)
            {
                Texture2D flailTex = TextureAssets.Projectile[Projectile.type].Value;
                Texture2D neckTex = NeckAsset.Value;
                Texture2D neckEndTex = NeckEndAsset.Value;
                DrawBag(lightColor, flailTex, neckTex, neckEndTex);

                if (glowTimer > 0)
                {
                    flailTex = NetSilouetteAsset.Value;
                    neckTex = NeckSilouetteAsset.Value;
                    neckEndTex = NeckEndSilouetteAsset.Value;

                    Color glowColor = Color.Goldenrod * (glowTimer / 30f);
                    glowColor.A = (byte)(40f - 40f * (glowTimer / 30f));

                    DrawBag(glowColor, flailTex, neckTex, neckEndTex);
                }
            }

            //Draw the open net (ez)
            if (CurrentAIState == AIState.OpenedNet)
            {
                SpriteEffects spriteEffects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Texture2D openNetTexture = OpenNetAsset.Value;
                Vector2 drawOrigin = new Vector2(openNetTexture.Width * 0.5f, openNetTexture.Height);

                float scale = 1.3f * (float)Math.Pow(1 - StateTimer, 0.1f) * (float)Math.Pow(StateTimer, 0.2f);

                Main.EntitySpriteDraw(openNetTexture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.Pi, drawOrigin, Projectile.scale * scale, spriteEffects, 0);
            }

            return false;
        }

        public void DrawBag(Color drawColor, Texture2D flailTex, Texture2D neckTex, Texture2D neckEndTex)
        {
            SpriteEffects spriteEffects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 rotationVector = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2();

            Vector2 neckPosition = Projectile.Center - rotationVector * (flailTex.Height / 2 - 8);
            Vector2 neckOrigin = new Vector2(neckTex.Width * 0.5f, neckTex.Height);
            Vector2 neckEndPosition = neckPosition - rotationVector * (neckTex.Height);
            Vector2 neckEndOrigin = new Vector2(neckEndTex.Width * 0.5f, neckEndTex.Height);

            //Epic stretch fail
            //Vector2 neckEndPosition = playerArmPosition + rotationVector * 10f;
            //Vector2 neckEndOrigin = new Vector2(neckEndTex.Width * 0.5f, 0);
            //Vector2 neckPosition = Projectile.Center - rotationVector * (flailTex.Height / 2 - 8);
            //Vector2 neckScale = new Vector2(1, (neckPosition - neckEndPosition - rotationVector * neckEndTex.Height).Length() / neckTex.Height) * Projectile.scale;
            //Vector2 neckOrigin = new Vector2(neckTex.Width * 0.5f, neckTex.Height);

            Main.EntitySpriteDraw(neckTex, neckPosition - Main.screenPosition, null, drawColor, Projectile.rotation, neckOrigin, Projectile.scale, spriteEffects, 0);
            Main.EntitySpriteDraw(neckEndTex, neckEndPosition - Main.screenPosition, null, drawColor, Projectile.rotation, neckEndOrigin, Projectile.scale, spriteEffects, 0);
            Main.EntitySpriteDraw(flailTex, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.rotation, flailTex.Size() / 2f, Projectile.scale, spriteEffects, 0);

        }
    }


    public class Caltrop : ModProjectile
    {
        public static readonly SoundStyle BounceSound = new("CalamityFables/Sounds/CaltropTink") { PitchVariance = 0.4f };
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Caltrop");
        }

        public float NoFallThroughHeight => Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.timeLeft = 60 * 15;
            Projectile.ArmorPenetration = 6;

            Projectile.aiStyle = ProjAIStyleID.GroundProjectile;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            if (Projectile.Bottom.Y >= NoFallThroughHeight)
                fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.Length() > 2f)
                SoundEngine.PlaySound(BounceSound with { Volume = BounceSound.Volume * Main.rand.NextFloat(0.3f, 0.6f), MaxInstances = 0 }, Projectile.Center);
            Projectile.velocity *= 0.5f;
            return base.OnTileCollide(oldVelocity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;

            float opacity = 1f;
            if (Projectile.timeLeft < 100f)
                opacity = Projectile.timeLeft / 100f;

            Main.EntitySpriteDraw(tex, Projectile.Center + Vector2.UnitY * 2f - Main.screenPosition, null, Projectile.GetAlpha(lightColor) * opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}