using CalamityFables.Particles;
using Terraria.DataStructures;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    [Autoload(false)]
    public class CrabulonBodyGore : ModGore
    {
        public override string Texture => AssetDirectory.Crabulon + "Gores/" + Name;
        public override string Name => InternalName != "" ? InternalName : base.Name;

        public string InternalName;

        public CrabulonBodyGore(string name)
        {
            InternalName = name;
        }

        public override void SetStaticDefaults()
        {
            if (!Main.dedServ)
            {
                GoreID.Sets.DrawBehind[Type] = true;
                ChildSafety.SafeGore[Type] = true;
            }
        }

        public override void OnSpawn(Gore gore, IEntitySource source)
        {
            gore.position -= new Vector2(gore.Width / 2, gore.Height / 2);
            gore.behindTiles = true;
        }

        public override bool Update(Gore gore)
        {
            //gore.rotation +=
            //gore.velocity = Collision.TileCollision(gore.position, gore.velocity, (int)((float)gore.Width * gore.scale), (int)((float)gore.Height * gore.scale));

            gore.timeLeft--;
            gore.position += gore.velocity;
            gore.rotation += gore.velocity.X * 0.02f;
            gore.velocity.Y += 0.3f;

            Rectangle hitbox = new Rectangle((int)gore.position.X, (int)gore.position.Y, (int)gore.Width, (int)gore.Height);
            hitbox.Inflate(-20, -45);

            Vector2 previousVelocity = gore.velocity;
            gore.velocity = Collision.TileCollision(hitbox.TopLeft(), gore.velocity, hitbox.Width, hitbox.Height);
            if (previousVelocity != gore.velocity)
                gore.velocity.X *= 0.95f;

            if (Math.Abs(gore.velocity.X) < 0.1f && gore.timeLeft > 110)
                gore.timeLeft = 110;

            if (gore.timeLeft <= 0)
            {
                gore.alpha += 3;
                gore.drawOffset.Y = MathF.Pow(gore.alpha / 255f, 1.6f) * 40f;
            }

            if (gore.alpha >= 255)
                gore.active = false;
            return false;
        }
    }

}
