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
            logger.LogInfo("\n================= 🧩 PARSED MESSAGE =================");
            logger.LogInfo($"Type           : {Type}");
            logger.LogInfo($"Match ID       : {MatchId}");
            logger.LogInfo($"Game ID        : {GameId}");
            logger.LogInfo($"Your ID        : {YourId}");

            if (Day != null) logger.LogInfo($"Day            : {Day}");
            if (Phase != null) logger.LogInfo($"Phase          : {Phase}");
            if (Timeout != null) logger.LogInfo($"Timeout (s)    : {Timeout}");
            if (FirstVoteTimeout != null) logger.LogInfo($"First Vote TO  : {FirstVoteTimeout}");
            if (TimeRemaining != null) logger.LogInfo($"Time Remaining : {TimeRemaining}");
            if (!string.IsNullOrEmpty(Otp)) logger.LogInfo($"OTP            : {Otp}");

            switch (Type)
            {
                // GAME START
                case "game-start":
                    logger.LogInfo($"Your Role      : {YourRole}");
                    logger.LogInfo($"Ravens         : {RavenCount}");
                    logger.LogInfo($"Detectives     : {DetectiveCount}");
                    logger.LogInfo($"Doctors        : {DoctorCount}");
                    logger.LogInfo($"Villagers      : {VillagerCount}");
                    break;

                // PLAYER STATUS
                case "player-status":
                    logger.LogInfo("All Players    :");
                    foreach (var pl in AllPlayers)
                    {
                        logger.LogInfo($"  - {FormatDict(pl)}");
                    }
                    break;

                // PHASE RESULT
                case "phase-result":
                    logger.LogInfo($"Player Lynched : {PlayerLynched}");
                    break;

                // ACK
                case "ack":
                    logger.LogInfo($"Request Status : {RequestStatus}");
                    break;

                // NIGHT DISCUSSION
                case "night-discussion":
                    logger.LogInfo($"Villagers Alive: {string.Join(", ", VillagersAlive)}");
                    break;

                // RAVEN COMMENT
                case "raven-comment":
                    logger.LogInfo("Discussions    :");
                    foreach (var d in Discussions)
                    {
                        d.TryGetValue("playerId", out var playerId);
                        d.TryGetValue("comment", out var comment);
                        d.TryGetValue("votes", out var votes);
                        logger.LogInfo($"  - {playerId}: {comment} (votes={votes})");
                    }
                    if (Votes.Count > 0)
                        logger.LogInfo($"Last Votes     : [{string.Join(", ", Votes)}]");
                    break;

                // MORNING DISCUSSION
                case "morning-discussion":
                    logger.LogInfo($"Players Alive  : {string.Join(", ", PlayersAlive)}");
                    break;

                // MORNING PLAYER COMMENT
                case "morning-player-comment":
                    logger.LogInfo("Discussions    :");
                    foreach (var d in Discussions)
                    {
                        d.TryGetValue("playerId", out var playerId);
                        d.TryGetValue("comment", out var comment);
                        d.TryGetValue("votes", out var votes);
                        logger.LogInfo($"  - {playerId}: {comment} (votes={votes})");
                    }
                    if (Votes.Count > 0)
                        logger.LogInfo($"Last Votes     : [{string.Join(", ", Votes)}]");
                    break;

                // NIGHT INVESTIGATION
                case "night-investigation":
                    logger.LogInfo($"Players Alive        : {string.Join(", ", PlayersAlive)}");
                    logger.LogInfo($"Identified Ravens    : {string.Join(", ", IdentifiedRaven)}");
                    logger.LogInfo($"Identified Villagers : {string.Join(", ", IdentifiedVillager)}");
                    break;

                // NIGHT PROTECTION
                case "night-protection":
                    logger.LogInfo($"Players Alive  : {string.Join(", ", PlayersAlive)}");
                    break;

                // ACK NIGHT INVESTIGATION
                case "ack-night-investigation":
                    logger.LogInfo($"Investigated   : {string.Join(", ", Investigated)}");
                    logger.LogInfo($"Is Raven       : {string.Join(", ", IsRaven)}");
                    break;

                // GAME RESULT
                case "game-result":
                    logger.LogInfo($"Game Result    : {Result}");
                    break;

                // UNKNOWN TYPE
                default:
                    logger.LogInfo("⚠️ No specific printer for this type; showing raw JSON:");
                    try
                    {
                        var json = JsonSerializer.Deserialize<object>(Raw ?? "");
                        logger.LogInfo(JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    catch
                    {
                        logger.LogInfo(Raw);
                    }
                    break;
            }

            logger.LogInfo("=====================================================");
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
