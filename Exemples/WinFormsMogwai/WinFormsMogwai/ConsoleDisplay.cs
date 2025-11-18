using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WinFormsMogwai
{
    internal class ConsoleDisplay : TextBox
    {
        private const int
            WM_MOUSEMOVE = 0x0200,
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_LBUTTONUP = 0x0202,
            WM_CAPTURECHANGED = 0x0215,
            EM_SETSEL = 0x00B1;

        private bool _InputMode = false;
        private int _InputStartPosition = 0;
        private TaskCompletionSource<string>? _InputTaskCompletionSource;

        public ConsoleDisplay() : base()
        {
            Multiline = true;
            BorderStyle = BorderStyle.FixedSingle;
            ReadOnly = true;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CAPTURECHANGED:
                    break;
                case WM_LBUTTONDOWN:
                    break;
                case WM_LBUTTONUP:
                    break;
                case WM_LBUTTONDBLCLK:
                    break;
                case WM_MOUSEMOVE:
                    break;
                case EM_SETSEL:
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_InputMode)
            {
                switch (e.KeyCode)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Left:
                    case Keys.Right:
                        e.SuppressKeyPress = true;
                        break;

                    case Keys.Back:
                        if (SelectionStart <= _InputStartPosition)
                        {
                            e.SuppressKeyPress = true;
                        }
                        break;

                    case Keys.Enter:
                        var s = Text.Substring(_InputStartPosition, SelectionStart - _InputStartPosition);
                        _InputMode = false;
                        e.SuppressKeyPress = true;
                        ReadOnly = true;
                        WriteLine("");                      
                       _InputTaskCompletionSource?.SetResult(s);                     
                        break;
                }
            }
            else
            {
                e.SuppressKeyPress = true;
            }
        }

        public void WriteLine(string text)
        {
            AppendText($"{text}\r\n");          
        }

        public void Write(string text)
        {
            AppendText(text);
        }

        public Task<string> Input(string prompt)
        {
            if (_InputMode)
            {
                if (_InputTaskCompletionSource != null)
                {
                    return _InputTaskCompletionSource.Task;
                }
                else
                {
                    throw new Exception("Console internal error !");
                }
            }

            Invoke(() =>
            {
                _InputMode = true;

                Write(prompt);

                _InputStartPosition = SelectionStart;
                ReadOnly = false;
                
                Focus();
            });

            _InputTaskCompletionSource = new TaskCompletionSource<string>();

            return _InputTaskCompletionSource.Task; 
        }

        public void StopInputMode()
        {
            if (_InputMode)
            {
                _InputMode = false;
                ReadOnly = true;
                _InputTaskCompletionSource?.SetResult(string.Empty);
            }
        }
    }
}
