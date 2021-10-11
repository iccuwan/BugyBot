using Discord.Commands;
using Discord.Audio;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using CliWrap;
using Discord;
using YoutubeExplode.Videos.Streams;
using System.IO;
using BurningCrusadeMusic.Services;

namespace BurningCrusadeMusic.Modules
{
	[Name("Music")]
	[Summary("Playing music")]
	public class MusicModule : ModuleBase<SocketCommandContext>
	{
		private readonly MusicService musicService;

		public MusicModule(MusicService ms)
		{
			musicService = ms;
		}

		[Command("play")]
		[Summary("Playing music from youtube url")]
		public async Task AddToQueryAsync(string url)
		{
			MusicData md = new MusicData
			{
				url = url,
				context = Context
			};
			_ = musicService.AddMusicToQuery(md);
			await ReplyAsync("Добавлено в очередь");
		}

		[Command("volume")]
		public async Task SetVolume(float _volume)
		{
			if (_volume > 0 && _volume <= 10)
			{
				musicService.Volume = _volume;
				await ReplyAsync($"Громкость {_volume}");
			}
			else
			{
				await ReplyAsync("Можно от 0 до 10");
			}
		}

		[Command("speed")]
		public Task SetSpeed(float _speed)
		{
			if (_speed >= 0.5f && _speed <= 2.0f)
			{
				musicService.Speed = _speed;
				return ReplyAsync($"Скорость {_speed}");
			}
			return ReplyAsync("Значение от 0.5 до 2");
		}

		[Command("reverse")]
		public Task SetReverse()
		{
			musicService.Reverse = !musicService.Reverse;
			string reply = musicService.Reverse ? "Реверс музыки включён" : "Реверс музыки выключен";
			return ReplyAsync(reply);
		}

		[Command("skip")]
		public async Task Skip()
		{
			//await musicService.ProcessedNextTrackAsync();
			await ReplyAsync("Трек не пропущен");
		}

		[Command("query")]
		public async Task Query()
		{
			string response = "Очередь треков\nСейчас играет: ";
			foreach (MusicData md in musicService.Query)
			{
				response += $"{md.url}\n";
			}
			await ReplyAsync(response);
		}
	}
}
