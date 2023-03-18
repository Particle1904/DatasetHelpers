using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System.Text;
using System.Text.RegularExpressions;

namespace SmartData.Lib.Services
{
    public class TagProcessor : ITagProcessorService
    {
        private string _txtSearchPattern = "*.txt";

        public TagProcessor()
        {
        }

        /// <summary>
        /// Processes all tag files in the specified folder by adding, emphasizing, or removing tags.
        /// </summary>
        /// <param name="inputFolderPath">The folder path where the tag files are located.</param>
        /// <param name="tagsToAdd">The tags to add to the tag files.</param>
        /// <param name="tagsToEmphasize">The tags to emphasize in the tag files.</param>
        /// <param name="tagsToRemove">The tags to remove from the tag files.</param>
        /// <returns>A task that represents the asynchronous processing of all tag files in the folder.</returns>
        public async Task ProcessAllTagFiles(string inputFolderPath, string tagsToAdd, string tagsToEmphasize, string tagsToRemove)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            foreach (string file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);

                string processedTags = ProcessListOfTags(readTags, tagsToAdd, tagsToEmphasize, tagsToRemove);

                await File.WriteAllTextAsync(file, processedTags);
            }
        }

        /// <summary>
        /// Processes all tag files in the specified folder by adding, emphasizing, or removing tags asynchronously.
        /// </summary>
        /// <param name="inputFolderPath">The folder path where the tag files are located.</param>
        /// <param name="tagsToAdd">The tags to add to the tag files.</param>
        /// <param name="tagsToEmphasize">The tags to emphasize in the tag files.</param>
        /// <param name="tagsToRemove">The tags to remove from the tag files.</param>
        /// <param name="progress">The progress object to update progress of processing.</param>
        /// <returns>A task that represents the asynchronous processing of all tag files in the folder.</returns>
        public async Task ProcessAllTagFiles(string inputFolderPath, string tagsToAdd, string tagsToEmphasize, string tagsToRemove, Progress progress)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (string file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = ProcessListOfTags(readTags, tagsToAdd, tagsToEmphasize, tagsToRemove);
                await File.WriteAllTextAsync(file, processedTags);
                progress.UpdateProgress();
            }
        }

        /// <summary>
        /// Calculates the count of each tag used in all the text files inside the specified folder 
        /// and writes the results to a file named "tag-count.txt" in the same folder.
        /// </summary>
        /// <param name="inputFolderPath">The path of the input folder containing the text files.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task CalculateListOfMostUsedTags(string inputFolderPath)
        {
            Dictionary<string, uint> tags = new Dictionary<string, uint>();

            foreach (string file in Directory.GetFiles(inputFolderPath, "*.txt"))
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

            int files = Directory.GetFiles(inputFolderPath).Length;

            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, uint> tag in sorted)
            {
                string line = $"{tag.Key}={tag.Value}";
                string formatted = line.Replace('_', ' ');

                stringBuilder.AppendLine($"formatted{Environment.NewLine}");
            }


            string filePath = Path.Combine(inputFolderPath, "tag-count.txt");

            await File.WriteAllTextAsync(filePath, stringBuilder.ToString());
        }

        /// <summary>
        /// This method takes a list of tags and processes them based on user input and predictions from a machine learning (ML) model. It first adds the user-input tags to the result list, then adds the tags that should be emphasized (brought to the front of the list), and finally adds the tags predicted by the ML model. The method also removes any unwanted tags. The processed tags are returned as a comma-separated string.
        /// </summary>
        /// <param name="predictedTags">An IEnumerable of predicted tags from the ML model.</param>
        /// <param name="tagsToAdd">A string of user input tags to add to the result list. Multiple tags should be separated by commas.</param>
        /// <param name="tagsToEmphasize">A string of tags to bring to the front of the result list. Multiple tags should be separated by commas.</param>
        /// <param name="tagsToRemove">A string of tags to remove from the result list. Multiple tags should be separated by commas.</param>
        /// <returns>A comma-separated string of processed tags.</returns>
        private string ProcessListOfTags(string predictedTags, string tagsToAdd, string tagsToEmphasize, string tagsToRemove)
        {
            List<string> tagsResult = new List<string>();

            // Add user input tags.
            if (!string.IsNullOrEmpty(tagsToAdd))
            {
                string[] tagsToAddSplit = tagsToAdd.Replace(", ", ",").Split(",");

                foreach (string tag in tagsToAddSplit)
                {
                    tagsResult.Add(tag);
                }
            }

            string[] predictedTagsSplit = predictedTags.Replace(", ", ",").Split(",");

            // Add tags that should be Emphasized(bring to the front of the list).
            if (!string.IsNullOrEmpty(tagsToEmphasize))
            {
                string[] tagsToEmphasizeSplit = tagsToEmphasize.Replace(", ", ",").Split(",");

                foreach (string tag in tagsToEmphasizeSplit)
                {
                    bool match = predictedTagsSplit.Any(x => tag.Equals(x, StringComparison.OrdinalIgnoreCase));
                    if (match)
                    {
                        tagsResult.Add(tag);
                    }
                }
            }

            // Add tags predicted by the ML model.
            foreach (string tag in predictedTagsSplit)
            {
                bool match = tagsResult.Any(x => tag.Equals(x));
                if (!match)
                {
                    tagsResult.Add(tag);
                }
            }

            // Remove unwanted tags.
            if (!string.IsNullOrEmpty(tagsToRemove))
            {
                string[] tagsToRemoveSplit = tagsToRemove.Replace(", ", ",").Split(",");

                foreach (string tag in tagsToRemoveSplit)
                {
                    tagsResult.RemoveAll(x => tag.Equals(x));
                }
            }

            return string.Join(", ", tagsResult.Distinct());
        }
    }
}
