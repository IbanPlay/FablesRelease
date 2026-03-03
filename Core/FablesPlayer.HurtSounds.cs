namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        public static void SetCustomHurtSound(this Player player, SoundStyle hurtSound, int soundDelay, float priority = 1f)
        {
            FablesPlayer modPlayer = player.GetModPlayer<FablesPlayer>();
            if (modPlayer.CustomHurtSoundPriority > priority)
                return;

            modPlayer.CustomHurtSound = hurtSound;
            modPlayer.CustomHurtSoundDelay = soundDelay;
            modPlayer.CustomHurtSoundPriority = priority;
        }
    }
}

namespace CalamityFables.Core
{
    public partial class FablesPlayer : ModPlayer
    {
        public SoundStyle? CustomHurtSound;
        public int CustomHurtSoundDelay;
        public int CustomHurtSoundCooldown;
        public float CustomHurtSoundPriority = -1f;


        public void ResetCustomHurtSound()
        {
            CustomHurtSound = null;
            CustomHurtSoundDelay = 0;
            CustomHurtSoundPriority = -1f;
        }

        public void TickDownHurtSoundTimer()
        {
            if (CustomHurtSoundCooldown > 0)
                CustomHurtSoundCooldown--;
        }

        public bool OverrideHurtSound()
        {
            if (CustomHurtSoundCooldown <= 0 && CustomHurtSound.HasValue)
            { 
                SoundEngine.PlaySound(CustomHurtSound.Value, Player.position);
                CustomHurtSoundCooldown = CustomHurtSoundDelay;
                return true;
            }
            return false;
        }
    }
}
