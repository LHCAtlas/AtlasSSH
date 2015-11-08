using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    public class LineBuffer
    {
        const string CrLf = "\r\n";

        public LineBuffer(Action<string> actionOnLine = null)
        {
            _actinoOnLine = actionOnLine;
        }

        string _text = "";
        private Action<string> _actinoOnLine;

        public void Add(string line)
        {
            _text += line;
            Flush();
        }

        private void Flush()
        {
            while (true)
            {
                var lend = _text.IndexOf(CrLf);
                if (lend < 0)
                    break;

                var line = _text.Substring(0, lend);
                ActOnLine(line);
                _text = _text.Substring(lend + 2);
            }
        }

        public void DumpRest()
        {
            Flush();
            ActOnLine(_text);
        }

        List<string> stringsToSuppress = new List<string>();

        /// <summary>
        /// Any line containing these strings will not be printed out
        /// </summary>
        /// <param name="whenLineContains"></param>
        public void Suppress(string whenLineContains)
        {
            stringsToSuppress.Add(whenLineContains);
        }

        /// <summary>
        /// See if the text is in the current buffer
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool Match(string text)
        {
            return _text.Contains(text);
        }

        /// <summary>
        /// Dump out a line, safely.
        /// </summary>
        /// <param name="line"></param>
        private void ActOnLine(string line)
        {
            Trace.WriteLine("ReturnedLine: " + line, "SSHConnection");
            if (_actinoOnLine == null)
                return;
            if (!stringsToSuppress.Any(s => line.Contains(s)))
            {
                _actinoOnLine(line);
            }
        }
    }
}
