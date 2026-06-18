namespace RogueLiteGame
{
    public static class Settings
    {
        // Настройки карты и мира
        public const int MapWidth = 15;
        public const int MapHeight = 15;
        public const int WorldSize = 15;

        // Настройки графики
        public const int CellWidth = 40;
        public const int CellHeight = 40;
        public const int LogHeight = 220; // Высота панели логов
        public const int MaxLogMessages = 5;

        // Геймплейные параметры
        public const int ActionCost = 100;
        public const int DefaultPlayerHealth = 100;
        public const int DefaultPlayerDamage = 10;
        public const int DefaultPlayerSpeed = 100;

        // Вычисляемые свойства для удобства
        public const int SidebarWidth = 300;
        public static int ScreenWidth => MapWidth * CellWidth + SidebarWidth;
        public static int ScreenHeight => MapHeight * CellHeight + 40; 

        // Настройки визуализации карты (UI)
        public const int MapRoomSize = 24;      // Размер квадрата комнаты на карте
        public const int MapConnectionLen = 8;  // Длина прохода
        public const int MapFontSize = 20;      // Размер шрифта для символов на карте
        public const int MapTitleFontSize = 30; // Размер заголовка карты
        public const int MaxRooms = 25;
        public const int MaxInventorySize = 5;
        public const string Version = "1.0.0";
        public const int DefaultPlayerMana = 30;
        public const float ManaRegenPerTurn = 0.5f;
        public const int PlayerVisionRadius = 4;
    }
}