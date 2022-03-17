using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace YTBatcher
{
    class Program
    {

        internal const string YTAPI = "https://www.googleapis.com/youtube/v3/";
        internal static string YTArguments { get; private set; }
        internal static string Directory { get; private set; }
        internal static string YTPath { get; private set; }
        internal static string YTKEY { get; private set; }

        static void Main(string[] args)
        {
            Directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + System.IO.Path.DirectorySeparatorChar;
            YTPath = Directory + "ytdl.exe";
            Start().Wait();
            Console.WriteLine("Process Complete! Press any key to exit");
            Console.ReadLine();
        }

        private static async Task Start()
        {

            // Check if youutube-dlp is installed, if not install it
            if (!File.Exists(YTPath))
            {
                Console.WriteLine("Downloading Youtube-DLP");
                await File.WriteAllBytesAsync(YTPath, await Request.Get("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"));
            }

            // Get youtube public API key
            YTKEY = await GetYTPublicKey();
            if(YTKEY == null)
            {
                Console.WriteLine("Failed to obtain youtube public api key!");
                return;
            }

            string downloadpath, playlistid;

            Logger.Title("Waiting for user input.");
            // Get download path to put videos
            while (true)
            {
                Console.Write("Enter path to download videos: ");
                downloadpath = Console.ReadLine().ToString();
                if (!System.IO.Directory.Exists(downloadpath))
                {
                    Console.WriteLine("Invalid Directory Path!");
                    continue;
                }
                break;
            }

            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"list=([A-Za-z0-9_+]*)\&?");
            System.Text.RegularExpressions.Match match;

            getplaylisturl:
            // Get playlist URL
            while (true)
            {
                Console.Write("Enter Youtube Playlist: ");
                string playlisturl = Console.ReadLine().ToString();
                if (Uri.IsWellFormedUriString(playlisturl, UriKind.RelativeOrAbsolute))
                {
                    match = regex.Match(playlisturl);
                    if(match.Success)
                    {
                        playlistid = match.Groups[1].Value;
                        break;
                    }
                }
                Console.WriteLine("Invalid YouTube Playlist URL!");
            }

            YTPlaylist playlist = null;
            Queue<YTPlaylistItem> items = new Queue<YTPlaylistItem>();
            
            // Headers used to retrieve playlist video ID's
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "Accept", "application/json" },
                { "User-Agent", "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US) AppleWebKit/534.19 (KHTML, like Gecko) Chrome/11.0.667.0 Safari/534.19"}
            };
            Console.WriteLine(playlistid);
            string apiurl = string.Format("https://youtube.googleapis.com/youtube/v3/playlistItems?part=snippet&part=id&maxResults=50&playlistId={0}&key={1}", playlistid, YTKEY);
            
            // Fetch playlist video ID's per page, appending them to Queue.
            while (true)
            {
                string url = apiurl;
                if(playlist != null)
                {
                    if(items.Count == playlist.pageInfo.totalResults)
                    {
                        break;
                    }
                    if (string.IsNullOrEmpty(playlist.nextPageToken))
                    {
                        break;
                    }else
                    {
                        url += "&pageToken=" + playlist.nextPageToken;
                    }
                }
                try
                {
                    playlist = await Request.GetJson<YTPlaylist>(url, headers);
                }
                catch (Exception e)
                {
                    Logger.Log("Failed to get playlist information: " + e.Message);
                    goto getplaylisturl;
                }
                foreach(YTPlaylistItem item in playlist.items)
                {
                    items.Enqueue(item);
                }
                Logger.Log(items.Count + " of " + playlist.pageInfo.totalResults);
                if(playlist.items.Length != playlist.pageInfo.resultsPerPage)
                {
                    break;
                }
            }

            string[] arguments = new string[]
            {
                "-o \""+downloadpath+Path.DirectorySeparatorChar+"%(title)s-%(id)s.%(ext)s\"",
                //@"-f 'bestvideo+bestaudio[ext=m4a]/bestvideo+bestaudio/best'",
                "-f b",
                "--merge-output-format mp4",
                "--rm-cache-dir",
                ""
            };

            // Get number of threads/videos should be downloaded simultaneously. 
            int downloadthreads = 1;
            while (true)
            {
                Console.Write("How many download threads?: ");
                if(!int.TryParse(Console.ReadLine().ToString(), out downloadthreads))
                {
                    Console.WriteLine("Invalid number (integer) provided!");
                    continue;
                }
                break;
            }
            int videos = items.Count;
            Task<string>[] tasks = new Task<string>[downloadthreads];

            YTPlaylistItem testpeek;
            // Download Commences! 
            while (items.TryPeek(out testpeek))
            {
                for(int i = 0; i < downloadthreads; i++)
                {
                    if(tasks[i] == null || tasks[i].IsCompleted)
                    {
                        YTPlaylistItem item;
                        if (!items.TryDequeue(out item))
                            break;
                        string[] s = (string[])arguments.Clone();
                        s[s.Length - 1] = item.snippet.resourceId.videoId;
                        Console.WriteLine("Downloading: {0}: [{1}]", item.snippet.resourceId.videoId, item.snippet.title);
                        tasks[i] = Execute(YTPath, s, false, false);
                    }
                }
                Logger.Title("{0}/{1} in process or completed.", videos-items.Count, videos);
                Task.WaitAny(tasks);
            }
            
            // after all tasks are dequed, ensure all tasks are complete.
            Task.WaitAll(tasks);

        }

        public static async Task<string> Execute(string filePath, string[] arguments, bool useShellExecute = false, bool createNoWindow = true)
        {

            return await Task.Run<string>(async() =>
            {
                string output = string.Empty;
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = filePath,
                        UseShellExecute = useShellExecute,
                        CreateNoWindow = createNoWindow,
                        Arguments = string.Join(" ", arguments),
                        RedirectStandardOutput = false,
                        RedirectStandardInput = false
                    }
                };
                if (p.Start())
                {
                    while (!p.HasExited)
                    {
                        await Task.Delay(500);
                        //p.StandardInput.WriteLine();
                        p.Refresh();
                    }
                    //output = await p.StandardOutput.ReadToEndAsync();
                }
                p.Dispose();
                p = null;
                return output;
            });
        }

        private static async Task<string> GetYTPublicKey()
        {
            string basejs = Encoding.UTF8.GetString(await Request.Get("https://www.youtube.com/yts/jsbin/player-vflPHG8dr/de_DE/base.js"));
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\$l\(.,.key.,.([A-Za-z0-9-_]*).\);");
            System.Text.RegularExpressions.Match match = regex.Match(basejs, 200000);
            if (!match.Success)
                return null;
            basejs = null;
            regex = null;
            return match.Groups[1].Value;
        }

    }
}
