/*
using Terraria.Graphics.Light;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumBattery")]
    public class WulfrumBattery : ModItem
    {
        public static readonly SoundStyle ExtraDropSound = new(SoundDirectory.Wulfrum + "WulfrumExtraDrop") { PitchVariance = 0.3f };
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void Load()
        {
            FablesProjectile.AIEvent += FablesProjectile_AIEvent;
        }

        private void FablesProjectile_AIEvent(Projectile projectile)
        {
            if (!projectile.npcProj && !projectile.trap && projectile.minion && !ProjectileID.Sets.MinionShot[projectile.type] && Main.player[projectile.owner].GetPlayerFlag("WulfrumBattery"))
            {
                float lightMult = 2f;
                if (Lighting.Mode == LightMode.Retro || Lighting.Mode == LightMode.Trippy) //The light loks wayyy too bright in retro/trippy
                    lightMult *= 0.5f;

                Lighting.AddLight(projectile.Center, Color.DeepSkyBlue.ToVector3() * lightMult);


                if (Main.rand.NextBool(16))
                {
                    float size = (projectile.Hitbox.Size() / 2f).Length();

                    Dust zapDust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(1f, 1f) * size, 226, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1f, 2.3f));
                    zapDust.noGravity = true;
                }
            }
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Wulfrum Battery");
            Tooltip.SetDefault("Empowers your summons with wulfrum energy, letting them emit light around them\n" +
                "7% increased summon damage\n" +
                "50% chance to get an extra scrap when killing wulfrum robots\n" +
                "Can also be scrapped at an extractinator");

            ItemID.Sets.ExtractinatorMode[Item.type] = Item.type;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 22;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.accessory = true;
            Item.rare = ItemRarityID.Blue;

            //Needed for extractination
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 10;
            Item.useTime = 2;
            Item.consumable = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage<SummonDamageClass>() += 0.07f;
            player.SetPlayerFlag("WulfrumBattery");
        }

        //Scrappable for 3-6 wulfrum scrap or a 20% chance to get an energy core
        public override void ExtractinatorUse(int extractinatorBlockType, ref int resultType, ref int resultStack)
        {
            resultType = ModContent.ItemType<WulfrumMetalScrap>();
            resultStack = Main.rand.Next(3, 6);

            if (Main.rand.NextFloat() > 0.8f)
            {
                resultStack = 1;
                resultType = ModContent.ItemType<EnergyCore>();
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int consumableIndex = tooltips.FindIndex(x => x.Name == "Consumable" && x.Mod == "Terraria");
            if (consumableIndex != -1)
                tooltips.RemoveAt(consumableIndex);
        }
    }


}

*/