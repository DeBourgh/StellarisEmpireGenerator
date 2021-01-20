using System;
using System.Collections.Generic;
using System.Text;

namespace StellarisEmpireGenerator.Core.ObjectModel
{
	public class EntityValue<T> : IEntityValue<T>
	{
		public EntityValue() { }
		public EntityValue(T Value) : this()
		{
			this.Value = Value;
		}

		public T Value { get; set; }

		object IEntityValue.Value { get => Value; set { Value = (T)value; } }

		public override string ToString()
		{
			return Value?.ToString() ?? base.ToString();
		}
	}
}
