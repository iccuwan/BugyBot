using CliWrap;
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
using System.Threading.Tasks;
using System.Timers;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace BurningCrusadeMusic.Services
{
	public class MusicService
	{
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;
		private readonly IServiceProvider provider;

		public List<MusicData> Query { get; private set; }
		private readonly YoutubeClient youtube = new YoutubeClient();

		public float Volume { get; set; }
		public float Speed { get; set; }
		public bool Reverse { get; set; }
		private bool isPlaying = false;
		private MusicData playingNow;

		private IAudioClient audioClient;
		private IVoiceChannel channel;
		private Timer disconnectTimer;
		private MemoryStream buffer;
		private AudioOutStream voiceStream;


		public MusicService(DiscordSocketClient _discord, CommandService _commands, IConfigurationRoot _config, IServiceProvider _provider)
		{
			discord = _discord;
			commands = _commands;
			config = _config;
			provider = _provider;

			Volume = 1;
			Speed = 1;
			Reverse = false;

			buffer = new MemoryStream();
			Query = new List<MusicData>();

			disconnectTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
			disconnectTimer.AutoReset = false;
			disconnectTimer.Elapsed += DisconnectFromVoice;
		}

		public async Task AddMusicToQuery(MusicData md)
		{
			Query.Add(md);
			IVoiceChannel _channel = (md.context.User as IGuildUser)?.VoiceChannel;
			if (_channel == null)
			{
				await md.context.Channel.SendMessageAsync("Нужно быть в голосовом канале, чтобы использовать эту команду");
				return;
			}
			if (Query.Count == 1)
			{
				await ProcessedNextTrackAsync();
			}
		}

		private async Task PlayMusic(MusicData md)
		{
			try
			{
				playingNow = md;
				isPlaying = true;
				channel = channel ?? (md.context.User as IGuildUser)?.VoiceChannel;
				await JoinToVoiceAsync(channel);

				var video = await youtube.Videos.GetAsync(md.url);
				if (video.Duration > TimeSpan.FromMinutes(10))
				{
					await md.context.Channel.SendMessageAsync("Не больше 10 минут пока что");
					await ProcessedNextTrackAsync(true);
					return;

				}
				var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
				var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
				var stream = await youtube.Videos.Streams.GetAsync(streamInfo);
				

				await md.context.Channel.SendMessageAsync($"Играю {video.Title} (Скорость: {Speed} Громкость: {Volume} Реверс: {Reverse})");

				string args = $" -hide_banner -loglevel panic -i pipe:0 -af volume={Volume} -af atempo={Speed}";
				if (Reverse)
				{
					args += " -af areverse";
				}
				args += " -ac 2 -f s16le -ar 48000 pipe:1";

				await Cli.Wrap("ffmpeg")
					.WithArguments(args)
					.WithStandardInputPipe(PipeSource.FromStream(stream))
					.WithStandardOutputPipe(PipeTarget.ToStream(buffer))
					.ExecuteAsync();
				await stream.DisposeAsync();
				try
				{
					if (voiceStream != null)
					{
						await voiceStream.DisposeAsync();
						voiceStream = null;
					}
					voiceStream = audioClient.CreatePCMStream(AudioApplication.Mixed);
					await voiceStream.WriteAsync(buffer.ToArray().AsMemory(0, (int)buffer.Length));
					isPlaying = true;
				}
				finally
				{
					isPlaying = false;
					await ProcessedNextTrackAsync();
				}
				isPlaying = false;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public async Task ProcessedNextTrackAsync(bool skip = false)
		{
			if (voiceStream != null || skip)
			{
				await voiceStream.DisposeAsync();
				buffer.SetLength(0);
				Query.Remove(playingNow);
				voiceStream = null;
			}
			if (Query.Count == 0)
			{
				disconnectTimer.Start();
			}
			else
			{
				if (disconnectTimer.Enabled)
				{
					disconnectTimer.Stop();
				}
				await PlayMusic(Query[0]);
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

		private Task AudioClient_Disconnected(System.Exception arg)
		{
			//audioClient.Dispose();
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
			if (videos[0] == null)
			{
				return null;
			}
			return videos[0].Id;
		}
	}
	public struct MusicData
	{
		public string url;
		public SocketCommandContext context;
	}
}
