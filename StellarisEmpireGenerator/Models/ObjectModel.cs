using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using StellarisEmpireGenerator.Core;

namespace StellarisEmpireGenerator.Models
{

	public class Entity
	{
		public const string BLOCK_BEGIN = "{";
		public const string BLOCK_END = "}";

		public static readonly string[] OPERATORS = new string[] { "=", "!=", "<=", ">=", ">", "<" };

		public const string COMMENT = "#";

		private Entity(string Key, IEntityValue Value)
		{
			this.Key = Key;
			this.Value = Value;
		}
		private Entity(string Key, IEntityValue Value, StringBuilder SourceFile) : this(Key, Value)
		{
			this.SourceFile = SourceFile;
		}

		private Entity(string Key, IEnumerable<Entity> Values) : this(Key, new EntityValue<IEnumerable<Entity>>(Values))
		{
			foreach (var entity in Values)
				entity.Parent = this;
		}
		private Entity(string Key, IEnumerable<Entity> Values, StringBuilder SourceFile) : this(Key, Values)
		{
			this.SourceFile = SourceFile;
		}

		public string Key { get; set; } = null;

		public IEntityValue Value { get; set; } = null;

		public StringBuilder SourceFile { get; private set; } = null;

		public string Text
		{
			get
			{
				var text = Value as IEntityValue<string>;
				return text?.Value ?? null;
			}
		}

		public HashSet<string> Set
		{
			get
			{
				var set = Value as IEntityValue<HashSet<string>>;
				return set?.Value ?? null;
			}
		}

		#region Functions for Parsing and Deserializing

		private static bool IsEmpty(string[] Tokens, int Index)
		{
			return
				(Tokens[Index + 1] == OPERATORS[0].ToString()) &&
				(Tokens[Index + 2] == BLOCK_BEGIN.ToString()) &&
				(Tokens[Index + 3] == BLOCK_END.ToString());
		}
		private static Entity GetEmptyBlock(string[] Tokens, ref int Index, StringBuilder SourceFile)
		{
			if (IsEmpty(Tokens, Index))
			{
				var e = new Entity(Tokens[0], Enumerable.Empty<Entity>(), SourceFile);

				Index += 4;

				return e;
			}
			else
				return null;
		}

