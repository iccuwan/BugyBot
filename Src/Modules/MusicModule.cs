using Discord.Commands;
using Discord.Audio;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using Discord;
using YoutubeExplode.Videos.Streams;
using System.IO;
using BugyBot.Services;
using System;
using System.Web;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BugyBot.Modules
{
	[Name("Music")]
	[Summary("Playing music")]
	public class MusicModule : ModuleBase<SocketCommandContext>
	{
		private readonly MusicService musicService;
		private readonly LocalizationService local;

		public MusicModule(MusicService ms, LocalizationService ls)
		{
			musicService = ms;
			local = ls;
		}

		[Command("restart")] // NOT WORKING IN LINUX SCREEN
		public async Task Restart()
		{
			await ReplyAsync(local.Phrase("Restart"));
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // In Linux the service will autorestart app
			{
				var info = new ProcessStartInfo
				{
					FileName = Process.GetCurrentProcess().ProcessName
				};
				Process.Start(info);
			}
			Environment.Exit(0);
		}

		[Command("play", RunMode = RunMode.Async)]
		[Summary("Playing music from youtube url")]
		public async Task AddToQueryAsync([Remainder]string url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				await ReplyAsync(local.Phrase("NotFound"));
				return;
			}
			if (!musicService.IsYoutubeLink(url))
			{
				await ReplyAsync(string.Format(local.Phrase("Searching"), url));
				url = await musicService.FindYoutube(url);
				if (url == null)
				{
					await ReplyAsync(local.Phrase("NotFound"));
					return;
				}
			}
			MusicData md = new MusicData
			{
				url = url,
				type = VoiceType.YOUTUBE,
				context = Context
			};
			_ = musicService.AddMusicToQuery(md);
			await ReplyAsync(local.Phrase("TrackAdded"));
		}

		[Command("s", RunMode = RunMode.Async)]
		public async Task AddTTSToQueue(string text)
		{
			MusicData md = new MusicData
			{
				url = text,
				type = VoiceType.TTS,
				context = Context
			};
			await ReplyAsync(local.Phrase("TrackAdded"));
			await musicService.AddMusicToQuery(md);
		}

		[Command("playlist", RunMode = RunMode.Async)]
		public async Task AddPlaylistToQuery(string url)
		{
			if (string.IsNullOrEmpty(url) || !musicService.IsYoutubeLink(url))
			{
				await ReplyAsync(local.Phrase("NotFound"));
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
			string reply = musicService.Loop ? local.Phrase("LoopEnabled") : local.Phrase("LoopDisabled");
			return ReplyAsync(reply);
		}

		[Command("volume")]
		public async Task SetVolume(float _volume)
		{
			if (_volume > 0 && _volume <= 10)
			{
				musicService.Volume = _volume;
				await ReplyAsync(string.Format(local.Phrase("Volume"), _volume));
			}
			else
			{
				await ReplyAsync(local.Phrase("VolumeInvalid"));
			}
		}

		[Command("speed")]
		public Task SetSpeed(float _speed)
		{
			if (_speed >= 0.5f && _speed <= 2.0f)
			{
				musicService.Speed = _speed;
				return ReplyAsync(string.Format(local.Phrase("Speed"), _speed));
			}
			return ReplyAsync(local.Phrase("SpeedInvalid"));
		}

		[Command("reverse")]
		public Task SetReverse()
		{
			musicService.Reverse = !musicService.Reverse;
			string reply = musicService.Reverse ? local.Phrase("ReverseEnabled") : local.Phrase("ReverseDisabled");
			return ReplyAsync(reply);
		}

		[Command("skip")]
		public async Task Skip()
		{
			await ReplyAsync(local.Phrase("TrackSkiped"));
			await musicService.ProcessedNextTrackAsync(true);
		}

		[Command("queue")]
		public async Task Queue()
		{
			string response = local.Phrase("Query");
			int i = 0;
			foreach (MusicData md in musicService.Queue)
			{
				response += $"{i}:{md.url}\n";
				i++;
			}
			await ReplyAsync(response);
		}
	}
}
