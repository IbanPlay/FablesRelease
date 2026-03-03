using Microsoft.Xna.Framework.Graphics;
using CalamityFables.Content.Tiles.VanityTrees;
using CalamityFables.Content.Boss;
using CalamityFables.Particles;
using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.Items.CrabulonDrops;
using CalamityFables.Content.Items.EarlyGameMisc;
using CalamityFables.Content.Items.DesertScourgeDrops;

namespace CalamityFables.Core
{
    public static class AssetDirectory
    {
        public const string Assets = "CalamityFables/Assets/";
        public const string Invisible = Assets + "Invisible";
        public const string Visible = Assets + "Visible";
        public const string UpwardsGradient = Assets + "SimpleGradient";

        public const string Noise = Assets + "Noise/";
        public const string Debug = Assets + "Debugging/";
        public const string Banners = Assets + "Banners/";
        public const string DebugSquare = Assets + "Debugging/DebugSquare";

        public const string UI = Assets + "UI/";
        public const string ModIcon = UI + "ModIcon/";
        public const string Keepsakes = UI + "Keepsakes/";
        public const string DeathFidgets = UI + "DeathFidgetToys/";
        public const string Buffs = Assets + "Buffs/";
        public const string Dust = Assets + "Dusts/";
        public const string Particles = Assets + "Particles/";
        public const string MiscTiles = Assets + "Tiles/Misc/";
        public const string MiscVanity = Assets + "Items/VanityMisc/";
        public const string Graves = Assets + "Tiles/Graves/";
        public const string VanityTrees = Assets + "Tiles/VanityTrees/";
        public const string MusicBoxes = Assets + "Tiles/MusicBoxes/";

        public const string SnowNPCs = Assets + "NPCs/Snow/";
        public const string SnowItems = Assets + "Items/Snow/";

        public const string BurntDesert = Assets + "Tiles/BurntDesert/";
        public const string DesertItems = Assets + "Items/Desert/";
        public const string DesertNPCs = Assets + "NPCs/Desert/";
        public const string SirNautilus = Assets + "Boss/SirNautilus/";
        public const string SirNautilusDialogue = Assets + "Boss/SirNautilus/TalkSprites/";
        public const string SirNautilusDrops = Assets + "Items/SirNautilus/";

        public const string SunlessSeaItems = Assets + "Items/SunlessSea/";

        public const string DesertScourge = Assets + "Boss/DesertScourge/";
        public const string DesertScourgeDrops = Assets + "Items/DesertScourge/";
        public const string AquaticScourge = Assets + "Boss/AquaticScourge/";

        public const string Crabulon = Assets + "Boss/Crabulon/";
        public const string CrabulonDrops = Assets + "Items/Crabulon/";

        public const string EarlyGameMisc = Assets + "Items/EarlyGameMisc/";
        public const string CursedItems = Assets + "Items/Cursed/";
        public const string CursedNPCs = Assets + "NPCs/Cursed/";
        public const string UndergroundNPCs = Assets + "NPCs/Underground/";

        public const string WulfrumNPC = Assets + "NPCs/Wulfrum/";
        public const string WulfrumItems = Assets + "Items/Wulfrum/";
        public const string WulfrumScrapyard = Assets + "Tiles/WulfrumScrapyard/";
        public const string WulfrumTiles = Assets + "Tiles/Wulfrum/";
        public const string WulfrumBanners = Assets + "Banners/Wulfrum/";
        public const string WulfrumFurniture = WulfrumTiles + "Furniture/";
        public const string WulfrumFurniturePaint = WulfrumTiles + "Furniture/Paint/";
        public const string WulfrumFurnitureItems = WulfrumFurniture + "Items/";

        public const string Food = Assets + "Items/Food/";
        public const string SkyItems = Assets + "Items/Sky/";
        public const string SkyNPCs = Assets + "NPCs/Sky/";
        public const string Marnite = Assets + "Items/Marnite/";

        public const string Tiles = Assets + "Tiles/";

        public static class PrimShaders
        {
            public static Effect GlowingStreakPrimitive => Scene["GlowingStreakPrimitive"].GetShader().Shader;

