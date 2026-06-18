using Raylib_cs;

namespace RogueLiteGame
{
    public class Item
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public char Symbol { get; set; }
        public Color ItemColor { get; set; }
        public string Effect { get; set; } // ключ эффекта
        public int X { get; set; }
        public int Y { get; set; }
        public string Description { get; set; }
        public string TeachesSpell { get; set; }

        public Item(string key, ItemTemplate t, int x, int y)
        {
            Key = key;
            Name = t.Name;
            Symbol = t.Symbol;
            ItemColor = new Color(t.R, t.G, t.B, (byte)255);
            Effect = t.Effect;
            X = x;
            Y = y;
            Description = t.Description;
            TeachesSpell = t.TeachesSpell;
        }
    }
}