namespace CalamityFables.Core
{
    //Done to help ming on jade mod
    /*
    public class PaintAnythingSystem : ModSystem
    {
        private static IList<ARenderTargetHolder> paintSystemRequests;

        public override void Load()
        {
            var tilePaintSystemRequests = typeof(TilePaintSystemV2).GetField("_requests", BindingFlags.NonPublic | BindingFlags.Instance);
            paintSystemRequests = tilePaintSystemRequests.GetValue(Main.instance.TilePaintSystem) as IList<ARenderTargetHolder>; // grab the requests through reflection
        }

        public static event Action ClearRenderTargets;
        public override void Unload()
        {
            Main.QueueMainThreadAction(() => ClearRenderTargets?.Invoke());
        }

        private static void RequestPaintTexture(Dictionary<UniversalVariationKey, WhateverPaintRenderTargetHolder> textureDict, ref UniversalVariationKey lookupKey, string texturePath, int copySettingsFrom = -1, TreePaintingSettings customSettings = null)
        {
            if (!textureDict.TryGetValue(lookupKey, out WhateverPaintRenderTargetHolder target))
            {
                target = new WhateverPaintRenderTargetHolder(lookupKey, texturePath, copySettingsFrom, customSettings);
                textureDict.Add(lookupKey, target);
            }

            //We don't need to process the requests ourselves, just let the paint system do it, gg ez
            if (!target.IsReady)
                paintSystemRequests.Add(target);
        }

        public static Texture2D TryGetTexturePaintAndRequestIfNotReady(Dictionary<UniversalVariationKey, WhateverPaintRenderTargetHolder> textureDict, int type, int paintColor, string texturePath, int copySettingsFrom = -1, TreePaintingSettings customSettings = null)
        {
            UniversalVariationKey variationKey = new UniversalVariationKey(type, paintColor);
            if (textureDict.TryGetValue(variationKey, out WhateverPaintRenderTargetHolder value) && value.IsReady)
                return value.Target;

            RequestPaintTexture(textureDict, ref variationKey, texturePath, copySettingsFrom, customSettings);
            return null;
        }


    }

    public static class PaintAnythingExtensions
    {
        public static Texture2D TryGetTexturePaintAndRequestIfNotReady(this Dictionary<UniversalVariationKey, WhateverPaintRenderTargetHolder> textureDict, int type, int paintColor, string texturePath, int copySettingsFrom = -1, TreePaintingSettings customSettings = null)
        {
            return PaintAnythingSystem.TryGetTexturePaintAndRequestIfNotReady(textureDict, type, paintColor, texturePath, copySettingsFrom, customSettings);
        }
    }

    public class WhateverPaintRenderTargetHolder : ARenderTargetHolder
    {
        public UniversalVariationKey Key;
        public int tileTypeToCopySettingsFrom;
        public TreePaintingSettings paintSettings;
        public string texturePath;

        public WhateverPaintRenderTargetHolder(UniversalVariationKey key, string texture, int copySettingsFrom = -1, TreePaintingSettings customSettings = null)
        {
            Key = key;
            texturePath = texture;

            //Lets us use custom parameters for the paint settings 
            if (customSettings != null)
                paintSettings = customSettings;
            //Else copy vanilla settings (by default -1, so not anything fancy)
            else
                paintSettings = TreePaintSystemData.GetTileSettings(copySettingsFrom, 0);
        }

        public override void Prepare()
        {
            Asset<Texture2D> asset = ModContent.Request<Texture2D>(texturePath);
            asset.Wait?.Invoke();
            PrepareTextureIfNecessary(asset.Value);
        }

        public override void PrepareShader() => PrepareShader(Key.PaintColor, paintSettings);
    }

    public struct UniversalVariationKey
    {
        public int ThingType;
        public int PaintColor;

        public UniversalVariationKey(int type, int color)
        {
            ThingType = type;
            PaintColor = color;
        }

        public bool Equals(UniversalVariationKey other)
        {
            if (ThingType == other.ThingType)
                return PaintColor == other.PaintColor;

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is UniversalVariationKey)
                return Equals((UniversalVariationKey)obj);

            return false;
        }

        public override int GetHashCode() => (7302013 ^ ThingType.GetHashCode()) * (7302013 ^ PaintColor.GetHashCode());

        public static bool operator ==(UniversalVariationKey left, UniversalVariationKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UniversalVariationKey left, UniversalVariationKey right)
        {
            return !left.Equals(right);
        }
    }
    */
}
