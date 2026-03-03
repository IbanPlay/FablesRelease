using NetEasy;
using Terraria.ModLoader.IO;

namespace CalamityFables.Cooldowns
{
    public class CooldownsPlayer : ModPlayer
    {
        public Dictionary<string, CooldownInstance> cooldowns;

        #region Saving & Loading
        public override void Initialize()
        {
            cooldowns = new Dictionary<string, CooldownInstance>(16);
        }

        public override void SaveData(TagCompound tag)
        {
            // Save all cooldowns which are marked as persisting through save/load.
            TagCompound cooldownsTag = new TagCompound();
            var cdIterator = cooldowns.GetEnumerator();
            while (cdIterator.MoveNext())
            {
                KeyValuePair<string, CooldownInstance> kv = cdIterator.Current;
                string id = kv.Key;
                CooldownInstance instance = kv.Value;

                // If the cooldown isn't supposed to persist, skip it.
                if (!instance.handler.SavedWithPlayer)
                    continue;

                // Add this cooldown to the overall cooldowns tag compound using its ID as the string key.
                TagCompound singleCDTag = instance.Save();
                cooldownsTag.Add(id, singleCDTag);
            }
            tag["cooldowns"] = cooldownsTag;
        }

        public override void LoadData(TagCompound tag)
        {
            // Clear the player's cooldowns in preparation for loading.
            cooldowns.Clear();
            if (!tag.ContainsKey("cooldowns"))
                return;

            // Load cooldowns and add them to the player's cooldown list.
            TagCompound cooldownsTag = tag.GetCompound("cooldowns");
            var tagIterator = cooldownsTag.GetEnumerator();
            while (tagIterator.MoveNext())
            {
                KeyValuePair<string, object> kv = tagIterator.Current;
                string id = kv.Key;
                TagCompound singleCDTag = cooldownsTag.GetCompound(id);
                CooldownInstance instance = new CooldownInstance(Player, id, singleCDTag);
                if (instance.player == null)
                    continue;

                cooldowns.Add(id, instance);
            }
        }
        #endregion


        public override void UpdateDead()
        {
            // This function runs every frame the player is dead, so if the player does not have any cooldowns, don't try to remove any.
            if (cooldowns.Count > 0)
            {
                // Iterate through all cooldowns and find those which do not persist through death.
                IList<string> removedCooldowns = new List<string>(16);
                var cdIterator = cooldowns.GetEnumerator();
                while (cdIterator.MoveNext())
                {
                    KeyValuePair<string, CooldownInstance> kv = cdIterator.Current;
                    string id = kv.Key;
                    CooldownInstance instance = kv.Value;
                    CooldownHandler handler = instance.handler;
                    if (!handler.PersistsThroughDeath)
                        removedCooldowns.Add(id);
                }
                cdIterator.Dispose();

                // Actually remove all cooldowns which do not persist through death.
                // If any cooldowns were removed, net sync the remaining cooldown dictionary.
                if (removedCooldowns.Count > 0)
                {
                    foreach (string cdID in removedCooldowns)
                        cooldowns.Remove(cdID);
                    SyncCooldownDictionary(Main.netMode == NetmodeID.Server);
                }
            }
        }

        public override void PostUpdateBuffs()
        {
            if (Player.whoAmI == Main.myPlayer && FablesConfig.Instance.VanillaCooldownDisplay)
            {
                // Add a cooldown display for potion sickness if the player has the vanilla counter ticking
                if (Player.potionDelay > 0 && !Player.HasCooldown(PotionSicknessCooldown.ID))
                    Player.AddCooldown(PotionSicknessCooldown.ID, Player.potionDelay, false);


                // Add a cooldown display for chaos state if the player has the vanilla counter ticking
                // This will make the cooldown look like vanilla Rod of Discord, as it wasn't applied by either Normality Relocator or Spectral Veil
                if (Player.chaosState)
                {
                    for (int l = 0; l < Player.MaxBuffs; l++)
                        if (Player.buffType[l] == BuffID.ChaosState)
                        {
                            if (!Player.HasCooldown(ChaosStateCooldown.ID))
                                Player.AddCooldown(ChaosStateCooldown.ID, Player.buffTime[l], false);
                            else if (Player.FindCooldown(ChaosStateCooldown.ID, out var cooldown) && Player.buffTime[l] > cooldown.timeLeft)
                                cooldown.timeLeft = Player.buffTime[l];
                            break;
                        }
                }
            }
        }

