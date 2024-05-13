using Enums;

using SmartData.Lib.Enums;

namespace SmartData.Lib.Helpers
{
    public static class Utilities
    {
        private const int _regexTimeoutSeconds = 15;

        /// <summary>
        /// Gets the timeout duration for regular expressions in seconds.
        /// </summary>
        /// <remarks>
        /// The time out is in 10 seconds.
        /// </remarks>
        public static TimeSpan RegexTimeout => TimeSpan.FromSeconds(_regexTimeoutSeconds);

        /// <summary>
        /// Gets an array of supported dimensions.
        /// </summary>
        public static SupportedDimensions[] ResolutionValues => (SupportedDimensions[])Enum.GetValues(typeof(SupportedDimensions));

        /// <summary>
        /// Gets an array of supported tag generator models.
        /// </summary>
        public static AvailableModels[] GeneratorModelValues
        {
            get
            {
                AvailableModels[] availableModels = { 
                    AvailableModels.JoyTag,
                    AvailableModels.WD14v2,
                    AvailableModels.WDv3,
                    AvailableModels.Z3DE621
                };
                return availableModels;
            }
        }

        /// <summary>
        /// Gets an array of supported upscaler models.
        /// </summary>
        public static AvailableModels[] GeneratorUpscalerModelValues
        {
            get
            {
                AvailableModels[] availableModels =
                {
                    AvailableModels.ParimgCompact_x2,
                    AvailableModels.HFA2kCompact_x2,
                    AvailableModels.HFA2kAVCSRFormerLight_x2,
                    AvailableModels.HFA2k_x4,
                    AvailableModels.SwinIR_x4,
                    AvailableModels.Swin2SR_x4,
                    AvailableModels.Nomos8kSCSRFormer_x4,
                    AvailableModels.Nomos8kSC_x4,
                    AvailableModels.LSDIRplusReal_x4,
                    AvailableModels.LSDIRplusNone_x4,
                    AvailableModels.LSDIRplusCompression_x4,
                    AvailableModels.LSDIRCompact3_x4,
                    AvailableModels.LSDIR_x4
                };
                return availableModels;
            }
        }

        /// <summary>
        /// Gets the supported images file extensions.
        /// </summary>
        /// <returns>The string representing a search pattern for the supported images.</returns>
        public static string GetSupportedImagesExtension => "*.jpg,*.jpeg,*.png,*.gif,*.webp,*.avif,*.heif";

        /// <summary>
        /// Gets an array of file paths in the specified directory that match any of the provided extensions,
        /// excluding files with the name "sample_prompt_custom.txt."
        /// </summary>
        /// <param name="folderPath">The path of the directory to search.</param>
        /// <param name="searchPattern">A comma-separated list of file extensions to match, e.g., ".txt,.docx,.png".</param>
        /// <returns>An array of strings representing the file paths that match the provided extensions.</returns>
        public static string[] GetFilesByMultipleExtensions(string folderPath, string searchPattern)
        {
            IEnumerable<string> result = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(extension => searchPattern.Contains(Path.GetExtension(extension).ToLower()));

            return result.Where(x => !x.Contains("sample_prompt_custom.txt")).ToArray();
        }

        /// <summary>
        /// Parses a string containing tags, removes extra spaces and formatting,
        /// and splits it into an array of individual tags.
        /// </summary>
        /// <param name="tags">The string containing tags to parse and clean.</param>
        /// <returns>An array of individual tags obtained from the input string.</returns>
        public static string[] ParseAndCleanTags(string tags)
        {
            return tags.Replace(", ", ",").Replace("  ", " ").Split(",");
        }

        /// <summary>
        /// Calculates the sigmoid function value for the given input.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The sigmoid value of the input.</returns>
        public static float Sigmoid(float x)
        {
            return 1f / (1f + (float)Math.Exp(-x));
        }
    }
}
