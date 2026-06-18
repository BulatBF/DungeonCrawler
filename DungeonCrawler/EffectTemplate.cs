namespace RogueLiteGame
{
    public class EffectTemplate
    {
        public string Name { get; set; } = "Unknown";
        public string Type { get; set; } = "instant";   // instant, over_time, modifier
        public string Stat { get; set; } = "health";     // health, damage, speed, teleport
        public int Value { get; set; } = 0;
        public int Duration { get; set; } = 0;
    }
}