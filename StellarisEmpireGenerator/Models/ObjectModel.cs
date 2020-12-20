using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisEmpireGenerator.Models
{
	public class Entity
	{

		public string Key { get; set; }
		public IEntityValue Value { get; set; }

		private static IEnumerable<Entity> Parse2(string[] Tokens)
		{

		}
		public static IEnumerable<Entity> Parse(string[] Tokens)
		{
			string potentialIdentifier = null;

			for (int i = 0;)

			sd
			throw new NotImplementedException();
		}

	}



	public interface IEntityValue
	{
		object Value { get; }
	}


	public interface IEntityValue<T> : IEntityValue
	{
		new T Value { get; }
	}


	public class EntityBlock : IEntityValue<Entity>
	{
		public Entity Value { get; set; }
		object IEntityValue.Value { get => Value; }
	}

	public class EntityCollection : IEntityValue<HashSet<string>>
	{
		public HashSet<string> Value { get; set; }
		object IEntityValue.Value { get => Value; }
	}

	public class EntitySingle : IEntityValue<string>
	{
		public string Value { get; set; }
		object IEntityValue.Value { get => Value; }
	}
}
