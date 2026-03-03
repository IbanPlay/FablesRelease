using CalamityFables.Particles;
using ReLogic.Utilities;
using Terraria.DataStructures;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumDiggingTurtle")]
    public class WulfrumDiggingTurtle : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Digging Turtle");
            Tooltip.SetDefault("Throws a rickety mining contraption to dig out a small tunnel\n" +
            "In case of an emergency, right click to instantly detonate all your digging turtles");
            Item.ResearchUnlockCount = 10;

            if (Main.dedServ)
                return;
            for (int i = 1; i < 4; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumTurtle" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            Item.useTime = Item.useAnimation = 8;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.shootSpeed = 20f;
            Item.shoot = ModContent.ProjectileType<WulfrumDiggingTurtleProjectile>();
            Item.width = 30;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(0, 0, 10, 0);
            Item.rare = ItemRarityID.Blue;
        }

        /*
		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
		{
			itemGroup = (ContentSamples.CreativeHelper.ItemGroup)CalamityResearchSorting.ToolsOther;
		}
        */

        public override bool AltFunctionUse(Player player) => true;

        public override bool ConsumeItem(Player player)
        {
            return player.altFunctionUse != 2;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Play a sound and detonate all owned turtles.
            if (player.altFunctionUse == 2)
            {
                bool explodedAny = false;

                for (int i = 0; i < Main.maxProjectiles; ++i)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.owner != player.whoAmI || p.type != Item.shoot)
                        continue;

                    p.ai[1] = 1f;
                    p.timeLeft = 1;
                    p.netUpdate = true;
                    p.netSpam = 0;

                    explodedAny = true;
                }

                if (explodedAny)
                    SoundEngine.PlaySound(SoundID.Item73, position);

                return false;
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe(3).
                AddIngredient(ItemID.Gel, 5). //Gel is a default combustible item to fuel the motors of the lil guys
                AddIngredient<WulfrumMetalScrap>(5).
                Register();
        }
    }


    public class WulfrumDiggingTurtleProjectile : ModProjectile
    {
        public static readonly SoundStyle IdleSound = new(SoundDirectory.Wulfrum + "WulfrumSawIdle") { IsLooped = true, Volume = 0.8f, MaxInstances = 0, PlayOnlyIfFocused = true };
        public static readonly SoundStyle CuttingSound = new(SoundDirectory.Wulfrum + "WulfrumSawCutting") { IsLooped = true, Volume = 0.7f, MaxInstances = 0, PlayOnlyIfFocused = true };
        public static readonly SoundStyle BreakingSound = new(SoundDirectory.Wulfrum + "WulfrumMachineBreak");

        private SlotId CuttingSoundSlot;
        private SlotId IdlingSoundSlot;

        public Player Owner => Main.player[Projectile.owner];
        public bool Diggging {
            get => Projectile.ai[0] == 1;
            set { Projectile.ai[0] = value ? 1f : 0f; }
        }

        public bool HasDug {
            get => Projectile.ai[1] == 1;
            set { Projectile.ai[1] = value ? 1f : 0f; }
        }

        public float CuttingVolume {
            get => Projectile.localAI[0];
            set { Projectile.localAI[0] = Math.Clamp(value, 0f, 1f); }
        }

        public override string Texture => AssetDirectory.WulfrumItems + "WulfrumDiggingTurtle";
        public static Texture2D SmallGearTexture;
        public static Texture2D GearTexture;

        public static int Lifetime = 400;
        public static int DigTime = 350;
        public static float DigSpeed = 1.5f;
        public static int MaxPickPower = 160;
        public static float ClearSpaceDiagonal = 50;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Digging Turtle");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!HasDug)
            {
                HasDug = true;
                Projectile.timeLeft = DigTime;
            }

            Diggging = true;
            Projectile.velocity = oldVelocity.SafeNormalize(Vector2.UnitY) * DigSpeed;

            return false;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.GreenYellow.ToVector3());

            //Idle chainsaw sounds
            if ((!SoundEngine.TryGetActiveSound(IdlingSoundSlot, out var idleSoundOut)))
                IdlingSoundSlot = SoundEngine.PlaySound(IdleSound with { Volume = IdleSound.Volume }, Projectile.Center);
            else if (idleSoundOut != null)
            {
                idleSoundOut.Volume = (1 - CuttingVolume);
                idleSoundOut.Position = Projectile.Center;
            }


            //Heavy cutting sound
            if ((!SoundEngine.TryGetActiveSound(CuttingSoundSlot, out var cuttingSoundOut)))
                CuttingSoundSlot = SoundEngine.PlaySound(CuttingSound with { Volume = CuttingSound.Volume }, Projectile.Center);
            else if (cuttingSoundOut != null)
            {
                cuttingSoundOut.Volume = CuttingVolume;
                cuttingSoundOut.Position = Projectile.Center;
            }

            SoundHandler.TrackSound(CuttingSoundSlot);
            SoundHandler.TrackSound(IdlingSoundSlot);

            if (Diggging)
            {
                CuttingVolume += 0.1f;
                for (int i = -1; i <= 1; i++)
                {
                    Point tilePos = (Projectile.Center + (Projectile.rotation + MathHelper.PiOver4 * i).ToRotationVector2() * 16).ToTileCoordinates();

                    DigTile(tilePos.X, tilePos.Y);
                }
            }

            //the flung state.
            else
            {
                CuttingVolume -= 0.1f;

                float fallSpeed = Projectile.velocity.Y;

                if (Projectile.timeLeft < 345)
                    Projectile.velocity += Vector2.UnitY * 0.5f * (1 - Math.Clamp((Projectile.timeLeft - 310f) / 35f, 0f, 1f));

                Projectile.velocity *= 0.98f;

                if (Projectile.velocity.Y > 0)
                    Projectile.velocity.Y = Math.Clamp(Projectile.velocity.Y, 0, Math.Max(18f, fallSpeed));

                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            if (!Collision.SolidCollision(Projectile.Center - Vector2.One * ClearSpaceDiagonal * 0.5f, (int)ClearSpaceDiagonal, (int)ClearSpaceDiagonal))
                Diggging = false;
        }

        public override void OnKill(int timeLeft)
        {
            /*
            if (Main.myPlayer == Owner.whoAmI)
            {
                if (Main.rand.NextBool() && !Projectile.noDropItem)
                    Item.NewItem(Projectile.GetSource_DropAsItem(), (int)Projectile.position.X, (int)Projectile.position.Y, Projectile.width, Projectile.height, ModContent.ItemType<WulfrumMetalScrap>(), 1);
            }
            */

            if (SoundEngine.TryGetActiveSound(CuttingSoundSlot, out var cuttingSoundOut))
                cuttingSoundOut.Stop();

            if (SoundEngine.TryGetActiveSound(IdlingSoundSlot, out var idleSoundOut))
                idleSoundOut.Stop();

            SoundEngine.PlaySound(BreakingSound, Projectile.position);

            int smokeCount = Main.rand.Next(5, 10);
            int sparkCount = Main.rand.Next(4, 8);

            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 12f);

                Color smokeStart = Main.rand.NextBool() ? Color.GreenYellow : Color.Aqua;
                Color smokeEnd = new Color(60, 60, 60);

                float smokeSize = Main.rand.NextFloat(1.4f, 2.2f);

                Particle smoke = new SmallSmokeParticle(Projectile.Center, velocity, smokeStart, smokeEnd, smokeSize, 135 - Main.rand.Next(30));
                ParticleHandler.SpawnParticle(smoke);
            }

            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 shrapnelVelocity = Main.rand.NextVector2Circular(9f, 9f);
                    float shrapnelScale = Main.rand.NextFloat(0.8f, 1f);

                    string goreType = i < 2 ? "WulfrumTurtle1" : i < 3 ? "WulfrumTurtle2" : "WulfrumTurtle3";

                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, shrapnelVelocity, Mod.Find<ModGore>(goreType).Type, shrapnelScale);
                }


            }

            for (int i = 0; i < sparkCount; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, 226, Main.rand.NextVector2Circular(18f, 18f), Scale: Main.rand.NextFloat(0.4f, 1f));
            }
        }

        public void DigTile(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile)
                return;

            int pickPower = Math.Min(Owner.GetBestPickPower(), MaxPickPower);
            int pickaxeRequirement = tile.GetRequiredPickPower(x, y);

            bool true_ = true;
            bool false_ = false;

            bool canBreakTileCheck = TileLoader.CanKillTile(x, y, tile.TileType, ref true_) && TileLoader.CanKillTile(x, y, tile.TileType, ref false_);
            bool shouldBreakTile = tile.ShouldBeMined();

            if (!Owner.noBuilding && shouldBreakTile && pickaxeRequirement < pickPower && canBreakTileCheck)
            {
                WorldGen.KillTile(x, y, false, false, false);
                if (!Main.tile[x, y].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y, 0f, 0, 0, 0);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            GearTexture = ModContent.Request<Texture2D>(AssetDirectory.WulfrumItems + "WulfrumDiggingTurtle_Gear").Value;

            SmallGearTexture = ModContent.Request<Texture2D>(AssetDirectory.WulfrumItems + "WulfrumDiggingTurtle_SmallGear").Value;


            Vector2 position = Projectile.Center - Main.screenPosition;
            if (Diggging)
                position += Main.rand.NextVector2Circular(2f, 2f);
            float drawRotation = Projectile.rotation + MathHelper.PiOver2;

            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 diggingGearOffset = new Vector2(9 * i, -11).RotatedBy(drawRotation) * Projectile.scale;
                Main.EntitySpriteDraw(SmallGearTexture, position + diggingGearOffset, null, lightColor, drawRotation + Main.GlobalTimeWrappedHourly * -10f * i, SmallGearTexture.Size() / 2f, Projectile.scale * 1.2f, 0, 0);
            }

            Vector2 largeGearOffset = new Vector2(0f, 3f).RotatedBy(drawRotation) * Projectile.scale;
            Main.EntitySpriteDraw(GearTexture, position + largeGearOffset, null, lightColor, Main.GlobalTimeWrappedHourly * 6f, GearTexture.Size() / 2f, Projectile.scale, 0, 0);

            Main.EntitySpriteDraw(texture, position, null, lightColor, drawRotation, texture.Size() / 2f, Projectile.scale, 0, 0);
            return false;
        }
    }
}
