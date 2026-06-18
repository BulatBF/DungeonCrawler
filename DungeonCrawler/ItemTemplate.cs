using System.Linq;

namespace RogueLiteGame
{
    public class ItemTemplate
    {
        public string Name { get; set; } = "Unknown";
        public char Symbol { get; set; } = '?';
        public byte R { get; set; } = 255;
        public byte G { get; set; } = 255;
        public byte B { get; set; } = 255;
        public string Effect { get; set; } = "none"; // ключ из effects.json
        public int SpawnWeight { get; set; } = 10;
        public int MinLevel { get; set; } = 1;
        public string Description { get; set; } = "";
        public string TeachesSpell { get; set; } = ""; // ключ заклинания которое изучается
    }
}