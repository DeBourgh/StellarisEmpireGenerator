using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisEmpireGenerator.Models
{

	public class Entity
	{
		private const char BLOCK_BEGIN = '{';
		private const char BLOCK_END = '}';
		private const char ASSIGNMENT = '=';

		public Entity() { }

		public Entity(string Key, IEntityValue Value)
		{
			this.Key = Key;
			this.Value = Value;
		}

		public string Key { get; set; }
		public IEntityValue Value { get; set; }

		public bool HasChildren { get => Children != null; }

		public IEnumerable<Entity> Children
		{
			get
			{
				var block = Value as IEntityValue<IEnumerable<Entity>>;
				return block?.Value ?? null;
			}
		}

		private static bool IsSet(string[] Tokens, int Index)
		{
			return
				(Tokens[Index + 1] == ASSIGNMENT.ToString()) &&
				(Tokens[Index + 2] == BLOCK_BEGIN.ToString()) &&
				(Tokens[Index + 4] != ASSIGNMENT.ToString());
		}
		private static Entity GetSet(string[] Tokens, ref int Index)
		{
			if (IsSet(Tokens, Index))
			{
				HashSet<string> value = new HashSet<string>();

				Entity entity = new Entity(Tokens[Index], new EntityValue<HashSet<string>>(value));

				for (Index += 3; Tokens[Index] != BLOCK_END.ToString(); Index++)
					value.Add(Tokens[Index]);

				Index++;

				return entity;
			}
			else
				return null;
		}

		private static bool IsAssignment(string[] Tokens, int Index)
		{
			return
				(Tokens[Index + 1] == ASSIGNMENT.ToString()) &&
				(Tokens[Index + 2] != BLOCK_BEGIN.ToString());
		}
		private static Entity GetAssignment(string[] Tokens, ref int Index)
		{
			if (IsAssignment(Tokens, Index))
			{
				Entity entity = new Entity(Tokens[Index], new EntityValue<string>(Tokens[Index + 2]));
				Index += 3;

				return entity;
			}
			else
				return null;
		}

		private static List<Entity> Parse2(string[] Tokens, ref int Index)
		{
			if (Tokens == null)
				throw new ArgumentNullException();

			List<Entity> entities = new List<Entity>();

			while (Index < Tokens.Length)
			{
				Entity entity;
				if (Tokens[Index] == BLOCK_END.ToString())
				{
					Index++;
					break;
				}
				else if ((entity = GetAssignment(Tokens, ref Index)) != null)
				{
					entities.Add(entity);
				}
				else if ((entity = GetSet(Tokens, ref Index)) != null)
				{
					entities.Add(entity);
				}
				else if (Tokens[Index] != BLOCK_END.ToString())
				{
					Entity block = new Entity
					{
						Key = Tokens[Index]
					};

					Index += 3;

					var blockValues = Parse2(Tokens, ref Index);

					block.Value = new EntityValue<IEnumerable<Entity>>(blockValues);
					entities.Add(block);
				}

			}

			return entities;
		}
		public static Entity Parse(string[] Tokens)
		{
			int index = 0;
			var entities = Parse2(Tokens, ref index);

			return new Entity("root", new EntityValue<IEnumerable<Entity>>(entities));
		}

		public IEnumerable<Entity> Flatten()
		{
			Queue<Entity> queue = new Queue<Entity>();

			Entity next = this;

			do
			{
				yield return next;

				if (next.Value is EntityValue<IEnumerable<Entity>> block)
				{
					foreach (var entity in block.Value)
						queue.Enqueue(entity);
				}

				next = queue.Dequeue();
			} while (queue.Count > 0);
		}

		//public Entity EntityByKey(string Key)
		//{

		//}

		//public static Entity FindKey(Entity Root, string Key)
		//{
		//	if (Root.Key.Contains(Key))
		//		return Root;
		//	else if (Root.Value is EntityValue<IEnumerable<Entity>>)
		//	{
		//		;
		//	}
		//	return null;
		//}
		//public Entity FindKey(string Key)
		//{

		//}

		public override string ToString()
		{
			return Key;
		}
	}

	public interface IEntityValue
	{
		object Value { get; set; }
	}


	public interface IEntityValue<T> : IEntityValue
	{
		new T Value { get; set; }
	}

	public class EntityValue<T> : IEntityValue<T>
	{
		public EntityValue() { }
		public EntityValue(T Value) : this()
		{
			this.Value = Value;
		}

		public T Value { get; set; }

		object IEntityValue.Value { get => Value; set { Value = (T)value; } }

		public override string ToString()
		{
			return Value?.ToString() ?? base.ToString();
		}
	}

	//public class EntityBlock : IEntityValue<Entity>
	//{
	//	public Entity Value { get; set; }
	//	object IEntityValue.Value { get => Value; }
	//}

	//public class EntitySet : IEntityValue<HashSet<string>>
	//{
	//	public HashSet<string> Value { get; set; }
	//	object IEntityValue.Value { get => Value; }
	//}

	//public class EntitySingle : IEntityValue<string>
	//{
	//	public string Value { get; set; }
	//	object IEntityValue.Value { get => Value; }
	//}
}
