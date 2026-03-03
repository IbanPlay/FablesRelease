using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Tiles.Graves;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using NetEasy;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using static CalamityFables.Core.CustomGravesPlayer;
using static CalamityFables.Helpers.FablesUtils;
using static Mono.Cecil.Cil.OpCodes;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Core
{
    public partial class CustomGravesPlayer : ModPlayer
    {
        public enum CustomGraveType
        {
            Regular,
            None,
            Sandstone,
            Ice,
            Jungle,
            Hell,
            Corrupt,
            Crimson,
            Hallow,
            Sky,
            Beach,
            Mushroom,
            Adaptive,
            Random
        }

        public const int FABLES_GRAVE_TYPE_COUNT = 14;
        public static int ModdedGraveTypeCount = 0;
        public static Dictionary<Mod, int> gravesPerMod = new Dictionary<Mod, int>();
        public static Dictionary<Mod, Asset<Texture2D>> modGraveIconSheets = new Dictionary<Mod, Asset<Texture2D>>();
        public static bool GravesPastRandom = false;

        public static int LastGraveTypeIndex => FABLES_GRAVE_TYPE_COUNT + ModdedGraveTypeCount - 1;

        public CustomGraveType selectedGraveType;
        public static readonly Dictionary<CustomGraveType, CustomGraveData> graveData = new Dictionary<CustomGraveType, CustomGraveData>();
        public static readonly Dictionary<CustomGraveType, CustomGraveData> queuedModdedGraveData = new Dictionary<CustomGraveType, CustomGraveData>();
        public readonly bool[] unlockedGraves = new bool[FABLES_GRAVE_TYPE_COUNT + ModdedGraveTypeCount];

        public CustomGraveData SelectedGrave { 
            get {
                if (graveData.TryGetValue(selectedGraveType, out CustomGraveData selectedData))
                    return selectedData;
                return graveData[0];
            }
        }

        public static List<BuilderToggle> BuilderToggles;
        public static int DummyToggleType;
        private static readonly MethodInfo DrawAccTogglesMethod = typeof(Main).GetMethod("DrawBuilderAccToggles_Inner", BindingFlags.NonPublic | BindingFlags.Instance);
        public static ILHook DrawAccTogglesHook;

        public override void Load()
        {
            gravesPerMod[CalamityFables.Instance] = 0;
            if (!Main.dedServ)
                modGraveIconSheets[CalamityFables.Instance] = Request<Texture2D>(AssetDirectory.UI + "GravestoneSelection");

            ModdedGraveTypeCount = 0;

            On_Player.DropTombstone += DropCustomTombstones;

            if (DrawAccTogglesMethod != null)
                DrawAccTogglesHook = new ILHook(DrawAccTogglesMethod, AddGraveSelectionToUI);

            var builderToggleField = typeof(BuilderToggleLoader).GetField("BuilderToggles", BindingFlags.Static | BindingFlags.NonPublic);
            if (builderToggleField != null)
                BuilderToggles = (List<BuilderToggle>)builderToggleField.GetValue(null);
        }


        public override void SetStaticDefaults()
        {
            graveData.Add(CustomGraveType.Regular, new CustomGraveData(CustomGraveType.Regular, "Default", (_, _) => true));
            graveData.Add(CustomGraveType.None, new CustomGraveData(CustomGraveType.None, "Disabled"));
            graveData.Add(CustomGraveType.Sandstone, new CustomGraveData(CustomGraveType.Sandstone, "Desert",
                SandstoneGrave.ProjectileTypes, GoldenSandstoneGrave.ProjectileTypes, p => p.ZoneDesert || p.ZoneUndergroundDesert || p.ZoneBeach, 1f));
            graveData.Add(CustomGraveType.Ice, new CustomGraveData(CustomGraveType.Ice, "Snow",
                FrozenGrave.ProjectileTypes, GoldenFrozenGrave.ProjectileTypes, p => p.ZoneSnow, 1f));
            graveData.Add(CustomGraveType.Jungle, new CustomGraveData(CustomGraveType.Jungle, "Jungle",
                JungleGrave.ProjectileTypes, LihzahrdGrave.ProjectileTypes, p => p.ZoneJungle || p.ZoneLihzhardTemple, 1.1f));
            graveData.Add(CustomGraveType.Hell, new CustomGraveData(CustomGraveType.Hell, "Hell",
                AshenGrave.ProjectileTypes, MantleGrave.ProjectileTypes, p => p.ZoneUnderworldHeight, 3f));
            graveData.Add(CustomGraveType.Corrupt, new CustomGraveData(CustomGraveType.Corrupt, "Corruption",
                CorruptGrave.ProjectileTypes, DemoniteGrave.ProjectileTypes, p => p.ZoneCorrupt, 2.5f));
            graveData.Add(CustomGraveType.Crimson, new CustomGraveData(CustomGraveType.Crimson, "Crimson",
                CrimsonGrave.ProjectileTypes, CrimtaneGrave.ProjectileTypes, p => p.ZoneCrimson, 2.5f));
            graveData.Add(CustomGraveType.Hallow, new CustomGraveData(CustomGraveType.Hallow, "Hallow",
                PearlstoneGrave.ProjectileTypes, CrystalGrave.ProjectileTypes, p => p.ZoneHallow, 2.5f));
            graveData.Add(CustomGraveType.Sky, new CustomGraveData(CustomGraveType.Sky, "Sky",
                SkyGrave.ProjectileTypes, SkywareGrave.ProjectileTypes, p => p.ZoneSkyHeight, 2f));
            graveData.Add(CustomGraveType.Beach, new CustomGraveData(CustomGraveType.Beach, "Beach",
                SandyGrave.ProjectileTypes, CoralGrave.ProjectileTypes, p => p.ZoneBeach, 1.5f));
            graveData.Add(CustomGraveType.Mushroom, new CustomGraveData(CustomGraveType.Mushroom, "GlowingMushroom",
                MyceliumGrave.ProjectileTypes, GoldenMyceliumGrave.ProjectileTypes, p => p.ZoneGlowshroom, 2.7f));
            graveData.Add(CustomGraveType.Adaptive, new CustomGraveData(CustomGraveType.Adaptive, "BiomeMatching", UnlockedAnyBiomeGraveOption));
            graveData.Add(CustomGraveType.Random, new CustomGraveData(CustomGraveType.Random, "Random", UnlockedAnyBiomeGraveOption));

            //Add the cached modded graves to our list if they were loaded before the fables ones
            foreach (var kvp in queuedModdedGraveData)
                graveData.Add(kvp.Key, kvp.Value);
        }

        public override void Unload()
        {
            ModdedGraveTypeCount = 0;
            graveData?.Clear();
            queuedModdedGraveData?.Clear();
            DrawAccTogglesHook?.Undo();
            gravesPerMod?.Clear();
            modGraveIconSheets?.Clear();
        }

        #region Detours and IL
        private void AddGraveSelectionToUI(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            int builderToggleIndex = 10;
            int toggleType = 16;
            int positionIndex = 0;

            int endIndexIndex = 8;
            int startIndexIndex = 7;
            int iteratorIndex = 9;


            //Gets the iterator and end index variables so we can navigate to the loop's end
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdloca(out endIndexIndex),
                    i => i.MatchCall<Main>("BuilderTogglePageHandler"),
                    i => i.MatchLdloc(out startIndexIndex),
                    i => i.MatchStloc(out iteratorIndex)))
            {
                LogILEpicFail("Add gravestone toggle to UI", "Could not locate call for BuilderTogglePageHandler");
                return;
            }

            //Gets the position calculation
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(iteratorIndex),
                    i => i.MatchLdcI4(12),
                    i => i.MatchRem(),
                    i => i.MatchLdcI4(24),
                    i => i.MatchMul(),
                    i => i.MatchConvR4(),
                    i => i.MatchNewobj<Vector2>(),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(out positionIndex)))
            {
                LogILEpicFail("Add gravestone toggle to UI", "Could not locate position calculation");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(out builderToggleIndex),
                    i => i.MatchCallvirt<BuilderToggle>("get_Type"),
                    i => i.MatchStloc(out toggleType)))
            {
                LogILEpicFail("Add gravestone toggle to UI", "Could not locate property call for builderToggle.Type");
                return;
            }

            ILLabel skipLoopLabel = cursor.DefineLabel();
            cursor.Emit(Ldloc, toggleType);
            cursor.Emit(Ldloc, positionIndex);
            cursor.EmitDelegate(DrawTombstoneStyleIcon);
            cursor.Emit(Brtrue, skipLoopLabel);

            //Get the label that loops
            if (!cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchLdloc(iteratorIndex),
                    i => i.MatchLdcI4(1),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(iteratorIndex),

                    i => i.MatchLdloc(iteratorIndex),
                    i => i.MatchLdloc(endIndexIndex),
                    i => i.MatchBlt(out ILLabel _)))
            {
                LogILEpicFail("Add gravestone toggle to UI", "Could not locate loop end");
                return;
            }
            cursor.MarkLabel(skipLoopLabel);
        }
        #endregion

        #region Properties about unlock states
        /// <summary>
        /// Checks if the local player has unlocked a custom grave. Only used for UI purposes
        /// </summary>
        public bool UnlockedCustomGraves {
            get {
                foreach (CustomGraveData data in graveData.Values)
                    if (data.graveType != CustomGraveType.Regular && data.IsUnlocked())
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Checks if among the unlocks, any of them are unlocked & a biome grave
        /// </summary>
        /// <param name="unlocks"></param>
        /// <returns></returns>
        public bool UnlockedAnyBiomeGraveOption(Player player, bool[] unlocks)
        {
            for (int i = 0; i < unlocks.Length; i++)
            {
                CustomGraveData data = graveData[(CustomGraveType)i];

                //Not a biome grave
                if (data.biomePickPriority == -1)
                    continue;
                if (data.unlockOverride != null)
                {
                    if (data.unlockOverride(player, unlocks))
                        return true;
                }
                else if (unlocks[i])
                    return true;
            }
            return false;
        }

        public CustomGraveType NextValidGraveType(int direction = 1)
        {
            if (!UnlockedCustomGraves)
                return CustomGraveType.Regular;
            CustomGraveType currentGraveType = selectedGraveType;

            while (true)
            {
                CustomGraveType nextGraveType;

                //Adding nonlinear wrapping if we have normal grave types that have indices past the "adaptive/random" ones which are meant to always be last
                if (GravesPastRandom)
                {
                    nextGraveType = currentGraveType + direction;

                    if (direction < 0)
                    {
                        //Wrap from the first (regular) to random, which is always the last
                        if (currentGraveType == CustomGraveType.Regular)
                            nextGraveType = CustomGraveType.Random;

                        //Moved back from the first grave after random, into random. Skip instead to the last grave before random/adaptive
                        else if (nextGraveType == CustomGraveType.Random)
                            nextGraveType = CustomGraveType.Adaptive - 1;

                        //If we're on adaptive right now and we're moving back, skip to the last grave type past random
                        else if (currentGraveType == CustomGraveType.Adaptive)
                            nextGraveType = (CustomGraveType)(LastGraveTypeIndex);
                    }
                    else
                    {
                        //Wrap from the last (random) to regular, which is always the first
                        if (currentGraveType == CustomGraveType.Random)
                            nextGraveType = CustomGraveType.Regular;

                        //Moved forward from the last grave before adaptive, into adaptive. Skip instead to the first grave after random/adaptive
                        else if (nextGraveType == CustomGraveType.Adaptive)
                            nextGraveType = CustomGraveType.Random + 1;

                        //If we're on the last grave type right now and moving forward, go into adaptive
                        else if ((int)currentGraveType == LastGraveTypeIndex)
                            nextGraveType = CustomGraveType.Adaptive;
                    }
                }
                else
                //basic modulus operation to wrap around
                    nextGraveType = (CustomGraveType)(((int)currentGraveType + direction).ModulusPositive(FABLES_GRAVE_TYPE_COUNT));


                CustomGraveData nextGraveData = graveData[nextGraveType];
                if (nextGraveData.IsUnlocked(this))
                    return nextGraveType;
                currentGraveType = nextGraveType;
            }
        }
        #endregion

        public bool Unlock(CustomGraveType type)
        {
            if (unlockedGraves[(int)type])
                return false;
            if (graveData[type].unlockOverride != null)
                return false;

            graveData[type].Unlock(this);
            unlockedGraves[(int)type] = true;
            selectedGraveType = type;
            return true;
        }

        #region Overriding tombstones
        private void DropCustomTombstones(On_Player.orig_DropTombstone orig, Player self, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            bool rich = coinsOwned > 100000;
            CustomGravesPlayer modPlayer = self.GetModPlayer<CustomGravesPlayer>();

            if (modPlayer.selectedGraveType == CustomGraveType.None)
                return;

            //Crabulon dot takes precedence and spawns mycelial tombstones, unless the player specifically is using no graves
            if (self.HasBuff<CrabulonDOT>())
            {
                SpawnTombstone(self, rich, MyceliumGrave.ProjectileTypes, GoldenMyceliumGrave.ProjectileTypes, deathText, hitDirection);
                return;
            }

            if (modPlayer.selectedGraveType == CustomGraveType.Regular)
                orig(self, coinsOwned, deathText, hitDirection);

            else if (modPlayer.selectedGraveType == CustomGraveType.Adaptive)
            {
                float highestPickPriority = -1;
                CustomGraveData highestPickData = null;
                foreach (CustomGraveData data in graveData.Values)
                {
                    if (data.biomePickPriority > highestPickPriority && data.IsUnlocked(modPlayer) && data.isPlayerInMatchingBiome(self))
                    {
                        highestPickPriority = data.biomePickPriority;
                        highestPickData = data;
                    }
                }

                if (highestPickData == null)
                    orig(self, coinsOwned, deathText, hitDirection);
                else
                    SpawnTombstone(self, rich, highestPickData.gravesPool, highestPickData.richGravesPool, deathText, hitDirection);
            }

            else if (modPlayer.selectedGraveType == CustomGraveType.Random)
            {
                List<int> unlockedGravePool = new List<int>() { ProjectileID.Tombstone, ProjectileID.GraveMarker, ProjectileID.Gravestone, ProjectileID.CrossGraveMarker, ProjectileID.Obelisk };
                List<int> unlockedRichGravePool = new List<int>() { ProjectileID.RichGravestone1, ProjectileID.RichGravestone2, ProjectileID.RichGravestone3, ProjectileID.RichGravestone4, ProjectileID.RichGravestone5 };

                foreach (CustomGraveData data in graveData.Values)
                {
                    if (data.biomePickPriority > -1 && data.IsUnlocked(modPlayer))
                    {
                        unlockedGravePool.AddRange(data.gravesPool);
                        unlockedRichGravePool.AddRange(data.richGravesPool);
                    }
                }

                SpawnTombstone(self, rich, unlockedRichGravePool, unlockedRichGravePool, deathText, hitDirection);
            }

            else if (modPlayer.SelectedGrave.gravesPool != null)
                SpawnTombstone(self, rich, modPlayer.SelectedGrave.gravesPool, modPlayer.SelectedGrave.richGravesPool, deathText, hitDirection);
        }

        private void SpawnTombstone(Player player, bool rich, List<int> poorGravesPool, List<int> richGravesPool, NetworkText deathText, int hitDirection)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float graveHorizontalSpeed = Math.Min(Main.rand.NextFloat(3.5f), 2f);
            if (Main.rand.NextBool())
                graveHorizontalSpeed *= -1;

            int graveType = Main.rand.Next(poorGravesPool);
            if (rich)
                graveType = Main.rand.Next(richGravesPool);

            //Copied from the base method
            IEntitySource source = player.GetSource_Misc("PlayerDeath_TombStone");
            Vector2 graveVelocity = new Vector2(Main.rand.NextFloat(1f, 3f) * hitDirection + graveHorizontalSpeed, Main.rand.NextFloat(-4f, -2f));
            int projectileSpawned = Projectile.NewProjectile(source, player.Center, graveVelocity, graveType, 0, 0f, Main.myPlayer);

            //Set the graves text
            DateTime now = DateTime.Now;
            string str = now.ToString("D");
            if (GameCulture.FromCultureName(GameCulture.CultureName.English).IsActive)
                str = now.ToString("MMMM d, yyy");
            string miscText = deathText.ToString() + "\n" + str;
            Main.projectile[projectileSpawned].miscText = miscText;

        }
        #endregion

        #region drawing
        private bool DrawTombstoneStyleIcon(int toggleType, Vector2 position)
        {
            if (toggleType != DummyToggleType)
                return false;

            CustomGravesPlayer modPlayer = Main.LocalPlayer.GetModPlayer<CustomGravesPlayer>();
            CustomGraveData selectedGraveType = graveData[modPlayer.selectedGraveType];

            Texture2D tex = modGraveIconSheets[selectedGraveType.mod].Value;
            Rectangle frame = tex.BetterFrame(gravesPerMod[selectedGraveType.mod], 2, selectedGraveType.perModIndex);
            float iconScale = 0.9f;

            //Position is centered fsr
            position -= frame.Size() / 2f;
            position += Vector2.One * 2;

            bool hovered = false;
            if (Main.mouseX > position.X && (float)Main.mouseX < (float)position.X + (float)frame.Width * iconScale && Main.mouseY > position.Y && (float)Main.mouseY < (float)position.Y + (float)frame.Height * iconScale)
            {
                hovered = true;
                Main.LocalPlayer.mouseInterface = true;

                //Hover text
                Main.instance.MouseText(modPlayer.SelectedGrave.name, 0, 0);
                Main.mouseText = true;

                //On click, flip between modes
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    modPlayer.selectedGraveType = modPlayer.NextValidGraveType();
                    modPlayer.SyncSelectedGraveType();
                }

                //On right click, go back
                else if (Main.mouseRight && Main.mouseRightRelease)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    modPlayer.selectedGraveType = modPlayer.NextValidGraveType(-1);
                    modPlayer.SyncSelectedGraveType();
                }
            }

            //Draw the icons
            Main.spriteBatch.Draw(tex, position, frame, Color.White, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);
            if (hovered)
                Main.spriteBatch.Draw(tex, position, tex.BetterFrame(gravesPerMod[selectedGraveType.mod], 2, selectedGraveType.perModIndex, 1), Main.OurFavoriteColor, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);

            //Fuck gamepad i think. Nique ta mère
            //UILinkPointNavigator.SetPosition(6000 + gamepadPointOffset, vector + rectangle.Size() * 0.65f);
            return true;
        }
        #endregion

        #region Syncing and loading
        public override void OnEnterWorld()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;
            NetEasy.Module packet = new SyncAllOfMyGravesPacket(this);
            packet.Send(-1, -1, false);
        }

        public void SyncSelectedGraveType()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;
            SyncSelectedGraveTypePacket packet = new SyncSelectedGraveTypePacket(this);
            packet.Send(-1, Player.whoAmI, false);
        }

        public override void SaveData(TagCompound tag)
        {
            CustomGraveData selectedGraveData = graveData[selectedGraveType];
            tag["selectedGrave"] = selectedGraveData.perModIndex;
            tag["selectedGraveMod"] = selectedGraveData.mod.Name;

            for (int i = 0; i < unlockedGraves.Length; i++)
            {
                CustomGraveData data = graveData[(CustomGraveType)i];
                //Dont save graves with custom unlock overrides
                if (data.unlockOverride != null)
                    continue;

                if (data.mod == CalamityFables.Instance)
                    tag["unlocked" + data.graveType.ToString() + "Grave"] = unlockedGraves[i];
                else
                    tag["unlocked" + data.mod.Name + data.savekey + "CrossmodGrave"] = unlockedGraves[i];
            }
        }

        public override void LoadData(TagCompound tag)
        {
            bool selectedGraveLoaded = true;

            if (!tag.TryGet("selectedGraveMod", out string selectedGraveModName))
                selectedGraveModName = "CalamityFables";
            else if (!ModLoader.HasMod(selectedGraveModName))
                selectedGraveLoaded = false;

            //Reset to a default selection
            selectedGraveType = CustomGraveType.Regular;

            //if the player's selection is from an actually loaded mod
            if (selectedGraveLoaded && tag.TryGet("selectedGrave", out int selectedGraveIndex))
            {
                //Since the fables graves are always the first ones in the list, its easy to find their type just by getting the index
                if (selectedGraveModName == "CalamityFables")
                    selectedGraveType = (CustomGraveType)selectedGraveIndex;
                else
                {
                    int graveTallyForMyMod = 0;

                    for (int i = FABLES_GRAVE_TYPE_COUNT; i < graveData.Count; i++)
                    {
                        CustomGraveData data = graveData[(CustomGraveType)i];
                        if (data.mod.Name == selectedGraveModName)
                        {
                            if (selectedGraveIndex == graveTallyForMyMod)
                            {
                                selectedGraveType = (CustomGraveType)i;
                                break;
                            }
                            graveTallyForMyMod++;
                        }
                    }
                }
            }


            for (int i = 0; i < unlockedGraves.Length; i++)
            {
                CustomGraveData data = graveData[(CustomGraveType)i];
                unlockedGraves[i] = false;

                //Dont bother checking for grave data with custom unlocks
                if (data.unlockOverride != null)
                    continue;

                //Load fables graves (easy)
                if (data.mod == CalamityFables.Instance)
                {
                    if (tag.TryGet("unlocked" + data.graveType.ToString() + "Grave", out bool graveUnlockStatus))
                        unlockedGraves[i] = graveUnlockStatus;
                }
                //Load graves from other mods (check with custom savekey & modname)
                else
                {
                    if (tag.TryGet("unlocked" + data.mod.Name + data.savekey + "CrossmodGrave", out bool graveUnlockStatus))
                        unlockedGraves[i] = graveUnlockStatus;
                }
            }
        }
        #endregion

        #region Crossmod calls
        public static int GetGravesetIndex(string name)
        {
            if (name == "desert")
                return (int)CustomGraveType.Sandstone;
            if (name == "ocean")
                return (int)CustomGraveType.Beach;
            if (name == "snow" || name == "taiga")
                return (int)CustomGraveType.Ice;
            if (name == "skyisland" || name == "skyislands" || name == "space")
                return (int)CustomGraveType.Sky;

            for (int i = 0; i < FABLES_GRAVE_TYPE_COUNT; i++)
            {
                string fablesGraveName = ((CustomGraveType)i).ToString().ToLower();
                if (fablesGraveName == name)
                    return i;
            }

            return -1;
        }


        public static void RegisterUITexture(Mod sourceMod, string texturePath)
        {
            if (Main.dedServ)
                return;
            modGraveIconSheets[sourceMod] = Request<Texture2D>(texturePath);
        }

        public static List<int> AutoloadModdedGraveSetSingle(Mod sourceMod, string texturePath, string internalName, bool gilded, string[] graveVariantName, Color mapColor, int dust)
        {
            if (graveVariantName.Length != 4)
                throw new ArgumentException("Grave variant names need to come in 4s");

            AutoloadedCrossmodGrave grave = new AutoloadedCrossmodGrave(internalName, texturePath, gilded, dust, mapColor, graveVariantName);
            sourceMod.AddContent(grave);
            return grave.ProjectilePool;
        }

        /*
        public static bool AutoloadModdedGraveSet(Mod sourceMod, string texturePath, string graveInternalName, string graveGildedInternalName, 
            Color normalGraveMapColor, Color gildedGraveMapColor, int normalDust, int gildedDust,
            LocalizedText graveTypeName, List<string> graveVariantNames, Func<Player, bool> inBiomeCheck, float biomePriority, Func<Player, bool[], bool> unlockOverride = null)
        {
            if (graveVariantNames.Count != 8)
                return false;

            string[] normalVariantNames = new string[4];
            for (int i = 0; i < 4; i++)
                normalVariantNames[i] = graveVariantNames[i];

            string[] gildedVariantNames = new string[4];
            for (int i = 0; i < 4; i++)
                gildedVariantNames[i] = graveVariantNames[i + 4];

            AutoloadedCrossmodGrave graveNormal = new AutoloadedCrossmodGrave(graveInternalName, texturePath, false, normalDust, normalGraveMapColor, normalVariantNames);
            sourceMod.AddContent(graveNormal);
            AutoloadedCrossmodGrave graveGilded = new AutoloadedCrossmodGrave(graveGildedInternalName, texturePath, true , gildedDust, gildedGraveMapColor, gildedVariantNames);
            sourceMod.AddContent(graveGilded);

            LoadModdedGraveType(sourceMod, graveInternalName, graveTypeName, graveNormal.ProjectilePool, graveGilded.ProjectilePool, inBiomeCheck, biomePriority, unlockOverride);
            return true;
        }
        */

        public static int LoadModdedGraveType(Mod sourceMod, string graveSavekey, int gravesetIconIndex, LocalizedText graveTypeName, List<int> graveProjectileTypes, List<int> goldenGraveProjectileTypes, Func<Player, bool> inBiomeCheck, float biomePriority = 1f, Func<Player, bool[], bool> unlockOverride = null)
        {
            //If we're loading these before the fables graves have been loaded; save them into a separate dictionnary so we maintain order
            Dictionary<CustomGraveType, CustomGraveData> dictToLoadInto = graveData.Count == 0 ? queuedModdedGraveData : graveData;

            //If we add modded graves that means there'll be graves with indices past random, so we have to account for these
            if (!GravesPastRandom)
                GravesPastRandom = true;

            if (!gravesPerMod.ContainsKey(sourceMod))
                gravesPerMod[sourceMod] = 0;
            int graveTypeIndex = LastGraveTypeIndex + 1;

            CustomGraveData moddedGraveData = new CustomGraveData((CustomGraveType)graveTypeIndex, graveTypeName,
                graveProjectileTypes, goldenGraveProjectileTypes, inBiomeCheck, biomePriority, sourceMod, unlockOverride);
            moddedGraveData.savekey = graveSavekey;

            moddedGraveData.perModIndex = gravesetIconIndex;

            dictToLoadInto.Add((CustomGraveType)graveTypeIndex, moddedGraveData);
            ModdedGraveTypeCount++;
            return LastGraveTypeIndex;
        }
        #endregion

        public class CustomGraveData
        {
            public readonly CustomGraveType graveType;
            public readonly LocalizedText label;
            public string name => label.Value;
            public readonly Func<Player, bool> isPlayerInMatchingBiome = null;
            public readonly Func<Player, bool[], bool> unlockOverride = null;
            public readonly float biomePickPriority = -1;

            public readonly List<int> gravesPool;
            public readonly List<int> richGravesPool;

            internal Mod mod = null;
            internal int perModIndex;
            internal string savekey = "";

            public bool IsUnlocked(int player = -1) => IsUnlocked(Main.player[player == -1 ? Main.myPlayer : player].GetModPlayer<CustomGravesPlayer>());
            public bool IsUnlocked(CustomGravesPlayer player)
            {
                bool[] unlockList = player.unlockedGraves;
                if (unlockOverride != null)
                    return unlockOverride(player.Player, unlockList);
                return unlockList[(int)graveType];
            }

            public void Unlock(int player = -1) => Unlock(Main.player[player == -1 ? Main.myPlayer : player].GetModPlayer<CustomGravesPlayer>());
            public void Unlock(CustomGravesPlayer player)
            {
                bool[] unlockList = player.unlockedGraves;
                if (unlockOverride != null)
                    return;
                unlockList[(int)graveType] = true;
                if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != player.Player.whoAmI)
                    return;
                SyncUnlockedGravesPacket packet = new SyncUnlockedGravesPacket((byte)Main.myPlayer, graveType, true);
                packet.Send(-1, (byte)Main.myPlayer, false);
            }

            public CustomGraveData(CustomGraveType graveType, LocalizedText name, Func<Player, bool[], bool> unlockOverride = null, Mod mod = null)
            {
                this.graveType = graveType;
                this.label = name;
                this.unlockOverride = unlockOverride;
                this.mod = mod ?? CalamityFables.Instance;
                perModIndex = gravesPerMod[this.mod];
                gravesPerMod[this.mod]++;
            }

            public CustomGraveData(CustomGraveType graveType, string name,  Func<Player, bool[], bool> unlockOverride = null, Mod mod = null)
            {
                this.graveType = graveType;
                this.label = CalamityFables.Instance.GetLocalization("Extras.BiomeGravestones." + name);
                this.unlockOverride = unlockOverride;
                this.mod = mod ?? CalamityFables.Instance;
                perModIndex = gravesPerMod[this.mod];
                gravesPerMod[this.mod]++;
            }

            public CustomGraveData(CustomGraveType graveType, string name, List<int> graves, List<int> richGraves, Func<Player, bool> inBiome, float biomePriority, Mod mod = null, Func<Player, bool[], bool> unlockOverride = null)
            {
                this.graveType = graveType;
                this.label = CalamityFables.Instance.GetLocalization("Extras.BiomeGravestones." + name);
                this.mod = mod ?? CalamityFables.Instance;
                this.unlockOverride = unlockOverride;

                gravesPool = graves;
                richGravesPool = richGraves;
                isPlayerInMatchingBiome = inBiome;
                biomePickPriority = biomePriority;

                perModIndex = gravesPerMod[this.mod];
                gravesPerMod[this.mod]++;
            }

            public CustomGraveData(CustomGraveType graveType, LocalizedText name, List<int> graves, List<int> richGraves, Func<Player, bool> inBiome, float biomePriority, Mod mod = null, Func<Player, bool[], bool> unlockOverride = null)
            {
                this.graveType = graveType;
                this.label = name;
                this.mod = mod ?? CalamityFables.Instance;
                this.unlockOverride = unlockOverride;

                gravesPool = graves;
                richGravesPool = richGraves;
                isPlayerInMatchingBiome = inBiome;
                biomePickPriority = biomePriority;

                perModIndex = gravesPerMod[this.mod];
                gravesPerMod[this.mod]++;
            }
        }
    }

    public class DummyGraveToggle : BuilderToggle
    {
        public override bool Active() => Main.LocalPlayer.GetModPlayer<CustomGravesPlayer>().UnlockedCustomGraves;
        public override string DisplayValue() => "";
        public override string Texture => AssetDirectory.Invisible;

        protected override void Register()
        {
            base.Register();

            if (CustomGravesPlayer.BuilderToggles == null)
                return;

            //Remove itself from the end of the list, and insert itself earlier
            CustomGravesPlayer.BuilderToggles.RemoveAt(Type);
            CustomGravesPlayer.BuilderToggles.Insert(2, this);
            CustomGravesPlayer.DummyToggleType = Type;
        }


        //Remove our thing before tml auto removes modded ones, because it only remvoes from the end of the list
        public override void Unload()
        {
            if (CustomGravesPlayer.BuilderToggles != null)
                CustomGravesPlayer.BuilderToggles.RemoveAt(2);
        }

    }

    [Serializable]
    public class SyncUnlockedGravesPacket : NetEasy.Module
    {
        public readonly byte whoAmI;
        public byte graveType;
        public bool unlockState;

        public SyncUnlockedGravesPacket(byte playerIndex, CustomGravesPlayer.CustomGraveType type, bool unlockState)
        {
            whoAmI = playerIndex;
            graveType = (byte)type;
            this.unlockState = unlockState;
        }

        protected override void Receive()
        {
            CustomGravesPlayer mp = Main.player[whoAmI].GetModPlayer<CustomGravesPlayer>();
            mp.unlockedGraves[graveType] = unlockState;
        }
    }

    [Serializable]
    public class SyncSelectedGraveTypePacket : NetEasy.Module
    {
        public readonly byte whoAmI;
        public int graveType;

        public SyncSelectedGraveTypePacket(CustomGravesPlayer player)
        {
            whoAmI = (byte)player.Player.whoAmI;
            graveType = (int)player.selectedGraveType;
        }

        protected override void Receive()
        {
            CustomGravesPlayer mp = Main.player[whoAmI].GetModPlayer<CustomGravesPlayer>();
            mp.selectedGraveType = (CustomGraveType)graveType;
        }
    }

    [Serializable]
    public class SyncAllOfMyGravesPacket : NetEasy.Module
    {
        public readonly byte whoAmI;
        public bool[] unlockStates;
        public int selectedGrave;

        public SyncAllOfMyGravesPacket(CustomGravesPlayer player)
        {
            whoAmI = (byte)player.Player.whoAmI;
            unlockStates = player.unlockedGraves;
            selectedGrave = (int)player.selectedGraveType;
        }

        protected override void Receive()
        {
            CustomGravesPlayer mp = Main.player[whoAmI].GetModPlayer<CustomGravesPlayer>();
            CalamityFables.Instance.Logger.Debug("Loading unlock states from player graves");
            CalamityFables.Instance.Logger.Debug(unlockStates.ToString());

            mp.selectedGraveType = (CustomGraveType)selectedGrave;
            for (int i = 0; i < unlockStates.Length; i++)
            {
                mp.unlockedGraves[i] = unlockStates[i];
            }
        }
    }
}
