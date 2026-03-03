using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;
using static Terraria.ID.NPCID.Sets;

namespace CalamityFables.Core
{
    public class ParasiteCoreSystem : ModSystem
    {
        private static Type loreItemType;

        public static Dictionary<int, int> Items_CalToFables;
        public static List<int> ReplacedCalItems;
        public static List<int> RemovedCalItems;

        public static Dictionary<int, int> NPCs_CalToFables;
        public static List<int> ReplacedCalNPCs;
        public static List<int> ReplacedCalTownNPCs;
        public static List<int> ReplacedCalBosses;

        //Shoutouts to Gabe
        private static MethodInfo CalamityAddBossMethod;
        public delegate void orig_CalamityAddBossMethod(Mod bossChecklist, Mod hostMod, string name, float difficulty, int npcType, Func<bool> downed, object summon,
            List<int> collection, string instructions, string despawn, Func<bool> available, Action<SpriteBatch, Rectangle, Color> portrait = null, string bossHeadTex = null);
        public delegate void hook_CalamityAddBossMethod(orig_CalamityAddBossMethod orig, Mod bossChecklist, Mod hostMod, string name, float difficulty, int npcType, Func<bool> downed, object summon,
            List<int> collection, string instructions, string despawn, Func<bool> available, Action<SpriteBatch, Rectangle, Color> portrait = null, string bossHeadTex = null);

        private static MethodInfo CalamityAddBossesMethod;
        public delegate void orig_CalamityAddBossesMethod(Mod bossChecklist, Mod hostMod, string name, float difficulty, List<int> npcTypes, Func<bool> downed, object summon,
            List<int> collection, string instructions, string despawn, Func<bool> available, Action<SpriteBatch, Rectangle, Color> portrait = null, string bossHeadTex = null);
        public delegate void hook_CalamityAddBossesMethod(orig_CalamityAddBossesMethod orig, Mod bossChecklist, Mod hostMod, string name, float difficulty, List<int> npcTypes, Func<bool> downed, object summon,
            List<int> collection, string instructions, string despawn, Func<bool> available, Action<SpriteBatch, Rectangle, Color> portrait = null, string bossHeadTex = null);

        public static PropertyInfo CalamityDesertScourgeDownedProperty;
        public static PropertyInfo CalamityCrabulonDownedProperty;

        #region Loading
        public override void OnModLoad()
        {
            Items_CalToFables = new Dictionary<int, int>();
            ReplacedCalItems = new List<int>();
            RemovedCalItems = new List<int>();

            NPCs_CalToFables = new Dictionary<int, int>();
            ReplacedCalNPCs = new List<int>();
            ReplacedCalTownNPCs = new List<int>();
            ReplacedCalBosses = new List<int>();

            if (!CalamityFables.CalamityEnabled)
                return;

            GetDownedBossProperties();

            if (!CalCompatConfig.Instance.ReplaceCalamity)
                return;

            //Hooks
            On_Main.UpdateTime_SpawnTownNPCs += BlockReplacedTownNPCs; //Prevents replaced/removed town NPCs from spawning
            On_BestiaryDatabaseNPCsPopulator.Populate += RemoveEntriesFromReplacedNPCs; //Removes the bestiary entries from replaced NPCs
            On_NPC.NewNPC += PreventSpawningTownNPCs; //REALLY prevent replaced town NPCs from spawning
            Terraria.GameContent.ItemDropRules.On_CommonCode.ModifyItemDropFromNPC += ForcefullyReplaceCalDrops; //Caca code for caca terraria loot code
            LoadBossChecklistRemoval(); //Prevent replaced cal bosses from being registered in boss checklist.

            FablesWorld.ModifyChestContentsEvent += ReplaceItemsInChests;

            // Store the lore item type. It will be used in a universal mod-agnostic item check to determine if a given item is a lore item and thusly should be canned.
            loreItemType = CalamityFables.Instance.CalamityTheOriginal.Code.GetType("CalamityMod.Items.LoreItems.LoreItem");
        }

        private void GetDownedBossProperties()
        {
            Type dbs = CalamityFables.Instance.CalamityTheOriginal.Code.GetType("CalamityMod.DownedBossSystem");
            if (dbs is not null)
            {
                CalamityDesertScourgeDownedProperty = dbs.GetProperty("downedDesertScourge", BindingFlags.Public | BindingFlags.Static);
                CalamityCrabulonDownedProperty = dbs.GetProperty("downedCrabulon", BindingFlags.Public | BindingFlags.Static);
            }
        }