            /// <summary>
            /// Samples the horizontal 'trailTexture', scrolled by 'time' <br/>
            /// Opacity of the trail is faded at the edges, and fades along the lenght of the trail based on 'fadeDistance' and 'fadePower' <br/>
            /// Final opacity is intensified by 1.5x, then multiplied with the primitive's vertex colors
            /// <code>
            /// float time;
            /// float fadeDistance;
            /// float fadePower;
            /// texture trailTexture; //Grayscale texture. Uses the red channel instead of alpha for opacity
            /// </code>
            /// 
            /// Used by many different things, notably the trails on wulfrum weapons
            /// </summary>
            public static Effect TaperedTextureMap => Scene["Primitive_TaperedTextureMap"].GetShader().Shader;

            /// <summary>
            /// Samples the provided 'sampleTexture' using the frame info from 'frame' and 'textureResolution'<br/>
            /// Sampled color is multiplied by the primitive's vertex colors
            /// <code>
            /// float4 frame;
            /// float2 textureResolution;
            /// texture sampleTexture;
            /// </code>
            /// 
            /// Used by <see cref="VerletNet"/> when rendering framed verlet links, and the <see cref="WisteriaTree"/>  and <see cref="FrostTree"/> for rendering the swaying treetop
            /// </summary>
            public static Effect FramedTextureMap => Scene["Primitive_FramedTextureMap"].GetShader().Shader;

            /// <summary>
            /// Samples the provided 'sampleTexture' using the frame info from 'frame' and 'textureResolution'. <br/>
            /// Additionally allows the mapped texture to be shortened along a primitive strip using 'stretch'.
            /// <code>
            /// float4 frame;
            /// float2 textureResolution;
            /// texture sampleTexture;
            /// float stretch;
            /// </code>
            /// </summary>
            public static Effect StretchedTextureMap => Scene["Primitive_StretchedTextureMap"].GetShader().Shader;

            /// <summary>
            /// Samples the provided 'sampleTexture' along the primitive (Texture must be horizontal)<br/>
            /// Sampling coordinates can be tiled or stretched using 'repeats', and scrolled with 'scroll' <br/>
            /// Sampled color is multiplied by the primitive's vertex colors
            /// <code>
            /// float4 scroll;
            /// float2 repeats;
            /// texture sampleTexture;
            /// </code>
            /// 
            /// Used by <see cref="VerletNet"/> when rendering non-framed verlet links, notably <see cref="Crabulon"/>'s numerous vines
            /// </summary>
            public static Effect TextureMap => Scene["Primitive_TextureMap"].GetShader().Shader;

            /// <summary>
            /// Samples the provided 'sampleTexture' along the primitive (Texture must be horizontal)<br/>
            /// Sampling coordinates can be tiled or stretched using 'repeats', and scrolled with 'scroll' <br/>
            /// Sampled color is heavily intensified, then multiplied by the vertex colors, leading to a glowing hot look
            /// <code>
            /// float4 scroll;
            /// float2 repeats;
            /// texture sampleTexture; //Grayscale texture. Uses the red channel instead of alpha for opacity
            /// </code>
            /// 
            /// Used by <see cref="CircularPulseShine"/> when rendering its glowing ring, or for glowing trails on desert scourge's electric drops
            /// </summary>
            public static Effect IntensifiedTextureMap => Scene["Primitive_IntensifiedTextureMap"].GetShader().Shader;

