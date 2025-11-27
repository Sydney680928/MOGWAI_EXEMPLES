using MOGWAI;
using MOGWAI.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MOGWAI_CLI
{
    public class EngineDelegate : MOGEngineDelegate
    {
        private bool _RequestStop;

        private MOGEngine _Engine { get; set; }

        public EngineDelegate(MOGEngine engine)
        {
            _Engine = engine;
        }

        #region ENGINE MANAGER

        public async Task StartEngine(string address, int port)
        {
            _Engine.Delegate = this;

            _Engine.StartDatagramServer(port);

            await _Engine.StartSocketServerAsync(address);

            while (!_RequestStop) await Task.Delay(500);
        }

        public void StopEngine() => _RequestStop = true;

        public async Task<EvalResult?> RunProgram(string path)
        {
            EvalResult? result;

            try
            {
                // We load the binary of the file passed as parameter

                byte[] bytes = File.ReadAllBytes(path);

                // We try to process the binary as a MOGX

                var p = MOGProgram.FromBinary(_Engine, bytes);

                if (p != null)
                {
                    // It is indeed a MOGX
                    // We execute it

                    result = await _Engine.RunAsync(p, false);
                }
                else
                {
                    // It must be source code (UTF8 text)

                    string code = Encoding.UTF8.GetString(bytes);

                    // We execute the code

                    result = await _Engine.RunAsync(code, false, false);
                }

                // We return the execution result

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Program file load error !");
            }

            return null;
        }

        #endregion

        #region ENGINE DELEGATE

        async Task MOGEngineDelegate.ConsoleClear(MOGEngine sender)
        {
            // MOGWAI console.clear function
            await Task.CompletedTask;

            Console.Clear();
        }

        async Task MOGEngineDelegate.ConsoleShow(MOGEngine sender)
        {
            // MOGWAI console.show function
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.ConsoleHide(MOGEngine sender)
        {
            // MOGWAI console.hide function
            await Task.CompletedTask;
        }

        async Task<string> MOGEngineDelegate.ConsoleReadLine(MOGEngine sender)
        {
            await Task.CompletedTask;

            return Console.ReadLine() ?? "";
        }

        async Task<string> MOGEngineDelegate.ConsolePrompt(MOGEngine sender, string prompt)
        {
            await Task.CompletedTask;

            Console.Write(prompt);
            return Console.ReadLine() ?? "";
        }

        async Task<int> MOGEngineDelegate.ConsoleInputKey(MOGEngine sender)
        {
            // MOGWAI console.inputkey function
            await Task.CompletedTask;

            if (Console.KeyAvailable) return Console.ReadKey(true).KeyChar;
            return 0;
        }

        async Task MOGEngineDelegate.ConsoleSetBackgroundColor(MOGEngine sender, string color)
        {
            await Task.CompletedTask;

            var c = Console.BackgroundColor;

            switch (color)
            {
                case "BLACK":
                    c = ConsoleColor.Black;
                    break;

                case "WHITE":
                    c = ConsoleColor.White;
                    break;

                case "BLUE":
                    c = ConsoleColor.Blue;
                    break;

                case "RED":
                    c = ConsoleColor.Red;
                    break;

                case "YELLOW":
                    c = ConsoleColor.Yellow;
                    break;
            }

            Console.BackgroundColor = c;
        }

        async Task MOGEngineDelegate.ConsoleSetCursorPosition(MOGEngine sender, int x, int y)
        {
            await Task.CompletedTask;

            try
            {
                Console.SetCursorPosition(x, y);
            }
            catch (Exception ex)
            {

            }
        }

        async Task<(int x, int y)> MOGEngineDelegate.ConsoleGetCursorPosition(MOGEngine sender)
        {
            await Task.CompletedTask;

            return Console.GetCursorPosition();
        }

        async Task MOGEngineDelegate.ConsoleSetForegroundColor(MOGEngine sender, string color)
        {
            await Task.CompletedTask;

            var c = Console.ForegroundColor;

            switch (color)
            {
                case "BLACK":
                    c = ConsoleColor.Black;
                    break;

                case "WHITE":
                    c = ConsoleColor.White;
                    break;

                case "BLUE":
                    c = ConsoleColor.Blue;
                    break;

                case "RED":
                    c = ConsoleColor.Red;
                    break;

                case "YELLOW":
                    c = ConsoleColor.Yellow;
                    break;

                case "GREEN":
                    c = ConsoleColor.Green;
                    break;
            }

            Console.ForegroundColor = c;
        }

        async Task MOGEngineDelegate.ConsoleWrite(MOGEngine sender, string text)
        {
            await Task.CompletedTask;

            Console.Write(text);
        }

        async Task MOGEngineDelegate.ConsoleWriteLine(MOGEngine sender, string text)
        {
            await Task.CompletedTask;

            Console.WriteLine(text);
        }

        async Task MOGEngineDelegate.CurrentEval(MOGEngine sender, MOGObject item)
        {
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.Trace(MOGEngine sender)
        {
            // MOGWAI debug.tron function
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.DebugModeChanged(MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        async Task<EvalResult> MOGEngineDelegate.EvalExternWord(MOGEngine sender, string word)
        {
            await Task.CompletedTask;

            if (word == "?timers")
            {
                return await ShowTimersExtension(sender, word);
            }

            return new EvalResult(MOGEngine.UnknownWordError);
        }

        async Task<string[]> MOGEngineDelegate.ExternalKeywords(MOGEngine sender)
        {
            // Return all additional keywords powered by the host
            await Task.CompletedTask;

            return new string[]
            {
                "?timers"
            };
        }

        async Task MOGEngineDelegate.ProgramEnded(MOGEngine sender, EvalResult result)
        {
            // Called when program ends
            // result parameter contains status (ok or error)
            await Task.CompletedTask;

            if (sender == _Engine)
            {
                Console.WriteLine();
                Console.WriteLine(result.ToString());
                Console.Title = "MOGWAI CLI - READY";
            }
        }

        async Task MOGEngineDelegate.ProgramStart(MOGEngine sender, string code)
        {
            // Called when program starts
            await Task.CompletedTask;

            if (sender == _Engine)
            {
                Console.Title = "MOGWAI CLI - BUSY";
            }
        }

        async Task MOGEngineDelegate.DebugMessage(MOGEngine sender, string message)
        {
            await Task.CompletedTask;

            Debug.WriteLine(message);
        }

        async Task MOGEngineDelegate.DebugClear(MOGWAI.MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.EvalLib(MOGEngine sender, string filename, string code)
        {
            // Called when a library is imported
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.EvalProgram(MOGEngine sender, string filename, string code)
        {
            // Called when a program is started
            await Task.CompletedTask;
        }

        async Task<string?> MOGEngineDelegate.HelpForExternalWord(MOGEngine sender, string word)
        {
            // MOGWAI help function
            await Task.CompletedTask;

            if (word == "?timers")
            {
                return "?timers\nAffiche l'état des timers";
            }

            return null;
        }

        async Task<bool> MOGEngineDelegate.IsReservedWord(MOGEngine sender, string word)
        {
            // Return all new reserved keywords by the host.
            // These keywords cannot be used as variable names for example.
            await Task.CompletedTask;

            return word == "?stack" || word == "?vars" || word == "?s" || word == "?v" || word == "?dump" || word == "?d" || word == "?timers";
        }

        async Task MOGEngineDelegate.RuntimeEvent(MOGEngine sender, string eventName, MOGObject? userInfo)
        {
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.IsPaused(MOGEngine sender)
        {
            // Called when runtime is paused
            await Task.CompletedTask;

            if (sender == _Engine)
            {
                Debug.WriteLine("PAUSED");
            }
        }

        async Task MOGEngineDelegate.IsResumed(MOGEngine sender)
        {
            // Called when runtime is resumed
            await Task.CompletedTask;

            if (sender == _Engine)
            {
                Debug.WriteLine("RESUMED");
            }
        }

        private async Task<EvalResult> ShowTimersExtension(MOGEngine engine, string word)
        {
            await Task.CompletedTask;

            // We retrieve the timers and display them with their state
            // Timer T1     -- true
            // Timer T2     -- false

            foreach (var name in engine.Timers)
            {
                Console.Write($"Timer {name}\t");
                Console.Write("-- ");
                Console.WriteLine(engine.TimerGetState(name) ? "on" : "off");
            }

            return new EvalResult(MOGEngine.NoError);
        }

        async Task MOGEngineDelegate.ParseStart(MOGEngine sender)
        {
            // Called when parsing step starts
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.ParseEnded(MOGEngine sender, MOGError error)
        {
            // Called when parsing step ends
            await Task.CompletedTask;

            if (error != MOGEngine.NoError)
            {
                Console.WriteLine("Parse error !");
                Console.WriteLine("");
            }
        }

        async Task MOGEngineDelegate.SocketServerDidStart(MOGEngine sender, IPAddress address, int port)
        {
            // Called when debug server starts
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.SocketServerDidStop(MOGEngine sender)
        {
            // We switch back to editor mode
            await Task.CompletedTask;

            // We switch back to connection waiting mode

            _RequestStop = true;
        }

        async Task MOGEngineDelegate.EventDidConsume(MOGEngine sender, string eventReference)
        {
            // Called when an event is consumed
            await Task.CompletedTask;
        }

        async Task<List<MOGSkill>?> MOGEngineDelegate.RequestSkills(MOGEngine sender)
        {
            // Return all host's skills
            await Task.CompletedTask;

            return null;
        }

        async Task MOGEngineDelegate.StudioDidConnect(MOGEngine sender)
        {
            // Called when MOGWAI STUDIO connects
            await Task.CompletedTask;
        }

        async Task MOGEngineDelegate.StudioDidDisconnect(MOGEngine sender)
        {
            // Called when MOGWAI STUDIO disconnects
            await Task.CompletedTask;
        }

        #endregion
    }
}