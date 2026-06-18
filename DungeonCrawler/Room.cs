using System.Collections.Generic;
using Raylib_cs;

namespace RogueLiteGame
{
    public class Room
    {
        public bool Exists = false; // Есть ли комната в этой ячейке сетки
        public bool Visited = false; 
        public char[,] Tiles;
        public List<Enemy> Enemies;
        public List<Item> Items;
        public bool[,] Explored;   // когда-либо видел
        public bool[,] VisibleNow; // видно в данный момент
        public Room()
        {
            Tiles = new char[Settings.MapWidth, Settings.MapHeight];
            Enemies = new List<Enemy>();
            Items = new List<Item>();
            Explored = new bool[Settings.MapWidth, Settings.MapHeight];
            VisibleNow = new bool[Settings.MapWidth, Settings.MapHeight];
        }
    }
}