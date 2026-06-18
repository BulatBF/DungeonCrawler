using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Text.Encodings.Web;

namespace RogueLiteGame
{
    public static class SpellLibrary
    {
        private static Dictionary<string, SpellTemplate> _templates = new();
        private const string ConfigFileName = "spells.json";
        private static Random rnd = new Random();
        public static List<string> GetAllKeys() => _templates.Keys.ToList();

        public static void Initialize()
        {
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFileName);
                    var data = JsonSerializer.Deserialize<Dictionary<string, SpellTemplate>>(json);
                    if (data != null) _templates = data;
                }
                catch (Exception ex)
                {
                    Game.Log($"spells.json ошибка: {ex.Message}");
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
            _templates.Add("firebolt", new SpellTemplate { Name = "Огненная стрела", Description = "Наносит 12 урона", Effect = "burn", DirectDamage = 12, ManaCost = 10, Range = 6, MinLevel = 1 });
            _templates.Add("poison_dart", new SpellTemplate { Name = "Ядовитый дротик", Description = "Наносит 4 урона и отравляет цель на 3 хода", Effect = "poison", DirectDamage = 4, ManaCost = 8, Range = 5, MinLevel = 1 });
            _templates.Add("heal_self", new SpellTemplate { Name = "Самоисцеление", Description = "Восстанавливает 30 здоровья", Effect = "heal", DirectDamage = 0, ManaCost = 15, Range = 0, MinLevel = 1 });
        }

        private static void SaveToDisk()
        {
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            string json = JsonSerializer.Serialize(_templates, options);
            File.WriteAllText(ConfigFileName, json);
        }

        public static Spell CreateSpell(string key)
        {
            if (!_templates.TryGetValue(key, out var t)) t = new SpellTemplate();
            return new Spell(key, t);
        }

        public static SpellTemplate? GetTemplate(string key)
        {
            _templates.TryGetValue(key, out var t);
            return t;
        }
        public static string GetRandomFindableSpell(int level)
        {
            var pool = _templates
                .Where(p => p.Value.Findable && p.Value.MinLevel <= level)
                .Select(p => p.Key)
                .ToList();

            if (pool.Count == 0) return "";
            return pool[rnd.Next(pool.Count)];
        }
    }
}