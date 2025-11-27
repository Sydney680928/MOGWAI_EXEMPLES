using Microsoft.Maui.Controls.Shapes;
using MOGWAI;
using MOGWAI.Classes;
using MOGWAI_RUNTIME.Classes;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace MOGWAI_RUNTIME.Pages
{
    public partial class MainPage : ContentPage, MOGEngineDelegate
    {
        private const string SCRIPT_EDITOR_FONT_SIZE = "ScriptEditorFontSize";
        private const string SCRIPT_RUN_FONT_SIZE = "ScriptRunFontSize";
        private const string SCRIPT_RUN_SCREEN_WIDTH = "ScriptRunScreenWidth";
        private const string SCRIPT_RUN_SCREEN_HEIGHT = "ScriptRunScreenHeight";

        private MOGEngine _MOGEngine;
        private bool _DebugMode;
        private string _Filename = "NO NAME";
        private bool _CodeIsSaved;
        private bool _CodeIsModified;

        private string _FullPath => System.IO.Path.Combine(AppGlobal.CodeFolder, _Filename) + ".mog";

        public int RunFontSize
        {
            get => Preferences.Default.Get(SCRIPT_RUN_FONT_SIZE, 8);

            set
            {
                Preferences.Default.Set(SCRIPT_RUN_FONT_SIZE, value);
                OnPropertyChanged(nameof(RunFontSize));
            }
        }

        public int EditorFontSize
        {
            get => Preferences.Default.Get(SCRIPT_EDITOR_FONT_SIZE, 10);

            set => Preferences.Default.Set(SCRIPT_EDITOR_FONT_SIZE, value);
        }

        public int RunScreenWidth
        {
            get => Preferences.Default.Get(SCRIPT_RUN_SCREEN_WIDTH, 60);

            set => Preferences.Default.Set(SCRIPT_RUN_SCREEN_WIDTH, value);
        }

        public int RunScreenHeight
        {
            get => Preferences.Default.Get(SCRIPT_RUN_SCREEN_HEIGHT, 30);

            set => Preferences.Default.Set(SCRIPT_RUN_SCREEN_HEIGHT, value);
        }

        public MainPage()
        {
            InitializeComponent();

            // Create MOGWAI engine

            _MOGEngine = new("MOGWAI RT", true, null, AppGlobal.DataFolder, this);

            // Output initialize

            var htmlSource = new HtmlWebViewSource
            {
                Html = Tools.GetStringFromResource("ConsoleWebView.html")
            };

            OutputDisplay.Source = htmlSource;
        }

        private void ShowCodeEditorScreen()
        {
            _DebugMode = false;
        
            // Auto power off -> ON

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _MOGEngine.StopDatagramServer();
                _MOGEngine.StopSocketServer();

                FlagRunPath.IsVisible = false;
                FlagDebugPath.IsVisible = false;
                FlagErrorPath.IsVisible = false;
                FlagPlugPath.IsVisible = false;
                FlagPausePath.IsVisible = false;

                CodeEditorGrid.IsVisible = true;
                RunGrid.IsVisible = false;
            });

#if ANDROID

            if (Platform.CurrentActivity is MainActivity activity)
            {
                activity.LeaveScreenOff();
            }

#endif

        }

        private async Task ShowRunScreenAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (_DebugMode)
                {
                    FlagDebugPath.IsVisible = true;
                }
                else
                {
                    FlagDebugPath.IsVisible = false;
                }

                FlagRunPath.IsVisible = _MOGEngine.IsRunning;
                FlagErrorPath.IsVisible = false;
                FlagPlugPath.IsVisible = _MOGEngine.IsSocketServerRunning;
                FlagPausePath.IsVisible = _MOGEngine.IsPaused;

                await OutputDisplay.EvaluateJavaScriptAsync($"setSize({RunFontSize});");

                CodeEditorGrid.IsVisible = false;
                RunGrid.IsVisible = true;
            });
        }

        private async Task ConsoleClearScreenAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await OutputDisplay.EvaluateJavaScriptAsync("consoleClearScreen();");
            });
        }

        private async Task SaveAs()
        {
            var r = await DisplayPromptAsync("Save as...", "Please enter the name of the script", "OK", "CANCEL", null, -1, null, _Filename);

            if (!string.IsNullOrEmpty(r))
            {
                var path = System.IO.Path.Combine(AppGlobal.CodeFolder, r);
                if (!path.ToUpper().EndsWith(".mog")) path += ".mog";

                if (File.Exists(path))
                {
                    var r1 = await DisplayAlert("Save as...", $"The script '{r}' already exists.\nDo you want to replace it?", "YES", "NO");
                    if (!r1) return;
                }

                _Filename = r;
                FilenameLabel.Text = _Filename;

                _CodeIsSaved = true;

                await Save();
            }
        }

        private async Task Save()
        {
            if (!_CodeIsSaved)
            {
                await SaveAs();
            }
            else
            {
                try
                {
                    using var writer = new StreamWriter(_FullPath);
                    writer.Write(CodeEditor.Text);
                    writer.Flush();
                    writer.Close();

                    _CodeIsSaved = true;
                    _CodeIsModified = false;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Save Script", $"Unable to save the script !\n\n{ex.Message}", "OK");
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (RunGrid.IsVisible)
            {
                // Si un programme tourne on l'arrête

                if (_MOGEngine.IsRunning)
                {
                    _MOGEngine.Halt();
                }

                // On repasse en mode édition si on est en

                ShowCodeEditorScreen();

                return true;
            }

            return base.OnBackButtonPressed();
        }

        private async void ContentView_Loaded(object sender, EventArgs e)
        {
            if (!AppGlobal.CreateDataStructure())
            {
                await DisplayAlert("MOGWAI RUNTIME", "Unable to create the structure required for the application !", "OK");
                return;
            }

            NavigationPage.SetHasNavigationBar(this, false);

            FilenameLabel.Text = _Filename;

            // On paramètre la taille de la police de l'éditeur

            CodeEditor.FontSize = EditorFontSize;

            // Par défaut on affiche l'éditeur de code sauf si du code tourne

            if (_MOGEngine.IsRunning || _MOGEngine.IsPaused)
            {
                await ShowRunScreenAsync();
            }
            else
            {
                ShowCodeEditorScreen();
            }

#if WINDOWS

            await DisplayAlert("MOGWAI RUNTIME", "In Windows, to open the [script] and [files] menus you must right-click.", "OK");
#endif
        }

        private async Task<bool> OpenFileAsync()
        {
            if (await CheckIfSaveIsRequested()) return false;

            var f = await SelectScripFile("Open Script");

            if (f != null)
            {
                try
                {
                    var fullpath = System.IO.Path.Combine(AppGlobal.CodeFolder, f) + ".mog";
                    using var reader = new StreamReader(fullpath);
                    var code = reader.ReadToEnd();
                    reader.Close();

                    _Filename = f;
                    _CodeIsSaved = true;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CodeEditor.Text = code;
                        _CodeIsModified = false;
                        FilenameLabel.Text = _Filename;
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Open Script", $"Unable to load this script !\n\n{ex.Message}", "OK");
                }
            }

            return false;
        }

        private async Task<bool> CheckIfSaveIsRequested()
        {
            if (_CodeIsModified)
            {
                var r = await DisplayAlert("Script", "The code has been modified and has not been saved.\n\nDo you still want to continue?", "CONTINUE (!)", "CANCEL");
                return !r;
            }

            return false;
        }

        private async Task<string?> SelectScripFile(string title)
        {
            // On liste les fichiers .mog du dossier code des scripts

            var files = Directory.GetFiles(AppGlobal.CodeFolder, "*.mog");

            // On ne garde que les noms de fichiers sans le chemin complet

            var items = new List<string>();
            foreach (var file in files)
            {
                items.Add(System.IO.Path.GetFileNameWithoutExtension(file));
            }

            var d = new BasicSelectorPage(title, items);
            await Navigation.PushModalAsync(d);

            while (!d.Done)
            {
                await Task.Delay(500);
            }

            return d.SelectedItem;
        }

        private async Task<string?> SelectDataFile(string title)
        {
            // On liste les fichiers du dossier data des scripts

            var files = Directory.GetFiles(AppGlobal.DataFolder);

            // On ne garde que les noms de fichiers sans le chemin complet

            var items = new List<string>();
            foreach (var file in files)
            {
                items.Add(System.IO.Path.GetFileName(file));
            }

            var d = new BasicSelectorPage(title, items);
            await Navigation.PushModalAsync(d);

            while (!d.Done)
            {
                await Task.Delay(500);
            }

            return d.SelectedItem;
        }

        private async Task<string?> SelectFile(string title)
        {
            var options = new PickOptions
            {
                PickerTitle = title
            };

            var result = await FilePicker.Default.PickAsync(options);
            return result?.FullPath ?? null;
        }

        private async Task ConsoleWriteAsync(string message)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var lines = message
                    .Replace("\r", "")
                    .Replace(" ", @"\xa0")
                    .Split("\n");

                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        await OutputDisplay.EvaluateJavaScriptAsync($"consoleWriteLine(`{lines[i]}`);");
                    }

                    await OutputDisplay.EvaluateJavaScriptAsync($"consoleWrite(`{lines[lines.Length - 1]}`);");
                }
            });
        }

        private async Task ConsoleWriteLineAsync(string message)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var lines = message
                    .Replace("\r", "")
                    .Replace(" ", @"\xa0")
                    .Split("\n");

                    foreach (var line in lines)
                    {
                        await OutputDisplay.EvaluateJavaScriptAsync($"consoleWriteLine(`{line}`);");
                    }
                }
                else
                {
                    await OutputDisplay.EvaluateJavaScriptAsync("consoleWriteLine(``);");
                }
            });
        }

        private async Task ConsoleStartInputMode()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {             
                await OutputDisplay.EvaluateJavaScriptAsync("startInputMode();");
            });
        }

        private async Task ConsoleExitInputMode()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await OutputDisplay.EvaluateJavaScriptAsync("exitInputMode();");
            });
        }

        private async Task<string> ConsolePrompt(string prompt)
        {
            await ConsoleWriteAsync(prompt);
            return await ConsoleInput();
        }

        private async Task<string> ConsoleInput()
        {
            return await MainThread.InvokeOnMainThreadAsync<string>(async () =>
            {
                OutputDisplay.Focus();

                await ConsoleStartInputMode();

                while (true)
                {
                    var r = await OutputDisplay.EvaluateJavaScriptAsync("inputModeInProgress");

                    if (r != null && r == "true")
                    {
                        break;
                    }
                }

                while (true)
                {
                    var r = await OutputDisplay.EvaluateJavaScriptAsync("inputModeInProgress");

                    if (r != null && r == "false")
                    {
                        // Saisie terminée.
                        // On sort

                        break;
                    }

                    await Task.Delay(500);
                }

                // On récupère la valeur saisie

                var v = await OutputDisplay.EvaluateJavaScriptAsync("lastInputValue");
                return v ?? "";
            });
        }

        private async void NewFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            if (!await CheckIfSaveIsRequested())
            {
                _Filename = "NO NAME";
                _CodeIsSaved = false;
                CodeEditor.Text = string.Empty;
                _CodeIsModified = false;
                FilenameLabel.Text = _Filename;
                CodeEditor.Focus();
            }
        }

        private async void OpenFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            await OpenFileAsync();
        }

        private async void SaveFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            await Save();
        }

        private async void SaveAsFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            await SaveAs();
        }

        private async void RenameFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectScripFile("Rename Script");

            if (f != null)
            {
                if (f == _Filename)
                {
                    await DisplayAlert("Rename Script", "You cannot rename the script that is currently being edited!", "OK");
                    return;
                }

                var r = await DisplayPromptAsync("Renommer Script", $"What new name do you want to give the script '{f}' ?", "RENAME", "CANCEL", null, -1, null, f);

                if (!string.IsNullOrEmpty(r))
                {
                    try
                    {
                        var oldName = System.IO.Path.Combine(AppGlobal.CodeFolder, f) + ".mog";
                        var newName = System.IO.Path.Combine(AppGlobal.CodeFolder, r) + ".mog";

                        File.Move(oldName, newName);

                        await DisplayAlert("Rename Script", "Script renamed.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Rename Script", $"Unable to rename this script!\n\n{ex.Message}", "OK");
                    }
                }
            }
        }

        private async void DeleteFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectScripFile("Delete Script");

            if (f != null)
            {
                if (f == _Filename)
                {
                    await DisplayAlert("Delete Script", "You cannot delete the script that is currently being edited!", "OK");
                    return;
                }

                var r = await DisplayAlert("Delete Script", $"Are you sure you want to delete the script '{f}'?", "DELETE", "CANCEL");

                if (r)
                {
                    var path = System.IO.Path.Combine(AppGlobal.CodeFolder, f) + ".mog";

                    try
                    {
                        File.Delete(path);
                        await DisplayAlert("Delete Script", "Script deleted.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Delete Script", $"Unable to delete this script!\n\n{ex.Message}", "OK");
                    }
                }
            }
        }

        private async void ShareFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            if (await CheckIfSaveIsRequested()) return;

            var f = await SelectScripFile("Share Script");

            if (f != null)
            {
                var path = System.IO.Path.Combine(AppGlobal.CodeFolder, f) + ".mog";

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Share le script {f}",
                    File = new ShareFile(path)
                });
            }
        }

        private async void ImportFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            if (await CheckIfSaveIsRequested()) return;

            var f = await SelectFile("Import Script");

            if (f != null)
            {
                // le fichier doit se terminer par .mog

                if (!System.IO.Path.GetFileName(f).ToUpper().EndsWith(".MOG"))
                {
                    await DisplayAlert("Import Script", "You must select a valid script (.mog)!", "OK");
                    return;
                }

                var filename = System.IO.Path.GetFileName(f);
                var destination = System.IO.Path.Combine(AppGlobal.CodeFolder, filename);

                if (File.Exists(destination))
                {
                    var r = await DisplayAlert("Importer Script", $"This script already exists. Do you want to replace it?", "REPLACE", "CANCEL");
                    if (!r) return;
                }

                try
                {
                    File.Copy(f, destination, true);
                    await DisplayAlert("Import Script", "Script importé.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Import Script", $"Unable to import this script!\n\n{ex.Message}", "OK");
                }
            }
        }

        private async void ShareDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectDataFile("Share File");

            if (f != null)
            {
                var path = System.IO.Path.Combine(AppGlobal.DataFolder, f);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Share the file {f}",
                    File = new ShareFile(path)
                });
            }
        }

        private async void ImportDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectFile("Import File");

            if (f != null)
            {
                var filename = System.IO.Path.GetFileName(f);
                var destination = System.IO.Path.Combine(AppGlobal.DataFolder, filename);

                if (File.Exists(destination))
                {
                    var r = await DisplayAlert("Import File", $"This file already exists. Do you want to replace it?", "REPLACE", "CANCEL");
                    if (!r) return;
                }

                try
                {
                    File.Copy(f, destination, true);
                    await DisplayAlert("Import File", "File imported.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Import File", $"Unable to import this file!\n\n{ex.Message}", "OK");
                }
            }
        }

        private async void RenameDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectDataFile("Rename File");

            if (f != null)
            {
                var r = await DisplayPromptAsync("Rename File", $"What new name do you want to give the file '{f}'?", "RENAME", "CANCEL", null, -1, null, f);

                if (!string.IsNullOrEmpty(r))
                {
                    try
                    {
                        var oldName = System.IO.Path.Combine(AppGlobal.DataFolder, f);
                        var newName = System.IO.Path.Combine(AppGlobal.DataFolder, r);

                        File.Move(oldName, newName);

                        await DisplayAlert("Rename File", "File renamed.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Rename File", $"Unable to rename this file!\n\n{ex.Message}", "OK");
                    }
                }
            }
        }

        private async void DeleteDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectDataFile("Delete File");

            if (f != null)
            {
                var r = await DisplayAlert("Delete File", $"Are you sure you want to delete the file '{f}'?", "DELETE", "CANCEL");

                if (r)
                {
                    var path = System.IO.Path.Combine(AppGlobal.DataFolder, f);

                    try
                    {
                        File.Delete(path);
                        await DisplayAlert("Delete File", "File deleted.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Delete File", $"Unable to delete this file!\n\n{ex.Message}", "OK");
                    }
                }
            }
        }

        private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _CodeIsModified = true;
        }

        private async void RunTapGesture_Tapped(object sender, TappedEventArgs e)
        {
            if (await CheckIfSaveIsRequested()) return;

            _DebugMode = false;

            await ShowRunScreenAsync();

            _ = _MOGEngine.RunAsync(CodeEditor.Text, false, false);
        }

        private async void DebugTapGesture_Tapped(object sender, TappedEventArgs e)
        {
            if (await CheckIfSaveIsRequested()) return;

            _DebugMode = true;

            await ShowRunScreenAsync();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _MOGEngine.StartDatagramServer(1968);
                await _MOGEngine.StartSocketServerAsync(IPAddress.Any.ToString());
            });
        }

        private async void HaltTapGesture_Tapped(object sender, TappedEventArgs e)
        {
            await ConsoleExitInputMode();
            _MOGEngine.Halt();
        }

        private async void FontPlusTapGesture_Tapped(object sender, TappedEventArgs e)
        {
            if (CodeEditorGrid.IsVisible)
            {
                if (EditorFontSize < 30)
                {
                    EditorFontSize += 1;
                    CodeEditor.FontSize = EditorFontSize;
                }
            }
            else
            {
                if (RunFontSize < 24)
                {
                    RunFontSize += 1;
                    await OutputDisplay.EvaluateJavaScriptAsync($"setSize({RunFontSize});");
                }
            }
        }

        private async void FontMinusGesture_Tapped(object sender, TappedEventArgs e)
        {
            if (CodeEditorGrid.IsVisible)
            {
                if (EditorFontSize > 10)
                {
                    EditorFontSize -= 1;
                    CodeEditor.FontSize = EditorFontSize;
                }
            }
            else
            {
                if (RunFontSize > 8)
                {
                    RunFontSize -= 1;
                    await OutputDisplay.EvaluateJavaScriptAsync($"setSize({RunFontSize});");
                }
            }
        }

        private void BackGesture_Tapped(object sender, TappedEventArgs e)
        {
            // Si un programme tourne on l'arrête

            if (_MOGEngine.IsRunning)
            {
                _MOGEngine.Halt();
            }

            // On repasse en mode édition

            ShowCodeEditorScreen();
        }

        private async void OutputDisplay_Unfocused(object sender, FocusEventArgs e)
        {
            var r = await OutputDisplay.EvaluateJavaScriptAsync("inputModeInProgress");

            if (r == "true")
            {
                OutputDisplay.Focus();  
            }
        }


        #region MOGWAI DELEGATE

        public async Task ProgramStart(MOGEngine sender, string code)
        {
            // Called when program starts
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CodeEditor.Text = code;
                FlagRunPath.IsVisible = true;
                FlagErrorPath.IsVisible = false;
            });

            // Auto power off -> OFF

            Tools.SuspendAutoPowerOff();
        }

        public async Task ParseStart(MOGEngine sender)
        {
            // Called when parsing step starts
            await Task.CompletedTask;
        }

        public async Task ParseEnded(MOGEngine sender, MOGError error)
        {
            // Called when parsing step ends
            if (!_DebugMode)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (error != MOGEngine.NoError)
                    {
                        await DisplayAlert("PARSE ERROR", error.Message, "OK");
                    }
                    else
                    {
                        await ShowRunScreenAsync();
                    }
                });
            }
        }

        public async Task ProgramEnded(MOGEngine sender, EvalResult result)
        {
            // Called when program ends
            // result parameter contains status (ok or error)
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                FlagRunPath.IsVisible = false;
                FlagErrorPath.IsVisible = result.Error != MOGEngine.NoError;

                await ConsoleWriteLineAsync("");
                await ConsoleWriteLineAsync(result.ToString());
                await ConsoleWriteLineAsync(" ");

                Tools.ResumeAutoPowerOff();
            });
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

        public async Task CurrentEval(MOGEngine sender, MOGObject item)
        {
            await Task.CompletedTask;
        }

        public async Task DebugMessage(MOGEngine sender, string message)
        {
            await Task.CompletedTask;
        }

        public async Task DebugClear(MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        public async Task IsPaused(MOGEngine sender)
        {
            // Called when runtime is paused
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FlagPausePath.IsVisible = true;
                FlagRunPath.IsVisible = false;
            });
        }

        public async Task IsResumed(MOGEngine sender)
        {
            // Called when runtime is resumed
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FlagPausePath.IsVisible = false;
                FlagRunPath.IsVisible = true;
            });
        }

        public async Task DebugModeChanged(MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        public async Task Trace(MOGEngine sender)
        {
            // MOGWAI debug.tron function
            await Task.CompletedTask;
        }

        public async Task EventDidConsume(MOGEngine sender, string eventReference)
        {
            // Called when an event is consumed
            await Task.CompletedTask;
        }

        public async Task<EvalResult> EvalExternWord(MOGEngine sender, string word)
        {
            await Task.CompletedTask;
            return new(MOGEngine.UnknownWordError);
        }

        public async Task<string?> HelpForExternalWord(MOGEngine sender, string word)
        {
            // MOGWAI help function
            await Task.CompletedTask;
            return "Help not found.";
        }

        public async Task<bool> IsReservedWord(MOGEngine sender, string word)
        {
            // Return all new reserved keywords by the host.
            // These keywords cannot be used as variable names for example.
            await Task.CompletedTask;
            return false;
        }

        public async Task RuntimeEvent(MOGEngine sender, string eventName, MOGObject? data)
        {
            // Called when host sends event to MOGWAI runtime
            await Task.CompletedTask;
        }

        public async Task<string[]> ExternalKeywords(MOGEngine sender)
        {
            // Return all additional keywords powered by the host
            await Task.CompletedTask;
            return [];
        }

        public async Task ConsoleShow(MOGEngine sender)
        {
            // MOGWAI console.show function
            await Task.CompletedTask;
        }

        public async Task ConsoleHide(MOGEngine sender)
        {
            // MOGWAI console.hide function
            await Task.CompletedTask;
        }

        public async Task ConsoleClear(MOGEngine sender)
        {
            // MOGWAI console.clear function
            await ConsoleClearScreenAsync();
        }

        public async Task ConsoleWrite(MOGEngine sender, string text)
        {
            await ConsoleWriteAsync(text);
        }

        public async Task ConsoleWriteLine(MOGEngine sender, string text)
        {
            await ConsoleWriteLineAsync(text);
        }

        public async Task<string> ConsoleReadLine(MOGEngine sender)
        {
            return await ConsoleInput();
        }

        public async Task<string> ConsolePrompt(MOGEngine sender, string prompt)
        {
            return await ConsolePrompt(prompt);
        }

        public async Task<int> ConsoleInputKey(MOGEngine sender)
        {
            // MOGWAI console.inputkey function
            await Task.CompletedTask;
            return 0;
        }

        public async Task ConsoleSetCursorPosition(MOGEngine sender, int x, int y)
        {
            await Task.CompletedTask;
        }

        public async Task<(int x, int y)> ConsoleGetCursorPosition(MOGEngine sender)
        {
            await Task.CompletedTask;
            return (0, 0);
        }

        public async Task ConsoleSetForegroundColor(MOGEngine sender, string color)
        {
            await Task.CompletedTask;
        }

        public async Task ConsoleSetBackgroundColor(MOGEngine sender, string color)
        {
            await Task.CompletedTask;
        }

        public async Task<List<MOGSkill>?> RequestSkills(MOGEngine sender)
        {
            // Return all host's skills
            await Task.CompletedTask;
            return [new("MOGWAI RUNTIME")];
        }

        public async Task SocketServerDidStart(MOGEngine sender, IPAddress address, int port)
        {
            // Called when debug server starts
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FlagPlugPath.IsVisible = true;
            });
        }

        public async Task SocketServerDidStop(MOGEngine sender)
        {
            // On repasse en mode édition

            await MainThread.InvokeOnMainThreadAsync(ShowCodeEditorScreen);
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

        #endregion

    }
}
