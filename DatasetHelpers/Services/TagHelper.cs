using System.Text;
using System.Text.RegularExpressions;

namespace DatasetHelpers.Services
{
    public class TagHelper
    {
        private readonly string _inputPath;
        private readonly string _outputPath;

        private readonly HashSet<string> _tags;
        private readonly HashSet<string> _negativeTags;

        public TagHelper(string inputPath, string outputPath)
        {
            _inputPath = inputPath;
            _outputPath = outputPath;

            string[] tags = File.ReadAllLines($"{Environment.CurrentDirectory}/tags.txt");
            _tags = new HashSet<string>(tags[0].Split(","));
            _negativeTags = new HashSet<string>(tags[1].Split(","));
        }

        public string ProcessListOfTags(IEnumerable<string> tags)
        {
            List<string> tagsResult = new List<string>();

            foreach (string tag in _tags)
            {
                tagsResult.Add(tag);
            }

            foreach (string predictedTag in tags)
            {
                bool match = tagsResult.Any(x => predictedTag.Equals(x));
                if (!match)
                {
                    tagsResult.Add(predictedTag);
                }
            }

            foreach (string negativeTag in _negativeTags)
            {
                tagsResult.RemoveAll(x => negativeTag.Equals(x));
            }

            return string.Join(", ", tagsResult);
        }

        public void CalculateListOfMostUsedTags()
        {
            Dictionary<string, uint> tags = new Dictionary<string, uint>();

            foreach (string file in Directory.GetFiles(_outputPath, "*.txt"))
            {
                string fileTags = File.ReadAllText(file);
                string[] split = Regex.Replace(fileTags, @"\r\n?|\n", "").Split(", ");

                foreach (string splittedTag in split)
                {
                    string match = tags.Keys.FirstOrDefault(x => x.Equals(splittedTag));
                    if (match == null)
                    {
                        tags.Add(splittedTag, 1);
                    }
                    else
                    {
                        tags[match]++;
                    }
                }
            }

            var sorted = tags.OrderByDescending(x => x.Value).ToList();

            int files = Directory.GetFiles(_outputPath).Length;
            string outputFile = $"tagCount";

            foreach (KeyValuePair<string, uint> tag in sorted)
            {
                string line = $"{tag.Key}={tag.Value}";
                string formatted = line.Replace('_', ' ');
                File.AppendAllText($"{_outputPath}/{outputFile}.txt", $"{formatted}{Environment.NewLine}", Encoding.UTF8);
            }
        }
    }
}
