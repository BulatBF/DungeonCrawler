using Raylib_cs;
using System.Collections.Generic;

namespace RogueLiteGame
{
    public class Entity
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Symbol { get; set; }
        public Color EntityColor { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Damage { get; set; }
        public string Name { get; set; }
        public int Vision { get; set; }
        public int Speed { get; set; }
        public int Energy { get; set; }
        public string TextureKey { get; set; } = "";
        public List<ActiveEffect> ActiveEffects { get; set; } = new List<ActiveEffect>();

        public Entity(int x, int y, char symbol, Color color, int health, int damage, int vision, int speed, string name = "?")
        {
            X = x; Y = y; Symbol = symbol; EntityColor = color;
            MaxHealth = health;
            Health = health;
            Damage = damage;
            Name = name;
            Vision = vision;
            Speed = speed;
            Energy = 0;
        }
    }
}