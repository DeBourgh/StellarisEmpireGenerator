using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Input;
using StellarisEmpireGenerator.Core;
using StellarisEmpireGenerator.Core.EmpireProperties;

using StellarisEmpireGenerator.ObjectModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Newtonsoft.Json;
using StellarisEmpireGenerator.Core.ObjectModel;

namespace StellarisEmpireGenerator.ViewModels
{
	public class MainViewModel : ObservableObject, IDataErrorInfo
	{
		private const string _configFileName = "StellarisEmpireGenerator.config";
		private const string _modelFileName = "StellarisEmpireGenerator.model";
		private const string _langFileName = "StellarisEmpireGenerator.lang.{0}";

		private readonly ConfigModel _config = null;

		public MainViewModel()
		{
			if (File.Exists(_configFileName))
				_config = ConfigModel.LoadFromFile(_configFileName);
			else
				_config = ConfigModel.ByDefault();

			LoadFromConfig();

			var props = PropertiesFromLocalFile();
			ApplyProperties(props);

			var dict = LanguageFromLocalFile();
			ApplyLanguageDictionary(dict);

			PropertyChanged += LanguageChanged;
		}

		private void LoadFromConfig()
		{
			InstallationDirectory = _config.InstallationDir;
			ModDirectory = _config.ModDir;

			LocalizationOptions = new ObservableCollection<string>(_config.LocalizationKeys);
			ActiveLocalizationKey = _config.ActiveLocalizationKey;
		}

		private void SaveIntoConfig()
		{
			_config.InstallationDir = InstallationDirectory;
			_config.ModDir = ModDirectory;

			_config.ActiveLocalizationKey = ActiveLocalizationKey;
		}

		private void PropertiesToLocalFile(IEnumerable<EmpireProperty> Properties)
		{
			if (Properties == null || !Properties.Any())
				return;

			EmpireProperty.ToFile(Properties.ToList(), string.Format(_modelFileName));
		}

		private IList<EmpireProperty> PropertiesFromLocalFile()
		{
			if (File.Exists(_modelFileName))
			{
				try
				{
					var properties = EmpireProperty.FromFile(_modelFileName);

					if (properties != null)
						return properties;
				}
				catch (Exception e)
				{
					string trace = e.StackTrace;
				}

				// File seems to be corrupt, so delete it
				File.Delete(_modelFileName);
				return null;
			}
			else
				return null;
		}

		private void LanguageToLocalFile()
		{
			string content = JsonConvert.SerializeObject(LangDict);

			File.WriteAllText(string.Format(_langFileName, LocalizationKeyLangDict), content, System.Text.Encoding.UTF8);

		}
		private IDictionary<string, string> LanguageFromLocalFile()
		{
			string langFile;
			if (File.Exists(langFile = string.Format(_langFileName, ActiveLocalizationKey)))
			{
				string content = File.ReadAllText(langFile, System.Text.Encoding.UTF8);

				try
				{
					Dictionary<string, string> langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
					return langDict;
				}
				catch
				{
					// File seems to be corrupt, so delete it
					File.Delete(langFile);
					return null;
				}
			}
			else
				return null;
		}

		private IEnumerable<EmpirePropertyViewModel> CollectAllPropertyViewModels()
		{
			return Authorities.Concat(Civics).Concat(Ethics).Concat(Origins).Concat(Traits).Concat(Species);
		}
		private IEnumerable<EmpirePropertyViewModel> GetWeightedPropertyViewModels()
		{
			var allowedProperties = CollectAllPropertyViewModels().Where(p => p.IsAllowed);
			return allowedProperties.SelectMany(a => Enumerable.Repeat(a, a.Weight));
		}
		private IEnumerable<string> CollectMechanicFiles(string RootFolder)
		{
			return
				_config.SubDirs
				.Select(x => string.Concat(RootFolder, x))
				.Where(x => Directory.Exists(x))
				.SelectMany(x => Directory.EnumerateFiles(x));
		}
		private IEnumerable<string> CollectFilePathsGameMechanics()
		{
			IEnumerable<string>
				installFolderFiles = Enumerable.Empty<string>(),
				modFolderFiles = Enumerable.Empty<string>();

			if (StellarisInstallationFolderExists)
				installFolderFiles = CollectMechanicFiles(InstallationDirectory);

			if (StellarisModFolderExists)
			{
				modFolderFiles = Directory.EnumerateDirectories(ModDirectory)
					.Select(dir => dir.NormDirectory())
					.SelectMany(x => CollectMechanicFiles(x));
			}

			List<string> files = new List<string>();

			files.AddRange(installFolderFiles);
			files.AddRange(modFolderFiles);

			return files;
		}

