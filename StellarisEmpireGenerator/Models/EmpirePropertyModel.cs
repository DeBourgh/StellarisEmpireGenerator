using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;

using StellarisEmpireGenerator.Core;

namespace StellarisEmpireGenerator.Models
{
	public abstract class EmpireProperty
	{
		protected static readonly string[] LogicGatesAsString = Enum.GetNames(typeof(Condition)).Select(e => e.ToLower()).ToArray();
		protected static readonly string[] EmpirePropertyTypesAsString = Enum.GetNames(typeof(EmpirePropertyType)).Select(e => e.ToLower()).ToArray();

		protected EmpireProperty() { }
		protected EmpireProperty(Entity SourceEntity, EmpirePropertyType Type)
		{
			this.Identifier = SourceEntity.Key;
			this.Type = Type;
			this.SourceEntity = SourceEntity;
		}

		#region Properties

		public string Identifier { get; private set; } = string.Empty;
		public string Name { get; private set; } = string.Empty;
		public int Weight { get; set; } = 1;
		public EmpirePropertyType Type { get; private set; } = EmpirePropertyType.Unknown;
		public IEnumerable<string> Dlc { get; private set; } = Enumerable.Empty<string>();

		public Constraint<EmpireProperty> Constraints { get; private set; }

		protected Constraint<EmpireProperty> Possible { get; private set; } = Constraint<EmpireProperty>.True;
		protected Constraint<EmpireProperty> Potential { get; private set; } = Constraint<EmpireProperty>.True;
		public Entity SourceEntity { get; private set; }

		public EmpirePropertyAuthority AsAuthority { get => this as EmpirePropertyAuthority; }
		public EmpirePropertyCivic AsCivic { get => this as EmpirePropertyCivic; }
		public EmpirePropertyEthic AsEthic { get => this as EmpirePropertyEthic; }
		public EmpirePropertyOrigin AsOrigin { get => this as EmpirePropertyOrigin; }
		public EmpirePropertySpecies AsSpecies { get => this as EmpirePropertySpecies; }
		public EmpirePropertyTrait AsTrait { get => this as EmpirePropertyTrait; }

		#endregion
		public static IEnumerable<EmpireProperty> FromEntityRoot(Entity Root)
		{
			ICollection<EmpireProperty> properties = new List<EmpireProperty>();
			ICollection<EmpireProperty> filter = new List<EmpireProperty>();

			if (!Root.Children.Any())
				return null;
			else
			{
				var children = Root.Children;

				ICollection<(EmpireProperty Prop, IEnumerable<string> Opposites)> traitOpposites = new LinkedList<(EmpireProperty, IEnumerable<string>)>();

				// Extract base properties
				foreach (var entity in children)
				{
					EmpireProperty ep;

					if ((ep = EmpirePropertyEthic.EthicFromNode(entity)) != null) { }
					else if ((ep = EmpirePropertyAuthority.AuthorityFromNode(entity)) != null) { }
					else if ((ep = EmpirePropertyOrigin.OriginFromNode(entity)) != null) { }
					else if ((ep = EmpirePropertyCivic.CivicFromNode(entity)) != null) { }
					else if ((ep = EmpirePropertyTrait.TraitFromNode(entity)) != null) { }
					else if ((ep = EmpirePropertySpecies.SpeciesFromNode(entity)) != null) { }

					if (ep != null)
						properties.Add(ep);
				}

				//var types = Enum.GetNames(typeof(EmpirePropertyType)).Select(x => x.ToLower()).ToArray();
				//var children2 = properties.SelectMany(e => e.SourceEntity.Children.WhereKey("possible").Concat(e.SourceEntity.Children.WhereKey("potential")));
				////var children2 = properties.Where(e => e.SourceEntity.Children.ContainsKey("possible") || e.SourceEntity.Children.ContainsKey("potential")).Select(x => x.SourceEntity);
				//var children3 = children2.SelectMany(e => e.Children.Where(e2 => !types.Contains(e2.Key)));

				foreach (var prop in properties)
				{
					prop.Possible = ExtractContraints("possible", prop.SourceEntity, properties, ConstraintAllocator);
					prop.Potential = ExtractContraints("potential", prop.SourceEntity, properties, ConstraintAllocator);

					prop.UpdateRelationsToOtherEmpireProperties(properties);

					prop.MergeConstraints();

					if (!properties.Any(p => prop.Potential.Evaluate(p, (p1, p2) => p1.Identifier == p2.Identifier)))
						filter.Add(prop);
				}
			}

			return properties.Except(filter);
		}

