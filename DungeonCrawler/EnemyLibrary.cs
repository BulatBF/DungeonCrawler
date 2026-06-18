using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Raylib_cs;
using System.Text.Encodings.Web;

namespace RogueLiteGame
{
    public static class EnemyLibrary
    {
        private static Dictionary<string, EnemyTemplate> _templates = new();
        private const string ConfigFileName = "enemies.json";
        private static Random rnd = new Random();
        public static List<string> GetAllKeys() => _templates.Keys.ToList();

        public static void Initialize()
        {
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFileName);
                    var data = JsonSerializer.Deserialize<Dictionary<string, EnemyTemplate>>(json);
                    if (data != null) _templates = data;
                }
                catch (Exception ex)
                {
                    Game.Log($"JSON ошибка: {ex.Message}");
                    LoadDefaults();
                }
            }
            else
            {
                LoadDefaults();
                SaveToDisk();
            }
        }

        private static void LoadDefaults()
        {
            _templates.Clear();
            _templates.Add("rat", new EnemyTemplate { Name = "Крыса", Symbol = 'r', R = 160, G = 160, B = 160, Health = 15, Damage = 5, Vision = 6, Speed = 150, SpawnWeight = 100, MinLevel = 1, ExperienceValue = 10 });
            _templates.Add("ghost", new EnemyTemplate { Name = "Призрак", Symbol = 'g', R = 200, G = 200, B = 255, Health = 20, Damage = 10, Vision = 12, Speed = 100, SpawnWeight = 40, MinLevel = 1, ExperienceValue = 25 });
            _templates.Add("ogre", new EnemyTemplate { Name = "Огр", Symbol = 'O', R = 255, G = 50, B = 50, Health = 60, Damage = 15, Vision = 8, Speed = 60, SpawnWeight = 10, MinLevel = 2, ExperienceValue = 100 });
        }

        private static void SaveToDisk()
        {
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
            string json = JsonSerializer.Serialize(_templates, options);
            File.WriteAllText(ConfigFileName, json);
        }

        public static Enemy CreateEnemy(string key, int x, int y)
        {
            if (!_templates.TryGetValue(key, out var t))
                return new Enemy(x, y, '?', Color.White, 10, 1, 1, 100, "Ошибка", 0);

            var enemy = new Enemy(x, y, t.Symbol, new Color(t.R, t.G, t.B, (byte)255),
                t.Health, t.Damage, t.Vision, t.Speed, t.Name, t.ExperienceValue);
            enemy.TextureKey = key; // ← ключ = имя PNG (rat.png, ghost.png...)
            enemy.IsBoss = t.IsBoss;
            return enemy;
        }

        // Вспомогательный метод, чтобы получить случайного врага из библиотеки
        public static string GetRandomEnemyKeyForLevel(int level)
        {
            // 1. Фильтруем врагов по уровню
            string biome = Game.CurrentBiome;
            var available = _templates
                .Where(p => p.Value.MinLevel <= level
                        && p.Value.SpawnWeight > 0
                        && p.Value.Biome == biome)
                .ToList();

            if (available.Count == 0)
                available = _templates.Where(p => p.Value.SpawnWeight > 0).ToList(); // fallback

            // 2. Считаем суммарный вес
            int totalWeight = available.Sum(pair => pair.Value.SpawnWeight);

            // 3. Выбираем случайное число
            int roll = rnd.Next(0, totalWeight);
            int currentWeight = 0;

            // 4. Ищем, в какой диапазон попало число
            foreach (var pair in available)
            {
                currentWeight += pair.Value.SpawnWeight;
                if (roll < currentWeight)
                    return pair.Key;
            }

            return available.Last().Key;
        }
    }
}