        private bool ReplaceItemsInChests(Chest chest, Tile chestTile, bool alreadyAddedItem)
        {
            for (int i = 0; i < Chest.maxItems; i++)
            {
                if (chest.item[i].IsAir)
                    continue;

                if (Items_CalToFables.TryGetValue(chest.item[i].type, out int fablesEquivalent))
                {
                    Item item = new Item(fablesEquivalent);
                    item.Prefix(ItemLoader.ChoosePrefix(item, Main.rand));
                    chest.item[i] = item;
                }
                else if (RemovedCalItems.Contains(chest.item[i].type))
                {
                    chest.item[i].TurnToAir();
                }
            }

            return false;
        }

        public static Hook BlockReplacedBossesFromChecklistHook;
        public static Hook BlockReplacedBossesFromChecklistHook_Segmented;

        public void LoadBossChecklistRemoval()
        {
            Type wrs = CalamityFables.Instance.CalamityTheOriginal.Code.GetType("CalamityMod.WeakReferenceSupport");
            if (wrs is null)
                return;

            Type[] methodArguments = new Type[]
                {
                    typeof(Mod), typeof(Mod),
                    typeof(string), typeof(float), typeof(int), typeof(Func<bool>), typeof(object), typeof(List<int>),
                    typeof(string), typeof(string), typeof(Func<bool>), typeof(Action<SpriteBatch, Rectangle, Color>), typeof(string)
                };

            CalamityAddBossMethod = wrs.GetMethod("AddBoss", BindingFlags.NonPublic | BindingFlags.Static, methodArguments);
            if (CalamityAddBossMethod is not null)
                BlockReplacedBossesFromChecklistHook = new(CalamityAddBossMethod, (hook_CalamityAddBossMethod)PreventBossChecklistFromRegisteringReplacedBosses);

            methodArguments[4] = typeof(List<int>);
            CalamityAddBossesMethod = wrs.GetMethod("AddBoss", BindingFlags.NonPublic | BindingFlags.Static, methodArguments);
            if (CalamityAddBossesMethod is not null)
                BlockReplacedBossesFromChecklistHook_Segmented = new(CalamityAddBossesMethod, (hook_CalamityAddBossesMethod)PreventBossChecklistFromRegisteringReplacedBosses_Segmented);

        }

        public override void Unload()
        {
            BlockReplacedBossesFromChecklistHook?.Undo();
            BlockReplacedBossesFromChecklistHook_Segmented?.Undo();
        }

        public static int CalItem(string item)
        {
            bool foundModItem = CalamityFables.Instance.CalamityTheOriginal.TryFind<ModItem>(item, out ModItem modItem);
            if (!foundModItem)
                return -1;

            return modItem.Type;
        }
        public static int CalNPC(string npc, out bool boss, out bool townNPC)
        {
            bool foundModNPC = CalamityFables.Instance.CalamityTheOriginal.TryFind<ModNPC>(npc, out ModNPC modNPC);
            if (!foundModNPC)
            {
                boss = false;
                townNPC = false;
                return -1;
            }

            boss = modNPC.NPC.boss;
            townNPC = modNPC.NPC.townNPC;

            return modNPC.Type;
        }

        public override void SetStaticDefaults()
        {
            if (!CalamityFables.CalamityEnabled)
                return;

            #region Hardcoding
            if (CalCompatConfig.Instance.ReplaceCalamity)
            {
                // Remove Cirrus from the game.
                // HardRemoveNPC("FAP"); <- One day...

                // Remove any and all lore items from the game.
                // Okay we probably don't need to do that actually tbh
                /*
                if (CalCompatConfig.Instance.RemoveCalamityLoreFluff && loreItemType is not null)
                {
                    for (int i = ItemID.Count; i < ItemLoader.ItemCount; i++)
                    {
                        ModItem modItem = ItemLoader.GetItem(i);
                        if (modItem is null || !modItem.GetType().IsSubclassOf(loreItemType))
                            continue;

                        HardRemoveItem(modItem.Name);
                    }
                }
                */
            }
            #endregion

            #region Items
            IEnumerable<ModItem> allFablesItems = Mod.GetContent<ModItem>();
            foreach (ModItem item in allFablesItems)
            {
                ReplacingCalamityAttribute attribute = item.GetType().GetCustomAttribute<ReplacingCalamityAttribute>();
                if (attribute != null)
                {
                    foreach (string calamityName in attribute.calamityVersions)
                    {
                        int calItem = CalItem(calamityName);
                        if (calItem == -1)
                            continue;

                        Items_CalToFables[calItem] = item.Type;
                        ReplacedCalItems.Add(calItem);
                    }
                }
            }

            //If we don't replace anything in calamity we dont need anything more. We just wanted to log the duplicate items.
            if (!CalCompatConfig.Instance.ReplaceCalamity)
                return;

            //List all the items that have been "replaced" with no replacemenets.
            RemovedCalItems = ReplacedCalItems.Where(i => Items_CalToFables.Keys.All(i2 => i2 != i)).ToList();
            #endregion

            #region NPCs
            IEnumerable<ModNPC> allFablesNPCs = Mod.GetContent<ModNPC>();
            foreach (ModNPC npc in allFablesNPCs)
            {
                ReplacingCalamityAttribute attribute = npc.GetType().GetCustomAttribute<ReplacingCalamityAttribute>();
                if (attribute != null)
                {
                    foreach (string calamityName in attribute.calamityVersions)
                    {
                        int calNPC = CalNPC(calamityName, out bool boss, out bool town);
                        if (calNPC == -1)
                            continue;

                        NPCs_CalToFables[calNPC] = npc.Type;
                        ReplacedCalNPCs.Add(calNPC);

                        if (town)
                            ReplacedCalTownNPCs.Add(calNPC);

                        if (boss)
                            ReplacedCalBosses.Add(calNPC);
                    }
                }
            }
            #endregion
        }

