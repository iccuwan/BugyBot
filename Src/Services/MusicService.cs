using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace BugyBot.Services
{
	public class MusicService
	{
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;
		private readonly IServiceProvider provider;
		private readonly LocalizationService local;

		public List<MusicData> Queue { get; private set; }
		private readonly YoutubeClient youtube = new YoutubeClient();

		public float Volume { get; set; }
		public float Speed { get; set; }
		public bool Reverse { get; set; }
		public bool Loop { get; set; }
		private MusicData playingNow;

		private IAudioClient audioClient;
		private IVoiceChannel channel;
		private System.Timers.Timer disconnectTimer;
		private AudioOutStream voiceStream;
		private CancellationTokenSource cancelTaskToken;
		private Process ffmpeg;


		public MusicService(DiscordSocketClient _discord, CommandService _commands, IConfigurationRoot _config, IServiceProvider _provider, LocalizationService _local)
		{
			discord = _discord;
			commands = _commands;
			config = _config;
			provider = _provider;
			local = _local;

			Volume = 1;
			Speed = 1;
			Reverse = false;
			Loop = false;

			Queue = new List<MusicData>();

			disconnectTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
			disconnectTimer.AutoReset = false;
			disconnectTimer.Elapsed += DisconnectFromVoice;
		}

		public async Task AddMusicToQuery(MusicData md, bool nextTrack = true)
		{
			Queue.Add(md);
			IVoiceChannel _channel = (md.context.User as IGuildUser)?.VoiceChannel;
			if (_channel == null)
			{
				await md.context.Channel.SendMessageAsync(local.Phrase("NeedBeInVoice"));
				return;
			}
			if (Queue.Count == 1 && nextTrack)
			{
				await ProcessedNextTrackAsync();
			}
		}

		public async Task AddPlaylistToQueryAsync(string playlistId, SocketCommandContext context)
		{
			var videos = await youtube.Playlists.GetVideosAsync(playlistId);
			foreach(var video in videos)
			{
				MusicData md = new MusicData
				{
					url = video.Url,
					context = context
				};
				await AddMusicToQuery(md, false);
			}
			await context.Channel.SendMessageAsync(string.Format(local.Phrase("PlaylistAdded"), videos.Count));
			await ProcessedNextTrackAsync();
		}

		private async Task PlayMusic(MusicData md)
		{
			try
			{

				var video = await youtube.Videos.GetAsync(md.url);
				var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
				var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
				var stream = await youtube.Videos.Streams.GetAsync(streamInfo);

				await md.context.Channel.SendMessageAsync(string.Format(local.Phrase("Playing"), video.Title, Speed, Volume, Reverse));

				await SendVoiceAsync(stream, md);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				await md.context.Channel.SendMessageAsync(local.Phrase("TrackLoadFailed"));
			}
			finally
			{
				await ProcessedNextTrackAsync(true);
			}
		}

		private async Task PlayTTS(MusicData md)
		{
			try
			{
				string args = "-o rhvoice -l ru -e -t male1";
				var info = new ProcessStartInfo
				{
					FileName = "spd-say",
					Arguments = args,
					RedirectStandardOutput = true,
					RedirectStandardInput = true
				};
				Process spd = new Process();
				spd.StartInfo = info;
				spd.Start();
				spd.StandardInput.Write(md.url);
				spd.StandardInput.Close();
				await SendVoiceAsync(spd.StandardOutput.BaseStream, md);
			}
			catch (Exception e)
			{

			}
		}

		public async Task SendVoiceAsync(Stream stream, MusicData md)
		{
			playingNow = md;
			channel = channel ?? (md.context.User as IGuildUser)?.VoiceChannel;
			await JoinToVoiceAsync(channel);


			string args = $" -hide_banner -loglevel panic -i pipe:0 -af volume={Volume} -af atempo={Speed}";
			if (Reverse)
			{
				args += " -af areverse";
			}
			args += " -ac 2 -f s16le -ar 48000 pipe:1";

			var info = new ProcessStartInfo
			{
				FileName = "ffmpeg",
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			ffmpeg = new Process();
			ffmpeg.StartInfo = info;
			ffmpeg.EnableRaisingEvents = true;
			ffmpeg.Start();

			if (voiceStream != null)
			{
				await voiceStream.DisposeAsync();
				voiceStream = null;
			}
			voiceStream = audioClient.CreatePCMStream(AudioApplication.Mixed);

			cancelTaskToken = new CancellationTokenSource();

			var inputTask = Task.Run(() =>
			{
				stream.CopyTo(ffmpeg.StandardInput.BaseStream);
				ffmpeg.StandardInput.Close();
			}, cancelTaskToken.Token);

			var outputTask = Task.Run(() =>
			{
				ffmpeg.StandardOutput.BaseStream.CopyTo(voiceStream);
			}, cancelTaskToken.Token);

			Task.WaitAll(inputTask, outputTask);
			ffmpeg.WaitForExit();
		}

		public async Task ProcessedNextTrackAsync(bool skip = false)
		{
			if (voiceStream != null || skip)
			{
				if (voiceStream != null)
				{
					await voiceStream.DisposeAsync();
					cancelTaskToken.Cancel();
				}
				if (!Loop || skip)
				{
					Queue.Remove(playingNow);
				}
				voiceStream = null;
				if (ffmpeg != null && !ffmpeg.HasExited)
				{
					ffmpeg.Kill();
				}
			}
			if (Queue.Count == 0)
			{
				disconnectTimer.Start();
			}
			else
			{
				if (disconnectTimer.Enabled)
				{
					disconnectTimer.Stop();
				}

				switch(Queue[0].type)
				{
					case VoiceType.YOUTUBE:
						await PlayMusic(Queue[0]);
						break;
					case VoiceType.TTS:
						await PlayTTS(Queue[0]);
						break;
				}
			}
		}

		private void DisconnectFromVoice(object sender, ElapsedEventArgs e)
		{
			_ = DisconnectFromVoiceAsync();
		}

		private async Task DisconnectFromVoiceAsync()
		{
			await audioClient.StopAsync();
		}

		private async Task JoinToVoiceAsync(IVoiceChannel _channel)
		{
			if (audioClient == null || audioClient.ConnectionState != ConnectionState.Connected)
			{
				try
				{
					audioClient = await _channel.ConnectAsync();
					audioClient.Disconnected += AudioClient_Disconnected;
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}

		private Task AudioClient_Disconnected(Exception arg)
		{
			return Task.CompletedTask;
		}

		public bool IsYoutubeLink(string url)
		{
			try
			{
				HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
				request.Method = "HEAD";
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					return response.ResponseUri.ToString().Contains("youtube.com");
				}
			}
			catch
			{
				return false;
			}
		}

		public async Task<string> FindYoutube(string request)
		{
			var videos = await youtube.Search.GetVideosAsync(request);
			if (videos.Count == 0)
			{
				return null;
			}
			return videos[0].Id;
		}
	}
	public struct MusicData
	{
		public string url;
		public VoiceType type;
		public SocketCommandContext context;
	}

	public enum VoiceType
	{
		YOUTUBE,
		TTS
	}
}
