using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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
                RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);

                switch (ravenDto.Type)
                {
                    case "game-start":
                        await engine.UpdateRavenGameInfoBasedOnGameStart(ravenDto);
                        return;

                    case "player-status":
                        await engine.UpdatedRavenGameInfoBasedOnPlayerStatus(ravenDto);
                        RavenWarehouse.LogGameInfo(ravenDto.GameId!, logger);
                        return;

                    case "night-discussion":
                        await engine.UpdatedRavenGameInfoBasedOnNightDiscussion(ravenDto);
                        RavenWarehouse.LogGameInfo(ravenDto.GameId!, logger);
                        try
                        {
                            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds((double)ravenDto.Timeout! - 5));
                            outgoing = await engine.TakeDecision(ravenDto, false, cts);
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogInfo("error: night-discussion timed-out");
                            outgoing = await engine.BuildRavenVoteFromNightDiscussion(ravenDto);
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo($"error: unknown exception{ex.Message}");
                            outgoing = await engine.BuildRavenVoteFromNightDiscussion(ravenDto);
                        }
                        break;

                    case "night-investigation":                        
                        try
                        {
                            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds((double)ravenDto.Timeout! - 5));
                            outgoing = await engine.TakeDecision(ravenDto, false, cts);
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogInfo("error: night-investigation timed-out");
                            outgoing = await engine.BuildDetectiveVoteFromNightInvestigation(ravenDto);
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo($"error: unknown exception{ex.Message}");
                            outgoing = await engine.BuildDetectiveVoteFromNightInvestigation(ravenDto);
                        }
                        break;

                    case "night-protection":
                        outgoing = await engine.BuildDoctorVoteFromNightProtection(ravenDto);
                        RavenWarehouse.LogGameInfo(ravenDto.GameId!, logger);
                        break;


                    case "morning-discussion":
                        try
                        {
                            if (ravenGameInfo.PlayersAlive!.Count == 2)
                            {
                                var msg = engine.GetDefaultReturnValue(ravenDto);
                                msg["type"] = "vote";
                                msg["comment"] = "Final blow";
                                msg["votes"] = ravenGameInfo.PlayersAlive.Except(new List<string> { ravenGameInfo.MyId });

                                outgoing = msg;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo($"Error on last 2 elimination case {ex.Message}");
                        }

                        try
                        {
                            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds((double)ravenDto.FirstVoteTimeout! - 5));
                            outgoing = await engine.TakeDecision(ravenDto, true, cts);
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogInfo("error: morning-discussion timed-out");
                            outgoing = await engine.BuildVoteFromMorningDiscussion(ravenDto);
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo($"error: unknown exception{ex.Message}");
                            outgoing = await engine.BuildVoteFromMorningDiscussion(ravenDto);
                        }
                        break;

                    case "morning-player-comment":
                        await engine.StoreComments(ravenDto);
                        RavenWarehouse.LogGameInfo(ravenDto.GameId!, logger);
                        return;

                    case "raven-comment":
                        engine.HandleRavenComment(ravenDto);
                        return;

                    case "ack-night-investigation":
                        await engine.UpdatedRavenGameInfoBasedOnAckNightInvestigation(ravenDto);
                        RavenWarehouse.LogGameInfo(ravenDto.GameId!, logger);
                        return;

                    case "ack":
                        return;

                    case "phase-result":
                        try
                        {
                            if (string.IsNullOrEmpty(ravenDto.PlayerLynched) && !ravenGameInfo.Doctors.Contains(ravenGameInfo.PlayerSelectedByRaven))
                            {
                                ravenGameInfo.Doctors.Add(ravenGameInfo.PlayerSelectedByRaven);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo($"Error: In phase-result {ex.Message}");
                        }
                        return;

                    case "game-result":
                        new FileRepository().ProcessComments(ravenDto, logger);
                        RavenWarehouse.TryRemoveGameInfo(ravenDto.GameId!);
                        return;

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
                RavenWarehouse.LogGameInfo(ravenDto.GameId!, logger);

                ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(rawOut));
                await ws.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
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
