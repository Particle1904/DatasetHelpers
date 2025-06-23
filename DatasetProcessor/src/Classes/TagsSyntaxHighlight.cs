using Avalonia.Media;

using AvaloniaEdit.Highlighting;

using SmartData.Lib.Helpers;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DatasetProcessor.src.Classes
{
    /// <summary>
    /// Defines a custom syntax highlighting for highlighting specific tags in text using regular expressions.
    /// </summary>
    public class TagsSyntaxHighlight : IHighlightingDefinition
    {
        /// <summary>
        /// Gets the name of the custom syntax highlighting definition.
        /// </summary>
        public string Name => "TagsSyntaxHighlight";

        /// <summary>
        /// Gets the main rule set for syntax highlighting.
        /// </summary>
        public HighlightingRuleSet MainRuleSet { get; }

        /// <summary>
        /// Initializes a new instance of the TagsSyntaxHighlight class with specific tags and foreground color.
        /// </summary>
        /// <param name="foregroundColor">The color used to highlight the specified tags.</param>
        /// <param name="tags">An array of tags to be highlighted.</param>
        public TagsSyntaxHighlight(Color foregroundColor, string[] tags)
        {
            MainRuleSet = new HighlightingRuleSet();

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Length == 0 || string.IsNullOrEmpty(tags[i]))
                {
                    continue;
                }

                HighlightingRule customWordRule = new HighlightingRule()
                {
                    Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(foregroundColor) },
                    Regex = new Regex(@$"\b({Regex.Escape(tags[i])})\b", RegexOptions.IgnoreCase, Utilities.RegexTimeout)
                };

                MainRuleSet.Rules.Add(customWordRule);
            }
        }

        /// <summary>
        /// Gets a collection of named highlighting colors, which is not used in this custom syntax highlighting.
        /// </summary>
        public IEnumerable<HighlightingColor> NamedHighlightingColors => new List<HighlightingColor>();

        /// <summary>
        /// Gets a collection of properties, which is not used in this custom syntax highlighting.
        /// </summary>
        public IDictionary<string, string> Properties => new Dictionary<string, string>();

        /// <summary>
        /// Gets a named highlighting color by name, which is not used in this custom syntax highlighting.
        /// </summary>
        /// <param name="name">The name of the highlighting color.</param>
        /// <returns>The named highlighting color, or null if not found.</returns>
        public HighlightingColor GetNamedColor(string name) => null;

        /// <summary>
        /// Gets a named rule set by name, which is not used in this custom syntax highlighting.
        /// </summary>
        /// <param name="name">The name of the rule set.</param>
        /// <returns>The named rule set, or null if not found.</returns>
        public HighlightingRuleSet GetNamedRuleSet(string name) => null;
    }
}
