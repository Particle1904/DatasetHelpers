using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartData.Lib.Services
{
    public class TagProcessor : ITagProcessorService
    {
        private readonly string _txtSearchPattern = "*.txt";
        private readonly string _captionSearchPattern = "*.caption";

        private static HashSet<string> _edgeCasesContains = new HashSet<string>()
        {
            "tattoo", "piercing", "headwear", "on", "up", "in", "of", "at", "looking", "viewer", "grabbing", "pubic",
            "apart", "by", "own mouth", "grab", "object insertion", "spread", "milking machine", "clothed", "together",
            "between", "removed", "adjusting", "around", "head wings", "thong", "lift", "while", "caressing",
            "veiny", "cutout", "torn", "back", "under", "behind", "shibari", "uniform", "tentacle around",
            "pull", "with", "very", "over", "tying", "arm hair", "pulled back", "holding", "drying hair",
            "hair ribbon", "through", "ring", "no", "top", "extra", "platform heels", "out", "peek", "disembodied",
            "ale mast", "ari mast", "reverse", "one", "across", "hold", "hitachi", "slip", "holster", "reverse"
        };

        private static HashSet<string> _edgeCasesEquals = new HashSet<string>()
        {
            "facial hair", "navel hair", "armpit hair", "chest hair", "pubic hair", "ass visible through thighs",
            "feet out of frame", "head out of frame", "leg lift", "thigh high", "closed eyes", "glowing eyes",
            "half-closed eyes", "rolling eyes", "tail ornament", "open mouth", "closed mouth", "thighhighs under boots",
            "high heel", "ball bra", "huge ass", "perky breasts", "playing with own hair", "crying with eyes open",
            "hair bow", "dress shirt", "hair scrunchie", "off shoulder", "thighband pantyhose", "short shorts",
            "open jacket", "short sleeves", "wide sleeves", "low wings", "detached wings", "covered eyes",
            "doughnut hair bun", "covered nipples", "covered mouth", "torn clothes", "revealing clothes",
            "clothes in mouth", "shirt in mouth", "m legs", "arm grab", "legs up", "licking lips", "puckered lips",
            "footwear bow", "platform footwear", "hand paper fan", "stuffed toy",
            "single hair bun", "double bun", "strap slip", "bags under eyes", "hair flower", "hair between eyes",
            "furry female", "furry male", "faceless male", "faceless female", "muscular female", "muscular male",
            "rei no himo", "cone hair bun", "crop top", "big hair", "upper teeth only", "lower teeth only",
            "colored inner hair", "thigh boots"
        };

        private static HashSet<string> _animalEars = new HashSet<string>()
        {
            "alpaca ears", "bat ears", "bear ears", "beaver ears", "bird ears", "boar ears", "cat ears", "cow ears",
            "deer ears", "dog ears", "fake animal ears", "ferret ears", "fox ears", "gazelle ears", "giraffe ears",
            "goat ears", "hamster ears", "hippopotamus ears", "horse ears", "jackal ears", "jaguar ears", "leopard ears",
            "lion ears", "monkey ears", "moose ears", "mouse ears", "otter ears", "panda ears", "pig ears",
            "rabbit ears", "raccoon ears", "rhinoceros ears", "sheep ears", "squirrel ears", "tamandua ears",
            "tapir ears", "tiger ears", "weasel ears", "wolf ears", "zebra ears", "animal ears"
        };

        private static HashSet<string> _breastsSizeKeywords = new HashSet<string>()
        {
            "small breasts", "medium breasts", "large breasts", "huge breasts", "gigantic breasts", "flat chest"
        };

        private static HashSet<string> _scleraColorsKeywords = new HashSet<string>()
        {
            "aqua sclera", "black sclera", "blue sclera", "brown sclera", "green sclera", "grey sclera",
            "orange sclera", "black sclera", "pink sclera", "purple sclera", "red sclera", "yellow sclera"
        };

        private static HashSet<string> _hairLengthKeywords = new HashSet<string>()
        {
            "short hair", "very long hair", "medium hair", "absurdly long hair", "very short hair", "long hair"
        };

        private static HashSet<string> _hairColorKeywords = new HashSet<string>()
        {
            "blonde hair", "blond hair", "brown hair", "black hair", "blue hair", "pink hair", "purple hair",
            "white hair", "grey hair", "gray hair", "red hair", "green hair", "orange hair", "aqua hair"
        };

        private static HashSet<string> _eyeColorsKeywords = new HashSet<string>()
        {
            "brown eyes", "black eyes", "blue eyes", "pink eyes", "purple eyes", "white eyes",
            "grey eyes", "gray eyes", "red eyes", "green eyes", "orange eyes", "aqua eyes", "yellow eyes"
        };

        private static HashSet<string> _lipsColorKeywords = new HashSet<string>()
        {
            "aqua lips", "black lips", "blue lips", "brown lips", "gold lips", "green lips", "grey lips",
            "purple lips", "blue lips", "orange lips", "pink lips", "purple lips", "red lips", "silver lips",
            "white lips", "yellow lips"
        };

        private static HashSet<string> _pSizeKeywords = new HashSet<string>()
        {
            "small pen", "medium pen", "large pen", "huge pen" , "gigantic pen"
        };

        private static HashSet<string> _pStateKeywords = new HashSet<string>()
        {
            "flaccid", "erection"
        };

        private static HashSet<string> _skinColorsKeywords = new HashSet<string>()
        {
            "black skin", "blue skin", "pink skin", "purple skin", "white skin", "grey skin",
            "gray skin", "red skin", "green skin", "orange skin", "yellow skin"
        };

        private static HashSet<string> _clothingKeywords = new HashSet<string>()
        {
            "clothing aside", "panties aside", "bikini bottom aside", "leotard aside", "swimsuit aside",
            "buruma aside", "thong aside", "shorts aside", "fundoshi aside", "pelvic curtain aside",
            "dress aside", "skirt aside", "apron aside", "bodysuit aside"
        };

        private static HashSet<string> _clothesPullKeywords = new HashSet<string>()
        {
            "clothes pull", "shirt pull", "dress pull", "skirt pull", "shorts pull", "pants pull",
            "one-piece swimsuit pull", "bikini pull", "bra pull", "pantyhose pull", "sports bra pull"
        };

        private static HashSet<string> _clothesLiftKeywords = new HashSet<string>()
        {
            "clothes lift", "bikini top lift", "dress lift", "hakama lift", "hoodie lift", "kimono lift",
            "pelvic curtain lift", "sarong lift", "shirt lift", "skirt lift", "sports bra lift",
            "sweater lift", "swimsuit lift", "towel lift", "bikini lift"
        };

        private static HashSet<string> _tagsToRemove = new HashSet<string>()
        {
            "questionable", "explicit", "sensitive", "censored", "uncensored", "solo", "general", "meme",
            "meme attire", "mosaic censoring"
        };

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
        /// Consolidates tags in text files within the specified input folder by grouping similar tags together.
        /// The consolidated tags are then saved back to the respective files.
        /// </summary>
        /// <param name="inputFolderPath">The path of the directory containing the text files with tags to consolidate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ConsolidateTags(string inputFolderPath)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            using (SHA256 sha256 = SHA256.Create())
            {
                foreach (string file in files)
                {
                    string readTags = await File.ReadAllTextAsync(file);
                    string consolidatedTags = ProcessConsolidateTags(sha256, readTags);
                    await File.WriteAllTextAsync(file, consolidatedTags);
                }
            }
        }

        /// <summary>
        /// Consolidates tags in text files within the specified input folder by grouping similar tags together.
        /// The consolidated tags are then saved back to the respective files, and progress is updated.
        /// </summary>
        /// <param name="inputFolderPath">The path of the directory containing the text files with tags to consolidate.</param>
        /// <param name="progress">The progress object to update with the status of the consolidation operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ConsolidateTags(string inputFolderPath, Progress progress)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            progress.TotalFiles = files.Length;

            using (SHA256 sha256 = SHA256.Create())
            {
                foreach (string file in files)
                {
                    string readTags = await File.ReadAllTextAsync(file);
                    string consolidatedTags = ProcessConsolidateTags(sha256, readTags);
                    await File.WriteAllTextAsync(file, consolidatedTags);
                    progress.UpdateProgress();
                }
            }
        }

        /// <summary>
        /// Consolidates similar tags in text files within the specified input folder and logs edge cases.
        /// The consolidated tags are then saved back to the respective files, and progress is updated.
        /// Additionally, the total time taken for the operation is logged.
        /// </summary>
        /// <param name="inputFolderPath">The path of the directory containing the text files with tags to consolidate.</param>
        /// <param name="loggerService">The logger service responsible for handling log storage.</param>
        /// <param name="progress">The progress object to update with the status of the consolidation operation.</param>
        /// <remarks>
        /// This method reads text files from the specified folder, consolidates similar tags in each file,
        /// and saves the consolidated tags back to the respective files. Progress is tracked using the provided
        /// progress object. Edge cases encountered during consolidation are logged within this method, and
        /// the total time taken for the operation is also logged.
        /// </remarks>
        public async Task ConsolidateTagsAndLogEdgeCases(string inputFolderPath, ILoggerService loggerService, Progress progress)
        {
            StringBuilder logStringBuilder = new StringBuilder();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            progress.TotalFiles = files.Length;

            using (SHA256 sha256 = SHA256.Create())
            {
                foreach (string file in files)
                {
                    string readTags = await File.ReadAllTextAsync(file);
                    string consolidatedTags = ProcessConsolidateTags(sha256, readTags);

                    string[] splitTags = consolidatedTags.Split(", ");
                    foreach (string item in splitTags)
                    {
                        if (item.Split(' ').Length >= 3)
                        {
                            logStringBuilder.AppendLine($"FILE: {file} | CONSOLIDATED TAG: {item}");
                        }
                    }

                    await File.WriteAllTextAsync(file, consolidatedTags);
                    progress.UpdateProgress();
                }
            }

            stopwatch.Stop();
            logStringBuilder.AppendLine($"{Environment.NewLine}TIME TAKEN: {stopwatch.Elapsed.Hours}:{stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds}");
            await loggerService.SaveStringBuilderToLogFile(logStringBuilder);
        }

        /// <summary>
        /// Performs search and replace operations on captions within files asynchronously.
        /// </summary>
        /// <param name="inputFolderPath">The folder path where the caption files are located.</param>
        /// <param name="wordsToBeReplaced">The words to be replaced in the captions.</param>
        /// <param name="wordsToReplace">The replacement words for the search operation.</param>
        public async Task FindAndReplace(string inputFolderPath, string wordsToBeReplaced, string wordsToReplace)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _captionSearchPattern);

            foreach (string file in files)
            {
                string readCaption = await File.ReadAllTextAsync(file);
                string processedCaption = ProcessSearchAndReplace(readCaption, wordsToBeReplaced, wordsToReplace);
                await File.WriteAllTextAsync(file, processedCaption);
            }
        }

        /// <summary>
        /// Performs search and replace operations on captions within files asynchronously, with progress tracking.
        /// </summary>
        /// <param name="inputFolderPath">The folder path where the caption files are located.</param>
        /// <param name="wordsToBeReplaced">The words to be replaced in the captions.</param>
        /// <param name="wordsToReplace">The replacement words for the search operation.</param>
        /// <param name="progress">The progress object to update progress of processing.</param>
        public async Task FindAndReplace(string inputFolderPath, string wordsToBeReplaced, string wordsToReplace, Progress progress)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _captionSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (string file in files)
            {
                string readCaption = await File.ReadAllTextAsync(file);
                string processedCaption = ProcessSearchAndReplace(readCaption, wordsToBeReplaced, wordsToReplace);
                await File.WriteAllTextAsync(file, processedCaption);
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
        public async Task ProcessTagsReplacement(string inputFolderPath, string tagsToBeReplaced, string tagsToReplace)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            foreach (var file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = ReplaceListOfTags(readTags, tagsToBeReplaced, tagsToReplace);
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
        public async Task ProcessTagsReplacement(string inputFolderPath, string tagsToBeReplaced, string tagsToReplace, Progress progress)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _txtSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (var file in files)
            {
                string readTags = await File.ReadAllTextAsync(file);
                string processedTags = ReplaceListOfTags(readTags, tagsToBeReplaced, tagsToReplace);
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
            Dictionary<string, uint> tagsWithFrequency = GetTagsWithFrequency(inputFolderPath);
            List<KeyValuePair<string, uint>> sorted = tagsWithFrequency.OrderByDescending(x => x.Value).ToList();

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
        /// Retrieves an array of unique tags extracted from text files in the specified folder.
        /// </summary>
        /// <param name="inputFolderPath">The path to the folder containing text files.</param>
        /// <returns>An array of strings representing unique tags found in the text files.</returns>
        public string[] GetTagsFromDataset(string inputFolderPath)
        {
            Dictionary<string, uint> tagsWithFrequency = GetTagsWithFrequency(inputFolderPath);

            return tagsWithFrequency.Keys.ToArray();
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
            HashSet<string> cleanedTags = new HashSet<string>(50);
            string[] tagsSplit = Utilities.ParseAndCleanTags(tags);

            bool hasBreastSize = false;
            bool hasMaleGenitaliaSize = false;
            bool hasMaleGenitaliaState = false;
            bool hasHairLength = false;
            bool hasHairColor = false;
            bool hasEyesColor = false;
            bool hasSkinColor = false;
            bool hasClothingAside = false;
            bool hasLipsColor = false;
            bool hasClothingLift = false;
            bool hasClothingPull = false;
            bool hasEyewear = false;
            bool hasScleraColor = false;
            bool hasAnimalEars = false;

            foreach (string tag in tagsSplit)
            {
                bool isRedundant = false;

                bool isBreastSize = IsBreastSize(tag);
                bool isMaleGenitalia = IsMaleGenitaliaSize(tag);
                bool isMaleGenitaliaState = IsMaleGenitaliaState(tag);
                bool isHairLength = IsHairLength(tag);
                bool isHairColor = IsHairColor(tag);
                bool isEyesColor = IsEyesColor(tag);
                bool isSkinColor = IsSkinColor(tag);
                bool isClothingAside = IsClothingAside(tag);
                bool isLipsColor = IsLipsColor(tag);
                bool isClothingLift = IsClothingLift(tag);
                bool isClothingPull = IsClothingPull(tag);
                bool isEyewear = IsEyewear(tag);
                bool isScleraColor = IsScleraColor(tag);
                bool isAnimalEars = IsAnimalEars(tag);

                foreach (string processedTag in cleanedTags)
                {
                    if (IsRedundantWith(tag, processedTag))
                    {
                        if (tag.Length < processedTag.Length)
                        {
                            isRedundant = true;
                        }
                        else
                        {
                            cleanedTags.Remove(processedTag);
                            break;
                        }
                    }
                }

                if (isBreastSize && !hasBreastSize)
                {
                    cleanedTags.Add(tag);
                    hasBreastSize = true;
                }
                else if (isMaleGenitalia && !hasMaleGenitaliaSize)
                {
                    cleanedTags.Add(tag);
                    hasMaleGenitaliaSize = true;
                }
                else if (isMaleGenitaliaState && !hasMaleGenitaliaState)
                {
                    cleanedTags.Add(tag);
                    hasMaleGenitaliaState = true;
                }
                else if (isHairLength && !hasHairLength)
                {
                    cleanedTags.Add(tag);
                    hasHairLength = true;
                }
                else if (isHairColor && !hasHairColor)
                {
                    if (tagsSplit.Any(x => x.Equals("two-tone hair", StringComparison.OrdinalIgnoreCase)))
                    {
                        cleanedTags.Add(tag);
                    }
                    else if (tagsSplit.Any(x => x.Equals("multicolored hair", StringComparison.OrdinalIgnoreCase)))
                    {
                        cleanedTags.Add(tag);
                    }
                    else
                    {
                        cleanedTags.Add(tag);
                        hasHairColor = true;
                    }
                }
                else if (isEyesColor && !hasEyesColor)
                {
                    cleanedTags.Add(tag);
                    hasEyesColor = true;
                }
                else if (isSkinColor && !hasSkinColor)
                {
                    cleanedTags.Add(tag);
                    hasSkinColor = true;
                }
                else if (isLipsColor && !hasLipsColor)
                {
                    if (tagsSplit.Any(x => x.Equals("multicolored lips", StringComparison.OrdinalIgnoreCase)))
                    {
                        cleanedTags.Add(tag);
                    }
                    else
                    {
                        cleanedTags.Add(tag);
                        hasLipsColor = true;
                    }
                }
                else if (isScleraColor && !hasScleraColor)
                {
                    if (tagsSplit.Any(x => x.Equals("mismatched sclera", StringComparison.OrdinalIgnoreCase)))
                    {
                        cleanedTags.Add(tag);
                    }
                    else
                    {
                        cleanedTags.Add(tag);
                        hasScleraColor = true;
                    }
                }
                else if (isAnimalEars && !hasAnimalEars)
                {
                    cleanedTags.Add(tag);
                    hasAnimalEars = true;
                }
                else if (isClothingAside && !hasClothingAside)
                {
                    cleanedTags.Add(tag);
                    hasClothingAside = true;
                }
                else if (isClothingLift && !hasClothingLift)
                {
                    cleanedTags.Add(tag);
                    hasClothingLift = true;
                }
                else if (isClothingPull && !hasClothingPull)
                {
                    cleanedTags.Add(tag);
                    hasClothingPull = true;
                }
                else if (isEyewear && !hasEyewear)
                {
                    cleanedTags.Add(tag);
                    hasEyewear = true;
                }
                else if (!isBreastSize && !isMaleGenitalia && !isMaleGenitaliaState && !isHairLength &&
                         !isHairColor && !isEyesColor && !isSkinColor && !isClothingAside && !isClothingLift &&
                         !isClothingPull && !isLipsColor && !isEyewear && !isScleraColor && !isAnimalEars &&
                         !isRedundant)
                {
                    cleanedTags.RemoveWhere(x => IsRedundantWith(x, tag));
                    cleanedTags.Add(tag);
                }
            }

            foreach (string tagToRemove in _tagsToRemove)
            {
                cleanedTags.RemoveWhere(x => x.Equals(tagToRemove, StringComparison.OrdinalIgnoreCase));
            }

            return GetCommaSeparatedString(cleanedTags.ToList());
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

        #region Tag Category check methods.
        /// <summary>
        /// Determines if the given tag is redundant with another tag.
        /// </summary>
        /// <param name="tag">The tag to compare.</param>
        /// <param name="otherTag">The other tag to compare against.</param>
        /// <returns>True if the tags are redundant, false otherwise.</returns>
        private static bool IsRedundantWith(string tag, string otherTag)
        {
            return (Regex.IsMatch(otherTag, $@"\b{Regex.Escape(tag)}\b", RegexOptions.IgnoreCase, Utilities.RegexTimeout) ||
                    Regex.IsMatch(tag, $@"\b{Regex.Escape(otherTag)}\b", RegexOptions.IgnoreCase, Utilities.RegexTimeout));
        }

        /// <summary>
        /// Determines if the given tag represents a sclera color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a sclera color, false otherwise.</returns>
        private static bool IsScleraColor(string tag)
        {
            return _scleraColorsKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a breast size.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a breast size, false otherwise.</returns>
        private static bool IsBreastSize(string tag)
        {
            return _breastsSizeKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents an eyewear.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents an eyewear, false otherwise.</returns>
        private static bool IsEyewear(string tag)
        {
            return tag.Contains("eyewear");
        }

        /// <summary>
        /// Determines if the given tag represents a hair length.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a hair length, false otherwise.</returns>
        private static bool IsHairLength(string tag)
        {
            return _hairLengthKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a male genitalia size.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a male genitalia size, false otherwise.</returns>
        private static bool IsMaleGenitaliaSize(string tag)
        {
            return _pSizeKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a male genitalia state.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a male genitalia state, false otherwise.</returns>
        private static bool IsMaleGenitaliaState(string tag)
        {


            return _pStateKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a hair color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a hair color, false otherwise.</returns>
        private static bool IsHairColor(string tag)
        {
            if (tag.Contains("hairband"))
            {
                return false;
            }

            return _hairColorKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents animal ears.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents animal ears, false otherwise.</returns>
        private static bool IsAnimalEars(string tag)
        {
            return _animalEars.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a piece of clothing being lifted.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a piece of clothing being lifted, false otherwise.</returns>
        private static bool IsClothingLift(string tag)
        {
            return _clothesLiftKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a piece of clothing being pulled.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a piece of clothing being pulled, false otherwise.</returns>
        private static bool IsClothingPull(string tag)
        {
            return _clothesPullKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents eyes color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents eyes color, false otherwise.</returns>
        private static bool IsEyesColor(string tag)
        {
            if (tag.Contains("eyeshadow"))
            {
                return false;
            }

            return _eyeColorsKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents skin color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents skin color, false otherwise.</returns>
        private static bool IsLipsColor(string tag)
        {
            return _lipsColorKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents lipstick color.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents lipstick color, false otherwise.</returns>
        private static bool IsSkinColor(string tag)
        {
            return _skinColorsKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if the given tag represents a piece of clothing to the side.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a piece of clothing to the side, false otherwise.</returns>
        private static bool IsClothingAside(string tag)
        {
            return _clothingKeywords.Any(x => tag.Contains(x, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        /// <summary>
        /// Processes search and replace operations on the provided captions.
        /// </summary>
        /// <param name="captions">The original caption content.</param>
        /// <param name="wordsToBeReplaced">The words to be replaced.</param>
        /// <param name="wordsToReplace">The replacement words.</param>
        /// <returns>The caption content after search and replace operations.</returns>
        private static string ProcessSearchAndReplace(string captions, string wordsToBeReplaced, string wordsToReplace)
        {
            string[] wordsToBeReplacedSplit = Utilities.ParseAndCleanTags(wordsToBeReplaced);
            string[] wordsToReplaceSplit = Utilities.ParseAndCleanTags(wordsToReplace);

            if (wordsToReplaceSplit.Length != wordsToBeReplacedSplit.Length)
            {
                throw new ArgumentException("Amount must be the same!");
            }

            string[] captionsSplit = Utilities.ParseAndCleanTags(captions);

            for (int i = 0; i < captionsSplit.Length; i++)
            {
                for (int j = 0; j < wordsToBeReplacedSplit.Length; j++)
                {
                    if (!captionsSplit[i].Contains(wordsToReplaceSplit[j]))
                    {
                        captionsSplit[i] = Regex.Replace(captionsSplit[i], $@"\b{Regex.Escape(wordsToBeReplacedSplit[j])}\b",
                            wordsToReplaceSplit[j], RegexOptions.IgnoreCase, Utilities.RegexTimeout);
                    }
                }
            }

            return string.Join(", ", captionsSplit);
        }

        /// <summary>
        /// This method takes a list of tags and processes them based on user input and predictions from a machine learning (ML) model. It first adds the user-input tags to the result list, then adds the tags that should be emphasized (brought to the front of the list), and finally adds the tags predicted by the ML model. The method also removes any unwanted tags. The processed tags are returned as a comma-separated string.
        /// </summary>
        /// <param name="predictedTags">An IEnumerable of predicted tags from the ML model.</param>
        /// <param name="tagsToAdd">A string of user input tags to add to the result list. Multiple tags should be separated by commas.</param>
        /// <param name="tagsToEmphasize">A string of tags to bring to the front of the result list. Multiple tags should be separated by commas.</param>
        /// <param name="tagsToRemove">A string of tags to remove from the result list. Multiple tags should be separated by commas.</param>
        /// <returns>A comma-separated string of processed tags.</returns>
        private static string ProcessListOfTags(string predictedTags, string tagsToAdd, string tagsToEmphasize, string tagsToRemove)
        {
            List<string> tagsResult = new List<string>();

            // Add user input tags.
            if (!string.IsNullOrEmpty(tagsToAdd))
            {
                string[] tagsToAddSplit = Utilities.ParseAndCleanTags(tagsToAdd);

                foreach (string tag in tagsToAddSplit)
                {
                    tagsResult.Add(tag);
                }
            }

            string[] predictedTagsSplit = Utilities.ParseAndCleanTags(predictedTags);

            // Add tags that should be Emphasized(bring to the front of the list).
            if (!string.IsNullOrEmpty(tagsToEmphasize))
            {
                string[] tagsToEmphasizeSplit = Utilities.ParseAndCleanTags(tagsToEmphasize);

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
                string[] tagsToRemoveSplit = Utilities.ParseAndCleanTags(tagsToRemove);

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
        /// <param name="tagsToBeReplaced">The comma-separated list of tags to be replaced.</param>
        /// <param name="tagsToReplace">The comma-separated list of replacement tags.</param>
        /// <returns>The modified string with the tags replaced.</returns>
        /// <exception cref="ArgumentException">Thrown when the number of tags to replace is not equal to the number of replacement tags.</exception>
        private static string ReplaceListOfTags(string tags, string tagsToBeReplaced, string tagsToReplace)
        {
            List<string> tagsResult = new List<string>();

            string[] tagsToBeReplacedSplit = Utilities.ParseAndCleanTags(tagsToBeReplaced);
            string[] tagsToReplaceSplit = Utilities.ParseAndCleanTags(tagsToReplace);

            if (tagsToReplaceSplit.Length != tagsToBeReplacedSplit.Length)
            {
                throw new ArgumentException("Amount of tags must be the same!");
            }

            string[] tagsSplit = Utilities.ParseAndCleanTags(tags);

            foreach (string tag in tagsSplit)
            {
                if (tagsToBeReplacedSplit.Contains(tag))
                {
                    int index = Array.IndexOf(tagsToBeReplacedSplit, tag);
                    tagsResult.Add(tagsToReplaceSplit[index]);
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
        private static string RandomizeTags(string tags)
        {
            List<string> tagsSplit = Utilities.ParseAndCleanTags(tags).ToList();

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

        /// <summary>
        /// Retrieves a dictionary of tags and their frequencies from text files in the specified folder.
        /// </summary>
        /// <param name="inputFolderPath">The path to the folder containing text files.</param>
        /// <returns>
        /// A dictionary where keys represent tags and values represent their corresponding frequencies,
        /// sorted in descending order of frequency.
        /// </returns>
        private static Dictionary<string, uint> GetTagsWithFrequency(string inputFolderPath)
        {
            Dictionary<string, uint> tagsWithFrequency = new Dictionary<string, uint>();
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, "*.txt");

            foreach (string file in files)
            {
                string fileTags = File.ReadAllText(file);
                string[] split = Utilities.ParseAndCleanTags(Regex.Replace(fileTags, @"\r\n?|\n", "", RegexOptions.IgnoreCase, Utilities.RegexTimeout));

                foreach (string splittedTag in split)
                {
                    string match = tagsWithFrequency.Keys.FirstOrDefault(x => x.Equals(splittedTag));
                    if (match == null)
                    {
                        tagsWithFrequency.Add(splittedTag, 1);
                    }
                    else
                    {
                        tagsWithFrequency[match]++;
                    }
                }
            }

            return tagsWithFrequency;
        }

        /// <summary>
        /// Processes and consolidates similar tags within the provided input tags based on shared words in multi-word tags.
        /// </summary>
        /// <param name="sha256">The SHA-256 instance used for computing hash codes.</param>
        /// <param name="tags">The input tags to be consolidated.</param>
        /// <returns>The consolidated tags as a single string.</returns>
        /// <remarks>
        /// This method processes the input tags, identifies and consolidates similar tags based on shared words
        /// in multi-word tags, and returns the consolidated tags as a single string.
        /// </remarks>
        private static string ProcessConsolidateTags(SHA256 sha256, string tags)
        {
            List<string> tagsResult = new List<string>();

            string[] splitTags = Utilities.ParseAndCleanTags(tags);

            Dictionary<string, List<string>> tagsWithSimilarity = GetDictionaryOfSimilarTags(tagsResult, splitTags);
            if (tagsWithSimilarity.Count == 0)
            {
                return tags;
            }

            Dictionary<string, List<string>> tagsWithSimilaritySorted = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<string>> tagWithSimilarity in tagsWithSimilarity)
            {
                List<string> hashSortedTags = HashAndSortTags(sha256, tagWithSimilarity.Value);
                tagsWithSimilaritySorted.Add(tagWithSimilarity.Key, hashSortedTags);
            }

            StringBuilder tagStringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, List<string>> keyValuePairs in tagsWithSimilaritySorted)
            {
                tagStringBuilder.Clear();
                foreach (string value in keyValuePairs.Value.Distinct())
                {
                    tagStringBuilder.Append(value);
                    tagStringBuilder.Append(" ");
                }
                tagStringBuilder.Append(keyValuePairs.Key);
                tagsResult.Add(tagStringBuilder.ToString());
            }

            return string.Join(", ", tagsResult.Distinct());
        }

        /// <summary>
        /// Creates a dictionary of similar tags based on shared words in multi-word tags.
        /// </summary>
        /// <param name="tagsResult">A list to store consolidated tags.</param>
        /// <param name="splitTags">An array of input tags to process.</param>
        /// <returns>A dictionary where keys are last words in multi-word tags, and values are lists of related tags.</returns>
        private static Dictionary<string, List<string>> GetDictionaryOfSimilarTags(List<string> tagsResult, string[] splitTags)
        {
            Dictionary<string, List<string>> tagsWithSimilarity = new Dictionary<string, List<string>>();

            foreach (string tag in splitTags)
            {
                if (tag.Contains(' ') && !IsEdgeCase(tag))
                {
                    string[] tagSplit = tag.Split(' ');
                    string lastTag = tagSplit.Last();

                    if (!tagsWithSimilarity.ContainsKey(lastTag))
                    {
                        tagsWithSimilarity.Add(lastTag, new List<string>());
                    }

                    foreach (string currentTag in tagSplit)
                    {
                        if (!currentTag.Equals(lastTag))
                        {
                            tagsWithSimilarity[lastTag].Add(currentTag);
                        }
                    }
                }
                else
                {
                    tagsResult.Add(tag);
                }
            }

            return tagsWithSimilarity;
        }

        /// <summary>
        /// Determines whether a given tag is an edge case, which may require special handling or consideration.
        /// </summary>
        /// <param name="tag">The tag to evaluate.</param>
        /// <returns>True if the tag is an edge case; otherwise, false.</returns>
        private static bool IsEdgeCase(string tag)
        {
            foreach (var edgeCase in _edgeCasesContains)
            {
                string pattern = @$"\b{Regex.Escape(edgeCase)}\b";
                if (Regex.IsMatch(tag, pattern, RegexOptions.IgnoreCase, Utilities.RegexTimeout))
                {
                    return true;
                }
            }

            if (_edgeCasesEquals.Any(hashedTag => tag.Equals(hashedTag)))
            {
                return true;
            }
            if (_eyeColorsKeywords.Any(hashedTag => tag.Equals(hashedTag)))
            {
                return true;
            }
            if (_breastsSizeKeywords.Any(hashedTag => tag.Equals(hashedTag)))
            {
                return true;
            }
            if (_pSizeKeywords.Any(hashedTag => tag.Contains(hashedTag)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Computes hash codes for the input tags using the provided SHA-256 instance
        /// and returns a sorted list of tags in ascending order based on their hash codes.
        /// </summary>
        /// <param name="sha256">The SHA-256 instance used for computing hash codes.</param>
        /// <param name="tags">A list of tags to be processed and sorted.</param>
        /// <returns>
        /// A sorted list of tags where the tags are ordered in ascending order based on their hash codes.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the input list of tags is empty.</exception>
        /// <remarks>
        /// This method computes hash codes for each tag using the provided SHA-256 instance
        /// and orders the tags by their hash codes. The resulting list provides a consistent
        /// and predictable order for the input tags.
        /// </remarks>
        private static List<string> HashAndSortTags(SHA256 sha256, List<string> tags)
        {
            if (tags.Count <= 1)
            {
                return tags;
            }

            Dictionary<string, int> hashedTags = new Dictionary<string, int>(tags.Count);

            foreach (var item in tags)
            {
                byte[] data = Encoding.UTF8.GetBytes(item);
                byte[] hash = sha256.ComputeHash(data);
                int hashCode = BitConverter.ToInt32(hash, 0);

                if (!hashedTags.ContainsKey(item))
                {
                    hashedTags.Add(item, hashCode);
                }
            }

            IOrderedEnumerable<KeyValuePair<string, int>> orderedTags = hashedTags.OrderBy(x => x.Value);
            return orderedTags.Select(x => x.Key).ToList();
        }
    }
}