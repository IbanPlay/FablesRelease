using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityFables.Content.Items.Food
{
    public abstract class BaseFoodItem : ModItem
    {
        public override string Texture => AssetDirectory.Food + Name;

        /// <summary>
        /// Buff given by this food item
        /// </summary>
        public abstract int FoodBuff { get; }

        /// <summary>
        /// Duration of the food buff
        /// </summary>
        public virtual int BuffTime => 60 * 60 * 5;

        public virtual Point SpriteSize => new Point(22, 22);

        /// <summary>
        /// This only changes the physics of the crumbs when eaten
        /// </summary>
        public virtual bool SolidFood => true;
        /// <summary>
        /// Color of the food crumbs when eaten
        /// </summary>
        public abstract Color[] CrumbColors { get; }

        /// <summary>
        /// Automatically makes the food item shimmer into <see cref="ItemID.Ambrosia"/> and adds it to the <see cref="RecipeGroupID.Fruit"/> recipe group
        /// </summary>

        public virtual bool IsFruit => false;

        public sealed override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(int.MaxValue, 3) { NotActuallyAnimating = true });
            ItemID.Sets.IsFood[Type] = true;

            if (IsFruit)
            {
                ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.Ambrosia;
                RecipeGroup.recipeGroups[RecipeGroupID.Fruit].ValidItems.Add(Type);
            }

            if (SolidFood)
                ItemID.Sets.FoodParticleColors[Item.type] = CrumbColors;
            else
                ItemID.Sets.DrinkParticleColors[Item.type] = CrumbColors;
        }

        public override sealed void SetDefaults()
        {
            Item.DefaultToFood(SpriteSize.X, SpriteSize.Y, FoodBuff, BuffTime, !SolidFood);
            SafeSetDefaults();
        }

        public virtual void SafeSetDefaults() { }

    }
}
