using Models.MachineLearning;

namespace Models
{
    public class TileData
    {
        public LaMaInputData LaMaInputData { get; }
        public int RowIndex { get; }
        public int ColumnIndex { get; }
        public int X { get; }
        public int Y { get; }

        public TileData(LaMaInputData lamaInputData, int rowIndex, int columnIndex, int x, int y)
        {
            LaMaInputData = lamaInputData;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            X = x;
            Y = y;
        }
    }
}
