using System;
using System.IO;
using System.Threading;
using System.Web.Hosting;


namespace SymOrdinary
{
    public class FileLogger
    {
        private static readonly object LogFileLock = new object();

        public static void Log(string source, string actionName, string message)
        {
            /*Create Message object and assign values with log parameter*/
            MessageTemplate messageTemplate = new MessageTemplate();
            messageTemplate.Source = source;
            messageTemplate.ActionName = actionName;
            messageTemplate.Message = message;

            /*Create new parameterized thread object*/
            Thread newThread = new Thread(new ParameterizedThreadStart(FileLogger.WriteToFile));
            newThread.IsBackground = true;

            /*Start thread*/
            newThread.Start(messageTemplate);
        }
        public static void WriteToFile(object messageTemplate)
        {
            try
            {
                MessageTemplate msTemplate = messageTemplate as MessageTemplate;
                string path = HostingEnvironment.MapPath("~/Files/LoggerFile/Logs.txt");
                if (msTemplate == null || string.IsNullOrEmpty(path))
                {
                    return;
                }

                string directory = Path.GetDirectoryName(path);
                string logText = Environment.NewLine
                    + DateTime.Now.ToString("yyyy-MM-dd H:mm:ss zzz")
                    + Environment.NewLine
                    + " Source : " + msTemplate.Source + " , Method : " + msTemplate.ActionName
                    + Environment.NewLine
                    + msTemplate.Message
                    + Environment.NewLine;

                lock (LogFileLock)
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.AppendAllText(path, logText);
                }
            }
            catch (IOException)
            {
                // Logging must never interrupt the application when the log file is locked.
            }
            catch (UnauthorizedAccessException)
            {
                // Logging must never interrupt the application when the log path is unavailable.
            }
        }

        public class MessageTemplate
        {
            public string Source { get; set; }
            public string ActionName { get; set; }
            public string Message { get; set; }
        }

    }
}