            /// <summary>
            /// Samples the horizontal 'sampleTexture', scrolled by 'time'<br/>
            /// Sampling coordinates can be tiled or stretched both horizontally and vertically using 'repeats' and 'verticalStretch'<br/>
            /// Layers the same 'sampleTexture' upon itself, using 'overlayScroll' and 'overlayOpacity' to offset and fade the second layer<br/>
            /// Then samples 'streaksNoiseTexture' in 1D, scrolling through the pixel-slice using half the 'time' offset <br/>
            /// Final result is sampleTexture multiplied by the vertex colors and intensified for a glowing look, and then multiplied by the 1D streak sample, giving it a streaky look
            /// <code>
            /// 
            /// float time;
            /// float repeats;
            /// float verticalStretch; //Most often to set to 0.5 to stretch the trail textures that tend to only occupy the middle of the texture
            /// texture sampleTexture; //Grayscale texture. Uses the red channel instead of alpha for opacity
            /// 
            /// float overlayScroll; //Often set to scroll opposite to 'time'
            /// float overlayOpacity; //Usually 0.5
            /// 
            /// float streakScale;
            /// texture streakNoiseTexture; //Grayscale texture, also uses the red channel for opacity
            /// </code>
            /// 
            /// Used by a lot of Nautilus projectiles, like the <see cref="NautilusTrident"/>  and <see cref="SignathionSpectralBolt"/>. Also used by some weapons like the <see cref="SyringeGunDart"/>
            /// </summary>
            public static Effect StreakyTrail => Scene["Primitive_StreakyTrail"].GetShader().Shader;


            /// <summary>
            /// Samples the horizontal 'sampleTexture', scrolled by 'scroll' and stretched/tiled by 'repeats'<br/>
            /// Samples the same texture again, but vertically shrunk by 'coreShrink', to act as the central glowing core for the noise<br/>
            /// Additionally samples a second texture, 'overlayNoise' with its own repeats and scroll parameters<br/>
            /// Final result is a vertex-tinted combo of the main texture and the overlay noise, with the glowing core overlaid ontop for added brightness<br/>
            /// <code>
            /// 
            /// float repeats;
            /// float scroll;
            /// float coreShrink; //Usually 0.5
            /// float coreOpacity;
            /// texture sampleTexture; //Grayscale texture, uses the red channel for opacity and for brightening effects
            ///
            /// float overlayRepeats;
            /// float overlayScroll;
            /// float overlayVerticalScale;
            /// texture overlayNoise; //Grayscale texture, also uses the red channel for opacity
            /// </code>
            /// 
            /// Used by some weapons like <see cref="ToxicBlowpipe"/>  and <see cref="AmpstringBow"/>
            /// </summary>
            public static Effect GlowingCoreWithOverlaidNoise => Scene["Primitive_GlowingCoreWithOverlaidNoise"].GetShader().Shader;

            /// <summary>
            /// Samples the horizontal 'sampleTexture', scrolled by 'scroll' and stretched/tiled by 'repeats'<br/>
            /// <code>
            /// 
            /// float repeats;
            /// float scroll;
            /// texture sampleTexture; //Grayscale texture, uses the red channel for opacity and for brightening effects
            /// 
            /// </code>
            /// 
            /// Used by some weapons like <see cref="ObsidianThrowingDagger"/>
            /// </summary>
            public static Effect ElectricGlowTrail => Scene["Primitive_ElectricGlowTrail"].GetShader().Shader;

        }

        public static class PixelShaders
        {

        }

        public static class CommonTextures
        {
            private static Asset<Texture2D> bloomCircle;
            /// <summary>
            /// A round circle of bloom, with a black background
            /// </summary>
            public static Asset<Texture2D> BloomCircle
            {
                get
                {
                    bloomCircle ??= ModContent.Request<Texture2D>(Particles + "BloomCircle");
                    return bloomCircle;
                }
            }

            private static Asset<Texture2D> bigLight;
            /// <summary>
            /// A circle of bloom over a transparent background. <br/>
            /// Unlike <see cref="BloomCircle"/>'s pretty smooth gradient, this one has a bigger solid core with a harsher falloff at the edges
            /// </summary>
            public static Asset<Texture2D> BigBloomCircle
            {
                get
                {
                    bigLight ??= ModContent.Request<Texture2D>(Particles + "BigLight");
                    return bigLight;
                }
            }


            private static Asset<Texture2D> pixelLight;
            /// <summary>
            /// A small, 72x72 circle of bloom over a transparent background, at a size where the pixels are noticeable<br/>
            /// Unlike <see cref="BloomCircle"/>'s pretty smooth gradient, this one has a bigger solid core with a harsher falloff at the edges
            /// </summary>
            public static Asset<Texture2D> PixelBloomCircle
            {
                get
                {
                    pixelLight ??= ModContent.Request<Texture2D>(Particles + "Light");
                    return pixelLight;
                }
            }

