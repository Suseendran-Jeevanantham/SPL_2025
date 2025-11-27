using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DefendingChampionsBot.Wordle
{
    public class WordleBot
    {
        public WordleParser parser;
        public WordleEngine engine;
        public Logger logger;
        public WordleDto wordleDto;

        public async void HandleRequest(ClientWebSocket ws, string message)
        {
            try
            {
                parser = new WordleParser();
                
                wordleDto = parser.ParseRequestToObject(message);
                if (wordleDto == null)
                {
                    return;
                }

                InitializeLogger();
                engine = new WordleEngine(logger);

                if (wordleDto.Type == "game result")
                {
                    string outcome = wordleDto.Result ?? "no outcome provided";
                    string? correctWord = wordleDto.Word;

                    logger.LogInfo($"Game result : {outcome}. Correct Word: {correctWord}");
                    return;
                }

                if (wordleDto.Type == "command")
                {
                    LogIncomingDetails();
                    UpdateHistoryOfPreviousResponse();
                    string guess = await GenerateResult();
                    
                    var resp = parser.ParsedObjectToResponse(wordleDto, guess);
                    if (resp != null)
                    {
                        string json = JsonSerializer.Serialize(resp);
                        var bytes = Encoding.UTF8.GetBytes(json);

                        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while processing: {ex.Message}");
            }
        }

        private void UpdateHistoryOfPreviousResponse()
        {
            if (!string.IsNullOrEmpty(wordleDto.LastGuess))
            {
                WordleGlobalVariables.AddHistoryForGame(wordleDto.GameId!, wordleDto.LastGuess, wordleDto.LastResult, logger);
            }
        }

        private void InitializeLogger()
        {
            logger = new Logger(wordleDto.MatchId!, wordleDto.GameId!);
        }

        private async Task<string> GenerateResult()
        {
            string guess = "hello";
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(58));
                guess = await engine.Move(wordleDto, logger, cts);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                logger.LogInfo($"Timout occurred. Returning hardcoded guess: {guess} Time: {stopwatch.Elapsed.TotalSeconds} seconds");
                return guess;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogInfo($"Unknown exception occurred {ex.Message}. Returning hardcoded guess: {guess} Time: {stopwatch.Elapsed.TotalSeconds} seconds");
                return guess;
            }
            stopwatch.Stop();
            logger.LogInfo($"Proposed guess from AI: {guess} Time: {stopwatch.Elapsed.TotalSeconds} seconds");
            return guess;
        }

        private void LogIncomingDetails()
        {
            var parts = new List<string>();

            void Add(string name, object? value)
            {
                if (value == null) return;

                if (value is string s && string.IsNullOrWhiteSpace(s))
                    return;

                parts.Add($"{name}: {value}");
            }
            
            Add("Result", wordleDto.Result);
            Add("Word", wordleDto.Word);

            if (!string.IsNullOrWhiteSpace(wordleDto.LastGuess))
                Add("LastGuess", wordleDto.LastGuess);

            if (wordleDto.LastResult != null && wordleDto.LastResult.Count > 0)
                Add("LastResult", $"[{string.Join(", ", wordleDto.LastResult)}]");

            Add("CurrentAttempt", wordleDto.CurrentAttempt);
            Add("WordLength", wordleDto.WordLength);
            Add("MaxAttempts", wordleDto.MaxAttempts);

            Add("Otp", wordleDto.Otp);
            Add("Type", wordleDto.Type);
            Add("Command", wordleDto.Command);
            Add("AckFor", wordleDto.AckFor);
            Add("AckData", wordleDto.AckData);
            Add("YourId", wordleDto.YourId);
            Add("Raw", wordleDto.Raw);

            string message = "WordleDto => " + string.Join(", ", parts);

            logger.LogInfo(message);
        }
    }
}
