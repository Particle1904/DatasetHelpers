using SmartData.Lib.Enums;

namespace SmartData.Lib.Helpers
{
    public static class Utilities
    {
        /// <summary>
        /// Gets an array of file paths in the specified directory that match any of the provided extensions.
        /// </summary>
        /// <param name="folderPath">The path of the directory to search.</param>
        /// <param name="searchPattern">A comma-separated list of file extensions to match, e.g. ".txt,.docx".</param>
        /// <returns>An array of strings representing the file paths that match the provided extensions.</returns>
        public static string[] GetFilesByMultipleExtensions(string folderPath, string searchPattern)
        {
            IEnumerable<string> result = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(extension => searchPattern.Contains(Path.GetExtension(extension).ToLower()));

            return result.ToArray();
        }

        public static SupportedDimensions[] Values => (SupportedDimensions[])Enum.GetValues(typeof(SupportedDimensions));
    }
}
