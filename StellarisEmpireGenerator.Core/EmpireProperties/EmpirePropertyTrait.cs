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

		//protected override bool OnAdd(GeneratorNode Node)
		//{
		//	//if (Node.TraitPointsAvailable == 1 && Node.SumTraitPoints == 0)
		//	//	return false;

		//	return base.OnAdd(Node);
		//}

		protected override bool OnAdding(EmpireProperty Pick, GeneratorNode Node)
		{
			if (Type == EmpirePropertyType.Trait)
			{
				if (Node.HasTraits)
					return false;
				else if (Node.TraitPointsAvailable == 1)
					return Node.TraitPointsBalance - Cost == 0;
			}

			return base.OnAdding(Pick, Node);
		}

		protected override void OnAdded(GeneratorNode Node)
		{
			if (!Node.HasSpecies)
			{
				Node.NextIterationRule = (p => p.Type == EmpirePropertyType.Species);
			}
			else
			{
				Node.TraitPointsAvailable--;
				Node.TraitPointsBalance -= Cost;

				//if (Node.TraitPointsAvailable == 0 && !Node.AreTraitsValid)
				//	Node.NextIterationRule = (p => false);
				//else
				//{
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
					Node.NextIterationRule = (p => p.Type == EmpirePropertyType.Trait);
				//}
			}

			base.OnAdded(Node);
		}

		protected override bool IsValidWith(EmpireProperty Prop, GeneratorNode Node)
		{

			//if ( && (Prop.Type == EmpirePropertyType.Trait))
			//	return false;

			return base.IsValidWith(Prop, Node);
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
