using System.Text.Json.Nodes;

namespace DefendingChampionsBot.Wordle
{
    public class WordleParser
    {
        public WordleDto? ParseRequestToObject(string message)
        {
            JsonNode? obj;

            try
            {
                obj = JsonNode.Parse(message);
            }
            catch
            {
                Console.WriteLine($"Could not parse this {message}");
                return null;
            }

            if (obj is null)
            {
                Console.WriteLine($"Object is null");
                return null;
            }

            var wordleDto = new WordleDto
            {
                Raw = message,
                Type = obj["type"]?.GetValue<string>(),
                Command = obj["command"]?.GetValue<string>(),
                MatchId = obj["matchId"]?.GetValue<string>(),
                GameId = obj["gameId"]?.GetValue<string>(),
                YourId = obj["yourId"]?.GetValue<string>(),
                Otp = obj["otp"]?.GetValue<string>(),
                WordLength = obj["wordLength"]?.GetValue<int?>(),
                MaxAttempts = obj["maxAttempts"]?.GetValue<int?>(),
                LastGuess = obj["lastGuess"]?.GetValue<string>() ?? "",
                CurrentAttempt = obj["currentAttempt"]?.GetValue<int?>(),
                AckFor = obj["ackFor"]?.GetValue<string>(),
                AckData = obj["ackData"]?.GetValue<string>(),
                Result = obj["result"]?.GetValue<string>(),
                Word = obj["word"]?.GetValue<string>()
            };

            var lastResNode = obj["lastResult"];

            if (lastResNode is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    var val = item?.GetValue<string>() ?? "";
                    wordleDto.LastResult.Add(MapWordleFeedbackToken(val));
                }
            }

            return wordleDto;
        }

        public Dictionary<string, object>? ParsedObjectToResponse(WordleDto wordleDto, string guess)
        {
            if (string.IsNullOrWhiteSpace(wordleDto.MatchId) ||
                string.IsNullOrWhiteSpace(wordleDto.GameId) ||
                string.IsNullOrWhiteSpace(wordleDto.Otp))
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                ["matchId"] = wordleDto.MatchId!,
                ["gameId"] = wordleDto.GameId!,
                ["otp"] = wordleDto.Otp!,
                ["guess"] = guess
            };
        }

        private string MapWordleFeedbackToken(string token)
        {
            if (token == null)
                return "absent";

            string cleaned = token.Trim().ToLowerInvariant();

            if (cleaned == "correct" || cleaned == "green" || cleaned == "g")
                return "correct";

            if (cleaned == "present" || cleaned == "yellow" || cleaned == "y")
                return "present";

            return "absent";
        }
    }
}
