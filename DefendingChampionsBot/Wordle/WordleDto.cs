using System.Text.Json.Serialization;

namespace DefendingChampionsBot.Wordle
{
    public class WordleDto
    {
        [JsonIgnore]
        public string Raw { get; set; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("command")]
        public string? Command { get; set; }

        [JsonPropertyName("matchId")]
        public string? MatchId { get; set; }

        [JsonPropertyName("gameId")]
        public string? GameId { get; set; }

        [JsonPropertyName("yourId")]
        public string? YourId { get; set; }

        [JsonPropertyName("otp")]
        public string? Otp { get; set; }

        [JsonPropertyName("wordLength")]
        public int? WordLength { get; set; }

        [JsonPropertyName("maxAttempts")]
        public int? MaxAttempts { get; set; }

        [JsonPropertyName("lastGuess")]
        public string LastGuess { get; set; } = "";

        [JsonIgnore]
        public List<string> LastResult { get; set; } = new();

        [JsonPropertyName("currentAttempt")]
        public int? CurrentAttempt { get; set; }

        [JsonPropertyName("ackFor")]
        public string? AckFor { get; set; }

        [JsonPropertyName("ackData")]
        public string? AckData { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("word")]
        public string? Word { get; set; }
    }
}
