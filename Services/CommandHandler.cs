using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;

namespace BugyBot.Services
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;
		private readonly IServiceProvider provider;

		private List<ulong> blacklist;

		public CommandHandler(DiscordSocketClient _discord, CommandService _commands, IConfigurationRoot _config, IServiceProvider _provider)
		{
			discord = _discord;
			commands = _commands;
			config = _config;
			provider = _provider;

			discord.MessageReceived += OnMessageReceivedAsync;

			blacklist = new List<ulong>();
			string blacklistPath = "config/blacklist.txt";
			if (File.Exists(blacklistPath))
			{
				string[] ids = File.ReadAllLines(blacklistPath);
				foreach(string id in ids)
				{
					if (!id.StartsWith('#'))
					{
						blacklist.Add(Convert.ToUInt64(id));
					}
				}
			}
		}

		private async Task OnMessageReceivedAsync(SocketMessage s)
		{
			var msg = s as SocketUserMessage;
			if (msg == null) return;
			if (msg.Author.Id == discord.CurrentUser.Id) return;

			var context = new SocketCommandContext(discord, msg);

			int argPos = 0;
			if (!blacklist.Contains(context.User.Id) && (msg.HasStringPrefix(config["prefix"], ref argPos) || msg.HasMentionPrefix(discord.CurrentUser, ref argPos)))
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
