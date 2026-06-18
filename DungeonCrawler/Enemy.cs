using Raylib_cs;

namespace RogueLiteGame
{
    public class Enemy : Entity
    {
        public int ExperienceReward { get; set; }
        public int LastKnownPlayerX { get; set; } = -1;
        public int LastKnownPlayerY { get; set; } = -1;
        public bool IsSearching { get; set; } = false;
        public bool IsBoss { get; set; } = false;

        public Enemy(int x, int y, char symbol, Color color, int health, int damage, int vision, int speed, string name, int xpReward)
            : base(x, y, symbol, color, health, damage, vision, speed, name)
        {
            ExperienceReward = xpReward;
        }
    }
}