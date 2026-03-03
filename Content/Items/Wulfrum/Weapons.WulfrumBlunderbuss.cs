using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumBlunderbuss")]
    public class WulfrumBlunderbuss : ModItem
    {
        public static readonly SoundStyle ShootSound = new(SoundDirectory.Wulfrum + "WulfrumBlunderbussFire") { Volume = 0.5f, PitchVariance = 0.1f };
        public static readonly SoundStyle ShootAndReloadSound = new(SoundDirectory.Wulfrum + "WulfrumBlunderbussFireAndReload") { PitchVariance = 0.1f };

        //public static float MinSpreadDistance = 460f;
        //public static float MaxSpreadDistance = 60f;
        //public static float MinSpread = 0.2f;
        public static float MaxSpread = 0.6f;
        //public static float MaxDamageFalloff = 0.8f;
        public static int BulletCount = 6;

        public static int ShotsPerScrap = 30;
        public int storedScrap = 0;

        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Blunderbuss");
            Tooltip.SetDefault("Consumes wulfrum scrap or silver coins to fire\n" +
                               //"The shot's spread can be diminished by aiming further away\n" +
                               "Ignores 3 points of enemy Defense\n" +
                               "[c/83B87E:\"The good thing about Wulfrum is that while it breaks fast, it can be reused even faster\"]\n" +
                               "[c/83B87E:\"In fact, it is quite common for a robot to be turned into ammo right after it stops functioning\"]");
            //Funny lore quip about how it can perform ecgologically be recyling the scrap parts of the faulty robots youre forced to shoot at as new ammo.
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 9;
            Item.ArmorPenetration = 3;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 23;
            Item.height = 8;
            Item.useTime = 55;
            Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = ShootSound;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<WulfrumScrapBullet>();
            Item.shootSpeed = 15f;
            Item.crit = 10;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();
        }

        public override bool CanUseItem(Player player) => storedScrap > 0 || (player.HasItem(ModContent.ItemType<WulfrumMetalScrap>()) || player.HasItem(ItemID.SilverCoin));

        public override void UseAnimation(Player player)
        {
            Item.UseSound = ShootSound;
            if (storedScrap == 1 && (player.HasItem(ModContent.ItemType<WulfrumMetalScrap>()) || player.HasItem(ItemID.SilverCoin)))
                Item.UseSound = ShootAndReloadSound;
        }

        public override bool? UseItem(Player player)
        {
            storedScrap--;

            if (storedScrap <= 0)
            {
                bool ammoConsumed = false;

                if (player.HasItem(ModContent.ItemType<WulfrumMetalScrap>()))
                {
                    player.ConsumeItem(ModContent.ItemType<WulfrumMetalScrap>());
                    ammoConsumed = true;
                }

                else if (player.HasItem(ItemID.SilverCoin))
                {
                    player.ConsumeItem(ItemID.SilverCoin);
                    ammoConsumed = true;
                }

                if (ammoConsumed)
                    storedScrap = ShotsPerScrap;
            }

            return base.UseItem(player);
        }

        /*
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            float aimLenght = (Main.MouseWorld - player.MountedCenter).Length();
            float damageMult = MathHelper.Lerp(1f, MaxDamageFalloff, Math.Clamp(aimLenght - MaxSpreadDistance, 0, MinSpreadDistance - MaxSpreadDistance) / (MinSpreadDistance - MaxSpreadDistance));
            damage = (int)(damage * damageMult);
        }
        */

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (CameraManager.Shake < 3f)
                CameraManager.Shake = 3f;

            float aimLenght = (Main.MouseWorld - player.MountedCenter).Length();
            //float spreadDistance = Math.Clamp(aimLenght - MaxSpreadDistance, 0, MinSpreadDistance - MaxSpreadDistance) / (MinSpreadDistance - MaxSpreadDistance);
            //float spread = MathHelper.Lerp(MaxSpread, MinSpread, spreadDistance);
            float spread = MaxSpread;

            Vector2 nuzzleDir = velocity.SafeNormalize(Vector2.Zero);


            for (int i = 0; i < BulletCount; i++)
            {
                Vector2 direction = nuzzleDir.RotatedByRandom(spread);
                Vector2 nuzzlePos = player.MountedCenter + direction * 15f;

                Projectile.NewProjectile(player.GetSource_ItemUse_WithPotentialAmmo(Item, Item.useAmmo), nuzzlePos, direction * Item.shootSpeed * Main.rand.NextFloat(1.5f, 2f), type, damage, (int)Item.knockBack, player.whoAmI, 0, 0);
            }

            return false;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;
            Vector2 itemSize = new Vector2(46, 16);
            Vector2 itemOrigin = new Vector2(-13, 3);

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

            //Reloads the gun 
            if (animProgress > 0.5f)
            {
                float backArmRotation = rotation + 0.52f * player.direction;

                Player.CompositeArmStretchAmount stretch = ((float)Math.Sin(MathHelper.Pi * (animProgress - 0.5f) / 0.36f)).ToStretchAmount();
                player.SetCompositeArmBack(true, stretch, backArmRotation);
            }
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float barScale = 1.2f;

            var barBG = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarBack").Value;
            var barFG = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarFront").Value;

            Vector2 drawPos = position + Vector2.UnitY * (frame.Height - 2 + 6f) * scale + Vector2.UnitX * (frame.Width - barBG.Width * barScale) * scale * 0.5f;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(storedScrap / (float)ShotsPerScrap * barFG.Width), barFG.Height);
            Color colorBG = Color.RoyalBlue;
            Color colorFG = Color.Lerp(Color.Teal, Color.YellowGreen, storedScrap / (float)ShotsPerScrap);

            spriteBatch.Draw(barBG, drawPos, null, colorBG, 0f, origin, scale * barScale, 0f, 0f);
            spriteBatch.Draw(barFG, drawPos, frameCrop, colorFG * 0.8f, 0f, origin, scale * barScale, 0f, 0f);

            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, storedScrap.ToString(), drawPos + new Vector2(-3, -3) * scale, Color.GreenYellow, Color.Black, scale);
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>(9).
                AddTile(TileID.Anvils).
                Register();
        }

        public override void OnCreated(ItemCreationContext context)
        {
            if (context is RecipeItemCreationContext)
                storedScrap = ShotsPerScrap;
        }

        #region saving the durability
        public override ModItem Clone(Item item)
        {
            ModItem clone = base.Clone(item);
            if (clone is WulfrumBlunderbuss a && item.ModItem is WulfrumBlunderbuss a2)
            {
                a.storedScrap = a2.storedScrap;
            }
            return clone;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["ammoStored"] = storedScrap;
        }

        public override void LoadData(TagCompound tag)
        {
            storedScrap = tag.GetInt("ammoStored");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(storedScrap);
        }

        public override void NetReceive(BinaryReader reader)
        {
            storedScrap = reader.ReadInt32();
        }
        #endregion
    }


    public class WulfrumScrapBullet : ModProjectile
    {
        internal PrimitiveTrail TrailDrawer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("High Speed Scrap");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override string Texture => AssetDirectory.Invisible;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 4;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.95f;
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

            return base.OnTileCollide(oldVelocity);
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Sqrt(Projectile.timeLeft / 100f);
            return Color.PaleGoldenrod * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio)
        {
            return completionRatio * 6.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;

            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 6f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition);

            return false;
        }
    }
}
