using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Generation;
using Terraria.Utilities;
using Terraria.GameContent.Achievements;
using static CalamityFables.Helpers.FablesUtils;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using System.IO;

namespace CalamityFables.Core
{
    public class FablesGeneralSystemHooks : ModSystem
    {
        public override void Load()
        {
            On_TrackGenerator.IsLocationInvalid += AddExtraMineshaftChecks;
            On_NPC.GetShimmered += CustomShimmerInteraction;
            On_Main.DrawInterface_Resources_Buffs += HideUnwantedBuffsDetour;
            IL_Main.DrawInterface_Resources_Buffs += HideUnwantedBuffs;
            On_Main.UpdateAtmosphereTransparencyToSkyColor += LogPreAtmosphereSkyColor;
            On_Main.TeleportEffect += CustomTeleportationEffects;
            IL_Player.PickTile += AddTileMineHook;
            IL_Main.DoDraw += AddOverInterfaceDrawHook;
            On_WorldGen.SwitchMB += ModifyMusicBoxSwitch;
            On_Main.UpdateTime_StartDay += DayStartReset;
            On_Main.UpdateTime_StartNight += NightStartReset;
            On_TileObject.DrawPreview += PreDrawTilePreview;
        }


        public delegate void ModifyTilePreviewDrawDelegate(TileObjectPreviewData preview, ref Vector2 position);
        public static event ModifyTilePreviewDrawDelegate ModifyTilePreviewDrawEvent;
        private void PreDrawTilePreview(On_TileObject.orig_DrawPreview orig, SpriteBatch sb, TileObjectPreviewData op, Vector2 position)
        {
            ModifyTilePreviewDrawEvent?.Invoke(op, ref position);
            orig(sb, op, position);
        }

        public delegate bool GrowModdedSaplingDelegate(int x, int y, bool underground, bool needsSapling = true, int generateHeightOverride = 0);
        public static EventSet<GrowModdedSaplingDelegate> FertilizeSaplingEvent = new();

        public static event Action OnNightStart;
        private void NightStartReset(On_Main.orig_UpdateTime_StartNight orig, ref bool stopEvents)
        {
            orig(ref stopEvents);
            OnNightStart?.Invoke();
        }

        public static event Action OnDayStart;
        private void DayStartReset(On_Main.orig_UpdateTime_StartDay orig, ref bool stopEvents)
        {
            orig(ref stopEvents);
            OnDayStart?.Invoke();
        }

        private void AddOverInterfaceDrawHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdcI4(37),
                i => i.MatchCall(typeof(TimeLogger).GetMethod("DetailedDrawTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))))
            {
                FablesUtils.LogILEpicFail("Add a hook for above interface drawing", "Coud not locate DetailedDrawTime");
                return;
            }

            cursor.EmitDelegate(OverInterfaceDrawing);
        }

        public static event Action DrawOverInterface;
        internal static void OverInterfaceDrawing() => DrawOverInterface?.Invoke();


        public static Color AtmospherelessColorOfTheSkies;
        private void LogPreAtmosphereSkyColor(On_Main.orig_UpdateAtmosphereTransparencyToSkyColor orig)
        {
            AtmospherelessColorOfTheSkies = Main.ColorOfTheSkies;
            orig();
        }

        private static int culledDebuffs = 0;
        private void HideUnwantedBuffsDetour(On_Main.orig_DrawInterface_Resources_Buffs orig, Main self)
        {
            culledDebuffs = 0;
            orig(self);
        }

        private void HideUnwantedBuffs(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel skipLoopLabel = null;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>("player"),
                i => i.MatchLdsfld<Main>("myPlayer"),
                i => i.MatchLdelemRef(),
                i => i.MatchLdfld<Player>("buffType"),
                i => i.MatchLdloc(3),
                i => i.MatchLdelemI4(),
                i => i.MatchLdcI4(0),
                i => i.MatchBle(out skipLoopLabel)))
            {
                FablesUtils.LogILEpicFail("Hide buffs", "Could not locate the Main.player[Main.myPlayer].buffType[i] > 0");
                return;
            }

