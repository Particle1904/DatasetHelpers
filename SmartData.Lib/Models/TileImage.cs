using SixLabors.ImageSharp;

namespace Models
{
    public class TileImage
    {
        public Image Image { get; }
        public int RowIndex { get; }
        public int ColumnIndex { get; }
        public int X { get; }
        public int Y { get; }

        public TileImage(Image image, int rowIndex, int columnIndex, int x, int y)
        {
            Image = image;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            X = x;
            Y = y;
        }
    }
}
