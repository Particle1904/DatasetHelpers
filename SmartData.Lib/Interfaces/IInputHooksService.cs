namespace SmartData.Lib.Interfaces
{
    public interface IInputHooksService
    {
        public event EventHandler ButtonF1;
        public event EventHandler ButtonF2;
        public event EventHandler ButtonF3;
        public event EventHandler ButtonF4;
        public event EventHandler ButtonF5;
        public event EventHandler ButtonF6;
        public event EventHandler ButtonF8;

        public event EventHandler MouseButton3;
        public event EventHandler MouseButton4;
        public event EventHandler MouseButton5;

        public bool IsActive { get; set; }
    }
}