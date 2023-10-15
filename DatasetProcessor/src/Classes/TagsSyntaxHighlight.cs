using Avalonia.Media;

using AvaloniaEdit.Highlighting;

using SmartData.Lib.Helpers;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DatasetProcessor.src.Classes
{
    public class TagsSyntaxHighlight : IHighlightingDefinition
    {
        public string Name => "TagsSyntaxHighlight";

        public HighlightingRuleSet MainRuleSet { get; }

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

        public IEnumerable<HighlightingColor> NamedHighlightingColors => null;

        public IDictionary<string, string> Properties => null;

        public HighlightingColor GetNamedColor(string name) => null;
        public HighlightingRuleSet GetNamedRuleSet(string name) => null;
    }
}
