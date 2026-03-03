using Terraria.Localization;
using static Microsoft.Xna.Framework.Input.Keys;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    [ReplacingCalamity("MetalMonstrosity")]
    public class SpikedBall : ModItem
    {
        public static readonly SoundStyle BreakSound = new("CalamityFables/Sounds/SpikedBallBreak");
        public static readonly SoundStyle ThrowSound = new("CalamityFables/Sounds/SpikedBallThrow");
        public static readonly SoundStyle UseSound = new("CalamityFables/Sounds/SpikedBallUse");
        public static readonly SoundStyle FullChargeSound = new("CalamityFables/Sounds/SpikedBallFullCharge");

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public static float SECONDSTOFULLYCHARGE = 1.5f;
        public static float MINIMUMCHARGEFORTHROW = 0.15f;
        public static float DEBRISDAMAGEMULTIPLIER = 0.2f;

        public static int MINIMUMDEBRIS = 5;
        public static int MAXIMUMDEBRIS = 18;

        public static LocalizedText ComicallySlowText;
        public static LocalizedText CantCritText;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spiked Ball");
            Tooltip.SetDefault("Hold SHIFT to throw the ball with a ballistic trajectory");
            Item.ResearchUnlockCount = 1;

            ComicallySlowText = Mod.GetLocalization("Extras.ItemTooltipExtras.ComicallySlowSpeed");
            CantCritText = Mod.GetLocalization("Extras.ItemTooltipExtras.CannotCrit");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = Item.useTime = 60;
            Item.reuseDelay = 60;
            Item.knockBack = 1.5f;
            Item.width = 32;
            Item.height = 32;
            Item.damage = 82;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<SpikedBallProjectile>();
            Item.shootSpeed = 20f;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 3);
            Item.DamageType = DamageClass.Ranged;
            Item.channel = true;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = UseSound with { Volume = 0.7f };
        }


        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SpikyBall, 100).
                AddIngredient(ItemID.Spike, 50).
                AddIngredient(ItemID.Bone, 5).
                AddTile(TileID.Anvils).
                Register();
        }

        public override void UseItemFrame(Player player)
        {
            if (player.channel)
            {
                player.bodyFrame = new Rectangle(0, 56 * 5, 40, 56);
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.FirstOrDefault(tooltip => tooltip.Name == "Speed" && tooltip.Mod == "Terraria").Text = ComicallySlowText.Value;
            tooltips.FirstOrDefault(n => n.Name == "CritChance").Text = CantCritText.Value;
        }
    }

    public class SpikedBallProjectile : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "SpikedBall";

        public static bool SawFunnyScreenshake = false;

        private enum AIState
        {
            HeldOut,
            Thrown
        }

        private AIState CurrentAIState {
            get => (AIState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }
        public ref float Charge => ref Projectile.ai[1];

        public ref float FallThroughHeight => ref Projectile.ai[2];

        public Player Owner => Main.player[Projectile.owner];

        float blinkTimer = 0;
        bool playedThrowSound = false;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spiked Ball");
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.rotation = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver2);
            Projectile.CritChance = 0;
        }

        public override bool ShouldUpdatePosition() => CurrentAIState == AIState.Thrown;

        public override void AI()
        {
            switch (CurrentAIState)
            {
                case AIState.HeldOut:
                    {
                        // Kill the projectile if the player dies or gets crowd controlled
                        if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Vector2.Distance(Projectile.Center, Owner.Center) > 900f)
                        {
                            Projectile.Kill();
                            return;
                        }
                        if (Main.myPlayer == Projectile.owner && Main.mapFullscreen)
                        {
                            Projectile.Kill();
                            return;
                        }

                        FablesUtils.GenericManipulatePlayerVariablesForHoldouts(Projectile, 2, doItemRotation: false);

                        //Held up above the player's head. 42 is player.height, but we dont use it becausze it changes with mounts
                        Projectile.Center = Owner.MountedCenter - Vector2.UnitY * (42 + Projectile.height) * 0.5f * Owner.gravDir;
                        Projectile.velocity = Vector2.Zero;
                        Owner.direction = (Owner.MouseWorld().X - Owner.Center.X).NonZeroSign();

                        if (Charge < 1)
                        {
                            int useTime = Owner.GetHeldItemUseTime() ?? 60;
                            Charge += 1 / (useTime * SpikedBall.SECONDSTOFULLYCHARGE);
                            Projectile.scale = 0.2f + 0.8f * (float)Math.Pow(Charge, 0.1f);

                            if (Main.rand.NextBool(8 - (int)(6 * Charge)))
                            {
                                Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2CircularEdge(Projectile.width / 2, Projectile.height / 2) * 2.9f;
                                Vector2 dustVelocity = (dustPosition).DirectionTo(Projectile.Center).RotatedBy(0.2f) * 3f;

                                Dust deez = Dust.NewDustPerfect(dustPosition, 43, dustVelocity + Owner.velocity, 140, Color.Goldenrod, 0.5f);
                                deez.noGravity = true;
                            }

                            if (Charge >= 1)
                            {
                                for (int k = 0; k <= 30; k++)
                                {
                                    Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2CircularEdge(Projectile.width / 2, Projectile.height / 2);
                                    Vector2 dustVelocity = (Projectile.Center + Vector2.UnitY * Projectile.height).DirectionTo(dustPosition) * 3f;

                                    Dust d = Dust.NewDustPerfect(dustPosition, 36, dustVelocity, 70, default, 1.5f);
                                    d.noGravity = true;
                                }

                                if (Main.myPlayer == Projectile.owner)
                                    SoundEngine.PlaySound(SpikedBall.FullChargeSound, Owner.Center);
                                blinkTimer = 1f;
                                Charge = 1f;
                                Projectile.scale = 1f;
                                Projectile.CritChance = 0;
                            }
                        }

                        if (!Owner.channel && Charge > SpikedBall.MINIMUMCHARGEFORTHROW) //throw if enough charge
                        {
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Item heldItem = Owner.HeldItem;
                                float shootSpeed = heldItem.shootSpeed;
                                Projectile.velocity = (Main.MouseWorld - Projectile.Center).SafeNormalize(-Vector2.UnitY) * Charge * shootSpeed;

                                if (Main.keyState.IsKeyDown(LeftShift))
                                {
                                    Projectile.velocity = FablesUtils.GetArcVel(Projectile.Center, Main.MouseWorld, 0.4f, 100, maxXvel: 15f * Charge);
                                }

                                FallThroughHeight = Main.MouseWorld.Y;
                                Projectile.scale = 1f;
                                Projectile.tileCollide = true;
                                Projectile.ignoreWater = false;
                                CurrentAIState = AIState.Thrown;
                                Projectile.timeLeft = 60 * 20;

                                if (Main.netMode == NetmodeID.MultiplayerClient)
                                    new SpikedBallThrowPacket(Projectile).Send(-1, -1, false);
                            }
                        }
                        break;
                    }

                case AIState.Thrown:
                    {
                        if (!playedThrowSound)
                        {
                            SoundEngine.PlaySound(SpikedBall.ThrowSound, Projectile.Center);
                            playedThrowSound = true;
                        }


                        Projectile.rotation += Projectile.velocity.X.NonZeroSign() * Projectile.velocity.Length() * 0.01f;
                        Projectile.velocity.Y += 0.4f;
                        break;
                    }
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            if (Projectile.Bottom.Y >= FallThroughHeight)
                fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            FuckingExplode();
            return base.OnTileCollide(oldVelocity);
        }

        public void FuckingExplode()
        {
            //Massively boosted screenshake the first time
            float shake = SawFunnyScreenshake ? 10 : 40;
            CameraManager.Shake += (5 + shake * Charge) * Utils.GetLerpValue(1500f, 700f, Main.LocalPlayer.Distance(Projectile.Center), true);
            SawFunnyScreenshake = true;

            SoundEngine.PlaySound(SpikedBall.BreakSound with { Volume = 0.3f + 0.5f * Charge }, Projectile.Center);


            for (int k = 0; k <= 30 * Charge; k++)
            {
                Vector2 dustPosition = Projectile.Bottom + Main.rand.NextFloatDirection() * Projectile.width * 0.8f * Vector2.UnitX - Vector2.UnitY * Main.rand.NextFloat(0f, 10f);
                Vector2 dustVelocity = (Projectile.Bottom + Vector2.UnitY * 30f).DirectionTo(dustPosition) * Main.rand.NextFloat(0.4f, 4f);

                Dust.NewDustPerfect(dustPosition, 146, dustVelocity, 0, default, Main.rand.NextFloat(0.8f, 1.1f));
            }

            if (Main.myPlayer != Projectile.owner)
                return;

            int debrisCount = SpikedBall.MINIMUMDEBRIS + (int)((SpikedBall.MAXIMUMDEBRIS - SpikedBall.MINIMUMDEBRIS) * Utils.GetLerpValue(SpikedBall.MINIMUMCHARGEFORTHROW, 1, Charge, true));

            int debrisType = ModContent.ProjectileType<SpikedBallFragment>();
            for (int i = 0; i < debrisCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 0.7f) * Main.rand.NextFloat(4f, 8f), debrisType, (int)(Projectile.damage * SpikedBall.DEBRISDAMAGEMULTIPLIER), 0, Projectile.owner, ai2: Main.MouseWorld.Y);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= (float)Math.Pow(Charge, 0.8f);
            modifiers.DisableCrit();
        }

        public override bool? CanDamage()
        {
            //Can't damage when held out
            if (CurrentAIState == AIState.HeldOut)
                return false;
            return base.CanDamage();
        }

        // PreDraw is used to draw a chain and trail before the projectile is drawn normally.
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 position = Projectile.Center;
            if (CurrentAIState == AIState.HeldOut)
                position += Owner.gfxOffY * Vector2.UnitY;

            Main.EntitySpriteDraw(tex, position - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);

            if (CurrentAIState == AIState.HeldOut && blinkTimer > 0)
            {
                blinkTimer -= 1 / (60f * 0.4f);
                tex = ModContent.Request<Texture2D>(Texture + "Silouette").Value;
                Color blinkColor = (Color.Goldenrod with { A = 120 }) * (float)Math.Pow(blinkTimer, 0.7f);

                Main.EntitySpriteDraw(tex, position - Main.screenPosition, null, blinkColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            }

            return false;
        }
    }

    [Serializable]
    public class SpikedBallThrowPacket : Module
    {
        byte whoAmI;
        int projectileIdentity;
        float fallThroughHeight;
        Vector2 velocity;

        public SpikedBallThrowPacket(Projectile proj)
        {
            whoAmI = (byte)proj.owner;
            projectileIdentity = proj.identity;
            fallThroughHeight = proj.ai[2];
            velocity = proj.velocity;
        }

        protected override void Receive()
        {
            Projectile proj = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].owner == whoAmI && Main.projectile[i].identity == projectileIdentity)
                {
                    proj = Main.projectile[i];
                    break;
                }
            }

            if (proj == null)
                return;

            proj.velocity = velocity;
            proj.ai[0] = 1;
            proj.ai[2] = fallThroughHeight;
            proj.scale = 1f;
            proj.tileCollide = true;
            proj.ignoreWater = false;
            proj.timeLeft = 60 * 20;

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }

    public class SpikedBallFragment : Caltrop
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shrapnel");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 60 * 10;
            Projectile.knockBack = 0;
        }

        public override void PostAI()
        {
            Projectile.ai[1]++;
        }

        public override bool? CanDamage()
        {
            if (Projectile.ai[1] < 10)
                return false;

            return base.CanDamage();
        }


        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DisableCrit();
        }
    }
}