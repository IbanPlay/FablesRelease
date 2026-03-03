using CalamityFables.Content.Items.CrabulonDrops;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using CalamityFables.Cooldowns;
using CalamityFables.Helpers;
using CalamityFables.Particles;
using System.Xml;
using CalamityFables.Content.Items.Food;

namespace CalamityFables.Content.Items.BurntDesert
{
    [ReplacingCalamity("StormlionMandible", "StormjawStaff")]
    public class Electrocells : ModItem
    {
        public static readonly SoundStyle DischargeSound = new(SoundDirectory.Sounds + "ElectrocellsDischarge") { Volume = 0.6f };
        public override string Texture => AssetDirectory.DesertItems + Name;
        public static Asset<Texture2D> OutlineTex;

        public static int DyeID;

        public static int MinDamageToActivate = 50; //Damage treshold for the electrocells to activate
        public static float reflectedDamagePercent = 1f; //percentage of the damage reflected 
        public static int MaxRange = 500;  //Max range for damage reflection
        public static int MaxTargets = 5;  //Max targets that can get the reflection applied to them
        public static float Cooldown = 2f; //Cooldown between discharges

        public override void Load()
        {
            FablesPlayer.ModifyDrawInfoEvent += ApplyZapEffect;
            FablesPlayer.OnHurtEvent += ElectrocuteOnHit;
        }

        private void ElectrocuteOnHit(Player player, Player.HurtInfo info)
        {
            if (info.Damage < MinDamageToActivate)
                return;

            if (player.HasCooldown("Electrocells"))
                return;

            int electroBuffIndex = player.FindBuffIndex(ModContent.BuffType<ElectrifiedBloodBuff>());

            if (electroBuffIndex != -1 || player.HasItem(Type))
            {
                bool foundNearbyEnemy = false;
                var source = Item.GetSource_FromThis();
                int reflectedDamage = (int)(info.Damage * reflectedDamagePercent);

                List<NPC> nearbyEnemies = new List<NPC>();

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.CanBeChasedBy(player) && n.WithinRange(player.Center, MaxRange) && (n.noTileCollide || Collision.CanHitLine(player.Center, 1, 1, n.Center, 1, 1)))
                    {
                        foundNearbyEnemy = true;
                        nearbyEnemies.Add(n);

                        //no need to check every npc if were not the player spawning projectiles
                        if (Main.myPlayer != player.whoAmI)
                            break;
                    }
                }