		public static void ApplyLanguage(IEnumerable<EmpireProperty> Properties, LanguageDictionary Dict)
		{
			foreach (var prop in Properties)
			{
				prop.Name = Dict[prop.Identifier];

				if (string.IsNullOrWhiteSpace(prop.Name))
					prop.Name = prop.Identifier;
			}
		}

		private void MergeConstraints()
		{
			Constraints = new Constraint<EmpireProperty>(Condition.Each);

			foreach (var objs in Potential.Objects.Concat(Possible.Objects))
				Constraints.Objects.Add(objs);

			var subCons = Potential.SubConstraints.Concat(Possible.SubConstraints);

			if (subCons.Any())
			{
				var first = subCons.First();
				Constraints.SubConstraints.Add(first);

				foreach (var subCon in subCons.Skip(1))
				{
					bool added = false;
					foreach (var con in Constraints.SubConstraints)
					{
						if (con.Group == subCon.Group)
						{
							added = true;

							foreach (var groupSubCons in subCon.SubConstraints)
								con.SubConstraints.Add(groupSubCons);

							break;
						}
					}

					if (!added)
						Constraints.SubConstraints.Add(subCon);
				}
			}
		}

		private static bool IsLogicGate(Entity Node)
		{
			return LogicGatesAsString.Contains(Node.Key.ToLower());
		}

		private static object ConstraintAllocator(Entity Node, IEnumerable<EmpireProperty> Properties)
		{
			if (Node.Key == "value")
			{
				var entity = Properties.FirstOrDefault(e => e.Identifier == Node.Text);

				if (entity != null)
					return entity;
				else
					return Constraint<EmpireProperty>.False;
			}

			return null;
		}
		//private static object HostHasDlcAllocator(Entity Node, IEnumerable<EmpireProperty> Properties)
		//{
		//	if (Node.Key == "host_has_dlc")
		//	{
		//		var entity = Properties.FirstOrDefault(e => e.Identifier == Node.Text);

		//		if (entity != null)
		//			return entity;
		//		else
		//			return Constraint<EmpireProperty>.False;
		//	}

		//	return null;
		//}

		private static Constraint<EmpireProperty> ExtractSubConstraints(Entity Node, IEnumerable<EmpireProperty> Properties, Func<Entity, IEnumerable<EmpireProperty>, object> Allocator)
		{
			Constraint<EmpireProperty> constraint = new Constraint<EmpireProperty>(Condition.Each);

			if (Enum.TryParse(Node.Key, true, out EmpirePropertyType group))
				constraint.Group = group;

			if (IsLogicGate(Node))
			{
				constraint.LogicGate = (Condition)Enum.Parse(typeof(Condition), Node.Key, true);
			}

			foreach (var child in Node.Children)
			{
				if (child.HasChildren)
					constraint.SubConstraints.Add(ExtractSubConstraints(child, Properties, Allocator));
				else
				{
					var text = child.Text;

					if (text != null)
					{
						if (child.Key == "always")
						{
							if (text == "yes")
								constraint.SubConstraints.Add(Constraint<EmpireProperty>.True);
							else if (text == "no")
								constraint.SubConstraints.Add(Constraint<EmpireProperty>.False);
						}
						else
						{
							var obj = Allocator(child, Properties);

							if (obj is EmpireProperty prop)
								constraint.Objects.Add(prop);
							else if (obj is Constraint<EmpireProperty> cons)
								constraint.SubConstraints.Add(cons);
						}
					}
				}
			}

			return constraint;
		}
		private static Constraint<EmpireProperty> ExtractContraints(string KeyNode, Entity Node, IEnumerable<EmpireProperty> Properties, Func<Entity, IEnumerable<EmpireProperty>, object> Allocator)
		{
			var keyNode = Node.Children.FirstOrDefaultKey(KeyNode);

			if (keyNode == null)
				return Constraint<EmpireProperty>.True;
			else
			{
				Constraint<EmpireProperty> constraints = ExtractSubConstraints(keyNode, Properties, Allocator);
				return constraints;
			}
		}

		//protected static IEnumerable<EmpirePropertyTrait> ExtractPotentialSecondarySpecies(Entity Node, IEnumerable<EmpireProperty> Properties)
		//{
		//	return Properties
		//		.Where(p => p is EmpirePropertyTrait);

		//	return Node.Children
		//		.FirstOrDefaultKey("has_secondary_species")?.Descendants
		//		.Where(e => e.Text != null)
		//		.Select(e => e.Text);
		//}

		private static IEnumerable<string> ExtractDlc(Entity Node)
		{
			return Node.Children.FirstOrDefaultKey("playable")?.Descendants.WhereKey("host_has_dlc").Select(e => e.Key) ?? Enumerable.Empty<string>();
			//return 
			//	Node.Descendants.WhereKey("host_has_dlc", e => e.Ancestors.ContainsKey("playable"))?.Text ?? null;
		}

