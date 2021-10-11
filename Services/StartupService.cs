using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace BurningCrusadeMusic.Services
{
	public class StartupService
	{
		private readonly IServiceProvider provider;
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;

		public StartupService(IServiceProvider _provider, DiscordSocketClient _discord, CommandService _commands, IConfigurationRoot _config)
		{
			provider = _provider;
			discord = _discord;
			commands = _commands;
			config = _config;
		}

		public async Task StartAsync()
		{
			string discordToken = config["token"];
			if (string.IsNullOrWhiteSpace(discordToken))
			{
				throw new Exception("Invalid discord token");
			}

			await discord.LoginAsync(TokenType.Bot, discordToken);
			await discord.StartAsync();

			await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
		}
	}
}
