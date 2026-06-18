using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Text.Encodings.Web;

namespace RogueLiteGame
{
    public static class ItemLibrary
    {
        private static Dictionary<string, ItemTemplate> _templates = new();
        private const string ConfigFileName = "items.json";
        private static Random rnd = new Random();
        public static List<string> GetAllKeys() => _templates.Keys.ToList();

        public static void Initialize()
        {
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFileName);
                    var data = JsonSerializer.Deserialize<Dictionary<string, ItemTemplate>>(json);
                    if (data != null) _templates = data;
                }
                catch (Exception ex)
                {
                    Game.Log($"items.json ошибка: {ex.Message}");
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
            _templates.Add("health_potion", new ItemTemplate { Name = "Зелье здоровья", Description = "Восстанавливает 30 здоровья", Symbol = '!', R = 255, G = 50, B = 50, Effect = "heal", SpawnWeight = 60, MinLevel = 1 });
            _templates.Add("strength_potion", new ItemTemplate { Name = "Зелье силы", Description = "Увеличивает силу на 5", Symbol = '!', R = 255, G = 165, B = 0, Effect = "strength", SpawnWeight = 25, MinLevel = 1 });
            _templates.Add("teleport_scroll", new ItemTemplate { Name = "Свиток телепорта", Description = "Телепортирует вас в случайную точку комнаты", Symbol = '?', R = 200, G = 100, B = 255, Effect = "teleport", SpawnWeight = 15, MinLevel = 1 });
        }

        private static void SaveToDisk()
        {
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            string json = JsonSerializer.Serialize(_templates, options);
            File.WriteAllText(ConfigFileName, json);
        }

        public static Item CreateItem(string key, int x, int y)
        {
            if (!_templates.TryGetValue(key, out var t))
                t = new ItemTemplate(); // заглушка
            return new Item(key, t, x, y);
        }

        public static string GetRandomItemKeyForLevel(int level)
        {
            var available = _templates.Where(p => p.Value.MinLevel <= level).ToList();
            if (available.Count == 0) return _templates.Keys.First();

            int totalWeight = available.Sum(p => p.Value.SpawnWeight);
            int roll = rnd.Next(0, totalWeight);
            int current = 0;
            foreach (var pair in available)
            {
                current += pair.Value.SpawnWeight;
                if (roll < current) return pair.Key;
            }
            return available.Last().Key;
        }
        public static ItemTemplate? GetTemplate(string key)
        {
            _templates.TryGetValue(key, out var t);
            return t;
        }
    }
}