using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System.Text;
using System.Text.RegularExpressions;

namespace SmartData.Lib.Services
{
    public class TagProcessor : ITagProcessorService
    {
        private string _txtSearchPattern = "*.txt";

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
        /// Processes the replacement of tags in all text files within the specified input folder path with the specified tags to replace and tags to be replaced.
        /// </summary>
        /// <param name="inputFolderPath">The input folder path containing the text files to be processed.</param>
        /// <param name="tagsToReplace">The comma-separated list of tags to replace.</param>
        /// <param name="tagsToBeReplaced">The comma-separated list of tags to be replaced.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task ProcessTagsReplacement(string inputFolderPath, string tagsToReplace, string tagsToBeReplaced)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            foreach (var file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = ReplaceListOfTags(readTags, tagsToReplace, tagsToBeReplaced);
                await File.WriteAllTextAsync(file, processedTags);
            }
        }

        /// <summary>
        /// Processes the replacement of tags in all text files within the specified input folder path with the specified tags to replace and tags to be replaced.
        /// </summary>
        /// <param name="inputFolderPath">The input folder path containing the text files to be processed.</param>
        /// <param name="tagsToReplace">The comma-separated list of tags to replace.</param>
        /// <param name="tagsToBeReplaced">The comma-separated list of tags to be replaced.</param>
        /// <param name="progress">The Progress object used to track the progress of the operation.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task ProcessTagsReplacement(string inputFolderPath, string tagsToReplace, string tagsToBeReplaced, Progress progress)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (var file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = ReplaceListOfTags(readTags, tagsToReplace, tagsToBeReplaced);
                await File.WriteAllTextAsync(file, processedTags);
                progress.UpdateProgress();
            }
        }

        /// <summary>
        /// Randomizes the tags of all the text files in the specified input folder path.
        /// </summary>
        /// <param name="inputFolderPath">The path of the input folder.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RandomizeTagsOfFiles(string inputFolderPath)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            foreach (string file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = RandomizeTags(readTags);
                await File.WriteAllTextAsync(file, processedTags);
            }
        }

        /// <summary>
        /// Randomizes the tags of all the text files in the specified input folder path.
        /// </summary>
        /// <param name="inputFolderPath">The path of the input folder.</param>
        /// <param name="progress">The progress tracker for updating the progress of the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RandomizeTagsOfFiles(string inputFolderPath, Progress progress)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (string file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = RandomizeTags(readTags);
                await File.WriteAllTextAsync(file, processedTags);
                progress.UpdateProgress();
            }
        }

        /// <summary>
        /// Apply redundancy removal for tags for all .txt files in the specified input folder path.
        /// </summary>
        /// <param name="inputFolderPath">The path of the input folder.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ApplyRedundancyRemovalToFiles(string inputFolderPath)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            foreach (string file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = ApplyRedundancyRemoval(readTags);
                await File.WriteAllTextAsync(file, processedTags);
            }
        }

        /// <summary>
        /// Apply redundancy removal for tags for all .txt files in the specified input folder path.
        /// </summary>
        /// <param name="inputFolderPath">The path of the input folder.</param>
        /// /// <param name="progress">The progress tracker for updating the progress of the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ApplyRedundancyRemovalToFiles(string inputFolderPath, Progress progress)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (string file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = ApplyRedundancyRemoval(readTags);
                await File.WriteAllTextAsync(file, processedTags);
                progress.UpdateProgress();
            }
        }

        /// <summary>
        /// Calculates the count of each tag used in all the text files inside the specified folder.
        /// </summary>
        /// <param name="inputFolderPath">The path of the input folder containing the text files.</param>
        /// <returns>Returns a formatted string with the processed tags by each line.</returns>
        public string CalculateListOfMostFrequentTags(string inputFolderPath)
        {
            Dictionary<string, uint> tags = new Dictionary<string, uint>();

            foreach (string file in Directory.GetFiles(inputFolderPath, "*.txt").Where(x => !x.Contains("sample_prompt_custom.txt")))
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
                stringBuilder.Append($"{formatted}{Environment.NewLine}");
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Removes redundant tags from the input string. For example: if "shirt, white shirt" is present in the input, 
        /// the output will only have "white shirt" since thats more descriptive. It will also remove common
        /// useless tags like "general" and "sensitive" and others.
        /// </summary>
        /// <param name="tags">The string containing tags.</param>
        /// <returns>The string with redundant tags removed.</returns>
        public string ApplyRedundancyRemoval(string tags)
        {
            List<string> cleanedTags = new List<string>();
            string[] tagsSplit = tags.Replace(", ", ",").Split(",");

            bool hasBreastSizeTag = false;
            bool hasMaleGenitaliaSizeTag = false;
            bool hasHairLengthTag = false;
            bool hasHairColorTag = false;
            bool hasEyesColorTag = false;
            bool hasSkinColorTag = false;

            foreach (string tag in tagsSplit)
            {
                bool isBreastSizeTag = IsBreastSize(tag);
                bool isMaleGenitaliaTag = IsMaleGenitaliaSize(tag);
                bool isHairLengthTag = IsHairLength(tag);
                bool isHairColor = IsHairColor(tag);
                bool isEyesColor = IsEyesColor(tag);
                bool isSkinColor = IsSkinColor(tag);
                bool isRedundant = false;

                foreach (string processedTag in cleanedTags)
                {
                    if (IsRedundantWith(tag, processedTag))
                    {
                        if (tag.Length < processedTag.Length)
                        {
                            isRedundant = true;
                            continue;
                        }
                        else
                        {
                            cleanedTags.Remove(processedTag);
                            break;
                        }
                    }
                }

                if (isBreastSizeTag && !hasBreastSizeTag)
                {
                    cleanedTags.Add(tag);
                    hasBreastSizeTag = true;
                }
                else if (isMaleGenitaliaTag && !hasMaleGenitaliaSizeTag)
                {
                    cleanedTags.Add(tag);
                    hasMaleGenitaliaSizeTag = true;
                }
                else if (isHairLengthTag && !hasHairLengthTag)
                {
                    cleanedTags.Add(tag);
                    hasHairLengthTag = true;
                }
                else if (isHairColor && !hasHairColorTag)
                {
                    cleanedTags.Add(tag);
                    hasHairColorTag = true;
                }
                else if (isEyesColor && !hasEyesColorTag)
                {
                    cleanedTags.Add(tag);
                    hasEyesColorTag = true;
                }
                else if (isSkinColor && !hasSkinColorTag)
                {
                    cleanedTags.Add(tag);
                    hasSkinColorTag = true;
                }
                else if (!isBreastSizeTag && !isMaleGenitaliaTag && !isHairLengthTag && !isHairColor && !isEyesColor && !isSkinColor && !isRedundant)
                {
                    cleanedTags.RemoveAll(x => IsRedundantWith(x, tag));
                    cleanedTags.Add(tag);
                }
            }

            string[] tagsToRemove = { "questionable", "explicit", "sensitive", "censored", "uncensored", "solo", "general" };
            foreach (string tagToRemove in tagsToRemove)
            {
                cleanedTags.RemoveAll(x => x.Equals(tagToRemove, StringComparison.OrdinalIgnoreCase));
            }

            return GetCommaSeparatedString(cleanedTags);
        }

        /// <summary>
        /// Constructs a comma-separated string from the elements in the specified list.
        /// </summary>
        /// <param name="predictedTags">The list of tags to construct a string from.</param>
        /// <returns>A string that contains the elements of the specified list separated by commas.</returns>
        public string GetCommaSeparatedString(List<string> predictedTags)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string tag in predictedTags)
            {
                if (tag != predictedTags.LastOrDefault())
                {
                    stringBuilder.Append($"{tag}, ");
                }
                else
                {
                    stringBuilder.Append(tag);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Determines if the given tag is redundant with another tag.
        /// </summary>
        /// <param name="tag">The tag to compare.</param>
        /// <param name="otherTag">The other tag to compare against.</param>
        /// <returns>True if the tags are redundant, false otherwise.</returns>
        private bool IsRedundantWith(string tag, string otherTag)
        {
            return (Regex.IsMatch(otherTag, $@"\b{Regex.Escape(tag)}\b", RegexOptions.IgnoreCase) || Regex.IsMatch(tag, $@"\b{Regex.Escape(otherTag)}\b", RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a breast size.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a breast size, false otherwise.</returns>
        private bool IsBreastSize(string tag)
        {
            string[] sizeKeywords = { "small b", "medium b", "large b", "huge b", "flat chest" };

            return sizeKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a hair length.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a hair length, false otherwise.</returns>
        private bool IsHairLength(string tag)
        {
            string[] sizeKeywords = { "short hair", "very long hair", "medium hair", "absurdly long hair", "very short hair", "long hair" };

            return sizeKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a male genitalia size.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a male genitalia size, false otherwise.</returns>
        private bool IsMaleGenitaliaSize(string tag)
        {
            string[] sizeKeywords = { "small p", "medium p", "large p", "huge p" };

            return sizeKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a hair color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a hair color, false otherwise.</returns>
        private bool IsHairColor(string tag)
        {
            if (tag.Contains("hairband"))
            {
                return false;
            }

            string[] colorKeywords = { "blonde hair", "brown hair", "black hair", "blue hair", "pink hair", "purple hair",
                "white hair", "grey hair", "red hair", "green hair", "orange hair", "aqua hair" };

            return colorKeywords.Any(x => tag.Equals(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents eyes color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents eyes color, false otherwise.</returns>
        private bool IsEyesColor(string tag)
        {
            if (tag.Contains("eyeshadow"))
            {
                return false;
            }

            string[] colorKeywords = { "brown eyes", "black eyes", "blue eyes", "pink eyes", "purple eyes", "white eyes",
                "grey eyes", "red eyes", "green eyes", "orange eyes", "aqua eyes", "yellow eyes" };

            return colorKeywords.Any(x => tag.Equals(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents skin color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents skin color, false otherwise.</returns>
        private bool IsSkinColor(string tag)
        {
            string[] colorKeywords = { "black skin", "blue skin", "pink skin", "purple skin", "white skin", "grey skin",
                "red skin", "green skin", "orange skin", "yellow skin" };

            return colorKeywords.Any(x => tag.Equals(x, StringComparison.OrdinalIgnoreCase));
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

        /// <summary>
        /// Replaces a list of tags in a string with another list of tags.
        /// </summary>
        /// <param name="tags">The original string containing the tags.</param>
        /// <param name="tagsToReplace">The comma-separated list of tags to be replaced.</param>
        /// <param name="tagsToBeReplaced">The comma-separated list of replacement tags.</param>
        /// <returns>The modified string with the tags replaced.</returns>
        /// <exception cref="ArgumentException">Thrown when the number of tags to replace is not equal to the number of replacement tags.</exception>
        private string ReplaceListOfTags(string tags, string tagsToReplace, string tagsToBeReplaced)
        {
            List<string> tagsResult = new List<string>();

            string[] tagsToReplaceSplit = tagsToReplace.Replace(", ", ",").Split(",");
            string[] tagsToBeReplacedSplit = tagsToBeReplaced.Replace(", ", ",").Split(",");

            if (tagsToReplaceSplit.Length != tagsToBeReplacedSplit.Length)
            {
                throw new ArgumentException("Amount of tags must be the same!");
            }

            string[] tagsSplit = tags.Replace(", ", ",").Split(",");

            foreach (string tag in tagsSplit)
            {
                if (tagsToReplaceSplit.Contains(tag))
                {
                    int index = Array.IndexOf(tagsToReplaceSplit, tag);
                    tagsResult.Add(tagsToBeReplacedSplit[index]);
                }
                else
                {
                    tagsResult.Add(tag);
                }
            }

            return string.Join(", ", tagsResult.Distinct());
        }

        /// <summary>
        /// Randomizes the order of tags separated by commas in a string.
        /// </summary>
        /// <param name="tags">A string containing tags separated by commas.</param>
        /// <returns>A string with the same tags as the input, but in a random order.</returns>
        private string RandomizeTags(string tags)
        {
            List<string> tagsSplit = tags.Replace(", ", ",").Split(",").ToList();

            Random random = new Random();

            tagsSplit.Sort((a, b) => random.Next(-1, 2));

            StringBuilder stringBuilder = new StringBuilder();
            foreach (string tag in tagsSplit)
            {
                if (tag != tagsSplit.LastOrDefault())
                {
                    stringBuilder.Append($"{tag}, ");
                }
                else
                {
                    stringBuilder.Append(tag);
                }
            }
            return stringBuilder.ToString();
        }
    }
}
