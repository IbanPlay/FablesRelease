using CalamityFables.Core.DrawLayers;
using Terraria.DataStructures;

namespace CalamityFables.Core
{

    /// <summary>
    /// Interface that can be used by items that need to hide the players front arm when held.
    /// Useful for prosthesis type items.
    /// </summary>
    public interface IHideFrontArm
    {
        /// <summary>
        /// When should the arm be hidden. Defauls to always
        /// </summary>
        bool ShouldHideArm(Player player) => true;
    }


    public partial class FablesPlayer : ModPlayer
    {


        public delegate void UpdateVisibleAccessoryDelegate(Player player, int itemSlot, Item item, bool modded);
        /// <summary>
        /// Use this to make items have visual effects that run even when the game is paused and on the main menu screen
        /// </summary>
        public static EventSet<UpdateVisibleAccessoryDelegate> UpdateVisibleAccessoryEvent = new();
        private void UpdateVisibleAccessory(On_Player.orig_UpdateVisibleAccessory orig, Player self, int itemSlot, Item item, bool modded)
        {
            orig(self, itemSlot, item, modded);
            if (UpdateVisibleAccessoryEvent.TryGetInvocation(item.type, out UpdateVisibleAccessoryDelegate del))
                del(self, itemSlot, item, modded);
        }


        public delegate void DrawEffectsDelegate(Player player, PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright);
        public static event DrawEffectsDelegate DrawEffectsEvent;

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            DrawEffectsEvent?.Invoke(Player, drawInfo, ref r, ref g, ref b, ref a, ref fullBright);
        }

        public delegate void HideDrawLayersDelegate(Player player, PlayerDrawSet drawInfo);
        public static event HideDrawLayersDelegate HideDrawLayersEvent;

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (Player is null)
                return;

            HideDrawLayersEvent?.Invoke(Player, drawInfo);

            if (Player.HeldItem.ModItem is IHideFrontArm amputator && amputator.ShouldHideArm(Player))
                PlayerDrawLayers.ArmOverItem.Hide();
        }

        internal float cachedHeadRotation;
        public delegate void ModifyDrawInfoDelegate(Player player, ref PlayerDrawSet drawInfo);
        public static event ModifyDrawInfoDelegate ModifyDrawInfoEvent;
        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            ModifyDrawInfoEvent?.Invoke(Player, ref drawInfo);

            if (cachedHeadRotation != 0)
            {
                Player.headRotation = cachedHeadRotation;
            }
        }

        public ILongBackAccessory backEquipVanity;
        public int backEquipDye;
        public int gogglesDye;

        private void UpdateCustomLayerDyes(On_Player.orig_UpdateItemDye orig, Player self, bool isNotInVanitySlot, bool isSetToHidden, Item armorItem, Item dyeItem)
        {
            orig(self, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem);
            if (FablesSets.IsElongatedTail[armorItem.type])
                self.GetModPlayer<FablesPlayer>().backEquipDye = dyeItem.dye;
            if (FablesSets.IsGogglesVanity[armorItem.type])
                self.GetModPlayer<FablesPlayer>().gogglesDye = dyeItem.dye;
        }
    }
}
