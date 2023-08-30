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

        public InputHooksService()
        {
            _keyboardTimer = new Stopwatch();
            _keyboardTimer.Start();

            _keyboardHook = new SimpleGlobalHook(true);
            _keyboardHook.KeyPressed += OnKeyDown;
            _keyboardHook.MousePressed += OnMouseButtonDown;
            _keyboardHook.RunAsync();
        }

        private void OnKeyDown(object sender, KeyboardHookEventArgs e)
        {
            if (CanProcessHook())
            {
                return;
            }

            if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF1)
            {
                OnF1ButtonDown(EventArgs.Empty);
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF2)
            {
                OnF2ButtonDown(EventArgs.Empty);
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF3)
            {
                OnF3ButtonDown(EventArgs.Empty);
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF4)
            {
                OnF4ButtonDown(EventArgs.Empty);
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF5)
            {
                OnF5ButtonDown(EventArgs.Empty);
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF6)
            {
                OnF6ButtonDown(EventArgs.Empty);
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF8)
            {
                OnF8ButtonDown(EventArgs.Empty);
            }

            _keyboardTimer.Restart();
        }

        private void OnMouseButtonDown(object sender, MouseHookEventArgs e)
        {
            if (CanProcessHook())
            {
                return;
            }

            if (e.RawEvent.Mouse.Button == MouseButton.Button4)
            {
                OnMouseButton4Down(EventArgs.Empty);
            }
            else if (e.RawEvent.Mouse.Button == MouseButton.Button5)
            {
                OnMouseButton5Down(EventArgs.Empty);
            }
            else if (e.RawEvent.Mouse.Button == MouseButton.Button3)
            {
                OnMouseButton3Down(EventArgs.Empty);
            }

            _keyboardTimer.Restart();
        }

        private void OnF1ButtonDown(EventArgs e)
        {
            ButtonF1?.Invoke(this, e);
        }

        private void OnF2ButtonDown(EventArgs e)
        {
            ButtonF2?.Invoke(this, e);
        }

        private void OnF3ButtonDown(EventArgs e)
        {
            ButtonF3?.Invoke(this, e);
        }

        private void OnF4ButtonDown(EventArgs e)
        {
            ButtonF4?.Invoke(this, e);
        }

        private void OnF5ButtonDown(EventArgs e)
        {
            ButtonF5?.Invoke(this, e);
        }

        private void OnF6ButtonDown(EventArgs e)
        {
            ButtonF6?.Invoke(this, e);
        }

        private void OnF8ButtonDown(EventArgs e)
        {
            ButtonF8?.Invoke(this, e);
        }

        private void OnMouseButton4Down(EventArgs e)
        {
            MouseButton4?.Invoke(this, e);
        }

        private void OnMouseButton5Down(EventArgs e)
        {
            MouseButton5?.Invoke(this, e);
        }

        private void OnMouseButton3Down(EventArgs e)
        {
            MouseButton3?.Invoke(this, e);
        }

        private bool CanProcessHook()
        {
            return _keyboardTimer.Elapsed.TotalMilliseconds <= _keyboardEventsDelay.TotalMilliseconds;
        }

        public void Dispose()
        {
            _keyboardHook.KeyPressed -= OnKeyDown;
            _keyboardHook?.Dispose();
            _keyboardHook = null;
        }
    }
}