        #region Hard removal utils
        public static void HardRemoveNPC(string npc)
        {
            int calNPC = CalNPC(npc, out bool boss, out bool town);
            if (calNPC == -1)
                return;

            ReplacedCalNPCs.Add(calNPC);

            if (town)
                ReplacedCalTownNPCs.Add(calNPC);

            if (boss)
                ReplacedCalBosses.Add(calNPC);
        }

        public static void HardRemoveItem(string item)
        {
            int calItem = CalItem(item);
            if (calItem == -1)
                return;
            ReplacedCalItems.Add(calItem);
        }
        #endregion
        #endregion

        #region Town NPCs
        private void BlockReplacedTownNPCs(Terraria.On_Main.orig_UpdateTime_SpawnTownNPCs orig)
        {
            orig();

            if (!CalamityFables.CalamityEnabled)
                return;

            //Prevent blocked town npcs for spawning
            for (int i = 0; i < ReplacedCalTownNPCs.Count; i++)
                if (Main.townNPCCanSpawn[ReplacedCalTownNPCs[i]])
                {
                    Main.townNPCCanSpawn[ReplacedCalTownNPCs[i]] = false;


                    if (WorldGen.prioritizedTownNPCType == ReplacedCalTownNPCs[i])
                        WorldGen.prioritizedTownNPCType = 0;
                }
        }

        private int PreventSpawningTownNPCs(Terraria.On_NPC.orig_NewNPC orig, Terraria.DataStructures.IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        {
            if (CalamityFables.CalamityEnabled && ReplacedCalTownNPCs.Contains(Type))
                return Main.maxNPCs;

            return orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
        }
        #endregion

        #region NPCs
        private void RemoveEntriesFromReplacedNPCs(Terraria.GameContent.Bestiary.On_BestiaryDatabaseNPCsPopulator.orig_Populate orig, BestiaryDatabaseNPCsPopulator self, BestiaryDatabase database)
        {
            if (CalamityFables.CalamityEnabled)
            {
                var hideNPC = new NPCBestiaryDrawModifiers(0) { Hide = true };

                for (int i = 0; i < ReplacedCalNPCs.Count; i++)
                {
                    if (NPCBestiaryDrawOffset.ContainsKey(ReplacedCalNPCs[i]))
                        NPCBestiaryDrawOffset[ReplacedCalNPCs[i]] = hideNPC;

                    else
                        NPCBestiaryDrawOffset.Add(ReplacedCalNPCs[i], hideNPC);
                }
            }
            orig(self, database);
        }

        public void PreventBossChecklistFromRegisteringReplacedBosses(orig_CalamityAddBossMethod orig, Mod bossChecklist, Mod hostMod, string name, float difficulty, int npcType, Func<bool> downed, object summon,
            List<int> collection, string instructions, string despawn, Func<bool> available, Action<SpriteBatch, Rectangle, Color> portrait = null, string bossHeadTex = null)
        {
            if (!ReplacedCalBosses.Contains(npcType))
                orig(bossChecklist, hostMod, name, difficulty, npcType, downed, summon, collection, instructions, despawn, available, portrait, bossHeadTex);
        }

        public void PreventBossChecklistFromRegisteringReplacedBosses_Segmented(orig_CalamityAddBossesMethod orig, Mod bossChecklist, Mod hostMod, string name, float difficulty, List<int> npcTypes, Func<bool> downed, object summon,
            List<int> collection, string instructions, string despawn, Func<bool> available, Action<SpriteBatch, Rectangle, Color> portrait = null, string bossHeadTex = null)
        {
            if (!ReplacedCalBosses.Intersect(npcTypes).Any())
                orig(bossChecklist, hostMod, name, difficulty, npcTypes, downed, summon, collection, instructions, despawn, available, portrait, bossHeadTex);
        }
        #endregion

        #region Items
        public override void PostAddRecipes()
        {
            bool strongFablification = CalCompatConfig.Instance.ReplaceCalamity;

            List<Recipe> fableRecipes = new List<Recipe>();

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];

                if (strongFablification)
                {
                    //Make replaced items uncraftable (obviously)
                    if (ReplacedCalItems.Contains(recipe.createItem.type))
                    {
                        recipe.DisableRecipe();
                        continue;
                    }

                    //Else, be sure the ingredients of the recipe use the fables variants
                    FablesifyRecipe(recipe);
                }

                else
                {
                    //Create a duplicate recipe using the fables materials (if the recipe got altered)
                    Recipe fablesRecipe = recipe.Clone();
                    if (FablesifyRecipe(fablesRecipe))
                        fableRecipes.Add(fablesRecipe);
                }
            }

