using SixLabors.ImageSharp;

namespace Models
{
    public class TileImage
    {
        public Image Image { get; private set; }
        public int RowIndex { get; private set; }
        public int ColumnIndex { get; private set; }

        public TileImage(Image image, int rowIndex, int columnIndex)
        {
            Image = image;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }
}