            private static Asset<Texture2D> bloomStreak;
            /// <summary>
            /// That one thin streak with bloom near the core, that terraria uses a lot for any star shaped effects
            /// </summary>
            public static Asset<Texture2D> BloomStreak
            {
                get
                {
                    bloomStreak ??= ModContent.Request<Texture2D>(Particles + "StreakBloom");
                    return bloomStreak;
                }
            }

            private static Asset<Texture2D> bloomFlare;
            /// <summary>
            /// Round bloom with streaky flares all around it on a black background
            /// </summary>
            public static Asset<Texture2D> BloomFlare
            {
                get
                {
                    bloomFlare ??= ModContent.Request<Texture2D>(Particles + "BloomFlare");
                    return bloomFlare;
                }
            }

            private static Asset<Texture2D> bloomAurora;
            /// <summary>
            /// A pointed symmetrical line of bloom in the general shape of a downwards kite, with a wide base that tapers upwards. Has a black background
            /// </summary>
            public static Asset<Texture2D> BloomDiamondColumn
            {
                get
                {
                    bloomAurora ??= ModContent.Request<Texture2D>(Particles + "AuroraLine");
                    return bloomAurora;
                }
            }

            private static Asset<Texture2D> chromaBurst;
            /// <summary>
            /// Shockwave looking circular burst with chromatic abberation, on a black background
            /// </summary>
            public static Asset<Texture2D> ChromaBurst
            {
                get
                {
                    chromaBurst ??= ModContent.Request<Texture2D>(Particles + "ChromaBurst");
                    return chromaBurst;
                }
            }

            private static Asset<Texture2D> bloomCircleTransparent;
            /// <summary>
            /// Bloom circle with a transparent background so it works with nonpremultiplied blend
            /// </summary>
            public static Asset<Texture2D> BloomCircleTransparent
            {
                get
                {
                    bloomCircleTransparent ??= ModContent.Request<Texture2D>(Assets + "GlowTransparentBG");
                    return bloomCircleTransparent;
                }
            }
        }

        public static class NoiseTextures
        {
            private static Asset<Texture2D> certifiedCrustyNoise;
            public static Asset<Texture2D> CertifiedCrusty
            {
                get
                {
                    certifiedCrustyNoise ??= ModContent.Request<Texture2D>(Noise + "CertifiedCrustyNoise");
                    return certifiedCrustyNoise;
                }
            }

            private static Asset<Texture2D> cracksDisplace;
            public static Asset<Texture2D> CracksDisplace
            {
                get
                {
                    cracksDisplace ??= ModContent.Request<Texture2D>(Noise + "CracksDisplace");
                    return cracksDisplace;
                }
            }

            private static Asset<Texture2D> cracksDisplace2;
            public static Asset<Texture2D> CracksDisplace2
            {
                get
                {
                    cracksDisplace2 ??= ModContent.Request<Texture2D>(Noise + "CracksDisplace2");
                    return cracksDisplace2;
                }
            }

            private static Asset<Texture2D> cracks;
            public static Asset<Texture2D> Cracks
            {
                get
                {
                    cracks ??= ModContent.Request<Texture2D>(Noise + "CracksNoise");
                    return cracks;
                }
            }

            private static Asset<Texture2D> cracksLarge;
            public static Asset<Texture2D> CracksLarge
            {
                get
                {
                    cracksLarge ??= ModContent.Request<Texture2D>(Noise + "LargeCracksNoise");
                    return cracksLarge;
                }
            }


            private static Asset<Texture2D> displaceBig;
            /// <summary>
            /// Very large scaled displace noise, not much fine noise at all, quite blurry
            /// </summary>
            public static Asset<Texture2D> DisplaceBig
            {
                get
                {
                    displaceBig ??= ModContent.Request<Texture2D>(Noise + "DisplaceNoise1");
                    return displaceBig;
                }
            }

            private static Asset<Texture2D> displace;
            /// <summary>
            /// Mid-scaled perlin displace, average granularity
            /// </summary>
            public static Asset<Texture2D> Displace
            {
                get
                {
                    displace ??= ModContent.Request<Texture2D>(Noise + "DisplaceNoise2");
                    return displace;
                }
            }