            cursor.Emit(OpCodes.Ldloc_3);
            cursor.EmitDelegate(IsBuffVisibleExtra);
            cursor.Emit(OpCodes.Brfalse_S, skipLoopLabel);

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(32),
                i => i.MatchLdloc(3)))
            {
                FablesUtils.LogILEpicFail("Hide buffs", "Could not locate the x position calculation for the buff");
                return;
            }

            //Substract the culled debuffs from the base X pos
            cursor.Emit<FablesGeneralSystemHooks>(OpCodes.Ldsfld, "culledDebuffs");
            cursor.Emit(OpCodes.Sub);

            int column = 4;
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(76),
                i => i.MatchStloc(out _),
                i => i.MatchLdloc(3),
                i => i.MatchStloc(out column)))
            {
                FablesUtils.LogILEpicFail("Hide buffs", "Could not locate the Y row position calculation for the buff");
                return;
            }

            cursor.Emit(OpCodes.Ldloc, column);
            cursor.Emit<FablesGeneralSystemHooks>(OpCodes.Ldsfld, "culledDebuffs");
            cursor.Emit(OpCodes.Sub);
            cursor.Emit(OpCodes.Stloc, column);
        }

        public delegate bool BuffCheckDelegate(int index);
        public static event BuffCheckDelegate BuffVisibilityChecks;
        internal bool IsBuffVisibleExtra(int index)
        {
            if (BuffVisibilityChecks == null)
                return true;

            bool visible = true;
            foreach (BuffCheckDelegate check in BuffVisibilityChecks.GetInvocationList())
                visible &= check(index);

            if (!visible)
                culledDebuffs++;

            return visible;
        }

        private void AddTileMineHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            int originalTileTypeIndex = il.MakeLocalVariable<ushort>();
            int originalTileIndex = 0;

            //Get the index of the mined tile local variable
            if (!cursor.TryGotoNext(MoveType.After, 
                i => i.MatchLdarg(1),
                i => i.MatchLdarg(2),
                i => i.MatchCall<Tilemap>("get_Item"),
                i => i.MatchStloc(out originalTileIndex)
                ))
            {
                LogILEpicFail("Add mine tile hook", "Could not locate the mined tile's local variable index");
                return;
            }

            //Cache the mined tile's type
            cursor.Emit(OpCodes.Ldloca_S, (byte)originalTileIndex);
            cursor.Emit<Tile>(OpCodes.Call, "get_type");
            cursor.Emit(OpCodes.Ldind_U2);
            cursor.Emit(OpCodes.Stloc, (short)originalTileTypeIndex);


            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<AchievementsHelper>("HandleMining")))
            {
                LogILEpicFail("Add mine tile hook", "Could not locate the HandleMining call");
                return;
            }

            cursor.Emit(OpCodes.Ldarg, 1);
            cursor.Emit(OpCodes.Ldarg, 2);
            cursor.Emit(OpCodes.Ldloc, (short)originalTileTypeIndex);
            cursor.EmitDelegate(MineTileHook);
        }


        //Player, pickaxe and tile provided for convenience instead of having to retrieve them for each individual delegate in the event
        public delegate void MineTileDelegate(Player player, Item pickaxe, int x, int y, Tile tile, int originalTileType);
        public static event MineTileDelegate OnMineTile;
        public static void MineTileHook(int x, int y, ushort originalTileType)
        {
            Player player = Main.LocalPlayer;
            Item pickaxe = player.inventory[player.selectedItem];
            Tile tile = Main.tile[x, y];
            OnMineTile?.Invoke(player, pickaxe, x, y, tile, originalTileType);
        }

        public delegate bool CustomTeleportationEffect(Rectangle effectRect, int Style, int extraInfo, float dustCountMult, TeleportationSide side, Vector2 otherPosition);
        public static event CustomTeleportationEffect CustomTeleportationEffectEvent;
        private void CustomTeleportationEffects(On_Main.orig_TeleportEffect orig, Rectangle effectRect, int Style, int extraInfo, float dustCountMult, TeleportationSide side, Vector2 otherPosition)
        {
            if (Style > 13 && CustomTeleportationEffectEvent != null)
            {
                foreach (CustomTeleportationEffect effectDelegate in CustomTeleportationEffectEvent.GetInvocationList())
                {
                    if (effectDelegate(effectRect, Style, extraInfo, dustCountMult, side, otherPosition))
                        return;
                }
            }

            orig(effectRect, Style, extraInfo, dustCountMult, side, otherPosition);
        }


        public delegate bool ModifyMusicBoxSwitchDelegate(int i, int j);
        /// <summary>
        /// Event called when a music box tile is toggled. Return true to prevent vanilla code from being ran <br/>
        /// Use <see cref="FablesUtils.MultiWayMusicBoxSwitch"/> for music boxes with more than one activated state
        /// </summary>
        public static event ModifyMusicBoxSwitchDelegate SwitchMusicBox;
        private void ModifyMusicBoxSwitch(On_WorldGen.orig_SwitchMB orig, int i, int j)
        {
            foreach (ModifyMusicBoxSwitchDelegate del in SwitchMusicBox.GetInvocationList())
            {
                if (del(i, j))
                    return;
            }
            orig(i, j);
        }

        #region Worldgen hooks
        public delegate bool IsLocationInvalidDelegate(int x, int y);
        public static event IsLocationInvalidDelegate AdditionalMineshaftChecks;

        private bool AddExtraMineshaftChecks(On_TrackGenerator.orig_IsLocationInvalid orig, int x, int y)
        {
            //If we can't generate a mineshaft there already, don't bother with the extra checks
            if (orig(x, y))
                return true;

            //If we don't have any extra checks, return falsee
            if (AdditionalMineshaftChecks == null)
                return false;

            //Check all our extra checks individually
            foreach (IsLocationInvalidDelegate extraCheck in AdditionalMineshaftChecks.GetInvocationList())
            {
                if (extraCheck(x, y))
                    return true;
            }

            return false;
        }

        #endregion

        public delegate void NPCAction(NPC npc);
        public static event NPCAction CustomShimmerEffects;
        private void CustomShimmerInteraction(On_NPC.orig_GetShimmered orig, NPC self)
        {
            orig(self);
            CustomShimmerEffects?.Invoke(self);
        }

        public static FastRandom fastRNG = FastRandom.CreateWithRandomSeed();
        public static void SetRNGCoords(int x, int y) => fastRNG = fastRNG.WithModifier(x, y);

        #region Base events

        public delegate bool HijackGetDataDelegate(ref byte messageID, ref BinaryReader binaryReader, int playerNumber);
        public static event HijackGetDataDelegate HijackGetDataEvent;
        public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
        {
            if (HijackGetDataEvent == null)
                return false;
            foreach (HijackGetDataDelegate hijack in HijackGetDataEvent.GetInvocationList())
                if (hijack(ref messageType, ref reader, playerNumber))
                    return true;

            return false;
        }


        public static event Action PostUpdateTimeEvent;
        public override void PostUpdateTime()
        {
            PostUpdateTimeEvent?.Invoke();
            fastRNG.NextSeed();
        }

        public static event Action PostSetupContentEvent;
        public delegate void AddBossChecklistEntry(Mod bossChecklist);
        public static event AddBossChecklistEntry LogBossChecklistEvent;

        public override void PostSetupContent()
        {
            PostSetupContentEvent?.Invoke();
            FablesSets.PostSetupContent();

            if (ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist))
            {
                LogBossChecklistEvent?.Invoke(bossChecklist);
            }
        }

        public static event Action PreUpdateNPCsEvent;
        public override void PreUpdateNPCs()
        {
            PreUpdateNPCsEvent?.Invoke();
        }

        public static event Action PreUpdateProjectilesEvent;
        public override void PreUpdateProjectiles()
        {
            PreUpdateProjectilesEvent?.Invoke();
        }       


        public static event Action PostUpdateEverythingEvent;
        public override void PostUpdateEverything()
        {
            PostUpdateEverythingEvent?.Invoke();
        }

        public static event Action PostUpdateNPCEvent;
        public override void PostUpdateNPCs()
        {
            PostUpdateNPCEvent?.Invoke();
        }

        public static event Action ClearWorldEvent;
        public override void ClearWorld()
        {
            ClearWorldEvent?.Invoke();
        }

        public delegate void TagCompoundDelegate(TagCompound tag);
        public static event TagCompoundDelegate SaveWorldDataEvent;
        public override void SaveWorldData(TagCompound tag)
        {
            SaveWorldDataEvent?.Invoke(tag);
        }

        public static event TagCompoundDelegate LoadWorldDataEvent;
        public override void LoadWorldData(TagCompound tag)
        {
            LoadWorldDataEvent?.Invoke(tag);
        }


        public delegate void NetSendDelegate(BinaryWriter writer);
        public static event NetSendDelegate NetSendEvent;
        public override void NetSend(BinaryWriter writer)
        {
            NetSendEvent?.Invoke(writer);
        }

        public delegate void NetReceiveDelegate(BinaryReader reader);
        public static event NetReceiveDelegate NetReceiveEvent;
        public override void NetReceive(BinaryReader reader)
        {
            NetReceiveEvent?.Invoke(reader);
        }

        public static event Action ResetNearbyTileEffectsEvent;
        public override void ResetNearbyTileEffects()
        {
            ResetNearbyTileEffectsEvent?.Invoke();
        }
        #endregion
    }
}
