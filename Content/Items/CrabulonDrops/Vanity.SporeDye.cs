using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class SporeDye : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public static int DyeID;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore Dye");
            Item.ResearchUnlockCount = 3;

            if (!Main.dedServ)
            {
                var shader = ModContent.Request<Effect>("CalamityFables/Effects/Dye/SporeDye", AssetRequestMode.ImmediateLoad);
                GameShaders.Armor.BindShader(Type, new SporeDyeArmorShaderData(shader, "SporeDyePass"))
                    .UseColor(CommonColors.MushroomDeepBlue)
                    .UseImage("Images/Misc/noise");
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
            CreateRecipe()
                .AddIngredient(ItemID.BottledWater)
                .AddIngredient(ItemID.GlowingMushroom, 3)
                .AddTile(TileID.DyeVat)
                .Register();
        }
    }


    public class SporeDyeArmorShaderData : ArmorShaderData
    {
        public SporeDyeArmorShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName) { }

        public override ArmorShaderData GetSecondaryShader(Entity entity) => GameShaders.Armor.GetShaderFromItemId(ItemID.WispDye);
    }
}