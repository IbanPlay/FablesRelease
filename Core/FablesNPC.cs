using static CalamityFables.Core.FablesNPC;

namespace CalamityFables.Core
{
    public partial class FablesNPC : GlobalNPC
    {
        public override void Load()
        {
            On_Main.DrawNPCHeadBoss += ModifyBossMapIconDrawing;
            On_NPC.GetTileCollisionParameters += ModifyCollisionParameters;
            On_NPC.ApplyTileCollision += CustomCollisionApplication;
            On_NPC.Collision_MoveSlopesAndStairFall += DisableSlopesAndStairs;
            On_NPC.ScaleStats_UseStrengthMultiplier += PreventStrenghtMult;
            On_Main.CacheNPCDraws += ResetDrawOrderCaches;
            FablesDrawLayers.DrawThingsBehindSolidTilesEvent += DrawNPCsAlwaysBehindTiles;
        }

        private void PreventStrenghtMult(On_NPC.orig_ScaleStats_UseStrengthMultiplier orig, NPC self, float strength)
        {
            if (!FablesSets.NoJourneyStengthScaling[self.type])
                orig(self, strength);
        }

        public static List<int> DrawBehindTilesAlways = new List<int>();
        private void ResetDrawOrderCaches(On_Main.orig_CacheNPCDraws orig, Main self)
        {
            DrawBehindTilesAlways.Clear();
            orig(self);
        }

        //Copy of main drawcachednpcs but its private so rip
        private void DrawNPCsAlwaysBehindTiles()
        {
            if (DrawBehindTilesAlways.Count == 0)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            for (int i = 0; i < DrawBehindTilesAlways.Count; i++)
            {
                try
                {
                    Main.instance.DrawNPC(DrawBehindTilesAlways[i], true);
                }
                catch
                {
                    Main.npc[DrawBehindTilesAlways[i]].active = false;
                }
            }

            Main.spriteBatch.End();
        }


        /// <summary>
        /// Lets you disable stairs and slope collision for this NPC by returning true
        /// </summary>
        public static event DisableSlopesDelegate DisableSlopesEvent;
        public delegate bool DisableSlopesDelegate(NPC npc);

        private void DisableSlopesAndStairs(On_NPC.orig_Collision_MoveSlopesAndStairFall orig, NPC self, bool fall)
        {
            if (DisableSlopesEvent != null)
            {
                foreach (DisableSlopesDelegate del in DisableSlopesEvent.GetInvocationList())
                    if (del(self))
                        return;
            }

            orig(self, fall);
        }


        /// <summary>
        /// Lets you override the way the collision is applied to the NPC <br/>
        /// The only thing necessary to do is to set the NPC's velocity using <see cref="Collision.TileCollision(Vector2, Vector2, int, int, bool, bool, int)"/> or <see cref="Collision.AdvancedTileCollision(bool[], Vector2, Vector2, int, int, bool, bool, int)"/> <br/>
        /// Return true to prevent vanilla collision application from being ran
        /// </summary>
        public static event ApplyCollisionDelegate ApplyCollisionEvent;
        public delegate bool ApplyCollisionDelegate(NPC npc, bool fall, Vector2 collisionPosition, int collisionWidth, int collisionHeight);
     
        private void CustomCollisionApplication(On_NPC.orig_ApplyTileCollision orig, NPC self, bool fall, Vector2 cPosition, int cWidth, int cHeight)
        {
            //Check for any custom collision and return if we found one
            if (ApplyCollisionEvent != null)
            {
                foreach (ApplyCollisionDelegate del in ApplyCollisionEvent.GetInvocationList())
                    if (del(self, fall,cPosition, cWidth, cHeight))
                        return;
            }

            orig(self, fall, cPosition, cWidth, cHeight);
        }

