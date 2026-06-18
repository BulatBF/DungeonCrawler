using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace RogueLiteGame
{
    public static class EffectLibrary
    {
        private static Dictionary<string, EffectTemplate> _templates = new();
        private const string ConfigFileName = "effects.json";
        private static Random rnd = new Random();

        public static void Initialize()
        {
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFileName);
                    var data = JsonSerializer.Deserialize<Dictionary<string, EffectTemplate>>(json);
                    if (data != null) _templates = data;
                }
                catch (Exception ex)
                {
                    Game.Log($"effects.json ошибка: {ex.Message}");
                    LoadDefaults();
                }
            }
            else { LoadDefaults(); SaveToDisk(); }
        }

        private static void LoadDefaults()
        {
            _templates.Clear();
            _templates.Add("heal", new EffectTemplate { Name = "Исцеление", Type = "instant", Stat = "health", Value = 30, Duration = 0 });
            _templates.Add("strength_buff", new EffectTemplate { Name = "Сила", Type = "modifier", Stat = "damage", Value = 5, Duration = 0 });
            _templates.Add("teleport", new EffectTemplate { Name = "Телепорт", Type = "instant", Stat = "teleport", Value = 0, Duration = 0 });
            _templates.Add("poison", new EffectTemplate { Name = "Яд", Type = "over_time", Stat = "health", Value = -5, Duration = 3 });
        }

        private static void SaveToDisk()
        {
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(ConfigFileName, JsonSerializer.Serialize(_templates, options));
        }

        public static EffectTemplate? Get(string key)
        {
            _templates.TryGetValue(key, out var t);
            return t;
        }

        // Применить эффект к существу. room нужен для телепорта.
        public static void Apply(string key, Entity target, Room room)
        {
            var t = Get(key);
            if (t == null) { Game.Log($"Эффект '{key}' не найден."); return; }

            switch (t.Type)
            {
                case "instant":
                    ApplyInstant(t, target, room);
                    break;

                case "over_time":
                    {
                        var active = new ActiveEffect(key, t);
                        target.ActiveEffects.Add(active);
                        Game.Log($"{target.Name}: наложен эффект {t.Name} ({t.Duration} ходов)");
                    }
                    break;
                case "modifier":
                    if (t.Duration <= 0)
                    {
                        // Постоянный бафф — применяем и забываем
                        ModifyStat(t.Stat, t.Value, target);
                        Game.Log($"{target.Name}: {t.Name} (+{t.Value})");
                    }
                    else
                    {
                        // Временный бафф — вешаем на N ходов
                        var active = new ActiveEffect(key, t);
                        target.ActiveEffects.Add(active);
                        ModifyStat(t.Stat, t.Value, target);
                        Game.Log($"{target.Name}: наложен эффект {t.Name} ({t.Duration} ходов)");
                    }
                    break;
            }
        }

        private static void ApplyInstant(EffectTemplate t, Entity target, Room room)
        {
            switch (t.Stat)
            {
                case "health":
                    target.Health = Math.Min(target.MaxHealth, target.Health + t.Value);
                    Game.Log($"{target.Name}: {(t.Value >= 0 ? "+" : "")}{t.Value} HP");
                    break;
                case "damage":  // ← добавить
                    target.Damage += t.Value;
                    Game.Log($"{target.Name}: урон +{t.Value} (теперь {target.Damage})");
                    break;
                case "speed":   // ← добавить
                    target.Speed += t.Value;
                    Game.Log($"{target.Name}: скорость +{t.Value}");
                    break;
                case "teleport":
                    int tx, ty, attempts = 0;
                    do { tx = rnd.Next(1, Settings.MapWidth - 1); ty = rnd.Next(1, Settings.MapHeight - 1); attempts++; }
                    while (room.Tiles[tx, ty] != ' ' && attempts < 100);
                    if (attempts < 100) { target.X = tx; target.Y = ty; Game.Log("Телепортация!"); }
                    break;
            }
        }

        private static void ModifyStat(string stat, int value, Entity target)
        {
            switch (stat)
            {
                case "damage": target.Damage += value; break;
                case "speed": target.Speed += value; break;
                case "health": target.MaxHealth += value; break;
            }
        }

        // Вызывается каждый ход — тикают over_time, истекают модификаторы
        public static void TickEffects(Entity target)
        {
            for (int i = target.ActiveEffects.Count - 1; i >= 0; i--)
            {
                var eff = target.ActiveEffects[i];

                if (!eff.IsModifier)
                {
                    // over_time — применяем эффект каждый ход
                    if (eff.Stat == "health")
                    {
                        target.Health += eff.Value;
                        if (target.Health > target.MaxHealth) target.Health = target.MaxHealth;
                    }
                }

                eff.RemainingTurns--;
                if (eff.RemainingTurns <= 0)
                {
                    // Модификатор — откатываем бафф обратно
                    if (eff.IsModifier)
                        ModifyStat(eff.Stat, -eff.Value, target);
                    Game.Log($"{target.Name}: эффект '{eff.Name}' закончился");
                    target.ActiveEffects.RemoveAt(i);
                }
            }
        }
    }
}