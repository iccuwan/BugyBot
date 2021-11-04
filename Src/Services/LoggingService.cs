using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BugyBot.Services
{
	public class LoggingService
	{
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;

		private string logDirectory { get; }
		private string logFile => Path.Combine(logDirectory, $"{DateTime.Now.ToString("yyyy-MM-dd")}.txt");

		public LoggingService(DiscordSocketClient _discord, CommandService _commands)
		{
			logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

			discord = _discord;
			commands = _commands;

			discord.Log += OnLogAsync;
			commands.Log += OnLogAsync;
		}

		private Task OnLogAsync(LogMessage msg)
		{
			if (!Directory.Exists(logDirectory))
			{
				Directory.CreateDirectory(logDirectory);
			}
			if (!File.Exists(logFile))
			{
				File.Create(logFile).Dispose();
			}

			string logText = $"{DateTime.Now.ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
			File.AppendAllTextAsync(logFile, logText + "\n");

			return Console.Out.WriteLineAsync(logText);
		}
	}
}
