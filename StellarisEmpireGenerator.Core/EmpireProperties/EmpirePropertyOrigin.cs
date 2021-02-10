using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyOrigin : EmpireProperty
	{
		public EmpirePropertyOrigin() : base() { }

		private EmpirePropertyOrigin(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Origin) { }

		//protected override bool OnAdding(GeneratorNode Node)
		//{
		//	if (Node.HasOrigin)
		//		return false;

		//	return base.OnAdding(Node);
		//}

		protected override void OnAdded(GeneratorNode Node)
		{
			Node.HasOrigin = true;

			foreach (var origin in Node.RemainingProperties.Where(p => p.IsOrigin))
				Node.RemoveSet.Add(origin);

			base.OnAdded(Node);
		}

		protected override bool OnRemoving(GeneratorNode Node)
		{
			if (Node.HasOrigin)
				return true;

			return base.OnRemoving(Node);
		}

		private static bool IsNodeOrigin(Entity Node)
		{
			return
				Node.Descendants.ContainsPair("is_origin", "yes") &&
				(!Node.Descendants.FirstOrDefaultPair("always", "no")?.Ancestors.ContainsKey("playable") ?? true);
		}

		internal static EmpirePropertyOrigin OriginFromNode(Entity Node)
		{
			if (IsNodeOrigin(Node))
				return new EmpirePropertyOrigin(Node);
			else
				return null;
		}
	}
}
