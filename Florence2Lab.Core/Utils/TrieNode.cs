namespace FlorenceTwoLab.Core.Utils
{
    public class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();

        public bool IsEndOfToken { get; set; }
    }
}
