namespace FlorenceTwoLab.Core;

/// <summary>
/// Enumerates the supported Florence-2 task types
/// </summary>
public enum Florence2TaskType
{
    // Basic captioning
    Caption, // supported by post-processor
    DetailedCaption, // supported by post-processor
    MoreDetailedCaption, // supported by post-processor

    // OCR
    Ocr, // supported by post-processor
    OcrWithRegions, // supported by post-processor

    // Object detection
    ObjectDetection, // supported by post-processor
    DenseRegionCaption, // supported by post-processor
    RegionProposal, // supported by post-processor

    // Region analysis
    RegionToDescription,
    RegionToSegmentation,
    RegionToCategory,
    RegionToOcr,

    // Grounding and detection
    CaptionToGrounding,
    ReferringExpressionSegmentation,
    OpenVocabularyDetection
}