		private IEnumerable<string> CollectLanguageFilePaths(string Path)
		{
			return _config.LocalizationDirs
				.Select(d => Path.NormDirectory() + d)
				.Concat(
					_config.LocalizationDirs
					.Select(d => (Path.NormDirectory() + d).NormDirectory() + ActiveLocalizationKey))
				.Where(e => Directory.Exists(e))
				.SelectMany(d => Directory.EnumerateFiles(d))
				.Where(p => p.EndsWith("yml"));
		}
		private IEnumerable<string> CollectLanguageFilePaths()
		{
			var languageFiles = new List<string>();

			if (StellarisInstallationFolderExists)
				languageFiles.AddRange(CollectLanguageFilePaths(InstallationDirectory));

			if (StellarisModFolderExists)
				languageFiles.AddRange(Directory.EnumerateDirectories(ModDirectory)
					.Select(dir => dir.NormDirectory())
					.SelectMany(x => CollectLanguageFilePaths(x)));

			return languageFiles;
		}

		#region Properties

		private string _stellarisInstallationFolder;
		public bool StellarisInstallationFolderExists { get { return Directory.Exists(InstallationDirectory); } }
		public string InstallationDirectory
		{
			get { return _stellarisInstallationFolder; }
			set { SetProperty(ref _stellarisInstallationFolder, value.NormDirectory()); }
		}

		private string _stellarisModFolder;
		public bool StellarisModFolderExists { get { return Directory.Exists(ModDirectory); } }
		public string ModDirectory
		{
			get { return _stellarisModFolder; }
			set { SetProperty(ref _stellarisModFolder, value.NormDirectory()); }
		}

		public ObservableCollection<string> LocalizationOptions { get; set; }

		private string _activeLocalizationKey;
		public string ActiveLocalizationKey
		{
			get { return _activeLocalizationKey; }
			set { SetProperty(ref _activeLocalizationKey, value); }
		}

		private string _localizationKeyLangDict = string.Empty;
		public string LocalizationKeyLangDict
		{
			get => _localizationKeyLangDict;
			set { SetProperty(ref _localizationKeyLangDict, value); }
		}

		private IDictionary<string, string> LangDict { get; set; }