            private static Asset<Texture2D> displaceSmall;
            /// <summary>
            /// Low-scaled perlin displace, lots of fine-scaled grain noise
            /// </summary>
            public static Asset<Texture2D> DisplaceSmall
            {
                get
                {
                    displaceSmall ??= ModContent.Request<Texture2D>(Noise + "DisplaceNoise3");
                    return displaceSmall;
                }
            }

            private static Asset<Texture2D> perlinLowContrast;
            /// <summary>
            /// Perlin noise with lessened contrast, hovers around grayish tones
            /// </summary>
            public static Asset<Texture2D> PerlinLowContrast
            {
                get
                {
                    perlinLowContrast ??= ModContent.Request<Texture2D>(Noise + "GradientNoise");
                    return perlinLowContrast;
                }
            }

            private static Asset<Texture2D> perlin;
            /// <summary>
            /// Perlin noise
            /// </summary>
            public static Asset<Texture2D> Perlin
            {
                get
                {
                    perlin ??= ModContent.Request<Texture2D>(Noise + "PerlinNoise");
                    return perlin;
                }
            }

            private static Asset<Texture2D> yepBrush;
            /// <summary>
            /// Scribbles drawn with yep brush
            /// </summary>
            public static Asset<Texture2D> YepBrush
            {
                get
                {
                    yepBrush ??= ModContent.Request<Texture2D>(Noise + "IbansBrush");
                    return yepBrush;
                }
            }


            private static Asset<Texture2D> iridescentThunder;
            /// <summary>
            /// Thunderous streaks with some iridescent sheen to it. Contains more midtones and less bright peaks than <see cref="IridescentThunder2"/>
            /// </summary>
            public static Asset<Texture2D> IridescentThunder
            {
                get
                {
                    iridescentThunder ??= ModContent.Request<Texture2D>(Noise + "IridescentThunder");
                    return iridescentThunder;
                }
            }


            private static Asset<Texture2D> iridescentThunder2;
            /// <summary>
            /// Thunderous streaks with some iridescent sheen to it. Much streakier than <see cref="IridescentThunder"/>
            /// </summary>
            public static Asset<Texture2D> IridescentThunder2
            {
                get
                {
                    iridescentThunder2 ??= ModContent.Request<Texture2D>(Noise + "IridescentThunder2");
                    return iridescentThunder2;
                }
            }

            private static Asset<Texture2D> lightning;
            /// <summary>
            /// Very thin electric thunder streaks, not much bloom around them at all
            /// </summary>
            public static Asset<Texture2D> Lightning
            {
                get
                {
                    lightning ??= ModContent.Request<Texture2D>(Noise + "LightningNoise");
                    return lightning;
                }
            }

            private static Asset<Texture2D> manifold;
            /// <summary>
            /// Manifold noise that hovers around darker tones, warbled all around. Sometimes used as displace noise eventhough its grayscale
            /// </summary>
            public static Asset<Texture2D> Manifold
            {
                get
                {
                    manifold ??= ModContent.Request<Texture2D>(Noise + "ManifoldDisplaceNoise");
                    return manifold;
                }
            }

            private static Asset<Texture2D> manifold2;
            /// <summary>
            /// Manifold noise that's brighter than <see cref="Manifold"/>, made up of separate regions that are brighter and darker
            /// </summary>
            public static Asset<Texture2D> Manifold2
            {
                get
                {
                    manifold2 ??= ModContent.Request<Texture2D>(Noise + "ManifoldNoise");
                    return manifold2;
                }
            }

            private static Asset<Texture2D> manifold3;
            /// <summary>
            /// The same as <see cref="Manifold2"/> but with a different pattern
            /// </summary>
            public static Asset<Texture2D> Manifold3
            {
                get
                {
                    manifold3 ??= ModContent.Request<Texture2D>(Noise + "WateryNoise");
                    return manifold3;
                }
            }

            private static Asset<Texture2D> manifoldRidges;
            /// <summary>
            /// Manifold noise with very visible dark ridges and bright peaks, akin to seeing mountains from above
            /// </summary>
            public static Asset<Texture2D> ManifoldRidges
            {
                get
                {
                    manifoldRidges ??= ModContent.Request<Texture2D>(Noise + "PrettyManifoldNoise");
                    return manifoldRidges;
                }
            }

