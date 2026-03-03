using System.IO;
using Terraria.DataStructures;

namespace CalamityFables.Core
{
    public partial class FablesItem : GlobalItem
    {
        public override void Load()
        {
            On_Item.GetShimmered += ShimmerOverride;


        }

        public delegate bool ItemCustomShimmerDelegate(Item item);
        public static EventSet<ItemCustomShimmerDelegate> CustomShimmerInteractionEvent = new EventSet<ItemCustomShimmerDelegate>();
        private void ShimmerOverride(On_Item.orig_GetShimmered orig, Item self)
        {
            if (CustomShimmerInteractionEvent.TryGetInvocation(self.type, out var shimmerEffect))
            {
                if (!shimmerEffect(self))
                    return;
            }

            orig(self);
        }

        public override bool InstancePerEntity => true;

        public string textureOverrideName = "";
        public Asset<Texture2D> textureOverride;

        public delegate void ModifyItemLootDelegate(Item item, ItemLoot itemLoot);
        public static event ModifyItemLootDelegate ModifyItemLootEvent;
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            ModifyItemLootEvent?.Invoke(item, itemLoot);
        }

        public delegate string IsArmorSetDelegate(Item head, Item body, Item legs);
        public static event IsArmorSetDelegate IsArmorSetEvent;
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (IsArmorSetEvent == null)
                return "";

            foreach (IsArmorSetDelegate armorSetCheck in IsArmorSetEvent.GetInvocationList())
            {
                string result = armorSetCheck(head, body, legs);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            return "";
        }

        public delegate void UpdateArmorSetDelegate(Player player, string set);
        public static event UpdateArmorSetDelegate UpdateArmorSetEvent;
        public override void UpdateArmorSet(Player player, string set)
        {
            UpdateArmorSetEvent?.Invoke(player, set);
        }

        public delegate void ModifyWeaponCritDelegate(Item item, Player player, ref float crit);

        /// <inheritdoc cref="GlobalItem.ModifyWeaponCrit(Item, Player, ref float)"/>
        public static event ModifyWeaponCritDelegate ModifyWeaponCritEvent;
        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            ModifyWeaponCritEvent?.Invoke(item, player, ref crit);
        }

        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (textureOverrideName != null && textureOverrideName != "")
            {
                textureOverride ??= ModContent.Request<Texture2D>(AssetDirectory.Assets + textureOverrideName);
                Texture2D tex = textureOverride.Value;
                Texture2D baseTexture = TextureAssets.Item[item.type].Value;
                float heightDiff = baseTexture.Height - tex.Height;

                spriteBatch.Draw(tex, item.Center - Main.screenPosition + Vector2.UnitY * heightDiff / 2f, null, lightColor, rotation, tex.Size() / 2f, scale, 0, 0);
                return false;
            }

            return true;
        }

        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (textureOverrideName != null && textureOverrideName != "")
            {
                textureOverride ??= ModContent.Request<Texture2D>(AssetDirectory.Assets + textureOverrideName);
                Texture2D tex = textureOverride.Value;
                spriteBatch.Draw(tex, position, null, drawColor, 0, origin, scale, 0, 0);
                return false;
            }

            return true;
        }

        public delegate void ModifyTooltipsDelegate(Item item, List<TooltipLine> tooltips);

        public static event ModifyTooltipsDelegate ModifyTooltipsEvent;
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            ModifyTooltipsEvent?.Invoke(item, tooltips);
        }

        public override void OnStack(Item destination, Item source, int numToTransfer)
        {
            //if (!source.TryGetGlobalItem(out FablesItem modItem2))
            //    return;

        }

        public override void SplitStack(Item destination, Item source, int numToTransfer)
        {
            if (!source.TryGetGlobalItem(out FablesItem modItem2))
                return;

            modItem2.textureOverrideName = textureOverrideName;
        }

        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(textureOverrideName);
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            textureOverrideName = reader.ReadString();
        }
    }
}

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        public static Item OverrideTexture(this Item item, string textureName)
        {
            if (item.TryGetGlobalItem(out FablesItem modItem))
                modItem.textureOverrideName = textureName;

            return item;
        }
    }
}
