using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace StellarisEmpireGenerator.Models
{
	public class ConfigModel
	{
		public ConfigModel()
		{

		}

		public string InstallationDir { get; set; } = @"..\steamapps\common\Stellaris";
		public string ModDir { get; set; } = @"..\steamapps\workshop\content\281990";
		//public string[] LocalizationDirs { get; set; } = new string[]
		//	{
		//		@"localisation\english",
		//		@"localisation\german"
		//	};
		public Dictionary<string, string> Localizations { get; set; } = new Dictionary<string, string>
			{
				{ "English", @"localisation\english" },
				{ "German", @"localisation\german" },

			};
		public string UsedLocalization { get; set; } = "English";

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

		public static void SaveToFile(ConfigModel Model,string Path)
		{
			string output = JsonConvert.SerializeObject(Model, Formatting.Indented);

			File.WriteAllText(Path, output, System.Text.Encoding.UTF8);
		}
	}
}
