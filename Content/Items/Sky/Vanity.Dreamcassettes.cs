namespace CalamityFables.Content.Items.Sky
{
    public abstract class ShaderDreamcassette : ModItem
    {
        public abstract void Activate(Player player);

        public override string Texture => AssetDirectory.SkyItems + Name;

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
                Activate(player);
        }
        public override void UpdateVanity(Player player) => Activate(player);
    }

    public class GoldenDreamcassette : ShaderDreamcassette
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Golden Dreamcassette");
            Tooltip.SetDefault("Allows the user to see the world differently\n" +
            "'Angelic chants echo from the cassette...'");
        }
        public override void Activate(Player player) => player.GetModPlayer<DreamcassettePlayer>().goldenShaderActive = true;
    }


    public class CelestialDreamcassette : ShaderDreamcassette
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Celestial Dreamcassette");
            Tooltip.SetDefault("Allows the user to see the world differently\n" +
            "'Dissonant harps echo from the cassette...'");
        }
        public override void Activate(Player player) => player.GetModPlayer<DreamcassettePlayer>().celestialShaderActive = true;
    }

    public class OldworldDreamcassette : ShaderDreamcassette
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Old World Dreamcassette");
            Tooltip.SetDefault("Allows the user to see the world differently\n" +
            "'Thunder cracks echo from the cassette...'");
        }
        public override void Activate(Player player) => player.GetModPlayer<DreamcassettePlayer>().oldworldShaderActive = true;
    }

    public class DreamcassettePlayer : ModPlayer
    {
        public bool goldenShaderActive;
        public float goldenShaderOpacity = 0;

        public bool celestialShaderActive;
        public float celestialShaderOpacity = 0;

        public bool oldworldShaderActive;
        public float oldworldShaderOpacity = 0;

        public override void ResetEffects()
        {
            goldenShaderActive = false;
            celestialShaderActive = false;
            oldworldShaderActive = false;
        }

        public override void PostUpdateMiscEffects()
        {
            //Golden
            if (goldenShaderActive)
                goldenShaderOpacity += 0.01f;
            else
                goldenShaderOpacity -= 0.01f;

            //Celestial
            if (celestialShaderActive)
                celestialShaderOpacity += 0.01f;
            else
                celestialShaderOpacity -= 0.01f;

            //Oldworld
            if (oldworldShaderActive)
                oldworldShaderOpacity += 0.01f;
            else
                oldworldShaderOpacity -= 0.01f;


            celestialShaderOpacity = MathHelper.Clamp(celestialShaderOpacity, 0f, 1f);
            goldenShaderOpacity = MathHelper.Clamp(goldenShaderOpacity, 0f, 1f);
            oldworldShaderOpacity = MathHelper.Clamp(oldworldShaderOpacity, 0f, 1f);
        }
    }

    public class GoldenDreamcastShader : ModSystem
    {
        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;

            if (Main.LocalPlayer.GetModPlayer<DreamcassettePlayer>().goldenShaderOpacity <= 0)
            {
                if (Scene["GoldenDreamcastGlow"].IsActive())
                    Scene["GoldenDreamcastGlow"].Deactivate();

                return;
            }

            float[] gradientMapBrightnesses = new float[]
            {
                0,
                0.2f,
                0.51f,
                0.74f,
                0.9f,
                1f
            };
            Vector3[] gradientMapColors = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Color(75, 23, 0).ToVector3(),
                new Color(223, 124, 0).ToVector3(),
                new Color(255, 208, 43).ToVector3(),
                new Color(255, 254, 112).ToVector3(),
                new Color(255, 255, 250).ToVector3(),
            };

            float[] linearLightBrightnesses = new float[]
            {
                0,
                0.2f,
                0.37f,
                0.59f,
                0.89f,
                1f
            };
            Vector3[] linearLightColors = new Vector3[]
            {
                new Color(0, 0, 27).ToVector3(),
                new Color(47, 19, 89).ToVector3(),
                new Color(97, 49, 102).ToVector3(),
                new Color(217, 87, 100).ToVector3(),
                new Color(255, 210, 108).ToVector3(),
                new Color(253, 238, 178).ToVector3(),
            };



            Effect shader = Scene["GoldenDreamcastGlow"].GetShader().Shader;
            float progress = FablesUtils.SineInOutEasing(Main.LocalPlayer.GetModPlayer<DreamcassettePlayer>().goldenShaderOpacity, 1);

            shader.Parameters["gradientMapStrenght"].SetValue(0.25f);
            shader.Parameters["gradientMapBrightnesses"].SetValue(gradientMapBrightnesses);
            shader.Parameters["gradientMapColors"].SetValue(gradientMapColors);

            float linearLightStrenght = 0.05f;
            if (Main.dayTime && Main.time < Main.dayLength * 0.05f)
                linearLightStrenght *= (float)Utils.GetLerpValue(0, Main.dayLength * 0.05f, Main.time, true);

            else if (Main.dayTime && Main.time >= Main.dayLength * 0.92f)
                linearLightStrenght *= (float)Utils.GetLerpValue(Main.dayLength, Main.dayLength * 0.92f, Main.time, true);

            else if (!Main.dayTime)
                linearLightStrenght = 0f;


            shader.Parameters["linearLightStrenght"].SetValue(linearLightStrenght);
            shader.Parameters["linearLightBrightnesses"].SetValue(linearLightBrightnesses);
            shader.Parameters["linearLightColors"].SetValue(linearLightColors);

            shader.Parameters["uOpacity"].SetValue(progress);
            shader.Parameters["brightnessOffset"].SetValue(-1f + 0.3f * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.14f) * 0.5f + 0.5f));

            if (Main.netMode != NetmodeID.Server && !Scene["GoldenDreamcastGlow"].IsActive())
            {
                Scene.Activate("GoldenDreamcastGlow").GetShader().UseProgress(0f).UseColor(Color.White.ToVector3());
            }
        }

    }

    public class IridescentDreamcastShader : ModSystem
    {
        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;

            if (Main.LocalPlayer.GetModPlayer<DreamcassettePlayer>().celestialShaderOpacity <= 0)
            {
                if (Scene["DreamcastIridescence"].IsActive())
                    Scene["DreamcastIridescence"].Deactivate();

                return;
            }

            float[] gradientMapBrightnesses = new float[]
            {
                0,
                0.12f,
                0.23f,
                0.52f,
                0.84f,
                1f,

                1f,1f,1f,1f,
            };
            Vector3[] gradientMapColors = new Vector3[]
            {
                new Color(5, 23, 83).ToVector3(),
                new Color(23, 50, 109).ToVector3(),
                new Color(40, 75, 133).ToVector3(),
                new Color(122, 130, 176).ToVector3(),
                new Color(217, 234, 184).ToVector3(),
                new Color(255, 255, 228).ToVector3(),

                new Vector3(0),new Vector3(0),new Vector3(0),new Vector3(0),
            };

            float[] linearLightBrightnesses = new float[]
            {
                0,
                0.35f,
                0.42f,
                0.47f,
                0.51f,
                0.56f,
                0.61f,
                0.64f,
                0.68f,
                0.72f
            };
            Vector3[] iridescentColors = new Vector3[]
            {
                new Color(6, 6, 6).ToVector3(),
                new Color(6, 6, 6).ToVector3(),
                new Color(51, 46, 78).ToVector3(),
                new Color(113, 53, 146).ToVector3(),
                new Color(174, 23, 189).ToVector3(),
                new Color(237, 128, 60).ToVector3(),
                new Color(247, 255, 101).ToVector3(),
                new Color(176, 234, 85).ToVector3(),
                new Color(102, 219, 249).ToVector3(),
                new Color(0, 0, 0).ToVector3(),
            };


            for (int i = 2; i < 9; i++)
            {
                linearLightBrightnesses[i] += (float)Math.Sin(Main.GlobalTimeWrappedHourly + i * 0.1f) * 0.01f;
            }


            Effect shader = Scene["DreamcastIridescence"].GetShader().Shader;
            float progress = FablesUtils.SineInOutEasing(Main.LocalPlayer.GetModPlayer<DreamcassettePlayer>().celestialShaderOpacity, 1);

            //Blue tint
            shader.Parameters["lightenStrenght"].SetValue(0.5f);
            shader.Parameters["lightenBrightnesses"].SetValue(gradientMapBrightnesses);
            shader.Parameters["lightenColors"].SetValue(gradientMapColors);

            shader.Parameters["iridescenceStrenght"].SetValue(0.6f);
            shader.Parameters["iridescenceBrightnesses"].SetValue(linearLightBrightnesses);
            shader.Parameters["iridescenceColors"].SetValue(iridescentColors);

            shader.Parameters["uOpacity"].SetValue(progress);

            if (Main.netMode != NetmodeID.Server && !Scene["DreamcastIridescence"].IsActive())
            {
                Scene.Activate("DreamcastIridescence").GetShader().UseProgress(0f).UseColor(Color.White.ToVector3());
            }
        }
    }

    public class OldworldOutskirtsShader : ModSystem
    {
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            float opacity = Main.LocalPlayer.GetModPlayer<DreamcassettePlayer>().oldworldShaderOpacity;

            opacity = -10f;

            if (opacity > 0)
            {
                Main.ColorOfTheSkies = Color.Lerp(Main.ColorOfTheSkies, new Color(125, 125, 125), opacity);
                backgroundColor = Color.Lerp(backgroundColor, new Color(60, 100, 122), opacity);
                tileColor = Color.Lerp(tileColor, new Color(80, 90, 120), opacity);
            }

            return;

        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;


            if (Main.LocalPlayer.GetModPlayer<DreamcassettePlayer>().oldworldShaderOpacity <= 0)
            {
                if (Scene["OldworldOvercast"].IsActive())
                    Scene["OldworldOvercast"].Deactivate();

                return;
            }

            float[] lightenGradientBrightnesses = new float[]
            {
                0,
                0.2f,
                0.42f,
                0.73f,
                0.89f,
                1f,
            };
            Vector3[] lightenGradientColors = new Vector3[]
            {
                new Color(30, 33, 39).ToVector3(),
                new Color(37, 60, 66).ToVector3(),
                new Color(124, 162, 208).ToVector3(),
                new Color(132, 197, 212).ToVector3(),
                new Color(240, 255, 215).ToVector3(),
                new Color(253, 255, 249).ToVector3(),
            };

            float[] colorDodgeBrightnesses = new float[]
            {
                0,
                0.1f,
                0.2f,
                0.5f,
                0.88f,
                0.1f
            };
            Vector3[] colorDodgeColors = new Vector3[]
            {
                new Color(115, 115, 115).ToVector3(),
                new Color(60, 67, 88).ToVector3(),
                new Color(37, 55, 79).ToVector3(),
                new Color(107, 188, 209).ToVector3(),
                new Color(185, 252, 226).ToVector3(),
                new Color(235, 255, 238).ToVector3(),
            };

            Effect shader = Scene["OldworldOvercast"].GetShader().Shader;
            float progress = FablesUtils.SineInOutEasing(Main.LocalPlayer.GetModPlayer<DreamcassettePlayer>().oldworldShaderOpacity, 1);

            shader.Parameters["lightenGradientMapStrenght"].SetValue(0.53f);
            shader.Parameters["lightenGradientMapBrightnesses"].SetValue(lightenGradientBrightnesses);
            shader.Parameters["lightenGradientMapColors"].SetValue(lightenGradientColors);

            shader.Parameters["colorDodgeStrenght"].SetValue(0.38f);

            shader.Parameters["desaturationMaskStretch"].SetValue(new Vector2(0.8f, 1.3f));
            shader.Parameters["desaturationMaskBlend"].SetValue(0.08f);
            shader.Parameters["edgeDesaturationStrenght"].SetValue(0.78f);

            shader.Parameters["colorDodgeMaskStretch"].SetValue(new Vector2(1.3f, 1.1f));
            shader.Parameters["colorDodgeMaskBlend"].SetValue(0.14f);
            shader.Parameters["colorDodgeGradientStrenght"].SetValue(0.18f);
            shader.Parameters["colorDodgeGradientBrightnesses"].SetValue(colorDodgeBrightnesses);
            shader.Parameters["colorDodgeGradientColors"].SetValue(colorDodgeColors);

            shader.Parameters["uOpacity"].SetValue(progress);

            if (Main.netMode != NetmodeID.Server && !Scene["OldworldOvercast"].IsActive())
            {
                Scene.Activate("OldworldOvercast").GetShader().UseProgress(0f).UseColor(Color.White.ToVector3());
            }
        }
    }
}
