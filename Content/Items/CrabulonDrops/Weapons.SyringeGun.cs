using Terraria.DataStructures;
using CalamityFables.Particles;
using static CalamityFables.Content.Boss.MushroomCrabBoss.SporedCorpse;
using CalamityFables.Content.Boss.MushroomCrabBoss;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    [ReplacingCalamity("HyphaeRod")]
    public class SyringeGun : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public static readonly SoundStyle MissSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SyringeGunMiss") { PitchVariance = 0.3f, MaxInstances = 3 };
        public static readonly SoundStyle HitSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SyringeGunHit", 3) { PitchVariance = 0.3f, MaxInstances = 3 };
        public static readonly SoundStyle FireSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SyringeGunFire") { PitchVariance = 0.2f };

        public static float EXPLOSION_RADIUS = 120;
        public static Vector2 EXPLOSION_DAMAGE_MULT_RANGE = new(0f, 7f);
        public static int DOT_PER_STACK = 5;

        public override void Load()
        {
            FablesNPC.UpdateLifeRegenEvent += ApplyInfestationDOT;
            FablesNPC.HitEffectEvent += NPCDeathCheck;
            FablesNPC.OnKillEvent += DropMushrooms;
        }

        public class SyringeDamageTracker : CustomGlobalData
        {
            public int LastSyringeDamage = 0;
        }

        private void ApplyInfestationDOT(NPC npc, ref int damage)
        {
            if (npc.GetNPCFlag("InfestationDOT"))
            {
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;

                int infestationRotCount = 0;
                //int num3 = 1; <= i don't know what this is for. IT divides the damage at the end, but it's 1 and never changes
                //It's always 1 for the bone javelin, the tentacle spike and the blood butcherer dot

                int rotType = ModContent.ProjectileType<InfestationRot>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == rotType && Main.projectile[i].ai[0] == 1f && Main.projectile[i].ai[1] == npc.whoAmI)
                        infestationRotCount++;
                }

                int infestationRotDamage = infestationRotCount * DOT_PER_STACK;
                npc.lifeRegen -= infestationRotDamage * 2;
                if (damage < infestationRotDamage)
                    damage = infestationRotDamage;
            }
        }

        private void NPCDeathCheck(NPC npc, NPC.HitInfo hit)
        {
            if (npc.life <= 0 && npc.GetNPCFlag("InfestationDOT"))
            {
                GetENNEDVFX(npc);
            }
        }

        public static void GetENNEDVFX(NPC npc)
        {
            float size = (npc.width * npc.height) / 90f;
            if (size > 100)
                size = 100;

            for (int i = 0; i < size; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f);
                Vector2 velocity = offset.SafeNormalize(Vector2.One) * Main.rand.NextFloat(1f, 3f);
                velocity.X *= Main.rand.NextFloat(1f, 2f);
                velocity += npc.velocity;
                velocity.Y -= 4f;

                ParticleHandler.SpawnParticle(new EnMushroomParticle(npc.Center + offset, velocity, Main.rand.Next(40, 120)));
            }


            for (int i = 0; i < 33; i++)
            {
                float smokeSize = Main.rand.NextFloat(2.5f, 3.6f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1f, 1f);

                Vector2 smokeCenter = npc.Center + gushDirection * 102f * Main.rand.NextFloat(0.2f, 0.55f);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f);

                Particle smoke = new SporeGas(smokeCenter, velocity, npc.Center, Math.Min(npc.width, npc.height) * 0.6f, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }
        }

        private void DropMushrooms(NPC npc)
        {
            if (npc.CountsAsACritter || npc.lifeMax < 100 || npc.SpawnedFromStatue)
                return;

            if (npc.GetNPCFlag("InfestationDOT"))
            {
                if (Main.rand.NextBool(3))
                    Item.NewItem(npc.GetSource_Loot(), npc.TopLeft, npc.Size, ItemID.GlowingMushroom);

                Player closestPlayer = Main.player[npc.FindClosestPlayer(out float distance)];
                if (closestPlayer.whoAmI == Main.myPlayer && closestPlayer.HasItem(Type))
                {
                    if (Main.rand.NextBool())
                        Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, Main.rand.NextVector2Circular(10f, 0f) - Vector2.UnitY * 3f, ModContent.ProjectileType<ManaMushroom>(), 0, 0, closestPlayer.whoAmI);

                    ExplosionHitboxFromInfectedNPC(npc, closestPlayer);
                }

            }
        }

        public static void ExplosionHitboxFromInfectedNPC(NPC npc, Player player)
        {
            if (!npc.GetNPCData<SyringeDamageTracker>(out var data) || data.LastSyringeDamage <= 0)
                return;

            int rotCount = 0;
            int rotType = ModContent.ProjectileType<InfestationRot>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == rotType && Main.projectile[i].ai[0] == 1f && Main.projectile[i].ai[1] == npc.whoAmI)
                    rotCount++;
            }

            int damage = (int)(data.LastSyringeDamage * Utils.Remap(rotCount, 0, 7, EXPLOSION_DAMAGE_MULT_RANGE.X, EXPLOSION_DAMAGE_MULT_RANGE.Y));
            Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, Vector2.Zero, ModContent.ProjectileType<InfestationExplosionHitbox>(), damage, 1, player.whoAmI);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Syringe Gun");
            Tooltip.SetDefault("Landing all 3 shots infects the target\n" +
                               "Infected enemies erupt into mana-rich mushrooms on death, spreading the infection to nearby monsters");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 26;
            Item.DamageType = DamageClass.Magic;
            Item.width = 23;
            Item.height = 8;

            Item.useAnimation = 12;
            Item.useTime = 4;
            Item.reuseDelay = 32;
            Item.useLimitPerAnimation = 3;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = Item.buyPrice(0, 7, 50, 0);
            Item.rare = ItemRarityID.Green;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SyringeGunDart>();
            Item.shootSpeed = 14f;
            Item.mana = 12;
            Item.UseSound = FireSound;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        public int shootGroupIdentity;
        public int empowermentLevel = 0; //Necessary in the case that we shoot so close to a target that the other projectile doesnt have time to spawn

        public override void HoldItem(Player player) => player.SyncMousePosition();

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;
            Vector2 itemSize = new Vector2(58, 38);
            Vector2 itemOrigin = new Vector2(-19, 10);

            if (player.reuseDelay > 0)
            {
                itemPosition += itemRotation.ToRotationVector2() * -14f * MathF.Pow(player.itemTime / (float)player.itemTimeMax, 1.5f);
            }
            else
            {
                itemPosition += itemRotation.ToRotationVector2() * 2f * MathF.Pow(1 - player.itemTime / (float)Item.reuseDelay, 1.5f);
            }

            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;

            float rotation = (player.Center - player.MouseWorld()).ToRotation() * player.gravDir + MathHelper.PiOver2;

            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;

            if (player.reuseDelay != 0)
            {
                rotation += Main.rand.NextFloat(-0.03f, 0.03f) + animProgress * player.direction * -0.3f;

                stretch = (player.itemTime / (float)player.itemTimeMax).ToStretchAmount();

                //if (animProgress < 0.4f)
                //    rotation += 0.1f * animProgress * player.direction;
            }
            else
            {
                rotation += MathF.Pow(player.itemTime / (float)Item.reuseDelay, 1.6f) * player.direction * -0.2f;
            }


            player.SetCompositeArmFront(true, stretch, rotation);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            Vector2 normal = velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 * velocity.X.NonZeroSign());
            position -= normal * Main.rand.NextFloat(4f, 19f);
            velocity = velocity.RotatedByRandom(0.05f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool firstProjectile = player.itemAnimation == player.itemAnimationMax;
            int firingSquad = firstProjectile ? -1 : shootGroupIdentity;
            if (firstProjectile)
                empowermentLevel = 0;

            Projectile dart = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer, firingSquad, empowermentLevel);

            //Save the group of darts
            if (firstProjectile)
                shootGroupIdentity = dart.identity;

            return false;
        }
    }

    public class SyringeGunDart : ModProjectile
    {
        internal PrimitiveTrail TrailDrawer;
        internal PrimitiveTrail TrailBloomDrawer;

        private List<Vector2> trailCache;
        public static Asset<Texture2D> GlowAsset;
        public static Asset<Texture2D> GlowCoreAsset;


        /// <summary>
        /// The group of syringe this dart was fired from
        /// Is set to the identity of the first projectile fired
        /// </summary>
        public ref float BundleID => ref Projectile.ai[0];


        /// <summary>
        /// Increased by one for every syringe that hit its target
        /// </summary>
        public ref float Charge => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Syringe");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 4000;
        }

        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.timeLeft = 400;
            Projectile.extraUpdates = 1;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //Shrink the hitbox a little to have the roots inset
            if (targetHitbox.Width > 8 && targetHitbox.Height > 8)
                targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);

            return projHitbox.Intersects(targetHitbox);
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (Color.Blue * 0.8f).ToVector3() * 0.5f);

            //If bundleID is -1 it means its the first of the burst
            if (BundleID == -1)
                BundleID = Projectile.identity;

            if (!Main.dedServ)
            {
                ManageCache();
                ManageTrail();
            }

            if (Projectile.timeLeft < 360)
            {
                Projectile.velocity.Y += 0.06f;
                if (Projectile.velocity.Y > 10)
                    Projectile.velocity.Y = 10;
            }

            if (Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.GemSapphire, -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.2f));
                d.noGravity = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public void EmpowerSquad()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.ItemAnimationActive && owner.HeldItem != null && owner.HeldItem.ModItem is SyringeGun gun && gun.shootGroupIdentity == BundleID)
                gun.empowermentLevel++;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.whoAmI != Projectile.whoAmI && p.type == Type && p.owner == Projectile.owner && p.ai[0] == Projectile.ai[0])
                {
                    p.ai[1]++;
                    p.netUpdate = true;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Center += oldVelocity;
            Projectile.velocity = oldVelocity;
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SyringeGun.MissSound with { Volume = SyringeGun.MissSound.Volume * Main.rand.NextFloat(0.2f, 0.6f)}, Projectile.Center);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.netUpdate = true;

            if (!target.GetNPCData<SyringeGun.SyringeDamageTracker>(out var data))
            {
                data = new SyringeGun.SyringeDamageTracker();
                target.SetNPCData(data);
            }

            // Store projectile damage so explosion damage can scale off it
            data.LastSyringeDamage = Projectile.damage;

            if (Charge < 2)
                EmpowerSquad();
            else if (Main.myPlayer == Projectile.owner)
            {
                //NPC got killed by the projectile, but this hook runs after the npc hit hook
                //So if they get killed by the last syringe they wont have the DOT, so we have to do the explosion ourselves
                //Use the packet's runlocally to do the vfx and everything there
                if (!target.active && !target.GetNPCFlag("InfestationDOT"))
                {
                    bool dropExtras = (!target.CountsAsACritter && target.lifeMax >= 100 && !target.SpawnedFromStatue);
                    Player owner = Main.player[Projectile.owner];

                    if (dropExtras)
                    {
                        //One in three to drop glowing mushrooù
                        if (Main.rand.NextBool(3))
                        {
                            int item = Item.NewItem(target.GetSource_Loot(), target.TopLeft, target.Size, ItemID.GlowingMushroom, 1, true);
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item, 1f);
                        }

                        if (Main.rand.NextBool())
                            Projectile.NewProjectile(target.GetSource_Death(), target.Center, Main.rand.NextVector2Circular(10f, 0f) - Vector2.UnitY * 3f, ModContent.ProjectileType<ManaMushroom>(), 0, 0, Projectile.owner);
                    }

                    SyringeGun.ExplosionHitboxFromInfectedNPC(target, owner);

                    new SyringeGunExplosionEffects(target.whoAmI, Projectile.Center).Send(-1, -1);
                    return;
                }


                Vector2 offsetFromNPC = (target.Center - Projectile.Center) * 0.75f;

                int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, offsetFromNPC, ModContent.ProjectileType<InfestationRot>(), 0, 0, Main.myPlayer, 1f, target.whoAmI);
                Projectile.KillOldestJavelin(index, ModContent.ProjectileType<InfestationRot>(), target.whoAmI, InfestationRot.scanBuffer);
                target.AddBuff(ModContent.BuffType<InfestationDOT>(), 800);
            }
        }

        #region Prim stuff
        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);

            TrailDrawer.SetPositionsSmart(trailCache, Projectile.position, FablesUtils.RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;

            TrailBloomDrawer = TrailBloomDrawer ?? new PrimitiveTrail(30, BloomWidthFunction, BloomColorFunction);
            TrailBloomDrawer.SetPositionsSmart(trailCache, Projectile.position, FablesUtils.RigidPointRetreivalFunction);
        }

        private void ManageCache()
        {
            if (trailCache == null)
            {
                trailCache = new List<Vector2>();
                for (int i = 0; i < 20; i++)
                    trailCache.Add(Projectile.Center);
            }

            trailCache.Add(Projectile.Center);
            while (trailCache.Count > 20)
                trailCache.RemoveAt(0);
        }


        internal Color ColorFunction(float completionRatio)
        {
            return new Color(70, 20 + (int)(100 * completionRatio), 255) * completionRatio * 0.6f;
        }

        internal float WidthFunction(float completionRatio)
        {
            return 4 + completionRatio * 1f;
        }

        internal Color BloomColorFunction(float completionRatio)
        {
            return new Color(20, 20 + (int)(60 * completionRatio), 255) * completionRatio * 0.1f;
        }

        internal float BloomWidthFunction(float completionRatio)
        {
            return 20f;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            if (Projectile.numHits > 0)
            {
                float volume = 1f;
                if (Charge < 2)
                    volume = Main.rand.NextFloat(0.8f, 1f);

                SoundEngine.PlaySound(SyringeGun.HitSound with { Volume = volume }, Projectile.Center);
            }

            ParticleHandler.SpawnParticle(new EmptySyringeParticle(this));

            if (trailCache != null)
            {
                GhostTrail clone = new GhostTrail(trailCache, TrailDrawer, 0.15f, null, "Primitive_StreakyTrail", delegate (Effect effect, float fading)
                {
                    effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
                    effect.Parameters["verticalStretch"].SetValue(0.5f);
                    effect.Parameters["repeats"].SetValue(3f);

                    effect.Parameters["overlayScroll"].SetValue(Main.GlobalTimeWrappedHourly * -1.5f);
                    effect.Parameters["overlayOpacity"].SetValue(0.5f);

                    effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                    effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
                    effect.Parameters["streakScale"].SetValue(0.5f);
                });

                clone.ShrinkTrailLenght = true;
                clone.DrawLayer = DrawhookLayer.BehindTiles;
                GhostTrailsHandler.LogNewTrail(clone);


                clone = new GhostTrail(trailCache, TrailBloomDrawer, 0.15f, null, "Primitive_StreakyTrail", delegate (Effect effect, float fading)
                {
                    effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
                    effect.Parameters["verticalStretch"].SetValue(0.5f);
                    effect.Parameters["repeats"].SetValue(3f);

                    effect.Parameters["overlayScroll"].SetValue(Main.GlobalTimeWrappedHourly * -1.5f);
                    effect.Parameters["overlayOpacity"].SetValue(0.5f);

                    effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                    effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
                    effect.Parameters["streakScale"].SetValue(0.5f);
                });

                clone.ShrinkTrailLenght = true;
                clone.DrawLayer = DrawhookLayer.BehindTiles;
                GhostTrailsHandler.LogNewTrail(clone);
            }
        }
        #endregion

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(3f);

            effect.Parameters["overlayScroll"].SetValue(Main.GlobalTimeWrappedHourly * -1.5f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
            effect.Parameters["streakScale"].SetValue(0.5f);
            TrailDrawer?.Render(effect, -Main.screenPosition);

            effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
            TrailBloomDrawer?.Render(effect, -Main.screenPosition);


            GlowAsset = GlowAsset ?? ModContent.Request<Texture2D>(Texture + "Glow");
            GlowCoreAsset = GlowCoreAsset ?? ModContent.Request<Texture2D>(Texture + "GlowCore");
            Texture2D tex = TextureAssets.Projectile[Type].Value;

            Vector2 lastPosition = Projectile.Center;
            for (int i = 1; i < 15; i += 2)
            {
                Vector2 afterImage = trailCache[^i];
                if (afterImage == lastPosition)
                    break;

                float opacity = MathF.Pow(1 - i / 15f, 2f) * 0.5f;
                float alphaStrenght = 20 + (1 - i / 15f) * 20;

                Color afterImageColor = Color.RoyalBlue with { A = 0 };

                Main.EntitySpriteDraw(tex, afterImage - Main.screenPosition, null, (afterImageColor with { A = (byte)alphaStrenght }) * opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0);

                lastPosition = afterImage;
            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0);

            Vector2 glowStretch = new Vector2(1f, 1.5f + MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f);

            tex = GlowAsset.Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.Blue with { A = 0 } * 1.3f, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.Blue with { A = 0 } * 1.3f, Projectile.rotation, tex.Size() / 2f, glowStretch * Projectile.scale, 0);


            tex = GlowCoreAsset.Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, new Color(160, 150, 255) with { A = 0 } * 0.5f, Projectile.rotation, tex.Size() / 2f, glowStretch * Projectile.scale, 0);

            return false;
        }
    }


    [Serializable]
    public class SyringeGunExplosionEffects : Module
    {
        byte whoAmI;
        int npc;
        Vector2 position;

        public SyringeGunExplosionEffects(int npc, Vector2 position)
        {
            whoAmI = (byte)Main.myPlayer;
            this.npc = npc;
            this.position = position;
        }

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
            else
            {
                SyringeGun.GetENNEDVFX(Main.npc[npc]); 
                ParticleHandler.SpawnParticle(new CircularPulseShine(position, Color.RoyalBlue with { A = 0 }, Main.rand.NextFloat(0.7f, 1f)));
            }

        }
    }


    public class InfestationRot : ModProjectile
    {
        /// <summary>
        /// The group of syringe this dart was fired from
        /// Is set to the identity of the first projectile fired
        /// </summary>
        public ref float BundleID => ref Projectile.ai[0];


        /// <summary>
        /// Increased by one for every syringe that hit its target
        /// </summary>
        public ref float Charge => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rot");
        }

        public override string Texture => AssetDirectory.Crabulon + "MushroomInfestationRoots";

        public NPC AnchorNPC => Main.npc[(int)Projectile.ai[1]];

        public List<SporedCorpseRoot> roots = new List<SporedCorpseRoot>();
        public int rootExpansion = 0;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.timeLeft = 800;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public static Point[] scanBuffer = new Point[7];

        public bool didEffects = false;
        public override void AI()
        {
            if (!didEffects)
            {
                ParticleHandler.SpawnParticle(new CircularPulseShine(Projectile.Center, Color.RoyalBlue with { A = 0 }, Main.rand.NextFloat(0.7f, 1f)));
                //SoundEngine.PlaySound(DesertScourgeDrops.TornElectrosac.ChargeSound, Projectile.Center);
                didEffects = true;
            }


            //Mostly taken from aistyle 113
            bool hitEffectTick = false;
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] % 30f == 0f)
                hitEffectTick = true;

            if (Projectile.ai[1] < 0 || Projectile.ai[1] >= 200 || !AnchorNPC.active || !AnchorNPC.CanBeChasedBy(this))
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi ;
            Projectile.Center = AnchorNPC.Center - Projectile.velocity;
            Projectile.gfxOffY = AnchorNPC.gfxOffY;
            if (hitEffectTick)
                AnchorNPC.HitEffect(0, 1.0);


            if (!Main.dedServ)
            {
                if (roots.Count == 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        roots.Add(new SporedCorpseRoot(5f));
                    }
                }
                rootExpansion++;

                if (ChildSafety.Disabled)
                {
                    foreach (SporedCorpseRoot root in roots)
                    {
                        if (rootExpansion - root.expansionDelay <= 14 && rootExpansion - root.expansionDelay >= 0)
                        {
                            float shrunkenRotation = ((root.rotation / MathHelper.TwoPi) - 0.5f) * MathHelper.PiOver4;
                            Vector2 rootPosition = Projectile.Center + (shrunkenRotation + Projectile.rotation).ToRotationVector2() * root.distance;
                            rootPosition += Main.rand.NextVector2Circular(3f, 3f);
                            Vector2 bloodSpeed = (shrunkenRotation + Projectile.rotation + Main.rand.NextFloat(-0.23f, 0.21f)).ToRotationVector2() * Main.rand.NextFloat(2f, 13f);

                            Dust.NewDustPerfect(rootPosition, DustID.Blood, bloodSpeed, Scale: Main.rand.NextFloat(0.8f, 1.7f));
                        }
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D rootTex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MushroomInfestationRoots").Value;
            Vector2 basePosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY + AnchorNPC.netOffset + Projectile.velocity * 0.4f;

            //Yes, npc specific bugfixes. i don't apologize
            if (AnchorNPC.ModNPC is Crabulon crab)
                basePosition += crab.visualOffset;

            lightColor *= Utils.GetLerpValue(0, 30, Projectile.timeLeft, true);

            foreach (SporedCorpseRoot root in roots)
            {
                if (rootExpansion <= root.expansionDelay)
                    continue;

                float expansion = Math.Min(1f, (rootExpansion - root.expansionDelay) / root.expansionTime * root.stretch);
                Vector2 scale = new Vector2(1f, 1f * (float)Math.Pow(expansion, 0.7f));
                float shrunkenRotation = ((root.rotation / MathHelper.TwoPi) - 0.5f) * MathHelper.PiOver4;

                Vector2 gorePosition = basePosition + (shrunkenRotation + Projectile.rotation).ToRotationVector2() * root.distance;
                Rectangle frame = rootTex.Frame(1, 7, 0, root.variant, 0, -2);
                Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
                float rotation = Projectile.rotation + shrunkenRotation + root.tilt + MathHelper.PiOver2;
                rotation += (float)Math.Sin((rootExpansion + root.rotation * 400f) * 0.1f * root.wiggleSpeed) * 0.1f;
                SpriteEffects effects = root.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Main.EntitySpriteDraw(rootTex, gorePosition - Main.screenPosition, frame, lightColor, rotation, origin, scale * Projectile.scale, effects, 0);
            }

            return false;
        }
    }


    public class InfestationExplosionHitbox : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public ref float fadeIn => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mycelium Explosion");
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 50;
            Projectile.hide = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (FablesUtils.AABBvCircle(targetHitbox, Projectile.Center, SyringeGun.EXPLOSION_RADIUS))
                return true;
            return false;
        }

        public override void AI()
        {

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == Projectile.owner && target.active)
            {
                Vector2 offsetFromNPC = target.Center.SafeDirectionTo(Projectile.Center) *  Math.Min(Main.rand.NextFloat(10f, 40f), NPC.sWidth / 2f);

                int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, offsetFromNPC, ModContent.ProjectileType<InfestationRot>(), 0, 0, Main.myPlayer, 1f, target.whoAmI);
                Projectile.KillOldestJavelin(index, ModContent.ProjectileType<InfestationRot>(), target.whoAmI, InfestationRot.scanBuffer);
                target.AddBuff(ModContent.BuffType<InfestationDOT>(), 800);
            }
        }
    }


    public class InfestationDOT : ModBuff
    {
        public override string Texture => AssetDirectory.CrabulonDrops + "Infestation";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Infested!");
            Description.SetDefault("Shifting tendons shape anatomy");
            Main.debuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            Main.buffNoSave[Type] = true;

            NPCID.Sets.SpecificDebuffImmunity[NPCID.FungoFish][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.AnomuraFungus][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.FungiBulb][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.GiantFungiBulb][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.MushiLadybug][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.SporeBat][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.SporeSkeleton][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.ZombieMushroom][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.ZombieMushroomHat][Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.SetNPCFlag(Name);
        }
    }

    public class ManaMushroom : ModProjectile
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public static Asset<Texture2D> Glow;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mana Mushroom");
        }

        public ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;

            Projectile.timeLeft = 60 * 8;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            colorVariant = Main.rand.Next(2);
            bloomVariant = Main.rand.Next(2);
        }

        public int colorVariant;
        public int bloomVariant;

        public override void AI()
        {
            Lighting.AddLight(Projectile.position, CommonColors.MushroomDeepBlue.ToVector3() * 4f);
            Player player = Main.player[Projectile.owner];

            Projectile.tileCollide = false;

            float grabRange = 260;
            if (player.manaMagnet)
                grabRange += Item.manaGrabRange;

            if (Timer < 30)
                grabRange = 0;

            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                SoundEngine.PlaySound(SoundID.Grab, player.Center);
                player.statMana += 100;
                if (Main.myPlayer == player.whoAmI)
                    player.ManaEffect(100);

                if (player.statMana > player.statManaMax2)
                    player.statMana = player.statManaMax2;

                Projectile.Kill();
            }
            else if (Projectile.WithinRange(player.Center, grabRange))
            {
                //This is just Player.PullItem_Pickup which is private fsr
                float distanceToPlayer = Projectile.Distance(player.Center);
                distanceToPlayer = 15 / distanceToPlayer;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, (player.Center - Projectile.Center) * distanceToPlayer, 1 / 5f);
            }
            else
            {
                Projectile.velocity.Y += 0.3f;
                if (Projectile.velocity.Y > 16)
                    Projectile.velocity.Y = 16;
                Projectile.velocity.X *= 0.99f;
                Projectile.tileCollide = true;
            }

            Projectile.rotation += (Projectile.velocity.X ) * 0.04f;
            Timer++;

            if (Main.rand.NextBool(4))
            {
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 dustPosition = Projectile.Center + gushDirection * Main.rand.NextFloat(2f, 14f);
                Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0f, 3f);
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.TintableDustLighted, velocity * 0.1f - Vector2.UnitY * 0.2f + Projectile.velocity * 0.4f, 100, Color.SlateBlue, Main.rand.NextFloat(0.5f, 1f));
                d.noLightEmittence = true;
                d.noGravity = true;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.Bottom.X < player.Bottom.X)
                fallThrough = true;

            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool strongBounce = oldVelocity.Length() > 4f && Math.Abs(oldVelocity.Y) > 2.2f;

            if (strongBounce)
            {
                Projectile.rotation += Main.rand.NextFloat(0.1f, 0.4f);
                SoundStyle sound = Main.rand.NextBool(8) ? SoundID.GlommerBounce : SoundID.Item174;
                SoundEngine.PlaySound(sound with { MaxInstances = 0 }, Projectile.Center);
            }


            if (Projectile.velocity.X == oldVelocity.X && Math.Abs(oldVelocity.Y) > 2.2f)
                Projectile.velocity.X *= -1;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * 0.9f;

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Volume = timeLeft == 0 ? 0.3f : 1f});
            for (int i = 0; i < 5; i++)
            {
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 dustPosition = Projectile.Center + gushDirection * Main.rand.NextFloat(0f, 2.6f);
                Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0f, 3f);
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.MushroomSpray, velocity);
                d.noLightEmittence = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Glow ??= ModContent.Request<Texture2D>(Texture + "Glow");
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = Glow.Value;

            Rectangle frame = texture.Frame(1, 2, 0, colorVariant, 0, -2);
            Rectangle bloomFrame = glow.Frame(1, 2, 0, bloomVariant, 0, -2);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, bloomFrame, Color.White with { A = 0 } * 0.6f, Projectile.rotation, bloomFrame.Size() / 2f, Projectile.scale, 0);

            return false;
        }
    }


    #region Particles
    public class EmptySyringeParticle : Particle
    {
        public override string Texture => AssetDirectory.CrabulonDrops + "EmptySyringe";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public bool hitTarget;

        public EmptySyringeParticle(SyringeGunDart dart, int lifetime = 40)
        {
            hitTarget = dart.Projectile.numHits > 0;

            Position = dart.Projectile.Center;
            Scale = dart.Projectile.scale;
            Color = Color.White;
            Velocity = dart.Projectile.velocity;
            Rotation = dart.Projectile.rotation;
            Lifetime = lifetime;

            if (!hitTarget)
            {
                Lifetime = 120;

                //Bounce back
                Velocity.X *= -Main.rand.NextFloat(0.1f, 0.5f);
                if (Math.Abs(Velocity.X) < 1f)
                    Velocity.X = (Main.rand.NextBool() ? -1 : 1) * Main.rand.NextFloat(1f, 3f);

                Velocity.Y = -Main.rand.NextFloat(3.5f, 7f);
            }

            else
            {
                Lifetime = 20;
                Velocity *= 0.15f;
                FrontLayer = false;
            }

        }

        public override void Update()
        {
            if (hitTarget)
            {
                Velocity *= 0.97f;
                Scale *= 0.98f;
            }

            else
            {
                Rotation += Velocity.X * 0.06f;
                Velocity.Y += 0.3f;
                Velocity.X *= 0.98f;

                Scale *= 0.99f;
            }

            Color = Lighting.GetColor(Position.ToTileCoordinates());
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = ParticleTexture;
            float opacity = 1 - MathF.Pow(LifetimeCompletion, 2f);
            spriteBatch.Draw(texture, Position - basePosition, null, Color * opacity, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }
    }

    public class EnMushroomParticle : Particle
    {
        public override string Texture => AssetDirectory.CrabulonDrops + "EnMushroom";
        public static Asset<Texture2D> BloomAsset;

        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public int frame;
        public float frameCounter;

        public EnMushroomParticle(Vector2 position, Vector2 velocity, int lifetime = 40)
        {
            Position = position;
            Color = Color.White;
            Velocity = velocity;

            Variant = Main.rand.Next(10);
            frame = Main.rand.Next(8);

            Lifetime = lifetime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Scale = Main.rand.NextFloat(1f, 1.3f);
        }

        public override void Update()
        {
            Rotation += Velocity.X * 0.03f;
            Velocity.Y += 0.2f;
            //Velocity.X *= 0.98f;
            Scale *= 0.995f;
            Color = Lighting.GetColor(Position.ToTileCoordinates());

            frameCounter += 0.2f;
            if (frameCounter > 1)
            {
                frameCounter = 0;
                frame++;
                if (frame >= 8)
                    frame = 0;
            }

            Vector2 oldVelocity = Velocity;
            Velocity = Collision.TileCollision(Position - Vector2.One * 5, Velocity, 10, 10);

            if (oldVelocity != Velocity)
                Bounce(oldVelocity);
        }

        public void Bounce(Vector2 oldVelocity)
        {
            bool strongBounce = oldVelocity.Length() > 4f && Math.Abs(oldVelocity.Y) > 2.2f;

            if (strongBounce)
            {
                Rotation += Main.rand.NextFloat(0.1f, 0.4f);
                frame += Main.rand.Next(4);

                if (frame >= 8)
                    frame -= 8;

                SoundStyle sound = Main.rand.NextBool(8) ? SoundID.GlommerBounce : SoundID.Item174;
                SoundEngine.PlaySound(sound with { MaxInstances = 0 }, Position);
            }


            if (Velocity.X == oldVelocity.X && Math.Abs(oldVelocity.Y) > 2.2f && Main.rand.NextBool())
                Velocity.X *= -1;

            if (Velocity.Y != oldVelocity.Y)
                Velocity.Y = -oldVelocity.Y * Main.rand.NextFloat(0.2f, 0.7f);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = ParticleTexture;
            float opacity = Utils.GetLerpValue(0f, 15, Lifetime - Time, true);
            Rectangle crop = texture.Frame(11, 8, Variant, frame, -2, -2);
            spriteBatch.Draw(texture, Position - basePosition, crop, Color * opacity, Rotation, crop.Size() * 0.5f, Scale, SpriteEffects.None, 0f);

            if (Variant > 3)
            {
                BloomAsset ??= ModContent.Request<Texture2D>(Texture + "Glow");
                texture = BloomAsset.Value;
                spriteBatch.Draw(texture, Position - basePosition, crop, Color.White * opacity, Rotation, crop.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            }
        }
    }
    #endregion
}
