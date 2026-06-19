using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RogueLiteGame
{
    public class RunRecord
    {
        public int Floor { get; set; }
        public int Kills { get; set; }
        public int Level { get; set; }
        public float Time { get; set; }
        public string Date { get; set; } = "";
    }

    public static class ScoreSystem
    {
        private const string FileName = "scores.json";
        private const int MaxRecords = 10;
        public static List<RunRecord> Records { get; private set; } = new();

        public static void Load()
        {
            if (!File.Exists(FileName)) { Records = new(); return; }
            try
            {
                var data = JsonSerializer.Deserialize<List<RunRecord>>(File.ReadAllText(FileName));
                Records = data ?? new();
            }
            catch { Records = new(); }
        }

        public static void RecordRun(int floor, int kills, int level, float time)
        {
            Records.Add(new RunRecord
            {
                Floor = floor, Kills = kills, Level = level, Time = time,
                Date = DateTime.Now.ToString("dd.MM.yyyy")
            });

            // Сортировка: глубже этаж — выше; при равенстве — больше убийств
            Records = Records
                .OrderByDescending(r => r.Floor)
                .ThenByDescending(r => r.Kills)
                .Take(MaxRecords)
                .ToList();

            Save();
        }

        private static void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(FileName, JsonSerializer.Serialize(Records, options));
        }

        public static void Reset()
        {
            Records = new();
            if (File.Exists(FileName)) File.Delete(FileName);
        }

        // Лучший результат (для подсветки "новый рекорд")
        public static int BestFloor => Records.Count > 0 ? Records.Max(r => r.Floor) : 0;
    }
}