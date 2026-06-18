using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RogueLiteGame
{
    class Program
    {
        static Font gameFont;
        static Font logFont;
        static List<string> WrapText(string text, Font font, float fontSize, float maxWidth)
            {
                List<string> lines = new List<string>();
                string[] words = text.Split(' ');
                string current = "";

                foreach (string word in words)
                {
                    string test = current.Length == 0 ? word : current + " " + word;
                    float width = Raylib.MeasureTextEx(font, test, fontSize, 1).X;
                    if (width > maxWidth && current.Length > 0)
                    {
                        lines.Add(current);
                        current = word;
                    }
                    else current = test;
                }
                if (current.Length > 0) lines.Add(current);
                return lines;
            }

        static void Main()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(Settings.ScreenWidth, Settings.ScreenHeight, "DungeonCrawler");
            Raylib.SetExitKey(KeyboardKey.Null);
            Raylib.InitAudioDevice();
            Raylib.SetTargetFPS(60);

            Music bgMusic = Raylib.LoadMusicStream("assets/music/dungeon.ogg");
            Raylib.PlayMusicStream(bgMusic);
            Raylib.SetMusicVolume(bgMusic, 0.5f); // громкость 0.0 - 1.0
            Sound hitSound = Raylib.LoadSound("assets/sounds/hit.wav");

            RenderTexture2D target = Raylib.LoadRenderTexture(Settings.ScreenWidth, Settings.ScreenHeight);
            Raylib.SetTextureFilter(target.Texture, TextureFilter.Point);

            // Загрузка шрифтов
            int[] glyphs = CreateGlyphs();
            gameFont = Raylib.LoadFontEx("assets/fonts/consolas.ttf", Settings.CellHeight, glyphs, glyphs.Length);
            logFont  = Raylib.LoadFontEx("assets/fonts/arial.ttf",    Settings.CellHeight, glyphs, glyphs.Length);

            TextureLibrary.LoadAll();
            Game.InitLibraries();

            while (!Raylib.WindowShouldClose() && !Game.ShouldExit)
            {
                Raylib.UpdateMusicStream(bgMusic); 
                if (Game.HandleInput()) Game.Update();

                Raylib.BeginTextureMode(target);
                Raylib.ClearBackground(Color.Black);
                DrawGame();
                Raylib.EndTextureMode();

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.DarkGray);
                
                float scale = Math.Min((float)Raylib.GetScreenWidth() / Settings.ScreenWidth, (float)Raylib.GetScreenHeight() / Settings.ScreenHeight);
                Rectangle source = new Rectangle(0, 0, target.Texture.Width, -target.Texture.Height);
                Rectangle dest = new Rectangle(
                    (Raylib.GetScreenWidth() - Settings.ScreenWidth * scale) / 2,
                    (Raylib.GetScreenHeight() - Settings.ScreenHeight * scale) / 2,
                    Settings.ScreenWidth * scale, Settings.ScreenHeight * scale);

                Raylib.DrawTexturePro(target.Texture, source, dest, Vector2.Zero, 0, Color.White);
                Raylib.EndDrawing();
            }

            Raylib.UnloadFont(gameFont);
            Raylib.UnloadFont(logFont);
            Raylib.UnloadRenderTexture(target);
            Raylib.UnloadMusicStream(bgMusic);
            Raylib.CloseAudioDevice();
            TextureLibrary.UnloadAll();
            Raylib.CloseWindow();
        }

        static int[] CreateGlyphs()
        {
            List<int> g = new List<int>();
            for (int i = 32; i < 127; i++) g.Add(i);
            for (int i = 1024; i < 1105; i++) g.Add(i);
            return g.ToArray();
        }

        static void DrawGame()
        {
            if (Game.CurrentState == GameState.Victory)
            {
                Raylib.DrawRectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight, new Color(0, 0, 0, 230));
                string t = "ПОБЕДА!";
                Vector2 sz = Raylib.MeasureTextEx(gameFont, t, 60, 2);
                Raylib.DrawTextEx(gameFont, t, new Vector2((Settings.ScreenWidth - sz.X) / 2f, 200), 60, 2, Color.Gold);
                string sub = "Владыка подземелья повержен. [R] - новая игра, [Esc] - меню";
                Vector2 sz2 = Raylib.MeasureTextEx(logFont, sub, 22, 1);
                Raylib.DrawTextEx(logFont, sub, new Vector2((Settings.ScreenWidth - sz2.X) / 2f, 300), 22, 1, Color.White);
            }
            if (Game.CurrentState == GameState.MainMenu)
            {
                Raylib.ClearBackground(Color.Black);
                string title = "DUNGEON CRAWLER";
                float titleFontSize = 50;
                Vector2 titleSize = Raylib.MeasureTextEx(logFont, title, titleFontSize, 2);
                float titleX = (Settings.ScreenWidth - titleSize.X) / 2f;
                Raylib.DrawTextEx(gameFont, title, new Vector2(titleX, 120), titleFontSize, 2, Color.Gold);

                Raylib.DrawTextEx(logFont, $"v{Settings.Version}",
                    new Vector2(Settings.ScreenWidth / 2 - 20, 180), 18, 1, Color.Gray);

                List<string> options = new() { "[1] Новая игра" };
                List<Color> colors = new() { Color.SkyBlue };
                if (SaveSystem.SaveExists())
                {
                    options.Add("[2] Продолжить");
                    colors.Add(Color.Lime);
                }
                options.Add("[3] Как играть");
                colors.Add(Color.Yellow);
                options.Add("[4] Выход");
                colors.Add(Color.Orange);

                for (int i = 0; i < options.Count; i++)
                {
                    Vector2 sz = Raylib.MeasureTextEx(logFont, options[i], 28, 1);
                    Raylib.DrawTextEx(logFont, options[i],
                        new Vector2((Settings.ScreenWidth - sz.X) / 2f, 280 + i * 50), 28, 1, colors[i]);
                }
                return; // больше ничего не рисуем
            }

            // === ОБУЧЕНИЕ ===
            if (Game.CurrentState == GameState.Tutorial)
            {
                Raylib.ClearBackground(Color.Black);
                Raylib.DrawTextEx(logFont, "КАК ИГРАТЬ", new Vector2(60, 40), 36, 2, Color.Gold);

                string[] lines = {
                    "WASD - движение (можно зажимать)",
                    "E / Enter - спуститься по лестнице (>)",
                    "G - подобрать предмет",
                    "1-5 - использовать предмет из инвентаря",
                    "M - карта мира",
                    "R - перезапустить игру",
                    "Esc - вернуться в меню",
                    "Z / X / C - применить заклинание (1/2/3)",
                    "В прицеле: Tab - выбрать цель, Enter - каст",
                    "",
                    "Цель: спускайтесь глубже, убивайте врагов,",
                    "набирайте опыт и прокачивайте героя.",
                };
                for (int i = 0; i < lines.Length; i++)
                    Raylib.DrawTextEx(logFont, lines[i], new Vector2(60, 110 + i * 32), 22, 1, Color.White);

                Raylib.DrawTextEx(logFont, "[Esc] Назад", new Vector2(60, Settings.ScreenHeight - 50), 24, 1, Color.Orange);
                return;
            }
            if (Game.CurrentState == GameState.Map)
            {
                DrawWorldMap();
                return; 
            }
            Room room = Game.GetCurrentRoom();
            for (int y = 0; y < Settings.MapHeight; y++)
                for (int x = 0; x < Settings.MapWidth; x++)
                {
                    if (!room.Explored[x, y]) continue; // не исследовано — чёрный экран

                    char tile = room.Tiles[x, y];
                    string biome = Game.CurrentBiome;
                    string key = tile switch
                    {
                        '#' => $"wall_{biome}",
                        '>' => "stairs",
                        _   => $"floor_{biome}"
                    };
                    DrawTile(key, x, y);

                    // Исследовано, но сейчас не видно — затемняем
                    if (!room.VisibleNow[x, y])
                        Raylib.DrawRectangle(x * Settings.CellWidth, y * Settings.CellHeight,
                            Settings.CellWidth, Settings.CellHeight, new Color(0, 0, 0, 160));
                }
            if (Game.CurrentState == GameState.Playing)
            {
                Item? itemHere = room.Items.Find(it => it.X == Game.Player.X && it.Y == Game.Player.Y);

                bool enemyNearby = room.Enemies.Any(e =>
                    Math.Max(Math.Abs(e.X - Game.Player.X), Math.Abs(e.Y - Game.Player.Y)) <= 5);

                if (itemHere != null && !enemyNearby)
                {
                    DrawItemTooltip(itemHere);
                }
            }
            
            foreach (var item in room.Items)
                if (room.VisibleNow[item.X, item.Y])
                    DrawTile(item.Key, item.X, item.Y);
            foreach (var e in room.Enemies)
                if (room.VisibleNow[e.X, e.Y])
                    DrawTile(e.TextureKey, e.X, e.Y);
            Enemy? boss = room.Enemies.Find(e => e.IsBoss && room.VisibleNow[e.X, e.Y]);
            if (boss != null)
            {
                int barW = 400, barH = 18;
                int bx = (Settings.MapWidth * Settings.CellWidth - barW) / 2;
                float pct = (float)boss.Health / boss.MaxHealth;
                Raylib.DrawRectangle(bx, 8, barW, barH, new Color(40, 0, 0, 220));
                Raylib.DrawRectangle(bx, 8, (int)(barW * pct), barH, Color.Red);
                Raylib.DrawRectangleLines(bx, 8, barW, barH, Color.Gold);
                string bn = boss.Name;
                Vector2 bsz = Raylib.MeasureTextEx(logFont, bn, 16, 1);
                Raylib.DrawTextEx(logFont, bn, new Vector2(bx + (barW - bsz.X) / 2f, 9), 16, 1, Color.White);
            }

            DrawTile("player", Game.Player.X, Game.Player.Y);
            
            int gameW = Settings.MapWidth * Settings.CellWidth;
            int gameH = Settings.MapHeight * Settings.CellHeight;

            if (Game.CurrentState == GameState.Targeting && Game.ValidTargets.Count > 0)
            {
                // Подсвечиваем всех валидных врагов слабо, выбранного — ярко
                for (int i = 0; i < Game.ValidTargets.Count; i++)
                {
                    var t = Game.ValidTargets[i];
                    Vector2 pos = new(t.X * Settings.CellWidth, t.Y * Settings.CellHeight);
                    Color c = (i == Game.TargetIndex)
                        ? new Color((byte)255, (byte)0, (byte)0, (byte)180)   // выбранный — красный
                        : new Color((byte)255, (byte)255, (byte)0, (byte)80); // остальные — жёлтый
                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(pos.X, pos.Y, Settings.CellWidth, Settings.CellHeight), 3, c);
                }

                // Подсказка внизу
                string hint = $"Прицел: {Game.SelectedSpell?.Name} | Tab - след. цель | Enter - каст | Esc - отмена";
                Raylib.DrawTextEx(logFont, hint, new Vector2(10, Settings.MapHeight * Settings.CellHeight - 30), 18, 1, Color.Gold);
            }

            // --- Строка статуса внизу ---
            string status = $"Этаж: {Game.DungeonLevel} | LVL: {Game.Player.Level} | XP: {Game.Player.Experience}/{Game.Player.ExperienceToNextLevel} | HP: {Game.Player.Health}/{Game.Player.MaxHealth} | MP: {Game.Player.Mana}/{Game.Player.MaxMana} | DMG: {Game.Player.Damage} | SPD: {Game.Player.Speed}";
            Raylib.DrawTextEx(logFont, status, new Vector2(10, gameH + 8), 20, 1, Color.Gold);

            // --- Боковая панель справа ---
            int sideX = gameW + 10; // отступ 10px от края игрового поля

            // Разделитель
            Raylib.DrawLine(gameW, 0, gameW, gameH, Color.DarkGray);

            // Инвентарь
            Raylib.DrawTextEx(logFont, "ИНВЕНТАРЬ", new Vector2(sideX, 10), 18, 1, Color.Gold);
            Raylib.DrawTextEx(logFont, "G — поднять  |  1-5 — использовать", new Vector2(sideX, 32), 13, 1, Color.DarkGray);

            for (int i = 0; i < Settings.MaxInventorySize; i++)
            {
                string slot;
                Color slotColor;
                if (i < Game.Player.Inventory.Count)
                {
                    slot = $"[{i + 1}] {Game.Player.Inventory[i].Name}";
                    slotColor = Game.Player.Inventory[i].ItemColor;
                }
                else
                {
                    slot = $"[{i + 1}] -пусто-";
                    slotColor = new Color((byte)80, (byte)80, (byte)80, (byte)255);
                }
                Raylib.DrawRectangle(sideX - 4, 55 + i * 28, 280, 24, new Color((byte)20, (byte)20, (byte)20, (byte)180));
                Raylib.DrawTextEx(logFont, slot, new Vector2(sideX, 58 + i * 28), 17, 1, slotColor);
            }

            // Разделитель между инвентарём и логами
            Raylib.DrawLine(gameW, 200, gameW + Settings.SidebarWidth, 200, new Color((byte)60, (byte)60, (byte)60, (byte)255));
            Raylib.DrawTextEx(logFont, "ЛОГ", new Vector2(sideX, 208), 18, 1, Color.Gold);

            // Логи
            float logY = 232;
            float logMaxWidth = Settings.SidebarWidth - 20;
            for (int i = 0; i < Game.Messages.Count; i++)
            {
                byte alpha = (byte)(120 + (i * 135 / Settings.MaxLogMessages));
                Color msgColor = new Color((byte)255, (byte)255, (byte)255, alpha);

                var lines = WrapText(Game.Messages[i], logFont, 16, logMaxWidth);
                foreach (var line in lines)
                {
                    Raylib.DrawTextEx(logFont, line, new Vector2(sideX, logY), 16, 1, msgColor);
                    logY += 20;
                }
            }
            

            if (Game.CurrentState == GameState.Console)
            {
                int consoleY = Settings.ScreenHeight - 40;
                // Фоновая полоса
                Raylib.DrawRectangle(0, consoleY, Settings.ScreenWidth, 40, new Color(20, 20, 20, 230));
                Raylib.DrawLine(0, consoleY, Settings.ScreenWidth, consoleY, Color.Lime);

                // Текст команды. Добавляем мигающий курсор
                string cursor = (Raylib.GetTime() % 1.0 < 0.5) ? "_" : "";
                Raylib.DrawTextEx(logFont, "> " + Game.CurrentCommand + cursor, 
                    new Vector2(10, consoleY + 10), 20, 1, Color.Lime);
            }


            if (Game.CurrentState == GameState.GameOver)
            {
                // Затемнение экрана
                Raylib.DrawRectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight, new Color(0, 0, 0, 200));
                
                string mainText = "ИГРА ОКОНЧЕНА";
                string subText = "Нажмите [R] для перезапуска";

                Vector2 mainSize = Raylib.MeasureTextEx(logFont, mainText, 40, 1);
                Vector2 subSize = Raylib.MeasureTextEx(logFont, subText, 25, 1);

                Vector2 mainPos = new Vector2(Settings.ScreenWidth / 2 - mainSize.X / 2, Settings.ScreenHeight / 2 - 50);
                Vector2 subPos = new Vector2(Settings.ScreenWidth / 2 - subSize.X / 2, Settings.ScreenHeight / 2 + 10);
                
                Raylib.DrawTextEx(logFont, mainText, mainPos, 40, 1, Color.Red);
                Raylib.DrawTextEx(logFont, subText, subPos, 25, 1, Color.White);
            }
            if (Game.CurrentState == GameState.LevelUp)
            {
                // Затемнение фона
                Raylib.DrawRectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight, new Color(0, 0, 0, 220));

                string title = "НОВЫЙ УРОВЕНЬ!";
                string sub = "Выберите улучшение:";
                string opt1 = "[1] Сила (+5 к урону)";
                string opt2 = "[2] Выносливость (+20 макс. HP)";
                string opt3 = "[3] Ловкость (+15 к скорости)";

                Raylib.DrawTextEx(logFont, title, new Vector2(Settings.ScreenWidth / 2 - 100, 100), 30, 1, Color.Gold);
                Raylib.DrawTextEx(logFont, sub, new Vector2(Settings.ScreenWidth / 2 - 100, 150), 20, 1, Color.White);
                
                Raylib.DrawTextEx(logFont, opt1, new Vector2(Settings.ScreenWidth / 2 - 100, 200), 20, 1, Color.SkyBlue);
                Raylib.DrawTextEx(logFont, opt2, new Vector2(Settings.ScreenWidth / 2 - 100, 230), 20, 1, Color.Lime);
                Raylib.DrawTextEx(logFont, opt3, new Vector2(Settings.ScreenWidth / 2 - 100, 260), 20, 1, Color.Orange);
            }
            if (Game.CurrentState == GameState.LearnSpell && Game.PendingSpell != null)
            {
                Raylib.DrawRectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight, new Color(0, 0, 0, 220));

                var sp = Game.PendingSpell;
                Raylib.DrawTextEx(logFont, "ИЗУЧИТЬ ЗАКЛИНАНИЕ", new Vector2(60, 60), 30, 1, Color.Gold);
                Raylib.DrawTextEx(logFont, $"{sp.Name} — {sp.Description}", new Vector2(60, 110), 20, 1, Color.SkyBlue);
                Raylib.DrawTextEx(logFont, "Слоты заняты. Что заменить?", new Vector2(60, 150), 20, 1, Color.White);

                for (int i = 0; i < Game.Player.SpellBook.Count; i++)
                {
                    var s = Game.Player.SpellBook[i];
                    Raylib.DrawTextEx(logFont, $"[{i + 1}] {s.Name} — {s.Description}",
                        new Vector2(60, 200 + i * 40), 20, 1, Color.Orange);
                }
                Raylib.DrawTextEx(logFont, "[Esc] Не изучать", new Vector2(60, 340), 20, 1, Color.Gray);
            }
        }

        static void DrawWorldMap()
        {
            Raylib.DrawRectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight, new Color(0, 0, 0, 230));

            int step = Settings.MapRoomSize + Settings.MapConnectionLen;
            int totalGridSize = Settings.WorldSize * step - Settings.MapConnectionLen;
            int startX = (Settings.ScreenWidth - totalGridSize) / 2;
            int startY = (Settings.ScreenHeight - totalGridSize) / 2;

            for (int y = 0; y < Settings.WorldSize; y++)
            {
                for (int x = 0; x < Settings.WorldSize; x++)
                {
                    Room r = Game.World[x, y];
                    if (!r.Exists) continue;

                    // Координаты левого верхнего угла комнаты
                    float rx = startX + x * step;
                    float ry = startY + y * step;
                    
                    // 1. Отрисовка ПРОХОДОВ (рисуем их ПЕРЕД комнатами, чтобы они были "под" ними)
                    if (x + 1 < Settings.WorldSize && Game.World[x + 1, y].Exists)
                    {
                        Color connColor = (r.Visited && Game.World[x + 1, y].Visited) ? Color.White : Color.DarkGray;
                        // Центрируем проход по вертикали относительно комнаты
                        Raylib.DrawRectangle((int)(rx + Settings.MapRoomSize), (int)(ry + Settings.MapRoomSize / 2 - 1), Settings.MapConnectionLen, 2, connColor);
                    }
                    if (y + 1 < Settings.WorldSize && Game.World[x, y + 1].Exists)
                    {
                        Color connColor = (r.Visited && Game.World[x, y + 1].Visited) ? Color.White : Color.DarkGray;
                        // Центрируем проход по горизонтали относительно комнаты
                        Raylib.DrawRectangle((int)(rx + Settings.MapRoomSize / 2 - 1), (int)(ry + Settings.MapRoomSize), 2, Settings.MapConnectionLen, connColor);
                    }

                    // 2. Отрисовка КОМНАТЫ
                    Rectangle roomRect = new Rectangle(rx, ry, Settings.MapRoomSize, Settings.MapRoomSize);

                    if (x == Game.CurRoomX && y == Game.CurRoomY)
                    {
                        // 1. Рисуем золотой квадрат
                        Raylib.DrawRectangleRec(roomRect, Color.Gold);
                        Raylib.DrawRectangleLinesEx(roomRect, 1, Color.White);
                        
                        int fontSize = Settings.MapFontSize;
                        
                        float xOffset = (Settings.MapRoomSize - (fontSize * 0.5f)) / 2; 
                        float yOffset = (Settings.MapRoomSize - fontSize) / 2;

                        Vector2 playerPos = new Vector2(rx + xOffset, ry + yOffset);

                        Raylib.DrawTextEx(gameFont, "@", playerPos, fontSize, 1, Color.Black);
                    }
                    else if (r.Visited)
                    {
                        Raylib.DrawRectangleRec(roomRect, Color.Gray);
                        Raylib.DrawRectangleLinesEx(roomRect, 1, Color.LightGray);
                    }
                    else
                    {
                        Raylib.DrawRectangleLinesEx(roomRect, 1, Color.DarkGray);
                    }
                }
            }
        }
        static void DrawTile(string key, int cellX, int cellY)
        {
            Texture2D tex = TextureLibrary.Get(key);
            Rectangle source = new Rectangle(0, 0, tex.Width, tex.Height);
            Rectangle dest = new Rectangle(
                cellX * Settings.CellWidth, cellY * Settings.CellHeight,
                Settings.CellWidth, Settings.CellHeight);
            Raylib.DrawTexturePro(tex, source, dest, Vector2.Zero, 0, Color.White);
        }
        static void DrawItemTooltip(Item item)
        {
            string line1 = item.Name;
            string line2 = item.Description;
            string line3 = "[G] подобрать";

            float fontSize = 16;
            float pad = 8;
            float w = Math.Max(
                Math.Max(Raylib.MeasureTextEx(logFont, line1, fontSize, 1).X,
                        Raylib.MeasureTextEx(logFont, line2, fontSize, 1).X),
                Raylib.MeasureTextEx(logFont, line3, fontSize, 1).X) + pad * 2;
            float h = fontSize * 3 + pad * 2 + 8;

            // Позиция рядом с игроком (чуть выше и правее), с защитой от выхода за экран
            float px = Game.Player.X * Settings.CellWidth + Settings.CellWidth;
            float py = Game.Player.Y * Settings.CellHeight - h;
            float gameW = Settings.MapWidth * Settings.CellWidth;
            float gameH = Settings.MapHeight * Settings.CellHeight;
            if (px + w > gameW) px = gameW - w;
            if (py < 0) py = Game.Player.Y * Settings.CellHeight + Settings.CellHeight;

            // Фон + рамка
            Raylib.DrawRectangle((int)px, (int)py, (int)w, (int)h, new Color(20, 20, 30, 230));
            Raylib.DrawRectangleLines((int)px, (int)py, (int)w, (int)h, Color.Gold);

            Raylib.DrawTextEx(logFont, line1, new Vector2(px + pad, py + pad), fontSize, 1, item.ItemColor);
            Raylib.DrawTextEx(logFont, line2, new Vector2(px + pad, py + pad + fontSize + 2), fontSize, 1, Color.White);
            Raylib.DrawTextEx(logFont, line3, new Vector2(px + pad, py + pad + (fontSize + 2) * 2), fontSize, 1, Color.Gray);
        }
    }
}