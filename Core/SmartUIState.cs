using Terraria.UI;

//Taken from starlight river. Love u guys <3
namespace CalamityFables.Core
{
    public abstract class SmartUIState : UIState
    {
        public abstract int InsertionIndex(List<GameInterfaceLayer> layers);

        public virtual bool Visible { get; set; } = false;

        public virtual InterfaceScaleType Scale { get; set; } = InterfaceScaleType.UI;

        public virtual void Unload() { }

        public virtual bool UpdatesWhileInvisible { get; set; } = false;
    }
}
