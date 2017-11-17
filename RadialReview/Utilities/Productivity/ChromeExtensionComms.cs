using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Utilities.Productivity
{
    public class ChromeExtensionComms
    {
        private static HttpListener _listener;
        private static List<String> commands = new List<string>();
        private static ChromeExtensionComms singleton = null;
        private static bool waitingForCleared = false;
        public static void SendCommand(string command,string details=null)
        {
            if (Config.RunChromeExt())
            {
                try
                {
                    if (singleton == null)
                        singleton = new ChromeExtensionComms();
                    singleton.Send(command,details);

                }
                catch (Exception)
                {
                   // int a = 0;
                }
            }
        }
        protected ChromeExtensionComms()
        {
            if (!Config.RunChromeExt())
                return;
            if (_listener == null)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:60024/");
                _listener.Start();
                _listener.BeginGetContext(new AsyncCallback(ChromeExtensionComms.ProcessRequest), null);
            }
        }


        protected void Send(string command, string details = null)
        {

            if (!Config.RunChromeExt())
                return;

            lock (_listener)
            {
                var toAdd = command;
                if (details != null)
                    toAdd = toAdd + "~" + details;
                commands.Add("\"" + toAdd + "\"");
            }
        }

        static void ProcessRequest(IAsyncResult result)
        {

            if (!Config.RunChromeExt())
                return;

            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            //Answer getCommand/get post data/do whatever

            _listener.BeginGetContext(new AsyncCallback(ChromeExtensionComms.ProcessRequest), null);
            string responseString=null;
            switch (context.Request.QueryString["action"])
            {
                case "getCommands": {   
                    lock (_listener)
                     {
                         responseString = "{\"commands\":[" + string.Join(",", commands) + "],\"status\":\"ok\"}";
                    }
                    break;
                }
                case "clearCommands":{
                    lock (_listener) { commands.Clear(); }
                    responseString = "{\"status\":\"ok\"}";
                    waitingForCleared = false;
                    break;
                }
            }

            responseString = responseString ?? "{\"status\":\"error\",\"message\":\"command not found\"}";

            HttpListenerResponse response = context.Response;
            response.ContentType = "application/json";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString.ToString());
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;

            output.Write(buffer, 0, buffer.Length);
            output.Close();


        }

        public async static Task<bool> SendCommandAndWait(string p,int timeoutMs=10000)
        {
            if (!Config.RunChromeExt())
                return false;
            SendCommand(p);
            waitingForCleared = true;

            var timerMs = 500;
            for (var i = 0; i < timeoutMs / timerMs; i++)
            {
                if (waitingForCleared == false)
                    return true;
                await Task.Delay(timerMs);
            }
            return false;
        }
    }
}