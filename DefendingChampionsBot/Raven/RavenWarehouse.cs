using System.Collections.Concurrent;
using System.Text;

namespace DefendingChampionsBot.Raven
{
    public static class RavenWarehouse
    {
        public static readonly ConcurrentDictionary<string, RavenGameInfo> Collection = new ConcurrentDictionary<string, RavenGameInfo>();

        public static void TryAddGameInfo(string gameId)
        {
            Collection.TryAdd(gameId, new RavenGameInfo());
        }

        public static void TryRemoveGameInfo(string gameId)
        {
            Collection.TryRemove("gameId", out RavenGameInfo removedValue);
        }

        public static RavenGameInfo GetGameInfo(string gameId)
        {
            if (Collection.TryGetValue(gameId, out RavenGameInfo gameInfo))
            {
                return gameInfo;
            }

            gameInfo = new RavenGameInfo();

            if (Collection.TryAdd(gameId, gameInfo))
            {
                return gameInfo;
            }

            Collection.TryGetValue(gameId, out gameInfo);
            return gameInfo;
        }

        public static void LogGameInfo(string gameId, Logger logger)
        {
            if (Collection.TryGetValue(gameId, out RavenGameInfo gameInfo))
            {
                gameInfo.Log(logger);
            }
        }
        
    }

    public class RavenGameInfo
    {
        public bool FoundOtherRaven = false;
        public string MyId { get; set; } = "";
        public string MyRole { get; set; } = "";
        public string Prompt { get; set; } = "";
        public List<string> Ravens { get; set; } = new();
        public List<string> Villagers { get; set; } = new();
        public List<string> Detectives { get; set; } = new();
        public List<string> Doctors { get; set; } = new();
        public List<string> AllPlayers { get; set; } = new();
        public List<string> PlayersAlive { get; set; } = new();
        public List<string> PlayersDead { get; set; } = new();
        public List<string> PlayerComments { get; set; } = new();

        public string GenerateUserPrompt(bool isMorning)
        {
            string question = GetQuestion(isMorning);
            Prompt =
$@"Game State Information:
MyId: {MyId}
MyRole: {MyRole}
PlayersAlive: {FormatList(PlayersAlive)}
PlayersDead: {FormatList(PlayersDead)}
Villagers: {FormatList(Villagers)}
Ravens: {FormatList(Ravens)}
Detectives: {FormatList(Detectives)}
Doctors: {FormatList(Doctors)}

Question:
{question}

Important: Respond with exactly ONE PlayerId from the provided data.";

            return Prompt;
        }

        private string GetQuestion(bool isMorning)
        {
            if (isMorning)
            {
                switch (MyRole)
                {
                    case "Detective":
                        return "I am a Detective. Which player should I vote to eliminate today?";
                    case "Villager":
                        return "I am a Villager. Which player should I vote to eliminate today?";
                    case "Raven":
                        return "I am a Raven. Which player should I vote to eliminate today?";
                    case "Doctor":
                        return "I am a Doctor. Which player should I vote to eliminate today?";
                    default:
                        return "Can you suggest a player except me?";
                }
            }
            else
            {
                switch (MyRole)
                {
                    case "Detective":
                        return "I am a Detective. Which player should I investigate?";
                    case "Villager":
                        return "I am a Villager. Which player should I vote to eliminate today?";
                    case "Raven":
                        return "I am a Raven. Which player should we eliminate tonight?";
                    case "Doctor":
                        return "I am a Doctor. Which player should I protect tonight?";
                    default:
                        return "Can you suggest a player except me?";
                }
            }
        }

        public void Log(Logger logger)
        {
            var sb = new StringBuilder();

            sb.AppendLine("--- GameState Log ---");

            sb.AppendLine($"FoundOtherRaven: {FoundOtherRaven}");
            sb.AppendLine($"MyId: {MyId}");
            sb.AppendLine($"MyRole: {MyRole}");

            sb.AppendLine("Ravens: " + string.Join(", ", Ravens));
            sb.AppendLine("Villagers: " + string.Join(", ", Villagers));
            sb.AppendLine("Detectives: " + string.Join(", ", Detectives));
            sb.AppendLine("Doctors: " + string.Join(", ", Doctors));
            sb.AppendLine("AllPlayers: " + string.Join(", ", AllPlayers));
            sb.AppendLine("PlayersAlive: " + string.Join(", ", PlayersAlive));
            sb.AppendLine("PlayersDead: " + string.Join(", ", PlayersDead));

            sb.AppendLine("PlayerComments:");
            foreach (var comment in PlayerComments)
            {
                sb.AppendLine(comment);
            }

            logger.LogInfo(sb.ToString());
        }

        private string FormatList(List<string> list)
        {
            return list.Count == 0 ? "(none)" : string.Join(", ", list);
        }
    }
}
