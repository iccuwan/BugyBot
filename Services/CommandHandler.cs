using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace BurningCrusadeMusic.Services
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;
		private readonly IServiceProvider provider;

		public CommandHandler(DiscordSocketClient _discord, CommandService _commands, IConfigurationRoot _config, IServiceProvider _provider)
		{
			discord = _discord;
			commands = _commands;
			config = _config;
			provider = _provider;

			discord.MessageReceived += OnMessageReceivedAsync;
		}

		private async Task OnMessageReceivedAsync(SocketMessage s)
		{
			var msg = s as SocketUserMessage;
			if (msg == null) return;
			if (msg.Author.Id == discord.CurrentUser.Id) return;

			var context = new SocketCommandContext(discord, msg);

			int argPos = 0;
			if (msg.HasStringPrefix(config["prefix"], ref argPos) || msg.HasMentionPrefix(discord.CurrentUser, ref argPos))
			{
				var result = await commands.ExecuteAsync(context, argPos, provider);

				if (!result.IsSuccess)
				{
					await context.Channel.SendMessageAsync(result.ToString());
				}
			}
		}
	}
}
