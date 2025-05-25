using SixLabors.ImageSharp;

namespace FlorenceTwoLab.Core;

public static class Florence2LocationTokens
{
    // Florence uses coordinates from 0-999 for location tokens
    private const int TokenCoordinateRange = 1000;

    /// <summary>
    /// Converts a normalized region (with float coordinates ranging from 0 to 1) to a Florence location token string.
    /// </summary>
    /// <param name="region">The normalized rectangular region to convert.</param>
    /// <returns>A string containing the Florence-style location tokens.</returns>
    /// <remarks>
    /// The region coordinates are scaled by 1000 and clamped to the valid token range (0 to 999).
    /// </remarks>
    public static string CreateNormalizedRegionString(this RectangleF region)
    {
        Point topleft = new Point(
            (int)(region.Left * TokenCoordinateRange),
            (int)(region.Top * TokenCoordinateRange));
        Point bottomRight = new Point(
            (int)(region.Right * TokenCoordinateRange),
            (int)(region.Bottom * TokenCoordinateRange));

        return CoordinatesToTokens(topleft, bottomRight);
    }

    /// <summary>
    /// Converts a pair of top-left and bottom-right coordinates into a Florence-style location token string.
    /// </summary>
    /// <param name="topLeft">The top-left corner of the region.</param>
    /// <param name="bottomRight">The bottom-right corner of the region.</param>
    /// <returns>A string containing the Florence-style location tokens in the format &lt;loc_x1&gt;&lt;loc_y1&gt;&lt;loc_x2&gt;&lt;loc_y2&gt;.</returns>
    /// <remarks>
    /// Coordinates are clamped to the 0-999 range before conversion.
    /// </remarks>
    public static string CoordinatesToTokens(Point topLeft, Point bottomRight)
    {
        // Florence expects coordinates in format: <loc_x1><loc_y1><loc_x2><loc_y2>
        return $"<loc_{NormalizeCoordinate(topLeft.X)}>" +
               $"<loc_{NormalizeCoordinate(topLeft.Y)}>" +
               $"<loc_{NormalizeCoordinate(bottomRight.X)}>" +
               $"<loc_{NormalizeCoordinate(bottomRight.Y)}>";
    }

    /// <summary>
    /// Converts a rectangle into a Florence-style location token string.
    /// </summary>
    /// <param name="boundingBox">The rectangle to convert.</param>
    /// <returns>A string containing the Florence-style location tokens.</returns>
    /// <remarks>
    /// This is a convenience overload that extracts the top-left and bottom-right points from the rectangle.
    /// </remarks>
    public static string CoordinatesToTokens(Rectangle boundingBox)
    {
        return CoordinatesToTokens(
            new Point(boundingBox.Left, boundingBox.Top),
            new Point(boundingBox.Right, boundingBox.Bottom));
    }

    /// <summary>
    /// Converts a Florence-style location token string back into a rectangle using the given image dimensions.
    /// </summary>
    /// <param name="locationTokens">The Florence-style location token string to parse.</param>
    /// <param name="imageSize">The size of the image used to denormalize the coordinates.</param>
    /// <returns>A rectangle representing the denormalized region described by the tokens.</returns>
    /// <exception cref="ArgumentException">Thrown when the token string does not contain exactly four coordinates.</exception>
    public static Rectangle TokensToCoordinates(string locationTokens, Size imageSize)
    {
        List<int> coordinates = ParseLocationTokens(locationTokens);
        if (coordinates.Count != 4)
        {
            throw new ArgumentException("Location tokens must contain exactly 4 coordinates", nameof(locationTokens));
        }

        return new Rectangle(
            DenormalizeCoordinate(coordinates[0], imageSize.Width),
            DenormalizeCoordinate(coordinates[1], imageSize.Height),
            DenormalizeCoordinate(coordinates[2], imageSize.Width) - DenormalizeCoordinate(coordinates[0], imageSize.Width),
            DenormalizeCoordinate(coordinates[3], imageSize.Height) - DenormalizeCoordinate(coordinates[1], imageSize.Height)
        );
    }

    /// <summary>
    /// Clamps a coordinate to the valid Florence token range (0 to 999).
    /// </summary>
    /// <param name="coordinate">The coordinate to normalize.</param>
    /// <returns>The normalized coordinate, clamped within the valid range.</returns>
    private static int NormalizeCoordinate(int coordinate)
    {
        return Math.Clamp(coordinate, 0, TokenCoordinateRange - 1);
    }

    /// <summary>
    /// Denormalizes a single coordinate from the 0-999 token space to the actual image dimension.
    /// </summary>
    /// <param name="normalizedCoordinate">The coordinate in token space (0 to 999).</param>
    /// <param name="imageDimension">The actual image width or height.</param>
    /// <returns>The denormalized pixel coordinate.</returns>
    public static int DenormalizeCoordinate(int normalizedCoordinate, int imageDimension)
    {
        return (normalizedCoordinate * imageDimension) / TokenCoordinateRange;
    }

    /// <summary>
    /// Parses a Florence-style location token string and extracts the integer coordinate values.
    /// </summary>
    /// <param name="tokens">The location token string to parse.</param>
    /// <returns>A list of integer coordinates extracted from the token string.</returns>
    private static List<int> ParseLocationTokens(string tokens)
    {
        List<int> coordinates = new List<int>();
        System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(tokens, @"<loc_(\d+)>");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int coordinate))
            {
                coordinates.Add(coordinate);
            }
        }

        return coordinates;
    }
}
