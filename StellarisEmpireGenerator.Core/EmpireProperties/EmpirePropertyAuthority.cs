using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyAuthority : EmpireProperty
	{
		public EmpirePropertyAuthority() : base() { }

		private EmpirePropertyAuthority(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Authority) { }

		private static bool IsAuthority(Entity Node)
		{
			Entity aiEmpire = Node.Descendants.FirstOrDefaultPair("value", "ai_empire");

			return
				Node.Key.StartsWith("auth_") &&
				(!aiEmpire?.Parent.Key.Equals("country_type") ?? true) &&
				(!aiEmpire?.Parent.Parent.Key.Equals("potential") ?? true);
		}

		internal static EmpirePropertyAuthority AuthorityFromNode(Entity Node)
		{
			if (IsAuthority(Node))
				return new EmpirePropertyAuthority(Node);
			else
				return null;
		}

		protected override void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties) { }
	}
}
