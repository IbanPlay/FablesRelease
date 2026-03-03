using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    [AutoloadEquip(EquipType.Waist)]
    [ReplacingCalamity("FungalClump")]
    public class LuminousMixture : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;
        public const float SURGE_BUFF_TIME = 8;

        public readonly static SoundStyle BuffSound = new SoundStyle(SoundDirectory.CrabulonDrops + "LuminousMixtureActivate");
        public readonly static SoundStyle TurnoffSound = new SoundStyle(SoundDirectory.CrabulonDrops + "LuminousMixtureDeactivate");
        public readonly static SoundStyle LoopSound = new SoundStyle(SoundDirectory.CrabulonDrops + "LuminousMixtureActiveLoop") { IsLooped = true };

        public static int GlowDyeID;

        public override void Load()
        {
            On_Player.ApplyPotionDelay += GiveLuminousMixtureBuff;
            FablesPlayer.ModifyDrawInfoEvent += ApplyBlinkEffect;
        }

        private void ApplyBlinkEffect(Player player, ref PlayerDrawSet drawInfo)
        {
            if (!player.HasBuff<FungalSurge>())
                return;

            FablesUtils.OverrideAllDyes(ref drawInfo, GlowDyeID);
        }

        private void GiveLuminousMixtureBuff(On_Player.orig_ApplyPotionDelay orig, Player self, Item sItem)
        {
            orig(self, sItem);

            if (!self.GetPlayerFlag(Name))
                return;

            self.AddBuff(ModContent.BuffType<FungalSurge>(), (int)(SURGE_BUFF_TIME * 60));

            //DEBUG
            //self.ClearBuff(BuffID.PotionSickness);
            //self.potionDelay = 0;

            //Run the vfx through the packet
            new FungalSurgeActivationVFXPacket().Send();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Luminous Mixture");
            Tooltip.SetDefault("Grants a short surge of power when using a healing item");
            //);

            if (!Main.dedServ)
            {
                var shader = ModContent.Request<Effect>("CalamityFables/Effects/Dye/LuminousSporeDye", AssetRequestMode.ImmediateLoad);
                GameShaders.Armor.BindShader(Type, new FungalSurgeArmorShaderData(shader, "LuminousSporeDyePass"))
                    .UseColor(CommonColors.MushroomDeepBlue)
                    .UseImage("Images/Misc/noise");

                GlowDyeID = GameShaders.Armor.GetShaderIdFromItemId(Type);
            }
        }

        public override void SetDefaults()
        {
            //Have to manually set it to 0 because we register a dye under its name
            Item.dye = 0;
            Item.expert = true;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
            Item.width = 32;
            Item.height = 32;
            Item.waistSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Waist);
            Item.maxStack = 1; //Counteract the dye thing
        }

        public override void UpdateEquip(Player player)
        {
            player.SetPlayerFlag(Name);
        }

        public override void EquipFrameEffects(Player player, EquipType type)
        {
            player.cWaist = SporeDye.DyeID;
        }
    }

    public class FungalSurge : ModBuff
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fungal Surge");
            Description.SetDefault("Major improvements to all stats");
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 8;
            player.moveSpeed += 0.3f;
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.GetAttackSpeed(DamageClass.Generic) += 0.2f;

            Lighting.AddLight(player.Center, TorchID.Mushroom);

            if (Main.rand.NextBool(6))
            {
                Vector2 randomDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                int dustType = Main.rand.NextBool() ? DustID.MushroomSpray : DustID.MushroomTorch;

                Dust d = Dust.NewDustPerfect(player.Center + randomDirection * Main.rand.NextFloat(9f, 40f), dustType, randomDirection * Main.rand.NextFloat(0.1f, 2f), 220, Scale: Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;

                if (!Main.rand.NextBool(4))
                {
                    d.velocity.X = 0;
                    d.velocity.Y = Math.Max(-9, d.velocity.Y);
                }

            }

            if (player.buffTime[buffIndex] == 1)
                SoundEngine.PlaySound(LuminousMixture.TurnoffSound, player.Center);
        }
    }

    [Serializable]
    public class FungalSurgeActivationVFXPacket : Module
    {
        byte whoAmI;

        public FungalSurgeActivationVFXPacket()
        {
            whoAmI = (byte)Main.myPlayer;
        }

        protected override void Receive() 
        {
            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, whoAmI, false);
                return;
            }

            Player player = Main.player[whoAmI];

            SoundEngine.PlaySound(LuminousMixture.BuffSound, player.Center);
            SoundEngine.PlaySound(LuminousMixture.LoopSound with { Volume = 0.4f }, player.Center, delegate (ActiveSound s)
            {
                s.Position = player.Center;
                if (Main.gameMenu || player.dead || !player.active || !player.HasBuff<FungalSurge>())
                    return false;

                return true;
            });

            ParticleHandler.SpawnParticle(new CircularPulseShine(player.Center, Color.RoyalBlue));

            for (int i = 0; i < 20; i++)
            {
                Vector2 randomDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust d = Dust.NewDustPerfect(player.Center + randomDirection * Main.rand.NextFloat(9f, 20f), DustID.GlowingMushroom, randomDirection * Main.rand.NextFloat(1f, 9f));
            }

            for (int i = 0; i < 13; i++)
            {
                Vector2 randomDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust d = Dust.NewDustPerfect(player.Center + randomDirection * Main.rand.NextFloat(9f, 40f), ModContent.DustType<SporeBudDust>(), randomDirection * Main.rand.NextFloat(0.1f, 2f), Main.rand.Next(40), Scale: Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 smokeCenter = player.Center + gushDirection * 22f * Main.rand.NextFloat(0.2f, 0.55f);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f);
                Particle smoke = new SporeGas(smokeCenter, velocity, player.Center, 33f, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }
        }
    }

    public class FungalSurgeArmorShaderData : ArmorShaderData
    {
        public FungalSurgeArmorShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName) { }

        public override void Apply(Entity entity, DrawData? drawData)
        {
            Player player = entity as Player;
            if (player == null || !player.HasBuff<FungalSurge>())
            {
                base.Apply(entity, drawData);
                UseSaturation(2f);
                UseOpacity(0f);
                return;
            }

            int fungalSurgeType = ModContent.BuffType<FungalSurge>();
            float effectCompletion = 1f;

            for (int i = 0; i < Player.MaxBuffs; i++)
                if (player.buffType[i] == fungalSurgeType)
                {
                    effectCompletion = Math.Min(player.buffTime[i] / (float)(LuminousMixture.SURGE_BUFF_TIME * 60), 1);
                    break;
                }

            if (effectCompletion > 0.95f)
                UseOpacity((effectCompletion - 0.95f) / 0.05f);
            else
                UseOpacity(0f);
            UseSaturation((float)Math.Pow(effectCompletion, 0.5f) + 1f);
            base.Apply(player, drawData);
        }
    }
}