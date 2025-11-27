using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DefendingChampionsBot.Wordle
{
    public static class WordleGlobalVariables
    {
        public static ConcurrentDictionary<string, Dictionary<string, List<string>>> GameResultDictionary = new ConcurrentDictionary<string, Dictionary<string, List<string>>>();

        public static void AddHistoryForGame(string gameId, string guess, List<string> result, Logger logger)
        {
            try
            {
                if (!GameResultDictionary.ContainsKey(gameId))
                {
                    GameResultDictionary[gameId] = new Dictionary<string, List<string>>();
                }
                if (!GameResultDictionary[gameId].ContainsKey(guess))
                {
                    GameResultDictionary[gameId].Add(guess, result);
                }
            }
            catch (Exception ex)
            {
                logger.LogInfo($"Unknown exception during add history {ex.Message}");
            }
        }

        public static Dictionary<string, List<string>> GetHistoryForGame(string gameId, Logger logger)
        {
            try
            {
                if (!GameResultDictionary.ContainsKey(gameId))
                {
                    return new Dictionary<string, List<string>>();
                }
                return GameResultDictionary[gameId];
            }
            catch (Exception ex)
            {
                logger.LogInfo($"Unknown exception during add history {ex.Message}");
            }
            return new Dictionary<string, List<string>>();
        }
    }
}
