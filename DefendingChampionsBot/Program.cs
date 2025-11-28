using System.Net.WebSockets;
using System.Text;
using DefendingChampionsBot;
using DefendingChampionsBot.Raven;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Start the VPN!!!");
        Console.WriteLine("Do not close this window!!!");
        _ = Initialize();
        Console.ReadLine();
    }

    public async static Task Initialize()
    {
        using var ws = new ClientWebSocket();

        try
        {
            await ws.ConnectAsync(new Uri(Constants.WS_URL), CancellationToken.None);
            Console.WriteLine("Connection established.");
            
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Connection closed by server");
                        break;
                    }

                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _ = Task.Run(() =>
                    {
                        new RavenBot().HandleRequest(ws, msg);
                        // new WordleBot().HandleRequest(ws, msg);
                    });
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Closing connection.\"");
                    break;
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"Connection closed with error: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while listening: {ex.Message}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
    }
}



