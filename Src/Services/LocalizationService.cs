using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BugyBot.Services
{
	public class LocalizationService
	{
		private readonly IConfigurationRoot config;
		private Dictionary<string, string> phrases;

		public LocalizationService(IConfigurationRoot _config)
		{
			config = _config;

			string localFilePath = $"localization/{config["lang"]}.json";
			if (!File.Exists(localFilePath))
			{
				throw new FileNotFoundException($"{localFilePath} not found");
			}
			string localJson = File.ReadAllText(localFilePath);
			phrases = JsonSerializer.Deserialize<Dictionary<string, string>>(localJson);
		}

		public string Phrase(string key)
		{
			if (phrases.ContainsKey(key))
			{
				return phrases[key];
			}
			return $"Phrase key {key} not found in lang file";
		}
	}
}
