using CalamityFables.Cooldowns;
using Terraria.Localization;
using Terraria.Map;
using Terraria.UI;

namespace CalamityFables.Content.Items.SirNautilusDrops
{
    public class WarriorsAmphora : ModItem
    {
        public static readonly SoundStyle TeleportationSound = new SoundStyle(SoundDirectory.NautilusDrops + "AmphoraTeleport") {  Volume = 0.8f };

        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public const float MINWARPCOOLDOWN = 60f * 60f * 1.5f; //1.5 minutes minimum
        public const float MAXWARPCOOLDOWN = 60f * 60f * 8f; //8 minutes maximum

        public const float MINWARPCOOLDOWNDISTANCE = 16f * 320; //320 tiles to get the minimum lenght
        public const float MAXWARPCOOLDOWNDISTANCE = 16f * 3200; //3200 tiles to get the maximum cooldown lenght

        public const int TELEPORT_STYLE = 101;

        public override void Load()
        {
            FablesGeneralSystemHooks.CustomTeleportationEffectEvent += AmporaWarpEffect;
        }

        private bool AmporaWarpEffect(Rectangle effectRect, int Style, int extraInfo, float dustCountMult, TeleportationSide side, Vector2 otherPosition)
        {
            //We use the item's type to get a teleportation ID that's hopefully not gonna get taken by other mods
            //update, in mp the style is a byte so we cant get the full range, hopefully this will be fine
            if (Style != TELEPORT_STYLE)
                return false;


            Vector2 teleportationPosition = effectRect.Center();
            SoundEngine.PlaySound(TeleportationSound, teleportationPosition);
            for (int i = 0; i < 43; i++)
            {
                Dust cust = Dust.NewDustDirect(teleportationPosition - Vector2.UnitX * 10f + Vector2.UnitY * 20f, effectRect.Width + 20, effectRect.Height, ModContent.DustType<SpectralWaterDustNoisy>(), 0f, 0f, 100, default, 1.8f);
                cust.noGravity = true;
                cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 5f);
                cust.rotation = 1.7f;
                cust.customData = Color.Teal.ToVector3();
            }

            return true;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Warrior's Amphora");
            Tooltip.SetDefault("Grants the ability to teleport to the location of your death\n" +
                "Click your death marker on the fullscreen map to teleport\n" +
                "Teleporting consumes one recall potion, and the cooldown scales with how far you died");
        }

        public override void SetDefaults()
        {
            Item.width = Item.height = 22;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
        }

        public override void UpdateInventory(Player player)
        {
            if (!player.HasCooldown(WarriorsAmphoraCooldown.ID))
                player.GetModPlayer<DeathWarpPlayer>().canDeathWarp = true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (!Main.LocalPlayer.showLastDeath || Main.LocalPlayer.lastDeathPostion == Vector2.Zero || !Main.LocalPlayer.GetModPlayer<DeathWarpPlayer>().canDeathWarp)
            {
                Texture2D tex = ModContent.Request<Texture2D>(Texture + "Closed").Value;
                Vector2 originalSize = ModContent.Request<Texture2D>(Texture + "Closed").Value.Size();

                spriteBatch.DrawNewInventorySprite(tex, originalSize, position, drawColor, origin, scale, Vector2.UnitY * 5f);
                return false;
            }


            Texture2D animatedTex = ModContent.Request<Texture2D>(Texture + "Animated").Value;
            frame = animatedTex.BetterFrame(1, 6, 0, (int)((Main.GlobalTimeWrappedHourly * 13f) % 6));
            spriteBatch.Draw(animatedTex, position, frame, drawColor, 0, origin, scale, 0, 0);

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D animatedTex = ModContent.Request<Texture2D>(Texture + "Animated").Value;
            Rectangle frame = animatedTex.BetterFrame(1, 6, 0, (int)((Main.GlobalTimeWrappedHourly * 13f) % 6));
            spriteBatch.Draw(animatedTex, Item.position - Vector2.UnitY * 2f - Main.screenPosition, frame, lightColor, 0, frame.Size() / 2, scale, 0, 0);

            return false;
        }
    }

    public class DeathWarpPlayer : ModPlayer
    {
        public bool canDeathWarp;
        public override void ResetEffects()
        {
            canDeathWarp = false;
        }
    }

    public class DeathWarpMapMarker : ModMapLayer
    {
        public static Asset<Texture2D> MapIcon;
        public static bool MouseOver = false;

        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (!Main.LocalPlayer.showLastDeath || Main.LocalPlayer.lastDeathPostion == Vector2.Zero || !Main.mapFullscreen)
                return;
            if (!Main.LocalPlayer.GetModPlayer<DeathWarpPlayer>().canDeathWarp)
                return;

            if (MapIcon == null)
                MapIcon = ModContent.Request<Texture2D>(AssetDirectory.UI + "NautilusDeathWarp");
            Texture2D icon = MapIcon.Value;

            bool hasRecallPot = false;
            bool hasUnityPot = false;

            if (Main.LocalPlayer.HasItem(ItemID.RecallPotion))
                hasRecallPot = true;
            else if (Main.LocalPlayer.HasItem(ItemID.WormholePotion))
                hasUnityPot = true;
            else
                return;

            // Here we define the scale that we wish to draw the icon when hovered and not hovered.
            float scaleIfNotSelected = 1f;
            float scaleIfSelected = (hasRecallPot || hasUnityPot) ? 1.15f : 1f;


            if (context.Draw(icon, Main.LocalPlayer.lastDeathPostion / 16, Color.White, new(1, 1, 0, 0), scaleIfNotSelected, scaleIfSelected, Alignment.Center).IsMouseOver)
            {
                if (!MouseOver)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    MouseOver = true;
                }

                text = Language.GetTextValue("Game.TeleportTo", "last death position");
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    Main.mapFullscreen = false;

                    int consumedPotionID = hasRecallPot ? ItemID.RecallPotion : ItemID.WormholePotion;

                    Vector2 teleportationPosition = Main.LocalPlayer.lastDeathPostion;
                    int steps = 0;
                    while (Collision.SolidCollision(teleportationPosition, Main.LocalPlayer.width, Main.LocalPlayer.height))
                    {
                        teleportationPosition.Y -= 16;
                        steps++;
                        if (steps > 3)
                        {
                            teleportationPosition = Main.LocalPlayer.lastDeathPostion;
                            break;
                        }
                    }

                    float travelDistance = Main.LocalPlayer.Distance(teleportationPosition);
                    float cooldownDuration = WarriorsAmphora.MINWARPCOOLDOWN + (WarriorsAmphora.MAXWARPCOOLDOWN - WarriorsAmphora.MINWARPCOOLDOWN) * Utils.GetLerpValue(WarriorsAmphora.MINWARPCOOLDOWNDISTANCE, WarriorsAmphora.MAXWARPCOOLDOWNDISTANCE, travelDistance, true);
                    Main.LocalPlayer.AddCooldown(WarriorsAmphoraCooldown.ID, (int)(cooldownDuration));

                    Main.LocalPlayer.CustomTeleport(teleportationPosition, WarriorsAmphora.TELEPORT_STYLE);
                    Main.LocalPlayer.ConsumeItem(consumedPotionID);
                    Main.LocalPlayer.showLastDeath = false;
                    Main.LocalPlayer.immune = true;
                    Main.LocalPlayer.immuneNoBlink = false;
                    Main.LocalPlayer.immuneTime = 60;
                }
            }

            else
                MouseOver = false;
        }
    }
}
