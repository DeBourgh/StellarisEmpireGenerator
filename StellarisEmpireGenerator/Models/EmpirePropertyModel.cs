using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisEmpireGenerator.Models
{
	public class EmpirePropertyModel
	{
		public string Identifier { get; set; }
		public string Name { get; set; }
		public uint Weight { get; set; }
		public EmpirePropertyType Type { get; set; }
		public string Dlc { get; set; }
		public IEnumerable<EmpirePropertyConditionModel> Conditions { get; set; }


		private static bool IsAuthority(Entity E)
		{
			return E.Key.StartsWith("auth_");
		}
		private static bool IsCivic(Entity E)
		{
			return E.Key.StartsWith("civic_");
		}
		private static bool IsOrigin(Entity E)
		{
			return E.Key.StartsWith("origin_");
		}
		private static bool IsTrait(Entity E)
		{
			return E.Key.StartsWith("trait_");
		}
		private static bool IsEthic(Entity E)
		{
			return E.Key.StartsWith("ethic_");
		}
		private static bool IsSpecies(Entity E)
		{
			return
				(E.Value is EntityValue<IEnumerable<Entity>> value) &&
				value.Value.Any(e => e.Key == "archetype");
		}

		public static IEnumerable<EmpirePropertyModel> FromEntityRoot(Entity Root)
		{
			List<EmpirePropertyModel> properties = new List<EmpirePropertyModel>();

			if (!(Root.Value is EntityValue<IEnumerable<Entity>>))
				return null;
			else
			{
				var rootValue = (Root.Value as EntityValue<IEnumerable<Entity>>).Value;
				foreach (Entity entity in rootValue)
				{
					//if (entity.)
				}
			}

			throw new NotImplementedException();
		}
	}

	//public class OriginModel : EmpirePropertyModel { }
	//public class TraitModel : EmpirePropertyModel { }
	//public class CivicModel : EmpirePropertyModel { }
	//public class AuthorityModel : EmpirePropertyModel { }
	//public class SpeciesModel : EmpirePropertyModel { }

	public class EmpirePropertyConditionModel
	{
		public Condition LogicGate { get; set; }
		public IEnumerable<EmpirePropertyModel> Properties { get; set; }
	}
	public enum Condition
	{
		Each,
		Or,
		Not,
		Nor
	}

	public enum EmpirePropertyType
	{
		Civic,
		Trait,
		Origin,
		Authority,
		Ethic,
		Species,
		Unknown,
	}
}