        /// <summary>
        /// Runs before tile collision is applied to the NPC's velocity <br/>
        /// Use this if you want the NPC to have a different tile collision shape than their full hitbox
        /// </summary>
        public static event ModifyCollisionParametersDelegate ModifyCollisionParametersEvent;
        public delegate void ModifyCollisionParametersDelegate(NPC npc, ref Vector2 collisionPosition, ref int collisionWidth, ref int collisionHeight);
        private void ModifyCollisionParameters(On_NPC.orig_GetTileCollisionParameters orig, NPC self, out Vector2 cPosition, out int cWidth, out int cHeight)
        {
            orig(self, out cPosition, out cWidth, out cHeight);
            if (ModifyCollisionParametersEvent != null)
            {
                foreach (ModifyCollisionParametersDelegate del in ModifyCollisionParametersEvent.GetInvocationList())
                    del(self, ref cPosition, ref cWidth, ref cHeight);
            }
        }

        public delegate void ModifyBossMapIconDrawingDelegate(NPC npc, ref byte alpha, ref float headScale, ref float rotation, ref SpriteEffects effects, ref int slotID, ref float x, ref float y);
        public static event ModifyBossMapIconDrawingDelegate ModifyBossMapIconDrawingEvent;
        private void ModifyBossMapIconDrawing(On_Main.orig_DrawNPCHeadBoss orig, Entity theNPC, byte alpha, float headScale, float rotation, SpriteEffects effects, int npcID, float x, float y)
        {
            NPC npc = (NPC)theNPC;
            ModifyBossMapIconDrawingEvent?.Invoke(npc, ref alpha, ref headScale, ref rotation, ref effects, ref npcID, ref x, ref y);
            orig(theNPC, alpha, headScale, rotation, effects, npcID, x, y);
        }

        public delegate void ModifyNPCLootDelegate(NPC npc, NPCLoot npcloot);
        /// <inheritdoc cref="GlobalNPC.ModifyNPCLoot(NPC, NPCLoot)"/>
        public static event ModifyNPCLootDelegate ModifyNPCLootEvent;
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            ModifyNPCLootEvent?.Invoke(npc, npcLoot);
        }

