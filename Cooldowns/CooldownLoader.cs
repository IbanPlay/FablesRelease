using System.Reflection;
using Terraria.ModLoader.Core;

namespace CalamityFables.Cooldowns
{
    public class CooldownLoader : ModSystem
    {
        // Indexed by ushort netID. Contains every registered cooldown.
        // Cooldowns are given netIDs when they are registered.
        // Cooldowns are useless until they are registered.
        public static Cooldown[] registry;
        private const ushort defaultSize = 256;
        private static ushort nextCDNetID = 0;

        private static Dictionary<string, ushort> nameToNetID = null;

        public override void Load()
        {
            registry = new Cooldown[defaultSize];
            nameToNetID = new Dictionary<string, ushort>(defaultSize);

            // TODO -- CooldownHandlers should be ILoadable in 1.4
            RegisterModCooldowns(CalamityFables.Instance);
        }

        public static void RegisterModCooldowns(Mod mod)
        {
            Type baseHandlerType = typeof(CooldownHandler);
            foreach (Type type in AssemblyManager.GetLoadableTypes(mod.Code))
            {
                if (type.IsSubclassOf(baseHandlerType) && !type.IsAbstract && type != baseHandlerType)
                {
                    //Get the static property ID of the handler
                    string handlerID = (string)type.GetProperty("ID").GetValue(null);

                    //If for whatever reason the ID is not set, create an ID from the mod and handler name
                    if (handlerID == null)
                        handlerID = mod.Name + "_" + type.Name;

                    //Use reflection to call the method. You can't use the type as the generic type argument of register here
                    MethodInfo methodInfo = typeof(CooldownLoader).GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                    Type[] genericArguments = new Type[] { type };

                    MethodInfo genericRegister = methodInfo.MakeGenericMethod(genericArguments);
                    genericRegister.Invoke(null, new object[] { handlerID });
                }
            }
        }

        public override void Unload()
        {
            registry = null;
            nameToNetID?.Clear();
            nameToNetID = null;
        }

        public static Cooldown Get(string id)
        {
            bool hasValue = nameToNetID.TryGetValue(id, out ushort netID);
            return hasValue ? registry[netID] : null;
        }

        /// <summary>
        /// Registers a CooldownHandler for use in netcode, assigning it a Cooldown and thus a netID. Cooldowns are useless until this has been done.
        /// </summary>
        /// <returns>The registered Cooldown.</returns>
        public static Cooldown<HandlerT> Register<HandlerT>(string id) where HandlerT : CooldownHandler
        {
            int currentMaxID = registry.Length;

            // This case only happens when you cap out at 65,536 cooldown registrations (which should never occur).
            // It just stops you from registering more cooldowns.
            if (nextCDNetID == currentMaxID)
                return null;

            Cooldown<HandlerT> cd = new Cooldown<HandlerT>(id, nextCDNetID);
            nameToNetID[cd.ID] = cd.netID;
            registry[cd.netID] = cd;
            ++nextCDNetID;

            // If the end of the array is reached, double its size.
            if (nextCDNetID == currentMaxID && currentMaxID < ushort.MaxValue)
            {
                Cooldown[] largerArray = new Cooldown[currentMaxID * 2];
                for (int i = 0; i < currentMaxID; ++i)
                    largerArray[i] = registry[i];

                registry = largerArray;
            }
            return cd;
        }
    }
}
