using Models.MachineLearning;

namespace Models
{
    public class TileData
    {
        public LaMaInputData LaMaInputData { get; private set; }
        public int RowIndex { get; private set; }
        public int ColumnIndex { get; private set; }

        public TileData(LaMaInputData lamaInputData, int rowIndex, int columnIndex)
        {
            LaMaInputData = lamaInputData;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }
}
