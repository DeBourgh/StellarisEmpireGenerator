using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyCivic : EmpireProperty
	{
		public EmpirePropertyCivic() : base() { }

		private EmpirePropertyCivic(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Civics) { }

		protected override bool OnAdding(EmpireProperty Pick, GeneratorNode Node)
		{
			if (Node.HasCivics)
				return false;

			return base.OnAdding(Pick, Node);
		}

		protected override void OnAdded(GeneratorNode Node)
		{
			Node.CivicPointsAvailable--;

			if (Node.HasCivics)
			{
				foreach (var civic in Node.RemainingProperties.Where(p => p.IsCivic))
					Node.RemoveSet.Add(civic);
			}

			base.OnAdded(Node);
		}


		protected override bool OnRemoving(GeneratorNode Node)
		{
			if (Node.HasCivics)
				return true;

			return base.OnRemoving(Node);
		}

		private static bool IsNodeCivic(Entity Node)
		{
			return
				Node.Key.StartsWith("civic_") &&
				!Node.Descendants.ContainsKey("country_type");
		}

		internal static EmpirePropertyCivic CivicFromNode(Entity Node)
		{
			if (IsNodeCivic(Node))
				return new EmpirePropertyCivic(Node);
			else
				return null;
		}
	}
}
