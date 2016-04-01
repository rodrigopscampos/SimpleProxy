using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SimpleProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("Inform the prefix handler");
                return;
            }

            var listener = new HttpListener();
            var prefix = args[0];
            listener.Prefixes.Add(prefix);

            listener.Start();

            Console.WriteLine("Listening {0}", prefix);

            while (true)
            {
                var context = listener.GetContext();
                Task.Run(() => Process(context));
            }
        }

        static void Process(HttpListenerContext context)
        {
            try
            {
                var listenerRequest = context.Request;

                Console.WriteLine("{0} {1}", listenerRequest.HttpMethod, listenerRequest.Url);

                var webRequest = (HttpWebRequest)WebRequest.Create(listenerRequest.Url);
                webRequest.Method = listenerRequest.HttpMethod;
                webRequest.ContentType = listenerRequest.ContentType;
                webRequest.UserAgent = listenerRequest.UserAgent;

                try
                {
                    using (Stream reqStream = webRequest.GetRequestStream())
                    {
                        listenerRequest.InputStream.CopyTo(reqStream);
                        reqStream.Close();
                    }
                }
                catch { }

                var webRequestResponse = default(HttpWebResponse);

                try
                {
                    webRequestResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException we)
                {
                    var r = (HttpWebResponse)we.Response;

                    context.Response.StatusCode = (int)r.StatusCode;
                    context.Response.StatusDescription = r.StatusDescription;

                    using (var stream = r.GetResponseStream())
                        stream.CopyTo(context.Response.OutputStream);

                    context.Response.Close();

                    return;
                }

                context.Response.ContentType = webRequestResponse.ContentType;

                using (var stream = webRequestResponse.GetResponseStream())
                    stream.CopyTo(context.Response.OutputStream);

                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.ToString());
            }
        }
    }
}
