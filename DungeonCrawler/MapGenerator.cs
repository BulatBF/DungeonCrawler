using System;
using System.Collections.Generic;

namespace RogueLiteGame
{
    public static class MapGenerator
    {
        private static Random rnd = new Random();

        public static void GenerateWorld(Room[,] world, int worldSize)
        {
            // 1. Очистка мира
            for (int x = 0; x < worldSize; x++)
                for (int y = 0; y < worldSize; y++)
                    world[x, y] = new Room();

            // 2. Алгоритм "случайного блуждания" для комнат
            int startX = worldSize / 2;
            int startY = worldSize / 2;
            
            Stack<(int x, int y)> stack = new();
            stack.Push((startX, startY));
            world[startX, startY].Exists = true;
            
            int roomsCreated = 1;
            (int x, int y) exitRoomCoords = (startX, startY);
            int maxPathDepth = 0;

            while (stack.Count > 0 && roomsCreated < Settings.MaxRooms)
            {
                if (stack.Count > maxPathDepth)
                {
                    maxPathDepth = stack.Count;
                    exitRoomCoords = stack.Peek();
                }

                var current = stack.Peek();
                var neighbors = GetUnvisitedNeighbors(current.x, current.y, world, worldSize);

                if (neighbors.Count > 0)
                {
                    // Выбираем случайного соседа
                    var next = neighbors[rnd.Next(neighbors.Count)];
                    world[next.x, next.y].Exists = true;
                    stack.Push(next);
                    roomsCreated++;

                    // 30% ШАНС ВЕТВЛЕНИЯ
                    // Если есть еще свободные соседи, с шансом 30% создаем "отросток" сразу
                    if (rnd.Next(100) < 30)
                    {
                        var extraNeighbors = GetUnvisitedNeighbors(current.x, current.y, world, worldSize);
                        if (extraNeighbors.Count > 0 && roomsCreated < Settings.MaxRooms)
                        {
                            var branch = extraNeighbors[rnd.Next(extraNeighbors.Count)];
                            world[branch.x, branch.y].Exists = true;
                            // Мы не пушим его в стек, чтобы основной путь продолжался, 
                            // но помечаем как существующий. Он будет "тупиком" или точкой старта позже.
                            roomsCreated++;
                        }
                    }
                }
                else
                {
                    // Тупик - возвращаемся назад по стеку
                    stack.Pop();
                }
            }
            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    if (world[x, y].Exists)
                    {
                        bool isExitRoom = (x == exitRoomCoords.x && y == exitRoomCoords.y);
                        DesignRoom(world, x, y, worldSize, isExitRoom);
                    }
                }
            }
        }
        private static List<(int x, int y)> GetUnvisitedNeighbors(int x, int y, Room[,] world, int worldSize)
        {
            List<(int x, int y)> neighbors = new();
            if (y > 0 && !world[x, y - 1].Exists) neighbors.Add((x, y - 1));
            if (y < worldSize - 1 && !world[x, y + 1].Exists) neighbors.Add((x, y + 1));
            if (x > 0 && !world[x - 1, y].Exists) neighbors.Add((x - 1, y));
            if (x < worldSize - 1 && !world[x + 1, y].Exists) neighbors.Add((x + 1, y));
            return neighbors;
        }

        private static void DesignRoom(Room[,] world, int rx, int ry, int worldSize, bool isExitRoom)
        {
            Room room = world[rx, ry];
            int midX = Settings.MapWidth / 2;
            int midY = Settings.MapHeight / 2;

            // 1. Сначала заполняем базовую коробку (пол и внешние стены)
            for (int x = 0; x < Settings.MapWidth; x++)
            {
                for (int y = 0; y < Settings.MapHeight; y++)
                {
                    if (x == 0 || x == Settings.MapWidth - 1 || y == 0 || y == Settings.MapHeight - 1)
                        room.Tiles[x, y] = '#';
                    else
                        room.Tiles[x, y] = ' ';
                }
            }

            // 2. Генерируем декор (ШАНС 40%)
            bool isStartRoom = (rx == worldSize / 2 && ry == worldSize / 2);
            if (!isStartRoom && rnd.Next(100) < 40)
            {
                int decorationType = rnd.Next(3); // ТЕПЕРЬ 3 ТИПА
                int offset = 3; // Отступ для колонн

                switch (decorationType)
                {
                    case 0: // Колонны (сдвинуты к центру)
                        room.Tiles[midX - offset, midY - offset] = '#';
                        room.Tiles[midX + offset, midY - offset] = '#';
                        room.Tiles[midX - offset, midY + offset] = '#';
                        room.Tiles[midX + offset, midY + offset] = '#';
                        break;

                    case 1: // Центральный блок 2x2
                        room.Tiles[midX, midY] = '#';
                        room.Tiles[midX + 1, midY] = '#';
                        room.Tiles[midX, midY + 1] = '#';
                        room.Tiles[midX + 1, midY + 1] = '#';
                        break;

                    case 2: // НОВОЕ: Разрушенная стена (случайные блоки в центре)
                        for (int i = 0; i < 6; i++)
                        {
                            int rDecX = rnd.Next(midX - 2, midX + 3);
                            int rDecY = rnd.Next(midY - 2, midY + 3);
                            room.Tiles[rDecX, rDecY] = '#';
                        }
                        break;
                }
            }

            // 3. Ставим лестницу ИЛИ босса (только на пустую клетку)
            if (isExitRoom)
            {
                int sx, sy;
                do {
                    sx = rnd.Next(2, Settings.MapWidth - 2);
                    sy = rnd.Next(2, Settings.MapHeight - 2);
                } while (room.Tiles[sx, sy] != ' ');

                bool isBossFloor = Game.DungeonLevel % 5 == 0;

                if (isBossFloor)
                {
                    // Босс-этаж: вместо лестницы — босс. Лестница появится после его смерти.
                    string bossKey = Game.DungeonLevel >= 20 ? "dungeon_lord"
                                : Game.DungeonLevel >= 10 ? "ogre_chief"
                                : "rat_king";
                    room.Enemies.Add(EnemyLibrary.CreateEnemy(bossKey, sx, sy));
                }
                else
                {
                    room.Tiles[sx, sy] = '>';
                }
            }

            // 4. Ставим двери (прорубаем в стенах)
            if (ry > 0 && world[rx, ry - 1].Exists) room.Tiles[midX, 0] = ' '; 
            if (ry < worldSize - 1 && world[rx, ry + 1].Exists) room.Tiles[midX, Settings.MapHeight - 1] = ' '; 
            if (rx > 0 && world[rx - 1, ry].Exists) room.Tiles[0, midY] = ' '; 
            if (rx < worldSize - 1 && world[rx + 1, ry].Exists) room.Tiles[Settings.MapWidth - 1, midY] = ' '; 

            // 5. Спавним врагов (ТОЛЬКО НА ПУСТУЮ КЛЕТКУ)
            if (!isStartRoom)
            {
                int count = rnd.Next(1, 4);
                for (int i = 0; i < count; i++)
                {
                    int ex, ey;
                    int attempts = 0;
                    do {
                        ex = rnd.Next(1, Settings.MapWidth - 1);
                        ey = rnd.Next(1, Settings.MapHeight - 1);
                        attempts++;
                    } while (room.Tiles[ex, ey] != ' ' && attempts < 100);

                    if (attempts < 100)
                    {
                        string randomType = EnemyLibrary.GetRandomEnemyKeyForLevel(Game.DungeonLevel); // ← объявление здесь
                        room.Enemies.Add(EnemyLibrary.CreateEnemy(randomType, ex, ey));
                    }
                }
            }
            if (!isStartRoom && rnd.Next(100) < 40)
            {
                int ix, iy;
                int attempts = 0;
                do {
                    ix = rnd.Next(1, Settings.MapWidth - 1);
                    iy = rnd.Next(1, Settings.MapHeight - 1);
                    attempts++;
                } while (room.Tiles[ix, iy] != ' ' && attempts < 100);

                if (attempts < 100)
                {
                    string itemKey = ItemLibrary.GetRandomItemKeyForLevel(Game.DungeonLevel);
                    room.Items.Add(ItemLibrary.CreateItem(itemKey, ix, iy));
                }
            }
        }
    }
}