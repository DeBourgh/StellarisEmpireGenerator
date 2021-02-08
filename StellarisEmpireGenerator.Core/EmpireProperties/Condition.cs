using System;
using System.Collections.Generic;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	public enum Condition
	{
		Required, // All, And
		Or, // Any, Or
		Not, // Not
		Nor, // !All
		Unknown
	}
}
