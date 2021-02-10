using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Linq;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyTrait : EmpireProperty
	{
		public EmpirePropertyTrait() : base() { }

		private EmpirePropertyTrait(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Trait)
		{
			Cost = ExtractTraitCost(SourceEntity);
		}

		private IEnumerable<EmpireProperty> TraitAllowedSpecies { get; set; } = null;
		private IEnumerable<EmpireProperty> TraitOpposites { get; set; } = null;

		private static bool IsNodeTrait(Entity Node)
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


		protected override bool OnAdding(EmpireProperty Pick, GeneratorNode Node)
		{
			if (Node.HasTraits)
				return false;
			else if ((Node.TraitPointsAvailable == 1) && (Node.TraitPointsBalance - Cost != 0))
				return false;

			return base.OnAdding(Pick, Node);
		}

		protected override void OnAdded(GeneratorNode Node)
		{
			if (!Node.HasSpecies)
			{
				Node.AddIterationRule(
					n => !n.HasSpecies,
					p => p.IsSpecies);
			}

			if (Node.HasSpecies)
			{
				Node.TraitPointsAvailable--;
				Node.TraitPointsBalance -= Cost;

				if (Node.TraitPointsBalance == 0)
				{
					if (Node.TraitPointsAvailable <= 1)
						Node.HasTraits = true;
					else
					{
						bool more = Convert.ToBoolean(GeneratorNode.Rnd.Next(2));
						if (!more)
							Node.HasTraits = true;
					}
				}

				if (!Node.HasTraits)
				{
					Node.AddIterationRule(
						n => !n.HasTraits,
						p => p.IsTrait);
				}
			}

			if (Node.HasTraits)
			{
				foreach (var trait in Node.RemainingProperties.Where(p => p.IsTrait))
					Node.RemoveSet.Add(trait);
			}

			base.OnAdded(Node);
		}

		internal static EmpirePropertyTrait TraitFromNode(Entity Node)
		{
			if (IsNodeTrait(Node))
				return new EmpirePropertyTrait(Node);
			else
				return null;
		}

		protected override Constraint ExtractConstraint(IEnumerable<EmpireProperty> Properties)
		{
			Constraint constraint = base.ExtractConstraint(Properties);

			TraitOpposites = ExtractTraitOpposites(SourceEntity, Properties);
			TraitAllowedSpecies = ExtractTraitAllowedArchetypes(SourceEntity, Properties);

			if (!TraitAllowedSpecies.Any(p => Properties.Contains(p)))
				return null;

			constraint.Add(Condition.Nor, EmpirePropertyType.Trait, TraitOpposites);
			constraint.Add(Condition.Or, EmpirePropertyType.Species, TraitAllowedSpecies);

			return constraint;
		}
	}
}