        public override void PostUpdateMiscEffects()
        {

            // Tick all cooldowns.
            // Depending on the code for each individual cooldown, this isn't guaranteed to do anything.
            // It may not tick down the timer or not do anything at all.
            IList<string> expiredCooldowns = new List<string>(16);
            var cdIterator = cooldowns.GetEnumerator();
            while (cdIterator.MoveNext())
            {
                KeyValuePair<string, CooldownInstance> kv = cdIterator.Current;
                string id = kv.Key;
                CooldownInstance instance = kv.Value;
                CooldownHandler handler = instance.handler;

                // If applicable, tick down this cooldown instance's timer.
                if (handler.CanTickDown)
                    --instance.timeLeft;

                //if (handler is WulfrumRoverDriveDurability)
                //    Dust.QuickDust(Player.Center - Vector2.UnitY * 20f, Color.Red);

                // Tick always runs, even if the timer does not decrement.
                handler.Tick();

                // Run on-completion code, play sounds and remove finished cooldowns.
                if (instance.timeLeft < 0)
                {
                    handler.OnCompleted();
                    if (handler.EndSound != null)
                        SoundEngine.PlaySound(handler.EndSound.GetValueOrDefault());
                    expiredCooldowns.Add(id);
                }
            }
            cdIterator.Dispose();

            // Remove all expired cooldowns.
            foreach (string cdID in expiredCooldowns)
                cooldowns.Remove(cdID);

            // If any cooldowns were removed, send a cooldown removal packet that lists all cooldowns to remove.
            if (expiredCooldowns.Count > 0)
                SyncCooldownRemoval(Main.netMode == NetmodeID.Server, expiredCooldowns);

        }


        public void SyncCooldownAddition(bool server, CooldownInstance cd)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            CooldownSyncPacket packet = new CooldownSyncPacket(Player, cd);
            packet.Send(-1, Player.whoAmI, false);
        }

