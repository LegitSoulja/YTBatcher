using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YTBatcher
{

    // Simple request library I wrote. Nothing special, I like doing things the hard way :)

    enum Method : byte { GET, POST }

    class Request
    {

        private static int PID = 0;

        private static Task<string> DictToBody(Dictionary<string, string> dict)
        {
            string[] data = new string[dict.Count];
            var e = dict.GetEnumerator();
            int pointer = 0;
            while (e.MoveNext())
                data[pointer++] = String.Format("{0}={1}", e.Current.Key, e.Current.Value);
            return Task.FromResult(String.Join('&', data));
        }


        private static async Task<byte[]> CreateRequest(string url, Method method, Dictionary<string, string> args = null, Dictionary<string, string> headers = null)
        {

            int pid = PID++;
            Logger.Title("Starting Request: #{0}, {1}", pid, url);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            byte[] buffer;

            if (headers != null)
            {
                var e = headers.GetEnumerator();
                while (e.MoveNext())
                    request.Headers.Add(e.Current.Key, e.Current.Value);
            }

            if (method == Method.POST && args != null)
            {
                using Stream stream = await request.GetRequestStreamAsync();
                buffer = Encoding.UTF8.GetBytes(await DictToBody(args));
                await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));
            }

            using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Logger.Title("#{0}: Making request to: {1}", pid, url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Array.Empty<byte>();
            }
            int ContentLength = (int)response.ContentLength;
            if (ContentLength <= 0)
            {
                var e = response.Headers.Keys.GetEnumerator();
                while (e.MoveNext())
                {
                    // some servers ignore header case sensitivity.
                    if (((string)e.Current).ToLower() == "content-length")
                    {
                        if (!int.TryParse((string)e.Current, out ContentLength))
                        {
                            ContentLength = 1024 * 4;
                        }
                        break;
                    }
                }
                if (ContentLength <= 0)
                {
                    ContentLength = 1024 * 4;
                }
            }
            buffer = new byte[ContentLength];
            using MemoryStream memory = new((int)5E8);
            using (Stream stream = response.GetResponseStream())
            {
                while (stream.CanRead)
                {
                    int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                    if (read == 0) break;
                    memory.Write(buffer, 0, read);
                    Logger.Title("#{0}: Read {1}/{2} bytes", pid, memory.Position, memory.Length);
                }
            }
            return memory.ToArray();
        }

        internal static async Task<byte[]> Get(string url, Dictionary<string, string> headers = null)
            => (await CreateRequest(url, Method.GET, null, headers)).ToArray();

        internal static async Task<T> GetJson<T>(string url, Dictionary<string, string> headers = null)
            => await System.Text.Json.JsonSerializer.DeserializeAsync<T>(new MemoryStream(await CreateRequest(url, Method.GET, null, headers)));

        internal static async Task<byte[]> Post(string url, Dictionary<string, string> args = null, Dictionary<string, string> headers = null)
            => (await CreateRequest(url, Method.POST, args, headers)).ToArray();

        internal static async Task<T> PostJson<T>(string url, Dictionary<string, string> args = null, Dictionary<string, string> headers = null)
            => await JsonSerializer.DeserializeAsync<T>(new MemoryStream(await CreateRequest(url, Method.POST, args, headers)));

    }
}
