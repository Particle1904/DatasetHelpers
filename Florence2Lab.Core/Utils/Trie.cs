namespace FlorenceTwoLab.Core.Utils;

public class Trie
{
    private readonly TrieNode _root = new();
    private readonly HashSet<string> _tokens = new();

    public bool Contains(string token) => _tokens.Contains(token);

    /// <summary>
    /// Adds a token to the Trie.
    /// </summary>
    /// <param name="token">The token to add. If null or empty, the method returns without modifying the Trie.</param>
    /// <remarks>
    /// Tokens are stored in both a HashSet for quick existence checks and in the Trie structure for prefix-based operations.
    /// </remarks>
    public void Add(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        _tokens.Add(token);
        TrieNode current = _root;

        foreach (char c in token)
        {
            if (!current.Children.ContainsKey(c))
            {
                current.Children[c] = new TrieNode();
            }
            current = current.Children[c];
        }

        current.IsEndOfToken = true;
    }

    /// <summary>
    /// Splits the input text into substrings using the tokens stored in the Trie as delimiters.
    /// </summary>
    /// <param name="text">The input text to split.</param>
    /// <returns>A list of substrings resulting from splitting the input text at token match boundaries.</returns>
    /// <remarks>
    /// This method scans the input text and identifies matches with any of the stored tokens.
    /// When a match is found, the text is split at that point. Overlapping matches are resolved by prioritizing the first complete match found.
    /// If the input text is null or empty, an empty list is returned.
    /// </remarks>
    public List<string> Split(string text)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return result;
        }

        // Dictionary to keep track of active matches
        // Key is the starting position, Value is the node we're at in the trie
        Dictionary<int, TrieNode> states = new Dictionary<int, TrieNode>();

        // List of split points in the text
        List<int> offsets = new List<int> { 0 };

        for (int current = 0; current < text.Length; current++)
        {
            char currentChar = text[current];

            // Process existing states
            List<int> statesToRemove = new List<int>();
            bool completeMatch = false;
            int matchStart = -1;
            int matchEnd = -1;

            foreach (KeyValuePair<int, TrieNode> state in states)
            {
                if (state.Value.Children.TryGetValue(currentChar, out TrieNode? nextNode))
                {
                    states[state.Key] = nextNode;
                    if (nextNode.IsEndOfToken)
                    {
                        // Found a complete match
                        completeMatch = true;
                        matchStart = state.Key;
                        matchEnd = current + 1;
                        break;
                    }
                }
                else
                {
                    statesToRemove.Add(state.Key);
                }
            }

            // Remove states that couldn't be extended
            foreach (int key in statesToRemove)
            {
                states.Remove(key);
            }

            // Try to start new match from current position
            if (_root.Children.ContainsKey(currentChar))
            {
                TrieNode node = _root.Children[currentChar];
                states[current] = node;
                // Check if single-character token
                if (node.IsEndOfToken)
                {
                    completeMatch = true;
                    matchStart = current;
                    matchEnd = current + 1;
                }
            }

            // Handle complete match
            if (completeMatch)
            {
                offsets.Add(matchStart);
                offsets.Add(matchEnd);
                states.Clear();
                current = matchEnd - 1; // -1 because loop will increment
            }
        }

        // Add final offset if not already present
        if (offsets.Last() != text.Length)
        {
            offsets.Add(text.Length);
        }

        // Convert offsets to substrings
        for (int i = 0; i < offsets.Count - 1; i++)
        {
            int start = offsets[i];
            int length = offsets[i + 1] - start;
            if (length > 0)
            {
                result.Add(text.Substring(start, length));
            }
        }

        return result;
    }
}
