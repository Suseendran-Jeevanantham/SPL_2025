using System.Text;
using System.Text.Json;

namespace DefendingChampionsBot.Raven
{
    public class RavenDto
    {
        // Original JSON string
        public string? Raw { get; set; }
        public string? Type { get; set; }

        // Core identifiers
        public string? MatchId { get; set; }
        public string? GameId { get; set; }
        public string? YourId { get; set; }

        // Game and phase context
        public int? Day { get; set; }
        public string? Phase { get; set; }   // "morning" or "night"
        public double? Timeout { get; set; }
        public double? FirstVoteTimeout { get; set; }
        public double? TimeRemaining { get; set; }

        // GAME START info
        public string? YourRole { get; set; }
        public int? RavenCount { get; set; }
        public int? DetectiveCount { get; set; }
        public int? DoctorCount { get; set; }
        public int? VillagerCount { get; set; }

        // Common interaction fields
        public string? Otp { get; set; }
        public string? Comment { get; set; }

        // Player lists / status
        public List<Dictionary<string, object>> AllPlayers { get; set; } = new();
        public List<string> VillagersAlive { get; set; } = new();
        public List<string> PlayersAlive { get; set; } = new();
        public List<Dictionary<string, object>> Discussions { get; set; } = new();

        // Voting information
        public List<string> Votes { get; set; } = new();
        public bool? DoneVoting { get; set; }
        public string? PlayerLynched { get; set; }

        // Role-identification info
        public List<string> IdentifiedRaven { get; set; } = new();
        public List<string> IdentifiedVillager { get; set; } = new();

        // Comment metadata
        public string? PlayerId { get; set; }
        public string? LlmModelUsed { get; set; }

        // Ack / investigation results
        public string? RequestStatus { get; set; }
        public List<string> Investigated { get; set; } = new();
        public List<bool> IsRaven { get; set; } = new();

        // Game result
        public string? Result { get; set; }

        public void ExtractLastDiscussion()
        {
            if (Discussions == null || Discussions.Count == 0)
                return;

            var last = Discussions[^1];
            if (last == null) return;

            if (last.TryGetValue("playerId", out var pid))
                PlayerId = pid?.ToString();

            if (last.TryGetValue("comment", out var cmt))
                Comment = cmt?.ToString();

            if (last.TryGetValue("votes", out var votesObj) && votesObj is JsonElement jsonVotes)
            {
                Votes = new List<string>();
                foreach (var v in jsonVotes.EnumerateArray())
                    Votes.Add(v.GetString()!);
            }
        }

        public void Log(Logger logger)
        {
            var sb = new StringBuilder();

            sb.AppendLine("\n================= 🧩 PARSED MESSAGE =================");
            sb.AppendLine($"Type           : {Type}");
            sb.AppendLine($"Match ID       : {MatchId}");
            sb.AppendLine($"Game ID        : {GameId}");
            sb.AppendLine($"Your ID        : {YourId}");

            if (Day != null) sb.AppendLine($"Day            : {Day}");
            if (Phase != null) sb.AppendLine($"Phase          : {Phase}");
            if (Timeout != null) sb.AppendLine($"Timeout (s)    : {Timeout}");
            if (FirstVoteTimeout != null) sb.AppendLine($"First Vote TO  : {FirstVoteTimeout}");
            if (TimeRemaining != null) sb.AppendLine($"Time Remaining : {TimeRemaining}");
            if (!string.IsNullOrEmpty(Otp)) sb.AppendLine($"OTP            : {Otp}");

            switch (Type)
            {
                // GAME START
                case "game-start":
                    sb.AppendLine($"Your Role      : {YourRole}");
                    sb.AppendLine($"Ravens         : {RavenCount}");
                    sb.AppendLine($"Detectives     : {DetectiveCount}");
                    sb.AppendLine($"Doctors        : {DoctorCount}");
                    sb.AppendLine($"Villagers      : {VillagerCount}");
                    break;

                // PLAYER STATUS
                case "player-status":
                    sb.AppendLine("All Players    :");
                    foreach (var pl in AllPlayers)
                    {
                        sb.AppendLine($"  - {FormatDict(pl)}");
                    }
                    break;

                // PHASE RESULT
                case "phase-result":
                    sb.AppendLine($"Player Lynched : {PlayerLynched}");
                    break;

                // ACK
                case "ack":
                    sb.AppendLine($"Request Status : {RequestStatus}");
                    break;

                // NIGHT DISCUSSION
                case "night-discussion":
                    sb.AppendLine($"Villagers Alive: {string.Join(", ", VillagersAlive)}");
                    break;

                // RAVEN COMMENT
                case "raven-comment":
                    sb.AppendLine("Discussions    :");
                    foreach (var d in Discussions)
                    {
                        d.TryGetValue("playerId", out var playerId);
                        d.TryGetValue("comment", out var comment);
                        d.TryGetValue("votes", out var votes);
                        sb.AppendLine($"  - {playerId}: {comment} (votes={votes})");
                    }
                    if (Votes.Count > 0)
                        sb.AppendLine($"Last Votes     : [{string.Join(", ", Votes)}]");
                    break;

                // MORNING DISCUSSION
                case "morning-discussion":
                    sb.AppendLine($"Players Alive  : {string.Join(", ", PlayersAlive)}");
                    break;

                // MORNING PLAYER COMMENT
                case "morning-player-comment":
                    sb.AppendLine("Discussions    :");
                    foreach (var d in Discussions)
                    {
                        d.TryGetValue("playerId", out var playerId);
                        d.TryGetValue("comment", out var comment);
                        d.TryGetValue("votes", out var votes);
                        sb.AppendLine($"  - {playerId}: {comment} (votes={votes})");
                    }
                    if (Votes.Count > 0)
                        sb.AppendLine($"Last Votes     : [{string.Join(", ", Votes)}]");
                    break;

                // NIGHT INVESTIGATION
                case "night-investigation":
                    sb.AppendLine($"Players Alive        : {string.Join(", ", PlayersAlive)}");
                    sb.AppendLine($"Identified Ravens    : {string.Join(", ", IdentifiedRaven)}");
                    sb.AppendLine($"Identified Villagers : {string.Join(", ", IdentifiedVillager)}");
                    break;

                // NIGHT PROTECTION
                case "night-protection":
                    sb.AppendLine($"Players Alive  : {string.Join(", ", PlayersAlive)}");
                    break;

                // ACK NIGHT INVESTIGATION
                case "ack-night-investigation":
                    sb.AppendLine($"Investigated   : {string.Join(", ", Investigated)}");
                    sb.AppendLine($"Is Raven       : {string.Join(", ", IsRaven)}");
                    break;

                // GAME RESULT
                case "game-result":
                    sb.AppendLine($"Game Result    : {Result}");
                    break;

                // UNKNOWN TYPE
                default:
                    sb.AppendLine("⚠️ No specific printer for this type; showing raw JSON:");
                    try
                    {
                        var json = JsonSerializer.Deserialize<object>(Raw ?? "");
                        sb.AppendLine(JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    catch
                    {
                        sb.AppendLine(Raw);
                    }
                    break;
            }

            sb.AppendLine("=====================================================");
            logger.LogInfo(sb.ToString());
        }

        private string FormatDict(Dictionary<string, object> dict)
        {
            List<string> parts = new List<string>();
            foreach (var kvp in dict)
            {
                parts.Add($"{kvp.Key}={kvp.Value}");
            }
            return "{ " + string.Join(", ", parts) + " }";
        }
    }
}
