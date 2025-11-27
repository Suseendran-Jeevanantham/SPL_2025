using OpenAI;
using OpenAI.Chat;

namespace DefendingChampionsBot
{
    public enum OpenAIModel
    {
        GPT5_Nano,
        GPT5_Mini,
        GPT4_1Mini,
        GPT4o,
        GPT4_1
    }

    public static class Constants
    {
        public static readonly string WS_URL = "ws://localhost:2025";
        public static readonly string OpenAPI_Key = "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";
        public static readonly OpenAIClient Client = new OpenAIClient(OpenAPI_Key);
        public static readonly ChatClient GPT5_Mini = Client.GetChatClient(OpenAIModel.GPT5_Mini.ToModelString());
        public static readonly ChatClient GPT5_Nano = Client.GetChatClient(OpenAIModel.GPT5_Nano.ToModelString());
        public static readonly ChatClient GPT4_1Mini = Client.GetChatClient(OpenAIModel.GPT4_1Mini.ToModelString());
        public static readonly ChatClient GPT4o = Client.GetChatClient(OpenAIModel.GPT4o.ToModelString());
        public static readonly ChatClient GPT4_1 = Client.GetChatClient(OpenAIModel.GPT4_1.ToModelString());

        public static readonly string SystemMessage = "You are solving a Wordle-like game.";
        public static readonly ChatCompletionOptions ChatCompletionOptions = new ChatCompletionOptions()
        {
#pragma warning disable OPENAI001
            ReasoningEffortLevel = ChatReasoningEffortLevel.Low
#pragma warning restore OPENAI001
        };

        public static string ToModelString(this OpenAIModel model)
        {
            return model switch
            {
                OpenAIModel.GPT5_Nano => "gpt-5-nano",
                OpenAIModel.GPT5_Mini => "gpt-5-mini",
                OpenAIModel.GPT4_1Mini => "gpt-4.1-mini",
                OpenAIModel.GPT4o => "gpt-4o",
                OpenAIModel.GPT4_1 => "gpt-4.1",
                _ => throw new ArgumentOutOfRangeException(nameof(model))
            };
        }
    }
}
