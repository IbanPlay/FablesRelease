using CalamityFables.Content.NPCs.Sky;
using Terraria.DataStructures;

namespace CalamityFables.Content.Items.Sky
{
    public class CloudSpriteInABottle : ModItem
    {
        public override string Texture => AssetDirectory.SkyItems + Name;

        public static int dustType;

        public override void Load()
        {
            FablesPlayer.HideDrawLayersEvent += Decapitate;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.CloudinaBottle;
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
            Item.vanity = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                DoVisuals(player);
        }
        public override void UpdateVanity(Player player)
        {
            DoVisuals(player);
        }

        public override void UpdateItemDye(Player player, int dye, bool hideVisual)
        {
            if (!hideVisual && dye != 0)
                player.cHead = dye;
        }

        public void DoVisuals(Player player)
        {
            player.SetPlayerFlag(Name + "Vanity");
            player.SetCustomHurtSound(CloudSprite.HitSound, 10, 0.9f);

            Vector2 dustOffset = -Vector2.UnitY * Main.rand.NextFloat(12f, 17f) * player.gravDir;

            player.sitting.GetSittingOffsetInfo(player, out Vector2 posOffset, out float seatYOffset);
            dustOffset.Y += seatYOffset;

            dustOffset.RotatedBy(player.bodyRotation);
            dustOffset.X += player.direction * 3f;

            //Push the offset ahead if the player moves too fast
            if (player.velocity.Length() > 9f)
                dustOffset += player.velocity;

            float heightBoost = Main.rand.NextFloat();
            Vector2 dustPosition = player.MountedCenter + dustOffset + Vector2.UnitY * (-15f + 20f * heightBoost);
            dustPosition.X += Main.rand.NextFloat(-3f, 8f) * player.direction;

            if (player.direction == 1)
                dustPosition.X -= 3;

            Vector2 dustVelocity = -Vector2.UnitY * 0.3f + player.velocity * 0.3f;
            dustVelocity.X -= player.direction * (0.06f + 0.5f * (1 - heightBoost));

            dustVelocity.Y *= player.gravDir;

            Dust.NewDustPerfect(dustPosition, dustType, dustVelocity, 0, Color.White, Main.rand.NextFloat(0.5f, 0.7f) + heightBoost * 0.3f);
        }

        private void Decapitate(Player player, PlayerDrawSet drawInfo)
        {
            if (player.GetPlayerFlag(Name + "Vanity") && !drawInfo.headOnlyRender)
                PlayerDrawLayers.Head.Hide();
        }
    }
}
