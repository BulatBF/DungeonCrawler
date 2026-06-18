namespace RogueLiteGame
{
    public class SpellTemplate
    {
        public string Name { get; set; } = "Unknown";
        public string Effect { get; set; } = "none"; // ключ из effects.json
        public int DirectDamage { get; set; } = 0;
        public int ManaCost { get; set; } = 10;
        public int Range { get; set; } = 5;
        public int MinLevel { get; set; } = 1;
        public string Description { get; set; } = "";
        public bool Findable { get; set; } = false; // true = может выпасть со свитка
    }
}