		private static bool IsSet(string[] Tokens, int Index)
		{
			return
				(Tokens[Index + 1] == OPERATORS[0].ToString()) &&
				(Tokens[Index + 2] == BLOCK_BEGIN.ToString()) &&
				(Tokens[Index + 4] != OPERATORS[0].ToString());
		}
		private static Entity GetSet(string[] Tokens, ref int Index, StringBuilder SourceFile)
		{
			if (IsSet(Tokens, Index))
			{
				HashSet<string> value = new HashSet<string>();

				Entity entity = new Entity(Tokens[Index], new EntityValue<HashSet<string>>(value), SourceFile);

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
				(OPERATORS.Contains(Tokens[1])) &&
				(Tokens[Index + 2] != BLOCK_BEGIN);
		}
		private static Entity GetAssignment(string[] Tokens, ref int Index, StringBuilder SourceFile)
		{
			if (IsAssignment(Tokens, Index))
			{
				Entity entity = new Entity(Tokens[Index], new EntityValue<string>(Tokens[Index + 2]), SourceFile);
				Index += 3;

				return entity;
			}
			else
				return null;
		}

		private static IEnumerable<Entity> Parse(string[] Tokens, ref int Index, StringBuilder SourceFile)
		{
			if (Tokens == null)
				throw new ArgumentNullException();

			ICollection<Entity> entities = new List<Entity>();

			while (Index < Tokens.Length)
			{
				Entity entity;
				if (Tokens[Index] == BLOCK_END.ToString())
				{
					Index++;
					break;
				}
				else if ((entity = GetEmptyBlock(Tokens, ref Index, SourceFile)) != null)
				{
					entities.Add(entity);
				}
				else if ((entity = GetAssignment(Tokens, ref Index, SourceFile)) != null)
				{
					entities.Add(entity);
				}
				else if ((entity = GetSet(Tokens, ref Index, SourceFile)) != null)
				{
					entities.Add(entity);
				}
				else if (Tokens[Index] != BLOCK_END.ToString())
				{
					var key = Tokens[Index];

					Index += 3;

					var blockValues = Parse(Tokens, ref Index, SourceFile);
					entities.Add(
						new Entity(
							key,
							blockValues,
							SourceFile)
						);
				}

			}

			return entities;
		}

		#endregion

		public static Entity Deserialize(string Content)
		{
			if (Content is null)
				throw new ArgumentNullException(nameof(Content));

			(StringBuilder stringBuilder, string[] tokens) = (new StringBuilder(string.Empty), Tokenize(Content.Split('\n')));
			//ICollection<IEnumerable<Entity>> deserializedTokenSets = new LinkedList<IEnumerable<Entity>>();

			int index = 0;
			var entities = Parse(tokens, ref index, stringBuilder);

			int count = entities.Count();
			if (count > 1)
				return new Entity("root", entities);
			else if (count == 1)
				return entities.First();
			else return null;
		}

		public static Entity FromFiles(IEnumerable<string> Files)
		{
			if (Files is null)
				throw new ArgumentNullException(nameof(Files));

			var stringBuilder = Files.Select(fs => new StringBuilder(fs));

			var tokensSet = stringBuilder.Select(sb => (sb, Tokenize(File.ReadAllLines(sb.ToString()))));
			ICollection<IEnumerable<Entity>> deserializedTokenSets = new LinkedList<IEnumerable<Entity>>();

			foreach ((StringBuilder sourceFile, string[] tokens) in tokensSet)
			{
				int index = 0;

				deserializedTokenSets.Add(Parse(tokens, ref index, sourceFile));
			}

			return new Entity("root", deserializedTokenSets.SelectMany(e => e));
		}

		#region Functions for Serializing Entity

		private const string Indent = "\t";
		private static string Indentation(int Level)
		{
			string indentation = string.Empty;
			for (int i = 0; i < Level; i++)
				indentation += Indent;

			return indentation;
		}
		private static bool SerializeSingleStatement(Entity Node, StringBuilder Sb, int Level)
		{
			var text = Node.Text;

			if (text == null)
				return false;

			Sb.AppendLine($"{Indentation(Level)}{Node.Key} = {text}");

			return true;
		}
		private static bool SerializeSet(Entity Node, StringBuilder Sb, int Level)
		{
			var set = Node.Set;

			if (set == null)
				return false;

			Sb.AppendLine($"{Indentation(Level)}{Node.Key} = {{");

			int itemLevel = Level + 1;
			foreach (var item in set)
				Sb.AppendLine($"{Indentation(itemLevel)}{item}");

			Sb.AppendLine($"{Indentation(Level)}}}");

			return true;
		}
		private static bool SerializeParent(Entity Node, StringBuilder Sb, int Level)
		{
			var children = Node.Children;

			if (!Node.HasChildren)
				return false;

			Sb.AppendLine($"{Indentation(Level)}{Node.Key} = {{");

			foreach (var child in children)
				Serialize2(child, Sb, Level + 1);

			Sb.AppendLine($"{Indentation(Level)}}}");

			return true;
		}
		private static void Serialize2(Entity Node, StringBuilder Sb, int Level)
		{
			if (!SerializeSingleStatement(Node, Sb, Level))
			{
				if (!SerializeSet(Node, Sb, Level))
				{
					if (!SerializeParent(Node, Sb, Level))
					{
						throw new Exception();
					}
				}
			}
		}

		#endregion

		public string Serialize()
		{
			StringBuilder sb = new StringBuilder();
			Serialize2(this, sb, 0);

			return sb.ToString();
		}

		private static string[] Tokenize(IEnumerable<string> FileLinesOfGameMechanics)
		{
			StringBuilder allText = new StringBuilder();

			foreach (var line in FileLinesOfGameMechanics)
			{
				string withoutComment = line;

				// Remove comment
				int indexHash = line.IndexOf(Entity.COMMENT);
				int lastIndexHash = line.LastIndexOf(Entity.COMMENT);
				int indexDoubleQuotes = line.IndexOf('"');
				int lastIndexDoubleQuotes = line.LastIndexOf('"');

				if (indexHash == 0)
					continue;
				else if (indexHash > 0)
				{
					if ((indexDoubleQuotes < 0) || (indexHash < indexDoubleQuotes))
						withoutComment = line.Remove(indexHash, line.Length - indexHash);
					else if (lastIndexHash > lastIndexDoubleQuotes)
						withoutComment = line.Remove(lastIndexHash, line.Length - lastIndexHash);
				}
				else withoutComment = line;

				allText.AppendLine(withoutComment);
			}

			string allTextSpacedEnsured = Regex.Replace(allText.ToString(), @"([<>!]?(=))|{|}", " $& ");
			string allTextSingleSpace = Regex.Replace(allTextSpacedEnsured, @"\s+", " ");

			// Split
			List<string> tokens = new List<string>();
			bool withinString = false;
			int nextSplitStart = 0;
			for (int i = 0; i < allTextSingleSpace.Length; i++)
			{
				char currentChar = allTextSingleSpace[i];

				switch (currentChar)
				{
					case '"':
						withinString = !withinString;
						break;
					case ' ':
						int nextSplitCount = i - nextSplitStart;
						if (nextSplitCount == 0)
							nextSplitStart = i + 1;
						else if (!withinString)
						{
							tokens.Add(allTextSingleSpace.Substring(nextSplitStart, i - nextSplitStart));
							nextSplitStart = i + 1;
						}
						break;
				}

			}

			return tokens
				.ToArray();
		}

		public override string ToString()
		{
			return Key;
		}

		#region Axes

		public IEnumerable<Entity> Ancestors
		{
			get
			{
				Entity nextParent = Parent;

				while (nextParent != null)
				{
					yield return nextParent;

					nextParent = nextParent.Parent;
				}
			}
		}
		public IEnumerable<Entity> AncestorsAndSelf
		{
			get
			{
				yield return this;

				Entity nextParent = Parent;

				while (nextParent != null)
				{
					yield return nextParent;

					nextParent = nextParent.Parent;
				}
			}
		}
		public IEnumerable<Entity> Siblings
		{
			get
			{
				if (Parent != null)
				{
					foreach (var sibling in Parent.Children)
					{
						if (sibling != this)
							yield return sibling;
					}
				}
				else
					yield break;
			}
		}
		public IEnumerable<Entity> SiblingsAndSelf
		{
			get
			{
				if (Parent != null)
				{
					foreach (var sibling in Parent.Children)
						yield return sibling;
				}
				else
					yield break;
			}
		}
		public IEnumerable<Entity> Descendants
		{
			get
			{
				Queue<Entity> queue = new Queue<Entity>();

				Entity next = this;

				do
				{
					var children = next.Children;

					if (children.Any())
					{
						foreach (var child in children)
							queue.Enqueue(child);
					}

					if (queue.Count > 0)
						next = queue.Dequeue();
					else
						break;

					yield return next;
				} while (true);
			}
		}
		public IEnumerable<Entity> DescendantsAndSelf
		{
			get
			{
				yield return this;

				Queue<Entity> queue = new Queue<Entity>();

				Entity next = this;

				do
				{
					var children = next.Children;

					if (children.Any())
					{
						foreach (var child in children)
							queue.Enqueue(child);
					}

					if (queue.Count > 0)
						next = queue.Dequeue();
					else
						break;

					yield return next;
				} while (true);
			}
		}
		public Entity Parent { get; private set; } = null;
		public IEnumerable<Entity> Children
		{
			get
			{
				if (Value is IEntityValue<IEnumerable<Entity>> children)
				{
					foreach (var child in children.Value)
						yield return child;
				}
				else
					yield break;
			}
		}

		public bool HasChildren { get => (Value as IEntityValue<IEnumerable<Entity>>) != null; }

		#endregion
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
}
