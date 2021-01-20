using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyTrait : EmpireProperty
	{
		public EmpirePropertyTrait() : base() { }

		private EmpirePropertyTrait(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Trait)
		{
			TraitCost = ExtractTraitCost(SourceEntity);
		}

		private IEnumerable<EmpireProperty> TraitAllowedSpecies { get; set; } = null;
		private IEnumerable<EmpireProperty> TraitOpposites { get; set; } = null;

		public int TraitCost { get; set; } = default;

		private static bool IsTrait(Entity Node)
		{
			return
				Node.Key.StartsWith("trait_") &&
				!Node.Children.ContainsKey("initial") &&
				Node.Children.ContainsKey("cost") &&
				!Node.Children.ContainsKey("leader_trait");
		}

		#region Extraction methods

		private static IEnumerable<EmpireProperty> ExtractTraitAllowedArchetypes(Entity Node, IEnumerable<EmpireProperty> Properties)
		{
			var archetypes = Node.Children.FirstOrDefaultKey("allowed_archetypes");
			if (archetypes != null)
			{
				HashSet<string> set;
				if ((set = archetypes.Set) != null)
				{
					return Properties.Where(p => (p is EmpirePropertySpecies sp) && set.Contains(sp.Archetype));
				}
			}

			return Enumerable.Empty<EmpireProperty>();
		}

		private static int ExtractTraitCost(Entity Node)
		{
			return
				int.Parse(Node.Children.FirstOrDefaultKey("cost")?.Text ?? "0");
		}

		private static IEnumerable<EmpireProperty> ExtractTraitOpposites(Entity Node, IEnumerable<EmpireProperty> Properties)
		{
			var opposites = Node.Children.FirstOrDefaultKey("opposites");
			if (opposites != null)
			{
				HashSet<string> set;
				if ((set = opposites.Set) != null)
				{
					var setIdentifier = set.Select(o => o.Substring(1, o.Length - 2)).ToHashSet();
					return Properties.Where(p => setIdentifier.Contains(p.Identifier));
				}
			}

			return Enumerable.Empty<EmpireProperty>();
		}

		#endregion

		internal static EmpirePropertyTrait TraitFromNode(Entity Node)
		{
			if (IsTrait(Node))
				return new EmpirePropertyTrait(Node);
			else
				return null;
		}

		protected override void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties)
		{
			// Extract trait opposites
			//foreach (var prop in Properties)
			//{
			//	EmpirePropertyTrait trait = prop.AsTrait;
			//	if (trait == null)
			//		continue;

			//	trait.TraitOpposites = ExtractTraitOpposites(trait.SourceEntity, Properties);
			//	trait.TraitAllowedSpecies = ExtractTraitAllowedArchetypes(trait.SourceEntity, Properties);
			//}

			TraitOpposites = ExtractTraitOpposites(SourceEntity, Properties);
			TraitAllowedSpecies = ExtractTraitAllowedArchetypes(SourceEntity, Properties);

			foreach (var opp in TraitOpposites)
				AddConstraint(Condition.Nor, opp);

			foreach (var allowedSp in TraitAllowedSpecies)
				AddConstraint(Condition.Or, allowedSp);
			//if (TraitOpposites != null && TraitOpposites.Count() > 0)
			//{
			//	Constraint<EmpireProperty> constraintTrait = new Constraint<EmpireProperty>(Condition.Each)
			//	{
			//		Group = EmpirePropertyType.Trait
			//	};

			//	Constraint<EmpireProperty> constraintTraitNor = new Constraint<EmpireProperty>(Condition.Nor);
			//	foreach (var opp in TraitOpposites)
			//		constraintTraitNor.Objects.Add(opp);

			//	constraintTrait.SubConstraints.Add(constraintTraitNor);
			//	Possible.SubConstraints.Add(constraintTrait);
			//}

			//if (TraitAllowedSpecies != null && TraitAllowedSpecies.Count() > 0)
			//{
			//	Constraint<EmpireProperty> constraintSpec = new Constraint<EmpireProperty>(Condition.Each)
			//	{
			//		Group = EmpirePropertyType.Species
			//	};

			//	Constraint<EmpireProperty> constraintSpecOr = new Constraint<EmpireProperty>(Condition.Or);
			//	foreach (var spec in TraitAllowedSpecies)
			//		constraintSpecOr.Objects.Add(spec);

			//	constraintSpec.SubConstraints.Add(constraintSpecOr);
			//	Possible.SubConstraints.Add(constraintSpec);
			//}
		}
	}
}
