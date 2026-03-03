using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Shaders;
using CalamityFables.Content.Boss.DesertWormBoss;


namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    [AutoloadEquip(EquipType.Neck)]
    public class IntestinalScarf : ModItem
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;
        public static int NeckEquipSlot = -1;
        public static int MIN_DAMAGE_TO_TRIGGER = 20;
        public static int VOMIT_DAMAGE = 100;

        public static float MAX_HEAL_PERCENT = 0.15f;
        public static int HEAL_DECAY_TIME = 60 * 6;
        public static int VOMIT_DESPAWN_TIME = 60 * 30;

        public static readonly SoundStyle VomitSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "IntestinalScarfVomit");
        public static readonly SoundStyle SplortchSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "IntestinalScarfFoodGoreLand");
        public static readonly SoundStyle EatSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "IntestinalScarfFoodConsume");


        public override void Load()
        {
            FablesBuff.PostDrawBuffEvent += VomitOnTheIcons;
            FablesPlayer.OnHurtEvent += ProjectileVomit;
        }

        private void ProjectileVomit(Player player, Player.HurtInfo info)
        {
            if (player.whoAmI != Main.myPlayer ||
                !player.wellFed ||
                info.Damage < MIN_DAMAGE_TO_TRIGGER ||
                !info.DamageSource.TryGetCausingEntity(out _) ||
                !player.GetPlayerFlag(Name) ||
                player.GetPlayerFlag("InsatiableHunger")
                )
                return;

            int buffID = -1;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (player.buffTime[i] >= 1 && BuffID.Sets.IsWellFed[player.buffType[i]])
                {
                    buffID = i;
                    break;
                }
            }

            if (buffID == -1)
                return;
            if (!player.GetPlayerAccessory(Type, out Item spawnItem))
                return;
            HEAL_DECAY_TIME = 60 * 6;

            int type = player.buffType[buffID];
            int time = player.buffTime[buffID];
            player.DelBuff(buffID);

            VerletNet scarf = player.GetModPlayer<FablesPlayer>().scarfSimulation;
            if (scarf != null)
                foreach (VerletPoint point in scarf.points)
                    point.oldPosition += Main.rand.NextVector2Circular(15f, 15f);

            NPC nearestNPC = null;
            float closestNPCDistance = float.PositiveInfinity;

            for (int i = 0; i < 200; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.CanBeChasedBy(player) && Collision.CanHit(player, npc))
                {
                    float distanceToPlayer = npc.DistanceSQ(player.Center);
                    if (distanceToPlayer < closestNPCDistance)
                    {
                        closestNPCDistance = distanceToPlayer;
                        nearestNPC = npc;
                    }
                }
            }

            Vector2 vomitVelocity;

            if (nearestNPC != null)
            {
                vomitVelocity = player.SafeDirectionTo(nearestNPC.Center) * 11f - Vector2.UnitY * 1f;
            }
            else
            {
                vomitVelocity = player.velocity * 0.3f - Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(4f, 6f);
            }

            Projectile.NewProjectile(
                player.GetSource_Accessory_OnHurt(spawnItem, info.DamageSource),
                player.MountedCenter - Vector2.UnitY * 20f * player.gravDir,
                vomitVelocity,
                ModContent.ProjectileType<RegurgitatedBuff>(),
                VOMIT_DAMAGE, 
                3,
                Main.myPlayer,
                info.Damage,
                type,
                time);
        }

        public static Asset<Texture2D> VomitOutline;
        public static Asset<Texture2D> VomitOverlay;

        public static Asset<Texture2D> HungerOverlay;


        private void VomitOnTheIcons(SpriteBatch spriteBatch, int type, int buffIndex, BuffDrawParams drawParams)
        {
            if (!BuffID.Sets.IsWellFed[type])
                return;

            //Insatiable hunger draws maws on the buff icon
            if (Main.LocalPlayer.GetPlayerFlag("InsatiableHunger"))
            {
                HungerOverlay ??= ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "InsatiableHungerOverlay");
                Texture2D teeth = HungerOverlay.Value;

                Color mawsColor = Color.Lerp(drawParams.DrawColor, Color.White, 0.6f);
                Rectangle mawsTop = new Rectangle(0, 0, teeth.Width, teeth.Height / 2);
                Rectangle mawsBottom = new Rectangle(0, teeth.Height / 2, teeth.Width, teeth.Height / 2);

                float gnawRotation = MathHelper.Clamp(MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.2f + MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + 0.3f) * 0.1f, -0.2f, 0.2f);
                float gnawStrenght = MathF.Pow(MathHelper.Clamp(MathF.Sin(Main.GlobalTimeWrappedHourly * 7f), 0, 1), 2f);

                Vector2 gnawOffset = Vector2.UnitY.RotatedBy(gnawRotation) * gnawStrenght * 4f;

                spriteBatch.Draw(HungerOverlay.Value, drawParams.Position + Vector2.One * 16f - gnawOffset, mawsTop, mawsColor, gnawRotation, new Vector2(teeth.Width / 2, teeth.Height / 2), 1f, 0, 0);
                spriteBatch.Draw(HungerOverlay.Value, drawParams.Position + Vector2.One * 16f + gnawOffset, mawsBottom, mawsColor, gnawRotation, new Vector2(teeth.Width / 2, 0), 1f, 0, 0);

                return;
            }

            else if (!Main.LocalPlayer.GetPlayerFlag(Name))
                return;

            VomitOutline ??= ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + Name + "DebuffOutline");
            VomitOverlay ??= ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + Name + "DebuffOverlay");

            Color outlineColor = Color.Lerp(drawParams.DrawColor, Color.White, 0.6f);
            spriteBatch.Draw(VomitOverlay.Value, drawParams.Position + Vector2.One * 2f, null, drawParams.DrawColor * 0.6f, 0f, Vector2.Zero, 1f, 0, 0);
            spriteBatch.Draw(VomitOutline.Value, drawParams.Position - Vector2.UnitX * 2f, null, outlineColor, 0f, Vector2.Zero, 1f, 0, 0);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Intestinal Scarf");
            Tooltip.SetDefault("Strong hits make you vomit food buffs as damaging projectiles\n" +
                "You can eat the vomited food back up to regain the buff\n" +
                "Eating the vomit grants additional regeneration and stats\n" +
                "[c/b3b2af:'Would you like an after-dinner mint, monsieur?']");

            if (!Main.dedServ)
            {
                NeckEquipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Neck);
            }
        }

        public override void SetDefaults()
        {
            Item.expert = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.accessory = true;
            Item.width = 32;
            Item.height = 32;
            Item.neckSlot = NeckEquipSlot;
        }

        public override void UpdateEquip(Player player)
        {
            player.SetPlayerFlag(Name);
            player.SetPlayerAccessory(Item);
        }
    }

    public class RegurgitatedBuff : ModProjectile
    {
        public bool Floored {
            get => Projectile.ai[0] == 1;
            set => Projectile.ai[0] = value ? 1 : 0;
        }

        public int BuffType => (int)Projectile.ai[1];
        public int BuffTime => (int)Projectile.ai[2];

        public Player Owner => Main.player[Projectile.owner];

        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;
        public static Asset<Texture2D> BackAsset;
        public static Asset<Texture2D> FlyingAsset;
        public static Asset<Texture2D> FlyingBackAsset;

        public static Asset<Texture2D> BuffOutlineAsset;
        public static Asset<Texture2D> GroundOutlineAsset;
        public static Asset<Texture2D> FlyingOutlineAsset;

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;
        internal float timeFloored = 0f;

        public float originalDamageTaken = 0;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Regurgitated Meal");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.timeLeft = IntestinalScarf.VOMIT_DESPAWN_TIME;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
        }

        public bool playedVomitSound = false;


        public override void AI()
        {
            if (!playedVomitSound)
            {
                playedVomitSound = true;
                SoundEngine.PlaySound(IntestinalScarf.VomitSound, Projectile.Center);
            }

            //We originally use projectile.ai[0] to store the damage of the first hit and then its just used for the floored or not calculations, so we have to cache it
            if (originalDamageTaken == 0 && Projectile.ai[0] > 1)
                originalDamageTaken = Projectile.ai[0];

            if (Floored)
            {
                Projectile.velocity.Y = 0;
                Projectile.velocity.X *= 0.95f;
                Projectile.rotation = 0;

                if (!FablesUtils.SolidCollisionFix(Projectile.BottomLeft, Projectile.width, 4, true))
                {
                    Projectile.tileCollide = true;
                    Floored = false;
                }

                //Flies effect
                if (Main.rand.NextBool(100))
                {
                    ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.PooFly, new ParticleOrchestraSettings
                    {
                        PositionInWorld = Main.rand.NextVector2FromRectangle(Projectile.Hitbox) + new Vector2(Main.rand.NextFloat(-20f, 20f), -16f)
                    });
                }


                if (Main.rand.NextBool(15))
                {
                    Vector2 streakOffset = Vector2.UnitX * Main.rand.NextFloat(-30f, 30f) - Vector2.UnitY * Main.rand.NextFloat(0f, 20f);
                    Vector2 streakPosition = Projectile.Bottom + streakOffset;
                    Color effectColor = Color.Lerp(Color.Olive, Color.YellowGreen, (float)Math.Pow(Math.Abs(streakOffset.X) / 30f, 0.4f));
                    Particle p = new PixelStreaks(streakPosition, effectColor, Color.Olive * 0.6f, Main.rand.NextFloat(0.3f, 0.7f));
                    p.Velocity *= 0.5f;

                    ParticleHandler.SpawnParticle(p);
                }

                if (Main.myPlayer == Projectile.owner && Projectile.Hitbox.Contains(Main.MouseWorld.ToPoint()) && Main.LocalPlayer.Distance(Projectile.Center) < 850)
                {
                    string buffName = Lang.GetBuffName(BuffType);
                    Main.instance.MouseTextHackZoom(buffName);
                    Main.LocalPlayer.cursorItemIconEnabled = false;
                }

                timeFloored++;

                float lightOpacity = 0.55f * Utils.GetLerpValue(0, 45, Projectile.timeLeft, true) * Utils.GetLerpValue(IntestinalScarf.VOMIT_DESPAWN_TIME, IntestinalScarf.VOMIT_DESPAWN_TIME - 220f, Projectile.timeLeft, true);

                Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.6f, 0.2f) * lightOpacity);

                if (Projectile.timeLeft < IntestinalScarf.VOMIT_DESPAWN_TIME - 60 && Owner.WithinRange(Projectile.Center, 50))
                    ConsumeTheMeat(Owner);
            }
            else
            {
                Projectile.velocity.Y += 0.3f;
                Projectile.rotation += Projectile.velocity.X * 0.05f;
                timeFloored = 0f;

                DoDust();
            }

            if (!Main.dedServ)
            {
                ManageCache();
                ManageTrail();
            }
        }

        public override bool? CanDamage()
        {
            if (Floored)
                return false;
            return base.CanDamage();
        }

        public void DoDust()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(3))
            {
                Dust dusty = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextBool() ? 5 : 4, -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.2f, 2f), 100);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.5f);
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);
                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            if (Main.rand.NextBool(4))
            {
                int goreType = Mod.Find<ModGore>("GroundBeefGore" + Main.rand.Next(1, 6).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Projectile.velocity * 0.4f, goreType);
                gore.timeLeft = 2;
                gore.alpha = Main.rand.Next(80, 100);
                gore.scale *= 0.5f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.X != Projectile.velocity.X)
                Projectile.velocity.X = -oldVelocity.X * 0.6f;

            if (oldVelocity.Y >= 0 && Projectile.velocity.Y < oldVelocity.Y)
            {
                Floored = true;
                Projectile.tileCollide = false;
                Projectile.velocity.X *= 0.2f;

                Projectile.velocity.Y = 0f;

                SoundEngine.PlaySound(IntestinalScarf.SplortchSound, Projectile.Center);
            }

            return false;
        }
        
        public void ConsumeTheMeat(Player player)
        {
            ParticleHandler.SpawnParticle(new FeedTheHungerEffect(Projectile.Bottom - Vector2.UnitY * 30f));
            player.AddBuff(BuffType, BuffTime);
            player.AddBuff(ModContent.BuffType<InsatiableHunger>(), 60 * 10);

            SoundEngine.PlaySound(IntestinalScarf.EatSound, player.Center);

            if (IntestinalScarf.VOMIT_DESPAWN_TIME - Projectile.timeLeft < IntestinalScarf.HEAL_DECAY_TIME)
            {
                float decay = Utils.GetLerpValue(IntestinalScarf.VOMIT_DESPAWN_TIME - IntestinalScarf.HEAL_DECAY_TIME, IntestinalScarf.VOMIT_DESPAWN_TIME, Projectile.timeLeft, true);
                int heal = (int)(originalDamageTaken * decay * IntestinalScarf.MAX_HEAL_PERCENT);
                if (heal > 0)
                    player.Heal(heal);

                if (player.immuneTime < 15)
                {
                    player.immune = true;
                    player.immuneNoBlink = true;
                    player.immuneTime = 15;
                }
            }

            Projectile.Kill();
            if (player.whoAmI == Main.myPlayer)
            {
                //VignetteFadeEffects.AddVignetteEffect(new VignettePunchModifier(16, 0.5f));
                FablesUtils.GoScary(0.0f, 0, 5, 20, 0.5f, 20, -0.3f, 200);
            }
        }

        public override void OnKill(int timeLeft)
        {

            if (Main.dedServ)
                return;

            for (int i = 0; i < 16; i++)
            {
                Dust dusty = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(60f, 20f), Main.rand.NextBool() ? 5 : 4, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 2f), 100);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.5f);
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);
                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            for (int i = 0; i < 7; i++)
            {
                int goreType = Mod.Find<ModGore>("GroundBeefGore" + Main.rand.Next(1, 6).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), -Vector2.UnitY.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.5f, 3f), goreType);
                gore.timeLeft = 2;
                gore.alpha = Main.rand.Next(80, 100);
                gore.scale *= Main.rand.NextFloat(0.5f, 1f);
            }
        }

        public void ManageCache()
        {
            if (cache == null)
            {
                cache = new List<Vector2>();

                for (int i = 0; i < 26; i++)
                {
                    cache.Add(Projectile.Center);
                }
            }

            cache.Add(Projectile.Center);
            while (cache.Count > 26)
                cache.RemoveAt(0);
        }

        public virtual void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);
            TrailDrawer.SetPositionsSmart(cache, Projectile.Center, FablesUtils.RigidPointRetreivalFunction);
        }

        public virtual float WidthFunction(float completion)
        {
            return 11f * (float)Math.Pow(completion, 0.6f) * (0.85f + 0.15f * Utils.GetLerpValue(12f, 5f, Projectile.velocity.Length(), true));
        }
        public virtual Color ColorFunction(float completion)
        {
            return Color.Lerp(Color.Olive, Color.DarkGoldenrod, (float)Math.Pow(completion, 3f)) * completion;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = Scene["GlorpTrail"].GetShader().Shader;
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 4.2f + Projectile.whoAmI * 0.1f);
            effect.Parameters["repeats"].SetValue(3f);
            effect.Parameters["voronoi"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "Voronoi").Value);
            effect.Parameters["noiseOverlay"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "PrettyManifoldNoise").Value);
            TrailDrawer?.Render(effect, -Main.screenPosition);

            Asset<Texture2D> baseAsset = TextureAssets.Projectile[Type];
            FlyingAsset ??= ModContent.Request<Texture2D>(Texture + "Flying");
            FlyingBackAsset ??=ModContent.Request<Texture2D>(Texture + "FlyingBack");
            BackAsset ??= ModContent.Request<Texture2D>(Texture + "Back");
            
            BuffOutlineAsset ??= ModContent.Request<Texture2D>(Texture + "BuffOutline");
            GroundOutlineAsset ??= ModContent.Request<Texture2D>(Texture + "Outline");
            FlyingOutlineAsset ??= ModContent.Request<Texture2D>(Texture + "FlyingOutline");
            IntestinalScarf.VomitOverlay = IntestinalScarf.VomitOverlay ?? ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "IntestinalScarfDebuffOverlay");


            Texture2D buffIcon = TextureAssets.Buff[BuffType].Value;
            Color outlineColor = FablesUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f) % 1, Color.Olive, Color.DarkGoldenrod, Color.Salmon, Color.Firebrick);

            if (!Floored)
            {
                Main.EntitySpriteDraw(FlyingOutlineAsset.Value, Projectile.Center - Main.screenPosition, null, outlineColor, Projectile.rotation, FlyingOutlineAsset.Size() / 2f, Projectile.scale, 0, 0);

                Main.EntitySpriteDraw(FlyingBackAsset.Value, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, FlyingBackAsset.Size() / 2f, Projectile.scale, 0, 0);
                Main.EntitySpriteDraw(buffIcon, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, buffIcon.Size() / 2f, Projectile.scale, 0, 0);
                Main.EntitySpriteDraw(IntestinalScarf.VomitOverlay.Value, Projectile.Center - Main.screenPosition, null, lightColor * 0.3f, Projectile.rotation, IntestinalScarf.VomitOverlay.Size() / 2f, Projectile.scale, 0, 0);
                Main.EntitySpriteDraw(FlyingAsset.Value, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, FlyingAsset.Size() / 2f, Projectile.scale, 0, 0);
            }

            else
            {
                Texture2D bloomTexture = AssetDirectory.CommonTextures.BigBloomCircle.Value;
                Vector2 bloomPos = Projectile.Bottom - Main.screenPosition - Vector2.UnitY * 22f;
                Color bloomColor = FablesUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.25f) % 1, Color.Olive, Color.OliveDrab, Color.Peru);
                bloomColor = Color.Lerp(bloomColor, Color.Black, 0.4f);
                bloomColor *= 0.35f * Utils.GetLerpValue(0, 45, Projectile.timeLeft, true) * Utils.GetLerpValue(IntestinalScarf.VOMIT_DESPAWN_TIME, IntestinalScarf.VOMIT_DESPAWN_TIME - 220f, Projectile.timeLeft, true);
                // bloomColor.A = 0;
                DrawMenacingAura(bloomTexture, bloomPos, bloomColor, 3f);

                Vector2 origin = new Vector2(baseAsset.Width() / 2f, baseAsset.Height());
                Vector2 buffOffset = new Vector2(4f, -16f);
                Rectangle crop = new Rectangle(0, 0, buffIcon.Width, buffIcon.Height - 3);
                Vector2 warble = new Vector2(1 - MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.1f, 1 + MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + 0.2f) * 0.06f) * Projectile.scale;

                Vector2 drawPos = Projectile.Bottom - Main.screenPosition + Vector2.UnitY * 2f;

                float dissapear = MathF.Pow(Utils.GetLerpValue(40, -14, Projectile.timeLeft, true), 1.6f);
                warble.X *= 1 + dissapear;
                warble.Y *= MathF.Pow(1 - dissapear, 2f);

                buffOffset *= warble;

                //Draw outlines
                Main.EntitySpriteDraw(GroundOutlineAsset.Value, drawPos, null, outlineColor, Projectile.rotation, new Vector2(GroundOutlineAsset.Width() / 2f, GroundOutlineAsset.Height() - 2f), warble, 0, 0);
                Main.EntitySpriteDraw(BuffOutlineAsset.Value, drawPos + buffOffset, new Rectangle(0, 0, BuffOutlineAsset.Width(), BuffOutlineAsset.Height() - 5), outlineColor, Projectile.rotation + 0.2f, BuffOutlineAsset.Size() / 2f, warble, 0, 0);

                //Draw sprite
                Main.EntitySpriteDraw(BackAsset.Value, drawPos, null, lightColor, Projectile.rotation, origin, warble, 0, 0);
                Main.EntitySpriteDraw(buffIcon, drawPos + buffOffset, crop, lightColor, Projectile.rotation + 0.2f, buffIcon.Size() / 2f, warble, 0, 0);
                Main.EntitySpriteDraw(IntestinalScarf.VomitOverlay.Value, drawPos + buffOffset, crop, lightColor * 0.3f, Projectile.rotation + 0.2f, IntestinalScarf.VomitOverlay.Size() / 2f, warble, 0, 0);
                Main.EntitySpriteDraw(baseAsset.Value, drawPos, null, lightColor, Projectile.rotation, origin, warble, 0, 0);
            }

            return false;
        }

        public void DrawMenacingAura(Texture2D texture, Vector2 position, Color auraColor, float auraSize)
        {

            Effect effect = Scene["RadialWarp"].GetShader().Shader;

            effect.Parameters["noiseTexture"].SetValue(AssetDirectory.NoiseTextures.Manifold3.Value);
            effect.Parameters["minRadius"].SetValue(0f);
            effect.Parameters["lerpStrenght"].SetValue(0.6f);
            effect.Parameters["radiusDisplacement"].SetValue(-0.21f);
            effect.Parameters["noiseScale"].SetValue(1f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.04f + Projectile.whoAmI * 0.04f);
            effect.Parameters["color"].SetValue(auraColor.ToVector4());

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(texture, position, null, Color.White, 0, texture.Size() / 2f, 0.5f * auraSize, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture, position, null, Color.White, 0, texture.Size() / 2f, 0.7f * auraSize, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    public class InsatiableHunger : ModBuff
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + "InsatiableHunger";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;

            DisplayName.SetDefault("Feed The Hunger");
            Description.SetDefault("Upgrades the stats provided by all food buffs even further\n"+
                "You won't vomit any more food... for now");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.wellFed)
            {
                //Each stage of well fed gives..
                player.statDefense++; //...1 defense more than the last
                player.GetCritChance(DamageClass.Generic) += 1f; //...1% more crit chance more than the last
                player.GetAttackSpeed(DamageClass.Melee) += 0.025f; //2.5% more melee speed
                player.GetDamage(DamageClass.Generic) += 0.025f; //2.5% more damage
                player.GetKnockback(DamageClass.Summon).Base += 0.25f; //0.25 more minion knockback
                player.moveSpeed += 0.1f; //10% more movement speed
                player.pickSpeed -= 0.05f; //5% more mining speed
            }
            player.SetPlayerFlag(Name);

            if (Main.rand.NextBool(15))
            {
                Vector2 streakOffset = Vector2.UnitX * Main.rand.NextFloat(-30f, 30f) + Vector2.UnitY * Main.rand.NextFloat(0f, 20f);
                Vector2 streakPosition = player.Center + streakOffset;
                Color effectColor = Color.Lerp(Color.White, Color.Red, (float)Math.Pow(Math.Abs(streakOffset.X) / 30f, 0.4f));

                ParticleHandler.SpawnParticle(new PixelStreaks(streakPosition, effectColor, Color.Red * 0.8f, Main.rand.NextFloat(0.7f, 1.2f)));
            }

            if (Main.rand.NextBool(10))
            {
                Vector2 dustPosition = player.Center + Vector2.UnitX * Main.rand.NextFloat(-20f, 20f) + Vector2.UnitY * Main.rand.NextFloat(-20f, 20f);
                Dust d = Dust.NewDustPerfect(dustPosition, 182, -Vector2.UnitY * Main.rand.NextFloat(1f, 4f), 0, Scale: Main.rand.NextFloat(0.7f, 1f));
                d.noLight = true;
                d.noGravity = true;
                d.alpha = 0;
            }
        }

        //Can't cancel it: Prevents it from being a constant machinegun of vomit
        public override bool RightClick(int buffIndex)
        {
            return false;
        }
    }


    public class FeedTheHungerEffect : Particle
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + "FeedTheHungerSplat";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;


        public FeedTheHungerEffect(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Color = Color.White;

            Rotation = Main.rand.NextFloat(-0.3f, 0.3f);
            Scale = 1.2f;

            Lifetime = 40;
        }

        public override void Update()
        {
            Scale = 1f + 0.3f * MathF.Pow(Utils.GetLerpValue(0.3f, 0f, LifetimeCompletion, true), 3);
            Rotation += 0.05f * MathF.Pow(Utils.GetLerpValue(0.4f, 0f, LifetimeCompletion, true), 3) * Rotation.NonZeroSign();
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D splatTex = ParticleTexture;
            Texture2D topTeeth = ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "FeedTheHungerTopTeeth").Value;
            Texture2D botTeeth = ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "FeedTheHungerBottomTeeth").Value;

            Vector2 origin = splatTex.Size() / 2f;
            float opacity = Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true) * Utils.GetLerpValue(0f, 0.1f, LifetimeCompletion, true);

            Vector2 jawDisplacementUnit = Vector2.UnitY.RotatedBy(Rotation) * Scale;

            Color drawColor = Color;

            //Fades less in red
            drawColor.R = (byte)(drawColor.R * Math.Pow(opacity, 0.8f));
            drawColor.G = (byte)(drawColor.G * Math.Pow(opacity, 2f));
            drawColor.B = (byte)(drawColor.B * Math.Pow(opacity, 2f));
            drawColor.A = (byte)(drawColor.A * opacity);

            jawDisplacementUnit *= MathF.Pow(Utils.GetLerpValue(0.8f, 0f, LifetimeCompletion, true), 4f) * 14f;


            spriteBatch.Draw(splatTex, Position - basePosition, null, drawColor * opacity * 0.2f, Rotation, origin, MathF.Pow(Scale, 3f), SpriteEffects.None, 0);
            spriteBatch.Draw(splatTex, Position - basePosition, null, drawColor * opacity, Rotation, origin, Scale, SpriteEffects.None, 0);


            spriteBatch.Draw(botTeeth, Position + jawDisplacementUnit - basePosition, null, drawColor, Rotation * 1.02f, origin, Scale * 1.1f, SpriteEffects.None, 0);
            spriteBatch.Draw(topTeeth, Position - jawDisplacementUnit - basePosition, null, drawColor, Rotation * 1.02f, origin, Scale * 1.1f, SpriteEffects.None, 0);


            spriteBatch.Draw(botTeeth, Position + jawDisplacementUnit - basePosition, null, drawColor * 0.4f, Rotation * 1.02f, origin, MathF.Pow(Scale, 3f) * 1.1f, SpriteEffects.None, 0);
            spriteBatch.Draw(topTeeth, Position - jawDisplacementUnit - basePosition, null, drawColor * 0.4f, Rotation * 1.02f, origin, MathF.Pow(Scale, 3f) * 1.1f, SpriteEffects.None, 0);
        }
    }
}