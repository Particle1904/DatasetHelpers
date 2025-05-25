using System.Text.RegularExpressions;

using SixLabors.ImageSharp;

namespace FlorenceTwoLab.Core;

// TODO: should split this class into multiple classes, one per task configuration
public partial class DecoderPostProcessor
{
    /// <summary>
    /// Gets the regular expression used to match labeled regions consisting of a category followed by location coordinates.
    /// </summary>
    /// <returns>A compiled regular expression that matches a category followed by one or more bounding boxes represented by 4 points.</returns>
    [GeneratedRegex(@"(\w+)(<loc_(\d+)><loc_(\d+)><loc_(\d+)><loc_(\d+)>)+", RegexOptions.Compiled)]
    private static partial Regex CategoryAndRegionRegex();

    /// <summary>
    /// Gets the regular expression used to match a category followed by a quadrilateral described with 8 location points.
    /// </summary>
    /// <returns>A compiled regular expression for extracting labeled quadrilateral boxes from the model output.</returns>
    [GeneratedRegex(@"([^<]+)(?:<loc_(\d+)><loc_(\d+)><loc_(\d+)><loc_(\d+)><loc_(\d+)><loc_(\d+)><loc_(\d+)><loc_(\d+)>)")]
    private static partial Regex CategoryAndQuadBoxRegex();

    /// <summary>
    /// Gets the regular expression used to extract individual location points.
    /// </summary>
    /// <returns>A compiled regular expression that matches a single location coordinate tag.</returns>
    [GeneratedRegex(@"<loc_(\d+)>")]
    private static partial Regex PointRegex();

    /// <summary>
    /// Gets the regular expression used to extract labeled polygons from model output strings.
    /// </summary>
    /// <returns>A compiled regular expression that matches category labels followed by polygon data.</returns>
    [GeneratedRegex(@"(\w+)<poly>(<loc_(\d+)>)+</poly>")]
    private static partial Regex LabeledPolygonsRegex();

    /// <summary>
    /// Processes the raw model output into structured results based on the specified task type.
    /// </summary>
    /// <param name="modelOutput">The raw output string from the model.</param>
    /// <param name="taskType">The task type indicating how the output should be interpreted.</param>
    /// <param name="imageWasPadded">Indicates whether the input image was padded before processing.</param>
    /// <param name="imageWidth">The width of the original image.</param>
    /// <param name="imageHeight">The height of the original image.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the structured output.</returns>
    /// <exception cref="ArgumentException">Thrown when the task type is unsupported.</exception>
    public async Task<Florence2Result> ProcessAsync(string modelOutput, Florence2TaskType taskType, bool imageWasPadded, int imageWidth, int imageHeight)
    {
        Florence2Tasks.TaskConfigurations.TryGetValue(taskType, out Florence2Tasks? taskConfig);

        // Florence2TaskType.OpenVocabularyDetection: arms<poly><loc_550><loc_421><loc_686><loc_510><loc_671><loc_740><loc_540><loc_616></poly>

        return taskConfig switch
        {
            // Advanced detection tasks, returns quad boxes.
            { ReturnsLabels: true, ReturnsBoundingBoxes: true, ReturnsPolygons: true } => await ProcessPointsAsQuadBoxesAsync(taskType, modelOutput, imageWasPadded, imageWidth, imageHeight),

            // Detection tasks
            { ReturnsLabels: true, ReturnsBoundingBoxes: true } => await ProcessPointsAsBoundingBoxesAsync(taskType, modelOutput, imageWasPadded, imageWidth, imageHeight),

            // Complex tasks
            { ReturnsLabels: true, ReturnsPolygons: true } => await ProcessLabeledPolygonsAsync(taskType, modelOutput, imageWasPadded, imageWidth, imageHeight),

            // Complex tasks
            { ReturnsPolygons: true } => await ProcessPointsAsPolygonsAsync(taskType, modelOutput, imageWasPadded, imageWidth, imageHeight),

            // Text generation tasks (captions, OCR)
            { ReturnsText: true } => new Florence2Result { TaskType = taskType, Text = modelOutput },

            // Region tasks - returns text probably
            // Florence2TaskType.RegionToDescription or
            //     Florence2TaskType.RegionToCategory or
            //     Florence2TaskType.RegionToOcr => await ProcessRegionResultAsync(taskType, modelOutput),

            _ => throw new ArgumentException($"Unsupported task type: {taskType}")
        };
    }

