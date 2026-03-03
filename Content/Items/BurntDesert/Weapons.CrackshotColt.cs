namespace CalamityFables.Content.Items.BurntDesert
{
    [ReplacingCalamity("CrackshotColt")]
    public class CrackshotColt : ModItem
    {
        public override string Texture => AssetDirectory.DesertItems + Name;

        public static readonly SoundStyle ShootSound = new("CalamityFables/Sounds/CrackshotColtShot") { PitchVariance = 0.1f };

        public static readonly SoundStyle BlingSound = new("CalamityFables/Sounds/Ultrabling") { PitchVariance = 0.5f };
        public static readonly SoundStyle BlingHitSound = new("CalamityFables/Sounds/UltrablingHit") { PitchVariance = 0.5f };

        public static float MaxDownwardsAngle4Coin = MathHelper.PiOver4;
        public static float RicochetDamageMult = 2.5f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crackshot Colt");
            Tooltip.SetDefault("Right click to toss a coin into the air\n" +
                               "Bullets ricochet off coins into nearby enemies\n" +
                               "Coin tosses consume copper coins");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 16;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 23;
            Item.height = 8;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CrackshotBlast>();
            Item.useAmmo = AmmoID.Bullet;
            Item.shootSpeed = 14f;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        public override bool AltFunctionUse(Player player) => true;
        public override void HoldItem(Player player) => player.SyncMousePosition();
        public override bool CanConsumeAmmo(Item ammo, Player player) => player.altFunctionUse != 2;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
                return player.CanAfford(1); //Breaks if the player has > 999 plat. Ask tml people to fix that?
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                player.BuyItem(1);
            }

            return base.UseItem(player);
        }

        public override float UseSpeedMultiplier(Player player)
        {
            if (player.altFunctionUse == 2)
                return 1.3f;
            return 1f;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {

            //Override every projectile
            type = ModContent.ProjectileType<CrackshotBlast>();

            if (player.altFunctionUse == 2)
            {
                damage = 0;
                type = ModContent.ProjectileType<CrackshotCoin>();

                //Ok the velocity is flipped because the in world coordinates have 0 at the top, so to do the typical trigo stuff we flip it, you get me.
                float shootAngle = (player.MouseWorld() - player.MountedCenter).ToRotation() * -1;

                if (shootAngle > -MathHelper.Pi + MaxDownwardsAngle4Coin && shootAngle < -MathHelper.PiOver2)
                    shootAngle = -MathHelper.Pi + MaxDownwardsAngle4Coin;

                else if (shootAngle < -MaxDownwardsAngle4Coin && shootAngle >= -MathHelper.PiOver2)
                    shootAngle = -MaxDownwardsAngle4Coin;

                velocity = (shootAngle * -1).ToRotationVector2() * 1.3f - Vector2.UnitY * 1.12f * player.gravDir + player.velocity / 4f;

                if (player.gravDir < 0)
                    position.Y += 20f;
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;
            Vector2 itemSize = new Vector2(40, 20);
            Vector2 itemOrigin = new Vector2(-15, 1);

            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);

            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.MouseWorld()).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.4f)
                rotation += -0.45f * (float)Math.Pow((0.4f - animProgress) / 0.4f, 2) * player.direction;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.FlintlockPistol).
                AddRecipeGroup(FablesRecipes.AnyGoldBarGroup, 5).
                AddIngredient(ItemID.AntlionMandible, 5).
                AddIngredient(ItemID.CopperCoin, 5).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class CrackshotBlast : ModProjectile, IDrawPixelated
    {
        internal PrimitiveTrail TrailDrawer;

        public DrawhookLayer layer => DrawhookLayer.AboveTiles;

        public ref float Boosted => ref Projectile.ai[0];
        public ref float DieSoon => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blast");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailLenght;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 4000;
        }

        public static int Lifetime = 600;
        public static int TrailLenght = 35;

        public override string Texture => AssetDirectory.Invisible;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 7;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => Projectile.numHits == 0;

        public bool playedSound = false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (Color.Gold * 0.8f).ToVector3() * 0.5f);

            if (!Main.dedServ)
            {
                ManageTrail();
                if (!playedSound)
                {
                    playedSound = true;
                    SoundEngine.PlaySound(CrackshotColt.ShootSound with { Volume = 0.6f }, Projectile.Center);
                }
            }

            if (Boosted == 0)
            {
                //Check for coins
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.ModProjectile != null && proj.type == ModContent.ProjectileType<CrackshotCoin>())
                    {
                        if (Collision.CheckAABBvAABBCollision(proj.Hitbox.TopLeft(), proj.Hitbox.Size(), Projectile.Hitbox.TopLeft(), Projectile.Hitbox.Size()))
                        {
                            NPC target = Projectile.Center.ClosestNPCAt(900, false, true);
                            if (target != null)
                            {
                                Projectile.velocity = (Projectile.DirectionTo(target.Center) * 16f);
                                Boosted = 1;
                                if (CameraManager.Shake < 2 && Main.LocalPlayer.WithinRange(Projectile.Center, 1000))
                                    CameraManager.Shake = 2;

                                Projectile.damage = (int)(Projectile.damage * CrackshotColt.RicochetDamageMult);
                                if (Projectile.owner == Main.myPlayer)
                                {
                                    Projectile.netUpdate = true;
                                    new SyncRicoshotPacket(Projectile, proj).Send(-1, -1, false);
                                }
                            }

                            proj.active = false;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Item.NewItem(proj.GetSource_DropAsItem(), proj.Center, Vector2.One, ItemID.CopperCoin);

                            SoundEngine.PlaySound(CrackshotColt.BlingHitSound, proj.Center);
                            return;
                        }
                    }
                }
            }

            if (DieSoon == 1)
                Projectile.velocity = Vector2.Zero;
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);

            TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, FablesUtils.RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Center += oldVelocity;
            Projectile.velocity = Vector2.Zero;
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            //DieSoon = 1f;
            //Projectile.timeLeft = Math.Min((Lifetime - Projectile.timeLeft), TrailLenght);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            //DieSoon = 1f;
            Projectile.netUpdate = true;
            //Projectile.timeLeft = Math.Min((Lifetime - Projectile.timeLeft), TrailLenght);
        }


        public int lastTimeLeft;
        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = Math.Min(lastTimeLeft / (float)TrailLenght, 1f);
            return Color.PaleGoldenrod * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio)
        {
            float width = Math.Min(lastTimeLeft / (float)TrailLenght, 1f);
            return (completionRatio) * 6.4f * width;
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            lastTimeLeft = Projectile.timeLeft;
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 6f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            
            lastTimeLeft = timeLeft;
            List<Vector2> positions = Projectile.oldPos.Reverse().ToList();
            for (int i = 0; i < positions.Count; i++)
                if (positions[i] != Vector2.Zero)
                    positions[i] += Projectile.Size * 0.5f;

            //Update the trail so its last position is the actual last position, since the position array isnt updated when it dies
            if (positions.Count > 3)
            {
                positions.RemoveAt(0);
                positions.Add(positions[^1] + (positions[^1] - positions[^2]).SafeNormalize(Vector2.Zero) * ((positions[^1] - positions[^2]).Length() + 8f));
            }

            if (positions.Count > 0 && TrailDrawer != null)
            {
                GhostTrail clone = new GhostTrail(positions, TrailDrawer, 0.25f, null, "Primitive_TaperedTextureMap", delegate (Effect effect, float fading)
                {
                    effect.Parameters["time"].SetValue(0f);
                    effect.Parameters["fadeDistance"].SetValue(0.3f);
                    effect.Parameters["fadePower"].SetValue(1 / 6f);
                    effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                });

                clone.ShrinkTrailLenght = true;
                clone.Pixelated = true;
                clone.DrawLayer = DrawhookLayer.BehindTiles;
                GhostTrailsHandler.LogNewTrail(clone);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class CrackshotCoin : ModProjectile
    {
        public override string Texture => AssetDirectory.DesertItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Coin");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            Main.projFrames[Projectile.type] = 8;
        }

        public static int Lifetime = 950;
        public float LifetimeCompletion => MathHelper.Clamp((Lifetime - Projectile.timeLeft) / (float)Lifetime, 0f, 1f);
        public float FadePercent => Math.Clamp(Projectile.timeLeft / FadeTime, 0f, 1f);
        public static float FadeTime => 30f;
        public Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Ranged;

            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.scale = 1.1f;
        }

        public bool playedSound;

        public override void AI()
        {
            if (!playedSound)
            {
                SoundEngine.PlaySound(CrackshotColt.BlingSound, Projectile.Center);
                playedSound = true;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 8)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Projectile.type])
            {
                Projectile.frame = 0;
            }

            Lighting.AddLight(Projectile.Center, Color.Goldenrod.ToVector3() * 0.2f);


            Projectile.rotation = Projectile.velocity.X * 0.5f;
            Projectile.velocity *= 0.998f;

            if (Projectile.timeLeft < Lifetime - 100)
                Projectile.velocity.Y += 0.01f;

            if (Projectile.Center.Distance(Owner.MountedCenter) > 1300 && Projectile.timeLeft > FadeTime)
                Projectile.timeLeft = (int)FadeTime;

            if (Main.rand.NextBool(10))
            {
                int schyste = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 246);
                Main.dust[schyste].noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            return base.OnTileCollide(oldVelocity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = new Rectangle(0, Projectile.frame * tex.Height / Main.projFrames[Projectile.type], tex.Width, tex.Height / Main.projFrames[Projectile.type]);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(lightColor) * FadePercent, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer && Main.rand.NextBool(4))
            {
                int drop = Item.NewItem(Projectile.GetSource_DropAsItem(), Projectile.Center, Vector2.One, ItemID.CopperCoin);

                if (Main.netMode == NetmodeID.MultiplayerClient && drop >= 0)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, drop, 1f);
            }
        }
    }

    [Serializable]
    public class SyncRicoshotPacket : Module
    {
        public Vector2 newVelocity;
        public Vector2 impactPosition;
        public int bulletIdentity;
        public int coinIdentity;
        public int sender;

        public SyncRicoshotPacket(Projectile bullet, Projectile coin)
        {
            sender = Main.myPlayer;
            bulletIdentity = bullet.identity;
            coinIdentity = coin.identity;
            impactPosition = coin.Center;
            newVelocity = bullet.velocity;
        }

        protected override void Receive()
        {
            //Despawn the coin
            Projectile coin = Main.projectile.FirstOrDefault(p => p.identity == coinIdentity);
            if (coin != null)
                coin.active = false;

            Projectile bullet = Main.projectile.FirstOrDefault(p => p.identity == bulletIdentity);
            if (bullet != null)
                bullet.velocity = newVelocity;

            if (Main.netMode == NetmodeID.Server)
                Send(-1, sender, false);
            else if (bullet.ModProjectile != null && bullet.ModProjectile is CrackshotBlast blast)
            {
                //Corrects the trail to make it so that the trail matches the ricochet perfectly
                Vector2 closestPosToRicochet = bullet.oldPos.OrderBy(v => v.DistanceSQ(impactPosition)).FirstOrDefault();
                if (closestPosToRicochet != Vector2.Zero)
                {
                    int ricochetCorrectionIndex = 0;
                    for (int i = 0; i < bullet.oldPos.Length; i++)
                    {
                        if (bullet.oldPos[i] == closestPosToRicochet)
                        {
                            bullet.oldPos[i] = impactPosition;
                            ricochetCorrectionIndex = i;
                            break;
                        }
                    }

                    //Straighten the rest of the trail
                    for (int i = 1; i < ricochetCorrectionIndex; i++)
                    {
                        bullet.oldPos[i] = Vector2.Lerp(bullet.oldPos[0], impactPosition, i / (float)ricochetCorrectionIndex);
                    }
                }
            }
        }
    }
}
