using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyEthic : EmpireProperty
	{
		public EmpirePropertyEthic() : base() { }
		private EmpirePropertyEthic(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Ethics)
		{
			EthicCost = ExtractEthicCost(SourceEntity);
			EthicCategory = ExtractEthicCategory(SourceEntity);
		}

		public int EthicCost { get; set; } = 1;

		public string EthicCategory { get; set; } = string.Empty;

		[JsonProperty(ReferenceLoopHandling = ReferenceLoopHandling.Ignore, IsReference = true)]
		public EmpirePropertyEthic NonFanaticVariant { get; set; } = null;

		[JsonProperty(ReferenceLoopHandling = ReferenceLoopHandling.Ignore, IsReference = true)]
		public EmpirePropertyEthic FanaticVariant { get; set; } = null;

		//[DataMember]
		//private string NonFanaticVariantId { get; set; }
		//[DataMember]
		//private string FanaticVariantId { get; set; }
		[JsonIgnore]
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
				//FanaticVariant.FanaticVariantId = fntProperty.Identifier;

				NonFanaticVariant = this;
				//NonFanaticVariantId = this.Identifier;

				fntProperty.FanaticVariant = fntProperty;
				//fntProperty.FanaticVariantId = fntProperty.Identifier;

				fntProperty.NonFanaticVariant = this;
				//fntProperty.NonFanaticVariantId = this.Identifier;
			}

			var ethics = Properties.Where(p => (p.Type == EmpirePropertyType.Ethics) && (p != this)).ToList();
			var incompatibleEthics = ethics
				.Where(p => p.AsEthic.EthicCost + EthicCost > MaximumEthicPoints)
				.Concat(
					ethics.Where(p => p.AsEthic.EthicCategory == EthicCategory));

			foreach (var e in incompatibleEthics)
				AddConstraint(Condition.Nor, e);
		}
	}
}
