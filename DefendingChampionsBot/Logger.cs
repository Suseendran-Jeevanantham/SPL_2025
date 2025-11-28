namespace DefendingChampionsBot
{
    public class Logger
    {
        private readonly object _lock = new();
        private readonly string _baseFolderPath;
        private readonly string _gameId;

        public Logger(string gameId)
        {
            _baseFolderPath = $@"D:\SPL 2025\Logs\raven\\match_logs";
            _gameId = gameId;
            if (!Directory.Exists(_baseFolderPath))
            {
                Directory.CreateDirectory(_baseFolderPath);
            }
        }

        public void LogInfo(string message)
        {
            try
            {
                string logLine = $"[{DateTime.Now:HH:mm:ss}]: {message}";

                lock (_lock)
                {
                    Console.WriteLine(_baseFolderPath + " " + _gameId);
                    File.AppendAllText($"{_baseFolderPath}\\{_gameId}.txt", logLine + Environment.NewLine);
                }
            }
            catch { }
        }
    }
}
