using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyOrigin : EmpireProperty
	{
		public EmpirePropertyOrigin() : base() { }

		private EmpirePropertyOrigin(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Origin) { }

		//public IEnumerable<EmpirePropertyTrait> SecondarySpeciesTraits { get; private set; } = Enumerable.Empty<EmpirePropertyTrait>();

		private static bool IsOrigin(Entity Node)
		{
			return
				Node.Descendants.ContainsPair("is_origin", "yes") &&
				(!Node.Descendants.FirstOrDefaultPair("always", "no")?.Ancestors.ContainsKey("playable") ?? true);
		}

		internal static EmpirePropertyOrigin OriginFromNode(Entity Node)
		{
			if (IsOrigin(Node))
				return new EmpirePropertyOrigin(Node);
			else
				return null;
		}

		protected override void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties) { }
	}
}
