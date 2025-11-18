using MOGWAI;
using MOGWAI.Classes;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WinFormsMogwai
{
    public partial class FormMain : Form, MOGWAI.Classes.MOGEngineDelegate
    {
        private MOGEngine _MogwaiEngine;

        private float currentWidth = 1;
        private Color currentColor = Color.Yellow;
        private Pen currentPen;
        private Bitmap? currentImage;
        private bool penIsDown = true;

        private double turtleX;
        private double turtleY;
        private double turtleAngle;
        private Color turtleColor = Color.Yellow;
        private bool turtleIsVisible = true;

        private const int EM_SETTABSTOPS = 0x00CB;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr h, int msg, int wParam, int[] lParam);

        public FormMain()
        {
            InitializeComponent();

            // Création du moteur de scripting.
            // On lui donne un nom qui sera utilisé pour créer son dossier de travail.
            // Par défaut, le dossier de travail est dans DOCUMENTS/MOGWAI/APPS/[NOM DU MOTEUR]

            _MogwaiEngine = new MOGEngine("MOGWAI INTEGRATION", @delegate: this);

            // Le dossier de travail sera donc "C:\Users\[USER]\Documents\MOGWAI\APPS\MOGWAI INTEGRATION"

            // On désactive certaines fonctions qui ne sont pas utilisables dans cet exemple.
            // Si ces fonctions sont utilisées, une erreur sera levée.

            _MogwaiEngine.SetKeywordStatus("console.cursor", false);
            _MogwaiEngine.SetKeywordStatus("console.show", false);
            _MogwaiEngine.SetKeywordStatus("console.hide", false);
            _MogwaiEngine.SetKeywordStatus("console.inputkey", false);

            // Initialisation du crayon utilisé par la tortue.

            currentPen = new Pen(currentColor, currentWidth);

            // Centrage de la tortue

            TurtleClear();

            // On règle la taille de décallage du tab dans le code

            SendMessage(CodeTextBox.Handle, EM_SETTABSTOPS, 1, [15]);
        }


        #region Private functions

        private void LoadCode(int index)
        {
            var code = GetStringFromResource($"Sample{index}.mog");
            CodeTextBox.Text = code ?? string.Empty;
        }

        private string? GetStringFromResource(string resource)
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"WinFormsMogwai.CodeExemple.{resource}");

                if (stream != null)
                {
                    var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion
        

        #region UI

        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            // On envoie le code à MOGWAI

            ExecuteButton.Enabled = false;
            CodeTextBox.Enabled = false;

            var r = await _MogwaiEngine.RunAsync(CodeTextBox.Text, false, false);

            ConsoleWriteLine("");
            ConsoleWriteLine(r.ToString());

            CodeTextBox.Enabled = true;
            ExecuteButton.Enabled = true;
        }

        private void HaltButton_Click(object sender, EventArgs e)
        {
            OutputTextBox.StopInputMode();
            _MogwaiEngine.Halt();
        }

        private void SamplesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCode(SamplesComboBox.SelectedIndex + 1);
        }

        private void DrawTurtlePictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (currentImage != null)
            {
                e.Graphics.DrawImage(currentImage, new Point(0, 0));
            }

            if (turtleIsVisible) DrawTurtle(e.Graphics, false);
        }
        
        private void FormMain_SizeChanged(object sender, EventArgs e)
        {
            TurtleClear();
            DrawTurtlePictureBox.Invalidate();
        }

        #endregion


        #region Turtle-related functions

        private double DegToRad(double angle)
        {
            return angle * Math.PI / 180.0;
        }

        private void RefreshGraphAreaIfNeed()
        {
            if (!turtleIsVisible)
            {
                DrawTurtlePictureBox.Invalidate();
            }
        }

        public void ShowTurtle(bool value)
        {
            if (InvokeRequired)
            {
                Invoke(() => { ShowTurtle(value); });

            }
            else
            {
                turtleIsVisible = value;
                DrawTurtlePictureBox.Invalidate();
            }
        }

        private void DrawTurtle(bool invalidate)
        {
            if (InvokeRequired)
            {
                Invoke(() => { DrawTurtle(invalidate); });
            }
            else
            {
                DrawTurtle(turtleX, turtleY, turtleAngle, turtleColor, invalidate);
            }
        }

        private void DrawTurtle(Graphics graphics, bool invalidate)
        {
            if (InvokeRequired)
            {
                Invoke(() => { DrawTurtle(graphics, invalidate); });
            }
            else
            {
                DrawTurtle(graphics, turtleX, turtleY, turtleAngle, turtleColor);
                if (invalidate) RefreshTurtlePictureBox();
            }
        }

        private void DrawTurtle(double x, double y, double angle, Color color, bool invalidate)
        {
            if (turtleIsVisible)
            {
                using (var g = DrawTurtlePictureBox.CreateGraphics())
                {
                    DrawTurtle(g, x, y, angle, color);
                }

                if (invalidate) DrawTurtlePictureBox.Invalidate();
            }
        }

        private void DrawTurtle(Graphics graphics, double x, double y, double angle, Color color)
        {
            angle = 180 - angle;

            double r = 40.0;
            double xd = (double)x;
            double yd = (double)y;
            double a1 = DegToRad(angle);
            double a2 = DegToRad(angle + 135);
            double a3 = DegToRad(angle + 225);

            Point p0 = new Point((int)x, (int)y);
            Point p1 = new Point((int)(xd + r * Math.Sin(a1)), (int)(yd + r * Math.Cos(a1)));
            Point p2 = new Point((int)(xd + r * Math.Sin(a2)), (int)(yd + r * Math.Cos(a2)));
            Point p3 = new Point((int)(xd + r * Math.Sin(a3)), (int)(yd + r * Math.Cos(a3)));

            Pen pen = new Pen(color, 2F);

            graphics.DrawLine(pen, p1, p2);
            graphics.DrawLine(pen, p2, p0);
            graphics.DrawLine(pen, p0, p3);
            graphics.DrawLine(pen, p3, p1);
        }

        private void TurtleClear()
        {
            if (InvokeRequired)
            {
                Invoke(TurtleClear);
            }
            else
            {
                turtleX = DrawTurtlePictureBox.Width / 2;
                turtleY = DrawTurtlePictureBox.Height / 2;
                turtleAngle = 0;

                currentImage = new Bitmap(DrawTurtlePictureBox.Width, DrawTurtlePictureBox.Height);

                DrawTurtlePictureBox.Invalidate();
            }
        }

        public void TurtleForward(int distance)
        {
            if (InvokeRequired)
            {
                Invoke(() => { TurtleForward(distance); });
            }
            else
            {
                double a = DegToRad(180 - turtleAngle);
                double x = turtleX + distance * Math.Sin(a);
                double y = turtleY + distance * Math.Cos(a);

                if (penIsDown)
                {
                    TurtleDrawLine(turtleX, turtleY, x, y);
                }

                turtleX = x;
                turtleY = y;

                if (turtleIsVisible)
                {
                    DrawTurtlePictureBox.Invalidate();
                }
            }
        }

        public void TurtleRotate(double angle)
        {
            if (InvokeRequired)
            {
                Invoke((Action)delegate { TurtleRotate(angle); });
                return;
            }

            turtleAngle += angle;
            if (turtleAngle > 360) { turtleAngle %= 360; }

            if (turtleIsVisible)
            {
                DrawTurtlePictureBox.Invalidate();
            }
        }

        public void TurtlePenDown(bool value)
        {
            penIsDown = value;
        }

        public void RefreshTurtlePictureBox()
        {
            DrawTurtlePictureBox.Invalidate();
        }

        public void TurtleSetColor(double alpha, double red, double green, double blue)
        {
            if (alpha > -1 && alpha < 256 &&
                red > -1 && red < 256 &&
                green > -1 && green < 256 &&
                blue > -1 && blue < 256)
            {
                currentColor = Color.FromArgb((int)alpha, (int)red, (int)green, (int)blue);
                currentPen = new Pen(currentColor, currentWidth);
            }
        }

        public void TurtleDrawPlot(double x, double y)
        {
            if (currentImage != null)
            {
                if (InvokeRequired)
                {
                    Invoke(() => { TurtleDrawPlot(x, y); });
                }
                else
                {
                    using (var g = Graphics.FromImage(currentImage))
                    {
                        g.DrawLine(currentPen, (float)x, (float)y, (float)x, (float)y);
                    }
                }
            }
        }

        public void TurtleDrawLine(double x1, double y1, double x2, double y2)
        {
            if (currentImage != null)
            {
                if (InvokeRequired)
                {
                    Invoke((Action)delegate { TurtleDrawLine(x1, y1, x2, y2); });
                }
                else
                {
                    using (var g = Graphics.FromImage(currentImage))
                    {
                        g.DrawLine(currentPen, (float)x1, (float)y1, (float)x2, (float)y2);
                    }
                }
            }
        }

        public void TurtleDrawRect(double x, double y, double w, double h)
        {
            if (currentImage != null)
            {
                if (InvokeRequired)
                {
                    Invoke(() => { TurtleDrawRect(x, y, w, h); });
                }
                else
                {
                    using (var g = Graphics.FromImage(currentImage))
                    {
                        g.DrawRectangle(currentPen, (float)x, (float)y, (float)w, (float)h);
                    }
                }
            }
        }

        public void TurtleDrawEllipse(double x, double y, double w, double h)
        {
            if (currentImage != null)
            {
                if (InvokeRequired)
                {
                    Invoke((Action)delegate { TurtleDrawEllipse(x, y, w, h); });
                }
                else
                {
                    using (var g = Graphics.FromImage(currentImage))
                    {
                        g.DrawEllipse(currentPen, (float)x, (float)y, (float)w, (float)h);
                    }
                }
            }
        }

        public void TurtleDrawCircle(double x, double y, double r)
        {
            if (currentImage != null)
            {
                if (InvokeRequired)
                {
                    Invoke((Action)delegate { TurtleDrawCircle(x, y, r); });
                }
                else
                {
                    using (var g = Graphics.FromImage(currentImage))
                    {
                        double left = x - r;
                        double top = y - r;
                        double width = r * 2;
                        double height = width;

                        g.DrawEllipse(currentPen, (float)left, (float)top, (float)width, (float)height);
                    }
                }
            }
        }

        #endregion


        #region Functions related to the output console

        private void ConsoleWrite(string? text)
        {
            if (InvokeRequired)
            {
                Invoke(() => { ConsoleWrite(text); });
            }
            else
            {
                OutputTextBox.Write(text ?? "");
            }
        }

        private void ConsoleWriteLine(string? text)
        {
            if (InvokeRequired)
            {
                Invoke(() => { ConsoleWriteLine(text); });
            }
            else
            {
                OutputTextBox.WriteLine(text ?? "");
            }
        }

        #endregion


        #region Functions of the MOGWAI delegate

        // All these functions are the link between the engine and its host (this window).
        // They are all called from a thread other than the UI thread.

        public async Task ConsoleClear(MOGEngine sender)
        {
            // MOGWAI cls function
            // We clear the output console

            await Task.CompletedTask;
            Invoke(() => { OutputTextBox.Clear(); });
        }

        public async Task<(int x, int y)> ConsoleGetCursorPosition(MOGEngine sender)
        {
            // MOGWAI console.cursor function
            // Not implemented in this example.

            await Task.CompletedTask;
            return (0, 0);
        }

        public async Task ConsoleHide(MOGEngine sender)
        {
            // MOGWAI console.hide function
            // Not implemented in this example.

            await Task.CompletedTask;
        }

        public async Task<int> ConsoleInputKey(MOGEngine sender)
        {
            // MOGWAI console.inputkey function
            // Not implemented in this example.

            await Task.CompletedTask;
            return 0;
        }

        public async Task<string> ConsoleReadLine(MOGEngine sender)
        {
            // MOGWAI input function
            // Get string from console

            return await OutputTextBox.Input("");          
        }

        public async Task<string> ConsolePrompt(MOGEngine sender, string prompt)
        {
            // MOGWAI prompt function
            // Get string from console with prompt

            return await OutputTextBox.Input(prompt);
        }

        public async Task ConsoleSetBackgroundColor(MOGEngine sender, string color)
        {
            // MOGWAI console.background function 
            // Not implemented in this example.

            await Task.CompletedTask;
        }

        public async Task ConsoleSetCursorPosition(MOGEngine sender, int x, int y)
        {
            // MOGWAI console.cursor function
            // Not implemented in this example.

            await Task.CompletedTask;
        }

        public async Task ConsoleSetForegroundColor(MOGEngine sender, string color)
        {
            // MOGWAI console.foreground function
            // Not implemented in this example.

            await Task.CompletedTask;
        }

        public async Task ConsoleShow(MOGEngine sender)
        {
            // MOGWAI console.show function
            // Not implemented in this example.

            await Task.CompletedTask;
        }

        public async Task ConsoleWrite(MOGEngine sender, string text)
        {
            // MOGAI print or ?? function

            await Task.CompletedTask;
            ConsoleWrite(text);
        }

        public async Task ConsoleWriteLine(MOGEngine sender, string text)
        {
            // MOGWAI println or ? function

            await Task.CompletedTask;
            ConsoleWriteLine(text);
        }

        public async Task CurrentEval(MOGEngine sender, MOGObject item)
        {
            // Called when MOGWAI evaluate current instruction

            await Task.CompletedTask;
        }

        public async Task DebugClear(MOGEngine sender)
        {
            // MOGWAI debug.clear function
            // Not implemented in this example.

            await Task.CompletedTask;
        }

        public async Task DebugMessage(MOGEngine sender, string message)
        {
            // MOGWAI debug.write function
            // Not implemented in this example.

            await Task.CompletedTask;
        }

        public async Task DebugModeChanged(MOGEngine sender)
        {
            // Called when debug mode change

            await Task.CompletedTask;
        }

        public async Task<EvalResult> EvalExternWord(MOGEngine sender, string word)
        {
            // Called when MOGWAI encounters a keyword it doesn't know.
            // In this case, it asks the host if it can respond.

            switch (word)
            {
                case "clg":
                    return await ClgExtension(sender, word);

                case "refresh":
                    return await RefreshExtension(sender, word);

                case "turtle.penDown":
                    return await PenDownExtension(sender, word);

                case "turtle.penUp":
                    return await PenUpExtension(sender, word);

                case "turtle.show":
                    return await ShowTurtleExtension(sender, word);

                case "turtle.hide":
                    return await HideTurtleExtension(sender, word);

                case "turtle.move":
                    return await MoveExtension(sender, word);

                case "turtle.turn":
                    return await TurnExtension(sender, word);
            }

            return new EvalResult(MOGEngine.UnknownWordError);
        }

        public async Task EvalLib(MOGEngine sender, string filename, string code)
        {
            // Called when a library is imported

            await Task.CompletedTask;
        }

        public async Task EvalProgram(MOGEngine sender, string filename, string code)
        {
            // Called when a program is started

            await Task.CompletedTask;
        }

        public async Task EventDidConsume(MOGEngine sender, string eventReference)
        {
            // Called when an event is consumed

            await Task.CompletedTask;
        }

        public async Task<string[]> ExternalKeywords(MOGEngine sender)
        {
            // Return all additionnals keywords powered by the host

            await Task.CompletedTask;

            return [
                "clg",
                "refresh",
                "turtle.penDown",
                "turtle.penUp",
                "turtle.show",
                "turtle.hide",
                "turtle.move",
                "turtle.turn"
                ];
        }

        public async Task<string?> HelpForExternalWord(MOGEngine sender, string word)
        {
            // MOGWAI help function

            await Task.CompletedTask;
            return null;
        }

        public async Task IsPaused(MOGEngine sender)
        {
            // Called when runtime is paused

            await Task.CompletedTask;
        }

        public async Task<bool> IsReservedWord(MOGEngine sender, string word)
        {
            // Return all new reserved keywords by the host.
            // This keywords can not used as variable name for exemple.

            var v = await ExternalKeywords(sender);
            return v.Contains(word);
        }

        public async Task IsResumed(MOGEngine sender)
        {
            // Called when runtime is resumed

            await Task.CompletedTask;
        }

        public async Task ParseEnded(MOGEngine sender, MOGError error)
        {
            // Called when parsing step ends

            await Task.CompletedTask;
        }

        public async Task ParseStart(MOGEngine sender)
        {
            // Called when parsing step starts

            await Task.CompletedTask;
        }

        public async Task ProgramEnded(MOGEngine sender, EvalResult result)
        {
            // Called when program ends
            // result parameter contains status (ok or error)

            await Task.CompletedTask;

            Invoke(() =>
            {
                HaltButton.Enabled = false;
            });
        }

        public async Task ProgramStart(MOGEngine sender, string code)
        {
            // Called when program starts

            await Task.CompletedTask;

            Invoke(() =>
            {
                HaltButton.Enabled = true;
            });
        }

        public async Task<List<MOGSkill>?> RequestSkills(MOGEngine sender)
        {
            // Return all host's skills

            await Task.CompletedTask;
            return [];
        }

        public async Task RuntimeEvent(MOGEngine sender, string eventName, MOGObject? data)
        {
            // Called when host send event to MOGWAI runtime

            await Task.CompletedTask;
        }

        public async Task SocketServerDidStart(MOGEngine sender, IPAddress address, int port)
        {
            // Called when debug server starts

            await Task.CompletedTask;
        }

        public async Task SocketServerDidStop(MOGEngine sender)
        {
            // Called when debug server stops

            await Task.CompletedTask;
        }

        public async Task StudioDidConnect(MOGEngine sender)
        {
            // Called when MOGWAI STUDIO connects

            await Task.CompletedTask;
        }

        public async Task StudioDidDisconnect(MOGEngine sender)
        {
            // Called when MOGWAI STUDIO disconnects

            await Task.CompletedTask;
        }

        public async Task Trace(MOGEngine sender)
        {
            // MOGWAI debug.tron function

            await Task.CompletedTask;
        }

        #endregion


        #region MOGWAI additional functions

        private async Task<EvalResult> ClgExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;
            TurtleClear();
            return new EvalResult(MOGEngine.NoError);
        }

        private async Task<EvalResult> RefreshExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;
            RefreshGraphAreaIfNeed();
            return new EvalResult(MOGEngine.NoError);
        }

        private async Task<EvalResult> PenDownExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;
            TurtlePenDown(true);
            return new EvalResult(MOGEngine.NoError);
        }

        private async Task<EvalResult> PenUpExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;
            TurtlePenDown(false);
            return new EvalResult(MOGEngine.NoError);
        }

        private async Task<EvalResult> ShowTurtleExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;
            ShowTurtle(true);
            return new EvalResult(MOGEngine.NoError);
        }

        private async Task<EvalResult> HideTurtleExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;
            ShowTurtle(false);
            return new EvalResult(MOGEngine.NoError);
        }

        private async Task<EvalResult> MoveExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;

            var sign = engine.StackSign(1);
            if (sign.Count == 0) return new EvalResult(MOGEngine.TooFewArgumentsError);
            if (sign[0] != MOGEngine.InternalTypeNumber) return new EvalResult(MOGEngine.BadArgumentTypeError);

            var n0 = engine.StackPop() as MOGNumber;
            TurtleForward(n0!.IntValue);

            return new EvalResult(MOGEngine.NoError);
        }

        private async Task<EvalResult> TurnExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;

            var sign = engine.StackSign(1);
            if (sign.Count == 0) return new EvalResult(MOGEngine.TooFewArgumentsError);
            if (sign[0] != MOGEngine.InternalTypeNumber) return new EvalResult(MOGEngine.BadArgumentTypeError);

            var n0 = engine.StackPop() as MOGNumber;
            TurtleRotate(n0!.Value);

            return new EvalResult(MOGEngine.NoError);
        }

        #endregion
    }
}
