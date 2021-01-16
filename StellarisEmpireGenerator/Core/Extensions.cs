using StellarisEmpireGenerator.Models;
using StellarisEmpireGenerator.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;

namespace StellarisEmpireGenerator.Core
{
	public static class EntityExtensions
	{
		public static Entity FirstOrDefaultKey(this IEnumerable<Entity> Source, string Key)
		{
			return Source.WhereKey(Key).FirstOrDefault();
		}
		public static Entity FirstOrDefaultText(this IEnumerable<Entity> Source, string Text)
		{
			return Source.WhereText(Text).FirstOrDefault();
		}
		public static Entity FirstOrDefaultPair(this IEnumerable<Entity> Source, string Key, string Text)
		{
			return Source.WherePair(Key, Text).FirstOrDefault();
		}
		public static Entity FirstOrDefaultKey(this IEnumerable<Entity> Source, string Key, Func<Entity, bool> Predicate)
		{
			return Source.WhereKey(Key).FirstOrDefault(e => Predicate(e));
		}
		public static Entity FirstOrDefaultText(this IEnumerable<Entity> Source, string Text, Func<Entity, bool> Predicate)
		{
			return Source.WhereText(Text).FirstOrDefault(e => Predicate(e));
		}
		public static Entity FirstOrDefaultPair(this IEnumerable<Entity> Source, string Key, string Text, Func<Entity, bool> Predicate)
		{
			return Source.WherePair(Key, Text).FirstOrDefault(e => Predicate(e));
		}
		public static bool ContainsKey(this IEnumerable<Entity> Source, string Key)
		{
			return Source.FirstOrDefaultKey(Key) != null;
		}
		public static bool ContainsPair(this IEnumerable<Entity> Source, string Key, string Text)
		{
			return Source.FirstOrDefaultPair(Key, Text) != null;
		}
		//public static bool ContainsPath(this IEnumerable<Entity> Source, params string[] KeyPath)
		//{
		//	if (KeyPath is null)
		//		throw new ArgumentNullException(nameof(KeyPath));



		//	var firstOfPath = Source.WhereKey(KeyPath[0]);

		//	foreach (var firsts in firstOfPath)
		//	{
		//		int i = 1;
		//		Entity next = firsts.Children.FirstOrDefaultKey(KeyPath[i]);

		//		while (i < KeyPath.Length)
		//		{


		//			if (next != null)
		//			{

		//			}
		//		}
		//	}
		//}
		//public static bool ContainsPath(this IEnumerable<Entity> Source, string TextValue, params string[] KeyPath)
		//{
		//	if (KeyPath is null)
		//		throw new ArgumentNullException(nameof(KeyPath));

		//	var firstOfPath = Source.WhereKey(KeyPath[0]);

		//	Entity firstFound = Source.FirstOrDefault
		//}

		public static IEnumerable<Entity> WhereKey(this IEnumerable<Entity> Source, string Key)
		{
			return Source.Where(e => e.Key.Equals(Key));
		}

		public static IEnumerable<Entity> WhereText(this IEnumerable<Entity> Source, string Text)
		{
			return Source.Where(e => e.Text?.Equals(Text) ?? false);
		}

		public static IEnumerable<Entity> WherePair(this IEnumerable<Entity> Source, string Key, string Text)
		{
			return Source.WhereKey(Key).WhereText(Text);
		}

		public static bool AnyPair(this IEnumerable<Entity> Source, string Key, string Text)
		{
			return Source.WherePair(Key, Text).Any();
		}

		//public static IEnumerable<Entity>(this En)
	}

	public static class EnumerableExtension
	{
		/// <summary>
		/// Source: https://stackoverflow.com/questions/1577822/passing-a-single-item-as-ienumerablet
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item"></param>
		/// <returns></returns>
		public static IEnumerable<T> Yield<T>(this T item)
		{
			yield return item;
		}
	}

	public static class IOExtensions
	{
		#region Normalizing Directory Paths

		private const char SLASH = '/';
		private const char BACKSLASH = '\\';
		public static string NormPath(this string Source)
		{
			return Source.Replace(SLASH, BACKSLASH);
		}
		public static string NormDirectory(this string Source)
		{
			return NormPath(Source.EndsWith(BACKSLASH.ToString())
					? Source
					: string.Concat(Source, BACKSLASH.ToString()));
		}

		#endregion
	}

	public static class EmpirePropertiesExtensions
	{
		//public static 

		//public static IEnumerable<EmpirePropertyViewModel> PropertyTypeNegative(this IEnumerable<EmpirePropertyViewModel> Source, IEnumerable<EmpirePropertyViewModel> Negative)
		//{
		//	return Source.Where(p => !Negative.Any(n => p.Source.Type == n.Source.Type));
		//}
	}
}