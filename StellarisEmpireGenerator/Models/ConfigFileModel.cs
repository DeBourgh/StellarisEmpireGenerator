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
				@"common\species_classes"
			};
	}
}