		public override string ToString()
		{
			switch (Type)
			{
				default:
					return string.Format("{0}: (Name: {2}, Type: {1}, Dlc: {3}, Weight: {4})",
						Identifier,
						Type,
						Name,
						Dlc,
						Weight);
			}

		}

		public override bool Equals(object obj)
		{
			if (obj != null && obj.GetType() == this.GetType())
				return ((EmpireProperty)obj).Identifier == this.Identifier;
			else
				return false;
		}

		public override int GetHashCode()
		{
			return Identifier.GetHashCode();
		}

		protected abstract void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties);

		public static IEnumerable<EmpireProperty> LoadFromFile(string Path)
		{
			string input = File.ReadAllText(Path);

			return JsonConvert.DeserializeObject<IEnumerable<EmpireProperty>>(input);
		}

		public static void SaveToFile(IEnumerable<EmpireProperty> Model, string Path)
		{
			string output = JsonConvert.SerializeObject(Model, Formatting.Indented);

			File.WriteAllText(Path, output, System.Text.Encoding.UTF8);
		}
	}

	public sealed class EmpirePropertyEthic : EmpireProperty
	{
		private EmpirePropertyEthic(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Ethics)
		{
			EthicCost = ExtractEthicCost(SourceEntity);
			EthicCategory = ExtractEthicCategory(SourceEntity);
		}

		public int EthicCost { get; private set; } = 1;

		public string EthicCategory { get; private set; } = string.Empty;
		public EmpirePropertyEthic NonFanaticVariant { get; private set; } = null;
		public EmpirePropertyEthic FanaticVariant { get; private set; } = null;

		public bool IsFanatic { get => FanaticVariant != null; }

		private static int ExtractEthicCost(Entity Node)
		{
			return int.Parse(Node.Children.FirstOrDefaultKey("cost")?.Text ?? default);
		}

		private static string ExtractEthicCategory(Entity Node)
		{
			return Node.Children.FirstOrDefaultKey("category")?.Text ?? string.Empty;
		}
		private static Entity ExtractFanaticVariant(Entity Node)
		{
			return Node.Children.FirstOrDefaultKey("fanatic_variant");
		}

		private static bool IsEthic(Entity Node)
		{
			return
				Node.Key.StartsWith("ethic_") &&
				Node.Children.ContainsKey("cost");
		}

		internal static EmpirePropertyEthic EthicFromNode(Entity Node)
		{
			if (IsEthic(Node))
				return new EmpirePropertyEthic(Node);
			else
				return null;
		}

		protected override void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties)
		{
			var fnt = ExtractFanaticVariant(SourceEntity);

			if (fnt != null)
			{
				var fntProperty = Properties.First(p => p.Identifier == fnt.Text) as EmpirePropertyEthic;

				FanaticVariant = fntProperty;
				NonFanaticVariant = this;

				fntProperty.FanaticVariant = fntProperty;
				fntProperty.NonFanaticVariant = this;
			}
		}
	}

	public sealed class EmpirePropertyAuthority : EmpireProperty
	{
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

	public sealed class EmpirePropertyOrigin : EmpireProperty
	{
		private EmpirePropertyOrigin(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Origin) { }

		public IEnumerable<EmpirePropertyTrait> SecondarySpeciesTraits { get; private set; } = Enumerable.Empty<EmpirePropertyTrait>();

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

	public sealed class EmpirePropertyCivic : EmpireProperty
	{
		private EmpirePropertyCivic(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Civics) { }

		public IEnumerable<EmpirePropertyTrait> SecondarySpeciesTraits { get; private set; } = Enumerable.Empty<EmpirePropertyTrait>();

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

	public sealed class EmpirePropertyTrait : EmpireProperty
	{
		private EmpirePropertyTrait(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Trait)
		{
			TraitCost = ExtractTraitCost(SourceEntity);
		}

		private IEnumerable<EmpireProperty> TraitAllowedSpecies { get; set; } = null;

		public int TraitCost { get; private set; } = default;

		private IEnumerable<EmpireProperty> TraitOpposites { get; set; } = null;

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
			foreach (var prop in Properties)
			{
				EmpirePropertyTrait trait = prop.AsTrait;
				if (trait == null)
					continue;

				trait.TraitOpposites = ExtractTraitOpposites(trait.SourceEntity, Properties);
				trait.TraitAllowedSpecies = ExtractTraitAllowedArchetypes(trait.SourceEntity, Properties);
			}

			if (TraitOpposites != null && TraitOpposites.Count() > 0)
			{
				Constraint<EmpireProperty> constraintTrait = new Constraint<EmpireProperty>(Condition.Each)
				{
					Group = EmpirePropertyType.Trait
				};

				Constraint<EmpireProperty> constraintTraitNor = new Constraint<EmpireProperty>(Condition.Nor);
				foreach (var opp in TraitOpposites)
				{
					constraintTraitNor.Objects.Add(opp);
				}

				constraintTrait.SubConstraints.Add(constraintTraitNor);
				Possible.SubConstraints.Add(constraintTrait);
			}

			if (TraitAllowedSpecies != null && TraitAllowedSpecies.Count() > 0)
			{
				Constraint<EmpireProperty> constraintSpec = new Constraint<EmpireProperty>(Condition.Each)
				{
					Group = EmpirePropertyType.Species
				};

				Constraint<EmpireProperty> constraintSpecOr = new Constraint<EmpireProperty>(Condition.Or);
				foreach (var spec in TraitAllowedSpecies)
				{
					constraintSpecOr.Objects.Add(spec);
				}

				constraintSpec.SubConstraints.Add(constraintSpecOr);
				Possible.SubConstraints.Add(constraintSpec);
			}
		}
	}

	public sealed class EmpirePropertySpecies : EmpireProperty
	{
		private EmpirePropertySpecies(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Species)
		{
			Archetype = ExtractArchetype(SourceEntity);
			CanBeSecondarySpecies = ExtractCanBeSecondary(SourceEntity);

			(int TraitPoints, int MaxTraitPoints) = ExtractTraitPoints(SourceEntity);
			this.TraitPoints = TraitPoints;
			this.MaxTraitPoints = MaxTraitPoints;
		}

		public string Archetype { get; private set; } = string.Empty;
		public int MaxTraitPoints { get; private set; } = default;
		public int TraitPoints { get; private set; } = default;
		public bool CanBeSecondarySpecies { get; set; } = true;

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

		private static bool IsSpecies(Entity Node)
		{
			return
				Node.Children.ContainsKey("archetype") &&
				(!Node.Descendants.FirstOrDefaultPair("always", "no")?.Ancestors.ContainsKey("playable") ?? true);
		}

		internal static EmpirePropertySpecies SpeciesFromNode(Entity Node)
		{
			if (IsSpecies(Node))
				return new EmpirePropertySpecies(Node);
			else
				return null;
		}

		protected override void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties) { }
	}

	public class Constraint<T>
	{
		public Constraint(Condition LogicGate)
		{
			this.LogicGate = LogicGate;
		}

		public Condition LogicGate { get; set; }

		public ICollection<T> Objects { get; } = new LinkedList<T>();

		public ICollection<Constraint<T>> SubConstraints { get; } = new LinkedList<Constraint<T>>();

		public EmpirePropertyType Group { get; set; } = EmpirePropertyType.Unknown;

		private bool Each<TEval>(TEval EvalItem, Func<T, TEval, bool> Validator)
		{
			foreach (var constraint in Objects)
			{
				if (!Validator(constraint, EvalItem))
					return false;
			}

			foreach (var constraint in SubConstraints)
			{
				if (!constraint.Evaluate(EvalItem, Validator))
					return false;
			}

			return true;
		}
		private bool Any<TEval>(TEval EvalItem, Func<T, TEval, bool> Validator)
		{
			foreach (var constraint in Objects)
			{
				if (Validator(constraint, EvalItem))
					return true;
			}

			foreach (var constraint in SubConstraints)
			{
				if (constraint.Evaluate(EvalItem, Validator))
					return true;
			}

			return false;
		}
		public bool Evaluate<TEval>(TEval EvalItem, Func<T, TEval, bool> Validator)
		{
			if (Validator is null)
				throw new ArgumentNullException(nameof(Validator));

			switch (LogicGate)
			{
				case Condition.Each:
					return Each(EvalItem, Validator);
				case Condition.Or:
					return Any(EvalItem, Validator);
				case Condition.Not:
					return !Each(EvalItem, Validator);
				case Condition.Nor:
					return !Any(EvalItem, Validator);
				default:
					return false;
			}
		}

		public static Constraint<T> True
		{
			get
			{
				Constraint<T> constraint = new Constraint<T>(Condition.Each);
				return constraint;
			}
		}

		public static Constraint<T> False
		{
			get
			{
				Constraint<T> constraint = new Constraint<T>(Condition.Or);
				return constraint;
			}
		}
	}

	public enum Condition
	{
		Each, // All, And
		Or, // Any, Or
		Not, // Not
		Nor, // !All
		Unknown
	}

	public enum EmpirePropertyType
	{
		Civics,
		Trait,
		Origin,
		Authority,
		Ethics,
		Species,
		Unknown,
	}
}
