namespace CalamityFables.Content.Items.Marnite
{
    [ReplacingCalamity("MarniteDeconstructor")]
    public class MarniteDeconstructor : ModItem
    {
        public override string Texture => AssetDirectory.Marnite + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Marnite Deconstructor");
            Tooltip.SetDefault("Its engine generates hardlight rays capable of excavating walls from a long distance\n" + "Ignores 10 points of enemy Defense");
        }

        public override void SetDefaults()
        {
            Item.damage = 6;
            Item.ArmorPenetration = 10;
            Item.knockBack = 0f;
            Item.useTime = 7;
            Item.useAnimation = 25;
            Item.hammer = 40;

            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.width = 36;
            Item.height = 18;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item23;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MarniteDeconstructorProj>();
            Item.shootSpeed = 40f;
            Item.tileBoost = 7;
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Sapphire).
                AddRecipeGroup("AnyGoldBar", 3).
                AddIngredient(ItemID.Granite, 5).
                AddIngredient(ItemID.Marble, 5).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class MarniteDeconstructorProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Marnite Deconstructor");
        }

        public override string Texture => AssetDirectory.Marnite + "MarniteDeconstructor";
        public static Asset<Texture2D> GlowmaskTex;
        public static Asset<Texture2D> SelectionTex;

        public Player Owner => Main.player[Projectile.owner];
        public ref float MoveInIntervals => ref Projectile.localAI[0];
        public ref float SpeenBeams => ref Projectile.localAI[1];
        public ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            SpeenBeams += Timer > 140 ? 1 : 1 + 2f * (float)Math.Pow(1 - Timer / 140f, 2);


            if (Projectile.soundDelay <= 0)
            {
                SoundEngine.PlaySound(MarniteObliterator.UseSound, Projectile.Center);
                Projectile.soundDelay = 23;
            }

            if ((Owner.Center - Projectile.Center).Length() >= 5f)
            {
                if ((Owner.MountedCenter - Projectile.Center).Length() >= 30f)
                {
                    DelegateMethods.v3_1 = Color.Blue.ToVector3() * 0.5f;
                    Utils.PlotTileLine(Owner.MountedCenter + Owner.MountedCenter.DirectionTo(Projectile.Center) * 30f, Projectile.Center, 8f, DelegateMethods.CastLightOpen);
                }

                Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 0.7f);
            }

            if (MoveInIntervals > 0f)
                MoveInIntervals -= 1f;

            if (!Owner.channel || Owner.noItems || Owner.CCed)
                Projectile.Kill();

            else if (MoveInIntervals <= 0f)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    Vector2 newVelocity = Owner.MouseWorld() - Owner.MountedCenter;

                    if (Main.tile[Player.tileTargetX, Player.tileTargetY].WallType != 0)
                    {
                        newVelocity = new Vector2(Player.tileTargetX, Player.tileTargetY) * 16f + Vector2.One * 8f - Owner.MountedCenter;
                        MoveInIntervals = 2f;
                    }

                    newVelocity = Vector2.Lerp(newVelocity, Projectile.velocity, 0.7f);
                    if (float.IsNaN(newVelocity.X) || float.IsNaN(newVelocity.Y))
                        newVelocity = -Vector2.UnitY;

                    if (newVelocity.Length() < 50f)
                        newVelocity = newVelocity.SafeNormalize(-Vector2.UnitY) * 50f;

                    //Tile reach is a fucking square in terraria. Can you believe that?
                    int tileBoost = Owner.inventory[Owner.selectedItem].tileBoost;
                    int fullRangeX = (Player.tileRangeX + tileBoost - 1) * 16 + 11; //Why are those 2 separate variables? Whatever
                    int fullRangeY = (Player.tileRangeY + tileBoost - 1) * 16 + 11;

                    newVelocity.X = Math.Clamp(newVelocity.X, -fullRangeX, fullRangeX);
                    newVelocity.Y = Math.Clamp(newVelocity.Y, -fullRangeY, fullRangeY);

                    if (newVelocity != Projectile.velocity)
                        Projectile.netUpdate = true;

                    Projectile.velocity = newVelocity;
                }
            }

            //Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
            Owner.heldProj = Projectile.whoAmI;
            Owner.ChangeDir(Math.Sign(Projectile.velocity.X));
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() * Owner.gravDir - MathHelper.PiOver2);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() * Owner.gravDir - MathHelper.PiOver2 - MathHelper.PiOver4 * 0.5f * Owner.direction);

            Owner.SetDummyItemTime(2);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center = Owner.MountedCenter + Projectile.velocity;
        }

        public void DrawBeam(Texture2D beamTex, Vector2 direction, float beamProgress)
        {
            Vector2 startPos = Owner.MountedCenter + direction * 38f + direction.RotatedBy(MathHelper.PiOver2) * (float)Math.Cos(MathHelper.TwoPi * beamProgress + SpeenBeams * 0.06f) * 8f;

            float squareHeight = (beamProgress + SpeenBeams * 0.02f) % 1;
            if (squareHeight < 0.25)
                squareHeight = 0;
            else if (squareHeight < 0.5)
                squareHeight = (squareHeight - 0.25f) / 0.25f;
            else if (squareHeight < 0.75)
                squareHeight = 1;
            else
                squareHeight = 1 - (squareHeight - 0.75f) / 0.25f;

            float squareWidth = (beamProgress + SpeenBeams * 0.02f) % 1;
            if (squareWidth < 0.25)
                squareWidth = squareWidth / 0.25f;
            else if (squareWidth < 0.5)
                squareWidth = 1;
            else if (squareWidth < 0.75)
                squareWidth = 1 - (squareWidth - 0.5f) / 0.25f;
            else
                squareWidth = 0;

            Vector2 endPos = Projectile.Center + new Vector2(squareWidth * 15.5f, squareHeight * 15.5f) - Vector2.One * 7.75f;

            float rotation = (endPos - startPos).ToRotation();
            Vector2 beamOrigin = new Vector2(beamTex.Width / 2f, beamTex.Height);
            Vector2 beamScale = new Vector2(5.4f, (startPos - endPos).Length() / (float)beamTex.Height);

            FablesUtils.DrawChromaticAberration(direction.RotatedBy(MathHelper.PiOver2), 1f, delegate (Vector2 offset, Color colorMod) {
                Color beamColor = Color.Lerp(Color.Blue, Color.Goldenrod, 0.5f + 0.5f * (float)Math.Sin(SpeenBeams * 0.2f));
                beamColor *= 0.54f;
                beamColor = beamColor.MultiplyRGB(colorMod);
                beamColor.A = 0;

                Main.EntitySpriteDraw(beamTex, startPos + offset - Main.screenPosition, null, beamColor, rotation + MathHelper.PiOver2, beamOrigin, beamScale, SpriteEffects.None, 0);

                beamScale.X = 2.4f;
                beamColor = Color.Lerp(Color.DeepSkyBlue, Color.Chocolate, 0.5f + 0.5f * (float)Math.Sin(SpeenBeams * 0.2f + 1.2f));
                beamColor = beamColor.MultiplyRGB(colorMod);
                beamColor.A = 0;

                Main.EntitySpriteDraw(beamTex, startPos + offset - Main.screenPosition, null, beamColor, rotation + MathHelper.PiOver2, beamOrigin, beamScale, SpriteEffects.None, 0);
            });
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Projectile.active)
                return false;

            Vector2 normalizedVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero);

            //Draw the holdout.
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(9f, tex.Height / 2f);
            SpriteEffects effect = SpriteEffects.None;
            if (Owner.direction * Owner.gravDir < 0)
                effect = SpriteEffects.FlipVertically;
            Main.EntitySpriteDraw(tex, Owner.MountedCenter + normalizedVelocity * 10f - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effect, 0);

            //Draw some bloom over the tool
            if (GlowmaskTex == null)
                GlowmaskTex = ModContent.Request<Texture2D>(Texture + "Bloom");
            Texture2D glowTex = GlowmaskTex.Value;
            float bloomOpacity = (float)Math.Pow(Math.Clamp(Timer / 100f, 0f, 1f), 2) * (0.85f + (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly))) * 0.8f;
            Color bloomColor = Color.Lerp(Color.DeepSkyBlue, Color.Chocolate, 0.5f + 0.5f * (float)Math.Sin(SpeenBeams * 0.2f + 1.2f));
            bloomColor.A = 0;
            Main.EntitySpriteDraw(glowTex, Owner.MountedCenter + normalizedVelocity * 10f - Main.screenPosition, null, bloomColor * bloomOpacity, Projectile.rotation, origin, Projectile.scale, effect, 0);

            //Add some bloom around the projectile's position
            Texture2D bloomTex = AssetDirectory.CommonTextures.BloomCircle.Value;
            Main.EntitySpriteDraw(bloomTex, Projectile.Center - Main.screenPosition, null, (Color.DeepSkyBlue * 0.3f) with { A = 0 }, MathHelper.PiOver2, bloomTex.Size() / 2f, 0.3f * Projectile.scale, SpriteEffects.None, 0);

            //Draw a square selection
            if (SelectionTex == null)
                SelectionTex = ModContent.Request<Texture2D>(Texture + "Selection");
            Texture2D selectionTex = SelectionTex.Value;
            FablesUtils.DrawChromaticAberration(Vector2.UnitX, 2f, delegate (Vector2 offset, Color colorMod) {
                Main.EntitySpriteDraw(selectionTex, Projectile.Center + offset - Main.screenPosition, null, bloomColor.MultiplyRGB(colorMod) with { A = 0 }, 0f, selectionTex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            });


            //Draw laser beams going around the selection
            Texture2D beamTex = ModContent.Request<Texture2D>(AssetDirectory.UpwardsGradient).Value;

            for (int i = 0; i < 2; i++)
            {
                DrawBeam(beamTex, normalizedVelocity, i / 2f);
            }
            return false;
        }
    }
}