                if (foundNearbyEnemy)
                {
                    if (Main.myPlayer == player.whoAmI)
                    {
                        int targetCount = 0;

                        //Descending so we iterate over the closest npcs first
                        nearbyEnemies = nearbyEnemies.OrderByDescending(n => n.DistanceSQ(player.Center)).ToList();

                        List<int> realNPCs = new List<int>();
                        for (int i = nearbyEnemies.Count - 1; i >= 0; i--)
                        {
                            NPC n = nearbyEnemies[i];

                            if (realNPCs.Contains(n.realLife))
                            {
                                nearbyEnemies.RemoveAt(i);
                                continue;
                            }

                            if (n.realLife == -1)
                            {
                                realNPCs.Add(n.whoAmI);
                            }
                            else
                            {
                                realNPCs.Add(n.realLife);
                            }
                        }

                        //Reversing so we prioritize closer npcs
                        nearbyEnemies.Reverse();

                        foreach (NPC reflectTarget in nearbyEnemies)
                        {
                            if (targetCount >= MaxTargets)
                                break;
                            Projectile.NewProjectile(source, reflectTarget.Center, Vector2.Zero, ModContent.ProjectileType<ElectrocellDischarge>(), reflectedDamage, 6, Main.myPlayer, reflectTarget.whoAmI, Main.LocalPlayer.Center.X, Main.LocalPlayer.Center.Y);
                            targetCount++;
                        }
                    }

                    if (Main.myPlayer == player.whoAmI)
                        CameraManager.Quake += 10;

                    SoundEngine.PlaySound(DischargeSound, player.Center);

                    //Lower buff duration
                    if (electroBuffIndex != -1)
                    {
                        player.buffTime[electroBuffIndex] -= 60 * 60;
                        player.buffTime[electroBuffIndex] = Math.Max(1, player.buffTime[electroBuffIndex]);
                    }
                    //Consume item
                    else
                        player.ConsumeItem(Type);

                    player.AddCooldown("Electrocells", (int)(Cooldown * 60));
                }
            }
        }

        private void ApplyZapEffect(Player player, ref PlayerDrawSet drawInfo)
        {
            if (!player.FindCooldown("Electrocells", out var cd) || cd.duration - cd.timeLeft >= 40f)
                return;

            player.immuneAlpha = 0;
            float fadeProbability = Utils.GetLerpValue(0.5f, 1f, (cd.duration - cd.timeLeft) / 40f);

            if (Main.rand.NextFloat() > fadeProbability)
                FablesUtils.OverrideAllDyes(ref drawInfo, DyeID);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Electrocells");
            Tooltip.SetDefault("Upon taking at least 50 damage, one electrocell cluster is consumed, electrocuting nearby enemies\n" +
                "'The organic goo crackles and burns at the touch'");
            Item.ResearchUnlockCount = 10;

            //Load a dye that we use for drawing the player as electrified
            if (!Main.dedServ)
            {
                var shader = ModContent.Request<Effect>("CalamityFables/Effects/Dye/ElectricDye", AssetRequestMode.ImmediateLoad);
                GameShaders.Armor.BindShader(Type, new ElectrocellBurstShaderData(shader, "ElectricDyePass"))
                    .UseImage(AssetDirectory.NoiseTextures.RGB)
                    .UseColor(new Color(101, 241, 209))
                    .UseSecondaryColor(new Color(177, 237, 59));
                DyeID = GameShaders.Armor.GetShaderIdFromItemId(Type);
            }
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.maxStack = 9999;
            Item.dye = 0;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(0, 0, 2, 0);
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            OutlineTex ??= ModContent.Request<Texture2D>(Texture + "Outline");
            Color glowColor = Color.Lerp(new Color(255, 239, 99), Color.White, 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f));
            origin += Vector2.One * 2;
            spriteBatch.Draw(OutlineTex.Value, position, null, glowColor, 0f, origin, scale, 0, 0);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            OutlineTex ??= ModContent.Request<Texture2D>(Texture + "Outline");
            Color glowColor = Color.Lerp(new Color(255, 239, 99), Color.White, 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f));
            spriteBatch.Draw(OutlineTex.Value, Item.Center - Main.screenPosition, null, glowColor, rotation, OutlineTex.Size() / 2f, scale, 0, 0);
        }

        public override void AddRecipes()
        {
            Recipe.Create(ItemID.ThunderSpear)
                .AddIngredient(ItemID.Spear)
                .AddIngredient(Type, 8)
                .AddRecipeGroup(FablesRecipes.AnyGoldBarGroup, 10)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.ThunderStaff)
               .AddIngredient(ItemID.AmberStaff)
               .AddIngredient(Type, 8)
               .AddRecipeGroup(FablesRecipes.AnyGoldBarGroup, 5)
               .AddTile(TileID.Anvils)
               .Register();
        }
    }

    public class ElectrocellDischarge : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public int TargetNPCIndex => (int)Projectile.ai[0];

        public Vector2 SourcePosition => new Vector2(Projectile.ai[1], Projectile.ai[2]);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Unstable Discharge");
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 16000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.DamageType = DamageClass.Default;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }

        //only hits its target once
        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == TargetNPCIndex ? null : false;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft == 2)
                VisualEffects();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (target.Center.X - SourcePosition.X).NonZeroSign();
        }

        public void VisualEffects()
        {
            Particle zap = new ElectricArcPrim(Projectile.Center, SourcePosition, -Vector2.UnitY * 60f, 4f);
            ParticleHandler.SpawnParticle(zap);
        }
    }


    public class ElectrocellBurstShaderData : ElectricDyeArmorShaderData
    {
        public ElectrocellBurstShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName) { }

        public override void Apply(Entity entity, DrawData? drawData)
        {
            

            if (entity is Player player)
            {
                //Force initial zap
                if (player.FindCooldown("Electrocells", out var cooldown) && cooldown.duration == cooldown.timeLeft)
                {
                     ZapTime[player.whoAmI % 6] = 20;
                }

            }

            base.Apply(entity, drawData);
        }
    }
}