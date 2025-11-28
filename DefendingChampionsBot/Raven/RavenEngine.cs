using static System.Net.WebRequestMethods;
using System.Xml.Linq;
using System.IO;
using OpenAI.Chat;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;

namespace DefendingChampionsBot.Raven
{
    public class RavenEngine
    {
        public Logger logger;
        public RavenEngine(Logger logger)
        {
            this.logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildVoteFromMorningDiscussion(RavenDto ravenDto)
        {
            Dictionary<string, object> msg = GetDefaultReturnValue(ravenDto);

            var alive = ravenDto.PlayersAlive ?? new List<string>();
            var possibleTargets = alive.FindAll(p => p != ravenDto.YourId);

            // Random target selection
            string voteTarget = null;
            if (possibleTargets.Count > 0)
            {
                var rand = new Random();
                voteTarget = possibleTargets[rand.Next(possibleTargets.Count)];
            }

            string comment = voteTarget != null
                ? $"I think {voteTarget} may be suspicious. Casting my vote."
                : "I have no one to vote for.";

            msg["type"] = "vote";
            msg["comment"] = comment;
            msg["votes"] = voteTarget != null ? new List<string> { voteTarget } : new List<string>();
            msg["doneVoting"] = true;

            return msg;
        }

        public async Task<Dictionary<string, object>> BuildRavenVoteFromNightDiscussion(RavenDto ravenDto)
        {
            Dictionary<string, object> msg = GetDefaultReturnValue(ravenDto);

            // Sort villagers
            var villagers = ravenDto.VillagersAlive != null
                            ? new List<string>(ravenDto.VillagersAlive)
                            : new List<string>();

            villagers.Sort();

            List<string> votes = new List<string>();
            string comment;

            if (villagers.Count > 0)
            {
                // Simple fallback: vote all villagers
                votes.AddRange(villagers);

                comment = "As Raven, I am voting to eliminate the following villagers: "
                          + string.Join(", ", votes);
            }
            else
            {
                votes = new List<string>();
                comment = "As Raven, I have no villagers to target.";
            }

            msg["type"] = "vote";
            msg["comment"] = comment;
            msg["votes"] = votes;

            return msg;
        }

        public async Task<Dictionary<string, object>> BuildDetectiveVoteFromNightInvestigation(RavenDto ravenDto)
        {
            Dictionary<string, object> msg = GetDefaultReturnValue(ravenDto);

            var alive = ravenDto.PlayersAlive != null
                        ? new List<string>(ravenDto.PlayersAlive)
                        : new List<string>();

            var safePlayers = ravenDto.IdentifiedVillager != null
                ? new HashSet<string>(ravenDto.IdentifiedVillager)
                : new HashSet<string>();

            // Possible targets = alive players who are NOT identified villagers
            var possibleTargets = alive.Where(p => !safePlayers.Contains(p)).ToList();

            possibleTargets = possibleTargets
                                .Except(new List<string> { ravenDto.YourId! })
                                .ToList();

            // If empty, fallback: investigate anyone alive
            if (possibleTargets.Count == 0)
                possibleTargets = alive.ToList();

            string voteTarget = null;

            if (possibleTargets.Count > 0)
            {
                // Pick 1 random target
                var rnd = new Random();
                voteTarget = possibleTargets[rnd.Next(possibleTargets.Count)];
            }

            string comment = $"As Detective, I want to investigate {voteTarget}. Casting my vote on them.";

            var votes = voteTarget != null
                ? new List<string> { voteTarget }
                : new List<string>();

            msg["type"] = "vote";
            msg["comment"] = comment;
            msg["votes"] = votes;

            return msg;
        }

        public async Task<Dictionary<string, object>> BuildDoctorVoteFromNightProtection(RavenDto ravenDto)
        {
            Dictionary<string, object> msg = GetDefaultReturnValue(ravenDto);
            msg["type"] = "vote";
            msg["comment"] = "As Doctor, I choose to protect me tonight.";
            msg["votes"] = new List<string> { ravenDto.YourId!};

            return msg;

            //var alive = ravenDto.PlayersAlive != null
            //            ? new List<string>(ravenDto.PlayersAlive)
            //            : new List<string>();

            //string protectTarget = null;

            //if (alive.Count > 0)
            //{
            //    var rnd = new Random();
            //    protectTarget = alive[rnd.Next(alive.Count)];
            //}

            //string comment = $"As Doctor, I choose to protect {protectTarget} tonight.";

            //var votes = protectTarget != null
            //    ? new List<string> { protectTarget }
            //    : new List<string>();

            //msg["type"] = "vote";
            //msg["comment"] = comment;
            //msg["votes"] = votes;

            //return msg;
        }

        public async Task StoreComments(RavenDto ravenDto)
        {
            RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);

            foreach (var d in ravenDto.Discussions)
            {
                d.TryGetValue("playerId", out var playerId);
                d.TryGetValue("comment", out var comment);
                d.TryGetValue("votes", out var votes);

                try
                {
                    ravenGameInfo.PlayerComments.Add(new PlayerCommentInfo()
                    {
                        PlayerId = playerId.ToString(),
                        Comment = $"{comment!.ToString()}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogInfo($"Error during storing comments {ex.Message}");
                    ravenDto.Log(logger);
                }
            }
        }

        

        public async Task UpdateRavenGameInfoBasedOnGameStart(RavenDto ravenDto)
        {
            try
            {
                RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);
                ravenGameInfo.MyId = ravenDto.YourId!;
                ravenGameInfo.MyRole = ravenDto.YourRole!;

                switch (ravenGameInfo.MyRole)
                {
                    case "Detective":
                        ravenGameInfo.Detectives.Add(ravenGameInfo.MyId);
                        ravenGameInfo.Villagers.Add(ravenGameInfo.MyId);
                        break;
                    case "Villager":
                        ravenGameInfo.Villagers.Add(ravenGameInfo.MyId);
                        break;
                    case "Raven":
                        ravenGameInfo.Ravens.Add(ravenGameInfo.MyId);
                        break;
                    case "Doctor":
                        ravenGameInfo.Doctors.Add(ravenGameInfo.MyId);
                        ravenGameInfo.Villagers.Add(ravenGameInfo.MyId);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogInfo($"Exception during UpdateRavenGameInfoBasedOnGameStart {ex.Message}");
            }

        }

        public async Task UpdatedRavenGameInfoBasedOnPlayerStatus(RavenDto ravenDto)
        {
            RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);

            try
            {
                ravenGameInfo.AllPlayers = new List<string>();
                ravenGameInfo.PlayersAlive = new List<string>();
                ravenGameInfo.PlayersDead = new List<string>();
                foreach (Dictionary<string, object> pl in ravenDto.AllPlayers)
                {
                    ravenGameInfo.AllPlayers.Add(pl["id"].ToString()!);
                    if ((bool)pl["isAlive?"])
                    {
                        ravenGameInfo.PlayersAlive.Add(pl["id"].ToString()!);
                    }
                    else
                    {
                        ravenGameInfo.PlayersDead.Add(pl["id"].ToString()!);
                        if ((string)pl["lynchedBy"] == "Raven")
                        {
                            ravenGameInfo.Villagers.Add(pl["id"].ToString()!);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInfo($"Exception during UpdatedRavenGameInfoBasedOnPlayerStatus {ex.Message}");
            }
        }

        public async Task UpdatedRavenGameInfoBasedOnNightDiscussion(RavenDto ravenDto)
        {
            RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);

            try
            {
                if (!ravenGameInfo.FoundOtherRaven)
                {
                    ravenGameInfo.FoundOtherRaven = true;
                    List<string> ravens = ravenGameInfo.AllPlayers.Except(ravenDto.VillagersAlive).ToList();
                    ravenGameInfo.Ravens = ravens;
                }
                ravenGameInfo.Villagers = ravenDto.VillagersAlive;
            }
            catch (Exception ex)
            {
                 logger.LogInfo($"Exception during UpdatedRavenGameInfoBasedOnNightDiscussion {ex.Message}");
            }
        }

        public async Task UpdatedRavenGameInfoBasedOnAckNightInvestigation(RavenDto ravenDto)
        {
            RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);

            if (ravenDto.IsRaven.First())
            {
                ravenGameInfo.Ravens.Add(ravenDto.Investigated.First());
            }
            else
            {
                ravenGameInfo.Villagers.Add(ravenDto.Investigated.First());
            }
        }

            public async Task<Dictionary<string, object>?> TakeDecision(RavenDto ravenDto, bool isMorning, CancellationTokenSource cts)
        {
            Dictionary<string, object> msg = GetDefaultReturnValue(ravenDto);
            RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);

            ChatClient gpt5_Mini_Chat = Constants.GPT5_Mini;
            ChatCompletionOptions options = Constants.ChatCompletionOptions;
            string prompt = ravenGameInfo.GenerateUserPrompt(isMorning);
            logger.LogInfo(prompt);
            Stopwatch st = new Stopwatch();
            st.Start();
            var result = await gpt5_Mini_Chat.CompleteChatAsync(
                messages:
                [
                    Constants.SystemMessage,
                    ChatMessage.CreateUserMessage(prompt)
                ],
                options,
                cancellationToken: cts.Token
            );
            st.Stop();
            logger.LogInfo($"Time taken: {st.Elapsed.TotalSeconds} seconds");
            ChatCompletion? message = result?.Value;

            string vote = message.Content.First().Text.Trim().ToUpper();

            msg["type"] = "vote";
            msg["comment"] = GenerateCommentBasedOnRole(ravenGameInfo.MyRole);
            msg["votes"] = new List<string>()
            {
                vote
            };

            if (!ravenGameInfo.PlayersAlive.Contains(vote))
            {
                throw new Exception($"Invalid vote {vote}");
            }

            if (ravenGameInfo.MyRole == "Raven" && !isMorning)
            {
                AppendAliveVillagers(msg, ravenGameInfo);
            }

            return msg;
        }

        private void AppendAliveVillagers(Dictionary<string, object> msg, RavenGameInfo ravenGameInfo)
        {
            List<string> list1 = msg["votes"] as List<string>;
            List<string> list2 = ravenGameInfo.Villagers.Except(ravenGameInfo.PlayersDead).ToList();

            List<string> finalList = list1.Concat(list2).Distinct().ToList();

            msg["votes"] = finalList;
        }

        private object GenerateCommentBasedOnRole(string myRole)
        {
            return "I am voting";
        }

        public Dictionary<string, object> GetDefaultReturnValue(RavenDto ravenDto)
        {
            return new Dictionary<string, object>
            {
                ["gameId"] = ravenDto.GameId!,
                ["yourId"] = ravenDto.YourId!,
                ["otp"] = ravenDto.Otp!,
            };
        }
    }
}
