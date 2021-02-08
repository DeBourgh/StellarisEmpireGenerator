using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	public class Constraint
	{
		protected static readonly string[] EmpirePropertyTypesAsString = Enum.GetNames(typeof(EmpirePropertyType)).Select(e => e.ToLower()).ToArray();

		public Constraint() { }

		public Dictionary<EmpirePropertyType, IEnumerable<ConstraintGroup>> ConstraintGroups { get; } = new Dictionary<EmpirePropertyType, IEnumerable<ConstraintGroup>>();

		public bool? Always { get; set; } = null;

		public bool IsAlwaysYes => !ConstraintGroups.Any() && Always.HasValue && Always.Value;

		//public bool IsValid(EmpireProperty Prop)
		//{
		//	if (ConstraintGroups.TryGetValue(Prop.Type, out IEnumerable<ConstraintGroup> cgs))
		//	{
		//		foreach(var cg in cgs)
		//		{

		//		}
		//	}

		//	return true;
		//}


		public void Add(Condition Cond, EmpirePropertyType Type, IEnumerable<EmpireProperty> Properties)
		{
			if (Cond == Condition.Not)
				Cond = Condition.Nor;

			if (Cond == Condition.Required)
			{
				foreach (var prop in Properties)
					Add(Condition.Or, Type, prop.Yield());

				return;
			}

			if (!ConstraintGroups.TryGetValue(Type, out IEnumerable<ConstraintGroup> logicGroups))
			{
				logicGroups = new HashSet<ConstraintGroup>();
				ConstraintGroups[Type] = logicGroups;
			}

			ConstraintGroup first;
			if (((first = logicGroups.FirstOrDefault(p => p.LogicGate == Cond)) == null) || (Cond == Condition.Or))
			{
				var cg = new ConstraintGroup(Type)
				{
					LogicGate = Cond
				};

				foreach (var prop in Properties)
					cg.Properties.Add(prop);

				(logicGroups as ICollection<ConstraintGroup>).Add(cg);
			}
			else
			{
				foreach (var prop in Properties)
					first.Properties.Add(prop);
			}
		}

		public bool Requires(EmpireProperty Prop)
		{
			if (ConstraintGroups.TryGetValue(Prop.Type, out IEnumerable<ConstraintGroup> value))
			{
				if (value.Any(c => c.LogicGate == Condition.Or && c.Properties.Count == 1 && c.Properties.Contains(Prop)))
					return true;
			}

			return false;
		}
	}

	public class ConstraintGroup
	{
		public ConstraintGroup(EmpirePropertyType Type)
		{
			this.Type = Type;
		}
		
		public Condition LogicGate { get; set; }

		[JsonProperty(ReferenceLoopHandling = ReferenceLoopHandling.Ignore, IsReference = true)]
		public HashSet<EmpireProperty> Properties { get; } = new HashSet<EmpireProperty>();

		public EmpirePropertyType Type { get; set; }

		public bool Validate(EmpireProperty Prop)
		{
			switch (LogicGate)
			{
				case Condition.Or:
					return ValidateOr(Prop);
				case Condition.Nor:
					return ValidateNor(Prop);
				default:
					return true;
			}
		}

		private bool ValidateOr(EmpireProperty Prop)
		{
			foreach (var prop in Properties)
			{
				if (prop.Identifier == Prop.Identifier)
					return true;
			}

			return false;
		}
		private bool ValidateNor(EmpireProperty Prop)
		{
			foreach (var prop in Properties)
			{
				if (prop.Identifier == Prop.Identifier)
					return false;
			}

			return true;
		}

		public override string ToString()
		{
			string output = LogicGate.ToString();

			if (Properties.Any())
			{
				output += ":";
				foreach (var prop in Properties)
					output += " " + prop.Identifier;
			}

			return output;
		}
	}
}