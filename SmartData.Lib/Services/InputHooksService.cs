using SharpHook;
using SharpHook.Native;

using SmartData.Lib.Interfaces;

using System.Diagnostics;

namespace SmartData.Lib.Services
{
    public class InputHooksService : IInputHooksService, IDisposable
    {
        private SimpleGlobalHook _keyboardHook;
        private Stopwatch _keyboardTimer;
        private TimeSpan _keyboardEventsDelay = TimeSpan.FromSeconds(0.1);

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

        private bool _isAltActive = false;
        public event EventHandler AltLeftArrowCombo;
        public event EventHandler AltRightArrowCombo;

        public bool IsActive { get; set; } = false;

        public InputHooksService()
        {

            _keyboardTimer = new Stopwatch();
            _keyboardTimer.Start();

            _keyboardHook = new SimpleGlobalHook(true);
            _keyboardHook.KeyPressed += OnKeyDown;
            _keyboardHook.KeyPressed += (sender, e) =>
            {
                if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcLeftAlt)
                {
                    _isAltActive = true;
                }
            };
            _keyboardHook.KeyReleased += (sender, e) =>
            {
                if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcLeftAlt)
                {
                    _isAltActive = false;
                }
            };
            _keyboardHook.MousePressed += OnMouseButtonDown;
            _keyboardHook.RunAsync();
        }

        private void OnKeyDown(object sender, KeyboardHookEventArgs e)
        {
            if (CanProcessHook())
            {
                return;
            }

            switch (e.RawEvent.Keyboard.KeyCode)
            {
                case KeyCode.VcF1:
                    ButtonF1?.Invoke(this, e);
                    break;
                case KeyCode.VcF2:
                    ButtonF2?.Invoke(this, e);
                    break;
                case KeyCode.VcF3:
                    ButtonF3?.Invoke(this, e);
                    break;
                case KeyCode.VcF4:
                    ButtonF4?.Invoke(this, e);
                    break;
                case KeyCode.VcF5:
                    ButtonF5?.Invoke(this, e);
                    break;
                case KeyCode.VcF6:
                    ButtonF6?.Invoke(this, e);
                    break;
                case KeyCode.VcF8:
                    ButtonF8?.Invoke(this, e);
                    break;
                case KeyCode.VcLeft:
                    if (_isAltActive)
                    {
                        AltLeftArrowCombo?.Invoke(this, e);
                    }
                    break;
                case KeyCode.VcRight:
                    if (_isAltActive)
                    {
                        AltRightArrowCombo?.Invoke(this, e);
                    }
                    break;
                default:
                    break;
            }

            _keyboardTimer.Restart();
        }

        private void OnMouseButtonDown(object sender, MouseHookEventArgs e)
        {
            if (CanProcessHook())
            {
                return;
            }

            switch (e.RawEvent.Mouse.Button)
            {
                case MouseButton.Button3:
                    MouseButton3?.Invoke(this, e);
                    break;
                case MouseButton.Button4:
                    MouseButton4?.Invoke(this, e);
                    break;
                case MouseButton.Button5:
                    MouseButton5?.Invoke(this, e);
                    break;
                default:
                    break;
            }

            _keyboardTimer.Restart();
        }

        private bool CanProcessHook()
        {
            return _keyboardTimer.Elapsed.TotalMilliseconds <= _keyboardEventsDelay.TotalMilliseconds && IsActive;
        }

        public void Dispose()
        {
            _keyboardHook.KeyPressed -= OnKeyDown;
            _keyboardHook?.Dispose();
            _keyboardHook = null;
        }
    }
}
