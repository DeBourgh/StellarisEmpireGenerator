using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyCivic : EmpireProperty
	{
		public EmpirePropertyCivic() : base() { }

		private EmpirePropertyCivic(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Civics) { }

		//public IEnumerable<EmpirePropertyTrait> SecondarySpeciesTraits { get; private set; } = Enumerable.Empty<EmpirePropertyTrait>();

		private static bool IsCivic(Entity Node)
		{
			return
				Node.Key.StartsWith("civic_") &&
				!Node.Descendants.ContainsKey("country_type");
		}

		internal static EmpirePropertyCivic CivicFromNode(Entity Node)
		{
			if (IsCivic(Node))
				return new EmpirePropertyCivic(Node);
			else
				return null;
		}

		protected override void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties) { }
	}
}
