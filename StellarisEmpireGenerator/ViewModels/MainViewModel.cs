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

namespace StellarisEmpireGenerator.ViewModels
{
	public class MainViewModel : ObservableObject, IDataErrorInfo
	{
		private const string _configFileName = "StellarisEmpireGenerator.config";
		private readonly ConfigModel _config = null;

		public MainViewModel()
		{
			if (File.Exists(_configFileName))
				_config = ConfigModel.LoadFromFile(_configFileName);
			else
				_config = ConfigModel.ByDefault();

			LoadFromConfig();
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

		public Dictionary<string, string> ErrorDict { get; } = new Dictionary<string, string>();

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

		//private ObservableCollection<string> _localizationOptions = new ObservableCollection<string>();
		public ObservableCollection<string> LocalizationOptions { get; set; }

		private string _activeLocalizationKey;
		public string ActiveLocalizationKey
		{
			get { return _activeLocalizationKey; }
			set { SetProperty(ref _activeLocalizationKey, value); }
		}


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
					_generateCommand = new RelayCommand(P => true, p => GenerateEmpires((int)p));
				}

				return _generateCommand;
			}
		}

		private int _generateCount = 1;
		public int GenerateCount
		{
			get => _generateCount;
			set { SetProperty(ref _generateCount, value); }
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

		public ObservableCollection<EmpirePropertyViewModel> Authorities { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Civics { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Ethics { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Origins { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Traits { get; } = new ObservableCollection<EmpirePropertyViewModel>();
		public ObservableCollection<EmpirePropertyViewModel> Species { get; } = new ObservableCollection<EmpirePropertyViewModel>();



		private IEnumerable<EmpirePropertyViewModel> CollectAllPropertyViewModels()
		{
			return Authorities.Concat(Civics).Concat(Ethics).Concat(Origins).Concat(Traits).Concat(Species);
		}

		//private void Epvm_PropertyChanged(object sender, PropertyChangedEventArgs e)
		//{

		//	throw new NotImplementedException();
		//}



		#region Command Methods

		private void Reload()
		{
			try
			{
				var mechPaths = CollectFilePathsGameMechanics();

				var root = Entity.FromFiles(mechPaths);

				var rootContent = root.Serialize();
				File.WriteAllText("allText.txt", rootContent);

				var props = EmpireProperty.FromEntityRoot(root);

				//EmpireProperty.SaveToFile(props, "props.txt");

				var langPaths = CollectLanguageFilePaths();
				var lang = LanguageDictionary.FromConfigModel(langPaths, ActiveLocalizationKey);
				EmpireProperty.ApplyLanguage(props, lang);

				Authorities.Clear();
				Origins.Clear();
				Civics.Clear();
				Ethics.Clear();
				Traits.Clear();
				Species.Clear();
				foreach (var prop in props.OrderBy(p => p.Name))
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

					//epvm.PropertyChanged += Epvm_PropertyChanged;
				}
			}
			catch (Exception e)
			{
				;
			}
		}

		private void ChangeAllowAll(object Parameter, bool Value)
		{
			var e = Enum.Parse(typeof(EmpirePropertyType), (string)Parameter);

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

			epvm.ToList().ForEach(p => p.IsAllowed = Value);
		}
		private void AllowAll(object Parameter)
		{
			ChangeAllowAll(Parameter, true);
		}
		private void DisallowAll(object Parameter)
		{
			ChangeAllowAll(Parameter, false);
		}

		//private void GenerateAuthority()
		//{

		//}
		//private void GenerateCivics()
		//{

		//}
		//private void GenerateEthic()
		//{

		//}
		//private void GenerateOrigin()
		//{

		//}
		//private void GenerateTraitsAndSpecies()
		//{

		//}

		private bool Validator(EmpireProperty Property, EmpirePropertyViewModel EpVm)
		{
			return Property.Identifier == EpVm.Source.Identifier;
		}



		private IEnumerable<EmpirePropertyViewModel> GetWeightedProperties()
		{
			var allowedProperties = CollectAllPropertyViewModels().Where(p => p.IsAllowed).OrderBy(p => $"{p.Source.Type}_{p.Source.Identifier}");
			return allowedProperties.SelectMany(a => Enumerable.Repeat(a, a.Weight));
		}


		//private IEnumerable<EmpirePropertyViewModel> GenerateEmpire()
		//{


		//	return null;
		//}

		private void GenerateEmpires(int Count)
		{
			var props = GetWeightedProperties().ToList();

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
			catch
			{

			}
		}

		#endregion

		public void SaveConfigToFile()
		{
			SaveIntoConfig();
			ConfigModel.SaveToFile(_config, _configFileName);
		}

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

		public class GeneratorNode
		{
			private static readonly Random Rnd = new Random(Environment.TickCount);

			private GeneratorNode(IEnumerable<EmpirePropertyViewModel> Source, bool Shuffle)
			{
				Solution = new List<EmpirePropertyViewModel>();
				Properties = new List<EmpirePropertyViewModel>(Source);
				RemainingIndexes = Enumerable.Range(0, Source.Count()).ToList();
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
			//public bool IsSolution { get; private set; } = false;

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
				var types = toMatch.Constraints.SubConstraints.Select(c => c.Group).Where(t => t != EmpirePropertyType.Unknown);
				IDictionary<EmpirePropertyType, int> firsts = types
					.Select(t => RemainingIndexes.First(i => Properties[i].Source.Type == t))
					.ToDictionary(i => Properties[RemainingIndexes[i]].Source.Type);

				foreach (var keyVal in firsts)
				{
					bool typeMatchFound = true;
					for (int i = keyVal.Value; i < RemainingIndexes.Count; i++)
					{
						var index = RemainingIndexes[i];
						var prop = Properties[index];

						bool e = Matches(ToMatch, prop);
						typeMatchFound &= e;

						if (!typeMatchFound)
							break;

						if (prop.Source.Type != keyVal.Key)
							break;
					}

					if (!typeMatchFound)
						return false;
				}

				return false;

				//var matchingIndex = RemainingIndexes[Index];
				//var matching = Properties[matchingIndex].Source;
				//var matchingType = matching.Type;

				//for (int i = Index; i < RemainingIndexes.Count; i++)
				//{
				//	var index = RemainingIndexes[i];
				//	var prop = Properties[index].Source;

				//	if (prop.Type != toMatch.Type)
				//		break;
				//	else
				//	{

				//	}
				//}

				//return false;
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
			private bool AreCompatible(EmpirePropertyViewModel Property1, EmpirePropertyViewModel Property2)
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

			private void PickProperty(EmpirePropertyViewModel Property)
			{
				for (int i = 0; i < RemainingIndexes.Count;)
				{
					var index = RemainingIndexes[i];
					var prop = Properties[index];

					if (!AreCompatible(Property, prop))
						RemoveProperty(i, prop.Weight);
					else
						i++;
				}

				switch (Property.Source.Type)
				{
					case EmpirePropertyType.Authority:
						HasAuthority = true;
						RemoveAuthorities();
						break;
					case EmpirePropertyType.Civics:
						CivicPointsAvailable--;
						RemoveCivics();
						break;
					case EmpirePropertyType.Origin:
						HasOrigin = true;
						RemoveOrigins();
						break;
				}
				//for (int i = toRemove.Count - 1; i >= 0; i--)
				//	RemoveProperty(toRemove[i]);
				//RemainingIndexes.RemoveAt(toRemove[i]);


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

					var toRemove = new List<EmpirePropertyViewModel>();

					for (int i = first; i < RemainingIndexes.Count;)
					{
						var index = RemainingIndexes[i];
						var civic = Properties[index];

						if (civic.Source.Type == EmpirePropertyType.Civics)
						{
							var b = HasPotentMatches(civic);

							if (!b)
							{
								toRemove.Add(civic);
								RemoveProperty(i, civic.Weight);
							}
							else
								i++;

							//if (!b)
							//	toRemove.Add(civic);
							//if (!b)
							//	RemoveProperty(i, civic.Source.Weight);
						}
						else
							break;
					}
					;
				}
				else if (CivicPointsAvailable == 0)
					RemoveType(EmpirePropertyType.Civics);
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
			//private void RemoveProperty(int Index, EmpirePropertyViewModel Property)
			//{
			//	RemoveProperty(Index, Property.Weight);
			//}

			//private void RemoveProperty(EmpirePropertyViewModel Property)
			//{

			//}

			public GeneratorNode Iterate()
			{
				// No further options, continue with parent node
				if (RemainingIndexes.Count == 0)
					return Parent;

				// Pick randomly an property
				int pickedIndex = Rnd.Next(RemainingIndexes.Count);
				int pickedRemainingIndex = RemainingIndexes[pickedIndex];
				var pickedProp = Properties[pickedRemainingIndex];

				//RemainingIndexes.RemoveAt(pickedIndex);
				pickedIndex = Properties.FindIndex(p => p.Source.Identifier == "civic_meritocracy");
				pickedProp = Properties.First(p => p.Source.Identifier == "civic_meritocracy");

				RemoveProperty(pickedIndex, pickedProp.Weight);



				// Check if this item fits in the current selection
				foreach (var cs in Solution)
				{
					if (!AreCompatible(pickedProp, cs))
						return this;
				}

				// Remove all same-items (if the picked property had a weight > 1)
				//int weight;
				//if ((weight = pickedProp.Weight) > 1)
				//	RemainingIndexes.RemoveRange(pickedIndex + 1, weight - 1);

				PickProperty(pickedProp);

				return new GeneratorNode(this, pickedProp);

				// Check if already picked items and newly picked one are compatible to each other


				//pickedProp = RemainingProperties.First(p => p.Source.Identifier == "civic_meritocracy");



				//var imp = ImpossibleProperties(pickedProp);
				//switch

				// Remove the index from the list



				//RemainingProperties.

				//return null;

				throw new NotImplementedException();
			}

			public static GeneratorNode CreateRoot(IEnumerable<EmpirePropertyViewModel> Source)
			{
				GeneratorNode node = new GeneratorNode(Source, true);
				return node;
			}
		}
	}
}