using CalamityFables.Content.UI;
using CalamityFables.Core.Visuals;
using MonoMod.Cil;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using static CalamityFables.Core.FablesPlayer;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public partial class FablesPlayer : ModPlayer
    {
        public override void Load()
        {
            MiscDataNetIDCounter = 0;
            On_Player.QuickGrapple += AddHook_OverrideGrapple;
            On_Player.UpdateItemDye += UpdateCustomLayerDyes;
            On_Player.UpdateVisibleAccessory += UpdateVisibleAccessory;
            IL_Player.KillMe += ACTUALLY_HideGores;
            On_Player.FigureOutWhatToPlace += OverridePlacedTile;
            On_Player.ItemCheck_ApplyHoldStyle += DisableHoldStyle;
            IL_Player.PlaceThing_TryReplacingTiles += OverridePlacedTileWhenReplacing;
            On_Player.ItemCheck_UseMiningTools_TryPoundingTile += OverrideTilePound;
            On_Player.DoesPickTargetTransformOnKill += DoesTileTransformOnHit;
            On_Player.PlaceThing_PaintScrapper_LongMoss += ScrapeTileInteraction;
            IL_Player.PlaceThing_Tiles += AllowTileOverriding;
            IL_WorldGen.PlaceTile += AllowTileOverridingPlaceTile;
            On_Player.PlaceThing_Tiles_BlockPlacementIfOverPlayers += BlockSolidMultitilesOverPlayers;
            IL_Player.UpdateManaRegen += IL_Player_UpdateManaRegen;

            On_Player.JumpMovement += On_Player_JumpMovement;
            On_Player.StopExtraJumpInProgress += ResetJump;
            On_Player.CancelAllJumpVisualEffects += ResetJump2;
            On_Player.UpdateManaRegen += On_Player_UpdateManaRegen;
            FablesDrawLayers.DrawThingsBehindSolidTilesAndBackgroundNPCsEvent += DrawMultiAnchorPlacementPreview;

            //On_Player.Hurt_HurtInfo_bool += DebugText;
        }

        #region Custom hooks
        public static event PlayerActionDelegate PreJumpMovement;
        public static event PlayerActionDelegate PostJumpMovement;
        private void On_Player_JumpMovement(On_Player.orig_JumpMovement orig, Player self)
        {
            PreJumpMovement?.Invoke(self);
            Player.jumpSpeed += Math.Abs(self.Fables().platformJumpMomentumShare);
            orig(self);
            PostJumpMovement?.Invoke(self);
            DetachFromPlatformOnJump(self);
        }

        public static event PlayerActionDelegate OnJumpEnd;
        private void ResetJump(On_Player.orig_StopExtraJumpInProgress orig, Player self)
        {
            orig(self);
            self.Fables().platformJumpMomentumShare = 0f;
            OnJumpEnd?.Invoke(self);
        }
        private void ResetJump2(On_Player.orig_CancelAllJumpVisualEffects orig, Player self)
        {
            orig(self);
            self.Fables().platformJumpMomentumShare = 0f;
            OnJumpEnd?.Invoke(self);
        }

        public delegate bool DisableItemHoldStyleDelegate(Player player, Item item, Rectangle heldItemFrame);
        /// <summary>
        /// Use this to disable effects that happen when holding an item. Return true to prevent the held item from having its effects
        /// </summary>
        public static event DisableItemHoldStyleDelegate DisableItemHoldEvent;

        private void DisableHoldStyle(On_Player.orig_ItemCheck_ApplyHoldStyle orig, Player self, float mountOffset, Item sItem, Rectangle heldItemFrame)
        {
            if (DisableItemHoldEvent != null)
            {
                foreach (DisableItemHoldStyleDelegate holdCheck in DisableItemHoldEvent.GetInvocationList())
                    if (holdCheck(self, sItem, heldItemFrame))
                        return;
            }
            orig(self, mountOffset, sItem, heldItemFrame);
        }

        public delegate void GetPlacementOverlapCheckDimensionsDelegate(ref int width, ref int height);
        /// <summary>
        /// Ran before placing a multitile. Use this to set the width and height of solid multitiles that shoudln't be placed over the player
        /// </summary>
        public static EventSet<GetPlacementOverlapCheckDimensionsDelegate> GetPlacementOverlapCheckDimensionsEvent = new();

        private void BlockSolidMultitilesOverPlayers(On_Player.orig_PlaceThing_Tiles_BlockPlacementIfOverPlayers orig, ref bool canPlace, ref TileObject data)
        {
            orig(ref canPlace, ref data);
            int width = 0;
            int height = 0;

            if (!GetPlacementOverlapCheckDimensionsEvent.TryGetInvocation(data.type, out GetPlacementOverlapCheckDimensionsDelegate del))
                return;

            del(ref width, ref height);

            if (width == 0 || height == 0)
                return;

            width *= 16;
            height *= 16;

            int x = data.xCoord * 16;
            int y = data.yCoord * 16;

            Rectangle value = new Rectangle(x, y, width, height);
            for (int i = 0; i < 255; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && !player.ghost && player.Hitbox.Intersects(value))
                {
                    canPlace = false;
                    break;
                }
            }
        }


        public delegate void OverridePlacedTileDelegate(Player player, Tile targetTile, Item item, ref int tileToPlace, ref int previewPlaceStyle, ref bool? overrideCanPlace);
        
        /// <summary>
        /// Allows you to override what kind of tile gets placed by this item, depending on the item, nearby tiles, etc... Used by hookstaff to switch between wall and ground tile types
        /// </summary>
        public static event OverridePlacedTileDelegate OverridePlacedTileEvent;

        private void OverridePlacedTile(On_Player.orig_FigureOutWhatToPlace orig, Player self, Tile targetTile, Item sItem, out int tileToCreate, out int previewPlaceStyle, out bool? overrideCanPlace, out int? forcedRandom)
        {
            orig(self, targetTile, sItem, out tileToCreate, out previewPlaceStyle, out overrideCanPlace, out forcedRandom);
            OverridePlacedTileEvent?.Invoke(self, targetTile, sItem, ref tileToCreate, ref previewPlaceStyle, ref overrideCanPlace);
        }

        private void OverridePlacedTileWhenReplacing(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel afterGrateCheckLabel = cursor.DefineLabel();
            int placedTileTypeIndex = 8;

            //Go before the grate check, since we're gonna have to skip it
            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg0(),
                i => i.MatchCall<Player>("get_UsingBiomeTorches"),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdloc(out placedTileTypeIndex),
                i => i.MatchLdcI4(4),
                i => i.MatchBneUn(out ILLabel _))) //We don't care about this label
            {
                FablesUtils.LogILEpicFail("Allow tile override when replacing tiles", "Could not locate UsingBiomeTorches");
                return;
            }

            cursor.EmitLdarg0();
            cursor.EmitLdloca(placedTileTypeIndex);
            cursor.EmitDelegate(ModifyPlacedTileTypeWhenReplacing);
        }

        private static void ModifyPlacedTileTypeWhenReplacing(Player player, ref int type)
        {
            int nothing = 0;
            bool? nothing2 = false;
            OverridePlacedTileEvent?.Invoke(player, Main.tile[player.TileTarget()], player.HeldItem, ref type, ref nothing, ref nothing2);
        }


        private void AllowTileOverriding(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel okayForPlacement = null;

            //Go before the grate check, since we're gonna have to skip it
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>("tileMoss"),
                i => i.MatchLdloc(1),
                i => i.MatchLdelemU1(),
                i => i.MatchBrtrue(out okayForPlacement)))
            {
                FablesUtils.LogILEpicFail("Allow grasslike tile replacement in Player.PlaceThing_Tiles", "Could not locate tileMoss check");
                return;
            }

            //Make it so if our tile is a custom placement tile, we don't care if theres already a tile where were trying to place it
            cursor.EmitLdloc(1);
            cursor.EmitDelegate(AllowTileOverriding_IsTileSeedLike);
            cursor.EmitBrtrue(okayForPlacement);
        }
        private static bool AllowTileOverriding_IsTileSeedLike(int tileType) => FablesSets.PlacesLikeGrassSeeds[tileType];


        private void AllowTileOverridingPlaceTile(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel okayForPlacement = null;

            if (!cursor.TryGotoNext(MoveType.After,
               i => i.MatchLdarg(4),
               i => i.MatchBrtrue(out okayForPlacement)))
            {
                FablesUtils.LogILEpicFail("Allow grasslike tile replacement in WorldGen.PlaceTile", "Could not locate forced check");
                return;
            }


            cursor.EmitLdarg(2);
            cursor.EmitLdarg(0);
            cursor.EmitLdarg(1);
            cursor.EmitDelegate(AllowTileOverriding_ValidTileForOverride);
            cursor.EmitBrtrue(okayForPlacement);
        }



        public delegate bool CanPlaceOverTileDelegate(int x, int y);
        /// <summary>
        /// When used in conjunction with <see cref="FablesSets.PlacesLikeGrassSeeds"/>, allows a tile to be placed over an existing one without having to break it first
        /// </summary>
        public static EventSet<CanPlaceOverTileDelegate> CanPlaceOverTileEvent = new();
        private static bool AllowTileOverriding_ValidTileForOverride(int placeType, int x, int y)
        {
            if (!FablesSets.PlacesLikeGrassSeeds[placeType] || !CanPlaceOverTileEvent.TryGetInvocation(placeType, out CanPlaceOverTileDelegate call))
                return false;
            return call(x, y);
        }

        public delegate bool TryTransformWhenHitDelegate(Player player, HitTile hitCounter, int damage, int x, int y, int pickPower, int bufferIndex, Tile tileTarget);
        public static EventSet<TryTransformWhenHitDelegate> TryTransformWhenHitEvent = new();
        private bool DoesTileTransformOnHit(On_Player.orig_DoesPickTargetTransformOnKill orig, Player self, HitTile hitCounter, int damage, int x, int y, int pickPower, int bufferIndex, Tile tileTarget)
        {
            if (orig(self, hitCounter, damage, x, y, pickPower, bufferIndex, tileTarget))
                return true;

            if (TryTransformWhenHitEvent != null && TryTransformWhenHitEvent.TryGetInvocation(tileTarget.TileType, out TryTransformWhenHitDelegate hitEffect))
            {
                return hitEffect(self, hitCounter, damage, x, y, pickPower, bufferIndex, tileTarget);
            }

            return false;
        }


        public delegate void ScrapeTileDelegate(Player player, int x, int y);
        public static EventSet<ScrapeTileDelegate> ScrapeTileEvent = new();
        private void ScrapeTileInteraction(On_Player.orig_PlaceThing_PaintScrapper_LongMoss orig, Player self, int x, int y)
        {
            orig(self, x, y);
            if (ScrapeTileEvent != null && ScrapeTileEvent.TryGetInvocation(Main.tile[x, y].TileType, out ScrapeTileDelegate scrapeEffect))
            {
                self.cursorItemIconEnabled = true;
                if (!self.ItemTimeIsZero || self.itemAnimation <= 0 || !self.controlUseItem)
                    return;

                scrapeEffect(self, x, y);
            }
        }

        public delegate bool OverrideGrappleDelegate(Player player);
        /// <summary>
        /// Return true to stop vanilla logic / other hook logic from happening. Return false to let vanilla logic happen.
        /// </summary>
        public static event OverrideGrappleDelegate OverrideGrappleEvent;
        private void AddHook_OverrideGrapple(On_Player.orig_QuickGrapple orig, Player self)
        {

            if (OverrideGrappleEvent != null)
            {
                foreach (OverrideGrappleDelegate grappleCheck in OverrideGrappleEvent.GetInvocationList())
                    if (grappleCheck(self))
                        return;
            }

            orig(self);
        }

        /// <summary>
        /// Performs vanilla logic associated with quick hooking
        /// This includes checking if the player is CCed in any way, dismounting the player, doing ProjectileLoader checks, updating blacklisted tiles, and despawning other hooks
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hookType"></param>
        /// <returns></returns>
        public static bool BasicGrappleChecks(Player player, int hookType, bool despawnOtherHooks)
        {
            if (player.frozen || player.tongued || player.webbed || player.stoned || player.dead)
                return false;

            if (player.mount.Active)
                player.mount.Dismount(player);

            if (ProjectileLoader.CanUseGrapple(hookType, player) is bool modCanGrapple)
            {
                if (!modCanGrapple)
                    return false;
            }

            player.UpdateBlacklistedTilesForGrappling();

            //Clear any previous hooks
            if (despawnOtherHooks)
            {
                for (int i = 0; i < Main.maxProjectiles; ++i)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.owner != player.whoAmI || p.aiStyle != 7 || p.type == hookType)
                        continue;

                    p.Kill();
                }
            }

            return true;
        }

        /// <summary>
        /// Called right before <see cref="Player.UpdateManaRegen"/>.
        /// </summary>
        public static event PlayerActionDelegate UpdateManaRegenEvent;

        /// <summary>
        /// Called after <see cref="Player.UpdateManaRegen"/> upon reaching maximum nana.
        /// </summary>
        public static event PlayerActionDelegate OnAchieveFullMana;
        private void On_Player_UpdateManaRegen(On_Player.orig_UpdateManaRegen orig, Player self)
        {
            // Check that player was not at max mana before
            bool maxMana = self.statMana == self.statManaMax2;

            // Run regen methods
            UpdateManaRegenEvent?.Invoke(self);
            orig(self);

            if (!maxMana && self.statMana == self.statManaMax2 && self.whoAmI == Main.myPlayer)
                OnAchieveFullMana?.Invoke(self);
        }

        private void IL_Player_UpdateManaRegen(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // Target: this.manaRegen = (int)((double)((float)this.manaRegen * num2) * 1.15);
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdloc1()))
            {
                FablesUtils.LogILEpicFail("Update Mana Regen", "Could not locate manaRegen multiplier");
                return;
            }

            // Remove instruction and replace with delegate
            cursor.Remove();
            cursor.EmitLdarg0();
            cursor.EmitLdloc1();
            cursor.EmitDelegate(ModifyManaRegenMultiplier);
        }

        public delegate void ModifyManaRegenMultiplierDelegate(Player player, ref float manaRegenMultiplier);

        /// <summary>
        /// Can be used to add a multiplier to a player's mana regeneration. <br/>
        /// Typically equals mana / maxMana * 0.8 + 0.2 and is set to 1 when <see cref="Player.manaRegenBuff"/> is active.
        /// </summary>
        public static event ModifyManaRegenMultiplierDelegate ModifyManaRegenMultiplierEvent;
        private float ModifyManaRegenMultiplier(Player player, float manaRegenMultiplier)
        {
            ModifyManaRegenMultiplierEvent?.Invoke(player, ref manaRegenMultiplier);
            return manaRegenMultiplier;
        }

        /*
        private void DebugText(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet)
        {
            CalamityFables.Instance.Logger.Debug("Player hurt " + self.name);
            orig(self, info, quiet);
        }
        */

        public delegate bool PoundOverrideDelegate(Player player, Item item, int tileHitType, ref bool hitWall, int x, int y);
        /// <summary>
        /// Events that will be checked for on every tile being pounded. Use this when you need to prevent any tile from being pounded in a specific scenario
        /// </summary>
        public static event PoundOverrideDelegate OverrideTilePoundEvent;
        /// <summary>
        /// Event set that will check for a specific tile
        /// </summary>
        public static EventSet<PoundOverrideDelegate> TileSpecificPoundOverrideEvent = new();

        private void OverrideTilePound(On_Player.orig_ItemCheck_UseMiningTools_TryPoundingTile orig, Player self, Item sItem, int tileHitId, ref bool hitWall, int x, int y)
        {
            if (Main.tile[x, y].HasTile)
            {
                int hitTileType = Main.tile[x, y].TileType;

                if (OverrideTilePoundEvent != null)
                {
                    foreach (PoundOverrideDelegate poundCheck in OverrideTilePoundEvent.GetInvocationList())
                        if (poundCheck(self, sItem, hitTileType, ref hitWall, x, y))
                            return;
                }

                PoundOverrideDelegate poundEvent = TileSpecificPoundOverrideEvent.GetInvocation(hitTileType);
                if (poundEvent != null && poundEvent(self, sItem, hitTileType, ref hitWall, x, y))
                    return;
            }

            orig(self, sItem, tileHitId, ref hitWall, x, y);
        }

        #endregion

        #region Fix for player gores being visible even with hidegores
        private void ACTUALLY_HideGores(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            int genGoreVariableIndex = 0;
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("stoned"),
                i => i.MatchBrtrue(out var _),
                i => i.MatchLdloc(out genGoreVariableIndex),
                i => i.MatchBrtrue(out var _)
                ))
            {
                FablesUtils.LogILEpicFail("Properly hide gores", "stoned || !genGore");
                return;
            }

            cursor.Emit(Ldarg_0);
            cursor.EmitDelegate(MakePlayerGoresZoomPast);
        }

        public void MakePlayerGoresZoomPast(Player player)
        {
            player.bodyVelocity = Vector2.One * 10000f;
            player.legVelocity = Vector2.One * 10000f;
            player.headVelocity = Vector2.One * 10000f;
        }
        #endregion

        /// <summary>
        /// Modifier for the player's movement speed that works as a stat modifier <br/>
        /// Applied multiplicatively during <see cref="ModPlayer.PostUpdateMiscEffects"/>
        /// </summary>
        public StatModifier MoveSpeedModifier;
        public int FrameStartMana { get; private set; }
        public float FrameStartMinionSlots { get; private set; }


        public Point lastTileTarget;
        public int lastFlexibleTileWandCycleOffset = 0;

        public static event PlayerActionDelegate ResetEffectsEvent;
        public override void ResetEffects()
        {
            ResetCustomHurtSound();

            if (Main.myPlayer == Player.whoAmI && !Player.isDisplayDollOrInanimate)
                FablesResourceOverlay.CurrentOverlayPriority = 0f;

            MoveSpeedModifier = StatModifier.Default;
            cachedHeadRotation = 0;
            FrameStartMana = Player.statMana;
            FrameStartMinionSlots = Player.slotsMinions;
            backEquipVanity = null;
            backEquipDye = 0;
            gogglesDye = 0;
            JustHurtTimer = Math.Max(0, JustHurtTimer - 1 / 60f);

            if (multiAnchorItem != null && Player.HeldItem != multiAnchorItem)
            {
                multiAnchorItem = null;
                multiAnchorPlaceCount = 0;
                multiAnchorPlacePoints.Clear();
            }
            else if (multiAnchorItem != null && multiAnchorItem.IsAir)
            {
                multiAnchorItem = null;
                multiAnchorPlaceCount = 0;
                multiAnchorPlacePoints.Clear();
            }

            if (multiAnchorItem != null && multiAnchorPlaceCount > 0)
            {
                bool movedTileTarget = lastTileTarget != Player.TileTarget();
                if (movedTileTarget || lastFlexibleTileWandCycleOffset != Player.FlexibleWandCycleOffset)
                {
                    (multiAnchorItem.ModItem as IMultiAnchorPlaceable).UpdatePreview(multiAnchorPlaceCount, multiAnchorPlacePoints, movedTileTarget);
                }
            }

            lastTileTarget = Player.TileTarget();
            lastFlexibleTileWandCycleOffset = Player.FlexibleWandCycleOffset;

            justPlacedMultiAnchorItem = false;

            foreach (string key in AccessoryVariables.Keys)
                AccessoryVariables[key] = false;
            foreach (int key in AccessoryItems.Keys)
                AccessoryItems[key] = null;
            foreach (var data in MiscData)
                data.Value.Reset();

            platformMomentumCarryBuffer--;
            if (platformMomentumCarryBuffer < 0)
            {
                platformMomentumCarryBuffer = 0;
                highestPlatformYvelocity = 0;
            }

            ResetEffectsEvent?.Invoke(Player);
        }



        public static event PlayerActionDelegate UpdateDeadEvent;
        public override void UpdateDead()
        {
            //if (Player.respawnTimer > 60 * 7)
            //    Player.respawnTimer = 60 * 7;

            foreach (string key in AccessoryVariables.Keys)
                AccessoryVariables[key] = false;
            foreach (int key in AccessoryItems.Keys)
                AccessoryItems[key] = null;
            foreach (var data in MiscData)
                data.Value.Reset();

            multiAnchorItem = null;
            multiAnchorPlaceCount = 0;
            multiAnchorPlacePoints.Clear();

            JustHurtTimer = 0;
            platformJumpMomentumShare = 0f;
            platformMomentumCarryBuffer = 0;
            highestPlatformYvelocity = 0;

            UpdateDeadEvent?.Invoke(Player);
        }

        public Dictionary<string, bool> AccessoryVariables = new Dictionary<string, bool>();
        public Dictionary<int, Item> AccessoryItems = new Dictionary<int, Item>();
        public Dictionary<Type, CustomGlobalData> MiscData = new Dictionary<Type, CustomGlobalData>();

        private static Dictionary<Type, ushort> MiscDataSync_TypeToNetID = [];
        private static Dictionary<ushort, Type> MiscDataSync_NetIDToType = [];
        private static ushort MiscDataNetIDCounter = 0;

        /// <summary>
        /// Registers a custom global data net ID to be used with <see cref="FablesUtils.SyncPlayerData{T}(Player)"/><br/>
        /// </summary>
        /// <remarks>
        /// This will not work if the global data being registered doesn't have the Serializable attribute!
        /// </remarks>
        /// <param name="dataTypeToSync">The data type to sync</param>
        public static void ReserveCustomGlobalDataNetID(Type dataTypeToSync)
        {
            MiscDataSync_TypeToNetID[dataTypeToSync] = MiscDataNetIDCounter;
            MiscDataSync_NetIDToType[MiscDataNetIDCounter] = dataTypeToSync;
            MiscDataNetIDCounter++;
        }

        public delegate void PlayerActionDelegate(Player player);
        public static event PlayerActionDelegate PostUpdateMiscEffectsEvent;

        public override void PostUpdateMiscEffects()
        {
            PostUpdateMiscEffectsEvent?.Invoke(Player);
            TickDownHurtSoundTimer();
            UpdateVisualVariables();
            Player.moveSpeed = MoveSpeedModifier.ApplyTo(Player.moveSpeed);
        }

        /// <summary>
        /// The last amount of grapples the player had out
        /// Done for projectile logic sake, in case one projectile was spawned before the grappling hooks and therefore gets updated before the grappling hooks can be registered this frame
        /// </summary>
        public int previousGrapCount = 0;
        public static event PlayerActionDelegate PreUpdateMovementEvent;
        public override void PreUpdateMovement()
        {
            PreUpdateMovementEvent?.Invoke(Player);
            previousGrapCount = Player.grapCount;
        }

        public static event PlayerActionDelegate SetControlsEvent;
        public override void SetControls()
        {
            SetControlsEvent?.Invoke(Player);
        }

        #region Ouch related hooks

        public delegate bool CanBeHitByNPCDelegate(Player player, NPC npc);
        public static event CanBeHitByNPCDelegate CanBeHitByNPCEvent;
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (CanBeHitByNPCEvent != null)
            {
                foreach (CanBeHitByNPCDelegate immunityCheck in CanBeHitByNPCEvent.GetInvocationList())
                    if (!immunityCheck(Player, npc))
                        return false;
            }

            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        public delegate bool CanBeHitByProjectileDelegate(Player player, Projectile projectile);
        public static event CanBeHitByProjectileDelegate CanBeHitByProjectileEvent;
        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (CanBeHitByProjectileEvent != null)
            {
                foreach (CanBeHitByProjectileDelegate immunityCheck in CanBeHitByProjectileEvent.GetInvocationList())
                    if (!immunityCheck(Player, proj))
                        return false;
            }

            return base.CanBeHitByProjectile(proj);
        }

        public delegate bool ImmuneToDelegate(Player player, PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable);
        public static event ImmuneToDelegate ImmuneToEvent;
        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            if (ImmuneToEvent != null)
            {
                foreach (ImmuneToDelegate immunityCheck in ImmuneToEvent.GetInvocationList())
                    if (immunityCheck(Player, damageSource, cooldownCounter, dodgeable))
                        return true;
            }

            return base.ImmuneTo(damageSource, cooldownCounter, dodgeable);
        }

        public delegate void ModifyHurtDelegate(Player player, ref Player.HurtModifiers modifiers);
        public static event ModifyHurtDelegate ModifyHurtEvent;
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)/* tModPorter Override ImmuneTo, FreeDodge or ConsumableDodge instead to prevent taking damage */
        {
            if (CustomHurtSound.HasValue)
                modifiers.DisableSound();
            ModifyHurtEvent?.Invoke(Player, ref modifiers);
        }

        /// <summary>
        /// Timer that goes from 1 to 0 in the second after the player got hurt
        /// </summary>
        public float JustHurtTimer = 0f;

        public delegate void OnHurtDelegate(Player player, Player.HurtInfo info);
        public static event OnHurtDelegate OnHurtEvent;
        public override void OnHurt(Player.HurtInfo info)
        {
            JustHurtTimer = 1;
            OverrideHurtSound();
            OnHurtEvent?.Invoke(Player, info);
        }

        public delegate bool PreKillDelegate(Player player, double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource);
        public static event PreKillDelegate PreKillEvent;
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (damageSource.TryGetCausingEntity(out Entity damageDealer))
            {
                if (damageDealer is NPC npc && npc.ModNPC is ICustomDeathMessages deathMessageNPC)
                    deathMessageNPC.CustomDeathMessage(Player, ref damageSource);
                else if (damageDealer is Projectile proj && proj.ModProjectile is ICustomDeathMessages deathMessageProjectile)
                    deathMessageProjectile.CustomDeathMessage(Player, ref damageSource);
            }

            //If the player dies from dot or fall DMG
            else if (damageSource.SourceOtherIndex == 8 || damageSource.SourceOtherIndex == 0)
            {
                float highestPriority = 0f;
                for (int i = 0; i < Player.MaxBuffs; i++)
                {
                    if (Player.buffTime[i] > 0)
                    {
                        ModBuff buff = ModContent.GetModBuff(Player.buffType[i]);
                        if (buff != null && buff is ICustomDeathMessages deathMessageBuff && deathMessageBuff.DoTDeathMessagePriority >= highestPriority)
                        {
                            deathMessageBuff.CustomDeathMessage(Player, ref damageSource);
                            highestPriority = deathMessageBuff.DoTDeathMessagePriority;
                        }
                    }
                }
            }

            if (PreKillEvent != null)
            {
                foreach (PreKillDelegate preKillHook in PreKillEvent.GetInvocationList())
                {
                    if (!preKillHook(Player, damage, hitDirection, pvp, ref playSound, ref genGore, ref damageSource))
                        return false;
                }
            }

            return true;
        }
        #endregion

        #region Regen related hooks

        public delegate void NaturalLifeRegenDelegate(Player player, ref float regen);
        /// <summary>
        /// Allows you to modify the player's natural life regeneration, separate from dots and such. <br/>
        /// Runs after all other <see cref="Player.lifeRegen"/> modifications do, so you can also use this to perform any final changes to it
        /// </summary>
        public static event NaturalLifeRegenDelegate NaturalLifeRegenEvent;

        public override void NaturalLifeRegen(ref float regen)
        {
            NaturalLifeRegenEvent?.Invoke(Player, ref regen);
        }

        /// <inheritdoc cref="ModPlayer.UpdateLifeRegen"/>
        public static event PlayerActionDelegate UpdateLifeRegenEvent;
        public override void UpdateLifeRegen()
        {
            UpdateLifeRegenEvent?.Invoke(Player);
        }

        /// <inheritdoc cref="ModPlayer.PostUpdateBuffs"/>
        public static event PlayerActionDelegate PostUpdateBuffsEvent;
        public override void PostUpdateBuffs() 
        {
            PostUpdateBuffsEvent?.Invoke(Player);
        }

        /// <inheritdoc cref="ModPlayer.UpdateBadLifeRegen"/>
        public static event PlayerActionDelegate UpdateBadLifeRegenEvent;
        public override void UpdateBadLifeRegen()
        {
            UpdateBadLifeRegenEvent?.Invoke(Player);
        }
    
        #endregion

        public delegate bool CanHitNPCDelegate(Player player, NPC target);
        public static event CanHitNPCDelegate CanHitNPCEvent;

        public override bool CanHitNPC(NPC target)
        {
            if (CanHitNPCEvent != null)
            {
                foreach (CanHitNPCDelegate canHit in CanHitNPCEvent.GetInvocationList())
                {
                    if (!canHit(Player, target))
                        return false;
                }
            }

            return true;
        }

        public delegate bool PlayerItemBooleanDelegate(Player player, Item item);

        /// <inheritdoc cref="ModPlayer.CanUseItem(Item)(Item)"/>
        public static event PlayerItemBooleanDelegate CanUseItemEvent;
        public override bool CanUseItem(Item item)
        {
            bool result = base.CanUseItem(item);
            if (CanUseItemEvent != null)
                foreach (PlayerItemBooleanDelegate eventEntry in CanUseItemEvent.GetInvocationList())
                    result &= eventEntry(Player, item);

            return result;
        }

        public delegate bool? NullablePlayerItemBooleanDelegate(Player player, Item item);

        /// <inheritdoc cref="ModPlayer.CanAutoReuseItem(Item)"/>
        public static event NullablePlayerItemBooleanDelegate CanAutoReuseItemEvent;
        public override bool? CanAutoReuseItem(Item item)
        {
            bool? result = base.CanAutoReuseItem(item);
            if (CanAutoReuseItemEvent != null)
                foreach (NullablePlayerItemBooleanDelegate eventEntry in CanAutoReuseItemEvent.GetInvocationList())
                {
                    bool? outcome = eventEntry(Player, item);
                    if (outcome.HasValue)
                    {
                        if (result is null)
                            result = outcome;
                        else
                            result &= outcome;
                    }
                }

            return result;
        }

        public static event PlayerActionDelegate FrameEffectsEvent;
        public override void FrameEffects()
        {
            FrameEffectsEvent?.Invoke(Player);
        }


        /// <inheritdoc cref="ModPlayer.PostUpdate"/>
        public static event PlayerActionDelegate PostUpdateEvent;
        public override void PostUpdate()
        {
            PostUpdateEvent?.Invoke(Player);
        }

        /// <inheritdoc cref="ModPlayer.UpdateEquips"/>
        public static event PlayerActionDelegate UpdateEquipsEvent;
        public override void UpdateEquips()
        {
            UpdateEquipsEvent?.Invoke(Player);
        }


        /// <inheritdoc cref="ArmorSetBonusActivated"/>
        public static event PlayerActionDelegate ArmorSetBonusActivatedEvent;
        public override void ArmorSetBonusActivated()
        {
            ArmorSetBonusActivatedEvent?.Invoke(Player);
        }

        public delegate void OnHitNPCWithProjDelegate(Player player, Projectile proj, NPC target, NPC.HitInfo hit, int damageDone);
        /// <inheritdoc cref="ModPlayer.OnHitNPCWithProj"/>
        public static event OnHitNPCWithProjDelegate OnHitNPCWithProjEvent;
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            OnHitNPCWithProjEvent?.Invoke(Player, proj, target, hit, damageDone);
        }

        public delegate bool ModifyNurseHealDelegate(Player player, NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText);
        /// <inheritdoc cref="ModPlayer.ModifyNurseHeal"/>
        public static event ModifyNurseHealDelegate ModifyNurseHealEvent;
        public override bool ModifyNurseHeal(NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText)
        {
            if (ModifyNurseHealEvent != null)
            {
                foreach (ModifyNurseHealDelegate nurseCheck in ModifyNurseHealEvent.GetInvocationList())
                {
                    if (!nurseCheck(Player, nurse, ref health, ref removeDebuffs, ref chatText))
                        return false;
                }
            }

            return true;
        }

        /// <inheritdoc cref="ModPlayer.UpdateVisibleAccessories"/>
        public static event PlayerActionDelegate UpdateVisibleAccessoriesEvent;
        public override void UpdateVisibleAccessories()
        {
            UpdateVisibleAccessoriesEvent?.Invoke(Player);
        }

        public delegate void SaveLoadDataDelegate(FablesPlayer player, TagCompound tag);
        public static event SaveLoadDataDelegate SaveDataEvent;
        public override void SaveData(TagCompound tag)
        {
            SaveDataEvent?.Invoke(this, tag);
            DeathScreenFidgetToysUI.SavePlayerData(this, tag);
        }

        public static event SaveLoadDataDelegate LoadDataEvent;
        public override void LoadData(TagCompound tag)
        {
            LoadDataEvent?.Invoke(this, tag);
            DeathScreenFidgetToysUI.LoadPlayerData(this, tag);
        }


        [Serializable]
        public abstract class SyncPlayerMiscData<T> : Module where T : CustomGlobalData
        {
            public byte sender;
            public byte playerToSync;
            public T dataToSync;
            //public ushort dataSyncNetID;

            public SyncPlayerMiscData(Player player, T data)
            {
                sender = (byte)Main.myPlayer;
                playerToSync = (byte)player.whoAmI;
                dataToSync = data;
                //dataSyncNetID = MiscDataSync_TypeToNetID[dataType];
            }

            protected override void Receive()
            {
                Player player = Main.player[playerToSync];
                //Type dataType = MiscDataSync_NetIDToType[dataSyncNetID];
                player.SetPlayerData(dataToSync);

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
        public static FablesPlayer Fables(this Player player) => player.GetModPlayer<FablesPlayer>();

        /// <summary>
        /// Writes a temporary boolean flag to this player, which gets reset on ResetEffects. Use <see cref="GetPlayerFlag(Player, string)"/> to retrieve the status of the flag <br/>
        /// Useable for accessories, buff/debuffs and such
        /// </summary>
        public static void SetPlayerFlag(this Player player, string flagKey)
        {
            player.GetModPlayer<FablesPlayer>().AccessoryVariables[flagKey] = true;
        }

        /// <summary>
        /// Reads if the player possesses the temporary flag set in <see cref="SetPlayerFlag(Player, string)"/><br/>
        /// Useable for accessories, buff/debuffs and such
        /// </summary>
        public static bool GetPlayerFlag(this Player player, string flagKey)
        {
            if (player.GetModPlayer<FablesPlayer>().AccessoryVariables.TryGetValue(flagKey, out bool flag))
                return flag;
            return false;
        }


        /// <summary>
        /// Writes custom data in the form of an object to this player.<br/>
        /// This data doesn't get cleared on ResetEffects, but you can override <see cref="CustomGlobalData.Reset"/> to reset values <br/>
        /// Can be read back with <see cref="GetPlayerData{T}(Player, out T)"/>
        /// </summary>
        /// <typeparam name="T">The class of the data object we're looking for</typeparam>
        /// <param name="player"></param>
        /// <param name="data">The data to write</param>
        public static void SetPlayerData<T>(this Player player, T data) where T : CustomGlobalData
        {
            player.Fables().MiscData[typeof(T)] = data;
        }

        /// <summary>
        /// Reads the custom data class written to this player with <see cref="SetPlayerData{T}(Player, T)"/>.<br/>
        /// This data doesn't get cleared on ResetEffects, but you can override <see cref="CustomGlobalData.Reset"/> to reset values 
        /// </summary>
        /// <typeparam name="T">The class of the data object we're looking for</typeparam>
        /// <param name="player"></param>
        /// <param name="result">The retrieved data, if any</param>
        /// <returns>If there was any data of this type written to the player</returns>
        public static bool GetPlayerData<T>(this Player player, out T result) where T : CustomGlobalData
        {
            result = default(T);
            if (player.Fables().MiscData.TryGetValue(typeof(T), out CustomGlobalData objResult))
            {
                result = (T)objResult;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sends the custom player data written to this player with <see cref="SetPlayerData{T}(Player, T)"/> across the net<br/>
        /// Doesn't sync if the player doesn't have any data written to them
        /// </summary>
        /// <remarks>The global data being synced needs to have the serializable attribute and be registered with <see cref="ReserveCustomGlobalDataNetID(Type)"/> to work at all </remarks>
        /// <typeparam name="T">The class of the data object we're looking for</typeparam>
        /// <param name="player">The player whose data is needed to snyc</param>
        public static void SyncPlayerData<T, T2>(this Player player) where T : CustomGlobalData where T2 : SyncPlayerMiscData<T>
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            if (!player.Fables().MiscData.TryGetValue(typeof(T), out CustomGlobalData objResult))
                return;

            T2 packet = (T2)Activator.CreateInstance(typeof(T2), player, (T)objResult);

            packet.Send(runLocally:false);
        }


        /// <summary>
        /// Saves an item this player, which gets reset on ResetEffects. Use <see cref="GetPlayerAccessory"/> to retrieve the item<br/>
        /// Useable for accessories that spawn projectiles and need the item as a source
        /// </summary>
        public static void SetPlayerAccessory(this Player player, Item item)
        {
            player.GetModPlayer<FablesPlayer>().AccessoryItems[item.type] = item;
        }

        /// <summary>
        /// Retrieves a temporary item saved to this player that was saved in <see cref="SetPlayerAccessory"/><br/>
        /// Useable for accessories that spawn projectiles and need the item as a source
        /// </summary>
        public static bool GetPlayerAccessory(this Player player, int key, out Item item)
        {
            if (player.GetModPlayer<FablesPlayer>().AccessoryItems.TryGetValue(key, out item))
                return true;

            return false;
        }

        /// <summary>
        /// Retrieves the use time of this player's currently held item.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>
        /// null if the held item is null or <see cref="Item.IsAir"/>.
        /// </returns>
        public static int? GetHeldItemUseTime(this Player player)
        {
            Item heldItem = player.HeldItem;
            if (heldItem is null || heldItem.IsAir)
                return null;

            return (int)(heldItem.useTime / player.GetWeaponAttackSpeed(heldItem));
        }
    }
}

