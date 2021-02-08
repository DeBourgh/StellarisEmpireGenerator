using Newtonsoft.Json;

using StellarisEmpireGenerator.Core.ObjectModel;

using System.Linq;

namespace StellarisEmpireGenerator.Core.EmpireProperties
{
	[JsonObject(IsReference = true)]
	public sealed class EmpirePropertyAuthority : EmpireProperty
	{
		public EmpirePropertyAuthority() : base() { }

		private EmpirePropertyAuthority(Entity SourceEntity) : base(SourceEntity, EmpirePropertyType.Authority) { }

		protected override void OnAdded(GeneratorNode Node)
		{
			Node.HasAuthority = true;

			foreach (var auth in Node.RemainingProperties.Where(p => p.IsAuthority))
				Node.RemoveSet.Add(auth);

			base.OnAdded(Node);
		}

		protected override bool IsValidWith(EmpireProperty Prop, GeneratorNode Node)
		{
			if (Prop.Type == EmpirePropertyType.Authority)
				return false;

			return base.IsValidWith(Prop, Node);
		}

		protected override bool OnRemoving(GeneratorNode Node)
		{
			if (Node.HasAuthority)
				return true;

			return base.OnRemoving(Node);
		}

		private static bool IsNodeAuthority(Entity Node)
		{
			Entity aiEmpire = Node.Descendants.FirstOrDefaultPair("value", "ai_empire");

			return
				Node.Key.StartsWith("auth_") &&
				(!aiEmpire?.Parent.Key.Equals("country_type") ?? true) &&
				(!aiEmpire?.Ancestors.ContainsKey("potential") ?? true);
		}

		internal static EmpirePropertyAuthority AuthorityFromNode(Entity Node)
		{
			if (IsNodeAuthority(Node))
				return new EmpirePropertyAuthority(Node);
			else
				return null;
		}
	}
}
