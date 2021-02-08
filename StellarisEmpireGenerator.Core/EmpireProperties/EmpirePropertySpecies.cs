using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertySpecies : EmpireProperty
	{
		public EmpirePropertySpecies() : base() { }

		private EmpirePropertySpecies(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Species)
		{
			Archetype = ExtractArchetype(SourceEntity);
			CanBeSecondarySpecies = ExtractCanBeSecondary(SourceEntity);

			(int TraitPoints, int MaxTraitPoints) = ExtractTraitPoints(SourceEntity);
			this.TraitPoints = TraitPoints;
			this.MaxTraitPoints = MaxTraitPoints;
		}

		public string Archetype { get; set; } = string.Empty;
		public int MaxTraitPoints { get; set; } = default;
		public int TraitPoints { get; set; } = default;

		//public override int MaxCountPerType => MaxCount;

		public bool CanBeSecondarySpecies { get; set; } = true;

		//protected override bool OnAdding(GeneratorNode Node)
		//{
		//	if (Node.HasSpecies)
		//		return false;

		//	return base.OnAdding(Node);
		//}

		protected override void OnAdded(GeneratorNode Node)
		{
			Node.HasSpecies = true;
			Node.MaxTraits = MaxTraitPoints;
			Node.TraitPointsAvailable = MaxTraitPoints;
			Node.TraitPointsBalance = TraitPoints;

			Node.TraitPointsAvailable -= Node.Solution.Count(p => p.Type == EmpirePropertyType.Trait);
			Node.TraitPointsBalance -= Node.Solution.Where(p => p.Type == EmpirePropertyType.Trait).Sum(p => p.Cost);

			Node.NextIterationRule = (p => p.Type == EmpirePropertyType.Trait);

			foreach (var species in Node.RemainingProperties.Where(p => p.IsSpecies).ToArray())
				Node.Remove(species);

			base.OnAdded(Node);
		}

		protected override bool OnRemoving(GeneratorNode Node)
		{
			if (Node.HasSpecies)
				return true;

			return base.OnRemoving(Node);
		}

		private static string ExtractArchetype(Entity Node)
		{
			return Node.Children.FirstOrDefaultKey("archetype")?.Text ?? null;
		}
		private static bool ExtractCanBeSecondary(Entity Node)
		{
			return !Node.Children.FirstOrDefaultPair("always", "no")?.Ancestors.ContainsKey("possible_secondary") ?? true;
		}
		private static (int TraitPoints, int MaxTraitPoints) ExtractTraitPoints(Entity Node)
		{
			Entity nodeArchetype = Node.Children.FirstOrDefaultKey("archetype");
			Entity archetype = Node.Parent.Children.FirstOrDefaultKey(nodeArchetype.Text);

			Entity inheriting;
			if ((inheriting = archetype.Children.FirstOrDefaultKey("inherit_trait_points_from")) != null)
				archetype = Node.Parent.Children.FirstOrDefaultKey(inheriting.Text);

			if (!int.TryParse(archetype.Children.FirstOrDefaultKey("species_trait_points").Text, out int traitPoints))
				int.TryParse(Node.Parent.Children.FirstOrDefaultKey(archetype.Children.FirstOrDefaultKey("species_trait_points").Text).Text, out traitPoints);

			if (!int.TryParse(archetype.Children.FirstOrDefaultKey("species_max_traits").Text, out int maxTraitPoints))
				int.TryParse(Node.Parent.Children.FirstOrDefaultKey(archetype.Children.FirstOrDefaultKey("species_max_traits").Text).Text, out maxTraitPoints);

			return (
				traitPoints,
				maxTraitPoints
				);
		}

		private static bool IsNodeSpecies(Entity Node)
		{
			return
				Node.Children.ContainsKey("archetype") &&
				Node.Children.ContainsKey("possible") &&
				(!Node.Descendants.FirstOrDefaultPair("always", "no")?.Ancestors.ContainsKey("playable") ?? true);
		}

		internal static EmpirePropertySpecies SpeciesFromNode(Entity Node)
		{
			if (IsNodeSpecies(Node))
				return new EmpirePropertySpecies(Node);
			else
				return null;
		}
	}
}
