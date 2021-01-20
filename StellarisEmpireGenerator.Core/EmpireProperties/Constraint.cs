using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public class Constraint<T>
	{
		public Constraint(Condition LogicGate)
		{
			this.LogicGate = LogicGate;
		}

		public Condition LogicGate { get; set; }

		[JsonProperty(ReferenceLoopHandling = ReferenceLoopHandling.Ignore, IsReference = true)]
		public List<T> Objects { get; } = new List<T>();

		[JsonProperty(ReferenceLoopHandling = ReferenceLoopHandling.Ignore, IsReference = true)]
		public List<Constraint<T>> SubConstraints { get; } = new List<Constraint<T>>();

		public EmpirePropertyType Group { get; set; } = EmpirePropertyType.Unknown;

		private bool Each<TEval>(TEval EvalItem, Func<T, TEval, bool> Validator)
		{
			foreach (var constraint in Objects)
			{
				if (!Validator(constraint, EvalItem))
					return false;
			}

			foreach (var constraint in SubConstraints)
			{
				if (!constraint.Evaluate(EvalItem, Validator))
					return false;
			}

			return true;
		}
		private bool Any<TEval>(TEval EvalItem, Func<T, TEval, bool> Validator)
		{
			foreach (var constraint in Objects)
			{
				if (Validator(constraint, EvalItem))
					return true;
			}

			foreach (var constraint in SubConstraints)
			{
				if (constraint.Evaluate(EvalItem, Validator))
					return true;
			}

			return false;
		}
		public bool Evaluate<TEval>(TEval EvalItem, Func<T, TEval, bool> Validator)
		{
			if (Validator is null)
				throw new ArgumentNullException(nameof(Validator));

			switch (LogicGate)
			{
				case Condition.Each:
					return Each(EvalItem, Validator);
				case Condition.Or:
					return Any(EvalItem, Validator);
				case Condition.Not:
					return !Each(EvalItem, Validator);
				case Condition.Nor:
					return !Any(EvalItem, Validator);
				default:
					return false;
			}
		}

		public static Constraint<T> True
		{
			get
			{
				Constraint<T> constraint = new Constraint<T>(Condition.Each);
				return constraint;
			}
		}

		public static Constraint<T> False
		{
			get
			{
				Constraint<T> constraint = new Constraint<T>(Condition.Or);
				return constraint;
			}
		}

		public override string ToString()
		{
			if (Group != EmpirePropertyType.Unknown)
				return Group.ToString();
			else
			{
				if (Objects.Count > 0)
				{
					string output = LogicGate.ToString() + ": " + Objects[0];
					foreach (var obj in Objects.Skip(1))
						output += obj.ToString();

					return output;
				}
				else
					return LogicGate.ToString();
			}
		}
	}
}
