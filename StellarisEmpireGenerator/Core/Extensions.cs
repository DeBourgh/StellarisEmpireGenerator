using StellarisEmpireGenerator.ObjectModel;
using StellarisEmpireGenerator.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;

namespace StellarisEmpireGenerator.Core
{


	public static class EnumerableExtension
	{
		/// <summary>
		/// Source: https://stackoverflow.com/questions/1577822/passing-a-single-item-as-ienumerablet
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item"></param>
		/// <returns></returns>

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