            private static Asset<Texture2D> milkyBlob;
            /// <summary>
            /// White background with rounded dark streaks that create outlines for blob-shapes across the white. Very high contrast
            /// </summary>
            public static Asset<Texture2D> MilkyBlob
            {
                get
                {
                    milkyBlob ??= ModContent.Request<Texture2D>(Noise + "MilkyBlobNoise");
                    return milkyBlob;
                }
            }

            private static Asset<Texture2D> mosaic;
            /// <summary>
            /// Two layers of voronoi cells overlaid onto one another, with one at a higher scale that's a bit less opaque
            /// </summary>
            public static Asset<Texture2D> Mosaic
            {
                get
                {
                    mosaic ??= ModContent.Request<Texture2D>(Noise + "MosaicNoise");
                    return mosaic;
                }
            }

            private static Asset<Texture2D> patchyTall;
            /// <summary>
            /// Vertically stretched noise with high contrast B/W, noise itself is blurred vertically
            /// </summary>
            public static Asset<Texture2D> PatchyTall
            {
                get
                {
                    patchyTall ??= ModContent.Request<Texture2D>(Noise + "PatchyTallNoise");
                    return patchyTall;
                }
            }

            private static Asset<Texture2D> pebbles;
            /// <summary>
            /// The pebbles texture from shadertoy
            /// </summary>
            public static Asset<Texture2D> Pebbles
            {
                get
                {
                    pebbles ??= ModContent.Request<Texture2D>(Noise + "PebblesNoise");
                    return pebbles;
                }
            }

            private static Asset<Texture2D> rgbGrime;
            /// <summary>
            /// Multicolored noise that appears to be a patchwork of small 4x4 noisy color clusters. Ripped from rain world
            /// </summary>
            public static Asset<Texture2D> RGBGrime
            {
                get
                {
                    rgbGrime ??= ModContent.Request<Texture2D>(Noise + "RainbowGrimeNoise");
                    return rgbGrime;
                }
            }

            private static Asset<Texture2D> verticalHolo;
            /// <summary>
            /// Vertically stretched blurry rainbow blobs
            /// </summary>
            public static Asset<Texture2D> VerticalHolo
            {
                get
                {
                    verticalHolo ??= ModContent.Request<Texture2D>(Noise + "RainbowPerlin");
                    return verticalHolo;
                }
            }

            private static Asset<Texture2D> rgbDark;
            /// <summary>
            /// Pure pixel-random noise with each pixel containing a different color. Colors are all max sat (therefore never going towards white)
            /// </summary>
            public static Asset<Texture2D> RGBDark
            {
                get
                {
                    rgbDark ??= ModContent.Request<Texture2D>(Noise + "RGBNoise");
                    return rgbDark;
                }
            }

            private static Asset<Texture2D> rgb;
            /// <summary>
            /// Pure pixel-random noise with each pixel containing a different color
            /// </summary>
            public static Asset<Texture2D> RGB
            {
                get
                {
                    rgb ??= ModContent.Request<Texture2D>(Noise + "RGBNoise2");
                    return rgb;
                }
            }


            private static Asset<Texture2D> techy;
            /// <summary>
            /// Square techy noise used by wulfrum effects notably
            /// </summary>
            public static Asset<Texture2D> Techy
            {
                get
                {
                    techy ??= ModContent.Request<Texture2D>(Noise + "TechyNoise");
                    return techy;
                }
            }

            private static Asset<Texture2D> tireScratch;
            /// <summary>
            /// Horizontal noise using the tirescratch brush, with rough edges
            /// </summary>
            public static Asset<Texture2D> TireScratch
            {
                get
                {
                    tireScratch ??= ModContent.Request<Texture2D>(Noise + "TireScratch");
                    return tireScratch;
                }
            }

            private static Asset<Texture2D> troubledWatery;
            /// <summary>
            /// Looks like troubled water
            /// </summary>
            public static Asset<Texture2D> TroubledWatery
            {
                get
                {
                    troubledWatery ??= ModContent.Request<Texture2D>(Noise + "TroubledWateryNoise");
                    return troubledWatery;
                }
            }

