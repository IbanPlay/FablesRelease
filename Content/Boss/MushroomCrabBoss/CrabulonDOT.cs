using CalamityFables.Content.Items.CrabulonDrops;
using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.Localization;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    /// <summary>
    /// Deals 6 damage per second to NPCs
    /// </summary>
    public class CrabulonDOT : ModBuff, ICustomDeathMessages
    {
        public override void Load()
        {
            Filters.Scene["CalamityFables:CrabulonPsychosis"] = new Filter(
                new CrabulonPsychosisShaderData(ModContent.Request<Effect>("CalamityFables/Effects/CrabulonAmbience"), "CrabulonAmbiencePass")
                .UseColor(new Color(6, 87, 255)) //Color used both to tint the bottom of the screen & to mask out the correct shades to get the holo effect applied to
                .UseImage(ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "ScreenshaderColorGrading"), 0) //Gradient map texture that color maps the screen to be bluer
                .UseImage(ModContent.Request<Texture2D>(AssetDirectory.Noise + "CracksDisplace"), 1) //Distortion map to make the image warble when the psychosis is high
                .UseImage(ModContent.Request<Texture2D>(AssetDirectory.Noise + "RainbowPerlin"), 2) //Holographic noise that's applied over the blue parts of the screen
                , EffectPriority.High);

        }


        public float DoTDeathMessagePriority => 2f;
        public bool CustomDeathMessage(Player player, ref PlayerDeathReason deathMessage)
        {
            deathMessage.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.CrabulonSpores." + Main.rand.Next(1, 6).ToString()).ToNetworkText(player.name);
            return true;
        }

        public override string Texture => AssetDirectory.Crabulon + "Spored";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spored");
            Description.SetDefault("Your lungs are filling with fungus!");
            Main.debuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            Main.buffNoSave[Type] = true;

            NPCID.Sets.SpecificDebuffImmunity[NPCID.FungoFish][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.AnomuraFungus][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.FungiBulb][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.GiantFungiBulb][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.MushiLadybug][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.SporeBat][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.SporeSkeleton][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.ZombieMushroom][Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[NPCID.ZombieMushroomHat][Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegenTime = 0;
            player.lifeRegen = Math.Min(player.lifeRegen, 0);

            //We want to inflict a third of the damage per second, since the debuff is applied for three seconds
            int damagePerSecond = (int)(Crabulon.SporeInfestation_InflictionDamage / (Crabulon.SporeInfestation_MaxInflictionTime));
            //The player looses half their liferegen value per second, so we double it
            player.lifeRegen -= damagePerSecond * 2;



            if (Main.rand.NextBool(6))
            {
                Vector2 position = player.Center + Main.rand.NextVector2Circular(22f, 22f);
                Dust.NewDustPerfect(position, DustID.GlowingMushroom, Main.rand.NextVector2Circular(3f, 3f), Scale: Main.rand.NextFloat(0.7f, 1.1f));
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 position = player.Center + Main.rand.NextVector2Circular(22f, 22f);
                float smokeSize = Main.rand.NextFloat(1.6f, 2.2f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.2f, 0.3f) - Vector2.UnitY * 0.6f;

                Particle smoke = new SporeGas(position, velocity, position, 50, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.lifeRegen -= Sporethrower.DEBUFF_DOT * 2;

            if (Main.rand.NextBool(7))
            {
                Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.6f, npc.height * 0.6f);
                Dust.NewDustPerfect(position, DustID.GlowingMushroom, Main.rand.NextVector2Circular(3f, 3f), Scale: Main.rand.NextFloat(0.7f, 1.1f));
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.6f, npc.height * 0.6f);
                float smokeSize = Main.rand.NextFloat(1.6f, 2.2f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.2f, 0.3f) - Vector2.UnitY * 0.6f;

                Particle smoke = new SporeGas(position, velocity, position, 50, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }
        }
    }
}
