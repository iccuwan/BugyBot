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
using System;
using System.Web;

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

		[Command("play", RunMode = RunMode.Async)]
		[Summary("Playing music from youtube url")]
		public async Task AddToQueryAsync([Remainder]string url)
		{
			//string url = string.Join(' ', input.ToArray());
			if (string.IsNullOrWhiteSpace(url))
			{
				await ReplyAsync("Ты дебил?");
				return;
			}
			if (!musicService.IsYoutubeLink(url))
			{
				await ReplyAsync($"Поиск по запросу {url}");
				url = await musicService.FindYoutube(url);
				if (url == null)
				{
					await ReplyAsync("Ничего не найдено");
					return;
				}
			}
			MusicData md = new MusicData
			{
				url = url,
				context = Context
			};
			_ = musicService.AddMusicToQuery(md);
			await ReplyAsync("Добавлено в очередь");
		}

		[Command("playlist", RunMode = RunMode.Async)]
		public async Task AddPlaylistToQuery(string url)
		{
			if (string.IsNullOrEmpty(url) || !musicService.IsYoutubeLink(url))
			{
				await ReplyAsync("Ссылка дно");
				return;
			}
			Uri uri = new Uri(url);
			string playlistId = HttpUtility.ParseQueryString(uri.Query).Get("list");
			if (playlistId != null)
			{
				await musicService.AddPlaylistToQueryAsync(playlistId, Context);
			}
		}

		[Command("loop")]
		public Task SetLoop()
		{
			musicService.Loop = !musicService.Loop;
			string reply = musicService.Reverse ? "Повтор музыки включён" : "Повтор музыки выключен";
			return ReplyAsync(reply);
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
			await ReplyAsync("Трек пропущен");
			await musicService.ProcessedNextTrackAsync(true);
		}

		[Command("query")]
		public async Task Query()
		{
			string response = "Очередь треков\nСейчас играет: ";
			int i = 0;
			foreach (MusicData md in musicService.Query)
			{
				response += $"{i}:{md.url}\n";
				i++;
			}
			await ReplyAsync(response);
		}
	}
}
