using System;
using System.Threading.Tasks;
using BugyBot.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BugyBot
{
	public class Startup
	{
		public IConfigurationRoot Configuration { get; }

		public Startup(string[] args)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddYamlFile("config.yml");
			Configuration = builder.Build();
		}

		public static async Task RunAsync(string[] args)
		{
			var startup = new Startup(args);
			await startup.RunAsync();
		}

		public async Task RunAsync()
		{
			var services = new ServiceCollection();
			ConfigureServices(services);

			var provider = services.BuildServiceProvider();
			provider.GetRequiredService<LoggingService>();
			provider.GetRequiredService<CommandHandler>();
			provider.GetRequiredService<LocalizationService>();

			await provider.GetRequiredService<StartupService>().StartAsync();
			await Task.Delay(-1);
		}

		private void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Verbose,
				MessageCacheSize = 1000
			}))
			.AddSingleton(new CommandService(new CommandServiceConfig
			{
				LogLevel = LogSeverity.Verbose,
				DefaultRunMode = RunMode.Async
			}))
			.AddSingleton<CommandHandler>()
			.AddSingleton<StartupService>()
			.AddSingleton<LoggingService>()
			.AddSingleton<MusicService>()
			.AddSingleton<LocalizationService>()
			.AddSingleton<Random>()
			.AddSingleton(Configuration);
		}
	}
}