        public delegate void OnHitByProjectileDelegate(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone);
        /// <inheritdoc cref="GlobalNPC.OnHitByProjectile(NPC, Projectile, NPC.HitInfo, int)"/>
        public static event OnHitByProjectileDelegate OnHitByProjectileEvent;
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            OnHitByProjectileEvent?.Invoke(npc, projectile, hit, damageDone);
        }

        public delegate void ModifyHitByProjectileDelegate(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers);
        /// <inheritdoc cref="GlobalNPC.ModifyHitByProjectile(NPC, Projectile, ref NPC.HitModifiers)"/>
        public static event ModifyHitByProjectileDelegate ModifyHitByProjectileEvent;
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ModifyHitByProjectileEvent?.Invoke(npc, projectile, ref modifiers);
        }

        public delegate void ModifyIncomingHitDelegate(NPC npc, ref NPC.HitModifiers modifiers);
        /// <inheritdoc cref="GlobalNPC.ModifyIncomingHit(NPC, ref NPC.HitModifiers)"/>
        public static event ModifyIncomingHitDelegate ModifyIncomingHitEvent;
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            ModifyIncomingHitEvent?.Invoke(npc, ref modifiers);
        }


        public delegate void UpdateLifeRegenDelegate(NPC npc, ref int damage);
        /// <inheritdoc cref="GlobalNPC.UpdateLifeRegen(NPC, ref int)"/>
        public static event UpdateLifeRegenDelegate UpdateLifeRegenEvent;
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            UpdateLifeRegenEvent?.Invoke(npc, ref damage);
        }

        public delegate void EditSpawnRateDelegate(Player player, ref int spawnRate, ref int maxSpawns);
        /// <inheritdoc cref="GlobalNPC.EditSpawnRate(Player, ref int, ref int)"/>
        public static event EditSpawnRateDelegate EditSpawnRateEvent;
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            EditSpawnRateEvent.Invoke(player, ref spawnRate, ref maxSpawns);
        }

        public delegate void HitEffectDelegate(NPC npc, NPC.HitInfo hit);
        /// <inheritdoc cref="GlobalNPC.HitEffect(NPC, NPC.HitInfo)"/>
        public static event HitEffectDelegate HitEffectEvent;
        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            HitEffectEvent?.Invoke(npc, hit);
        }


        public delegate void NPCActionDelegate(NPC npc);
        /// <inheritdoc cref="GlobalNPC.OnKill(NPC)"/>
        public static event NPCActionDelegate OnKillEvent;
        public override void OnKill(NPC npc)
        {
            OnKillEvent?.Invoke(npc);
        }



        public delegate void ModifyHitPlayerDelegate(NPC npc, Player target, ref Player.HurtModifiers modifiers);
        /// <inheritdoc cref="GlobalNPC.ModifyHitPlayer(NPC, Player, ref Player.HurtModifiers)"/>
        public static event ModifyHitPlayerDelegate ModifyHitPlayerEvent;
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            ModifyHitPlayerEvent?.Invoke(npc, target, ref modifiers);
        }

        public delegate void ModifyHitNPCDelegate(NPC npc, NPC target, ref NPC.HitModifiers modifiers);
        /// <inheritdoc cref="GlobalNPC.ModifyHitNPC(NPC, NPC, ref NPC.HitModifiers)"/>
        public static event ModifyHitNPCDelegate ModifyHitNPCEvent;
        public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers)
        {
            ModifyHitNPCEvent?.Invoke(npc, target, ref modifiers);
        }

        /// <inheritdoc cref="GlobalNPC.AI(NPC)"/>
        public static event NPCActionDelegate AIEvent;
        public override void AI(NPC npc)
        {
            AIEvent?.Invoke(npc);
        }

        /// <inheritdoc cref="GlobalNPC.PostAI(NPC)"/>
        public static event NPCActionDelegate PostAIEvent;
        public override void PostAI(NPC npc)
        {
            PostAIEvent?.Invoke(npc);
        }

        public delegate void SpawnNPCDelegate(int npc, int tileX, int tileY);
        /// <inheritdoc cref="GlobalNPC.SpawnNPC(int, int, int)"/>
        public static event SpawnNPCDelegate SpawnNPCEvent;
        public override void SpawnNPC(int npc, int tileX, int tileY)
        {
            SpawnNPCEvent?.Invoke(npc, tileX, tileY);
        }

        public delegate void ModifyShopDelegate(NPCShop shop);
        /// <inheritdoc cref="GlobalNPC.ModifyShop(NPCShop)"/>
        public static event ModifyShopDelegate ModifyShopEvent;
        public override void ModifyShop(NPCShop shop)
        {
            ModifyShopEvent?.Invoke(shop);
        }

        public delegate bool PreDrawDelegate(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);

        /// <inheritdoc cref="GlobalNPC.PreDraw(NPC, SpriteBatch, Vector2, Color)"/>
        public static event PreDrawDelegate PreDrawEvent;
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool result = true;
            if (PreDrawEvent != null)
            {
                foreach (PreDrawDelegate eventEntry in PreDrawEvent.GetInvocationList())
                    result &= eventEntry(npc, spriteBatch, screenPos, drawColor);
            }
            return result;
        }

        public delegate void PostDrawDelegate(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);

        /// <inheritdoc cref="GlobalNPC.PostDraw(NPC, SpriteBatch, Vector2, Color)"/>
        public static event PostDrawDelegate PostDrawEvent;
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            PostDrawEvent?.Invoke(npc, spriteBatch, screenPos, drawColor);
        }

        public override bool InstancePerEntity => true;
        public override void ResetEffects(NPC npc)
        {
            MiscTags.Clear();
            foreach (var data in MiscData)
                data.Value.Reset();
        }

        public Dictionary<string, bool> MiscTags = new Dictionary<string, bool>();

        public Dictionary<Type, CustomGlobalData> MiscData = new Dictionary<Type, CustomGlobalData>();

        [Serializable]
        public abstract class SyncNPCMiscData<T> : Module where T : CustomGlobalData
        {
            public byte sender;
            public byte npcToSync;
            public T dataToSync;

            public SyncNPCMiscData(NPC npc, T data)
            {
                sender = (byte)Main.myPlayer;
                npcToSync = (byte)npc.whoAmI;
                dataToSync = data;
            }

            protected override void Receive()
            {
                NPC npc = Main.npc[npcToSync];
                npc.SetNPCData(dataToSync);

                if (Main.netMode == NetmodeID.Server)
                    Send(-1, sender, false);
            }
        }
    }
}

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        /// <summary>
        /// Writes a temporary boolean flag to this NPC, which gets reset on ResetEffects. Use <see cref="GetNPCFlag(NPC, string)"/> to retrieve the status of the flag <br/>
        /// Useable for debuff tags and such<br/>
        /// </summary>
        public static void SetNPCFlag(this NPC npc, string flagKey)
        {
            npc.GetGlobalNPC<FablesNPC>().MiscTags[flagKey] = true;
        }

        /// <summary>
        /// Reads if the NPC possesses the temporary flag set in <see cref="SetNPCFlag(NPC, string)"/><br/>
        /// Useable for debuff tags and such.
        /// </summary>
        public static bool GetNPCFlag(this NPC npc, string flagKey)
        {
            if (npc.GetGlobalNPC<FablesNPC>().MiscTags.TryGetValue(flagKey, out bool flag))
                return flag;
            return false;
        }

        /// <summary>
        /// Writes custom data in the form of an object to this NPC.<br/>
        /// This data doesn't get cleared on ResetEffects, but you can override <see cref="CustomGlobalData.Reset"/> to reset values 
        /// Can be read back with <see cref="GetNPCData{T}(NPC, out T)"/>
        /// </summary>
        /// <typeparam name="T">The class of the data object we're looking for</typeparam>
        /// <param name="npc"></param>
        /// <param name="data">The data to write</param>
        public static void SetNPCData<T>(this NPC npc, T data) where T : CustomGlobalData
        {
            npc.GetGlobalNPC<FablesNPC>().MiscData[typeof(T)] = data;
        }

        /// <summary>
        /// Reads the custom data class written to this NPC with <see cref="SetNPCData{T}(NPC, T)"/>.<br/>
        /// This data doesn't get cleared on ResetEffects, but you can override <see cref="CustomGlobalData.Reset"/> to reset values 
        /// </summary>
        /// <typeparam name="T">The class of the data object we're looking for</typeparam>
        /// <param name="npc"></param>
        /// <param name="result">The retrieved data, if any</param>
        /// <returns>If there was any data of this type written to the NPC</returns>
        public static bool GetNPCData<T>(this NPC npc, out T result) where T : CustomGlobalData
        {
            result = default(T);
            if (npc.GetGlobalNPC<FablesNPC>().MiscData.TryGetValue(typeof(T), out CustomGlobalData objResult))
            {
                result = (T)objResult;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sends the custom NPC data written to this NPC with <see cref="SetNPCData{T}(NPC, T){T}(Player, T)"/> across the net<br/>
        /// Doesn't sync if the NPC doesn't have any data written to them
        /// </summary>
        /// <remarks>The global data being synced needs to have the serializable attribute to work at all </remarks>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="npc"></param>
        public static void SyncNPCData<T, T2>(this NPC npc) where T : CustomGlobalData where T2 : SyncNPCMiscData<T>
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            if (!npc.GetGlobalNPC<FablesNPC>().MiscData.TryGetValue(typeof(T), out CustomGlobalData objResult))
                return;

            T2 packet = (T2)Activator.CreateInstance(typeof(T2), npc, (T)objResult);

            packet.Send(runLocally: false);
        }

        /// <summary>
        /// Retrieves the main entity of a segmented NPC. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="iframesProvider"></param>
        /// <returns>
        /// False if this NPC is not segmented or is the main entity itself.
        /// </returns>
        public static bool GetIFramesProvider(this NPC target, out int iframesProvider)
        {
            iframesProvider = target.whoAmI;
            if (target.realLife == -1)
            {
                //Eow
                if (target.aiStyle == NPCAIStyleID.Worm)
                {
                    int nextSegment = (int)target.ai[1];

                    if (target.type == NPCID.EaterofWorldsHead)
                        return false;

                    while (nextSegment >= 0 && nextSegment < Main.maxNPCs && Main.npc[nextSegment].active && Main.npc[nextSegment].ai[0] == iframesProvider)
                    {
                        iframesProvider = nextSegment;
                        nextSegment = (int)Main.npc[nextSegment].ai[1];

                        if (Main.npc[nextSegment].type == NPCID.EaterofWorldsHead)
                            break;
                    }
                    return true;
                }
                return false;
            }

            iframesProvider = target.realLife;
            return true;
        }
    }
}