        public void SyncCooldownRemoval(bool server, IList<string> cooldownIDs)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            CooldownRemovalSyncPacket packet = new CooldownRemovalSyncPacket(Player, cooldownIDs);
            packet.Send(-1, Player.whoAmI, false);
        }

        public void SyncCooldownDictionary(bool server)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            CooldownDictionarySyncPacket packet = new CooldownDictionarySyncPacket(this);
            packet.Send(-1, Player.whoAmI, false);
        }
    }




    [Serializable]
    public class CooldownSyncPacket : Module
    {
        public readonly byte whoAmI;
        public readonly ushort netID;
        public readonly int duration;
        public readonly int timeLeft;

        public CooldownSyncPacket(Player player, CooldownInstance cd)
        {
            whoAmI = (byte)player.whoAmI;
            netID = cd.netID;
            duration = cd.duration;
            timeLeft = cd.timeLeft;
        }

        protected override void Receive()
        {
            CooldownsPlayer mp = Main.player[whoAmI].GetModPlayer<CooldownsPlayer>();

            CooldownInstance cd = new CooldownInstance(netID, whoAmI, duration, timeLeft);
            string id = CooldownLoader.registry[netID].ID;
            mp.cooldowns[id] = cd;

            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, mp.Player.whoAmI, false);
                return;
            }
        }
    }

    [Serializable]
    public class CooldownRemovalSyncPacket : Module
    {
        public readonly byte whoAmI;
        public readonly int cooldownCount;
        public readonly ushort[] IDs;

        public CooldownRemovalSyncPacket(Player player, IList<string> cooldownIDs)
        {
            whoAmI = (byte)player.whoAmI;
            cooldownCount = cooldownIDs.Count;
            IDs = new ushort[cooldownCount];

            int i = 0;
            foreach (string id in cooldownIDs)
            {
                IDs[i] = CooldownLoader.Get(id).netID;
                i++;
            }
        }

        protected override void Receive()
        {
            CooldownsPlayer mp = Main.player[whoAmI].GetModPlayer<CooldownsPlayer>();

            for (int i = 0; i < cooldownCount; ++i)
            {
                ushort netID = IDs[i];
                mp.cooldowns.Remove(CooldownLoader.registry[netID].ID);
            }

            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, mp.Player.whoAmI, false);
                return;
            }
        }
    }

    [Serializable]
    public class CooldownDictionarySyncPacket : Module
    {
        public readonly byte whoAmI;
        public readonly int cooldownCount;

        public readonly ushort[] IDs;
        public readonly int[] durations;
        public readonly int[] timeLefts;

        public CooldownDictionarySyncPacket(CooldownsPlayer player)
        {
            whoAmI = (byte)player.Player.whoAmI;
            cooldownCount = player.cooldowns.Count;

            IDs = new ushort[cooldownCount];
            durations = new int[cooldownCount];
            timeLefts = new int[cooldownCount];

            int i = 0;
            foreach (CooldownInstance cd in player.cooldowns.Values)
            {
                IDs[i] = cd.netID;
                durations[i] = cd.duration;
                timeLefts[i] = cd.timeLeft;
                i++;
            }
        }

        protected override void Receive()
        {
            CooldownsPlayer mp = Main.player[whoAmI].GetModPlayer<CooldownsPlayer>();

            if (cooldownCount <= 0)
                return;

            // Cooldown dictionary packets are just a span of serialized cooldown instances. So each one can be read exactly as with a single cooldown.
            Dictionary<ushort, CooldownInstance> syncedCooldowns = new Dictionary<ushort, CooldownInstance>(cooldownCount);
            for (int i = 0; i < cooldownCount; ++i)
            {
                CooldownInstance instance = new CooldownInstance(IDs[i], whoAmI, durations[i], timeLefts[i]);
                syncedCooldowns[instance.netID] = instance;
            }


            HashSet<ushort> localIDs = new HashSet<ushort>();
            foreach (CooldownInstance localInstance in mp.cooldowns.Values)
                localIDs.Add(localInstance.netID);

            HashSet<ushort> syncedIDs = new HashSet<ushort>();
            foreach (ushort syncedID in syncedCooldowns.Keys)
                syncedIDs.Add(syncedID);

            HashSet<ushort> combinedIDSet = new HashSet<ushort>();
            combinedIDSet.UnionWith(localIDs);
            combinedIDSet.UnionWith(syncedIDs);

            foreach (ushort netID in combinedIDSet)
            {
                bool existsLocally = localIDs.Contains(netID);
                bool existsRemotely = syncedIDs.Contains(netID);
                string id = CooldownLoader.registry[netID].ID;

                // Exists locally but not remotely = cull -- destroy the local copy.
                if (existsLocally && !existsRemotely)
                    mp.cooldowns.Remove(id);
                // Exists remotely but not locally = add -- insert into the dictionary.
                else if (existsRemotely && !existsLocally)
                    mp.cooldowns[id] = syncedCooldowns[netID];
                // Exists in both places = update -- update timing fields but don't replace the instance.
                else if (existsLocally && existsRemotely)
                {
                    CooldownInstance localInstance = mp.cooldowns[id];
                    localInstance.duration = syncedCooldowns[netID].duration;
                    localInstance.timeLeft = syncedCooldowns[netID].timeLeft;
                }
            }

            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, mp.Player.whoAmI, false);
                return;
            }
        }
    }
}
