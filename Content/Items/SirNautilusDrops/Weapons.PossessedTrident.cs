using CalamityFables.Core.DrawLayers;
using Terraria.Enums;

namespace CalamityFables.Content.Items.SirNautilusDrops
{
    [ReplacingCalamity("ScourgeoftheDesert")]
    public class PossessedTrident : ModItem
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + "NautilusTrident";

        public static SoundStyle SwingSound = new SoundStyle(SoundDirectory.NautilusDrops + "PossessedTridentSwing", 2) { MaxInstances = 0, Volume = 0.8f };
        public static SoundStyle GhostSwingSound = new SoundStyle(SoundDirectory.NautilusDrops + "PossessedTridentGhostSwing", 2) { MaxInstances = 0, Volume = 0.5f };
        public static SoundStyle GhostVoiceSound = new SoundStyle(SoundDirectory.NautilusDrops + "PossessedTridentGhostVoice", 3) { MaxInstances = 0 };

        public static float MULTIHIT_FALLOFF = 0.85f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Possessed Trident");
            Tooltip.SetDefault("Conjures a ghostly companion with every thrust");
            ItemID.Sets.Spears[Type] = true;
            ItemID.Sets.BonusAttackSpeedMultiplier[Type] = 0.25f;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 56;

            Item.damage = 18;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
            Item.useAnimation = 28;
            Item.useTime = 28;
            Item.knockBack = 12f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.UseSound = SwingSound with { Volume = 0.7f };
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);

            Item.shoot = ModContent.ProjectileType<PossessedTridentProjectile>();
            Item.shootSpeed = 44f;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity = velocity.RotatedByRandom(0.1f);
        }
    }

    public class PossessedTridentProjectile : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public List<Vector2> cache;

        public override string Texture => AssetDirectory.SirNautilusDrops + "NautilusTridentProjectile";
        public Asset<Texture2D> Glowmask;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Trident");
            ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.ownerHitCheck = true;

            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;

            Projectile.hide = true;
            //hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float bladeLength = 85 * Projectile.scale;
            float bladeWidth = 30 * Projectile.scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * bladeLength, bladeWidth, ref collisionPoint);
        }

        public bool didFullThrustEffects = false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            Owner.UpdateBasicHoldoutVariables(Projectile, -1);

            Owner.direction = Projectile.velocity.X.NonZeroSign();
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);

            Vector2 ownerOrigin = Owner.RotatedRelativePoint(Owner.MountedCenter);

            Projectile.Center = ownerOrigin;
            float attackProgress = 1 - (Owner.itemTime / (float)Owner.itemTimeMax);
            float thrustRotation = Projectile.velocity.ToRotation();
            thrustRotation += 0.1f - 0.3f * MathF.Pow(attackProgress, 3f) * Owner.direction;

            float thrustDisplace = MathF.Pow(MathF.Sin(attackProgress * MathHelper.Pi), 0.4f);

            Projectile.position += (Vector2.UnitX * (40 + thrustDisplace * 80f)).RotatedBy(thrustRotation);
            Projectile.rotation = thrustRotation + MathHelper.PiOver4 * Owner.direction;
            if (Owner.direction == -1)
                Projectile.rotation += MathHelper.PiOver2;

            Owner.SetCompositeArmFront(true, thrustDisplace.ToStretchAmount(), thrustRotation - MathHelper.PiOver2);

            if (Owner.whoAmI == Main.myPlayer && Owner.itemAnimation <= 2)
            {
                Projectile.Kill();
                Owner.reuseDelay = 2;
            }

            //Spawn the followup
            if (attackProgress >= 0.4f /*&& Owner.channel*/ && Projectile.ai[0] == 0 && Main.myPlayer == Projectile.owner)
            {
                float projectileDistance = Projectile.Center.Distance(Owner.Center);
                float cursorDistance = MathHelper.Clamp(Owner.Distance(Main.MouseWorld), projectileDistance * 0.5f, projectileDistance * 1.2f);

                float ghostDistance = -projectileDistance + cursorDistance;

                Vector2 position = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * ghostDistance;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, Projectile.velocity, ModContent.ProjectileType<NautilusGhostApparition>(), Projectile.damage, Projectile.knockBack / 2, Main.myPlayer, Projectile.whoAmI);
                Projectile.ai[0] = 1;
            }

            if (Main.dedServ)
                return;

            if (!Main.rand.NextBool(3) && attackProgress < 0.5f)
            {
                Vector2 direction = (Projectile.velocity + Main.rand.NextVector2CircularEdge(1f, 1f)).SafeNormalize(Vector2.Zero);
                Vector2 position = Vector2.Lerp(ownerOrigin, Projectile.Center, Main.rand.NextFloat(0.3f, 1f)) + Main.rand.NextVector2Circular(12f, 12f);
                float speed = Main.rand.NextFloat(1.2f, 2f);

                Dust d = Dust.NewDustPerfect(position, ModContent.DustType<SpectralWaterDustEmbers>(), direction * speed, 15, Color.White, Main.rand.NextFloat(0.7f, 1.4f));
                d.noGravity = true;
            }

            if (attackProgress >= 0.5f && !didFullThrustEffects)
            {
                didFullThrustEffects = true;
                SoundEngine.PlaySound(PossessedTrident.GhostSwingSound with { Volume = 0.3f, PitchVariance = 0.3f }, Projectile.Center);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                    Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(20f, 30f);
                    float speed = Main.rand.NextFloat(1.2f, 3f);

                    Dust d = Dust.NewDustPerfect(position, ModContent.DustType<SpectralWaterDustNoisy>(), direction * speed, 15, Color.White, Main.rand.NextFloat(1f, 1.3f));
                    d.noGravity = true;
                    d.rotation = Main.rand.NextFloat(0f, 3f);
                    d.customData = Color.Teal.ToVector3();
                    d.velocity += Projectile.velocity * 0.04f;
                }

            }

            if (cache == null)
                cache = new List<Vector2>();
            cache.Add(Projectile.Center);
            while (cache.Count > 10)
                cache.RemoveAt(0);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (target.Center.X - Owner.Center.X).NonZeroSign();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            //Projectile.damage = (int)(Projectile.damage * PossessedTrident.MULTIHIT_FALLOFF);
        }


        public override void CutTiles()
        {
            // tilecut_0 is an unnamed decompiled variable which tells CutTiles how the tiles are being cut (in this case, via a Projectile).
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.TileActionAttempt cut = new Utils.TileActionAttempt(DelegateMethods.CutTiles);

            float bladeWidth = 30 * Projectile.scale;
            float bladeLength = 85 * Projectile.scale;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * bladeLength, bladeWidth, cut);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Glowmask ??= ModContent.Request<Texture2D>(Texture + "Glow");
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glowmask = Glowmask.Value;
            Vector2 origin = new Vector2(texture.Width - 6, 6);
            float drawRotation = Projectile.rotation;

            float attackProgress = Owner.itemTime / (float)Owner.itemTimeMax;

            //drawRotation += Main.rand.NextFloatDirection() * MathF.Pow(attackProgress, 4f) * 0.12f;

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, drawRotation, origin, Projectile.scale, 0, 0);

            int i = 0;

            foreach (Vector2 oldPos in cache)
            {
                float afterimageProgress = i / (float)cache.Count;
                float opacity = MathF.Pow(afterimageProgress, 3f);
                opacity *= MathF.Pow(attackProgress, 1.2f);
                Color afterimageColor = Color.Lerp(Color.Turquoise, Color.DodgerBlue, afterimageProgress);

                for (int r = 0; r <2; r++)
                    Main.EntitySpriteDraw(glowmask, oldPos - Main.screenPosition, null, afterimageColor with { A = 0 } * opacity * 0.6f, drawRotation, origin, Projectile.scale, 0, 0);
                i++;
            }

            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            origin = lensFlare.Size() / 2f;
            Vector2 scale = new Vector2(0.4f, 1.6f) * Projectile.scale * 0.8f;
            scale *= Main.rand.NextFloat(0.9f, 1.1f);
            scale.Y *= Main.rand.NextFloat(0.9f, 1.3f);

            float offsetRotation = Projectile.rotation - MathHelper.PiOver4 * Owner.direction;
            if (Owner.direction == -1)
                offsetRotation -= MathHelper.PiOver2;

            Vector2 lensFlarePosition = Projectile.Center - offsetRotation.ToRotationVector2() * 30f;
            Color lensFlareColor = Main.rand.NextBool() ? Color.SpringGreen : Color.DodgerBlue;
            float lensFlareOpacity = MathF.Pow(MathF.Sin(Utils.GetLerpValue(0.1f, 0.6f, 1 - attackProgress, true) * MathHelper.Pi), 0.7f);
            scale *= lensFlareOpacity;
            float lensFlareRotation = MathF.Pow(Utils.GetLerpValue(0.1f, 0.6f, 1 - attackProgress, true), 2f) * MathHelper.PiOver2;

            for (i = 0; i < 4; i++)
            {
                int indx = i % 2;
                Main.EntitySpriteDraw(lensFlare, lensFlarePosition - Main.screenPosition, null, lensFlareColor with { A = 0 } * 0.8f * lensFlareOpacity, MathHelper.PiOver2 * indx + lensFlareRotation, origin, scale, 0, 0);
                Main.EntitySpriteDraw(lensFlare, lensFlarePosition - Main.screenPosition, null, Color.White * lensFlareOpacity, MathHelper.PiOver2 * indx + lensFlareRotation, origin, scale * 0.6f, 0, 0);
            }

            return false;
        }
    }

    public class NautilusGhostApparition : ModProjectile, IDrawPixelated
    {
        public Player Owner => Main.player[Projectile.owner];
        public List<Vector2> cacheEdge;
        public List<Vector2> cacheHilt;
        public PrimitiveSliceTrail trail;

        public override string Texture => AssetDirectory.SirNautilusDrops + Name;
        public Asset<Texture2D> Glowmask;
        public Asset<Texture2D> Outline;

        public int LinkedSpear => (int)Projectile.ai[0];

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nautilus");
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.ownerHitCheck = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 80;

            Projectile.timeLeft = 70;
            Projectile.extraUpdates = 9;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float bladeWidth = 30 * Projectile.scale;

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), bladeTip, bladeHitboxOrigin, bladeWidth, ref collisionPoint);
        }

        public float Completion => 1 - Projectile.timeLeft / 70f;
        public float SwipeRotation => (MathF.Pow(Completion, 0.5f) * MathHelper.Pi - MathHelper.PiOver2) * Projectile.direction;

        public Vector2 bladeTip;
        public Vector2 bladeHilt;
        public Vector2 bladeHitboxOrigin;

        public float TridentLenght => 85f * Projectile.scale;

        public bool didApparitionEffects = false;

        public override void AI()
        {
            Projectile.direction = Projectile.velocity.X.NonZeroSign();
            float completion = Completion;
            Projectile.rotation = Projectile.velocity.ToRotation() + SwipeRotation;

            //Projectile.Center += Projectile.velocity * MathF.Pow(1 - completion, 2f) * 0.1f;

            Vector2 pointing = Projectile.velocity.RotatedBy(SwipeRotation).SafeNormalize(Vector2.One);
            

            Vector2 offset = Vector2.UnitX.RotatedBy(-MathHelper.PiOver4 * Projectile.direction + MathHelper.PiOver2 * 1.4f * Projectile.direction * MathF.Pow(completion, 0.3f)) * 40f;
            offset.X *= 2.4f;
            offset.Y *= 1.6f - 0.6f * MathF.Pow(completion, 2f);
            bladeHilt = Projectile.Center + offset.RotatedBy(Projectile.velocity.ToRotation());
            bladeTip = bladeHilt + pointing * TridentLenght;
            bladeHitboxOrigin = bladeHilt - pointing * 30 * Projectile.scale; //This is just to extend the hitbox a bit behind the actual trident to avoid missing

            if (Projectile.timeLeft <= 60)
            {
                Projectile.extraUpdates = 1;
                if (!didApparitionEffects)
                {
                    Vector2 apparitionPosition = Vector2.Lerp(bladeHilt, bladeTip, -0.3f);
                    apparitionPosition = Projectile.Center + (apparitionPosition - Projectile.Center).RotatedBy(0.3f * Projectile.direction);

                    Vector2 normal = pointing.RotatedBy(MathHelper.PiOver2 * 1.2f * Projectile.direction);

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 velocity = -normal * Main.rand.NextFloat(0.2f, 2f);
                        Vector2 positionOffset = Main.rand.NextVector2Circular(22f, 22f);
                        velocity += positionOffset * 0.02f;

                        Dust d = Dust.NewDustPerfect(apparitionPosition + positionOffset, ModContent.DustType<SpectralWaterDustEmbers>(), velocity, 100, Color.White, Main.rand.NextFloat(1.2f, 2.5f));
                        d.noGravity = true;
                    }

                    if (Main.rand.NextBool(4))
                        SoundEngine.PlaySound(PossessedTrident.GhostVoiceSound, Projectile.Center);

                    didApparitionEffects = true;
                }
            }

            if (!Main.dedServ)
            {
                ManagePrimitiveStuff();

                if (Main.rand.NextBool(5 + (int)(Utils.GetLerpValue(0.6f, 1f, completion, true) * 4f)))
                {
                    Vector2 positionAlongBlade = Vector2.Lerp(bladeHilt, bladeTip, 0.5f + MathF.Pow(Main.rand.NextFloat(), 0.5f) * 0.5f);
                    Vector2 velocity = pointing.RotatedBy(Projectile.direction * MathHelper.PiOver2);

                    Dust d = Dust.NewDustPerfect(positionAlongBlade, ModContent.DustType<SpectralWaterDustEmbers>(), velocity, 100, Color.White, Main.rand.NextFloat(1f, 1.5f));
                    d.noGravity = true;
                }

                if (Projectile.timeLeft == 1)
                {
                    Vector2 apparitionPosition = Vector2.Lerp(bladeHilt, bladeTip, 0.1f);
                    for (int i = 0; i < 23; i++)
                    {
                        Vector2 velocity = pointing.RotatedBy(Projectile.direction * MathHelper.PiOver2) * Main.rand.NextFloat(0.2f, 2f);
                        Vector2 positionOffset = Main.rand.NextVector2Circular(22f, 30f);
                        velocity += positionOffset * 0.02f;

                        int dustType = Main.rand.NextBool(4) ? ModContent.DustType<SpectralWaterDustEmbers>() : ModContent.DustType<SpectralWaterDustNoisy>();

                        Dust d = Dust.NewDustPerfect(apparitionPosition + positionOffset, dustType, velocity, 100, Color.White, Main.rand.NextFloat(1.2f, 2.5f));
                        d.noGravity = true;
                    }
                }
            }
        }

        public override bool? CanDamage()
        {
            if (Projectile.timeLeft < 16)
                return false;
            return base.CanDamage();
        }

        public override bool? CanHitNPC(NPC target)
        {
            /*
            if (LinkedSpear >= 0 && LinkedSpear <= Main.maxProjectiles)
            {
                Projectile spear = Main.projectile[LinkedSpear];
                if (spear.active && spear.owner == Projectile.owner && spear.type == ModContent.ProjectileType<PossessedTridentProjectile>())
                {
                    //Shares local iframes with spear
                    if (spear.localNPCImmunity[target.whoAmI] != 0)
                        return false;
                }
            }
            */
            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            /*
            if (LinkedSpear >= 0 && LinkedSpear <= Main.maxProjectiles)
            {
                Projectile spear = Main.projectile[LinkedSpear];
                if (spear.active && spear.owner == Projectile.owner && spear.type == ModContent.ProjectileType<PossessedTridentProjectile>())
                {
                    //Shares local iframes with spear
                    spear.localNPCImmunity[target.whoAmI] = spear.localNPCHitCooldown;
                }
            }
            */
            Projectile.damage = (int)(Projectile.damage * PossessedTrident.MULTIHIT_FALLOFF);
        }

        public override void CutTiles()
        {
            // tilecut_0 is an unnamed decompiled variable which tells CutTiles how the tiles are being cut (in this case, via a Projectile).
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.TileActionAttempt cut = new Utils.TileActionAttempt(DelegateMethods.CutTiles);

            float bladeWidth = 30 * Projectile.scale;
            Utils.PlotTileLine(bladeHitboxOrigin, bladeTip, bladeWidth, cut);
        }

        public void ManagePrimitiveStuff()
        {
            if (cacheEdge == null)
            {
                cacheEdge = new List<Vector2>();
            }
            cacheEdge.Add(bladeTip);
            while (cacheEdge.Count > 26)
                cacheEdge.RemoveAt(0);

            if (cacheHilt == null)
            {
                cacheHilt = new List<Vector2>();
            }
            cacheHilt.Add(bladeHilt);
            while (cacheHilt.Count > 26)
                cacheHilt.RemoveAt(0);

            trail ??= new PrimitiveSliceTrail(10, SliceColorFunction);
            trail.SetPositions(cacheEdge, cacheHilt);
        }

        public Color SliceColorFunction(float progress)
        {
            return Color.Lerp(Color.Teal, Color.DodgerBlue, progress) with { A = 0 } * 1.6f * drawOpacity;
        }

        public float drawOpacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Glowmask ??= ModContent.Request<Texture2D>(Texture + "Glow");
            Outline ??= ModContent.Request<Texture2D>(Texture + "Outline");

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glowmask = Glowmask.Value;
            Texture2D outline = Outline.Value;
            SpriteEffects effects = Projectile.velocity.X.NonZeroSign() == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            float completion = Completion;
            float actualCompletion = Utils.GetLerpValue(60, 0, Projectile.timeLeft, true);
            drawOpacity = 1 - MathF.Pow(Utils.GetLerpValue(0.7f, 1f, completion, true), 1.6f);

            DrawTrident();

            Rectangle frame = texture.Frame(1, 5, 0, (int)(actualCompletion * 5), 0, -2);
            Vector2 origin = frame.Size() / 2f;

            Vector2 drawPosition = Vector2.Lerp(bladeHilt, bladeTip, 0.2f);

            float directionRotation = Projectile.velocity.ToRotation();
            float drawRotation = directionRotation;// + directionRotation.AngleBetween(Projectile.rotation) * -0.3f * Projectile.direction;
            drawRotation += MathF.Pow(completion, 5f) * 0.7f * Projectile.direction;

            if (effects == SpriteEffects.FlipHorizontally)
            {
                drawRotation += MathHelper.Pi;
            }


            Main.EntitySpriteDraw(texture, drawPosition - Main.screenPosition, frame, Color.White * drawOpacity, drawRotation, origin, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(glowmask, drawPosition - Main.screenPosition, frame, Color.White with { A = 0 } * drawOpacity, drawRotation, origin, Projectile.scale, effects, 0);

            Color outlineColor = new Color(170, 243, 251, 0) * MathF.Pow(1 - actualCompletion, 2f);
            Main.EntitySpriteDraw(outline, drawPosition - Main.screenPosition, frame, outlineColor, drawRotation, origin, Projectile.scale, effects, 0);

            return false;
        }

        public void DrawTrident()
        {
            Texture2D tridentTex = ModContent.Request<Texture2D>(Texture + "Trident").Value;

            float completion = Completion;
            SpriteEffects effects = Projectile.velocity.X.NonZeroSign() == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rotation = Projectile.rotation + MathHelper.PiOver4 * Projectile.direction;
            Vector2 origin = new Vector2(4, tridentTex.Height - 4);


            if (effects == SpriteEffects.FlipHorizontally)
            {
                origin.X = tridentTex.Width - origin.X;
                rotation += MathHelper.Pi;
            }

            Vector2 extraGlowPosition = bladeHilt - Projectile.rotation.ToRotationVector2() * 3f * Projectile.scale;

            Main.EntitySpriteDraw(tridentTex, bladeHilt - Main.screenPosition, null, Color.White * drawOpacity, rotation, origin, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(tridentTex, extraGlowPosition - Main.screenPosition, null, Color.DodgerBlue with { A = 0 } * 0.4f * drawOpacity, rotation, origin, Projectile.scale * 1.1f, effects, 0);


            Texture2D lensFlare = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            origin = lensFlare.Size() / 2f;
            Vector2 scale = new Vector2(0.5f, 1f) * Projectile.scale;
            float fade = MathF.Pow(MathF.Sin(completion * MathHelper.Pi), 0.6f);

            scale *= fade;
            float opacity = drawOpacity * fade;

            Main.EntitySpriteDraw(lensFlare, bladeTip - Main.screenPosition, null, Color.DodgerBlue with { A = 0 } * opacity, MathHelper.PiOver2, origin, scale * 1.4f, effects, 0);
            Main.EntitySpriteDraw(lensFlare, bladeTip - Main.screenPosition, null, Color.White with { A = 0 } * opacity, MathHelper.PiOver2, origin, scale, effects, 0);
        }


        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = Scene["SlicePrimitive"].GetShader().Shader;
            effect.Parameters["edgeSize"].SetValue(0.07f);
            effect.Parameters["edgeSizePower"].SetValue(2f);
            effect.Parameters["edgeTransitionSize"].SetValue(0.03f);
            effect.Parameters["edgeTransitionOpacity"].SetValue(0.2f);
            effect.Parameters["edgeColorMultiplier"].SetValue(new Vector4(1.8f, 1.8f, 3f, 2f));
            effect.Parameters["edgeColorAdd"].SetValue(new Vector4(1.8f, 1.8f, 1f, 2f));
            effect.Parameters["horizontalPower"].SetValue(1f + Completion * 3f);
            effect.Parameters["verticalPower"].SetValue(1.2f);
            trail?.Render(effect, -Main.screenPosition);
        }
    }
}
