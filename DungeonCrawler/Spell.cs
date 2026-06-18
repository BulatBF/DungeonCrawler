namespace RogueLiteGame
{
    public class Spell
    {
        public string Key;
        public string Name;
        public string Effect;
        public int DirectDamage;
        public int ManaCost;
        public int Range;
        public string Description;

        public Spell(string key, SpellTemplate t)
        {
            Key = key; Name = t.Name; Effect = t.Effect;
            DirectDamage = t.DirectDamage; ManaCost = t.ManaCost; Range = t.Range;
            Description = t.Description;
        }
    }
}