using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisEmpireGenerator.Models
{
	public class EmpirePropertyModel
	{
		public string Name { get; set; }
		public uint Weight { get; set; }

		public EmpirePropertyConditionModel Condition { get; set; }
	}

	public class EmpirePropertyConditionModel
	{
		public Condition LogicGate { get; set; }
		public EmpirePropertyModel[] Properties { get; set; }
	}
	public enum Condition
	{
		Each,
		Or,
		Not,
		Nor
	}
}
