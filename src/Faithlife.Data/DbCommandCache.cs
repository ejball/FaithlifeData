using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Faithlife.Data;

/// <summary>
/// A cache of <see cref="IDbCommand" /> by command text.
/// </summary>
public abstract class DbCommandCache
{
	/// <summary>
	/// Creates a new command cache.
	/// </summary>
	public static DbCommandCache Create() => new DictionaryCache();

	/// <summary>
	/// Gets the specified cached command, if any.
	/// </summary>
	public abstract bool TryGetCommand(string text, [MaybeNullWhen(false)] out IDbCommand command);

	/// <summary>
	/// Adds the specified command to the cache.
	/// </summary>
	public abstract void AddCommand(string text, IDbCommand command);

	/// <summary>
	/// Gets the cached commands.
	/// </summary>
	public abstract IReadOnlyCollection<IDbCommand> GetCommands();

	private sealed class DictionaryCache : DbCommandCache
	{
		public override bool TryGetCommand(string text, [MaybeNullWhen(false)] out IDbCommand command) => m_dictionary.TryGetValue(text, out command);

		public override void AddCommand(string text, IDbCommand command) => m_dictionary.Add(text, command);

		public override IReadOnlyCollection<IDbCommand> GetCommands() => m_dictionary.Values;

		private readonly Dictionary<string, IDbCommand> m_dictionary = new();
	}
}
