using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.NPCs.Wulfrum;
using Terraria.Localization;

namespace CalamityFables.Core
{
    public static class FablesModCalls
    {
        private static Func<TArgument> GetDelegateArgument<TArgument>(object arg, string failureExceptionText)
        {
            if (arg is TArgument parsedArgument)
                return () => parsedArgument;
            if (arg is Func<TArgument> function)
                return function;

            throw new ArgumentException(failureExceptionText);
        }

        public static object Call(params object[] args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            if (args.Length == 0)
                throw new ArgumentException("Arguments cannot be empty!");

            if (args[0] is not string callType)
                throw new ArgumentException("First argument needs to be a string!");


            callType = callType.ToLower();

            switch (callType)
            {
                case "progression.defeatedwulfrumtrial":
                    return WulfrumNexus.CanWulfrumNexusSpawnNaturally;
                case "progression.defeatednautilus":
                    return SirNautilusDialogue.DefeatedNautilus;
                case "progression.defeateddesertscourge":
                    return WorldProgressionSystem.DefeatedDesertScourge;
                case "progression.defeatedcrabulon":
                    return WorldProgressionSystem.DefeatedCrabulon;
                case "progression.crabulonkillcount":
                    return WorldProgressionSystem.crabulonsDefeated;

                case "gravestones.getgravesetindex":
                    if (args[1] is not string gravesetToFindName)
                        throw new ArgumentException("Second argument needs to be the name of the graveset you're looking for the index of");
                    return CustomGravesPlayer.GetGravesetIndex(gravesetToFindName.ToLower());

                case "gravestones.registerselectorsheet":
                    if (args[1] is not Mod sheetMod)
                        throw new ArgumentException("Second argument needs to be the mod instance loading the sheet");
                    if (args[2] is not string sheetTexturePath)
                        throw new ArgumentException("Third argument needs to be the texturepath to the texture sheet");
                    CustomGravesPlayer.RegisterUITexture(sheetMod, sheetTexturePath);
                    return null;

                case "gravestones.autoloadgraveset":
                    if (args[1] is not Mod gravesetMod)
                        throw new ArgumentException("Second argument needs to be the mod instance autoloading the graves");
                    if (args[2] is not string gravesetTexturePath)
                        throw new ArgumentException("Third argument needs to be the texturepath to the texture sheet");
                    if (args[3] is not string gravesetName)
                        throw new ArgumentException("Fourth argument needs to be the internal name of the grave set");
                    if (args[4] is not bool graveGilded)
                        throw new ArgumentException("Fifth argument needs to be wether or not the graveset is a gilded variant");
                    if (args[5] is not string[] graveVariantNames || graveVariantNames.Length != 4)
                        throw new ArgumentException("Sixth argument needs to be a list for the 4 individual internal names of the gravestone variants (in order on the tile sprite sheet)");
                    if (args[6] is not Color gravesetMapColor)
                        throw new ArgumentException("Seventh argument needs to be the map color of the grave set");
                    if (args[7] is not int gravesetDust)
                    {
                        if (args[7] is short gravesetDustShort)
                            gravesetDust = gravesetDustShort;
                        else
                            throw new ArgumentException("Eighth argument needs to be the dust type for the grave set");
                    }


                    return CustomGravesPlayer.AutoloadModdedGraveSetSingle(gravesetMod, gravesetTexturePath, gravesetName, graveGilded, graveVariantNames, gravesetMapColor, gravesetDust);

                case "gravestones.registergraveset":
                    if (args[1] is not Mod gravesMod)
                        throw new ArgumentException("Second argument needs to be the mod instance autoloading the graveset");
                    if (args[2] is not string gravesetSaveKey)
                        throw new ArgumentException("Third argument needs to be the index of the graveset on the selector sheet you registered");
                    if (args[3] is not int gravesetIconIndex)
                        throw new ArgumentException("Fourth argument needs to be the unique save key for the graveset");
                    if (args[4] is not LocalizedText gravesetLocalizedName)
                        throw new ArgumentException("Fifth argument needs to be the localized name of the graveset");
                    if (args[5] is not List<int> normalGraveProjectiles || args[6] is not List<int> gildedGraveProjectiles)
                        throw new ArgumentException("Sixth and Seventh arguments need to be the lists of the projectiles for the graves (normal and gilded)");
                    if (args[7] is not Func<Player, bool> gravesetBiomeCheck)
                        throw new ArgumentException("Eighth argument must be a delegate check that returns if the player is in the matching biome for the grave set");

                    float gravesetBiomePriority = 1f;
                    if (args.Length >= 9)
                    {
                        if (args[8] is not float _gravesetBiomePriority)
                            throw new ArgumentException("Ninth argument must be the graveset's biome priority as a float (Spreading biomes use 2.5 while hell uses 3)");
                        else
                            gravesetBiomePriority = _gravesetBiomePriority;
                    }

                    Func<Player, bool[], bool> gravesetCustomUnlock = null;
                    if (args.Length >= 10)
                    {
                        if (args[9] is not Func<Player, bool[], bool> _gravesetCustomUnlock)
                            throw new ArgumentException("Tenth argument (Unlock override) must be a delegate that takes in an array of booleans (the player's unlocked graves indices) and returns wether or not the player can select this graveset");
                        else
                            gravesetCustomUnlock = _gravesetCustomUnlock;
                    }                        

                    return CustomGravesPlayer.LoadModdedGraveType(gravesMod, gravesetSaveKey, gravesetIconIndex, gravesetLocalizedName, normalGraveProjectiles, gildedGraveProjectiles, gravesetBiomeCheck, gravesetBiomePriority, gravesetCustomUnlock);

                case "gravestones.unlockgraveset":
                    if (args[1] is not Player gravesetUnlockPlayer)
                        throw new ArgumentException("Second argument needs to be the player for whom to unlock the graveset");
                    if (args[2] is not int gravesetIndex)
                        throw new ArgumentException("Third argument needs to be the index of the gravestone to unlock");

                    CustomGravesPlayer.graveData[(CustomGravesPlayer.CustomGraveType)gravesetIndex].Unlock(gravesetUnlockPlayer.whoAmI);
                    return true;

                case "vfx.customdrawlayers.drawbehindnonsolidtiles":
                    FablesDrawLayers.DrawThings_BehindNonSolidTiles();
                    return true;
                case "vfx.customdrawlayers.drawbehindsolidtiles":
                    FablesDrawLayers.DrawThings_BehindSolidTilesAndBackgroundNPCs();
                    return true;
                case "vfx.customdrawlayers.drawabovesolidtiles":
                    FablesDrawLayers.DrawThings_AboveSolidTiles();
                    return true;

                case "vfx.displaybossintrocard":
                    if (!FablesConfig.Instance.BossIntroCardsActivated)
                        return false;
                    if (BossIntroScreens.currentCard != null)
                        return false;

                    if (args.Length < 9)
                        throw new ArgumentException("vfx.displayBossIntroCard needs 8 arguments (minimum) to be valid!");

                    Func<string> bossNameFunction = GetDelegateArgument<string>(args[1], "First argument for vfx.displayBossIntroCard must be the boss name as a string or Func<string>");
                    if (args[2] is not string bossTitle)
                        throw new ArgumentException("Second argument for vfx.displayBossIntroCard must be the boss title as a string");
                    if (args[3] is not int duration)
                        throw new ArgumentException("Second argument for vfx.displayBossIntroCard must be the duration of the card in frames as an int");
                    if (args[4] is not bool flipped)
                        throw new ArgumentException("Fourth argument for vfx.displayBossIntroCard must be a bool to get the bars horizontal flip");
                    if (args[5] is not Color edgeColor)
                        throw new ArgumentException("Fifth argument for vfx.displayBossIntroCard must be the Color of the bar's borders");
                    if (args[6] is not Color titleColor)
                        throw new ArgumentException("Sixth argument for vfx.displayBossIntroCard must be the Color of the boss' title");
                    if (args[7] is not Color nameColorChroma1 || args[8] is not Color nameColorChroma2)
                        throw new ArgumentException("Seventh and eight arguments for vfx.displayBossIntroCard must be Colors of the chromatic abberation effect around the boss name");

                    BossIntroCard newCard = new BossIntroCard(bossNameFunction, bossTitle, duration, flipped, edgeColor, titleColor, nameColorChroma1, nameColorChroma2);
                    if (args.Length > 10)
                    {
                        if (args[9] is not string musicTitle || args[10] is not string composerName)
                            throw new ArgumentException("Ninth and tenth arguments for vfx.displayBossIntroCard must be the music title and composer name as a string");
                        newCard.music = new MusicTrackInfo(musicTitle, composerName);
                    }

                    BossIntroScreens.currentCard = newCard;
                    return true;

                case "world.wulfrumbunkerrect":
                    return PointOfInterestMarkerSystem.WulfrumBunkerRectangle;
                case "world.sealedchamberrect":
                    return PointOfInterestMarkerSystem.NautilusChamberRectangle;
                case "world.insidewulfrumbunker":
                    {
                        if (args[1] is Player player)
                        {
                            Rectangle bunkerBounds = PointOfInterestMarkerSystem.WulfrumBunkerRectangle;
                            bunkerBounds.Inflate(-2, -2);
                            bunkerBounds.Height += 12;
                            bunkerBounds.X *= 16;
                            bunkerBounds.Y *= 16;
                            bunkerBounds.Width *= 16;
                            bunkerBounds.Height *= 16;
                            return player.Hitbox.Intersects(bunkerBounds);
                        }
                        else if (args[1] is Point point)
                            return PointOfInterestMarkerSystem.WulfrumBunkerPos != Point.Zero && PointOfInterestMarkerSystem.WulfrumBunkerRectangle.Contains(point);
                        else
                            throw new ArgumentException("First argument for world.insideWulfrumBunker call must be a player or a Point!");
                    }
                case "world.insidesealedchamber":
                    {
                        if (args[1] is Player player)
                            return player.Hitbox.Intersects(PointOfInterestMarkerSystem.NautilusChamberWorldRectangle);
                        else if (args[1] is Point point)
                            return PointOfInterestMarkerSystem.NautilusChamberPos != Vector2.Zero && PointOfInterestMarkerSystem.NautilusChamberRectangle.Contains(point);
                        else
                            throw new ArgumentException("First argument for world.insideSealedChamber call must be a player or a Point!");
                    }
            }

            return false;
        }
    }
}