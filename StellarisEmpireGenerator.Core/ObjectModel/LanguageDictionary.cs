using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using StellarisEmpireGenerator.Core.ObjectModel;

namespace StellarisEmpireGenerator.ObjectModel
{
	public class LanguageDictionary
	{
		private IDictionary<string, string> _dictionary;

		private LanguageDictionary(IDictionary<string, string> Dict)
		{
			_dictionary = Dict;
			Dictionary = new ReadOnlyDictionary<string, string>(_dictionary);
		}

		public IReadOnlyDictionary<string, string> Dictionary { get; }

		private static IDictionary<string, string> ExtractLanguageDictionary(IEnumerable<string> LanguageFiles, string LanguageKey)
		{
			Dictionary<string, string> langDict = new Dictionary<string, string>();

			foreach (var file in LanguageFiles)
			{
				//Debug.WriteLine(file);
				using (StreamReader sr = new StreamReader(file))
				{
					int c = 0;
					while (!sr.EndOfStream)
					{

						//if ((c++ % 1000) == 0)
						//	Debug.WriteLine(c);

						string line = sr.ReadLine();

						// Remove comment
						int indexHash = line.IndexOf(Entity.COMMENT);
						int lastIndexHash = line.LastIndexOf(Entity.COMMENT);
						int indexDoubleQuotes = line.IndexOf('"');
						int lastIndexDoubleQuotes = line.LastIndexOf('"');

						if (indexHash == 0)
							continue;
						else if (indexHash > 0)
						{
							if ((indexDoubleQuotes < 0) || (indexHash < indexDoubleQuotes))
								line = line.Remove(indexHash, line.Length - indexHash);
							else if (lastIndexHash > lastIndexDoubleQuotes)
								line = line.Remove(lastIndexHash, line.Length - lastIndexHash);
						}

						Match m = Regex.Match(line, @"^l_[^\:]*:");
						if (m.Success)
						{
							if (!m.Value.Contains(LanguageKey))
								break;
						}

						line = Regex.Replace(line, @"^\s+", "");
						m = Regex.Match(line, @"\:\d* ");
						//int keyIndex;
						//if ((keyIndex = line.IndexOf(":0")) < 0)
						//	continue;

						if (!m.Success || m.Captures[0].Index > indexDoubleQuotes)
							continue;

						// Key
						var key = line.Substring(0, m.Captures[0].Index);

						// Value
						indexDoubleQuotes = line.IndexOf('"');
						lastIndexDoubleQuotes = line.LastIndexOf('"');
						int length = lastIndexDoubleQuotes - indexDoubleQuotes;
						var value = line.Substring(indexDoubleQuotes + 1, length - 1);

						langDict[key] = value;
					}
				}
			}

			return langDict;
		}

		public string GetRawValue(string Key)
		{
			return Dictionary[Key];
		}

		private string ResolveValue(string Var)
		{
			string varName = Var.Substring(1, Var.Length - 2);

			return this[varName];
		}

		public string this[string Key]
		{
			get
			{
				if (Dictionary.TryGetValue(Key, out string value))
				{
					var matches = Regex.Matches(value, @"\$[^\$]+\$");

					int count;
					if ((count = matches.Count) > 0)
					{
						IDictionary<string, string> replDict = new Dictionary<string, string>();
						for (int i = 0; i < count; i++)
						{
							var origValue = matches[i].Captures[0].Value;

							string toReslove = origValue;

							int matchIndex;
							if ((matchIndex = origValue.IndexOf("|")) >= 0)
								toReslove = origValue.Remove(matchIndex, origValue.Length - matchIndex - 1);

							var replace = ResolveValue(toReslove);

							if (replace != null)
								replDict.Add(origValue, replace);
						}

						foreach (var replItem in replDict)
							value = value.Replace(replItem.Key, replItem.Value);
					}

					return value;
				}
				else
					return null;
			}
		}

		public static LanguageDictionary FromConfigModel(IEnumerable<string> PotentialFiles, string LanguageKey)
		{
			var dict = new LanguageDictionary(ExtractLanguageDictionary(PotentialFiles, LanguageKey));

			return dict;
		}

		public IDictionary<string, string> ReduceDictionary(IEnumerable<string> TargetKeySet, bool AddEmptyKeys = false)
		{
			IDictionary<string, string> reduced = new Dictionary<string, string>();

			foreach (var key in TargetKeySet)
			{
				string val = this[key];

				if (val != null || AddEmptyKeys)
					reduced.Add(key, val);
			}

			return reduced;
		}
	}
}
