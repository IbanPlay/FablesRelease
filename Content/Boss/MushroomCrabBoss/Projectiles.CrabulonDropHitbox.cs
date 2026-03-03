using CalamityFables.Content.Projectiles;
using Terraria.DataStructures;
using Terraria.Localization;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class CrabulonDropHitbox : HostileDirectStrike, ICustomDeathMessages
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crabulon");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        //Same death message as the stomp
        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            customDeath.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.CrabulonDropSlam." + Main.rand.Next(1, 5).ToString()).ToNetworkText(player.name);
            return true;
        }
    }

    public class CrabulonClawHitbox : HostileDirectStrike, ICustomDeathMessages
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crabulon");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            customDeath.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.CrabulonClaw." + Main.rand.Next(1, 5).ToString()).ToNetworkText(player.name);
            return true;
        }
    }
}
