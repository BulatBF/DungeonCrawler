using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace RogueLiteGame
{
    public enum GameState { MainMenu, Playing, GameOver, Victory, Console, Map, LevelUp, Tutorial, Targeting, LearnSpell, Scores }
    public static class Game
    {
        public static Room[,] World = new Room[Settings.WorldSize, Settings.WorldSize];
        public static int CurRoomX = Settings.WorldSize / 2;
        public static int CurRoomY = Settings.WorldSize / 2;
        public static Player Player = new Player();
        
        public static List<string> Messages = new List<string>();
        public static GameState CurrentState = GameState.MainMenu;
        private static Random rnd = new Random();
        public static string CurrentCommand = "";
        public static int DungeonLevel = 1;
        private static float moveTimer = 0f;
        private const float MoveDelay = 0.12f; // задержка между шагами при зажатии (секунды)
        private const float FirstMoveDelay = 0.25f; // задержка перед автоповтором
        private static bool firstMove = true;
        public static bool ShouldExit = false;
        public static Spell? SelectedSpell = null;     // какое заклинание кастуем
        public static int TargetIndex = 0;             // индекс выбранного врага
        public static List<Enemy> ValidTargets = new(); // враги в радиусе
        public static Spell? PendingSpell = null;
        public static int EnemiesKilled = 0;
        public static int ItemsCollected = 0;
        public static float RunStartTime = 0f;   // время старта забега (сек)
        public static float RunElapsedTime = 0f; // итоговое время забега
        public static string CurrentBiome => DungeonLevel switch
        {
            <= 5  => "dungeon",
            <= 10 => "caves",
            <= 15 => "catacombs",
            _     => "lair"
        };
        
        public static void Initialize()
        {

            CurrentState = GameState.Playing;
            Messages.Clear();

            Player.ResetStats();
            EnemiesKilled = 0;
            ItemsCollected = 0;
            RunStartTime = (float)Raylib.GetTime();
            RunElapsedTime = 0f;

            MapGenerator.GenerateWorld(World, Settings.WorldSize);

            CurRoomX = Settings.WorldSize / 2;
            CurRoomY = Settings.WorldSize / 2;

            GetCurrentRoom().Visited = true;
            Game.Log("Добро пожаловать в лабиринт!");
            UpdateVisionMap();
        }
        public class CommandInfo
        {
            public string Usage;       // "spawn [name] [x] [y]"
            public string Description; // "Создать врага"
            public CommandInfo(string usage, string desc) { Usage = usage; Description = desc; }
        }

        public static Dictionary<string, CommandInfo> Commands = new()
        {
            { "spawn", new CommandInfo("spawn [name] [x] [y]", "Создать врага в точке") },
            { "give",  new CommandInfo("give [item] [slot]",   "Выдать предмет в ячейку") },
            { "hp",    new CommandInfo("hp [value]",            "Установить здоровье игрока") },
            { "help",  new CommandInfo("help",                  "Показать все команды") },
        };

        public static Room GetCurrentRoom() => World[CurRoomX, CurRoomY];

        public static bool HandleInput()
        {
            if (CurrentState == GameState.Scores)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    CurrentState = GameState.MainMenu;
                if (Raylib.IsKeyPressed(KeyboardKey.Delete))
                {
                    ScoreSystem.Reset();
                    Game.Log("Рекорды сброшены.");
                }
                return false;
            }
            if (CurrentState == GameState.MainMenu)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.One)) StartNewGame();
                if (Raylib.IsKeyPressed(KeyboardKey.Two) && SaveSystem.SaveExists()) ContinueGame();
                if (Raylib.IsKeyPressed(KeyboardKey.Three)) OpenTutorial();
                if (Raylib.IsKeyPressed(KeyboardKey.Four)) OpenScores();
                if (Raylib.IsKeyPressed(KeyboardKey.Five)) QuitGame();
                return false;
            }

            if (CurrentState == GameState.LearnSpell)
            {
                if (PendingSpell == null) { CurrentState = GameState.Playing; return false; }

                // 1-3 заменить слот
                KeyboardKey[] keys = { KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three };
                for (int i = 0; i < 3 && i < Player.SpellBook.Count; i++)
                {
                    if (Raylib.IsKeyPressed(keys[i]))
                    {
                        Game.Log($"{Player.SpellBook[i].Name} заменено на {PendingSpell.Name}");
                        Player.SpellBook[i] = PendingSpell;
                        PendingSpell = null;
                        CurrentState = GameState.Playing;
                        return false;
                    }
                }

                // Esc — не заменять
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    Game.Log($"{PendingSpell.Name} не изучено.");
                    PendingSpell = null;
                    CurrentState = GameState.Playing;
                }
                return false;
            }
            // === ОБУЧЕНИЕ ===
            if (CurrentState == GameState.Tutorial)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Backspace))
                    CurrentState = GameState.MainMenu;
                return false;
            }
            if (CurrentState == GameState.Targeting)
            {
                if (ValidTargets.Count == 0) // цели исчезли
                {
                    CancelTargeting();
                    return false;
                }

                // Tab — следующая цель
                if (Raylib.IsKeyPressed(KeyboardKey.Tab))
                    TargetIndex = (TargetIndex + 1) % ValidTargets.Count;

                // Enter / тот же слот — каст
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    Enemy chosenTarget = ValidTargets[TargetIndex];
                    Spell spell = SelectedSpell!;
                    CancelTargeting();
                    CastSpell(spell, chosenTarget);
                    return true; // ход проходит
                }

                // Esc — отмена
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    CancelTargeting();

                return false;
            }
            if (CurrentState == GameState.LevelUp)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.One)) // СИЛА
                {
                    Player.Damage += 5;
                    Game.Log("Ваша сила растет! Урон увеличен.");
                    ApplyLevelUp();
                    return false;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Two)) // ВЫНОСЛИВОСТЬ
                {
                    Player.MaxHealth += 20;
                    Player.Health = Player.MaxHealth; // Полный отхил при прокачке HP
                    Game.Log("Вы стали крепче! HP увеличено и восстановлено.");
                    ApplyLevelUp();
                    return false;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Three)) // ЛОВКОСТЬ
                {
                    Player.Speed += 15;
                    Game.Log("Вы стали быстрее!");
                    ApplyLevelUp();
                    return false;
                }
                return false; // Пока мы в меню LevelUp, обычный ввод заблокирован
            }
            if (CurrentState == GameState.GameOver || CurrentState == GameState.Victory)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    Initialize();        // новая игра
                    return false;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    CurrentState = GameState.MainMenu;
                    return false;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.T))   // ← показать рекорды
                {
                    CurrentState = GameState.Scores;
                    return false;
                }
                return false;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Grave))
            {
                CurrentState = (CurrentState == GameState.Console) ? GameState.Playing : GameState.Console;
                CurrentCommand = "";
                return false;
            }
            if (CurrentState == GameState.Console)
            {
                HandleConsoleInput(); // Теперь этот метод существует!
                return false;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.M))
            {
                CurrentState = (CurrentState == GameState.Map) ? GameState.Playing : GameState.Map;
                return false;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                if (Player.Health > 0) // мёртвого не сохраняем
                {
                    SaveSystem.Save();
                    Game.Log("Игра сохранена.");
                }
                CurrentState = GameState.MainMenu;
                return false;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.R)) // Рестарт
            {
                ResetGame();
                return false;
            }

            


            if (CurrentState != GameState.Playing) return false;
            for (int i = 0; i < Settings.MaxInventorySize; i++)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.One + i) && i < Player.Inventory.Count)
                {
                    UseItem(i);
                    return true;
                }
            }

            int nX = Player.X;
            int nY = Player.Y;
            bool inputDetected = false;

            int dirX = 0, dirY = 0;
            if (Raylib.IsKeyDown(KeyboardKey.W)) dirY = -1;
            else if (Raylib.IsKeyDown(KeyboardKey.S)) dirY = 1;
            else if (Raylib.IsKeyDown(KeyboardKey.A)) dirX = -1;
            else if (Raylib.IsKeyDown(KeyboardKey.D)) dirX = 1;
            
            if (dirX < 0) Player.FacingLeft = false;
            else if (dirX > 0) Player.FacingLeft = true;
            
            if (Raylib.IsKeyPressed(KeyboardKey.G))
            {
                Room currentRoom = GetCurrentRoom();
                Item? item = currentRoom.Items.Find(i => i.X == Player.X && i.Y == Player.Y);
                if (item != null)
                {
                    if (Player.Inventory.Count < Settings.MaxInventorySize)
                    {
                        Player.Inventory.Add(item);
                        currentRoom.Items.Remove(item);
                        Game.Log($"Вы подобрали: {item.Name}");
                        ItemsCollected++;
                    }
                    else
                    {
                        Game.Log("Инвентарь полон! (5/5)");
                    }
                }
                else
                {
                    Game.Log("Здесь нечего подбирать.");
                }
                return false;
            }
            KeyboardKey[] spellKeys = { KeyboardKey.Z, KeyboardKey.X, KeyboardKey.C };
            for (int i = 0; i < spellKeys.Length && i < Player.SpellBook.Count; i++)
            {
                if (Raylib.IsKeyPressed(spellKeys[i]))
                {
                    StartTargeting(Player.SpellBook[i]);
                    return false;
                }
            }

            if (dirX != 0 || dirY != 0)
            {
                // Первое нажатие — двигаемся сразу
                bool justPressed = Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.S)
                                || Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.D);

                if (justPressed)
                {
                    inputDetected = true;
                    firstMove = true;
                    moveTimer = 0f;
                }
                else
                {
                    // Кнопка удерживается — отсчитываем таймер
                    moveTimer += Raylib.GetFrameTime();
                    float delay = firstMove ? FirstMoveDelay : MoveDelay;
                    if (moveTimer >= delay)
                    {
                        inputDetected = true;
                        moveTimer = 0f;
                        firstMove = false;
                    }
                }

                nX = Player.X + dirX;
                nY = Player.Y + dirY;
            }
            else
            {
                // Ни одна кнопка не зажата — сбрасываем
                firstMove = true;
                moveTimer = 0f;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.E))
            {
                Room currentRoom = GetCurrentRoom();
                if (currentRoom.Tiles[Player.X, Player.Y] == '>')
                {
                    NextFloor();
                    return true; 
                }
            }

            if (!inputDetected) return false;

            if (nX < 0 && CurRoomX > 0)
            {
                CurRoomX--;
                Player.X = Settings.MapWidth - 2;
                Player.Y = nY;
                PlacePlayerSafely();
                GetCurrentRoom().Visited = true;
                UpdateVisionMap();
                return true;
            }
            if (nX >= Settings.MapWidth && CurRoomX < Settings.WorldSize - 1)
            {
                CurRoomX++;
                Player.X = 1;
                Player.Y = nY;
                PlacePlayerSafely();
                GetCurrentRoom().Visited = true;
                UpdateVisionMap();
                return true;
            }
            if (nY < 0 && CurRoomY > 0)
            {
                CurRoomY--;
                Player.X = nX;
                Player.Y = Settings.MapHeight - 2;
                PlacePlayerSafely();
                GetCurrentRoom().Visited = true;
                UpdateVisionMap();
                return true;
            }
            if (nY >= Settings.MapHeight && CurRoomY < Settings.WorldSize - 1)
            {
                CurRoomY++;
                Player.X = nX;
                Player.Y = 1;
                PlacePlayerSafely();
                GetCurrentRoom().Visited = true;
                UpdateVisionMap();
                return true;
            }
            Room room = GetCurrentRoom();

            Enemy? target = room.Enemies.Find(e => e.X == nX && e.Y == nY);
            if (target != null)
            {
                target.Health -= Player.Damage;
                int displayHp = Math.Max(0, target.Health);
                Game.Log($"Вы нанесли {Player.Damage} урона {target.Name}! (HP: {displayHp})");
                Player.Energy -= Settings.ActionCost;
                return true;
            }
            if (nX >= 0 && nX < Settings.MapWidth && nY >= 0 && nY < Settings.MapHeight && room.Tiles[nX, nY] != '#')
            {
                Player.X = nX;
                Player.Y = nY;
                Player.Energy -= Settings.ActionCost;
                return true;
            }

            return false;
        }
        private static void HandleConsoleInput()
        {
            int keyChar = Raylib.GetCharPressed();
            while (keyChar > 0)
            {
                if (keyChar >= 32 && keyChar <= 126) CurrentCommand += (char)keyChar;
                keyChar = Raylib.GetCharPressed();
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && CurrentCommand.Length > 0)
                CurrentCommand = CurrentCommand.Substring(0, CurrentCommand.Length - 1);

            // === TAB-автодополнение ===
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                HandleTabCompletion();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                ExecuteCommand(CurrentCommand);
                CurrentCommand = "";
                CurrentState = GameState.Playing;
            }
        }
        private static void HandleTabCompletion()
        {
            string[] parts = CurrentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool endsWithSpace = CurrentCommand.EndsWith(" ");

            // Случай 1: пустая строка — показываем все команды
            if (parts.Length == 0)
            {
                Game.Log("Команды: " + string.Join(", ", Commands.Keys));
                return;
            }

            // Случай 2: дополняем имя команды (печатаем первое слово, пробела ещё нет)
            if (parts.Length == 1 && !endsWithSpace)
            {
                var matches = Commands.Keys.Where(k => k.StartsWith(parts[0].ToLower())).ToList();
                CompleteOrList(matches, parts[0]);
                return;
            }

            // Случай 3: дополняем аргумент (второе слово) для команд give/spawn
            string action = parts[0].ToLower();
            if (action == "give" || action == "spawn")
            {
                // Что уже напечатано во втором слове
                string prefix = (parts.Length >= 2 && !endsWithSpace) ? parts[1] : "";

                List<string> candidates = action == "give"
                    ? ItemLibrary.GetAllKeys()
                    : EnemyLibrary.GetAllKeys();

                var matches = candidates.Where(c => c.StartsWith(prefix.ToLower())).ToList();

                if (matches.Count == 1)
                {
                    // Дополняем: команда + единственное совпадение
                    CurrentCommand = $"{action} {matches[0]} ";
                }
                else if (matches.Count > 1)
                {
                    Game.Log("Варианты: " + string.Join(", ", matches));
                    // Дополняем до общего префикса
                    string common = LongestCommonPrefix(matches);
                    if (common.Length > prefix.Length)
                        CurrentCommand = $"{action} {common}";
                }
                else
                {
                    Game.Log("Нет совпадений.");
                }
                return;
            }

            // Для остальных команд показываем формат
            if (Commands.TryGetValue(action, out var info))
                Game.Log($"Формат: {info.Usage}");
        }

        // Дополнить до единственного совпадения или вывести список
        private static void CompleteOrList(List<string> matches, string current)
        {
            if (matches.Count == 1)
            {
                CurrentCommand = matches[0] + " ";
            }
            else if (matches.Count > 1)
            {
                Game.Log("Команды: " + string.Join(", ", matches));
                string common = LongestCommonPrefix(matches);
                if (common.Length > current.Length)
                    CurrentCommand = common;
            }
            else
            {
                Game.Log("Нет такой команды. Tab — список всех.");
            }
        }

        // Самый длинный общий префикс списка строк
        private static string LongestCommonPrefix(List<string> items)
        {
            if (items.Count == 0) return "";
            string prefix = items[0];
            foreach (string s in items)
            {
                while (!s.StartsWith(prefix))
                {
                    prefix = prefix.Substring(0, prefix.Length - 1);
                    if (prefix == "") return "";
                }
            }
            return prefix;
        }

        public static void Update()
        {
            if (CurrentState != GameState.Playing) return;
            if (Player.Health <= 0)
            {
                Player.Health = 0;
                CurrentState = GameState.GameOver;
                SaveSystem.DeleteSave();
                EndRun();
                return;
            }
            Room room = GetCurrentRoom();
            bool enemyNearby = room.Enemies.Any(e =>
                Vector2.Distance(new Vector2(e.X, e.Y), new Vector2(Player.X, Player.Y)) <= 5);

            while (Player.Energy < Settings.ActionCost)
            {
                if (Player.Health <= 0) break;
                Player.Energy += Player.Speed / 10;

                foreach (var enemy in room.Enemies)
                {
                    enemy.Energy += enemy.Speed / 10;
                    if (enemy.Energy >= Settings.ActionCost)
                    {
                        PerformAI(enemy, room);
                        EffectLibrary.TickEffects(enemy); // ← тик эффектов врага в его ход
                        enemy.Energy -= Settings.ActionCost;
                    }
                    if (Player.Health <= 0) break;
                }
                if (Player.Health <= 0)
                {
                    Player.Health = 0;
                    CurrentState = GameState.GameOver;
                    SaveSystem.DeleteSave();
                    EndRun();
                    return;
                }
            }

            // Игрок накопил энергию на ход → тикаем его эффекты
            EffectLibrary.TickEffects(Player);
            if (!enemyNearby)
            {
                Player.ManaRegenAccumulator += Settings.ManaRegenPerTurn;
                int manaGain = (int)Player.ManaRegenAccumulator;
                if (manaGain > 0)
                {
                    Player.Mana = Math.Min(Player.MaxMana, Player.Mana + manaGain);
                    Player.ManaRegenAccumulator -= manaGain;
                }
            }

            if (Player.Health <= 0)
            {
                Player.Health = 0;
                CurrentState = GameState.GameOver;
                SaveSystem.DeleteSave();
                EndRun();
                return;
            }

            foreach (var e in room.Enemies)
            {
                if (e.Health <= 0)
                {
                    Player.Experience += e.ExperienceReward;
                    Game.Log($"{e.Name} повержен! +{e.ExperienceReward} XP");

                    if (e.IsBoss)
                    {
                        if (DungeonLevel >= 20)
                        {
                            // Финальный босс — победа!
                            CurrentState = GameState.Victory;
                            SaveSystem.DeleteSave();
                            EndRun();
                        }
                        else
                        {
                            // Мини-босс — лестница на месте смерти
                            room.Tiles[e.X, e.Y] = '>';
                            Game.Log("Путь вниз открыт!");
                        }
                    }
                    EnemiesKilled++;
                }
            }
            room.Enemies.RemoveAll(e => e.Health <= 0);

            if (Player.Experience >= Player.ExperienceToNextLevel)
            {
                CurrentState = GameState.LevelUp;
            }
            UpdateVisionMap();
        }

        // ВОЗВРАЩАЕМ УМНЫЙ ИИ
        private static void PerformAI(Entity e, Room r)
        {
            if (Player.Health <= 0) return;
            Enemy enemy = (Enemy)e;

            float dist = Vector2.Distance(new Vector2(e.X, e.Y), new Vector2(Player.X, Player.Y));
            bool canSeePlayer = dist <= e.Vision && HasLineOfSight(e.X, e.Y, Player.X, Player.Y, r);

            int dx = 0, dy = 0;

            if (canSeePlayer)
            {
                enemy.LastKnownPlayerX = Player.X;
                enemy.LastKnownPlayerY = Player.Y;
                enemy.IsSearching = true;
                
                int wantDx = 0, wantDy = 0;
                if (e.X < Player.X) wantDx = 1; else if (e.X > Player.X) wantDx = -1;
                if (e.Y < Player.Y) wantDy = 1; else if (e.Y > Player.Y) wantDy = -1;
                if (wantDx != 0 && wantDy != 0)
                {
                    if (rnd.Next(2) == 0) dx = wantDx;
                    else dy = wantDy;
                }
                else
                {
                    dx = wantDx;
                    dy = wantDy;
                }
            }
            else if (enemy.IsSearching)
            {
                // Не видим игрока, но идём к последней известной позиции
                int wantDx = 0, wantDy = 0;
                if (e.X < enemy.LastKnownPlayerX) wantDx = 1; 
                else if (e.X > enemy.LastKnownPlayerX) wantDx = -1;
                if (e.Y < enemy.LastKnownPlayerY) wantDy = 1; 
                else if (e.Y > enemy.LastKnownPlayerY) wantDy = -1;

                if (wantDx != 0 && wantDy != 0)
                {
                    if (rnd.Next(2) == 0) dx = wantDx;
                    else dy = wantDy;
                }
                else { dx = wantDx; dy = wantDy; }

                // Достигли последней известной позиции — прекращаем поиск
                if (e.X == enemy.LastKnownPlayerX && e.Y == enemy.LastKnownPlayerY)
                {
                    enemy.IsSearching = false;
                    enemy.LastKnownPlayerX = -1;
                    enemy.LastKnownPlayerY = -1;
                }
            }
            else
            {
                // Случайное блуждание (шанс 30%)
                if (rnd.Next(0, 10) < 3)
                {
                    if (rnd.Next(0, 2) == 0) dx = rnd.Next(-1, 2);
                    else dy = rnd.Next(-1, 2);
                }
            }
            if (dx < 0) e.FacingLeft = false;
            else if (dx > 0) e.FacingLeft = true;

            int nX = e.X + dx;
            int nY = e.Y + dy;

            // Атака игрока
            if (nX == Player.X && nY == Player.Y)
            {
                Player.Health -= e.Damage;
                Game.Log($"{e.Name} наносит вам {e.Damage} урона!");
            }
            // Движение, если клетка свободна
            else if (nX >= 0 && nX < Settings.MapWidth && nY >= 0 && nY < Settings.MapHeight && r.Tiles[nX, nY] != '#')
            {
                bool isOccupiedByEnemy = r.Enemies.Exists(other => other != e && other.X == nX && other.Y == nY);
                bool isOccupiedByPlayer = (nX == Player.X && nY == Player.Y);
                
                if (!isOccupiedByEnemy && !isOccupiedByPlayer)
                {
                    e.X = nX;
                    e.Y = nY;
                }
            }
        }

        // ВОЗВРАЩАЕМ АЛГОРИТМ ПРЯМОЙ ВИДИМОСТИ (Bresenham)
        public static bool HasLineOfSight(int x0, int y0, int x1, int y1, Room r)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 == x1 && y0 == y1) return true;
                if (x0 < 0 || x0 >= Settings.MapWidth || y0 < 0 || y0 >= Settings.MapHeight || r.Tiles[x0, y0] == '#') return false;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        // ОБНОВЛЕННАЯ КАРТА ЗРЕНИЯ (теперь учитывает стены)
        public static void UpdateVisionMap()
        {
            Room room = GetCurrentRoom();

            for (int y = 0; y < Settings.MapHeight; y++)
            {
                for (int x = 0; x < Settings.MapWidth; x++)
                {
                    room.VisibleNow[x, y] = false;

                    float d = Vector2.Distance(new Vector2(Player.X, Player.Y), new Vector2(x, y));
                    if (d <= Player.Vision + 0.5f && HasLineOfSight(Player.X, Player.Y, x, y, room))
                    {
                        room.VisibleNow[x, y] = true;
                        room.Explored[x, y] = true;
                    }
                }
            }
        }

        public static void Log(string text)
        {
            Messages.Add(text);
            if (Messages.Count > Settings.MaxLogMessages)
            {
                Messages.RemoveAt(0); // Удаляем самое старое сообщение
            }
        }
        public static void ResetGame()
        {
            Initialize();
        }

        public static void ExecuteCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;

            string[] parts = cmd.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string action = parts[0].ToLower();

            switch (action)
            {
                case "help":
                    Game.Log("=== Команды ===");
                    foreach (var c in Commands)
                        Game.Log($"{c.Value.Usage} — {c.Value.Description}");
                    break;
                case "spawn":
                    // Формат: spawn [name] [x] [y]
                    if (parts.Length == 4)
                    {
                        string name = parts[1];
                        if (int.TryParse(parts[2], out int x) && int.TryParse(parts[3], out int y))
                        {
                            // Проверка границ карты
                            if (x >= 0 && x < Settings.MapWidth && y >= 0 && y < Settings.MapHeight)
                            {
                                var enemy = EnemyLibrary.CreateEnemy(name, x, y);
                                GetCurrentRoom().Enemies.Add(enemy);
                                Game.Log($"Спавн: {name} на {x},{y}");
                            }
                        }
                    }
                    else
                    {
                        Game.Log("Ошибка! Используйте: spawn [name] [x] [y]");
                    }
                    break;

                case "hp":
                    // Бонусная команда: hp [value]
                    if (parts.Length == 2 && int.TryParse(parts[1], out int val))
                    {
                        Player.Health = val;
                        Game.Log($"HP игрока установлено в {val}");
                    }
                    break;
                case "give":
                if (parts.Length == 3)
                {
                    string itemKey = parts[1];
                    if (int.TryParse(parts[2], out int slot) && slot >= 1 && slot <= Settings.MaxInventorySize)
                    {
                        // Проверяем что предмет существует в библиотеке
                        if (ItemLibrary.GetTemplate(itemKey) == null)
                        {
                            Game.Log($"Предмет '{itemKey}' не найден.");
                            break;
                        }
                        Item newItem = ItemLibrary.CreateItem(itemKey, Player.X, Player.Y);

                        int index = slot - 1; // ячейки 1-5 → индексы 0-4

                        if (index < Player.Inventory.Count)
                        {
                            // Ячейка занята — заменяем
                            Player.Inventory[index] = newItem;
                            Game.Log($"Ячейка {slot}: заменён на {newItem.Name}");
                        }
                        else if (Player.Inventory.Count < Settings.MaxInventorySize)
                        {
                            // Добавляем в конец (ближайшая свободная ячейка)
                            Player.Inventory.Add(newItem);
                            Game.Log($"Выдан {newItem.Name} в ячейку {Player.Inventory.Count}");
                        }
                        else
                        {
                            Game.Log("Инвентарь полон!");
                        }
                    }
                    else
                    {
                        Game.Log($"Слот должен быть 1-{Settings.MaxInventorySize}");
                    }
                }
                
                else
                {
                    Game.Log("Ошибка! Используйте: give [item_key] [slot]");
                }
                break;

                default:
                    Game.Log($"Неизвестная команда: {action}");
                    break;
            }
            UpdateVisionMap();
        }
        public static void NextFloor()
        {
            DungeonLevel++;
            
            // Сохраняем текущее здоровье игрока
            int currentHP = Player.Health;

            // Генерируем новый мир
            MapGenerator.GenerateWorld(World, Settings.WorldSize);
            
            // Сбрасываем координаты и состояние для нового этажа
            CurRoomX = Settings.WorldSize / 2;
            CurRoomY = Settings.WorldSize / 2;
            
            Player.X = Settings.MapWidth / 2;
            Player.Y = Settings.MapHeight / 2;
            Player.Health = currentHP; // Возвращаем здоровье
            Player.Energy = Settings.ActionCost;

            GetCurrentRoom().Visited = true;
            UpdateVisionMap();
            
            Game.Log($"Вы спустились на этаж {DungeonLevel}...");
        }
        private static void ApplyLevelUp()
        {
            Player.Level++;
            Player.Experience -= Player.ExperienceToNextLevel;
            Player.ExperienceToNextLevel = (int)(Player.ExperienceToNextLevel * 1.5f);

            // Сначала проверяем заклинание
            bool offeringSpell = false;
            if (Player.Level % 3 == 0)
            {
                string[] levelSpells = { "firebolt", "poison_dart", "heal_self" };
                int idx = (Player.Level / 3 - 1) % levelSpells.Length;
                Spell newSpell = SpellLibrary.CreateSpell(levelSpells[idx]);
                OfferSpell(newSpell);
                offeringSpell = (CurrentState == GameState.LearnSpell);
            }

            // Состояние Playing ставим только если не открыли экран заклинания
            // и не осталось опыта на ещё один уровень
            if (!offeringSpell && Player.Experience < Player.ExperienceToNextLevel)
                CurrentState = GameState.Playing;
        }
        private static void UseItem(int index)
        {
            Item item = Player.Inventory[index];
            Room room = GetCurrentRoom();

            // Страница заклинания — изучаем
            if (!string.IsNullOrEmpty(item.TeachesSpell))
            {
                string spellKey = item.TeachesSpell;

                // "random" — выбираем случайное доступное заклинание
                if (spellKey == "random")
                    spellKey = SpellLibrary.GetRandomFindableSpell(DungeonLevel);

                if (string.IsNullOrEmpty(spellKey))
                {
                    Game.Log("Свиток рассыпался в пыль... (нет доступных заклинаний)");
                    Player.Inventory.RemoveAt(index);
                    return;
                }

                Spell spell = SpellLibrary.CreateSpell(spellKey);
                Player.Inventory.RemoveAt(index);
                OfferSpell(spell);
                return;
            }

            // Обычный предмет — эффект
            Game.Log($"Вы использовали: {item.Name}");
            EffectLibrary.Apply(item.Effect, Player, room);
            Player.Inventory.RemoveAt(index);
        }
        private static void PlacePlayerSafely()
        {
            Room room = GetCurrentRoom();
            // Если текущая позиция свободна — оставляем
            if (room.Tiles[Player.X, Player.Y] != '#' &&
                !room.Enemies.Exists(e => e.X == Player.X && e.Y == Player.Y))
                return;

            // Иначе ищем ближайшую свободную клетку по спирали
            for (int radius = 1; radius < Settings.MapWidth; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                        int tx = Player.X + dx;
                        int ty = Player.Y + dy;
                        if (tx < 1 || tx >= Settings.MapWidth - 1 || ty < 1 || ty >= Settings.MapHeight - 1) continue;
                        if (room.Tiles[tx, ty] != '#' &&
                            !room.Enemies.Exists(e => e.X == tx && e.Y == ty))
                        {
                            Player.X = tx;
                            Player.Y = ty;
                            return;
                        }
                    }
                }
            }
        }
        private static void StartTargeting(Spell spell)
        {
            // Проверка маны
            if (Player.Mana < spell.ManaCost)
            {
                Game.Log($"Недостаточно маны для {spell.Name} ({Player.Mana}/{spell.ManaCost})");
                return;
            }

            // Заклинание на себя (Range 0) — кастуем сразу без прицеливания
            if (spell.Range == 0)
            {
                CastSpell(spell, Player);
                return;
            }

            // Собираем видимых врагов в радиусе
            Room room = GetCurrentRoom();
            ValidTargets = room.Enemies.Where(e =>
            {
                float d = Vector2.Distance(new Vector2(e.X, e.Y), new Vector2(Player.X, Player.Y));
                return d <= spell.Range
                    && room.VisibleNow[e.X, e.Y]
                    && HasLineOfSight(Player.X, Player.Y, e.X, e.Y, room);
            }).ToList();

            if (ValidTargets.Count == 0)
            {
                Game.Log("Нет целей в радиусе.");
                return;
            }

            SelectedSpell = spell;
            TargetIndex = 0;
            CurrentState = GameState.Targeting;
        }
        private static void CancelTargeting()
        {
            CurrentState = GameState.Playing;
            SelectedSpell = null;
            ValidTargets.Clear();
            TargetIndex = 0;
        }

        private static void CastSpell(Spell spell, Entity target)
        {
            if (Player.Mana < spell.ManaCost)
            {
                Game.Log("Недостаточно маны!");
                return;
            }
            Player.Mana -= spell.ManaCost;
            Room room = GetCurrentRoom();

            Game.Log($"Вы кастуете {spell.Name}!");

            // Прямой урон
            if (spell.DirectDamage > 0)
            {
                target.Health -= spell.DirectDamage;
                int displayHp = Math.Max(0, target.Health);
                Game.Log($"{spell.Name}: {spell.DirectDamage} урона {target.Name} (HP: {displayHp})");
            }

            // Накладываемый эффект (горение, яд, лечение)
            if (spell.Effect != "none")
                EffectLibrary.Apply(spell.Effect, target, room);
            // Проверка смерти от заклинания
            if (target is Enemy enemy && enemy.Health <= 0)
            {
                Player.Experience += enemy.ExperienceReward;
                Game.Log($"{enemy.Name} повержен! +{enemy.ExperienceReward} XP");

                if (enemy.IsBoss)
                {
                    if (DungeonLevel >= 20)
                    {
                        CurrentState = GameState.Victory;
                        SaveSystem.DeleteSave();
                        EndRun();
                    }
                    else
                    {
                        room.Tiles[enemy.X, enemy.Y] = '>';
                        Game.Log("Путь вниз открыт!");
                    }
                }
                EnemiesKilled++;

                room.Enemies.Remove(enemy);
                if (Player.Experience >= Player.ExperienceToNextLevel && CurrentState == GameState.Playing)
                    CurrentState = GameState.LevelUp;
            }
        }
        public static void OfferSpell(Spell spell)
        {
            // Уже знаем это заклинание?
            if (Player.SpellBook.Any(s => s.Key == spell.Key))
            {
                Game.Log($"{spell.Name} уже изучено.");
                return;
            }

            // Есть свободный слот — учим сразу
            if (Player.SpellBook.Count < 3)
            {
                Player.SpellBook.Add(spell);
                Game.Log($"Изучено заклинание: {spell.Name}!");
                return;
            }

            // Слоты заняты — открываем экран замены
            PendingSpell = spell;
            CurrentState = GameState.LearnSpell;
        }
        public static void InitLibraries()
        {
            EnemyLibrary.Initialize();
            ItemLibrary.Initialize();
            EffectLibrary.Initialize();
            SpellLibrary.Initialize();
        }
        public static void EndRun()
        {
            RunElapsedTime = (float)Raylib.GetTime() - RunStartTime;
            ScoreSystem.RecordRun(DungeonLevel, EnemiesKilled, Player.Level, RunElapsedTime);
        }
        public static void StartNewGame()
        {
            Initialize();
            SaveSystem.DeleteSave();
        }
        public static void ContinueGame()
        {
            if (!SaveSystem.Load()) Game.Log("Не удалось загрузить.");
        }
        public static void OpenTutorial() => CurrentState = GameState.Tutorial;
        public static void OpenScores() => CurrentState = GameState.Scores;
        public static void QuitGame() => ShouldExit = true;
    }
}