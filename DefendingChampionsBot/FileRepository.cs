using DefendingChampionsBot.Raven;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;

namespace DefendingChampionsBot
{
    public class FileRepository
    {
        private readonly object _lock = new();
        private readonly string _baseFolderPath;

        public FileRepository()
        {
            _baseFolderPath = $@"D:\SPL 2025\Logs\raven\\repository\\raw";
            if (!Directory.Exists(_baseFolderPath))
            {
                Directory.CreateDirectory(_baseFolderPath);
            }
        }

        public void ProcessComments(RavenDto ravenDto, Logger logger)
        {
            try
            {
                RavenGameInfo ravenGameInfo = RavenWarehouse.GetGameInfo(ravenDto.GameId!);

                foreach (var playerComment in ravenGameInfo.PlayerComments)
                {
                    if (ravenGameInfo.Ravens.Contains(playerComment.PlayerId))
                    {
                        CaptureRavenLogs(playerComment.Comment!.ToString()!);
                    }
                    else if (ravenGameInfo.Detectives.Contains(playerComment.PlayerId))
                    {
                        CaptureDetectiveLogs(playerComment.Comment!.ToString()!);
                    }
                    else if (ravenGameInfo.Villagers.Contains(playerComment.PlayerId))
                    {
                        CaptureVillagerLogs(playerComment.Comment!.ToString()!);
                    }
                    else if (ravenGameInfo.Doctors.Contains(playerComment.PlayerId))
                    {
                        CaptureDoctorLogs(playerComment.Comment!.ToString()!);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInfo($"Error during Process Comments {ex.Message}");
            }
        }

        public void CaptureVillagerLogs(string message)
        {
            try
            {
                string logLine = $"{message}";

                lock (_lock)
                {
                    File.AppendAllText($"{_baseFolderPath}\\Villager.txt", logLine + Environment.NewLine);
                }
            }
            catch { }
        }

        public void CaptureDetectiveLogs(string message)
        {
            try
            {
                string logLine = $"{message}";

                lock (_lock)
                {
                    File.AppendAllText($"{_baseFolderPath}\\Detective.txt", logLine + Environment.NewLine);
                }
            }
            catch { }
        }

        public void CaptureDoctorLogs(string message)
        {
            try
            {
                string logLine = $"{message}";

                lock (_lock)
                {
                    File.AppendAllText($"{_baseFolderPath}\\Doctor.txt", logLine + Environment.NewLine);
                }
            }
            catch { }
        }

        public void CaptureRavenLogs(string message)
        {
            try
            {
                string logLine = $"{message}";

                lock (_lock)
                {
                    File.AppendAllText($"{_baseFolderPath}\\Raven.txt", logLine + Environment.NewLine);
                }
            }
            catch { }
        }
    }
}
