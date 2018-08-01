using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace InterfaceRpc.Service
{
	public class Settings
	{
		private const string SettingsFileName = "rpcsettings.json";

		private static readonly Lazy<Settings> lazy = new Lazy<Settings>(LoadSettings);

		public static Settings Instance { get { return lazy.Value; } }

		private Settings()
		{
		}

		private static Settings LoadSettings()
		{
			var json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName));
			return JsonConvert.DeserializeObject<Settings>(json);
		}

		public IEnumerable<string> WebServerPrefixes { get; set; }

		public int MaxConnections { get; set; }
	}
}
