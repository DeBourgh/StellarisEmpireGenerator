using System;
using System.Collections.Generic;
using System.Linq;

namespace StellarisEmpireGenerator.Core.ObjectModel
{
	public static class Extensions
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
	}
}
