using Raylib_cs;

namespace RogueLiteGame
{
    public class Player : Entity
    {
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int ExperienceToNextLevel { get; set; } = 100;
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public float ManaRegenAccumulator { get; set; } = 0f; 
        public List<Item> Inventory { get; set; } = new List<Item>();
        public List<Spell> SpellBook { get; set; } = new List<Spell>();

        public Player() : base(
            Settings.MapWidth / 2, Settings.MapHeight / 2,
            '@', Color.SkyBlue,
            Settings.DefaultPlayerHealth,
            Settings.DefaultPlayerDamage,
            Settings.PlayerVisionRadius,
            Settings.DefaultPlayerSpeed,
            "Игрок")
        { }

        public void ResetStats()
        {
            X = Settings.MapWidth / 2;
            Y = Settings.MapHeight / 2;
            Health = Settings.DefaultPlayerHealth;
            MaxHealth = Settings.DefaultPlayerHealth;
            Damage = Settings.DefaultPlayerDamage;
            Speed = Settings.DefaultPlayerSpeed;
            Level = 1;
            Experience = 0;
            ExperienceToNextLevel = 100;
            Energy = Settings.ActionCost;
            Inventory.Clear();
            MaxMana = Settings.DefaultPlayerMana;
            Mana = MaxMana;
            ManaRegenAccumulator = 0f;
            SpellBook.Clear();
        }
    }
}