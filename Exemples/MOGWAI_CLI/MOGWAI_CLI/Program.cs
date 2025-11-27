// See https://aka.ms/new-console-template for more information

using Microsoft.Win32;
using MOGWAI;
using MOGWAI_CLI;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

var engine = new MOGWAI.MOGEngine("MOGWAI CLI", keepAlive: true);
var consoleEngine = new EngineDelegate(engine);
engine.Delegate = consoleEngine;

Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    if (e.SpecialKey == ConsoleSpecialKey.ControlC)
    {
        engine.Halt();
        e.Cancel = true;
    }
}

Dictionary<string, object> ParseProgramArguments(string[] args)
{
    Dictionary<string, object> dico = new();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (arg == "/DEBUG")
        {
            dico.Add("DEBUG", true);
        }
        else if (arg.StartsWith("/PORT="))
        {
            if (int.TryParse(arg.Substring(6), out int port))
            {
                dico.Add("PORT", port);
            }
        }
        else if (arg.StartsWith("/IP="))
        {
            dico.Add("IP", arg.Substring(4));
        }
    }

    return dico;
}

if (args.Length > 0)
{
    // We process the command line arguments
    // /DEBUG as arg0 => Activation of debug mode with the studio
    // /PORT=x => port to use in /DEBUG

    var dico = ParseProgramArguments(args);

    if (dico.ContainsKey("DEBUG"))
    {
        // We retrieve the IP address and port if specified

        string address = dico.ContainsKey("IP") ? dico["IP"] as string ?? "0.0.0.0" : IPAddress.Any.ToString();
        int port = dico.ContainsKey("PORT") ? (int)dico["PORT"] : 1968;

        // We activate the UDP presence server on the network

        engine.StartDatagramServer(port);

        // We activate the TCP/IP server for the studio

        await engine.StartSocketServerAsync(address);
    }
    else if (File.Exists(args[0]))
    {
        Console.Title = "MOGWAI - BUSY";
        Console.Clear();

        var r = await consoleEngine.RunProgram(args[0]);

        Console.Title = "MOGWAI - READY";

        Console.Write("\nPress any key to exit...");
        while (!Console.KeyAvailable) { Thread.Sleep(100); }
    }
}

if (args.Length == 1)
{
    var arg = args[0];

    if (arg.StartsWith("/DEBUG"))
    {
        Console.Title = "MOGWAI CLI - DEBUG";
        Console.Clear();

        while (true)
        {
            await consoleEngine.StartEngine(IPAddress.Any.ToString(), 1968);
        }
    }
    else if (File.Exists(args[0]))
    {
        var t = consoleEngine.RunProgram(args[0]);
        if (t != null) await t;
    }
}
else
{
    Console.Title = "MOGWAI CLI - READY";
    Console.Clear();

    var r = await engine.Run("mogwai.info prompt: get", false, false);
    Console.Clear();

    if (r.Error == MOGEngine.NoError)
    {
        var n0 = engine.StackPop();
        if (n0 != null) Console.WriteLine(n0.ToText());
        Console.WriteLine("");
    }

    StringBuilder inputBuffer = new();
    var next = false;

    while (true)
    {
        inputBuffer.Clear();

        do
        {
            if (inputBuffer.Length == 0)
            {
                Console.Write("> ");
            }
            else
            {
                Console.Write("- ");
            }

            var code = Console.ReadLine() ?? string.Empty;

            if (code.EndsWith("..."))
            {
                inputBuffer.AppendLine(code.Substring(0, code.Length - 3));
                next = true;
            }
            else
            {
                inputBuffer.AppendLine(code);
                next = false;
            }
        } while (next);

        var c = inputBuffer.ToString();

        if (!string.IsNullOrEmpty(c))
        {
            await engine.RunAsync(c, false, false);
        }
        else
        {
            break;
        }
    }
}