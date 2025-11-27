using OpenAI;
using OpenAI.Chat;
using System.Text;

namespace DefendingChampionsBot.Wordle
{
    public class WordleEngine
    {
        
        public readonly Logger logger;
        public WordleEngine(Logger logger)
        {
            this.logger = logger;
        }
        public OpenAIClient Client
        {
            get
            {
                return Constants.Client;
            }
        }

        public async Task<string> Move(WordleDto wordleDto, Logger logger, CancellationTokenSource cts)
        {
            string prompt = BuildPromptFromHistory(wordleDto);
            logger.LogInfo(prompt);

            ChatClient chat = Client.GetChatClient(OpenAIModel.GPT5_Mini.ToModelString());

            var result = await chat.CompleteChatAsync(
                messages:
                [
                    ChatMessage.CreateUserMessage(prompt)
                ],
                cancellationToken: cts.Token
            );

            ChatCompletion? message = result?.Value;

            if (message?.Content is null || !message.Content.Any())
            {
                return string.Empty;
            }

            string guess = message.Content.First().Text.Trim().ToUpper();
            return guess.Trim().ToUpper();
        }

        private string BuildPromptFromHistory(WordleDto wordleDto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are solving a Wordle-like game.");
            sb.AppendLine($"Word length: {wordleDto.WordLength}");
            sb.AppendLine($"Attempt number: {wordleDto.CurrentAttempt}");
            sb.AppendLine($"Max attempts: {wordleDto.MaxAttempts}");
            sb.AppendLine();

            var gameHistory = WordleGlobalVariables.GetHistoryForGame(wordleDto.GameId!, logger);

            int i = 0;
            foreach (var kvp in gameHistory)
            {
                i++;
                if (i == 1)
                {
                    sb.AppendLine("Here is the complete guess history:");
                }
                string guess = kvp.Key;
                string result = string.Join(", ", kvp.Value);
                sb.AppendLine($"Guess {i}: {guess}");
                sb.AppendLine($"Result {i}: {result}");
                sb.AppendLine();
            }

            sb.AppendLine("Objective: Minimize the number of attempts.");
            sb.AppendLine("Follow Wordle rules:");
            sb.AppendLine("- 'Correct'  = correct letter in correct position");
            sb.AppendLine("- 'Present'  = letter exists but wrong position");
            sb.AppendLine("- 'Absent'   = letter does NOT exist in the word");
            sb.AppendLine();
            sb.AppendLine($"Return ONLY a single English word with exactly {wordleDto.WordLength} letters.");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
