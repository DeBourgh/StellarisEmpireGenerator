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

using StellarisEmpireGenerator.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Newtonsoft.Json;

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
				catch { }

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

			foreach (var prop in Properties)
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

				return;
			}

			LangDict = Dict;
			LocalizationKeyLangDict = ActiveLocalizationKey;

			foreach (var propVm in CollectAllPropertyViewModels())
				propVm.Name = LangDict[propVm.Source.Identifier] ?? propVm.Source.Identifier;

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

			try
			{
				var generatedEmpires = new List<EmpirePropertyViewModel>();
				for (int i = 0; i < Count; i++)
				{
					GeneratorNode next = GeneratorNode.CreateRoot(props);
					while (true)
					{
						var node = next.Iterate();

						if (node == null)
							break;
					}
				}
			}
			catch (Exception e)
			{
				;
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

		public class GeneratorNode
		{
			private static readonly Random Rnd = new Random(Environment.TickCount);

			private GeneratorNode(IEnumerable<EmpirePropertyViewModel> WeightedSource)
			{
				Solution = new List<EmpirePropertyViewModel>();
				Properties = WeightedSource.OrderBy(p => $"{p.Source.Type}_{p.Source.Identifier}").ToList();
				RemainingIndexes = Enumerable.Range(0, WeightedSource.Count()).ToList();
			}

			private GeneratorNode(GeneratorNode Parent, EmpirePropertyViewModel PickedProperty)
			{
				this.Parent = Parent;

				Solution = new List<EmpirePropertyViewModel>(Parent.Solution)
				{
					PickedProperty
				};

				Properties = Parent.Properties;
				RemainingIndexes = new List<int>(Parent.RemainingIndexes);

				HasAuthority = Parent.HasAuthority;
				CivicPointsAvailable = Parent.CivicPointsAvailable;
				EthicPointsAvailable = Parent.EthicPointsAvailable;
				HasOrigin = Parent.HasOrigin;
				HasSpecies = Parent.HasSpecies;
				TraitPointsAvailable = Parent.TraitPointsAvailable;
			}

			public GeneratorNode Parent { get; private set; }

			public bool IsSolution { get => HasAuthority && HasCivics && HasEthics && HasOrigin && HasSpecies && HasTraits; }

			private bool HasAuthority { get; set; }

			private int CivicPointsAvailable { get; set; } = 2;
			private bool HasCivics { get => CivicPointsAvailable == 0; }

			private int EthicPointsAvailable { get; set; } = 3;
			private bool HasEthics { get => EthicPointsAvailable == 0; }
			private bool HasOrigin { get; set; }
			private bool HasSpecies { get; set; }

			private int TraitPointsAvailable { get; set; } = -1;
			private bool HasTraits { get => TraitPointsAvailable == 0; }

			//private int CivicsCount { get; private set; }
			public ObservableCollection<EmpireViewModel> Empires { get; } = new ObservableCollection<EmpireViewModel>();

			public ICollection<EmpirePropertyViewModel> Solution { get; }

			public List<EmpirePropertyViewModel> Properties { get; private set; }

			private EmpirePropertyViewModel CurrentPick { get; set; }

			public List<EmpirePropertyViewModel> RemainingProperties { get { return RemainingIndexes.Select(i => Properties[i]).ToList(); } }

			//public Dictionary<int, EmpirePropertyViewModel> g;

			public List<int> RemainingIndexes { get; }

			//public IList<int> AttemptedPicks { get; }

			//private bool HasPotentMatches()
			//{
			//	return false;
			//}
			private bool HasPotentMatches(EmpirePropertyViewModel ToMatch)
			{
				var toMatch = ToMatch.Source;
				var types = toMatch.Constraints.SubConstraints.Select(c => c.Group).Where(t => t != EmpirePropertyType.Unknown).ToList();
				int typesCount = types.Count;
				int remainingCount = RemainingIndexes.Count;

				bool typeMatchFound = false;
				bool eachTypeMatchFound = true;

				var currentTypeIndex = 0;
				bool isComparing = false;
				int i = 0;
				while ((currentTypeIndex < typesCount) && (i < remainingCount))
				{
					var prop = Properties[RemainingIndexes[i]];
					var type = types[currentTypeIndex];

					int cmp;

					if ((cmp = prop.Source.Type.CompareTo(type)) == 0)
					{
						i++;

						if (typeMatchFound)
							continue;

						isComparing = true;

						bool e = Matches(ToMatch, prop);
						typeMatchFound |= e;
					}
					else
					{
						if (cmp < 0)
							i++;
						else
							currentTypeIndex++;

						if (isComparing)
						{
							eachTypeMatchFound &= typeMatchFound;
							typeMatchFound = false;

							isComparing = false;

							continue;
						}
					}
				}

				return eachTypeMatchFound;
			}

			private bool Matches(EmpirePropertyViewModel ToMatch, EmpirePropertyViewModel Matching)
			{
				var toMatch = ToMatch.Source;
				var matching = Matching.Source;

				foreach (var cons in toMatch.Constraints.SubConstraints)
				{
					if (matching.Type != cons.Group)
						continue;
					else
					{
						bool e = cons.Evaluate(matching, (p1, p2) => p1.Identifier == p2.Identifier);
						if (e)
							return true;
					}
				}

				//foreach (var cons in prop2.Constraints.SubConstraints)
				//{
				//	if (prop1.Type != cons.Group)
				//		continue;
				//	else
				//	{
				//		bool e = cons.Evaluate(prop1, (p1, p2) => p1.Identifier == p2.Identifier);
				//		if (e)
				//			return true;
				//	}
				//}

				return false;
			}
			private bool CheckProperties(EmpirePropertyViewModel Property1, EmpirePropertyViewModel Property2)
			{
				var prop1 = Property1.Source;
				var prop2 = Property2.Source;

				foreach (var cons in prop1.Constraints.SubConstraints)
				{
					if (prop2.Type != cons.Group)
						continue;
					else
					{
						bool e = cons.Evaluate(prop2, (p1, p2) => p1.Identifier == p2.Identifier);
						if (!e)
							return false;
					}
				}

				foreach (var cons in prop2.Constraints.SubConstraints)
				{
					if (prop1.Type != cons.Group)
						continue;
					else
					{
						bool e = cons.Evaluate(prop1, (p1, p2) => p1.Identifier == p2.Identifier);
						if (!e)
							return false;
					}
				}

				return true;
			}
			private bool CheckProperty(EmpirePropertyViewModel Property)
			{
				var prop = Property.Source;

				foreach (var sol in Solution)
				{
					if (!CheckProperties(Property, sol))
						return false;
				}

				switch (prop.Type)
				{
					case EmpirePropertyType.Authority:
						break;
					case EmpirePropertyType.Civics:
						//if (CivicPointsAvailable == 0)
						//	return false;
						break;
					case EmpirePropertyType.Ethics:
						if (Property.Source.AsEthic.EthicCost > EthicPointsAvailable)
							return false;
						break;
					case EmpirePropertyType.Origin:
						break;
					case EmpirePropertyType.Species:
						break;
					case EmpirePropertyType.Trait:
						break;
				}

				return true;
			}

			private void PickProperty(EmpirePropertyViewModel Property)
			{
				for (int i = 0; i < RemainingIndexes.Count;)
				{
					var index = RemainingIndexes[i];
					var prop = Properties[index];

					if (!CheckProperties(Property, prop))
						RemoveProperty(i, prop.Weight);
					else
						i++;
				}

				switch (Property.Source.Type)
				{
					case EmpirePropertyType.Authority:
						PickAuthority();
						break;
					case EmpirePropertyType.Civics:
						PickCivic();
						break;
					case EmpirePropertyType.Ethics:
						PickEthic();
						break;
					case EmpirePropertyType.Origin:
						HasOrigin = true;
						RemoveOrigins();
						break;
					case EmpirePropertyType.Species:
						break;
					case EmpirePropertyType.Trait:
						break;
				}
			}

			private void PickAuthority()
			{
				HasAuthority = true;
				RemoveAuthorities();
			}
			private void PickCivic()
			{
				CivicPointsAvailable--;
				RemoveCivics();
			}
			private void PickEthic()
			{
				EthicPointsAvailable -= CurrentPick.Source.AsEthic.EthicCost;
				RemoveEthics();
			}

			private void RemoveType(EmpirePropertyType Type)
			{
				var first = RemainingIndexes.First(i => Properties[i].Source.Type == Type);
				for (int i = first; i < RemainingIndexes.Count;)
				{
					var index = RemainingIndexes[i];
					var prop = Properties[index].Source;

					if (prop.Type == Type)
						RemoveProperty(i, prop.Weight);
					else
						break;
				}
			}
			private void RemoveAuthorities()
			{
				RemoveType(EmpirePropertyType.Authority);
			}
			private void RemoveCivics()
			{
				if (CivicPointsAvailable == 1)
				{
					// Remove civics that are now impossible because of the first civic selection
					var first = RemainingIndexes.First(i => Properties[i].Source.Type == EmpirePropertyType.Civics);

					//var toRemove = new List<EmpirePropertyViewModel>();

					for (int i = first; i < RemainingIndexes.Count;)
					{
						var index = RemainingIndexes[i];
						var civicVm = Properties[index];

						if (civicVm.Source.Type == EmpirePropertyType.Civics)
						{
							var b = HasPotentMatches(civicVm);

							if (!b)
								RemoveProperty(i, civicVm.Weight);
							else
								i++;
						}
						else
							break;
					}
				}
				else if (CivicPointsAvailable == 0)
					RemoveType(EmpirePropertyType.Civics);
			}

			private void RemoveEthics()
			{
				if (EthicPointsAvailable == 0)
					RemoveType(EmpirePropertyType.Ethics);

				//// Remove civics that are now impossible because of the first civic selection
				//var first = RemainingIndexes.First(i => Properties[i].Source.Type == EmpirePropertyType.Ethics);

				////var toRemove = new List<EmpirePropertyViewModel>();

				//for (int i = first; i < RemainingIndexes.Count;)
				//{
				//	var index = RemainingIndexes[i];
				//	var ethicVm = Properties[index];
				//	var ethic = ethicVm.Source.AsEthic;

				//	if (ethic != null)
				//	{
				//		//if (ethic.EthicCost > EthicPointsAvailable)

				//		//var b = HasPotentMatches(ethic);

				//		//if (!b)
				//		//	RemoveProperty(i, ethic.Weight);
				//		//else
				//		//	i++;
				//	}
				//	else
				//		break;
				//}
			}

			private void RemoveOrigins()
			{
				RemoveType(EmpirePropertyType.Origin);
			}

			private IEnumerable<EmpirePropertyViewModel> ImpossibleProperties(EmpirePropertyViewModel Pick)
			{
				//var constraints = Pick.Source.Potential.SubConstraints.Concat(Pick.Source.Possible.SubConstraints).ToList();
				//ICollection<EmpirePropertyViewModel> toRemove = new List<EmpirePropertyViewModel>();
				//ICollection<EmpirePropertyViewModel> toRemove2 = new List<EmpirePropertyViewModel>();

				//foreach (var prop in RemainingProperties.Where(p => p != Pick))
				//{
				//	foreach (var c in constraints)
				//	{
				//		if (c.Group != prop.Source.Type)
				//			continue;

				//		bool e = c.Evaluate(prop, (p1, p2) => p1.Identifier == p2.Source.Identifier);
				//		if (!e)
				//			toRemove.Add(prop);
				//	}
				//}

				//var except = RemainingProperties.Except(toRemove);


				//foreach(var pickType in RemainingProperties)

				throw new NotImplementedException();
			}

			private void RemoveProperty(int Index)
			{
				int weight = Properties[Index].Weight;
				RemoveProperty(Index, weight);
			}
			private void RemoveProperty(int Index, int Weight)
			{
#if DEBUG
				Debug.WriteLine(Properties[RemainingIndexes[Index]]);
#endif
				RemainingIndexes.RemoveRange(Index, Weight);
			}

			public GeneratorNode Iterate()
			{
				// No further options, continue with parent node
				if (RemainingIndexes.Count == 0)
					return Parent;

				// Pick randomly an property
				int pickedIndex = Rnd.Next(RemainingIndexes.Count);
				int pickedRemainingIndex = RemainingIndexes[pickedIndex];
				CurrentPick = Properties[pickedRemainingIndex];

				//RemainingIndexes.RemoveAt(pickedIndex);
				pickedIndex = Properties.FindIndex(p => p.Source.Identifier == "ethic_fanatic_authoritarian");
				CurrentPick = Properties.First(p => p.Source.Identifier == "ethic_fanatic_authoritarian");

				RemoveProperty(pickedIndex, CurrentPick.Weight);

				// Check if this item fits in the current selection
				if (!CheckProperty(CurrentPick))
					return this;




				// Remove all same-items (if the picked property had a weight > 1)
				//int weight;
				//if ((weight = pickedProp.Weight) > 1)
				//	RemainingIndexes.RemoveRange(pickedIndex + 1, weight - 1);

				PickProperty(CurrentPick);

				return new GeneratorNode(this, CurrentPick);

				// Check if already picked items and newly picked one are compatible to each other

				//var imp = ImpossibleProperties(pickedProp);
				//switch

				// Remove the index from the list



				//RemainingProperties.

				//return null;

				throw new NotImplementedException();
			}

			public static GeneratorNode CreateRoot(IEnumerable<EmpirePropertyViewModel> Source)
			{
				GeneratorNode node = new GeneratorNode(Source);
				return node;
			}
		}
	}
}