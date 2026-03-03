using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Items.BurntDesert
{
    public class ElectricDye : ModItem
    {
        public override string Texture => AssetDirectory.DesertItems + Name;

        public static int DyeID;

        public override void Load()
        {
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += UpdateZap;
        }

        private void UpdateZap()
        {
            for (int i = 0; i < ElectricDyeArmorShaderData.ZapTime.Length; i++)
            {
                int zapTime = ElectricDyeArmorShaderData.ZapTime[i];

                //Tick down zap time and reset if necessary
                if (zapTime <= 0 && Main.rand.NextBool(60))
                    zapTime = 4;
                else
                    zapTime--;

                ElectricDyeArmorShaderData.ZapTime[i] = zapTime;
            }

        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Electric Dye");
            Item.ResearchUnlockCount = 3;

            if (!Main.dedServ)
            {
                Asset<Effect> shader = ModContent.Request<Effect>("CalamityFables/Effects/Dye/ElectricDye", AssetRequestMode.ImmediateLoad);
                GameShaders.Armor.BindShader(Type, new ElectricDyeArmorShaderData(shader, "ElectricDyePass"))
                    .UseImage(AssetDirectory.NoiseTextures.RGB)
                    .UseColor(new Color(101, 241, 209))
                    .UseSecondaryColor(new Color(177, 237, 59));
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
                .AddIngredient<Electrocells>(3)
                .AddTile(TileID.DyeVat)
                .Register();
        }
    }

    public class ElectricDyeArmorShaderData : ArmorShaderData
    {
        public ElectricDyeArmorShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName) { }

        public static int[] ZapTime = new int[6];

        public override void Apply(Entity entity, DrawData? drawData)
        {
            int uniqueID = 0;

            if (entity is Player player)
                uniqueID = player.whoAmI;
            else if (entity is Projectile projectile)
                uniqueID = projectile.whoAmI;

            UseSaturation(uniqueID);
            UseImage(AssetDirectory.NoiseTextures.RGB);
            base.Apply(entity, drawData);

            //Use one of the 6 cooldown slots for the zapping electricity for the offset
            int zapping = ZapTime[uniqueID % 6] > 0 ? 1 : 0;

            //Gotta do that for the point sampling
            Shader.Parameters["rgbNoise"]?.SetValue(AssetDirectory.NoiseTextures.RGB.Value);
            
            float electricity = Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly));
            Shader.Parameters["glowStrenght"]?.SetValue(electricity + zapping);

            float displaceDir = (float)Math.Sin(Main.GlobalTimeWrappedHourly);
            Shader.Parameters["displaceStrenght"]?.SetValue(Math.Abs(displaceDir) + zapping * 0.5f);
        }
    }
}