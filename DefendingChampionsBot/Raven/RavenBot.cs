using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DefendingChampionsBot.Raven
{
    public class RavenBot
    {
        public RavenParser parser;
        public RavenEngine engine;
        public Logger logger;
        public RavenDto ravenDto;

        public async void HandleRequest(ClientWebSocket ws, string message)
        {
            try
            {
                parser = new RavenParser();

                ravenDto = parser.ParseMessage(message);
                if (ravenDto == null)
                    return;

                InitializeLogger();
                logger.LogInfo("Incoming");
                ravenDto.Log(logger);

                engine = new RavenEngine(logger);
                Dictionary<string, object> outgoing = null;

                switch (ravenDto.Type)
                {
                    case "morning-discussion":
                        outgoing = await engine.BuildVoteFromMorningDiscussion(ravenDto);
                        break;

                    case "night-discussion":
                        outgoing = await engine.BuildRavenVoteFromNightDiscussion(ravenDto);
                        break;

                    case "night-investigation":
                        outgoing = await engine.BuildDetectiveVoteFromNightInvestigation(ravenDto);
                        break;

                    case "night-protection":
                        outgoing = await engine.BuildDoctorVoteFromNightProtection(ravenDto);
                        break;

                    default:
                        logger.LogInfo($"No action required for type '{ravenDto.Type}'.");
                        return;
                }

                if (outgoing == null)
                {
                    logger.LogInfo($"No outgoing message created for '{ravenDto.Type}'.");
                    return;
                }

                string rawOut = JsonSerializer.Serialize(outgoing);
                var outParsedDto = parser.ParseOutgoingMessage(rawOut);

                logger.LogInfo("Outgoing");
                outParsedDto.Log(logger);

                ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(rawOut));
                await ws.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

                //string json = JsonSerializer.Serialize(outParsedDto);
                //var bytes = Encoding.UTF8.GetBytes(json);

                //await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in HandleMessageAndRespond: {ex}");
            }
        }

        private void InitializeLogger()
        {
            logger = new Logger(ravenDto.GameId!);
        }
    }
}
