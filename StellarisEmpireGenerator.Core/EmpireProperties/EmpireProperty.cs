using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public class EmpireProperty
	{
		public static readonly int MaximumEthicPoints = 3;

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

		public string Identifier { get; set; } = string.Empty;

		//public string Name { get; set; } = string.Empty;

		public int Weight { get; set; } = 1;

		public bool IsAllowed { get; set; } = true;

		public EmpirePropertyType Type { get; set; } = EmpirePropertyType.Unknown;

		public Constraint<EmpireProperty> Constraints { get; set; } = Constraint<EmpireProperty>.True;

		[JsonIgnore]
		protected Constraint<EmpireProperty> Possible { get; private set; } = Constraint<EmpireProperty>.True;
		[JsonIgnore]
		protected Constraint<EmpireProperty> Potential { get; private set; } = Constraint<EmpireProperty>.True;

		[JsonIgnore]
		public Entity SourceEntity { get; private set; }

		//[JsonIgnore]
		[JsonIgnore]
		public EmpirePropertyAuthority AsAuthority { get => this as EmpirePropertyAuthority; }
		[JsonIgnore]
		public EmpirePropertyCivic AsCivic { get => this as EmpirePropertyCivic; }
		[JsonIgnore]
		public EmpirePropertyEthic AsEthic { get => this as EmpirePropertyEthic; }
		[JsonIgnore]
		public EmpirePropertyOrigin AsOrigin { get => this as EmpirePropertyOrigin; }
		[JsonIgnore]
		public EmpirePropertySpecies AsSpecies { get => this as EmpirePropertySpecies; }
		[JsonIgnore]
		public EmpirePropertyTrait AsTrait { get => this as EmpirePropertyTrait; }



		#endregion

		#region Extraction Logic

		private void MergeConstraints()
		{
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

			Constraints.Objects.Sort((c1, c2) => c1.Type.CompareTo(c2.Type));
			Constraints.SubConstraints.Sort((c1, c2) => c1.Group.CompareTo(c2.Group));
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

		protected virtual void UpdateRelationsToOtherEmpireProperties(IEnumerable<EmpireProperty> Properties) { }

		protected void AddConstraint(Condition LogicGate, EmpireProperty Property)
		{
			var type = Property.Type;
			Constraint<EmpireProperty> typeSubConstraint;

			if ((typeSubConstraint = Constraints.SubConstraints.FirstOrDefault(c => c.Group == type)) == null)
			{
				typeSubConstraint = new Constraint<EmpireProperty>(Condition.Each)
				{
					Group = type
				};

				Constraints.SubConstraints.Add(typeSubConstraint);
			}

			if (LogicGate == Condition.Each)
				typeSubConstraint.Objects.Add(Property);
			else
			{
				Constraint<EmpireProperty> logicGateSubConstraint;

				if ((logicGateSubConstraint = typeSubConstraint.SubConstraints.FirstOrDefault(c => c.LogicGate == LogicGate)) == null)
				{
					logicGateSubConstraint = new Constraint<EmpireProperty>(LogicGate);
					typeSubConstraint.SubConstraints.Add(logicGateSubConstraint);
				}

				logicGateSubConstraint.Objects.Add(Property);

			}
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

		#region Generating Solutions

		public static IEnumerable<EmpireProperty> GenerateSolution(IEnumerable<EmpireProperty> Properties)
		{
			if (Properties is null)
				throw new ArgumentNullException(nameof(Properties));

			throw new NotImplementedException();
		}
		public static IEnumerable<IEnumerable<EmpireProperty>> GenerateSolution(IEnumerable<EmpireProperty> Properties, int Amount)
		{
			if (Amount < 1)
				throw new ArgumentException(nameof(Amount));

			List<IEnumerable<EmpireProperty>> solutions = new List<IEnumerable<EmpireProperty>>(Amount);

			for (int i = 0; i < Amount; i++)
				solutions.Add(GenerateSolution(Properties));

			return solutions;
		}

		private class GeneratorNode
		{
			private static readonly Random rnd = new Random(Environment.TickCount);


		}

		//		public class GeneratorNode
		//		{
		//			private static readonly Random Rnd = new Random(Environment.TickCount);

		//			private GeneratorNode(IEnumerable<EmpireProperty> WeightedSource)
		//			{
		//				Solution = new List<EmpireProperty>();
		//				Properties = WeightedSource.OrderBy(p => $"{p.Type}_{p.Identifier}").ToList();
		//				RemainingIndexes = Enumerable.Range(0, WeightedSource.Count()).ToList();
		//			}

		//			private GeneratorNode(GeneratorNode Parent, EmpireProperty PickedProperty)
		//			{
		//				this.Parent = Parent;

		//				Solution = new List<EmpireProperty>(Parent.Solution)
		//				{
		//					PickedProperty
		//				};

		//				Properties = Parent.Properties;
		//				RemainingIndexes = new List<int>(Parent.RemainingIndexes);

		//				HasAuthority = Parent.HasAuthority;
		//				CivicPointsAvailable = Parent.CivicPointsAvailable;
		//				EthicPointsAvailable = Parent.EthicPointsAvailable;
		//				HasOrigin = Parent.HasOrigin;
		//				HasSpecies = Parent.HasSpecies;
		//				TraitPointsAvailable = Parent.TraitPointsAvailable;
		//			}

		//			public GeneratorNode Parent { get; private set; }

		//			public bool IsSolution { get => HasAuthority && HasCivics && HasEthics && HasOrigin && HasSpecies && HasTraits; }

		//			private bool HasAuthority { get; set; }

		//			private int CivicPointsAvailable { get; set; } = 2;
		//			private bool HasCivics { get => CivicPointsAvailable == 0; }

		//			private int EthicPointsAvailable { get; set; } = 3;
		//			private bool HasEthics { get => EthicPointsAvailable == 0; }
		//			private bool HasOrigin { get; set; }
		//			private bool HasSpecies { get; set; }

		//			private int TraitPointsAvailable { get; set; } = -1;
		//			private bool HasTraits { get => TraitPointsAvailable == 0; }

		//			//private int CivicsCount { get; private set; }
		//			//public ObservableCollection<EmpireViewModel> Empires { get; } = new ObservableCollection<EmpireViewModel>();

		//			public ICollection<EmpireProperty> Solution { get; }

		//			public List<EmpireProperty> Properties { get; private set; }

		//			private EmpireProperty CurrentPick { get; set; }

		//			public List<EmpireProperty> RemainingProperties { get { return RemainingIndexes.Select(i => Properties[i]).ToList(); } }

		//			//public Dictionary<int, EmpirePropertyViewModel> g;

		//			public List<int> RemainingIndexes { get; }

		//			//public IList<int> AttemptedPicks { get; }

		//			//private bool HasPotentMatches()
		//			//{
		//			//	return false;
		//			//}
		//			private bool HasPotentMatches(EmpireProperty ToMatch)
		//			{
		//				var toMatch = ToMatch.Source;
		//				var types = toMatch.Constraints.SubConstraints.Select(c => c.Group).Where(t => t != EmpirePropertyType.Unknown).ToList();
		//				int typesCount = types.Count;
		//				int remainingCount = RemainingIndexes.Count;

		//				bool typeMatchFound = false;
		//				bool eachTypeMatchFound = true;

		//				var currentTypeIndex = 0;
		//				bool isComparing = false;
		//				int i = 0;
		//				while ((currentTypeIndex < typesCount) && (i < remainingCount))
		//				{
		//					var prop = Properties[RemainingIndexes[i]];
		//					var type = types[currentTypeIndex];

		//					int cmp;

		//					if ((cmp = prop.Source.Type.CompareTo(type)) == 0)
		//					{
		//						i++;

		//						if (typeMatchFound)
		//							continue;

		//						isComparing = true;

		//						bool e = Matches(ToMatch, prop);
		//						typeMatchFound |= e;
		//					}
		//					else
		//					{
		//						if (cmp < 0)
		//							i++;
		//						else
		//							currentTypeIndex++;

		//						if (isComparing)
		//						{
		//							eachTypeMatchFound &= typeMatchFound;
		//							typeMatchFound = false;

		//							isComparing = false;

		//							continue;
		//						}
		//					}
		//				}

		//				return eachTypeMatchFound;
		//			}

		//			private bool Matches(EmpirePropertyViewModel ToMatch, EmpirePropertyViewModel Matching)
		//			{
		//				var toMatch = ToMatch.Source;
		//				var matching = Matching.Source;

		//				foreach (var cons in toMatch.Constraints.SubConstraints)
		//				{
		//					if (matching.Type != cons.Group)
		//						continue;
		//					else
		//					{
		//						bool e = cons.Evaluate(matching, (p1, p2) => p1.Identifier == p2.Identifier);
		//						if (e)
		//							return true;
		//					}
		//				}

		//				//foreach (var cons in prop2.Constraints.SubConstraints)
		//				//{
		//				//	if (prop1.Type != cons.Group)
		//				//		continue;
		//				//	else
		//				//	{
		//				//		bool e = cons.Evaluate(prop1, (p1, p2) => p1.Identifier == p2.Identifier);
		//				//		if (e)
		//				//			return true;
		//				//	}
		//				//}

		//				return false;
		//			}
		//			private bool CheckProperties(EmpirePropertyViewModel Property1, EmpirePropertyViewModel Property2)
		//			{
		//				var prop1 = Property1.Source;
		//				var prop2 = Property2.Source;

		//				foreach (var cons in prop1.Constraints.SubConstraints)
		//				{
		//					if (prop2.Type != cons.Group)
		//						continue;
		//					else
		//					{
		//						bool e = cons.Evaluate(prop2, (p1, p2) => p1.Identifier == p2.Identifier);
		//						if (!e)
		//							return false;
		//					}
		//				}

		//				foreach (var cons in prop2.Constraints.SubConstraints)
		//				{
		//					if (prop1.Type != cons.Group)
		//						continue;
		//					else
		//					{
		//						bool e = cons.Evaluate(prop1, (p1, p2) => p1.Identifier == p2.Identifier);
		//						if (!e)
		//							return false;
		//					}
		//				}

		//				return true;
		//			}
		//			private bool CheckProperty(EmpirePropertyViewModel Property)
		//			{
		//				var prop = Property.Source;

		//				foreach (var sol in Solution)
		//				{
		//					if (!CheckProperties(Property, sol))
		//						return false;
		//				}

		//				switch (prop.Type)
		//				{
		//					case EmpirePropertyType.Authority:
		//						break;
		//					case EmpirePropertyType.Civics:
		//						//if (CivicPointsAvailable == 0)
		//						//	return false;
		//						break;
		//					case EmpirePropertyType.Ethics:
		//						if (Property.Source.AsEthic.EthicCost > EthicPointsAvailable)
		//							return false;
		//						break;
		//					case EmpirePropertyType.Origin:
		//						break;
		//					case EmpirePropertyType.Species:
		//						break;
		//					case EmpirePropertyType.Trait:
		//						break;
		//				}

		//				return true;
		//			}

		//			private void PickProperty(EmpirePropertyViewModel Property)
		//			{
		//				for (int i = 0; i < RemainingIndexes.Count;)
		//				{
		//					var index = RemainingIndexes[i];
		//					var prop = Properties[index];

		//					if (!CheckProperties(Property, prop))
		//						RemoveProperty(i, prop.Weight);
		//					else
		//						i++;
		//				}

		//				switch (Property.Source.Type)
		//				{
		//					case EmpirePropertyType.Authority:
		//						PickAuthority();
		//						break;
		//					case EmpirePropertyType.Civics:
		//						PickCivic();
		//						break;
		//					case EmpirePropertyType.Ethics:
		//						PickEthic();
		//						break;
		//					case EmpirePropertyType.Origin:
		//						HasOrigin = true;
		//						RemoveOrigins();
		//						break;
		//					case EmpirePropertyType.Species:
		//						break;
		//					case EmpirePropertyType.Trait:
		//						break;
		//				}
		//			}

		//			private void PickAuthority()
		//			{
		//				HasAuthority = true;
		//				RemoveAuthorities();
		//			}
		//			private void PickCivic()
		//			{
		//				CivicPointsAvailable--;
		//				RemoveCivics();
		//			}
		//			private void PickEthic()
		//			{
		//				EthicPointsAvailable -= CurrentPick.Source.AsEthic.EthicCost;
		//				RemoveEthics();
		//			}

		//			private void RemoveType(EmpirePropertyType Type)
		//			{
		//				var first = RemainingIndexes.First(i => Properties[i].Source.Type == Type);
		//				for (int i = first; i < RemainingIndexes.Count;)
		//				{
		//					var index = RemainingIndexes[i];
		//					var prop = Properties[index].Source;

		//					if (prop.Type == Type)
		//						RemoveProperty(i, prop.Weight);
		//					else
		//						break;
		//				}
		//			}
		//			private void RemoveAuthorities()
		//			{
		//				RemoveType(EmpirePropertyType.Authority);
		//			}
		//			private void RemoveCivics()
		//			{
		//				if (CivicPointsAvailable == 1)
		//				{
		//					// Remove civics that are now impossible because of the first civic selection
		//					var first = RemainingIndexes.First(i => Properties[i].Source.Type == EmpirePropertyType.Civics);

		//					//var toRemove = new List<EmpirePropertyViewModel>();

		//					for (int i = first; i < RemainingIndexes.Count;)
		//					{
		//						var index = RemainingIndexes[i];
		//						var civicVm = Properties[index];

		//						if (civicVm.Source.Type == EmpirePropertyType.Civics)
		//						{
		//							var b = HasPotentMatches(civicVm);

		//							if (!b)
		//								RemoveProperty(i, civicVm.Weight);
		//							else
		//								i++;
		//						}
		//						else
		//							break;
		//					}
		//				}
		//				else if (CivicPointsAvailable == 0)
		//					RemoveType(EmpirePropertyType.Civics);
		//			}

		//			private void RemoveEthics()
		//			{
		//				if (EthicPointsAvailable == 0)
		//					RemoveType(EmpirePropertyType.Ethics);

		//				//// Remove civics that are now impossible because of the first civic selection
		//				//var first = RemainingIndexes.First(i => Properties[i].Source.Type == EmpirePropertyType.Ethics);

		//				////var toRemove = new List<EmpirePropertyViewModel>();

		//				//for (int i = first; i < RemainingIndexes.Count;)
		//				//{
		//				//	var index = RemainingIndexes[i];
		//				//	var ethicVm = Properties[index];
		//				//	var ethic = ethicVm.Source.AsEthic;

		//				//	if (ethic != null)
		//				//	{
		//				//		//if (ethic.EthicCost > EthicPointsAvailable)

		//				//		//var b = HasPotentMatches(ethic);

		//				//		//if (!b)
		//				//		//	RemoveProperty(i, ethic.Weight);
		//				//		//else
		//				//		//	i++;
		//				//	}
		//				//	else
		//				//		break;
		//				//}
		//			}

		//			private void RemoveOrigins()
		//			{
		//				RemoveType(EmpirePropertyType.Origin);
		//			}

		//			private IEnumerable<EmpirePropertyViewModel> ImpossibleProperties(EmpirePropertyViewModel Pick)
		//			{
		//				//var constraints = Pick.Source.Potential.SubConstraints.Concat(Pick.Source.Possible.SubConstraints).ToList();
		//				//ICollection<EmpirePropertyViewModel> toRemove = new List<EmpirePropertyViewModel>();
		//				//ICollection<EmpirePropertyViewModel> toRemove2 = new List<EmpirePropertyViewModel>();

		//				//foreach (var prop in RemainingProperties.Where(p => p != Pick))
		//				//{
		//				//	foreach (var c in constraints)
		//				//	{
		//				//		if (c.Group != prop.Source.Type)
		//				//			continue;

		//				//		bool e = c.Evaluate(prop, (p1, p2) => p1.Identifier == p2.Source.Identifier);
		//				//		if (!e)
		//				//			toRemove.Add(prop);
		//				//	}
		//				//}

		//				//var except = RemainingProperties.Except(toRemove);


		//				//foreach(var pickType in RemainingProperties)

		//				throw new NotImplementedException();
		//			}

		//			private void RemoveProperty(int Index)
		//			{
		//				int weight = Properties[Index].Weight;
		//				RemoveProperty(Index, weight);
		//			}
		//			private void RemoveProperty(int Index, int Weight)
		//			{
		//#if DEBUG
		//				Debug.WriteLine(Properties[RemainingIndexes[Index]]);
		//#endif
		//				RemainingIndexes.RemoveRange(Index, Weight);
		//			}

		//			public GeneratorNode Iterate()
		//			{
		//				// No further options, continue with parent node
		//				if (RemainingIndexes.Count == 0)
		//					return Parent;

		//				// Pick randomly an property
		//				int pickedIndex = Rnd.Next(RemainingIndexes.Count);
		//				int pickedRemainingIndex = RemainingIndexes[pickedIndex];
		//				CurrentPick = Properties[pickedRemainingIndex];

		//				//RemainingIndexes.RemoveAt(pickedIndex);
		//				pickedIndex = Properties.FindIndex(p => p.Source.Identifier == "ethic_fanatic_authoritarian");
		//				CurrentPick = Properties.First(p => p.Source.Identifier == "ethic_fanatic_authoritarian");

		//				RemoveProperty(pickedIndex, CurrentPick.Weight);

		//				// Check if this item fits in the current selection
		//				if (!CheckProperty(CurrentPick))
		//					return this;




		//				// Remove all same-items (if the picked property had a weight > 1)
		//				//int weight;
		//				//if ((weight = pickedProp.Weight) > 1)
		//				//	RemainingIndexes.RemoveRange(pickedIndex + 1, weight - 1);

		//				PickProperty(CurrentPick);

		//				return new GeneratorNode(this, CurrentPick);

		//				// Check if already picked items and newly picked one are compatible to each other

		//				//var imp = ImpossibleProperties(pickedProp);
		//				//switch

		//				// Remove the index from the list



		//				//RemainingProperties.

		//				//return null;

		//				throw new NotImplementedException();
		//			}

		//			public static GeneratorNode CreateRoot(IEnumerable<EmpirePropertyViewModel> Source)
		//			{
		//				GeneratorNode node = new GeneratorNode(Source);
		//				return node;
		//			}
		//}

		#endregion
	}
}
