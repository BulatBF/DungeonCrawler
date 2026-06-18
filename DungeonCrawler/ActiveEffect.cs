namespace RogueLiteGame
{
    // Экземпляр эффекта, действующий на конкретное существо
    public class ActiveEffect
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Stat { get; set; }
        public int Value { get; set; }
        public int RemainingTurns { get; set; }
        public bool IsModifier { get; set; } // true = бафф (откатить в конце), false = over_time (тикает)

        public ActiveEffect(string key, EffectTemplate t)
        {
            Key = key;
            Name = t.Name;
            Stat = t.Stat;
            Value = t.Value;
            RemainingTurns = t.Duration;
            IsModifier = t.Type == "modifier";
        }
    }
}