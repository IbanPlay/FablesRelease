namespace CalamityFables.Core
{
    public static class SoundDirectory
    {
        public const string Sounds = "CalamityFables/Sounds/";
        public const string Tiles = Sounds + "TileSounds/";
        public const string Cooldowns = Sounds + "Cooldowns/";
        public const string Music = Sounds + "Music/";

        public const string Wulfrum = Sounds + "Wulfrum/";

        public const string Nautilus = Sounds + "SirNautilus/";
        public const string NautilusDrops = Sounds + "SirNautilusDrops/";

        public const string DesertScourge = Sounds + "DesertScourge/";
        public const string DesertScourgeDrops = Sounds + "DesertScourgeDrops/";

        public const string Crabulon = Sounds + "Crabulon/";
        public const string CrabulonDrops = Sounds + "CrabulonDrops/";

        public static class CommonSounds
        {
            public static readonly SoundStyle WulfrumNPCHitSound = new(Wulfrum + "WulfrumHit");
            public static readonly SoundStyle WulfrumNPCDeathSound = new(Wulfrum + "WulfrumDeath");
            public static readonly SoundStyle WulfrumTilePlaceSound = new(Wulfrum + "WulfrumPlace", 3);

            public static readonly SoundStyle Comedy = new SoundStyle("CalamityFables/Sounds/Comedy") { PlayOnlyIfFocused = true, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

            public static readonly SoundStyle Ultrabling = new("CalamityFables/Sounds/Ultrabling");

            public static readonly SoundStyle LouderItem7 = new("CalamityFables/Sounds/LouderItem7");


            public static readonly SoundStyle LooneyBlink1 = new("CalamityFables/Sounds/Blink1") { MaxInstances = 0 };
            public static readonly SoundStyle LooneyBlink2 = new("CalamityFables/Sounds/Blink2") { MaxInstances = 0 };
        }
    }
}