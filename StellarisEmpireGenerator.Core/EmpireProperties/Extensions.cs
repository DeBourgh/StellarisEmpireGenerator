using System;
using System.Collections.Generic;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	public static class Extensions
	{
		public static IEnumerable<T> Yield<T>(this T item)
		{
			yield return item;
		}
	}
}
