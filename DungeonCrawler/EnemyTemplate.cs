namespace RogueLiteGame
{
    public class EnemyTemplate
    {
        public string Name { get; set; } = "Unknown";
        public char Symbol { get; set; } = '?';
        public byte R { get; set; } = 255;
        public byte G { get; set; } = 255;
        public byte B { get; set; } = 255;
        public int Health { get; set; } = 10;
        public int Damage { get; set; } = 5;
        public int Vision { get; set; } = 5;
        public int Speed { get; set; } = 100;
        public int SpawnWeight { get; set; } = 10;
        public int MinLevel { get; set; } = 1;
        public int ExperienceValue { get; set; } = 10;
        public bool IsBoss { get; set; } = false;
        public string Biome { get; set; } = "dungeon"; // dungeon, caves, catacombs, lair
    }
}