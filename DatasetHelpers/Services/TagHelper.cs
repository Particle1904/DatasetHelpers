using System.Text;
using System.Text.RegularExpressions;

namespace DatasetHelpers.Services
{
    public class TagHelper
    {
        public void DoSomething()
        {
            string path = Environment.CurrentDirectory;
            if (!Directory.Exists($"{path}/Input"))
            {
                Directory.CreateDirectory($"{path}/Input");
            }

            if (!Directory.Exists($"{path}/Output"))
            {
                Directory.CreateDirectory($"{path}/Output");
            }

            Dictionary<string, uint> tags = new Dictionary<string, uint>();

            foreach (string file in Directory.GetFiles($"{path}/Input"))
            {
                string fileTags = File.ReadAllText(file);
                string[] split = Regex.Replace(fileTags, @"\r\n?|\n", "").Split(", ");

                foreach (string tag in split)
                {
                    if (!tags.ContainsKey(tag))
                    {
                        tags.Add(tag, 1);
                    }
                    else
                    {
                        tags[tag]++;
                    }
                }
            }

            var sorted = tags.OrderByDescending(x => x.Value).ToList();

            int files = Directory.GetFiles($"{path}/Output").Length;
            string outputFile = $"tagCount{files++}";

            foreach (KeyValuePair<string, uint> tag in sorted)
            {
                string line = $"{tag.Key}={tag.Value}";
                string formatted = line.Replace('_', ' ');
                File.AppendAllText($"{path}/Output/{outputFile}.txt", $"{formatted}{Environment.NewLine}", Encoding.UTF8);
            }
        }
    }
}
