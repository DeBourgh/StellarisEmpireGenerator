using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public abstract class EmpireProperty
	{
		public static int MaxAuthorities { get; set; } = 1;
		public static int MaxCivics { get; set; } = 2;
		public static int MaxEthics { get; set; } = 3;
		public static int MaxOrigins { get; set; } = 1;
		public static int MaxSpecies { get; set; } = 1;
		public static int MaxTraits { get; set; } = -1; // Is not a fixed value, depends on the species

		protected static readonly string[] LogicGatesAsString = new string[] { "nor", "or", "not" };

		protected static readonly string[] EmpirePropertyTypesAsString = Enum.GetNames(typeof(EmpirePropertyType)).Select(e => e.ToLower()).ToArray();

		protected EmpireProperty() { }
		protected EmpireProperty(Entity SourceEntity, EmpirePropertyType Type)
		{
			this.Identifier = SourceEntity.Key;
			this.Type = Type;
			this.SourceEntity = SourceEntity;
		}

		#region Properties

		public string Identifier { get; set; } = string.Empty;

		public int Weight { get; set; } = 1;

		public bool IsAllowed { get; set; } = true;

		public EmpirePropertyType Type { get; set; } = EmpirePropertyType.Unknown;

		public Constraint Constraints { get; set; } = null;

		public virtual int Cost { get; set; } = 1;

		[JsonIgnore]
		public Entity SourceEntity { get; private set; }

		[JsonIgnore]
		public EmpirePropertyAuthority AsAuthority => this as EmpirePropertyAuthority;
		[JsonIgnore]
		public EmpirePropertyCivic AsCivic => this as EmpirePropertyCivic;
		[JsonIgnore]
		public EmpirePropertyEthic AsEthic => this as EmpirePropertyEthic;
		[JsonIgnore]
		public EmpirePropertyOrigin AsOrigin => this as EmpirePropertyOrigin;
		[JsonIgnore]
		public EmpirePropertySpecies AsSpecies => this as EmpirePropertySpecies;
		[JsonIgnore]
		public EmpirePropertyTrait AsTrait => this as EmpirePropertyTrait;

		[JsonIgnore]
		public bool IsAuthority => Type == EmpirePropertyType.Authority;
		[JsonIgnore]
		public bool IsCivic => Type == EmpirePropertyType.Civics;
		[JsonIgnore]
		public bool IsEthic => Type == EmpirePropertyType.Ethics;
		[JsonIgnore]
		public bool IsOrigin => Type == EmpirePropertyType.Origin;
		[JsonIgnore]
		public bool IsSpecies => Type == EmpirePropertyType.Species;
		[JsonIgnore]
		public bool IsTrait => Type == EmpirePropertyType.Trait;

		//public abstract int MaxCountPerType { get; }

		#endregion

		#region Extraction Logic

		private bool ResolveValues(Constraint Into, Entity Node, IEnumerable<EmpireProperty> Properties, EmpirePropertyType Type, bool IsLogicGate)
		{
			var values = Node.Children.WhereKey("value").Select(e => e.Text).ToList();

			var properties = Properties.Where(p => values.Contains(p.Identifier));

			string logicKey;
			if (IsLogicGate && LogicGatesAsString.Contains(logicKey = Node.Key.ToLower()))
			{
				Condition logicGate = (Condition)Enum.Parse(typeof(Condition), logicKey, true);
				Into.Add(logicGate, Type, properties);

			}
			else if (values.Any())
			{
				if (!properties.Any())
					return false;

				Into.Add(Condition.Required, Type, properties);
			}

			return true;
		}

		protected virtual Constraint ExtractConstraint(IEnumerable<EmpireProperty> Properties)
		{
			Constraint constraint = new Constraint();

			var potentialChildren = SourceEntity.Children.FirstOrDefaultKey("potential")?.Children ?? Enumerable.Empty<Entity>();
			var possibleChildren = SourceEntity.Children.FirstOrDefaultKey("possible")?.Children ?? Enumerable.Empty<Entity>();

			var empireProperties = potentialChildren.Concat(possibleChildren);

			foreach (var empireProperty in empireProperties)
			{
				var key = empireProperty.Key;
				var text = empireProperty.Text;

				if (EmpirePropertyTypesAsString.Contains(key))
				{
					var empirePropType = (EmpirePropertyType)Enum.Parse(typeof(EmpirePropertyType), key, true);

					if (!ResolveValues(constraint, empireProperty, Properties, empirePropType, false))
						return null;

					foreach (var epChild in empireProperty.Children)
					{
						if (!ResolveValues(constraint, epChild, Properties, empirePropType, true))
							return null;

					}

				}
				else if (key == "always")
				{
					if (text == "yes")
						constraint.Always = true;
					else if (text == "no")
						constraint.Always = false;
				}
			}

			return constraint;
		}

		#endregion

		#region IO

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

				foreach (var prop in properties.Where(p => p.Type == EmpirePropertyType.Species))
				{
					var constraints = prop.ExtractConstraint(properties);
					prop.Constraints = constraints;

					if (constraints == null)
						filter.Add(prop);
				}


				foreach (var prop in properties.Where(p => p.Type != EmpirePropertyType.Species))
				{
					var constraints = prop.ExtractConstraint(properties);
					prop.Constraints = constraints;

					if (constraints == null)
						filter.Add(prop);
				}
			}

			return properties.Except(filter);
		}

		public static List<EmpireProperty> FromFile(string Path)
		{
			string input = File.ReadAllText(Path);

			return JsonConvert.DeserializeObject<List<EmpireProperty>>(input, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
		}

		public static void ToFile(List<EmpireProperty> Model, string Path)
		{
			string output = JsonConvert.SerializeObject(
				Model,
				Formatting.Indented,
				new JsonSerializerSettings()
				{
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
					PreserveReferencesHandling = PreserveReferencesHandling.All,
					TypeNameHandling = TypeNameHandling.All
				});

			File.WriteAllText(Path, output, System.Text.Encoding.UTF8);
		}

		#endregion

		private static IEnumerable<string> ExtractDlc(Entity Node)
		{
			return Node.Children.FirstOrDefaultKey("playable")?.Descendants.WhereKey("host_has_dlc").Select(e => e.Key) ?? Enumerable.Empty<string>();
			//return 
			//	Node.Descendants.WhereKey("host_has_dlc", e => e.Ancestors.ContainsKey("playable"))?.Text ?? null;
		}

		#region Object Overrides

		public override string ToString()
		{
			return Identifier;
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

		#endregion

		#region Generate Solutions

		public static IEnumerable<EmpireProperty> GenerateSolution(IEnumerable<EmpireProperty> Properties)
		{
			if (Properties is null)
				throw new ArgumentNullException(nameof(Properties));

			GeneratorNode next = GeneratorNode.CreateRoot(Properties);

			while (true)
			{
				next = next.Next();

				if (next == null)
					return null;
				else if (next.IsSolution)
					return next.Solution;
			}
		}
		public static IEnumerable<IEnumerable<EmpireProperty>> GenerateSolution(IEnumerable<EmpireProperty> Properties, int Amount)
		{
			if (Properties is null)
				throw new ArgumentNullException();

			if (Amount < 1)
				throw new ArgumentException(nameof(Amount));

			ICollection<IEnumerable<EmpireProperty>> solutions = new List<IEnumerable<EmpireProperty>>(Amount);

			for (int i = 0; i < Amount; i++)
				solutions.Add(GenerateSolution(Properties));

			return solutions;
		}

		protected virtual IEnumerable<EmpireProperty> DependentProperties()
		{

			if (Constraints.IsAlwaysYes)
				return Enumerable.Empty<EmpireProperty>();
			else
				return Constraints.ConstraintGroups
					.SelectMany(cg => cg.Value)
					.Where(cg => (cg.LogicGate == Condition.Or) && (cg.Properties.Count == 1))
					.Select(cg => cg.Properties.First());
		}

		protected IEnumerable<EmpireProperty> RequiredBy(IEnumerable<EmpireProperty> Source)
		{
			return Source.Where(p => p.Constraints.Requires(this));
		}

		protected virtual IEnumerable<EmpireProperty> ExclusiveProperties(IEnumerable<EmpireProperty> Source)
		{
			//foreach (var )
			throw new NotImplementedException();
		}

		protected virtual bool OnAdding(EmpireProperty Pick, GeneratorNode Node)
		{
			foreach (var solItem in Node.Solution)
			{
				if (!ValidateEachOther(Pick, solItem))
					return false;
			}

			return true;
		}
		protected virtual void OnAdded(GeneratorNode Node)
		{
			var newRequiredConstraints = Constraints.ConstraintGroups.Values.SelectMany(cgs => cgs).Where(cg => cg.LogicGate == Condition.Or);
			var newRequiredConstraintsList = new LinkedList<ConstraintGroup>(newRequiredConstraints);
			Node.NonFulfilledRequiredConstraints.Add(this, newRequiredConstraintsList);

			foreach (var nfrc in Node.NonFulfilledRequiredConstraints)
			{
				var matchedConstraints = nfrc.Value.Where(cg => (cg.Type == Type) && cg.Validate(this)).ToArray();

				foreach (var matchedConstraint in matchedConstraints)
					nfrc.Value.Remove(matchedConstraint);
			}
		}

		protected virtual bool OnRemoving(GeneratorNode Node)
		{
			foreach (var solutionItem in Node.Solution)
			{
				if (!ValidateEachOther(this, solutionItem, Node))
					return true;
			}

			return false;
		}

		//protected virtual bool IsValidWith(EmpireProperty Prop)
		//{
		//	var propType = Prop.Type;
		//	if (Constraints.ConstraintGroups.TryGetValue(propType, out IEnumerable<ConstraintGroup> cgs))
		//	{
		//		int maxOfType = MaxByType(propType);

		//		foreach (var cg in cgs)
		//		{
		//			bool isValid = cg.Validate(Prop);

		//			if (cg.LogicGate == Condition.Or)
		//			{
		//				maxOfType -= cg.Properties.Min(p => p.Cost);

		//				if (isValid)
		//					return true;
		//				else if (maxOfType == 0)
		//					return false;
		//			}
		//			else if ((cg.LogicGate == Condition.Nor) && (!isValid))
		//				return false;
		//		}
		//	}

		//	return true;
		//}



		protected virtual bool IsValidWith(EmpireProperty Prop)
		{
			var propType = Prop.Type;
			if (Constraints.IsAlwaysYes)
				return true;
			else if (Constraints.ConstraintGroups.TryGetValue(propType, out IEnumerable<ConstraintGroup> cgs))
			{
				int maxOfType = MaxByType(propType);

				foreach (var cg in cgs)
				{
					bool isValid = cg.Validate(Prop);

					if (cg.LogicGate == Condition.Or)
					{
						maxOfType -= cg.Properties.Min(p => p.Cost);

						if (isValid)
							return true;
						else if (Prop.Cost > maxOfType)
							return false;
					}
					else if ((cg.LogicGate == Condition.Nor) && (!isValid))
						return false;
				}
			}

			return true;
		}

		protected virtual bool IsValidWith(EmpireProperty Prop, GeneratorNode Node)
		{
			var propType = Prop.Type;
			if (Constraints.IsAlwaysYes)
				return true;
			else if (Constraints.ConstraintGroups.TryGetValue(propType, out IEnumerable<ConstraintGroup> cgs))
			{
				//if (Node.Solution.Contains(this))
				//{
				int maxOfType = Node.AvailableByType(propType);

				foreach (var cg in cgs)
				{
					bool isValid = cg.Validate(Prop);

					if (cg.LogicGate == Condition.Or)
					{
						maxOfType -= cg.Properties.Min(p => p.Cost);

						if (isValid)
							return true;
						else if (Prop.Cost > maxOfType)
							return false;
					}
					else if ((cg.LogicGate == Condition.Nor) && (!isValid))
						return false;
				}
				//}
				//else
				//{
				//	foreach (var cg in cgs.Where(c => c.LogicGate == Condition.Nor))
				//	{
				//		bool isValid = cg.Validate(Prop);
				//		if (!isValid)
				//			return false;
				//	}

				//	if (Node.NonFulfilledRequiredConstraints.TryGetValue(this, out ICollection<ConstraintGroup> nonFulfilledRequiredConstraints))
				//	{
				//		int maxOfType = Node.AvailableByType(propType);

				//		foreach (var cg in nonFulfilledRequiredConstraints)
				//		{
				//			maxOfType -= cg.Properties.Min(p => p.Cost);

				//			bool isValid = cg.Validate(Prop);
				//			if (isValid)
				//				return true;
				//			else if (Prop.Cost > maxOfType)
				//				return false;
				//		}
				//	}

				//}
			}

			return true;
		}



		//protected virtual bool ValidateTo(EmpireProperty Prop)
		//{
		//	var propType = Prop.Type;
		//	if (Constraints.ConstraintGroups.TryGetValue(propType, out IEnumerable<ConstraintGroup> cgs))
		//	{
		//		int maxOfType = MaxByType(propType);

		//		foreach (var cg in cgs)
		//		{
		//			bool isValid = cg.Validate(Prop);

		//			if (cg.LogicGate == Condition.Or)
		//			{
		//				maxOfType--;

		//				if (isValid)
		//					return true;
		//				else if (maxOfType == 0)
		//					return false;
		//			}
		//			else if ((cg.LogicGate == Condition.Nor) && (!isValid))
		//				return false;

		//		}
		//	}

		//	return true;
		//}

		//protected virtual bool ValidateTo(EmpireProperty Prop, GeneratorNode Node)
		//{
		//	var propType = Prop.Type;
		//	if (Constraints.ConstraintGroups.TryGetValue(propType, out IEnumerable<ConstraintGroup> cgs))
		//	{
		//		int maxOfType = Node.MaxByType(propType);

		//		foreach (var cg in cgs)
		//		{
		//			bool isValid = cg.Validate(Prop);

		//			if (cg.LogicGate == Condition.Or)
		//			{
		//				maxOfType--;

		//				if (isValid)
		//					return true;
		//				else if (maxOfType == 0)
		//					return false;
		//			}
		//			else if ((cg.LogicGate == Condition.Nor) && (!isValid))
		//				return false;
		//		}
		//	}

		//	return true;
		//}
		//protected virtual bool ValidateTo(EmpireProperty Prop, GeneratorNode Node)
		//{
		//	var propType = Prop.Type;
		//	if (Constraints.ConstraintGroups.TryGetValue(propType, out IEnumerable<ConstraintGroup> cgs))
		//	{
		//		int maxOfType = Node.MaxByType(propType);

		//		foreach (var cg in cgs)
		//		{
		//			bool isValid = cg.Validate(Prop);

		//			if (cg.LogicGate == Condition.Or)
		//			{
		//				maxOfType -= cg.Properties.Min(p => p.Cost);

		//				if (isValid)
		//					return true;
		//				else if (maxOfType == 0)
		//					return false;
		//			}
		//			else if ((cg.LogicGate == Condition.Nor) && (!isValid))
		//				return false;
		//		}
		//	}

		//	return true;
		//}
		//protected virtual bool ValidateTo(EmpireProperty Prop, GeneratorNode Node)
		//{
		//	var propType = Prop.Type;
		//	if (Constraints.ConstraintGroups.TryGetValue(propType, out IEnumerable<ConstraintGroup> cgs))
		//	{
		//		int maxOfType = Node.MaxByType(propType);

		//		foreach (var cg in cgs)
		//		{
		//			bool isValid = cg.Validate(Prop);

		//			if (cg.LogicGate == Condition.Or)
		//			{
		//				maxOfType -= cg.Properties.Min(p => p.Cost);

		//				if (isValid)
		//					return true;
		//				else if (maxOfType == 0)
		//					return false;
		//			}
		//			else if ((cg.LogicGate == Condition.Nor) && (!isValid))
		//				return false;
		//		}
		//	}

		//	return true;
		//}

		//protected virtual bool ValidateTo(IEnumerable<EmpireProperty> Properties, GeneratorNode Node)
		//{
		//	foreach (var prop in Properties)
		//	{
		//		if (!ValidateEachOther(this, prop, Node))
		//			return false;
		//	}

		//	return true;
		//}

		//protected virtual bool ValidateTo(IEnumerable<EmpireProperty> Properties)
		//{
		//	foreach (var prop in Properties)
		//	{
		//		if (!ValidateEachOther(this, prop))
		//			return false;
		//	}

		//	return true;
		//}

		//protected static bool ValidateEachOther(EmpireProperty Prop1, EmpireProperty Prop2)
		//{
		//	if (!Prop1.ValidateTo(Prop2))
		//		return false;

		//	if (!Prop2.ValidateTo(Prop1))
		//		return false;

		//	return true;
		//}

		protected static bool ValidateEachOther(EmpireProperty Prop1, EmpireProperty Prop2)
		{
			if (!Prop1.IsValidWith(Prop2))
				return false;

			if (!Prop2.IsValidWith(Prop1))
				return false;

			return true;
		}
		protected static bool ValidateEachOther(EmpireProperty Prop1, EmpireProperty Prop2, GeneratorNode Node)
		{
			if (!Prop1.IsValidWith(Prop2, Node))
				return false;

			if (!Prop2.IsValidWith(Prop1, Node))
				return false;

			return true;
		}

		public static int MaxByType(EmpirePropertyType Type)
		{
			switch (Type)
			{
				case EmpirePropertyType.Authority:
					return MaxAuthorities;
				case EmpirePropertyType.Civics:
					return MaxCivics;
				case EmpirePropertyType.Ethics:
					return MaxEthics;
				case EmpirePropertyType.Origin:
					return MaxOrigins;
				case EmpirePropertyType.Species:
					return MaxSpecies;
				case EmpirePropertyType.Trait:
					return MaxTraits;
				default:
					return int.MinValue;
			}
		}

		protected class GeneratorNode
		{
			private static Random CreateRnd(int? Seed = null)
			{
				int seed = Seed ?? Environment.TickCount;

				Debug.WriteLine(seed);

				return new Random(seed);
			}

			public static readonly Random Rnd = CreateRnd(2109574937);

			private GeneratorNode(IEnumerable<EmpireProperty> Properties)
			{
				Parent = null;
				Solution = new HashSet<EmpireProperty>();
				RemainingProperties = new HashSet<EmpireProperty>(Properties.OrderBy(p => $"{p.Type}_{p.Identifier}"));
				WeightSum = RemainingProperties.Sum(p => p.Weight);
				RemoveSet = new HashSet<EmpireProperty>();
			}

			private GeneratorNode(GeneratorNode Parent)
			{
				this.Parent = Parent;
				HasAuthority = Parent.HasAuthority;
				CivicPointsAvailable = Parent.CivicPointsAvailable;
				EthicPointsAvailable = Parent.EthicPointsAvailable;
				HasOrigin = Parent.HasOrigin;
				HasSpecies = Parent.HasSpecies;
				TraitPointsAvailable = Parent.TraitPointsAvailable;
				TraitPointsBalance = Parent.TraitPointsBalance;
				HasTraits = Parent.HasTraits;

				MaxTraits = Parent.MaxTraits;


				Solution = new HashSet<EmpireProperty>(Parent.Solution);
				RemainingProperties = new HashSet<EmpireProperty>(Parent.RemainingProperties);
				WeightSum = Parent.WeightSum;
				NonFulfilledRequiredConstraints = new Dictionary<EmpireProperty, ICollection<ConstraintGroup>>(Parent.NonFulfilledRequiredConstraints);

				NextIterationRules = Parent.NextIterationRules;

				RemoveSet = Parent.RemoveSet;
			}

			public GeneratorNode Parent { get; private set; }

			public bool IsSolution { get => HasAuthority && HasCivics && HasEthics && HasOrigin && HasSpecies && HasTraits; }

			public int MaxTraits { get; set; } = -1;
			public int TraitPointsBalance { get; set; } = 0;

			public int AvailableByType(EmpirePropertyType Type)
			{
				switch (Type)
				{
					case EmpirePropertyType.Authority:
						return HasAuthority ? 0 : MaxByType(Type);
					case EmpirePropertyType.Civics:
						return CivicPointsAvailable;
					case EmpirePropertyType.Ethics:
						return EthicPointsAvailable;
					case EmpirePropertyType.Origin:
						return HasOrigin ? 0 : MaxByType(Type);
					case EmpirePropertyType.Species:
						return HasSpecies ? 0 : MaxByType(Type);
					case EmpirePropertyType.Trait:
						return TraitPointsAvailable;
					default:
						return int.MinValue;
				}
			}
			public int MaxByType(EmpirePropertyType Type)
			{
				switch (Type)
				{
					case EmpirePropertyType.Trait:
						return MaxTraits;
					default:
						return EmpireProperty.MaxByType(Type);
				}
			}

			public bool HasAuthority { get; set; } = false;

			public int CivicPointsAvailable { get; set; } = EmpireProperty.MaxByType(EmpirePropertyType.Civics);
			public bool HasCivics => CivicPointsAvailable == 0;

			public int EthicPointsAvailable { get; set; } = EmpireProperty.MaxByType(EmpirePropertyType.Ethics);
			public bool HasEthics => EthicPointsAvailable == 0;
			public bool HasOrigin { get; set; } = false;
			public bool HasSpecies { get; set; } = false;

			public int TraitPointsAvailable { get; set; } = -1;
			public bool HasTraits { get; set; } = false;
			public bool AreTraitsValid => (TraitPointsAvailable >= 0) && (TraitPointsBalance == 0);

			public ICollection<EmpireProperty> Solution { get; private set; }

			public HashSet<EmpireProperty> RemainingProperties { get; private set; }

			public IDictionary<EmpireProperty, ICollection<ConstraintGroup>> NonFulfilledRequiredConstraints { get; } = new Dictionary<EmpireProperty, ICollection<ConstraintGroup>>();

			public int WeightSum { get; private set; }

			public HashSet<EmpireProperty> RemoveSet { get; private set; }

			public void AddIterationRule(Func<GeneratorNode, bool> RuleCondition, Func<EmpireProperty, bool> Rule)
			{
				if (RuleCondition is null)
					throw new ArgumentNullException(nameof(RuleCondition));

				if (Rule is null)
					throw new ArgumentNullException(nameof(Rule));

				if (NextIterationRules is null)
					NextIterationRules = new LinkedList<(Func<GeneratorNode, bool> Condition, Func<EmpireProperty, bool> Rule)>();

				NextIterationRules.AddLast((RuleCondition, Rule));
			}
			private LinkedList<(Func<GeneratorNode, bool> Condition, Func<EmpireProperty, bool> Rule)> NextIterationRules { get; set; } = null;

			

			protected void RemoveImpossibleProperties()
			{
				foreach (var prop in RemainingProperties)
				{
					if (prop.OnRemoving(this))
						RemoveSet.Add(prop);
				}

				Queue<EmpireProperty> removeQ = new Queue<EmpireProperty>();
				foreach (var toRemove in RemoveSet)
				{
					if (!Solution.Contains(toRemove))
						removeQ.Enqueue(toRemove);
				}

				while (removeQ.Count > 0)
				{
					var next = removeQ.Dequeue();

					var depProps = next.RequiredBy(RemainingProperties);

					foreach (var depProp in depProps)
					{
						if (!RemoveSet.Contains(depProp))
						{
							RemoveSet.Add(depProp);
							removeQ.Enqueue(depProp);
						}
					}
				}

				foreach (var rem in RemoveSet)
					RemoveSingleProperty(rem);

				RemoveSet.Clear();
			}

			private void RemoveSingleProperty(EmpireProperty Prop)
			{
				if (RemainingProperties.Remove(Prop))
					WeightSum -= Prop.Weight;

				if ((CurrentSubset != null) && (CurrentSubset != RemainingProperties))
					CurrentSubset.Remove(Prop);
			}

			private EmpireProperty Pick(IEnumerable<EmpireProperty> Source)
			{
				EmpireProperty pick = null;

				int weight = (RemainingProperties == Source)
					? WeightSum
					: Source.Sum(p => p.Weight);
				int next = Rnd.Next(weight);
				int sum = 0;

				foreach (var remain in Source)
				{
					int curWeight = remain.Weight;
					sum += curWeight;

					if (next < sum)
					{
						pick = remain;
						break;
					}
				}

				return pick;
			}

			private HashSet<EmpireProperty> CurrentSubset { get; set; } = null;

			//public GeneratorNode Next()
			//{
			//	if (NextIterationRules != null && NextIterationRules.Count > 0)
			//	{
			//		foreach((Func<GeneratorNode, bool> Condition, Func<EmpireProperty, bool> Rule) rule in NextIterationRules)
			//		{
			//			if (rule.Condition.Invoke(this))
			//			{
			//				rule.Rule
			//			}
			//		}
			//	}
			//	else
			//		return Next(RemainingProperties);

			//}

			//private GeneratorNode Next (ICollection<EmpireProperty> Subset)
			//{
			//	while (CurrentSubset.Count > 0)
			//	{
			//		EmpireProperty pick = Pick(CurrentSubset);

			//		 COMMENT OUT FOLLOWING LINE
			//		if (Solution.Count == 1 && RemainingProperties.Any(p => p.Identifier == "civic_inwards_perfection"))
			//			pick = RemainingProperties.First(p => p.Identifier == "civic_inwards_perfection");
			//		else if (Solution.Count == 1 && RemainingProperties.Any(p => p.Identifier == "ethic_fanatic_xenophobe"))
			//			pick = RemainingProperties.First(p => p.Identifier == "ethic_fanatic_xenophobe");

			//		Queue<EmpireProperty> addQ = new Queue<EmpireProperty>();
			//		addQ.Enqueue(pick);

			//		do
			//		{
			//			EmpireProperty next = addQ.Dequeue();

			//			if (next.OnAdding(next, this))
			//			{
			//				Solution.Add(next);
			//				next.OnAdded(this);
			//				Remove(pick);

			//				var dependentProperties = next.DependentProperties();
			//				foreach (var dependent in dependentProperties)
			//				{
			//					if (!Solution.Contains(dependent))
			//					{
			//						if (RemainingProperties.Contains(dependent))
			//							addQ.Enqueue(dependent);
			//						else
			//							return Parent;
			//					}
			//				}
			//			}
			//			else
			//				return Parent;


			//		} while (addQ.Count > 0);

			//		RemoveImpossibleProperties();

			//		GeneratorNode nextNode = new GeneratorNode(this);
			//		return nextNode;
			//	}

			//	return Parent;
			//}

			public GeneratorNode Next()
			{
				if ((NextIterationRules != null) && (NextIterationRules.Count > 0))
				{
					do
					{
						var firstValue = NextIterationRules.First.Value;
						NextIterationRules.RemoveFirst();

						if (firstValue.Condition.Invoke(this))
						{
							CurrentSubset = RemainingProperties.Where(firstValue.Rule).ToHashSet();
							break;
						}

					} while (NextIterationRules.Count > 0);

					if (NextIterationRules.Count == 0)
						NextIterationRules = null;
				}

				if (CurrentSubset == null)
					CurrentSubset = RemainingProperties;

				while (CurrentSubset.Count > 0)
				{
					EmpireProperty pick = Pick(CurrentSubset);

					// COMMENT OUT FOLLOWING LINE
					//if (Solution.Count == 1 && RemainingProperties.Any(p => p.Identifier == "civic_inwards_perfection"))
					//	pick = RemainingProperties.First(p => p.Identifier == "civic_inwards_perfection");
					//else if (Solution.Count == 1 && RemainingProperties.Any(p => p.Identifier == "ethic_fanatic_xenophobe"))
					//	pick = RemainingProperties.First(p => p.Identifier == "ethic_fanatic_xenophobe");

					Queue<EmpireProperty> addQ = new Queue<EmpireProperty>();
					addQ.Enqueue(pick);

					do
					{
						EmpireProperty next = addQ.Dequeue();

						if (next.OnAdding(next, this))
						{
							Solution.Add(next);
							next.OnAdded(this);
							RemoveSingleProperty(pick);

							var dependentProperties = next.DependentProperties()
								//.Concat(
								//	RemainingProperties
								//	.Where(p => p.DependentProperties().Contains(next)))
								;
							
							foreach (var dependent in dependentProperties)
							{
								if (!Solution.Contains(dependent))
								{
									if (RemainingProperties.Contains(dependent))
										addQ.Enqueue(dependent);
									else
										return Parent;
								}
							}
						}
						else
							return Parent;
					} while (addQ.Count > 0);

					RemoveImpossibleProperties();

					GeneratorNode nextNode = new GeneratorNode(this);
					return nextNode;
				}

				return Parent;
			}

			public static GeneratorNode CreateRoot(IEnumerable<EmpireProperty> Properties)
			{
				return new GeneratorNode(Properties);
			}
		}

		#endregion
	}
}
