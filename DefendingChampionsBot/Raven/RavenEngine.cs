using static System.Net.WebRequestMethods;
using System.Xml.Linq;

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
            var alive = ravenDto.PlayersAlive != null
                        ? new List<string>(ravenDto.PlayersAlive)
                        : new List<string>();

            string protectTarget = null;

            if (alive.Count > 0)
            {
                var rnd = new Random();
                protectTarget = alive[rnd.Next(alive.Count)];
            }

            string comment = $"As Doctor, I choose to protect {protectTarget} tonight.";

            var votes = protectTarget != null
                ? new List<string> { protectTarget }
                : new List<string>();

            msg["type"] = "vote";
            msg["comment"] = comment;
            msg["votes"] = votes;

            return msg;
        }

        private Dictionary<string, object> GetDefaultReturnValue(RavenDto ravenDto)
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
