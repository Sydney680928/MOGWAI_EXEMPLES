using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOGWAI_CLI
{
    public class ServerMessage
    {
        public string Source { get; set; }

        public string Function { get; set; }

        public List<string> Parameters { get; set; }

        public ServerMessage(string source, string function, params string[] parameters)
        {
            Source = source;
            Function = function;
            Parameters = parameters.ToList();
        }
    }
}
