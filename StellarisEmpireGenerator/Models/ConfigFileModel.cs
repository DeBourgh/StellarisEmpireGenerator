using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace StellarisEmpireGenerator.ObjectModel
{
	public class ConfigModel
	{
		private ConfigModel()
		{

		}

		public string InstallationDir { get; set; } = @"..\steamapps\common\Stellaris";
		public string ModDir { get; set; } = @"..\steamapps\workshop\content\281990";

		public ICollection<string> LocalizationDirs { get; } = new List<string>();

		public IList<string> LocalizationKeys { get; } = new List<string>();

		public string ActiveLocalizationKey { get; set; } = "english";


		public string[] SubDirs { get; set; } = new string[]
			{
				@"common\governments",
				@"common\governments\authorities",
				@"common\governments\civics",
				@"common\species_archetypes",
				@"common\species_classes",
				@"common\traits",
				@"common\ethics"
			};

		public static ConfigModel LoadFromFile(string Path)
		{
			string input = File.ReadAllText(Path);

			return JsonConvert.DeserializeObject<ConfigModel>(input);
		}

		public static void SaveToFile(ConfigModel Model, string Path)
		{
			string output = JsonConvert.SerializeObject(Model, Formatting.Indented);

			File.WriteAllText(Path, output, System.Text.Encoding.UTF8);
		}

		public static ConfigModel ByDefault()
		{
			ConfigModel cm = new ConfigModel();
			cm.LocalizationDirs.Add("localisation");
			cm.LocalizationDirs.Add("localisation_synced");
			cm.LocalizationKeys.Add("english");
			cm.LocalizationKeys.Add("german");

			return cm;
		}
	}
}
