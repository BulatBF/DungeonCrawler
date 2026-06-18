using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace RogueLiteGame
{
    // ===== DTO-классы (то что пишется в JSON) =====

    public class SaveData
    {
        public int DungeonLevel { get; set; }
        public int CurRoomX { get; set; }
        public int CurRoomY { get; set; }
        public PlayerSave Player { get; set; } = new();
        public List<RoomSave> Rooms { get; set; } = new();
    }

    public class PlayerSave
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Damage { get; set; }
        public int Speed { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public List<string> InventoryKeys { get; set; } = new();
        public List<string> SpellKeys { get; set; } = new();
        public List<ActiveEffectSave> Effects { get; set; } = new();
    }

    public class ActiveEffectSave
    {
        public string Key { get; set; } = "";
        public int RemainingTurns { get; set; }
    }

    public class RoomSave
    {
        public int WorldX { get; set; }
        public int WorldY { get; set; }
        public bool Visited { get; set; }
        public List<string> Tiles { get; set; } = new();    // ряды карты
        public List<string> Explored { get; set; } = new();  // ряды из 0/1
        public List<EnemySave> Enemies { get; set; } = new();
        public List<ItemSave> Items { get; set; } = new();
    }

    public class EnemySave
    {
        public string Key { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
    }

    public class ItemSave
    {
        public string Key { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
    }

    // ===== Сама система =====

    public static class SaveSystem
    {
        private const string SaveFileName = "savegame.json";

        public static bool SaveExists() => File.Exists(SaveFileName);

        public static void DeleteSave()
        {
            if (File.Exists(SaveFileName)) File.Delete(SaveFileName);
        }

        public static void Save()
        {
            var data = new SaveData
            {
                DungeonLevel = Game.DungeonLevel,
                CurRoomX = Game.CurRoomX,
                CurRoomY = Game.CurRoomY,
                Player = new PlayerSave
                {
                    X = Game.Player.X,
                    Y = Game.Player.Y,
                    Health = Game.Player.Health,
                    MaxHealth = Game.Player.MaxHealth,
                    Damage = Game.Player.Damage,
                    Speed = Game.Player.Speed,
                    Level = Game.Player.Level,
                    Experience = Game.Player.Experience,
                    ExperienceToNextLevel = Game.Player.ExperienceToNextLevel,
                    Mana = Game.Player.Mana,
                    MaxMana = Game.Player.MaxMana,
                }
            };

            foreach (var item in Game.Player.Inventory)
                data.Player.InventoryKeys.Add(item.Key);
            foreach (var spell in Game.Player.SpellBook)
                data.Player.SpellKeys.Add(spell.Key);
            foreach (var eff in Game.Player.ActiveEffects)
                data.Player.Effects.Add(new ActiveEffectSave { Key = eff.Key, RemainingTurns = eff.RemainingTurns });

            // Сохраняем только существующие комнаты
            for (int wy = 0; wy < Settings.WorldSize; wy++)
            {
                for (int wx = 0; wx < Settings.WorldSize; wx++)
                {
                    Room room = Game.World[wx, wy];
                    if (room == null || !room.Exists) continue;

                    var rs = new RoomSave { WorldX = wx, WorldY = wy, Visited = room.Visited };

                    for (int y = 0; y < Settings.MapHeight; y++)
                    {
                        var tileRow = new StringBuilder(Settings.MapWidth);
                        var expRow = new StringBuilder(Settings.MapWidth);
                        for (int x = 0; x < Settings.MapWidth; x++)
                        {
                            tileRow.Append(room.Tiles[x, y]);
                            expRow.Append(room.Explored[x, y] ? '1' : '0');
                        }
                        rs.Tiles.Add(tileRow.ToString());
                        rs.Explored.Add(expRow.ToString());
                    }

                    foreach (var e in room.Enemies)
                        rs.Enemies.Add(new EnemySave { Key = e.TextureKey, X = e.X, Y = e.Y, Health = e.Health });
                    foreach (var it in room.Items)
                        rs.Items.Add(new ItemSave { Key = it.Key, X = it.X, Y = it.Y });

                    data.Rooms.Add(rs);
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(SaveFileName, JsonSerializer.Serialize(data, options));
        }

        public static bool Load()
        {
            if (!SaveExists()) return false;
            try
            {
                var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SaveFileName));
                if (data == null) return false;

                Game.DungeonLevel = data.DungeonLevel;
                Game.CurRoomX = data.CurRoomX;
                Game.CurRoomY = data.CurRoomY;

                // Игрок
                var p = data.Player;
                Game.Player.X = p.X;
                Game.Player.Y = p.Y;
                Game.Player.Health = p.Health;
                Game.Player.MaxHealth = p.MaxHealth;
                Game.Player.Damage = p.Damage;
                Game.Player.Speed = p.Speed;
                Game.Player.Level = p.Level;
                Game.Player.Experience = p.Experience;
                Game.Player.ExperienceToNextLevel = p.ExperienceToNextLevel;
                Game.Player.Mana = p.Mana;
                Game.Player.MaxMana = p.MaxMana;
                Game.Player.Energy = Settings.ActionCost;

                Game.Player.Inventory.Clear();
                foreach (string key in p.InventoryKeys)
                    Game.Player.Inventory.Add(ItemLibrary.CreateItem(key, 0, 0));

                Game.Player.SpellBook.Clear();
                foreach (string key in p.SpellKeys)
                    Game.Player.SpellBook.Add(SpellLibrary.CreateSpell(key));

                Game.Player.ActiveEffects.Clear();
                foreach (var es in p.Effects)
                {
                    var template = EffectLibrary.Get(es.Key);
                    if (template != null)
                    {
                        var ae = new ActiveEffect(es.Key, template);
                        ae.RemainingTurns = es.RemainingTurns;
                        Game.Player.ActiveEffects.Add(ae);
                    }
                }

                // Мир — пересоздаём пустым и заполняем из сейва
                Game.World = new Room[Settings.WorldSize, Settings.WorldSize];
                for (int wy = 0; wy < Settings.WorldSize; wy++)
                    for (int wx = 0; wx < Settings.WorldSize; wx++)
                        Game.World[wx, wy] = new Room();

                foreach (var rs in data.Rooms)
                {
                    Room room = Game.World[rs.WorldX, rs.WorldY];
                    room.Exists = true;
                    room.Visited = rs.Visited;

                    for (int y = 0; y < Settings.MapHeight; y++)
                    {
                        for (int x = 0; x < Settings.MapWidth; x++)
                        {
                            room.Tiles[x, y] = rs.Tiles[y][x];
                            room.Explored[x, y] = rs.Explored[y][x] == '1';
                        }
                    }

                    foreach (var es in rs.Enemies)
                    {
                        var enemy = EnemyLibrary.CreateEnemy(es.Key, es.X, es.Y);
                        enemy.Health = es.Health;
                        room.Enemies.Add(enemy);
                    }
                    foreach (var its in rs.Items)
                        room.Items.Add(ItemLibrary.CreateItem(its.Key, its.X, its.Y));
                }

                Game.CurrentState = GameState.Playing;
                Game.UpdateVisionMap();
                return true;
            }
            catch (Exception ex)
            {
                Game.Log($"Ошибка загрузки: {ex.Message}");
                return false;
            }
        }
    }
}