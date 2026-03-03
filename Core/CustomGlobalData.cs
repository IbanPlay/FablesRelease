namespace CalamityFables.Core
{
    /// <summary>
    /// Base class for custom data that can be stored into NPCs and Players and Projectiles using <see cref="FablesUtils.SetNPCData{T}(NPC, T)"/>
    /// </summary>
    public abstract class CustomGlobalData
    {
        /// <summary>
        /// Use this to clear out the data when ResetEffects happen
        /// </summary>
        public virtual void Reset()
        {

        }

        /// <summary>
        /// Set this to true if the data should be synced on projectiles <br/>
        /// Automatically sets itself to false after being synced once
        /// </summary>
        public bool needsSyncing_forProjectiles;
    }
}
