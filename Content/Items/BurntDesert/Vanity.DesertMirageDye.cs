using CalamityFables.Content.Tiles.BurntDesert;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Items.BurntDesert
{
    public class DesertMirageDye : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public static int DyeID;

        public override void Load()
        {
            FablesPlayer.ModifyDrawInfoEvent += DissapearSkin;
        }

        private void DissapearSkin(Player player, ref PlayerDrawSet drawInfo)
        {
            if (drawInfo.cHead == DyeID && player.head != -1 && !ArmorIDs.Head.Sets.DrawHatHair[player.head] && !ArmorIDs.Head.Sets.DrawFullHair[player.head])
            {
                drawInfo.colorHead *= 0f;
                drawInfo.colorEyes *= 0f;
                drawInfo.colorEyeWhites *= 0f;
            }
            if (drawInfo.cBody == DyeID && player.body != -1)
                drawInfo.hidesTopSkin = true;
            if (drawInfo.cLegs == DyeID && player.legs != -1)
                drawInfo.hidesBottomSkin = true;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Desert Mirage Dye");
            Item.ResearchUnlockCount = 3;

            if (!Main.dedServ)
            {
                SandstoneGraveyardGhost.DisplaceNoiseTex ??= ModContent.Request<Texture2D>(AssetDirectory.Noise + "DisplaceNoise3");

                Asset<Effect> shader = ModContent.Request<Effect>("CalamityFables/Effects/Dye/DesertMirageDye", AssetRequestMode.ImmediateLoad);
                GameShaders.Armor.BindShader(Type, new DesertMirageDyeArmorShaderData(shader, "DesertMirageDyePass"))
                    .UseImage(SandstoneGraveyardGhost.DisplaceNoiseTex);
            }
        }

        public override void SetDefaults()
        {
            //Cache the dye ID, because the dye ID is automatically registered in BindShader, and we don't want it to get overriden in clonedefaults
            DyeID = Item.dye;
            Item.CloneDefaults(ItemID.AcidDye);
            Item.dye = DyeID;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.BottledWater)
                .AddIngredient(ItemID.DesertFossil, 3)
                //.AddTile(TileID.DyeVat)
                .AddCondition(SealedChamber.InNautilusChamber)
                .Register();
        }
    }

    public class DesertMirageDyeArmorShaderData : ArmorShaderData
    {
        public DesertMirageDyeArmorShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName) { }

        public override void Apply(Entity entity, DrawData? drawData)
        {
            int uniqueID = 0;

            if (entity is Player player)
                uniqueID = player.whoAmI;
            else if (entity is Projectile projectile)
                uniqueID = projectile.whoAmI;

            Color glowColor = FablesUtils.MulticolorLerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.4f + uniqueID)), Color.Cyan, Color.Turquoise, Color.MediumSpringGreen, Color.DeepSkyBlue);

            UseSaturation(uniqueID);
            UseColor(glowColor);
            UseSecondaryColor(Color.Lerp(glowColor, Color.White, 0.6f));

            base.Apply(entity, drawData);
        }
    }
}