            foreach (Recipe recipe in fableRecipes)
                recipe.Register();
        }

        public bool FablesifyRecipe(Recipe recipe)
        {
            bool wasAltered = false;

            for (int i = recipe.requiredItem.Count - 1; i >= 0; i--)
            {
                int itemType = recipe.requiredItem[i].type;
                int stack = recipe.requiredItem[i].stack;

                //If the ingredient has a fables equivalent, swap the ingredient out for the fables one
                if (Items_CalToFables.TryGetValue(itemType, out int fablesEquivalent))
                {
                    recipe.RemoveIngredient(recipe.requiredItem[i]);

                    //Lower max stack to the cap of the replacement item's stack 
                    //This is for recipes which use multiple of a consumable item that fables turned into a non consumable item
                    ModItem fablesItem = ItemLoader.GetItem(fablesEquivalent);
                    if (fablesItem.Item.maxStack < stack)
                        stack = fablesItem.Item.maxStack;

                    recipe.AddIngredient(fablesEquivalent, stack);
                    wasAltered = true;
                }

                //If the ingredient was removed by fables, but not replaced, remove the ingredient altogether
                else if (RemovedCalItems.Contains(itemType))
                {
                    recipe.RemoveIngredient(recipe.requiredItem[i]);
                    wasAltered = true;

                    //If the recipe was somehow only crafted with removed cal items, disable the recipe altogether
                    if (recipe.requiredItem.Count == 0)
                    {
                        recipe.DisableRecipe();
                        return false;
                    }
                }
            }

            return wasAltered;
        }



        //Hopefully shouldn't have to happen but the dogshit drop code from 1.4 forces us to do it
        private void ForcefullyReplaceCalDrops(Terraria.GameContent.ItemDropRules.On_CommonCode.orig_ModifyItemDropFromNPC orig, NPC npc, int itemIndex)
        {
            orig(npc, itemIndex);

            Item item = Main.item[itemIndex];
            if (Items_CalToFables.TryGetValue(item.type, out int fablesType))
            {
                //Reset the item to its fables variant
                int prefix = item.prefix;
                item.SetDefaults(fablesType);
                item.Prefix(prefix);
            }

            else if (RemovedCalItems.Contains(item.type))
            {
                //Prevent the item from spawning altogether
                item.type = ItemID.None;
                item.active = false;
            }
        }

        public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            if ((msgType == MessageID.SyncItem || msgType == MessageID.InstancedItem) && Main.item[number].active)
            {
                int itemType = Main.item[number].type;

                if (Items_CalToFables.TryGetValue(itemType, out int fablesType))
                {
                    int prefix = Main.item[number].prefix;
                    Main.item[number].SetDefaults(fablesType);
                    Main.item[number].Prefix(prefix);
                }

                else if (RemovedCalItems.Contains(itemType))
                    return false;
            }

            return base.HijackSendData(whoAmI, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }
        #endregion
    }

    public class ParasitedItem : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (!CalamityFables.CalamityEnabled || !CalCompatConfig.Instance.ReplaceCalamity)
                return;

            itemLoot.FablesifyDropPool();
        }
    }


    public class ParasitedNPC : GlobalNPC
    {
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (!CalamityFables.CalamityEnabled || !CalCompatConfig.Instance.ReplaceCalamity)
                return;

            foreach (KeyValuePair<int, float> entry in pool)
            {
                if (ParasiteCoreSystem.ReplacedCalNPCs.Contains(entry.Key))
                {
                    pool[entry.Key] = 0f;
                }
            }
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (!CalamityFables.CalamityEnabled || !CalCompatConfig.Instance.ReplaceCalamity)
                return;

            npcLoot.FablesifyDropPool();
        }
    }
}
