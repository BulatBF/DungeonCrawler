using System.Collections.Generic;
using System.IO;
using Raylib_cs;

namespace RogueLiteGame
{
    public static class TextureLibrary
    {
        private static Dictionary<string, Texture2D> _textures = new();
        private static Texture2D _missing; // заглушка для отсутствующих

        public static void LoadAll()
        {
            string dir = "assets/tiles";
            if (Directory.Exists(dir))
            {
                // Ищем PNG во всех подпапках (terrain, entities, items, misc)
                foreach (string file in Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories))
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    Texture2D tex = Raylib.LoadTexture(file);
                    Raylib.SetTextureFilter(tex, TextureFilter.Point);
                    _textures[key] = tex;
                }
            }

            Image img = Raylib.GenImageColor(16, 16, Color.Magenta);
            _missing = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
        }

        public static Texture2D Get(string key)
        {
            if (_textures.TryGetValue(key, out var tex)) return tex;
            return _missing;
        }

        public static bool Has(string key) => _textures.ContainsKey(key);

        public static void UnloadAll()
        {
            foreach (var tex in _textures.Values) Raylib.UnloadTexture(tex);
            Raylib.UnloadTexture(_missing);
            _textures.Clear();
        }
    }
}