using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NorcusSetClient
{
    public class Logger
    {
        private StringBuilder stringBuilder;

        public Logger()
        {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            stringBuilder = new StringBuilder(assemblyName.FullName +
                "\r\n------------------------------------------------------------------------");
        }

        /// <summary>
        /// Append text to log
        /// </summary>
        /// <param name="text">Text to log</param>
        public void Log(string text)
        {
            stringBuilder.Append("\r\n" + DateTime.Now.ToShortDateString() + " " +
                DateTime.Now.ToLongTimeString() + ": " + text);
        }
        /// <summary>
        /// Saves log into file (appends text to existing file)
        /// </summary>
        /// <param name="logFileName"></param>
        public void Save()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);

            using (StreamWriter w = File.AppendText(path + "\\" + assemblyName + "_Log.txt"))
            {
                w.WriteLine(stringBuilder.ToString());
                w.WriteLine("---------- END OF LOG ----------\r\n");
            }
        }
    }
}