		public ObservableCollection<EmpirePropertyViewModel> Authorities { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Civics { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Ethics { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Origins { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Traits { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Species { get; } = new ObservableCollection<EmpirePropertyViewModel>();

		public ObservableCollection<EmpireViewModel> Empires { get; } = new ObservableCollection<EmpireViewModel>();

		private ICommand _reloadCommand;
		public ICommand ReloadCommand
		{
			get
			{
				if (_reloadCommand == null)
				{
					_reloadCommand = new RelayCommand(
						p => StellarisInstallationFolderExists,
						p => Reload());
				}

				return _reloadCommand;
			}
		}

		private ICommand _allowAllCommand;
		public ICommand AllowAllCommand
		{
			get
			{
				if (_allowAllCommand == null)
				{
					_allowAllCommand = new RelayCommand(
						p => true,
						p => AllowAll(p));
				}

				return _allowAllCommand;
			}
		}

		private ICommand _disallowAllCommand;
		public ICommand DisallowAllCommand
		{
			get
			{
				if (_disallowAllCommand == null)
				{
					_disallowAllCommand = new RelayCommand(
						p => true,
						p => DisallowAll(p));
				}

				return _disallowAllCommand;
			}
		}

		private ICommand _generateCommand;
		public ICommand GenerateCommand
		{
			get
			{
				if (_generateCommand == null)
				{
					_generateCommand = new RelayCommand(p => true, p => GenerateEmpires((int)p));
				}

				return _generateCommand;
			}
		}

		private ICommand _loadLanguageCommand;
		public ICommand LoadLanguageCommand
		{
			get
			{
				if (_loadLanguageCommand == null)
				{
					_loadLanguageCommand = new RelayCommand(p => true, p => LoadLanguage());
				}

				return _loadLanguageCommand;
			}
		}

		private int _generateCount = 1;
		public int GenerateCount
		{
			get => _generateCount;
			set { SetProperty(ref _generateCount, value); }
		}

		#endregion

		#region Command Methods

		private void Reload()
		{
			LocalizationKeyLangDict = ActiveLocalizationKey;
			try
			{
				var props = PropertiesFromGameFiles();

				PropertiesToLocalFile(props);
				ApplyProperties(props);
				LoadLanguage();
			}
			catch (Exception e)
			{
				;
			}
		}

		private void LoadLanguage()
		{
			var dict = LanguageDictionaryFromGameFiles();
			ApplyLanguageDictionary(dict);
			LanguageToLocalFile();
		}

		private IEnumerable<EmpireProperty> PropertiesFromGameFiles(bool SerializeLocallyOnLoad = true)
		{
			var mechPaths = CollectFilePathsGameMechanics();
			Entity root = Entity.FromFiles(mechPaths);

			var props = EmpireProperty.FromEntityRoot(root);

			if (SerializeLocallyOnLoad)
				PropertiesToLocalFile(props);

			return props;
		}

		private void ApplyProperties(IEnumerable<EmpireProperty> Properties)
		{
			if (Properties == null)
				return;

			Authorities.Clear();
			Origins.Clear();
			Civics.Clear();
			Ethics.Clear();
			Traits.Clear();
			Species.Clear();

			foreach (var prop in Properties.OrderBy(p => $"{p.Type}_{p.Identifier}"))
			{
				var epvm = new EmpirePropertyViewModel(prop);

				switch (prop.Type)
				{
					case EmpirePropertyType.Origin:
						Origins.Add(epvm);
						break;
					case EmpirePropertyType.Civics:
						Civics.Add(epvm);
						break;
					case EmpirePropertyType.Authority:
						Authorities.Add(epvm);
						break;
					case EmpirePropertyType.Trait:
						Traits.Add(epvm);
						break;
					case EmpirePropertyType.Species:
						Species.Add(epvm);
						break;
					case EmpirePropertyType.Ethics:
						Ethics.Add(epvm);
						break;
				}
			}
		}

		private void ApplyLanguageDictionary(IDictionary<string, string> Dict)
		{
			if (Dict == null)
			{
				foreach (var propVm in CollectAllPropertyViewModels())
					propVm.Name = propVm.Source.Identifier;

				foreach (var empireVm in Empires)
					empireVm.ApplyLanguageDict(null);

				return;
			}

			LangDict = Dict;
			LocalizationKeyLangDict = ActiveLocalizationKey;

			foreach (var propVm in CollectAllPropertyViewModels())
			{
				if (LangDict.TryGetValue(propVm.Source.Identifier, out string value))
					propVm.Name = value;
				else
					propVm.Name = propVm.Source.Identifier;
			}

			foreach (var empireVm in Empires)
				empireVm.ApplyLanguageDict(LangDict);
		}
		private IDictionary<string, string> LanguageDictionaryFromGameFiles(bool SerializeLocallyOnLoad = true)
		{
			if (!CollectAllPropertyViewModels().Any())
				return null;

			var langPaths = CollectLanguageFilePaths();
			var langDict = LanguageDictionary.FromConfigModel(langPaths, ActiveLocalizationKey);

			var langDictReduced = langDict.ReduceDictionary(CollectAllPropertyViewModels().Select(p => p.Source.Identifier));

			if (SerializeLocallyOnLoad)
				LanguageToLocalFile();

			return langDictReduced;
		}

		private void ChangeAllowAll(object Parameter, bool Value)
		{
			var e = (EmpirePropertyType)Parameter;

			IEnumerable<EmpirePropertyViewModel> epvm = Enumerable.Empty<EmpirePropertyViewModel>();

			switch (e)
			{
				case EmpirePropertyType.Origin:
					epvm = Origins;
					break;
				case EmpirePropertyType.Civics:
					epvm = Civics;
					break;
				case EmpirePropertyType.Authority:
					epvm = Authorities;
					break;
				case EmpirePropertyType.Trait:
					epvm = Traits;
					break;
				case EmpirePropertyType.Species:
					epvm = Species;
					break;
				case EmpirePropertyType.Ethics:
					epvm = Ethics;
					break;
			}

			foreach (var p in epvm)
				p.IsAllowed = Value;
		}
		private void AllowAll(object Parameter)
		{
			ChangeAllowAll(Parameter, true);
		}
		private void DisallowAll(object Parameter)
		{
			ChangeAllowAll(Parameter, false);
		}

		private void GenerateEmpires(int Count)
		{
			var props = GetWeightedPropertyViewModels().ToList();

			var empires = EmpireProperty.GenerateSolution(props.Select(p => p.Source), GenerateCount);

			Empires.Clear();

			foreach (var empire in empires)
			{
				EmpireViewModel empireVm = new EmpireViewModel(
					empire.First(e => e.Type == EmpirePropertyType.Authority),
					empire.Where(e => e.Type == EmpirePropertyType.Civics),
					empire.Where(e => e.Type == EmpirePropertyType.Ethics),
					empire.First(e => e.Type == EmpirePropertyType.Origin),
					empire.First(e => e.Type == EmpirePropertyType.Species),
					empire.Where(e => e.Type == EmpirePropertyType.Trait)
				);

				empireVm.ApplyLanguageDict(LangDict);
				Empires.Add(empireVm);
			}

		}

		#endregion

		public void Save()
		{
			SaveIntoConfig();
			ConfigModel.SaveToFile(_config, _configFileName);

			PropertiesToLocalFile(CollectAllPropertyViewModels().Select(p => p.Source));
			LanguageToLocalFile();
		}

		#region IDataErrorInfoImplementation

		public Dictionary<string, string> ErrorDict { get; } = new Dictionary<string, string>();

		public string this[string PropertyName]
		{
			get
			{
				string message = null;

				switch (PropertyName)
				{
					case nameof(InstallationDirectory):
						if (!StellarisInstallationFolderExists) { message = "The given folder does not exist."; }
						break;
					case nameof(ModDirectory):
						if (!StellarisModFolderExists) { message = "The given folder does not exist."; }
						break;
				}


				ErrorDict[PropertyName] = message;
				if (message == null)
					ErrorDict.Remove(PropertyName);

				OnPropertyChanged(nameof(ErrorDict));

				return message;
			}
		}
		public string Error { get { return null; } }

		#endregion

		#region Events

		private void LanguageChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ActiveLocalizationKey))
			{
				var langDict = LanguageFromLocalFile();

				if (langDict != null)
					ApplyLanguageDictionary(langDict);
			}
		}

		#endregion
	}
}