            private static Asset<Texture2D> troubledWateryDarker;
            /// <summary>
            /// The same as <see cref="TroubledWatery"/>, but the darker tones are darker, reaching black
            /// </summary>
            public static Asset<Texture2D> TroubledWateryDarker
            {
                get
                {
                    troubledWateryDarker ??= ModContent.Request<Texture2D>(Noise + "TroubledWateryNoise2");
                    return troubledWateryDarker;
                }
            }

            private static Asset<Texture2D> turbulent;
            /// <summary>
            /// High contrast distored noise with warbly black lines separating white patches. Somewhat horizontally stretched
            /// </summary>
            public static Asset<Texture2D> Turbulent
            {
                get
                {
                    turbulent ??= ModContent.Request<Texture2D>(Noise + "TurbulentNoise");
                    return turbulent;
                }
            }

            private static Asset<Texture2D> turbulent2;
            /// <summary>
            /// Highly distorted noise with high contrast, but less than <see cref="turbulent"/>. No particular stretch
            /// </summary>
            public static Asset<Texture2D> Turbulent2
            {
                get
                {
                    turbulent2 ??= ModContent.Request<Texture2D>(Noise + "TurbulentNoise2");
                    return turbulent2;
                }
            }

            private static Asset<Texture2D> voronoi;
            /// <summary>
            /// Voronoi
            /// </summary>
            public static Asset<Texture2D> Voronoi
            {
                get
                {
                    voronoi ??= ModContent.Request<Texture2D>(Noise + "Voronoi");
                    return voronoi;
                }
            }

            private static Asset<Texture2D> voronoiDots;
            /// <summary>
            /// Small white dots on dark bg, taken from the highest peaks of voronoi noise
            /// </summary>
            public static Asset<Texture2D> VoronoiDots
            {
                get
                {
                    voronoiDots ??= ModContent.Request<Texture2D>(Noise + "VoronoiseDots");
                    return voronoiDots;
                }
            }

            private static Asset<Texture2D> voronoiMountains;
            /// <summary>
            /// Voronoi noise with distance from cell edge used, leading to dark ridges between shapes and white peaks at the top, leading to an appearance of polygonal mountains
            /// </summary>
            public static Asset<Texture2D> VoronoiMountains
            {
                get
                {
                    voronoiMountains ??= ModContent.Request<Texture2D>(Noise + "VoronoiShapes");
                    return voronoiMountains;
                }
            }

            private static Asset<Texture2D> viscous;
            /// <summary>
            /// Akin to a reversed <see cref="MilkyBlob"/> noise, with dark blobs and white curved outlines around them. high contrast
            /// </summary>
            public static Asset<Texture2D> Viscous
            {
                get
                {
                    viscous ??= ModContent.Request<Texture2D>(Noise + "ViscousNoise");
                    return viscous;
                }
            }

            private static Asset<Texture2D> whiteNoise;
            /// <summary>
            /// Pure tv static white noise
            /// </summary>
            public static Asset<Texture2D> WhiteNoise
            {
                get
                {
                    whiteNoise ??= ModContent.Request<Texture2D>(Noise + "WhiteNoise");
                    return whiteNoise;
                }
            }

            private static Asset<Texture2D> downwardsWateryCoulée;
            /// <summary>
            /// Looks like layered blorps of water
            /// </summary>
            public static Asset<Texture2D> DownwardsWateryCoulée
            {
                get
                {
                    downwardsWateryCoulée ??= ModContent.Request<Texture2D>(Noise + "WaterGlorpNoise");
                    return downwardsWateryCoulée;
                }
            }

            private static Asset<Texture2D> voronoiNormalMap;
            /// <summary>
            /// A normal map generated using voronoi noise as a height map
            /// </summary>
            public static Asset<Texture2D> VoronoiNormalMap
            {
                get
                {
                    voronoiNormalMap ??= ModContent.Request<Texture2D>(Noise + "VoronoiNormal");
                    return voronoiNormalMap;
                }
            }
        }
    }
}