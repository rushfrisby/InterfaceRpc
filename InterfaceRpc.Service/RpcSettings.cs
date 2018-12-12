using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace InterfaceRpc.Service
{
	public class RpcSettings
	{
		private const string SettingsFileName = "rpcsettings.json";

		public static RpcSettings Load(string settingsFileName = null)
		{
			if(!string.IsNullOrWhiteSpace(settingsFileName))
			{
				var userFile = new FileInfo(settingsFileName);
				if (!userFile.Exists)
				{
					throw new FileNotFoundException($"RPC Settings file not found", userFile.FullName);
				}
			}

			var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var dllDirectory = (new FileInfo(dllPath)).DirectoryName;
			var settingFilePath = Path.Combine(dllDirectory, SettingsFileName);
			if(!File.Exists(settingFilePath))
			{
				var altFile = new FileInfo(SettingsFileName);
				if(!altFile.Exists)
				{
					throw new FileNotFoundException($"RPC Settings file not found. Checked '{settingFilePath}' and '{altFile.FullName}'", settingFilePath);
				}
				settingFilePath = altFile.FullName;
			}
			var json = File.ReadAllText(settingFilePath);
			return JsonConvert.DeserializeObject<RpcSettings>(json);
		}

		public IEnumerable<string> WebServerPrefixes { get; set; }

		public int MaxConnections { get; set; }
	}
}
