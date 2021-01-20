using System;
using System.Collections.Generic;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	public enum Condition
	{
		Each, // All, And
		Or, // Any, Or
		Not, // Not
		Nor, // !All
		Unknown
	}
}