    /// <summary>
    /// Processes labeled rectangular regions (bounding boxes) from the model output.
    /// </summary>
    /// <param name="taskType">The task type being processed.</param>
    /// <param name="modelOutput">The raw output string from the model.</param>
    /// <param name="imageWasPadded">Indicates if the image was padded during preprocessing.</param>
    /// <param name="imageWidth">The original width of the input image.</param>
    /// <param name="imageHeight">The original height of the input image.</param>
    /// <returns>A task containing the result with extracted labels and bounding boxes.</returns>
    private async Task<Florence2Result> ProcessPointsAsBoundingBoxesAsync(Florence2TaskType taskType, string modelOutput, bool imageWasPadded, int imageWidth, int imageHeight)
    {
        // NOTE: "wheel" has two bounding boxes, "door" has one
        // example data: car<loc_54><loc_375><loc_906><loc_707>door<loc_710><loc_276><loc_908><loc_537>wheel<loc_708><loc_557><loc_865><loc_704><loc_147><loc_563><loc_305><loc_705>
        // regex that parses one or more "(category)(one or more groups of 4 loc-tokens)"
        Regex regex = CategoryAndRegionRegex();

        float w = imageWidth / 1000f;
        float h = imageHeight / 1000f;

        List<string> labels = new();
        List<Rectangle> boundingBoxes = new();

        Match match = regex.Match(modelOutput);
        while (match.Success)
        {
            string label = match.Groups[1].Value;
            int captureCount = match.Groups[2].Captures.Count;
            for (int i = 0; i < captureCount; i++)
            {
                float x1 = int.Parse(match.Groups[3].Captures[i].Value);
                float y1 = int.Parse(match.Groups[4].Captures[i].Value);
                float x2 = int.Parse(match.Groups[5].Captures[i].Value);
                float y2 = int.Parse(match.Groups[6].Captures[i].Value);

                labels.Add(label);
                boundingBoxes.Add(new Rectangle(
                    (int)((0.5f + x1) * w),
                    (int)((0.5f + y1) * h),
                    (int)((x2 - x1) * w),
                    (int)((y2 - y1) * h)));
            }

            match = match.NextMatch();
        }

        return new Florence2Result { TaskType = taskType, BoundingBoxes = boundingBoxes, Labels = labels };
    }

    /// <summary>
    /// Processes labeled quadrilaterals (quad boxes) from the model output.
    /// </summary>
    /// <param name="taskType">The task type being processed.</param>
    /// <param name="modelOutput">The raw output string from the model.</param>
    /// <param name="imageWasPadded">Indicates if the image was padded during preprocessing.</param>
    /// <param name="imageWidth">The original width of the input image.</param>
    /// <param name="imageHeight">The original height of the input image.</param>
    /// <returns>A task containing the result with extracted labels, quad boxes, and corresponding bounding rectangles.</returns>
    private async Task<Florence2Result> ProcessPointsAsQuadBoxesAsync(Florence2TaskType taskType, string modelOutput, bool imageWasPadded, int imageWidth, int imageHeight)
    {
        // Regex to match text followed by 8 location coordinates
        Regex regex = CategoryAndQuadBoxRegex();

        MatchCollection matches = regex.Matches(modelOutput);

        List<IReadOnlyCollection<Point>> quadBoxes = new List<IReadOnlyCollection<Point>>();
        List<string> labels = new List<string>();

        float w = imageWidth / 1000f;
        float h = imageHeight / 1000f;

        foreach (Match match in matches)
        {
            string text = match.Groups[1].Value;

            // Extract all 8 coordinates
            Point[] points = new Point[4];
            for (int i = 0; i < 8; i += 2)
            {
                // Add 2 to group index because group[1] is the text
                float valueX = 0.5f + int.Parse(match.Groups[i + 2].Value);
                float valueY = 0.5f + int.Parse(match.Groups[i + 3].Value);

                // Convert from 0-1000 range to image coordinates
                points[i / 2] = new Point((int)(valueX * w), (int)(valueY * h));
            }

            quadBoxes.Add(points);

            labels.Add(text);
        }

        // If you need to maintain compatibility with existing Rectangle format,
        // you could compute bounding rectangles that encompass each quad:
        List<Rectangle> boundingBoxes = quadBoxes.Select(quad =>
        {
            int minX = quad.Min(p => p.X);
            int minY = quad.Min(p => p.Y);
            int maxX = quad.Max(p => p.X);
            int maxY = quad.Max(p => p.Y);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }).ToList();

        return new Florence2Result
        {
            TaskType = taskType,
            BoundingBoxes = boundingBoxes,
            Labels = labels,
            Polygons = quadBoxes
        };
    }

