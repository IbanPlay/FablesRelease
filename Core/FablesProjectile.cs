using MonoMod.Cil;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace CalamityFables.Core
{
    public partial class FablesProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public Dictionary<Type, CustomGlobalData> MiscData = [];

        private static Dictionary<Type, ushort> MiscDataSync_TypeToNetID = [];
        private static Dictionary<ushort, Type> MiscDataSync_NetIDToType = [];
        public delegate void NetSerializeDelegate(CustomGlobalData data, Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter);
        public delegate void NetDeserializeDelegate(CustomGlobalData data, Projectile projectile, BitReader bitReader, BinaryReader binaryReader);
        private static ushort MiscDataNetIDCounter = 0;
        private static readonly List<NetSerializeDelegate> MiscDataSerializationDelegates = new();
        private static readonly List<NetDeserializeDelegate> MiscDataDeserializationDelegates = new();

        public static readonly int[] minionPosPerType = new int[Main.maxProjectiles];
        internal static bool minionPosPerTypeILEditBroke = false;

        /// <summary>
        /// Registers delegates to use to synchronize <see cref="CustomGlobalData"/> for projectiles <br/>
        /// Syncing only happens when necessary (<see cref="CustomGlobalData.needsSyncing_forProjectiles"/>) to prevent sending unnecessary data <br/>
        /// </summary>
        /// <remarks>
        /// If the <see cref="CustomGlobalData"/> has a parameterless constructor, it'll automatically instantiate the global data on any projectile recieving sync data without an instance of the data already present
        /// </remarks>
        /// <param name="dataTypeToSync">The data type to sync</param>
        /// <param name="serializationDelegate">Delegate that writes the custom info to send. Only called if <see cref="CustomGlobalData.needsSyncing_forProjectiles"/> is set to true. <br/>
        /// If the only thing you need to sync is the presence of the custom data itself without having to sync any specific data, you can leave this delegate as null</param>
        /// <param name="deserializationDelegate">The delegate for recieving the sync data<br/>
        /// If the data type has a parameterless constructor, the sync will automatically create and assign an instance of the data to the projectile, so you can leave this delegate as null if you only care about the presence of the delegate</param>
        public static void RegisterSyncedData(Type dataTypeToSync, NetSerializeDelegate serializationDelegate, NetDeserializeDelegate deserializationDelegate)
        {
            MiscDataSync_TypeToNetID[dataTypeToSync] = MiscDataNetIDCounter;
            MiscDataSync_NetIDToType[MiscDataNetIDCounter] = dataTypeToSync;
            MiscDataSerializationDelegates.Add(serializationDelegate);
            MiscDataDeserializationDelegates.Add(deserializationDelegate);
            MiscDataNetIDCounter++;
        }

        public int supercritHits;

        public override void Load()
        {
            IL_Player.UpdateProjectileCaches += IL_Player_UpdateProjectileCaches;
            On_Main.CacheProjDraws += ResetDrawOrderCaches;
            On_Main.GetProjectileDesiredShader += ModifyProjectileDye;
            FablesDrawLayers.DrawThingsBehindSolidTilesEvent += DrawCachedProjectilesBehindTiles;
            IL_Projectile.AI_001 += DisableVelocityCap;
        }

        private void IL_Player_UpdateProjectileCaches(MonoMod.Cil.ILContext il)
        {
            minionPosPerTypeILEditBroke = true;
            ILCursor cursor = new ILCursor(il);

            //Go before the grate check, since we're gonna have to skip it
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg0(),
                i => i.MatchLdfld<Player>("ownedProjectileCounts"),
                i => i.MatchLdsfld<Main>("projectile"),
                i => i.MatchLdloc(0),
                i => i.MatchLdelemRef(),
                i => i.MatchLdfld<Projectile>("type"),
                i => i.MatchLdelema<int>(),
                i => i.MatchDup(),
                i => i.MatchLdindI4(),
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStindI4()))
            {
                FablesUtils.LogILEpicFail("Add per-type minionPos cache", "Could not locate ownedProjectileCounts[Main.projectile[j].type]++");
                return;
            }

            cursor.EmitLdarg0();
            cursor.EmitLdloc0();
            cursor.EmitDelegate(UpdateMinionPosCache);

            minionPosPerTypeILEditBroke = false;
        }

        private static void DisableVelocityCap(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(1),
                i => i.MatchStloc2()
                ))
            {
                FablesUtils.LogILEpicFail("Disable Velocity Cap AI_001", "Could not locate boolean to disable cap");
                return;
            }

            cursor.EmitLdarg0();
            cursor.EmitDelegate(SetVelocityCap);
            cursor.EmitStloc2();
        }

        public delegate bool SetVelocityCapDelegate(Projectile projectile1);

        /// <summary>
        /// Disables the AI 1 velocit
        /// </summary>
        public static event SetVelocityCapDelegate SetVelocityCapEvent;
        private static bool SetVelocityCap(Projectile projectile)
        {
            bool result = true;

            if (SetVelocityCapEvent != null)
            {
                foreach (SetVelocityCapDelegate entry in SetVelocityCapEvent.GetInvocationList())
                    result &= entry(projectile);
            }

            return result;
        }

        private static void UpdateMinionPosCache(Player player, int projIndex)
        {
            Projectile p = Main.projectile[projIndex];
            minionPosPerType[projIndex] = player.ownedProjectileCounts[p.type] - 1;
        }

        public override void Unload()
        {
            //In unload instead of load to avoid any possible scenario where the list gets cleared after stuffgets written there in Load() hooks that run earlier
            MiscDataNetIDCounter = 0;
            MiscDataSerializationDelegates.Clear();
            MiscDataDeserializationDelegates.Clear();
        }

        public static List<int> DrawBehindTilesAlways = new List<int>();
        private void ResetDrawOrderCaches(On_Main.orig_CacheProjDraws orig, Main self)
        {
            DrawBehindTilesAlways.Clear();
            orig(self);
        }

        //Copy of main drawcachednpcs but its private so rip
        private void DrawCachedProjectilesBehindTiles()
        {
            if (DrawBehindTilesAlways.Count == 0)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            Main.CurrentDrawnEntity = null;
            Main.CurrentDrawnEntityShader = 0;

            for (int i = 0; i < DrawBehindTilesAlways.Count; i++)
            {
                try
                {
                    Main.instance.DrawProj(DrawBehindTilesAlways[i]);
                }
                catch (Exception e)
                {
                    TimeLogger.DrawException(e);
                    Main.projectile[DrawBehindTilesAlways[i]].active = false;
                }
            }

            Main.CurrentDrawnEntity = null;
            Main.CurrentDrawnEntityShader = 0;
            Main.spriteBatch.End();
        }

        private void ResetEffects()
        {
            foreach (var data in MiscData)
                data.Value.Reset();
        }

        private void SuperCrits(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (supercritHits > 0 && projectile.CritChance > 100)
                modifiers.CritDamage += (projectile.CritChance - 100) / 100f;

            supercritHits--;
        }

        private static void FertilizerFix(Projectile projectile)
        {
            //WE HAVE TO DO THAT BECAUSE FERTILIZER FSR ONLY CALLS GROWTREEFROMSAPLING FOR NON MODDED SAPLINGS
            if (projectile.type == ProjectileID.Fertilizer && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int checkXStart =  Math.Max(2, (int)(projectile.position.X / 16f) - 1);
                int checkXEnd = Math.Min((int)((projectile.position.X + (float)projectile.width) / 16f) + 2, Main.maxTilesX - 2);
                int checkYStart = Math.Max(2, (int)(projectile.position.Y / 16f) - 1);
                int checkYEnd = Math.Min((int)((projectile.position.Y + (float)projectile.height) / 16f) + 2, Main.maxTilesY - 2) ;


                for (int i = checkXStart; i < checkXEnd; i++)
                {
                    for (int j = checkYStart; j < checkYEnd; j++)
                    {
                        if (!(projectile.position.X + projectile.width > i * 16) || !(projectile.position.X < (i + 1) * 16) || !(projectile.position.Y + projectile.height > j * 16) || !(projectile.position.Y < (j + 1) * 16) || !Main.tile[i, j].HasTile)
                            continue;
;
                        Tile tile = Main.tile[i, j];
                        if (tile.TileType >= TileID.Count && TileID.Sets.CommonSapling[tile.TileType])
                        {
                            bool underground = j > (int)Main.worldSurface - 1;
                            if (Main.remixWorld && j >= (int)Main.worldSurface - 1 && j < Main.maxTilesY - 20)
                                underground = false;

                            FablesGeneralSystemHooks.GrowModdedSaplingDelegate growEvent = FablesGeneralSystemHooks.FertilizeSaplingEvent.GetInvocation(Main.tile[i, j].TileType);
                            if (growEvent != null)
                                growEvent(i, j, underground, true);
                        }
                    }
                }
            }
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            List<KeyValuePair<Type, CustomGlobalData>> dataToSync = new List<KeyValuePair<Type, CustomGlobalData>>();

            //Check every custom data for any that want to be synced and add them to a list
            foreach (KeyValuePair<Type, CustomGlobalData> data in MiscData)
            {
                if (data.Value.needsSyncing_forProjectiles)
                {
                    dataToSync.Add(data);
                    data.Value.needsSyncing_forProjectiles = false;
                }
            }

            binaryWriter.Write((ushort)dataToSync.Count);
            foreach (KeyValuePair<Type, CustomGlobalData> syncedData in dataToSync)
            {
                ushort syncNetID = MiscDataSync_TypeToNetID[syncedData.Key];

                //Writes the net ID
                binaryWriter.Write(syncNetID);

                //Writes the custom data
                MiscDataSerializationDelegates[syncNetID]?.Invoke(syncedData.Value, projectile, bitWriter, binaryWriter);
            }
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            ushort syncedDataCount = binaryReader.ReadUInt16();
            if (syncedDataCount == 0)
                return;

            for (int i = 0; i < syncedDataCount; i++)
            {
                //Read the net ID, then reads the custom data
                ushort syncNetID = binaryReader.ReadUInt16();
                Type syncedType = MiscDataSync_NetIDToType[syncNetID];

                #if DEBUG
                if (syncedType.GetConstructor(Type.EmptyTypes) != null)
                    throw new ArgumentException("Cannot initialize an instance of CustomGlobalData without an empty construct!");
                #endif

                if (!MiscData.TryGetValue(syncedType, out CustomGlobalData syncedData) && syncedType.GetConstructor(Type.EmptyTypes) != null)
                {
                    syncedData = (CustomGlobalData)Activator.CreateInstance(syncedType);

                    if (Main.dedServ)
                        syncedData.needsSyncing_forProjectiles = true;

                    MiscData[syncedType] = syncedData;
                }

                MiscDataDeserializationDelegates[syncNetID]?.Invoke(syncedData, projectile, bitReader, binaryReader);
            }
        }

        #region Custom Events
        public delegate void ProjectileActionDelegate(Projectile projectile);
        public delegate bool BooleanProjectileActionDelegate(Projectile projectile);
        public delegate bool? NullableBooleanProjectileActionDelegate(Projectile projectile);

        public delegate void ModifyProjectileDyeDelegate(Projectile projectile, ref int dyeID);

        /// <summary>
        /// Can be used to modify the dye shader used when drawing a projectile
        /// </summary>
        public static event ModifyProjectileDyeDelegate ModifyProjectileDyeEvent;
        private int ModifyProjectileDye(On_Main.orig_GetProjectileDesiredShader orig, Projectile proj)
        {
            int dyeID = orig(proj);
            ModifyProjectileDyeEvent?.Invoke(proj, ref dyeID);
                return dyeID;
        }

        public delegate void OnSpawnDelegate(Projectile projectile, IEntitySource source);

        /// <inheritdoc cref="GlobalProjectile.OnSpawn(Projectile, IEntitySource)"/>
        public static event OnSpawnDelegate OnSpawnEvent;
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            OnSpawnEvent?.Invoke(projectile, source);
        }

        /// <inheritdoc cref="GlobalProjectile.PreAI(Projectile)"/>
        public static event BooleanProjectileActionDelegate PreAIEvent;
        public override bool PreAI(Projectile projectile)
        {
            ResetEffects();
            if (PreAIEvent is not null)
                foreach (BooleanProjectileActionDelegate eventEntry in PreAIEvent.GetInvocationList())
                    if (!eventEntry(projectile))
                        return false;
            return true;
        }

        /// <inheritdoc cref="GlobalProjectile.AI(Projectile)"/>
        public static event ProjectileActionDelegate AIEvent;
        public override void AI(Projectile projectile)
        {
            FertilizerFix(projectile);
            AIEvent?.Invoke(projectile);
        }

        /// <inheritdoc cref="GlobalProjectile.PostAI(Projectile)"/>
        public static event ProjectileActionDelegate PostAIEvent;
        public override void PostAI(Projectile projectile)
        {
            PostAIEvent?.Invoke(projectile);
        }

        public delegate bool OnTileCollideDelegate(Projectile projectile, Vector2 oldVelocity);

        /// <inheritdoc cref="GlobalProjectile.OnTileCollide(Projectile, Vector2)"/>
        public static event OnTileCollideDelegate OnTileCollideEvent;
        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            if (OnTileCollideEvent is not null)
                foreach (OnTileCollideDelegate eventEntry in OnTileCollideEvent.GetInvocationList())
                    if (!eventEntry(projectile, oldVelocity))
                        return false;
            return true;
        }

        public delegate bool PreKillDelegate(Projectile projectile, int timeLeft);

        /// <inheritdoc cref="GlobalProjectile.PreKill(Projectile, int)"/>
        public static event PreKillDelegate PreKillEvent;
        public override bool PreKill(Projectile projectile, int timeLeft)
        {
            if (PreKillEvent is not null)
                foreach (PreKillDelegate eventEntry in PreKillEvent.GetInvocationList())
                    if (!eventEntry(projectile, timeLeft))
                        return false;
            return true;
        }

        public delegate void OnKillDelegate(Projectile projectile, int timeLeft);

        /// <inheritdoc cref="GlobalProjectile.PreKill(Projectile, int)"/>
        public static event OnKillDelegate OnKillEvent;
        public override void OnKill(Projectile projectile, int timeLeft)
        {
            OnKillEvent?.Invoke(projectile, timeLeft);
        }

        /// <inheritdoc cref="GlobalProjectile.CanDamage(Projectile)"/>
        public static event NullableBooleanProjectileActionDelegate CanDamageEvent;
        public override bool? CanDamage(Projectile projectile)
        {
            bool forceTrue = false;
            if (CanDamageEvent is not null)
                foreach (NullableBooleanProjectileActionDelegate eventEntry in CanDamageEvent.GetInvocationList())
                {
                    bool? result = eventEntry(projectile);
                    if (result is not null)
                    {
                        if (result.Value)
                            forceTrue = true;
                        else
                            return false;
                    }
                }
            return forceTrue ? true : null;
        }

        public delegate bool? CanHitNPCDelegate(Projectile projectile, NPC target);

        /// <inheritdoc cref="GlobalProjectile.CanHitNPC(Projectile, NPC)"/>
        public static event CanHitNPCDelegate CanHitNPCEvent;
        public override bool? CanHitNPC(Projectile projectile, NPC target)
        {
            bool forceTrue = false;
            if (CanHitNPCEvent is not null)
                foreach (CanHitNPCDelegate eventEntry in CanHitNPCEvent.GetInvocationList())
                {
                    bool? result = eventEntry(projectile, target);
                    if (result is not null)
                    {
                        if (result.Value)
                            forceTrue = true;
                        else
                            return false;
                    }
                }
            return forceTrue ? true : null;
        }

        public delegate void ModifyHitNPCDelegate(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers);

        /// <inheritdoc cref="GlobalProjectile.ModifyHitNPC(Projectile, NPC, ref NPC.HitModifiers)"/>
        public static event ModifyHitNPCDelegate ModifyHitNPCEvent;
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            SuperCrits(projectile, ref modifiers);
            ModifyHitNPCEvent?.Invoke(projectile, target, ref modifiers);
        }

        public delegate void OnHitNPCDelegate(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone);

        /// <inheritdoc cref="GlobalProjectile.OnHitNPC(Projectile, NPC, NPC.HitInfo, int)"/>
        public static event OnHitNPCDelegate OnHitNPCEvent;
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            OnHitNPCEvent?.Invoke(projectile, target, hit, damageDone);
        }

        public delegate bool BooleanProjectilePlayerAction(Projectile projectile, Player target);

        /// <inheritdoc cref="GlobalProjectile.CanHitPvp(Projectile, Player)"/>
        public static event BooleanProjectilePlayerAction CanHitPvpEvent;
        public override bool CanHitPvp(Projectile projectile, Player target)
        {
            if (CanHitPvpEvent is not null)
                foreach (BooleanProjectilePlayerAction eventEntry in CanHitPvpEvent.GetInvocationList())
                    if (!eventEntry(projectile, target))
                        return false;
            return true;
        }

        /// <inheritdoc cref="GlobalProjectile.CanHitPlayer(Projectile, Player)"/>
        public static event BooleanProjectilePlayerAction CanHitPlayerEvent;
        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (CanHitPlayerEvent is not null)
                foreach (BooleanProjectilePlayerAction eventEntry in CanHitPlayerEvent.GetInvocationList())
                    if (!eventEntry(projectile, target))
                        return false;
            return true;
        }

        public delegate void ModifyHitPlayerDelegate(Projectile projectile, Player target, ref Player.HurtModifiers modifiers);

        /// <inheritdoc cref="GlobalProjectile.ModifyHitPlayer(Projectile, Player, ref Player.HurtModifiers)"/>
        public static event ModifyHitPlayerDelegate ModifyHitPlayerEvent;
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            ModifyHitPlayerEvent?.Invoke(projectile, target, ref modifiers);
        }

        public delegate void OnHitPlayerDelegate(Projectile projectile, Player target, Player.HurtInfo info);

        /// <inheritdoc cref="GlobalProjectile.OnHitPlayer(Projectile, Player, Player.HurtInfo)"/>
        public static event OnHitPlayerDelegate OnHitPlayerEvent;
        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            OnHitPlayerEvent?.Invoke(projectile, target, info);
        }

        public delegate bool? CollidingDelegate(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox);

        /// <inheritdoc cref="GlobalProjectile.Colliding(Projectile, Rectangle, Rectangle)"/>
        public static event CollidingDelegate CollidingEvent;
        public override bool? Colliding(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox)
        {
            bool forceTrue = false;
            if (CollidingEvent is not null)
                foreach (CollidingDelegate eventEntry in CollidingEvent.GetInvocationList())
                {
                    bool? result = eventEntry(projectile, projHitbox, targetHitbox);
                    if (result is not null)
                    {
                        if (result.Value)
                            forceTrue = true;
                        else
                            return false;
                    }
                }
            return forceTrue ? true : null;
        }

        /// <inheritdoc cref="GlobalProjectile.PreDrawExtras(Projectile)"/>
        public static event BooleanProjectileActionDelegate PreDrawExtrasEvent;
        public override bool PreDrawExtras(Projectile projectile)
        {
            if (PreDrawExtrasEvent is not null)
                foreach (BooleanProjectileActionDelegate eventEntry in PreDrawExtrasEvent.GetInvocationList())
                    if (!eventEntry(projectile))
                        return false;
            return true;
        }

        public delegate bool PreDrawDelegate(Projectile projectile, ref Color lightColor);

        /// <inheritdoc cref="GlobalProjectile.PreDraw(Projectile, ref Color)"/>
        public static event PreDrawDelegate PreDrawEvent;
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (PreDrawEvent is not null)
                foreach (PreDrawDelegate eventEntry in PreDrawEvent.GetInvocationList())
                    if (!eventEntry(projectile, ref lightColor))
                        return false;
            return true;
        }

        public delegate void PostDrawDelegate(Projectile projectile, Color lightColor);

        /// <inheritdoc cref="GlobalProjectile.PostDraw(Projectile, Color)"/>
        public static event PostDrawDelegate PostDrawEvent;
        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            PostDrawEvent?.Invoke(projectile, lightColor);
        }

        public delegate void DrawBehindDelegate(Projectile projectile, int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI);

        /// <inheritdoc cref="GlobalProjectile.DrawBehind(Projectile, int, List{int}, List{int}, List{int}, List{int}, List{int})"/>
        public static event DrawBehindDelegate DrawBehindEvent;
        public override void DrawBehind(Projectile projectile, int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            DrawBehindEvent?.Invoke(projectile, index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }

        public delegate bool? CanUseGrappleDelegate(int type, Player player);

        /// <inheritdoc cref="GlobalProjectile.CanUseGrapple(int, Player)"/>
        public static event CanUseGrappleDelegate CanUseGrappleEvent;
        public override bool? CanUseGrapple(int type, Player player)
        {
            bool forceTrue = false;
            if (CanUseGrappleEvent is not null)
                foreach (CanUseGrappleDelegate eventEntry in CanUseGrappleEvent.GetInvocationList())
                {
                    bool? result = eventEntry(type, player);
                    if (result is not null)
                    {
                        if (result.Value)
                            forceTrue = true;
                        else
                            return false;
                    }
                }
            return forceTrue ? true : null;
        }
        #endregion
    }
}

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        #region FablesProjectile Helpers

        /// <summary>
        /// Writes custom data in the form of an object to this projectile. <br/>
        /// This data doesn't get cleared in Pre-AI, but you can override <see cref="CustomGlobalData.Reset"/> to reset values. <br/>
        /// Can be read back with <see cref="GetProjectileData{T}(Projectile, out T)"/>
        /// </summary>
        /// <typeparam name="T">The class of the data object we're looking for</typeparam>
        /// <param name="npc"></param>
        /// <param name="data">The data to write</param>
        public static void SetProjectileData<T>(this Projectile projectile, T data) where T : CustomGlobalData
        {
            projectile.GetGlobalProjectile<FablesProjectile>().MiscData[typeof(T)] = data;
        }

        /// <summary>
        /// Reads the custom data class written to this projectile with <see cref="SetProjectileData{T}(Projectile, T)"/>.<br/>
        /// This data doesn't get cleared on ResetEffects, but you can override <see cref="CustomGlobalData.Reset"/> to reset values/
        /// </summary>
        /// <typeparam name="T">The class of the data object we're looking for</typeparam>
        /// <param name="npc"></param>
        /// <param name="result">The retrieved data, if any</param>
        /// <returns>If there was any data of this type written to the projectile</returns>
        public static bool GetProjectileData<T>(this Projectile projectile, out T result) where T : CustomGlobalData
        {
            result = default(T);
            if (projectile.GetGlobalProjectile<FablesProjectile>().MiscData.TryGetValue(typeof(T), out CustomGlobalData objResult))
            {
                result = (T)objResult;
                return true;
            }
            return false;
        }

        #endregion

        #region Generic Projectile Helpers

        public delegate bool ValidTargetDelegate(NPC target, bool canBeChased);

        /// <summary>
        /// Finds the closest NPC to this projectile, within a specfied distance. <br/>
        /// Can check for line of sight and blacklist certain NPCs.
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="maxRange"></param>
        /// <param name="checkLineOfSight"></param>
        /// <param name="validTargetFunc"></param>
        /// <returns></returns>
        public static NPC FindTarget(this Projectile projectile, float maxRange, bool checkLineOfSight = false, ValidTargetDelegate validTargetFunc = null)
        {
            List<NPC> targets = projectile.FindTargets(maxRange, 1, checkLineOfSight, validTargetFunc);

            // Must return null if no targets where found
            return targets.Count > 0 ? targets.First() : null;
        }

        /// <summary>
        /// Finds the closest NPCs to this projectile, within a specfied distance. <br/>
        /// Set targets to -1 to pick an infinite number of nearby targets. <br/>
        /// Can check for line of sight and blacklist certain NPCs.
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="maxRange"></param>
        /// <param name="targets"></param>
        /// <param name="checkLineOfSight"></param>
        /// <param name="validTargetFunc"></param>
        /// <returns></returns>
        public static List<NPC> FindTargets(this Projectile projectile, float maxRange, int targets = 1, bool checkLineOfSight = false, ValidTargetDelegate validTargetFunc = null)
        {
            List<NPC> nearbyEnemies = [];
            List<NPC> targetList = [];

            // Find all enemies in range
            foreach (NPC npc in Main.ActiveNPCs)
            {
                bool validTarget = validTargetFunc?.Invoke(npc, npc.CanBeChasedBy(projectile)) ?? npc.CanBeChasedBy(projectile);
                if (validTarget && npc.WithinRange(projectile.Center, maxRange) && (!checkLineOfSight || Collision.CanHit(projectile, npc)))
                    nearbyEnemies.Add(npc);
            }

            if (nearbyEnemies.Count > 0)
            {
                int targetCount = 0;

                // Re-order targets by closest first
                nearbyEnemies = [.. nearbyEnemies.OrderBy(target => target.DistanceSQ(projectile.Center))];

                // Add first few targets to the output list
                foreach (NPC target in nearbyEnemies)
                {
                    if (targets > 0 && targetCount >= targets)
                        break;

                    targetList.Add(target);
                    targetCount++;
                }
            }

            return targetList;
        }

        /// <summary>
        /// Finds the best homing target for this projectile, dependant on the targets proximity and angle relative to the projectile's velocity. <br/>
        /// Certain NPCs can be blacklisted.
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="maxRange"></param>
        /// <param name="maxAngle"></param>
        /// <param name="validTargetFunc"></param>
        /// <returns></returns>
        public static NPC FindHomingTarget(this Projectile projectile, float maxRange, float maxAngle = MathHelper.PiOver2,ValidTargetDelegate validTargetFunc = null)
        {
            List<NPC> targets = projectile.FindHomingTargets(maxRange, 1, maxAngle, validTargetFunc);

            // Must return null if no targets where found
            return targets.Count > 0 ? targets.First() : null;
        }

        /// <summary>
        /// Finds the best homing targets for this projectile, dependant on the targets proximity and angle relative to the projectile's velocity. <br/>
        /// Certain NPCs can be blacklisted.
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="maxRange"></param>
        /// <param name="targets"></param>
        /// <param name="maxAngle"></param>
        /// <param name="validTargetFunc"></param>
        /// <returns></returns>
        public static List<NPC> FindHomingTargets(this Projectile projectile, float maxRange, int targets = 1, float maxAngle = MathHelper.PiOver2, ValidTargetDelegate validTargetFunc = null)
        {
            List<NPC> nearbyEnemies = [];
            List<NPC> targetList = [];

            // Find all enemies in range and within the max angle
            foreach (NPC npc in Main.ActiveNPCs)
            {
                bool validTarget = validTargetFunc?.Invoke(npc, npc.CanBeChasedBy(projectile)) ?? npc.CanBeChasedBy(projectile);
                if (validTarget && npc.WithinRange(projectile.Center, maxRange) && projectile.velocity.AngleBetween(npc.DirectionFrom(projectile.Center)) < maxAngle && Collision.CanHit(projectile, npc))
                    nearbyEnemies.Add(npc);
            }

            if (nearbyEnemies.Count > 0)
            {
                int targetCount = 0;

                // Order the highest score targets first
                nearbyEnemies = [.. nearbyEnemies.OrderByDescending(orderingFunction)];

                float orderingFunction(NPC target)
                {
                    // Greater score the lower the distance
                    float distance = 1 - target.Distance(projectile.Center) / maxRange;

                    // Score directly related to dot product, 1 being the greatest value
                    Vector2 projDirection = projectile.velocity;
                    Vector2 directionToTarget = target.DirectionFrom(projectile.Center);
                    float dot = projDirection.DotProduct(directionToTarget, true);

                    return distance + dot;
                }

                // Add first few targets to the output list
                foreach (NPC target in nearbyEnemies)
                {
                    if (targetCount >= targets)
                        break;

                    targetList.Add(target);
                    targetCount++;
                }
            }

            return targetList;
        }

        /// <summary>
        /// Gets the owner player of this projectile.
        /// </summary>
        /// <param name="projectile"></param>
        /// <returns></returns>
        public static Player GetOwner(this Projectile projectile) => Main.player[projectile.owner];

        public static int PerTypeMinionPos(this Projectile projectile)
        {
            if (FablesProjectile.minionPosPerTypeILEditBroke)
                return projectile.minionPos;
            return FablesProjectile.minionPosPerType[projectile.whoAmI];
        }
        #endregion
    }
}