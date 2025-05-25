using SixLabors.ImageSharp;

namespace FlorenceTwoLab.Core;

public class Florence2Tasks
{
    public static readonly IReadOnlyDictionary<Florence2TaskType, Florence2Tasks> TaskConfigurations = new Dictionary<Florence2TaskType, Florence2Tasks>
    {
        // Basic query with text output
        [Florence2TaskType.Caption] = new(Florence2TaskType.Caption, "<CAPTION>", "What does the image describe?", false, false, true, false, false, false),
        [Florence2TaskType.DetailedCaption] = new(Florence2TaskType.DetailedCaption, "<DETAILED_CAPTION>", "Describe in detail what is shown in the image.", false, false, true, false, false, false),
        [Florence2TaskType.MoreDetailedCaption] = new(Florence2TaskType.MoreDetailedCaption, "<MORE_DETAILED_CAPTION>", "Describe with a paragraph what is shown in the image.", false, false, true, false, false, false),
        [Florence2TaskType.Ocr] = new(Florence2TaskType.Ocr, "<OCR>", "What is the text in the image?", false, false, true, false, false, false),

        // Basic query with regions/labels/polygons output 
        [Florence2TaskType.OcrWithRegions] = new(Florence2TaskType.OcrWithRegions, "<OCR_WITH_REGION>", "What is the text in the image, with regions?", false, false, false, true, true, true),

        // Basic query with regions/labels output 
        [Florence2TaskType.ObjectDetection] = new(Florence2TaskType.ObjectDetection, "<OD>", "Locate the objects with category name in the image.", false, false, false, true, true, false),
        [Florence2TaskType.DenseRegionCaption] = new(Florence2TaskType.DenseRegionCaption, "<DENSE_REGION_CAPTION>", "Locate the objects in the image, with their descriptions.", false, false, false, true, true, false),
        [Florence2TaskType.RegionProposal] = new(Florence2TaskType.RegionProposal, "<REGION_PROPOSAL>", "Locate the region proposals in the image.", false, false, false, true, true, false),

        // Grounding and detection
        [Florence2TaskType.CaptionToGrounding] = new(Florence2TaskType.CaptionToGrounding, "<CAPTION_TO_PHRASE_GROUNDING>", "Locate the phrases in the caption: {0}", false, true, false, true, true, false),
        [Florence2TaskType.ReferringExpressionSegmentation] = new(Florence2TaskType.ReferringExpressionSegmentation, "<REFERRING_EXPRESSION_SEGMENTATION>", "Locate {0} in the image with mask", false, true, false, false, false, true),
        [Florence2TaskType.OpenVocabularyDetection] = new(Florence2TaskType.OpenVocabularyDetection, "<OPEN_VOCABULARY_DETECTION>", "Locate {0} in the image.", false, true, false, true, false, true), // not sure yet

        // Region analysis
        [Florence2TaskType.RegionToSegmentation] = new(Florence2TaskType.RegionToSegmentation, "<REGION_TO_SEGMENTATION>", "What is the polygon mask of region {0}", true, false, false, false, false, true), // not sure yet
        [Florence2TaskType.RegionToCategory] = new(Florence2TaskType.RegionToCategory, "<REGION_TO_CATEGORY>", "What is the region {0}?", true, false, true, false, false, false), // not sure yet
        [Florence2TaskType.RegionToDescription] = new(Florence2TaskType.RegionToDescription, "<REGION_TO_DESCRIPTION>", "What does the region {0} describe?", true, false, true, false, false, false), // not sure yet
        [Florence2TaskType.RegionToOcr] = new(Florence2TaskType.RegionToOcr, "<REGION_TO_OCR>", "What text is in the region {0}?", true, false, true, false, false, false) // not sure yet
    };

