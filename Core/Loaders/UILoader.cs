using Terraria.UI;

//Taken from starlight river. Ily guys <3
namespace CalamityFables.Core
{
    public class AutoUISystem : ModSystem
    {
        public static List<UserInterface> UserInterfaces;
        public static List<SmartUIState> UIStates;

        public static float MapHeight;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            Mod Mod = CalamityFables.Instance;

            UserInterfaces = new List<UserInterface>();
            UIStates = new List<SmartUIState>();

            foreach (Type t in Mod.Code.GetTypes())
            {
                if (t.IsSubclassOf(typeof(SmartUIState)))
                {
                    var state = (SmartUIState)Activator.CreateInstance(t, null);
                    var userInterface = new UserInterface();
                    userInterface.SetState(state);

                    UIStates?.Add(state);
                    UserInterfaces?.Add(userInterface);
                }
            }
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            UIStates.ForEach(n => n.Unload());
            UserInterfaces = null;
            UIStates = null;
        }

        public static void AddLayer(List<GameInterfaceLayer> layers, UIState state, int index, InterfaceScaleType scale)
        {
            string name = state == null ? "Unknown" : state.ToString();
            layers.Insert(index, new LegacyGameInterfaceLayer("CalamityFables: " + name,
                delegate {
                    state.Draw(Main.spriteBatch);
                    return true;
                }, scale));
        }

        public static T GetUIState<T>() where T : SmartUIState => UIStates.FirstOrDefault(n => n is T) as T;

        public static void ReloadState<T>() where T : SmartUIState
        {
            var index = UIStates.IndexOf(GetUIState<T>());
            UIStates[index] = (T)Activator.CreateInstance(typeof(T), null);
            UserInterfaces[index] = new UserInterface();
            UserInterfaces[index].SetState(UIStates[index]);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            for (int k = 0; k < UIStates.Count; k++)
            {
                var state = UIStates[k];
                if (state.Visible || state.UpdatesWhileInvisible)
                {
                    UserInterfaces[k].Update(gameTime);
                }
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            //Main.Mh is private. FUCK you main
            MapHeight = 0;
            if (Main.mapEnabled)
            {
                if (!Main.mapFullscreen && Main.mapStyle == 1)
                {
                    MapHeight = 256;
                }

                if (MapHeight + Main.instance.RecommendedEquipmentAreaPushUp > Main.screenHeight)
                    MapHeight = Main.screenHeight - Main.instance.RecommendedEquipmentAreaPushUp;
            }

            for (int k = 0; k < UIStates.Count; k++)
            {
                var state = UIStates[k];
                if (state.Visible)
                    AddLayer(layers, state, state.InsertionIndex(layers), state.Scale);
            }
        }
    }
}
