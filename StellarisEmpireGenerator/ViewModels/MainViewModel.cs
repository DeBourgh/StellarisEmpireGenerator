﻿using System;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using StellarisEmpireGenerator.Models;

namespace StellarisEmpireGenerator.ViewModels
{
	public class MainViewModel : ObservableObject, IDataErrorInfo
	{
		private const string _configFileName = "StellarisEmpireGenerator.config";
		private ConfigModel _config = null;

		public MainViewModel()
		{
			if (File.Exists(_configFileName))
				_config = ConfigModel.LoadFromFile(_configFileName);
			else
				_config = new ConfigModel();

			LoadFromConfig();
		}
		private void LoadFromConfig()
		{
			InstallationDirectory = _config.InstallationDir;
			ModDirectory = _config.ModDir;

			LocalizationOptions = new ObservableCollection<string>(_config.Localizations.Keys);
			UsedLocalization = _config.UsedLocalization;

		}
		private void SaveIntoConfig()
		{
			_config.InstallationDir = InstallationDirectory;
			_config.ModDir = ModDirectory;

			_config.UsedLocalization = UsedLocalization;
		}

		public Dictionary<string, string> ErrorDict { get; } = new Dictionary<string, string>();

		private string _stellarisInstallationFolder;
		public bool StellarisInstallationFolderExists { get { return Directory.Exists(InstallationDirectory); } }
		public string InstallationDirectory
		{
			get { return _stellarisInstallationFolder; }
			set { SetProperty(ref _stellarisInstallationFolder, NormDirectory(value)); }
		}

		private string _stellarisModFolder;
		public bool StellarisModFolderExists { get { return Directory.Exists(ModDirectory); } }
		public string ModDirectory
		{
			get { return _stellarisModFolder; }
			set { SetProperty(ref _stellarisModFolder, NormDirectory(value)); }
		}

		//private ObservableCollection<string> _localizationOptions = new ObservableCollection<string>();
		public ObservableCollection<string> LocalizationOptions { get; set; }

		private string _usedLocalization;
		public string UsedLocalization
		{
			get { return _usedLocalization; }
			set { SetProperty(ref _usedLocalization, value); }
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


		#region Normalizing Directory Paths

		private const char SLASH = '/';
		private const char BACKSLASH = '\\';
		private string NormPath(string Filepath)
		{
			return Filepath.Replace(SLASH, BACKSLASH);
		}
		private string NormDirectory(string Dirpath)
		{
			return NormPath(Dirpath.EndsWith(BACKSLASH.ToString())
					? Dirpath
					: string.Concat(Dirpath, BACKSLASH.ToString()));
		}

		#endregion

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
				modFolderFiles = Directory.EnumerateDirectories(ModDirectory).SelectMany(x => CollectMechanicFiles(x));
			}

			List<string> files = new List<string>();

			files.AddRange(installFolderFiles);
			files.AddRange(modFolderFiles);

			return files;
		}
		private IEnumerable<string> CollectLanguageFiles()
		{
			throw new NotImplementedException();
			//return CollectMechanicFiles(_config.Localizations[_config.UsedLocalization]);

		}
		private string[] Tokenize(IEnumerable<string> FilePathsGameMechanics)
		{
			StringBuilder allText = new StringBuilder();

			var lines = FilePathsGameMechanics
				.SelectMany(x => File.ReadAllLines(x));

			File.WriteAllLines("collectedFiles.txt", lines);

			foreach (var line in lines)
			{
				int index = line.IndexOf(COMMENT);

				if (index == 0)
					continue;
				else if (index > 0)
				{
					string withoutComment = line.Remove(index, line.Length - index);
					allText.AppendLine(withoutComment);
				}
				else
					allText.AppendLine(line);
			}

			allText
				.Replace("{", " { ")
				.Replace("}", " } ")
				.Replace("=", " = ");

			string allTextSingleSpace = Regex.Replace(allText.ToString(), @"\s+", " ");

			// Split
			List<string> tokens = new List<string>();
			bool withinString = false;
			int nextSplitStart = 0;
			for (int i = 0; i < allTextSingleSpace.Length; i++)
			{
				char currentChar = allTextSingleSpace[i];

				switch (currentChar)
				{
					case '"':
						withinString = !withinString;
						break;
					case ' ':
						int nextSplitCount = i - nextSplitStart;
						if (nextSplitCount == 0)
							nextSplitStart = i + 1;
						else if (!withinString)
						{
							tokens.Add(allTextSingleSpace.Substring(nextSplitStart, i - nextSplitStart));
							nextSplitStart = i + 1;
						}
						break;
				}

			}

			return tokens
				//.Where(s => !string.IsNullOrWhiteSpace(s))
				.ToArray();
		}



		private const char COMMENT = '#';
		private void Reload()
		{
			try
			{
				var paths = CollectFilePathsGameMechanics();
				var tokens = Tokenize(paths);
				using (TextWriter tw = new StreamWriter("tokens.txt"))
				{
					foreach (var token in tokens)
						tw.WriteLine(token);
				}
				var entityModel = Entity.Parse(tokens);

				var flatten = entityModel.Flatten();
				using (TextWriter tw = new StreamWriter("flatten.txt"))
				{
					foreach (var flat in flatten)
						tw.WriteLine(flat);
				}

				//int Index = 0;
				//Entity.FindKey(entityModel, null, ref Index);

			}
			catch (Exception e)
			{
				;
			}
		}

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
	}
}
