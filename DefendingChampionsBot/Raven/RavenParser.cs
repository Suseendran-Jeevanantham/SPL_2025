using System.Text.Json;

namespace DefendingChampionsBot.Raven
{
    public class RavenParser
    {
        public RavenDto? ParseMessage(string msg)
        {
            JsonDocument doc;

            try
            {
                doc = JsonDocument.Parse(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not parse this {msg}");
                return null;
            }

            var root = doc.RootElement;

            RavenDto p = new RavenDto
            {
                Raw = msg,
                Type = root.GetPropertyOrDefault("type")?.GetString(),
                MatchId = root.GetPropertyOrDefault("matchId")?.GetString(),
                GameId = root.GetPropertyOrDefault("gameId")?.GetString(),
                YourId = root.GetPropertyOrDefault("yourId")?.GetString(),
                Otp = root.GetPropertyOrDefault("otp")?.GetString(),
                Day = root.GetPropertyOrDefault("day")?.GetInt32OrNull(),
                Phase = root.GetPropertyOrDefault("phase")?.GetString(),
                Timeout = root.GetPropertyOrDefault("timeout")?.GetDoubleOrNull(),
                FirstVoteTimeout = root.GetPropertyOrDefault("firstVoteTimeout")?.GetDoubleOrNull(),
                TimeRemaining = root.GetPropertyOrDefault("timeRemaining")?.GetDoubleOrNull()
            };

            switch (p.Type)
            {
                case "game-start":
                    p.RavenCount = root.GetPropertyOrDefault("ravenCount")?.GetInt32OrNull();
                    p.DetectiveCount = root.GetPropertyOrDefault("detectiveCount")?.GetInt32OrNull();
                    p.DoctorCount = root.GetPropertyOrDefault("doctorCount")?.GetInt32OrNull();
                    p.VillagerCount = root.GetPropertyOrDefault("villagerCount")?.GetInt32OrNull();
                    p.YourRole = root.GetPropertyOrDefault("yourRole")?.GetString();
                    return p;

                case "player-status":
                    p.AllPlayers = root.GetListOfObjectDictionaries("allPlayers");
                    return p;

                case "phase-result":
                    p.PlayerLynched = root.GetPropertyOrDefault("playerLynched")?.GetString();
                    return p;

                case "ack":
                    p.RequestStatus = root.GetPropertyOrDefault("requestStatus")?.GetString();
                    return p;

                case "night-discussion":
                    p.VillagersAlive = root.GetStringList("villagersAlive");
                    return p;

                case "raven-comment":
                    p.Discussions = root.GetListOfObjectDictionaries("discussions");
                    p.ExtractLastDiscussion();
                    return p;

                case "morning-discussion":
                    p.PlayersAlive = root.GetStringList("playersAlive");
                    return p;

                case "morning-player-comment":
                    p.Discussions = root.GetListOfObjectDictionaries("discussions");
                    p.ExtractLastDiscussion();
                    return p;

                case "night-investigation":
                    p.PlayersAlive = root.GetStringList("playersAlive");
                    p.IdentifiedRaven = root.GetStringList("identifiedRavens");
                    p.IdentifiedVillager = root.GetStringList("identifiedVillagers");
                    return p;

                case "night-protection":
                    p.PlayersAlive = root.GetStringList("playersAlive");
                    return p;

                case "ack-night-investigation":
                    p.Investigated = root.GetStringList("investigated");
                    p.IsRaven = root.GetBoolList("isRaven");
                    return p;

                case "game-result":
                    p.Result = root.GetPropertyOrDefault("result")?.GetString();
                    return p;
            }

            return p; // fallback for unknown types
        }

        public RavenDto ParseOutgoingMessage(string raw)
        {
            JsonDocument doc;
            JsonElement root;

            try
            {
                doc = JsonDocument.Parse(raw);
                root = doc.RootElement;
            }
            catch
            {
                // Return RavenDto with only raw for logging
                return new RavenDto
                {
                    Raw = raw
                };
            }

            RavenDto p = new RavenDto
            {
                Raw = raw,
                Type = root.GetPropertyOrDefault("type")?.GetString(),
                GameId = root.GetPropertyOrDefault("gameId")?.GetString(),
                YourId = root.GetPropertyOrDefault("yourId")?.GetString(),
                Otp = root.GetPropertyOrDefault("otp")?.GetString(),
                Comment = root.GetPropertyOrDefault("comment")?.GetString(),

                // Voting-related fields
                Votes = root.GetStringList("votes"),
                DoneVoting = root.GetPropertyOrDefault("doneVoting")?.GetBoolean(),
                LlmModelUsed = root.GetPropertyOrDefault("llmModelUsed")?.GetString()
            };

            return p;
        }
    }

    public static class JsonExtensions
    {
        public static JsonElement? GetPropertyOrDefault(this JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) ? value : (JsonElement?)null;
        }

        public static int? GetInt32OrNull(this JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int v) ? v : (int?)null;
        }

        public static double? GetDoubleOrNull(this JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double v) ? v : (double?)null;
        }

        public static List<string> GetStringList(this JsonElement element, string name)
        {
            var list = new List<string>();
            if (!element.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var x in arr.EnumerateArray())
                if (x.ValueKind == JsonValueKind.String)
                    list.Add(x.GetString()!);

            return list;
        }

        public static List<bool> GetBoolList(this JsonElement element, string name)
        {
            var list = new List<bool>();
            if (!element.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var x in arr.EnumerateArray())
                if (x.ValueKind == JsonValueKind.True || x.ValueKind == JsonValueKind.False)
                    list.Add(x.GetBoolean());

            return list;
        }

        public static List<Dictionary<string, object>> GetListOfObjectDictionaries(this JsonElement element, string name)
        {
            var list = new List<Dictionary<string, object>>();

            if (!element.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var obj in arr.EnumerateArray())
            {
                var dict = new Dictionary<string, object>();

                foreach (var prop in obj.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString()!,
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.True or JsonValueKind.False => prop.Value.GetBoolean(),
                        _ => prop.Value.ToString()!
                    };
                }

                list.Add(dict);
            }

            return list;
        }
    }
}
