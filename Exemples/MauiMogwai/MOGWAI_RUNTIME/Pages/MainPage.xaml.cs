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
        private string _Filename = "SANS NOM";
        private bool _CodeIsSaved;
        private bool _CodeIsModified;

        private string _FullPath => System.IO.Path.Combine(AppGlobal.CodeFolder, _Filename) + ".mog";

        public int RunFontSize
        {
            get => Preferences.Default.Get(SCRIPT_RUN_FONT_SIZE, 10);

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

            // Création du moteur MOGWAI

            _MOGEngine = new("MOGWAI RT", true, null, AppGlobal.DataFolder, this);

            // Initialisation de la console de sortie

            var htmlSource = new HtmlWebViewSource
            {
                Html = Tools.GetStringFromResource("ConsoleWebView.html")
            };

            OutputDisplay2.Source = htmlSource;
        }

        private void ShowCodeEditorScreen()
        {
            _DebugMode = false;
        
            // Réactivation de l'auto power off

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

                CodeEditorGrid.IsVisible = false;
                RunGrid.IsVisible = true;
            });
        }

        /*
        private void ConsoleClear()
        {
            MainThread.InvokeOnMainThreadAsync(OutputDisplay.Clear).Wait();          
        }
        */

        private async Task ConsoleClearScreenAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await OutputDisplay2.EvaluateJavaScriptAsync("consoleClearScreen();");
            });
        }

        /*
        private void ConsoleWrite(string text)
        {
            var t = MainThread.InvokeOnMainThreadAsync(() => { OutputDisplay.Write(text); });
            t.Wait();
        }
        */

        /*
        private void ConsoleWriteLine(string text)
        {
            var t = MainThread.InvokeOnMainThreadAsync(() => { OutputDisplay.WriteLine(text); });
            t.Wait();
        }
        */

        private async Task SaveAs()
        {
            var r = await DisplayPromptAsync("Enregistrer sous...", "Veuillez saisir le nom du script", "OK", "ANNULER", null, -1, null, _Filename);

            if (!string.IsNullOrEmpty(r))
            {
                var path = System.IO.Path.Combine(AppGlobal.CodeFolder, r);
                if (!path.ToUpper().EndsWith(".mog")) path += ".mog";

                if (File.Exists(path))
                {
                    var r1 = await DisplayAlert("Enregistrer sous...", $"Le script '{r}' existe déjà.\nVoulez-vous le remplacer", "OUI", "NON");
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
                    await DisplayAlert("Enregistrer Script", $"Impossible d'enregistrer le script !\n\n{ex.Message}", "OK");
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
                await DisplayAlert("MOGWAI RT", "Impossible de créer la structure nécessaire à l'application !", "OK");
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
        }

        private void OutputDisplay2_Loaded(object sender, EventArgs e)
        {

        }

        private async Task<bool> OpenFileAsync()
        {
            if (await CheckIfSaveIsRequested()) return false;

            var f = await SelectScripFile("Ouvrir script");

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

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CodeEditor.Text = code;
                        _CodeIsModified = false;
                        FilenameLabel.Text = _Filename;
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ouvrir Script", $"Impossible de charger ce script !\n\n{ex.Message}", "OK");
                }
            }

            return false;
        }

        private async Task<bool> CheckIfSaveIsRequested()
        {
            if (_CodeIsModified)
            {
                var r = await DisplayAlert("Script", "Le code a été modifié et n'a pas été enregistré.\n\nVoulez-vous continuer tout de même ?", "CONTINUER (!)", "ANNULER");
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
                    var lines = message.Replace("\r", "").Replace(" ", @"\xa0").Split("\n");

                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        await OutputDisplay2.EvaluateJavaScriptAsync($"consoleWriteLine(\"{lines[i]}\");");
                    }

                    await OutputDisplay2.EvaluateJavaScriptAsync($"consoleWrite(\"{lines[lines.Length - 1]}\");");
                }
            });
        }

        private async Task ConsoleWriteLineAsync(string message)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var lines = message.Replace("\r", "").Replace(" ", @"\xa0").Split("\n");

                    foreach (var line in lines)
                    {
                        await OutputDisplay2.EvaluateJavaScriptAsync($"consoleWriteLine(\"{line}\");");
                    }
                }
                else
                {
                    await OutputDisplay2.EvaluateJavaScriptAsync("consoleWriteLine(\"\");");
                }
            });
        }

        private async Task ConsoleStartInputMode()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {             
                await OutputDisplay2.EvaluateJavaScriptAsync("startInputMode();");
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
                OutputDisplay2.Focus();

                await ConsoleStartInputMode();

                while (true)
                {
                    var r = await OutputDisplay2.EvaluateJavaScriptAsync("inputModeInProgress");

                    if (r != null && r == "true")
                    {
                        break;
                    }
                }

                while (true)
                {
                    var r = await OutputDisplay2.EvaluateJavaScriptAsync("inputModeInProgress");

                    if (r != null && r == "false")
                    {
                        // Saisie terminée.
                        // On sort

                        break;
                    }

                    await Task.Delay(500);
                }

                // On récupère la valeur saisie

                var v = await OutputDisplay2.EvaluateJavaScriptAsync("lastInputValue");
                return v ?? "";
            });
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
            var f = await SelectScripFile("Renommer Script");

            if (f != null)
            {
                if (f == _Filename)
                {
                    await DisplayAlert("Renommer Script", "Vous ne pouvez pas renommer le script en cours d'édition !", "OK");
                    return;
                }

                var r = await DisplayPromptAsync("Renommer Script", $"Quel nouveau nom voulez-vous donner au script '{f}' ?", "RENOMMER", "ANNULER", null, -1, null, f);

                if (!string.IsNullOrEmpty(r))
                {
                    try
                    {
                        var oldName = System.IO.Path.Combine(AppGlobal.CodeFolder, f) + ".mog";
                        var newName = System.IO.Path.Combine(AppGlobal.CodeFolder, r) + ".mog";

                        File.Move(oldName, newName);

                        await DisplayAlert("Renommer Script", "Script renommé.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Renommer Script", $"Impossible de renommer ce script !\n\n{ex.Message}", "OK");
                    }
                }
            }
        }

        private async void DeleteFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectScripFile("Supprimer Script");

            if (f != null)
            {
                if (f == _Filename)
                {
                    await DisplayAlert("Supprimer Script", "Vous ne pouvez pas supprimer le script en cours d'édition !", "OK");
                    return;
                }

                var r = await DisplayAlert("Supprimer Script", $"Etes-vous sûr de vouloir supprimer le script '{f}' ?", "SUPPRIMER", "ANNULER");

                if (r)
                {
                    var path = System.IO.Path.Combine(AppGlobal.CodeFolder, f) + ".mog";

                    try
                    {
                        File.Delete(path);
                        await DisplayAlert("Supprimer Script", "Script supprimé.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Supprimer Script", $"Impossible de supprimer ce script !\n\n{ex.Message}", "OK");
                    }
                }
            }
        }

        private async void ShareFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            if (await CheckIfSaveIsRequested()) return;

            var f = await SelectScripFile("Partager Script");

            if (f != null)
            {
                var path = System.IO.Path.Combine(AppGlobal.CodeFolder, f) + ".mog";

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Partager le script {f}",
                    File = new ShareFile(path)
                });
            }
        }

        private async void ImportFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            if (await CheckIfSaveIsRequested()) return;

            var f = await SelectFile("Importer Script");

            if (f != null)
            {
                // le fichier doit se terminer par .mog

                if (!System.IO.Path.GetFileName(f).ToUpper().EndsWith(".MOG"))
                {
                    await DisplayAlert("Importer Script", "Vous devez sélectionner un script valide (.mog) !", "OK");
                    return;
                }

                var filename = System.IO.Path.GetFileName(f);
                var destination = System.IO.Path.Combine(AppGlobal.CodeFolder, filename);

                if (File.Exists(destination))
                {
                    var r = await DisplayAlert("Importer Script", $"Ce script existe déjà. Voulez-vous le remplacer ?", "REMPLACER", "ANNULER");
                    if (!r) return;
                }

                try
                {
                    File.Copy(f, destination, true);
                    await DisplayAlert("Importer Script", "Script importé.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Importer Script", $"Impossible d'importer ce script !\n\n{ex.Message}", "OK");
                }
            }
        }

        private async void ShareDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectDataFile("Partager Fichier");

            if (f != null)
            {
                var path = System.IO.Path.Combine(AppGlobal.DataFolder, f);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Partager le fichier {f}",
                    File = new ShareFile(path)
                });
            }
        }

        private async void ImportDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectFile("Importer Fichier");

            if (f != null)
            {
                var filename = System.IO.Path.GetFileName(f);
                var destination = System.IO.Path.Combine(AppGlobal.DataFolder, filename);

                if (File.Exists(destination))
                {
                    var r = await DisplayAlert("Importer Fichier", $"Ce fichier existe déjà. Voulez-vous le remplacer ?", "REMPLACER", "ANNULER");
                    if (!r) return;
                }

                try
                {
                    File.Copy(f, destination, true);
                    await DisplayAlert("Importer Fichier", "Fichier importé.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Importer Fichier", $"Impossible d'importer ce fichier !\n\n{ex.Message}", "OK");
                }
            }
        }

        private async void RenameDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectDataFile("Renommer Fichier");

            if (f != null)
            {
                var r = await DisplayPromptAsync("Renommer Fichier", $"Quel nouveau nom voulez-vous donner au script '{f}' ?", "RENOMMER", "ANNULER", null, -1, null, f);

                if (!string.IsNullOrEmpty(r))
                {
                    try
                    {
                        var oldName = System.IO.Path.Combine(AppGlobal.DataFolder, f);
                        var newName = System.IO.Path.Combine(AppGlobal.DataFolder, r);

                        File.Move(oldName, newName);

                        await DisplayAlert("Renommer Fichier", "Fichier renommé.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Renommer Fichier", $"Impossible de renommer ce fichier !\n\n{ex.Message}", "OK");
                    }
                }
            }
        }

        private async void DeleteDataFileMenu_ItemTapped(Plugin.ContextMenuContainer.ContextMenuItem item)
        {
            var f = await SelectDataFile("Supprimer Fichier");

            if (f != null)
            {
                var r = await DisplayAlert("Supprimer Fichier", $"Etes-vous sûr de vouloir supprimer le fichier '{f}' ?", "SUPPRIMER", "ANNULER");

                if (r)
                {
                    var path = System.IO.Path.Combine(AppGlobal.DataFolder, f);

                    try
                    {
                        File.Delete(path);
                        await DisplayAlert("Supprimer Fichier", "Fichier supprimé.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Supprimer Fichier", $"Impossible de supprimer ce fichier !\n\n{ex.Message}", "OK");
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

        private void HaltTapGesture_Tapped(object sender, TappedEventArgs e)
        {
            _MOGEngine.Halt();
        }

        private void GridTapGesture_Tapped(object sender, TappedEventArgs e)
        {

        }

        private async void SizeTapGesture_Tapped(object sender, TappedEventArgs e)
        {
            var size = $"{RunScreenWidth};{RunScreenHeight}";

            var r = await DisplayPromptAsync("SORTIE", "Veuillez saisir la taille de la console de sortie", "OK", "CANCEL", "width;height", -1, Keyboard.Text, size);

            if (!string.IsNullOrEmpty(r))
            {
                var items = r.Split(';');

                if (items.Length == 2 && int.TryParse(items[0], null, out var cw) && int.TryParse(items[1], null, out var ch))
                {
                    if (cw > 10 && cw < 100 && ch > 10 && ch < 100)
                    {

                    }
                    else
                    {
                        await DisplayAlert("SORTIE", "Vous devez saisir des valeurs entre 10 et 100 au format 'width;height' !", "OK");
                    }
                }
            }
        }

        private void FontPlusTapGesture_Tapped(object sender, TappedEventArgs e)
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
                if (RunFontSize < 30)
                {
                    RunFontSize += 1;
                }
            }
        }

        private void FontMinusGesture_Tapped(object sender, TappedEventArgs e)
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
                if (RunFontSize > 10)
                {
                    RunFontSize -= 1;
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


        #region MOGWAI DELEGATE

        public async Task ProgramStart(MOGEngine sender, string code)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CodeEditor.Text = code;
                FlagRunPath.IsVisible = true;
                FlagErrorPath.IsVisible = false;
            });

            // désactivation de l'auto power off

            Tools.SuspendAutoPowerOff();
        }

        public async Task ParseStart(MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        public async Task ParseEnded(MOGEngine sender, MOGError error)
        {
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
            await MainThread.InvokeOnMainThreadAsync( async () =>
            {
                FlagRunPath.IsVisible = false;
                FlagErrorPath.IsVisible = result.Error != MOGEngine.NoError;

                // Message de fin de programme sur écran

                await ConsoleWriteLineAsync("");

                if (result.Error != MOGEngine.NoError)
                {
                    await ConsoleWriteLineAsync("Program did stop with error !");
                    await ConsoleWriteLineAsync(result.ToString());
                }
                else
                {
                    await ConsoleWriteLineAsync("Program did stop without error.");
                }

                await ConsoleWriteLineAsync(" ");

                // Réactivation de l'auto power off

                Tools.ResumeAutoPowerOff();
            });
        }

        public async Task EvalLib(MOGEngine sender, string filename, string code)
        {
            await Task.CompletedTask;
        }

        public async Task EvalProgram(MOGEngine sender, string filename, string code)
        {
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
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FlagPausePath.IsVisible = true;
                FlagRunPath.IsVisible = false;
            });
        }

        public async Task IsResumed(MOGEngine sender)
        {
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
            await Task.CompletedTask;
        }

        public async Task EventDidConsume(MOGEngine sender, string eventReference)
        {
            await Task.CompletedTask;
        }

        public async Task<EvalResult> EvalExternWord(MOGEngine sender, string word)
        {
            await Task.CompletedTask;
            return new(MOGEngine.UnknownWordError);
        }

        public async Task<string?> HelpForExternalWord(MOGEngine sender, string word)
        {
            await Task.CompletedTask;
            return "Help not found.";
        }

        public async Task<bool> IsReservedWord(MOGEngine sender, string word)
        {
            await Task.CompletedTask;
            return false;
        }

        public async Task RuntimeEvent(MOGEngine sender, string eventName, MOGObject? data)
        {
            await Task.CompletedTask;
        }

        public async Task<string[]> ExternalKeywords(MOGEngine sender)
        {
            await Task.CompletedTask;
            return [];
        }

        public async Task ConsoleShow(MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        public async Task ConsoleHide(MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        public async Task ConsoleClear(MOGEngine sender)
        {
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
            await ConsoleWriteAsync(prompt);
            return await ConsoleInput();
        }

        public async Task<int> ConsoleInputKey(MOGEngine sender)
        {
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
            await Task.CompletedTask;
            return [new("MYCOMETA")];
        }

        public async Task SocketServerDidStart(MOGEngine sender, IPAddress address, int port)
        {
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
            await Task.CompletedTask;
        }

        public async Task StudioDidDisconnect(MOGEngine sender)
        {
            await Task.CompletedTask;
        }

        #endregion
    }
}