    /// <summary>
    /// Processes labeled polygons from the model output using structured polygon tags.
    /// </summary>
    /// <param name="taskType">The task type being processed.</param>
    /// <param name="modelOutput">The raw output string from the model.</param>
    /// <param name="imageWasPadded">Indicates if the image was padded during preprocessing.</param>
    /// <param name="imageWidth">The original width of the input image.</param>
    /// <param name="imageHeight">The original height of the input image.</param>
    /// <returns>A task containing the result with extracted labels and polygons.</returns>
    private Task<Florence2Result> ProcessLabeledPolygonsAsync(Florence2TaskType taskType, string modelOutput, bool imageWasPadded, int imageWidth, int imageHeight)
    {
        Regex regex = LabeledPolygonsRegex();
        Match match = regex.Match(modelOutput);

        List<string> labels = new List<string>();
        List<IReadOnlyCollection<Point>> polygons = new List<IReadOnlyCollection<Point>>();
        float w = imageWidth / 1000f;
        float h = imageHeight / 1000f;

        while (match.Success)
        {
            string label = match.Groups[1].Value;
            List<Point> polygon = new List<Point>();
            Group coordinates = match.Groups[3];

            for (int i = 0; i < coordinates.Captures.Count; i += 2)
            {
                int x = (int)((0.5f + int.Parse(coordinates.Captures[i].Value)) * w);
                int y = (int)((0.5f + int.Parse(coordinates.Captures[i + 1].Value)) * h);
                polygon.Add(new Point(x, y));
            }

            labels.Add(label);
            polygons.Add(polygon);

            match = match.NextMatch();
        }

        return Task.FromResult(new Florence2Result { TaskType = taskType, Labels = labels, Polygons = polygons });
    }

    /// <summary>
    /// Processes unnamed polygons from the model output using location point tags.
    /// </summary>
    /// <param name="taskType">The task type being processed.</param>
    /// <param name="modelOutput">The raw output string from the model.</param>
    /// <param name="imageWasPadded">Indicates if the image was padded during preprocessing.</param>
    /// <param name="imageWidth">The original width of the input image.</param>
    /// <param name="imageHeight">The original height of the input image.</param>
    /// <returns>A task containing the result with extracted polygons.</returns>
    private Task<Florence2Result> ProcessPointsAsPolygonsAsync(Florence2TaskType taskType, string modelOutput, bool imageWasPadded, int imageWidth, int imageHeight)
    {
        Regex regex = PointRegex();
        Regex.ValueMatchEnumerator matches = regex.EnumerateMatches(modelOutput);

        // for now, we only support a single polygon
        List<IReadOnlyCollection<Point>> polygons = new List<IReadOnlyCollection<Point>>();
        List<Point> polygon = new List<Point>();
        polygons.Add(polygon);

        float w = imageWidth / 1000f;
        float h = imageHeight / 1000f;

        // With match "<loc_XX>" the X is at index 5, and has the length match.Length - 5 - 1
        const int offset = 5;
        const int lengthOffset = 6;

        int count = 0;
        int x = 0;
        foreach (ValueMatch match in matches)
        {
            int matchOffset = match.Index + offset;
            int matchLength = match.Length - lengthOffset;
            if (count % 2 == 0)
            {
                x = (int)((0.5f + int.Parse(modelOutput.AsSpan(matchOffset, matchLength))) * w);
            }
            else
            {
                int y = (int)((0.5f + int.Parse(modelOutput.AsSpan(matchOffset, matchLength))) * h);
                polygon.Add(new Point(x, y));
            }

            count++;
        }

        return Task.FromResult(new Florence2Result { TaskType = taskType, Polygons = polygons });
    }
}
