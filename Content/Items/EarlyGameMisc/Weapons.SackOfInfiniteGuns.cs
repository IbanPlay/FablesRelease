using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Particles;
using Terraria.DataStructures;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class SackOfInfiniteGuns : ModItem
    {
        public static readonly SoundStyle Misc = new("CalamityFables/Sounds/InfiniteGunBagMisc", 2) { PitchVariance = 0.4f };
        public static readonly SoundStyle Throw = new("CalamityFables/Sounds/InfiniteGunBagThrow", 2) { PitchVariance = 0.4f };
        public static readonly SoundStyle Casing = new("CalamityFables/Sounds/InfiniteGunBagBulletCasing", 2) { PitchVariance = 0.2f };
        public static readonly SoundStyle Impact = new("CalamityFables/Sounds/InfiniteGunBagImpactGround", 2) { PitchVariance = 0.2f };

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void Load() => EquipLoader.AddEquipTexture(Mod, Texture + "_Neck", EquipType.Neck, name: Name);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sack of Infinite Guns");
            Tooltip.SetDefault("");
        }

        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.DamageType = DamageClass.Magic;
            Item.width = 23;
            Item.height = 8;
            Item.useTime = Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SackOfInfiniteGunsProjectile>();
            Item.mana = 10;
            Item.shootSpeed = 10f;
            Item.UseSound = Throw;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, Main.rand.Next((int)SackOfInfiniteGunsProjectile.Style.Count));
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.FlintlockPistol, 3).
                AddIngredient(ItemID.FallenStar, 5).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class SackOfInfiniteGunsProjectile : ModProjectile, IDrawPixelated
    {
        public delegate void UseDelegate(int numShots, float damageMult, int counter, Projectile projectile, NPC[] targets);
        public readonly record struct StyleInfo(int NumShots, UseDelegate Delegate, float DamageMult = 1f);

        public enum Style { Flintlock, Revolver, Long, Stub, Shotgun, Count }

        internal static readonly Dictionary<Style, StyleInfo> Infos = [];

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public DrawhookLayer layer => DrawhookLayer.AboveTiles;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blast");
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            Main.projFrames[Type] = 5;

            Infos.Add(Style.Flintlock, new(2, BasicShoot, 1.35f));
            Infos.Add(Style.Revolver, new(1, BasicShoot));
            Infos.Add(Style.Long, new(1, BasicShoot));
            Infos.Add(Style.Stub, new(3, BasicShoot, 1.6f));
            Infos.Add(Style.Shotgun, new(5, ShotgunShoot));
        }

        public const int TrailLength = 10;
        public const int StruckDuration = 50;

        public bool HitTarget => OnHitCounter != 0;

        public Style StyleType
        {
            get => (Style)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public ref float OnHitCounter => ref Projectile.ai[1];

        public NPC[] NPCTargets;
        internal PrimitiveTrail TrailDrawer;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 160;
            Projectile.extraUpdates = 1;
            Projectile.frame = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            const int fall_duration = 150;
            const int fade_out_duration = 15;

            int styleType = (int)StyleType;
            if (Projectile.frame != styleType) //On spawn effects
            {
                SoundEngine.PlaySound(SackOfInfiniteGuns.Misc, Projectile.Center);
                Projectile.frame = styleType;
            }

            Projectile.rotation += 0.04f * Projectile.velocity.Length() * Projectile.direction;

            if (HitTarget)
            {
                if (OnHitCounter == 1) //Had just struck the target
                {
                    Projectile.timeLeft = StruckDuration + fall_duration;

                    OnHitEffects();
                    NPCTargets = FindTargets(300);
                }

                if (Projectile.timeLeft > fall_duration)
                {
                    if (!Main.dedServ)
                        ManageTrail();

                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Zero, 0.1f);
                    Projectile.rotation += 0.3f * Projectile.direction;

                    if (NPCTargets.Length != 0)
                    {
                        StyleInfo info = Infos[StyleType];
                        info.Delegate.Invoke(info.NumShots, info.DamageMult, (int)OnHitCounter, Projectile, NPCTargets);
                    }

                    if (Main.rand.NextBool(5) && Projectile.velocity.Length() > 1)
                    {
                        Vector2 velocity = Projectile.velocity * 1.2f;
                        Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, velocity.X, velocity.Y, Scale: 2).noGravity = true;
                    }
                }
                else if (!Main.dedServ)
                {
                    TrailDrawer = null;
                }

                OnHitCounter++;
            } //Slow down and begin firing

            if (Projectile.timeLeft <= fall_duration)
            {
                Projectile.velocity.X *= 0.99f;
                Projectile.velocity.Y += 0.15f;

                if (Projectile.timeLeft == fall_duration)
                    SoundEngine.PlaySound(SackOfInfiniteGuns.Casing, Projectile.Center);

                if (Projectile.timeLeft < fade_out_duration)
                    Projectile.Opacity -= 1f / fade_out_duration;
            }
        }

        #region Shoot Styles
        public static void BasicShoot(int numShots, float damageMult, int counter, Projectile projectile, NPC[] targets)
        {
            if (counter % (StruckDuration / numShots) == 0)
            {
                Vector2 velocity = projectile.DirectionTo(targets[Main.rand.Next(targets.Length)].Center) * 10;
                SoundEngine.PlaySound(CrackshotColt.ShootSound with { Pitch = counter / (float)StruckDuration, Volume = 0.7f }, projectile.Center);
                SoundEngine.PlaySound(SackOfInfiniteGuns.Casing, projectile.Center);

                if (Main.myPlayer == projectile.owner)
                {
                    int damage = (int)(projectile.damage / numShots * damageMult);
                    Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, velocity, ModContent.ProjectileType<InfiniteBullet>(), damage, projectile.knockBack, projectile.owner);
                }
            }
        }

        public static void ShotgunShoot(int numShots, float damageMult, int counter, Projectile projectile, NPC[] targets)
        {
            if (counter == StruckDuration / 2)
            {
                Vector2 velocity = projectile.DirectionTo(targets[Main.rand.Next(targets.Length)].Center) * 10;
                SoundEngine.PlaySound(WulfrumBlunderbuss.ShootSound with { Pitch = 0.5f }, projectile.Center);

                if (Main.myPlayer == projectile.owner)
                {
                    for (int i = 0; i < numShots; i++)
                    {
                        int damage = (int)(projectile.damage * damageMult);
                        Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, (velocity * Main.rand.NextFloat(0.8f, 1f)).RotateRandom(1f), ModContent.ProjectileType<InfiniteBullet>(), projectile.damage, projectile.knockBack, projectile.owner);
                    }
                }
            }
        }

        #endregion

        private NPC[] FindTargets(int distanceLimit) => [.. Projectile.FindTargets(distanceLimit, -1, true, (npc, canBeChased) => npc.type == NPCID.TargetDummy || canBeChased)];

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Check for successful bounce
            if (BounceOnHit())
            {
                // Bounce off tiles
                if (Projectile.velocity.X != oldVelocity.X)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Projectile.velocity.Y != oldVelocity.Y)
                    Projectile.velocity.Y = -oldVelocity.Y;

                // Vertical bias
                Projectile.velocity.Y -= Main.rand.NextFloat(4, 8) * oldVelocity.Y.NonZeroSign();

                return false;
            }

            Projectile.Center += oldVelocity;
            Projectile.velocity = Vector2.Zero;
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            // Won't break on tiles after bouncing
            return OnHitCounter == 0 || OnHitCounter > StruckDuration;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Check for successful bounce
            if (!BounceOnHit())
                return;

            Vector2 velocityLengths = new(Math.Abs(Projectile.velocity.X), Math.Abs(Projectile.velocity.Y));
            if (velocityLengths.X > velocityLengths.Y)
                Projectile.velocity.X *= -1;
            else
                Projectile.velocity.Y *= -1;

            Projectile.velocity.Y -= Main.rand.NextFloat(8, 14); //Upward ricochet bias
        }

        private bool BounceOnHit()
        {
            // Dont trigger without nearby targets
            if (HitTarget || FindTargets(300).Length <= 0)
                return false;

            OnHitCounter++;       
            Projectile.netUpdate = true;

            return true;
        }

        public void OnHitEffects()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(CrackshotColt.BlingHitSound with { Pitch = 1f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.NPCHit1, Projectile.Center);

                ParticleHandler.SpawnParticle(new SoundwaveRing(Projectile.Center, Vector2.Zero, Color.White, Color.PaleVioletRed, 25, 1, 8, lifeTime: 6));

                float rotation = Main.rand.NextFloat(-1.0f, 1.0f);
                ParticleHandler.SpawnParticle(new TwinkleShine(Projectile.Center, Main.rand.NextVector2Unit() * 0.1f, Color.PaleVioletRed, Color.MediumPurple, rotation, new Vector2(10, 2), new Vector2(10, 0), 15));
                ParticleHandler.SpawnParticle(new TwinkleShine(Projectile.Center, Main.rand.NextVector2Unit() * 0.1f, Color.White, Color.MediumPurple, rotation, new Vector2(10, 2), new Vector2(8, 0), 10));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Smoke, 0, 0, 150, Scale: 2);
                dust.noGravity = true;
                dust.velocity = Vector2.UnitY * -Main.rand.NextFloat(3);
            }

            SoundEngine.PlaySound(SackOfInfiniteGuns.Impact, Projectile.Center);
        }

        #region visuals & drawing
        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction, new RoundedTip());

            TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, FablesUtils.RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = Math.Min(Projectile.timeLeft / (float)TrailLength, 1f);
            return Color.Lerp(Color.Goldenrod, Color.White, 0.5f) * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio)
        {
            float width = Math.Min(Projectile.timeLeft / (float)TrailLength, 1f);
            return completionRatio * 20f * width;
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 6f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition);

            if (HitTarget)
            {
                float opacity = Math.Max(1f - Projectile.velocity.Length(), 0);
                int frame = (int)((1f - opacity) * 4);

                DrawSmear(Projectile.Center, Projectile.rotation * 1.85f, Color.SandyBrown * opacity, 24, frame);
                DrawSmear(Projectile.Center, Projectile.rotation * 1.9f, Color.Lerp(Color.SandyBrown, Color.Silver, 0.5f) * opacity, 24, frame);
                DrawSmear(Projectile.Center, Projectile.rotation * 2f, Color.Goldenrod with { A = 0 } * opacity, 24, frame);
            }
        }

        public static void DrawSmear(Vector2 origin, float rotation, Color color, float distance, int frame)
        {
            Main.instance.LoadProjectile(985);
            Texture2D smear = TextureAssets.Projectile[985].Value;

            Rectangle source = smear.Frame(1, 4, 0, 0);
            Vector2 position = (origin + (Vector2.UnitX * distance).RotatedBy(rotation) - Main.screenPosition) * 0.5f;
            float scale = distance * 0.005f;

            Main.EntitySpriteDraw(smear, position, source, color, rotation, new Vector2(source.Width, source.Height / 2), scale, default, 0);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.spriteDirection = Projectile.direction;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
            SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (!HitTarget)
            {
                for (int i = ProjectileID.Sets.TrailCacheLength[Type] - 1; i >= 0; i--)
                {
                    float progression = 1 - i / (float)ProjectileID.Sets.TrailCacheLength[Type];
                    Color trailColor = lightColor * 0.5f * (float)Math.Pow(progression, 2f);
                    Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, source, trailColor, Projectile.rotation, source.Size() / 2f, Projectile.scale, effects);
                }

                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects);
            }
            else
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects);

                float opacity = Math.Max(2f - Projectile.velocity.Length(), 0);
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Color.PaleVioletRed with { A = 0 } * opacity, Projectile.rotation, source.Size() / 2, Projectile.scale * 1.3f, effects);
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Color.White with { A = 0 } * opacity, Projectile.rotation, source.Size() / 2, Projectile.scale * 1.2f, effects);
            }

            return false;
        }
        #endregion
    }

    public class InfiniteBullet : ModProjectile, IDrawPixelated
    {
        internal PrimitiveTrail TrailDrawer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override string Texture => AssetDirectory.Invisible;

        public DrawhookLayer layer => DrawhookLayer.AboveTiles;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 4;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (Color.PaleGoldenrod * 0.8f).ToVector3() * 0.5f);

            if (!Main.dedServ)
                ManageTrail();
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction, new TriangularTip(3f));

            TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, FablesUtils.SmoothBezierPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            return true;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Sqrt(Projectile.timeLeft / 100f);
            return Color.Lerp(Color.PaleGoldenrod, Color.PaleVioletRed, completionRatio) * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio) => completionRatio * 6.4f;

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.GoldFlame);
                    dust.noGravity = true;
                }

                ParticleHandler.SpawnParticle(new ShineFlashParticle(Projectile.Center + Projectile.velocity, Vector2.Zero, Color.Goldenrod, 0.4f, lifetime: 5));
            }
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;

            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 6f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition);
        }
    }

    public class SackOfInfiniteGunsLayer : PlayerDrawLayer
    {
        internal static readonly Asset<Texture2D> BackPack = ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "SackOfInfiniteGuns_Back");
        internal static readonly Asset<Texture2D> Chain = ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "SackOfInfiniteGuns_Chain");

        public const int Chain_Length = 6;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Backpacks);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => IsActive(drawInfo.drawPlayer);

        public class ChainData : CustomGlobalData
        {
            public VerletNet ChainSimulation;
            public Vector2 ChainOrigin;
            public bool DrawChain = false;
        }

        public override void Load()
        {
            FablesPlayer.UpdateVisibleAccessoriesEvent += UpdateVisibleAccessories;
            FablesPlayer.FrameEffectsEvent += FrameEffects;
        }

        public override void Unload()
        {
            FablesPlayer.UpdateVisibleAccessoriesEvent -= UpdateVisibleAccessories;
            FablesPlayer.FrameEffectsEvent -= FrameEffects;
        }

        public static bool IsActive(Player player) => !player.dead && player.HeldItem?.type == ModContent.ItemType<SackOfInfiniteGuns>();

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Texture2D sack = BackPack.Value;

            Player player = drawInfo.drawPlayer;

            // Add player data
            if (!player.GetPlayerData(out ChainData data))
            {
                data = new ChainData();
                player.SetPlayerData(data);
            }

            Vector2 position = drawInfo.BodyPosition() + new Vector2(-12 * player.direction, player.gravDir);
            position += Main.OffsetsPlayerHeadgear[player.bodyFrame.Y / player.bodyFrame.Height] * player.gravDir;

            int frame = (int)(player.bodyFrameCounter / 18) % 4;
            if (player.ItemAnimationActive)
            {
                frame = (int)(player.itemAnimation / (float)player.itemAnimationMax * 3f);
                position.Y -= 2 * player.gravDir;
            }

            Rectangle source = sack.Frame(1, 4, 0, frame, 0, -2);
            Color color = drawInfo.colorArmorBody;

            drawInfo.DrawDataCache.Add(new DrawData(sack, position, source, color, 0f, source.Size() / 2f, 1, drawInfo.playerEffect));
            DrawChain(drawInfo, data);

            data.ChainOrigin = position + Main.screenPosition - new Vector2(8 * player.direction, ((frame == 2) ? 4 : 2) * player.gravDir);
            data.DrawChain = true;  // Only set to true when the origin has been reset
        }

        public static void DrawChain(PlayerDrawSet drawInfo, ChainData data)
        {
            Texture2D texture = Chain.Value;

            VerletNet trail = data.ChainSimulation;
            if (trail is null)
                return;

            Vector2 origin = new Vector2(0, texture.Height / 2f);
            SpriteEffects effects = (drawInfo.drawPlayer.direction == 1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
            List<Vector2> drawPositions = [];

            // Create a list of all draw positions
            const float segment_Length = 8;
            for (int i = 0; i < Chain_Length; i++)
            {
                float lengthOfNextSection = segment_Length * i;

                // First entry is always the first base point
                if (i == 0)
                    drawPositions.Add(trail.points[0].position);
                // Increment distance and add new points
                else if (DistanceAndIndex(trail.points, ref lengthOfNextSection, out int basePointsIndex))
                {
                    Vector2 currentBasePoint = trail.points[basePointsIndex].position;
                    Vector2 nextBasePoint = trail.points[basePointsIndex + 1].position;
                    float baseSectionLength = currentBasePoint.Distance(nextBasePoint);

                    Vector2 retrievedPoint = Vector2.Lerp(currentBasePoint, nextBasePoint, Utils.GetLerpValue(0, baseSectionLength, lengthOfNextSection));
                    drawPositions.Add(retrievedPoint);
                }
            }

            // Draw from list
            for (int i = 0; i < drawPositions.Count; i++)
            {
                Vector2 drawPosition = drawPositions[i];

                Color color = Lighting.GetColor(drawPosition.ToTileCoordinates()) * (drawInfo.colorArmorBody.A / 255f);
                float rotation = (i == drawPositions.Count - 1 ? drawPosition - drawPositions[i - 1] : drawPositions[i + 1] - drawPosition).ToRotation();

                drawInfo.DrawDataCache.Add(new(texture, drawPosition - Main.screenPosition, null, color, rotation, origin, 1, effects, 0) { shader = drawInfo.drawPlayer.cBackpack });
            }

            static bool DistanceAndIndex(List<VerletPoint> points, ref float length, out int index)
            {
                index = 0;

                // Check through each index until the given length is short enough
                while (index < points.Count - 1)
                {
                    // Get length of base points at index
                    float baseSectionLength = points[index].position.Distance(points[index + 1].position);

                    // Go to next section if length is greater
                    if (baseSectionLength < length)
                    {
                        length -= baseSectionLength;
                        index++;
                    }
                    // Otherwise, stop and return true, signifying that length is not too long
                    else
                        return true;
                }

                return false;
            }
        }

        private static void UpdateVisibleAccessories(Player player)
        {            
            if (!player.GetPlayerData(out ChainData data))
                return;

            if (Main.dedServ || !IsActive(player) || !data.DrawChain)
            {
                data.ChainSimulation = null;
                data.DrawChain = false;
                return;
            }

            ref var chainOrigin = ref data.ChainOrigin;
            ref var chainSimulation = ref data.ChainSimulation;

            Vector2 origin = chainOrigin + player.velocity;
            if (chainSimulation is null || !chainSimulation.points[0].position.WithinRange(origin, 400f))
            {
                chainSimulation = new VerletNet();
                chainSimulation.AddChain(new VerletPoint(origin, true), new VerletPoint(origin - Vector2.UnitX * player.direction * 8 * Chain_Length), Chain_Length);
            }
            else
            {
                foreach (VerletPoint pt in chainSimulation.points)
                    pt.tileCollideStyle = 1;

                chainSimulation.points[0].position = origin;
                chainSimulation.Update(3, 1f, 0.9f);
            }
        }

        private static void FrameEffects(Player player)
        {
            if (IsActive(player))
                player.neck = EquipLoader.GetEquipSlot(CalamityFables.Instance, nameof(SackOfInfiniteGuns), EquipType.Neck);
        }
    }
}