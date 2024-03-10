namespace SmartData.Lib.Models.MachineLearning
{
    public class DetectedPerson
    {
        public float[] BoundingBox { get; }
        public float Confidence { get; }

        public DetectedPerson(float[] boundingBox, float confidence)
        {
            BoundingBox = boundingBox;
            Confidence = confidence;
        }
    }
}