    private Florence2Tasks(Florence2TaskType taskType, string promptAlias, string prompt, bool requiresRegionInput, bool requiresSubPrompt,
        bool returnsText, bool returnsLabels, bool returnsBoundingBoxes, bool returnsPolygons)
    {
        TaskType = taskType;
        PromptAlias = promptAlias;
        Prompt = prompt;
        RequiresRegionInput = requiresRegionInput;
        RequiresSubPrompt = requiresSubPrompt;
        ReturnsText = returnsText;
        ReturnsLabels = returnsLabels;
        ReturnsBoundingBoxes = returnsBoundingBoxes;
        ReturnsPolygons = returnsPolygons;
    }

    public Florence2TaskType TaskType { get; }
    public string PromptAlias { get; }
    public string Prompt { get; }
    public bool RequiresRegionInput { get; }
    public bool RequiresSubPrompt { get; }
    public bool ReturnsText { get; }
    public bool ReturnsLabels { get; }
    public bool ReturnsBoundingBoxes { get; }
    public bool ReturnsPolygons { get; }

    /// <summary>
    /// Creates a query for the specified task type.
    /// </summary>
    /// <param name="taskType">
    /// The task type. Supported types are:
    /// - Caption
    /// - DetailedCaption
    /// - MoreDetailedCaption
    /// - Ocr
    /// - OcrWithRegions
    /// - ObjectDetection
    /// - DenseRegionCaption
    /// - RegionProposal
    /// </param>
    /// <returns>A query for the specified task type.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the task type is unsupported, or when it requires region or sub-prompt parameters.
    /// </exception>
    public static Florence2Query CreateQuery(Florence2TaskType taskType)
    {
        if (!TaskConfigurations.TryGetValue(taskType, out Florence2Tasks? config))
        {
            throw new ArgumentException($"Unsupported task type: {taskType}");
        }

        if (config.RequiresRegionInput)
        {
            throw new ArgumentException($"Task {taskType} requires region parameter");
        }

        if (config.RequiresSubPrompt)
        {
            throw new ArgumentException($"Task {taskType} requires sub-prompt parameter");
        }

        return new Florence2Query(taskType, config.Prompt);
    }

    /// <summary>
    /// Creates a query for the specified task type with the specified region.
    /// </summary>
    /// <param name="taskType">
    /// The task type. Supported types are:
    /// - RegionToSegmentation
    /// - RegionToCategory
    /// - RegionToDescription
    /// - RegionToOcr
    /// </param>
    /// <param name="region">The region of the image to query.</param>
    /// <returns>A query for the specified task type with the specified region.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the task type is unsupported or does not handle region parameters.
    /// </exception>
    public static Florence2Query CreateQuery(Florence2TaskType taskType, RectangleF region)
    {
        if (!TaskConfigurations.TryGetValue(taskType, out Florence2Tasks? config))
        {
            throw new ArgumentException($"Unsupported task type: {taskType}");
        }

        if (!config.RequiresRegionInput)
        {
            throw new ArgumentException($"Task {taskType} does not handle region parameter");
        }

        string regionString = region.CreateNormalizedRegionString();
        return new Florence2Query(taskType, string.Format(config.Prompt, regionString));
    }

    /// <summary>
    /// Creates a query for the specified task type with the specified sub-prompt.
    /// </summary>
    /// <param name="taskType">
    /// The task type. Supported types are:
    /// - CaptionToGrounding
    /// - ReferringExpressionSegmentation
    /// - OpenVocabularyDetection
    /// </param>
    /// <param name="subPrompt">The sub-prompt to include in the query.</param>
    /// <returns>A query for the specified task type with the specified sub-prompt.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the task type is unsupported or does not handle sub-prompts.
    /// </exception>
    public static Florence2Query CreateQuery(Florence2TaskType taskType, string subPrompt)
    {
        if (!TaskConfigurations.TryGetValue(taskType, out Florence2Tasks? config))
        {
            throw new ArgumentException($"Unsupported task type: {taskType}");
        }

        if (!config.RequiresSubPrompt)
        {
            throw new ArgumentException($"Task {taskType} does not handle input parameter");
        }

        return new Florence2Query(taskType, string.Format(config.Prompt, subPrompt));
    }
}
