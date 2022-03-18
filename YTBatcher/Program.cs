using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace YTBatcher
{
    class Program
    {

        internal const string YTAPI = "https://www.googleapis.com/youtube/v3/";
        private static string Directory, YTPath, YTKEY;

        static void Main()
        {
            Directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + System.IO.Path.DirectorySeparatorChar;
            Start().Wait();
            Logger.SuccessAndTitle("Process Complete! Press any key to exit");
            Console.ReadLine();
        }

        private static async Task Start()
        {
            #region [Declarations]
            string downloadpath, playlistid, apiurl;
            Queue<YTPlaylistItem> items = new Queue<YTPlaylistItem>();

            // Headers used to retrieve playlist video ID's
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "Accept", "application/json" },
                { "User-Agent", "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US) AppleWebKit/534.19 (KHTML, like Gecko) Chrome/11.0.667.0 Safari/534.19"}
            };

            // Prepared arguments for youtube-dl
            string[] arguments = new string[]
            {
                "",
                //@"-f 'bestvideo+bestaudio[ext=m4a]/bestvideo+bestaudio/best'",
                "-f b",
                "--merge-output-format mp4",
                "--rm-cache-dir",
                ""
            };
            #endregion

            #region [Check / Install / Update Youtube-DLP]
            // Check if youutube-dlp is installed, if not install it
            YTPath = string.Format("{0}{1}", Directory, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp"));
            if (!File.Exists(YTPath))
            {
                Logger.SuccessAndTitle("Downloading Youtube-DLP");
                await File.WriteAllBytesAsync(YTPath, await Request.Get((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" : "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp")));
            }
            else
            {
                Logger.Acknowledge("Updating Youtube-DLP");
                await Execute(YTPath, new string[] { "-U" });
            }
            #endregion

            #region [Get Youtube Public API key]
            Logger.Acknowledge("Getting Youtube Public API Key");
            YTKEY = await GetYTPublicKey();
            if (YTKEY == null)
            {
                Logger.Error("Failed to obtain youtube public api key!");
                return;
            }
            await Task.Delay(1000);
            Console.Clear();
            #endregion

            #region [Get File Path To Download Videos]
            Logger.Title("Waiting for user input.");
            // Get download path to put videos
            while (true)
            {
                Logger.Write.Acknowledge("Enter path to download videos: ");
                downloadpath = Console.ReadLine().ToString();
                if (!System.IO.Directory.Exists(downloadpath))
                {
                    Logger.Warn("Invalid Directory Path!");
                    continue;
                }
                break;
            }
            arguments[0] = "-o \"" + downloadpath + Path.DirectorySeparatorChar + "%(title)s-%(id)s.%(ext)s\"";
            #endregion

            #region [Get Playlist URL]
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"list=([A-Za-z0-9_\-+]*)\&?");
            System.Text.RegularExpressions.Match match;

        getplaylisturl:
            // Get playlist URL
            while (true)
            {
                Logger.Write.Acknowledge("Enter Youtube Playlist: ");
                string playlisturl = Console.ReadLine().ToString();
                if (Uri.IsWellFormedUriString(playlisturl, UriKind.RelativeOrAbsolute))
                {
                    match = regex.Match(playlisturl);
                    if (match.Success)
                    {
                        playlistid = match.Groups[1].Value;
                        break;
                    }
                }
                Logger.Warn("Invalid YouTube Playlist URL!");
            }
            apiurl = string.Format("https://youtube.googleapis.com/youtube/v3/playlistItems?part=snippet&part=id&maxResults=50&playlistId={0}&key={1}", playlistid, YTKEY);
            #endregion

            #region [Get Videos In Playlist]
            try
            {
                // Get first page of videos of playlist
                YTPlaylist playlist = await Request.GetJson<YTPlaylist>(apiurl, headers);

                if (playlist == null)
                {
                    Logger.Error("Failed to get playlist information.");
                    goto getplaylisturl;
                }

                foreach (YTPlaylistItem item in playlist.Items)
                    items.Enqueue(item);

                Logger.LogAndTitle("{0} of {1} videos obtained frm playlist", items.Count, playlist.PageInfo.TotalResults);

                // Fetch other videos of pages of the playlist
                while (!string.IsNullOrEmpty(playlist.NextPageToken))
                {
                    string url = apiurl;
                    if (playlist != null)
                    {
                        url += "&pageToken=" + playlist.NextPageToken;
                        playlist = await Request.GetJson<YTPlaylist>(url, headers);
                        if (playlist != null)
                            foreach (YTPlaylistItem item in playlist.Items)
                                items.Enqueue(item);
                        if (items.Count == playlist.PageInfo.TotalResults || playlist.Items.Length != playlist.PageInfo.ResultsPerPage)
                            break;
                        Logger.LogAndTitle("{0} of {1} videos obtained frm playlist", items.Count, playlist.PageInfo.TotalResults);
                        continue;
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get playlist information: " + e.Message);
                goto getplaylisturl;
            }
            #endregion

            #region [Get Number Of Thread/Videos To Download Simultaneously]
            int downloadthreads;
            while (true)
            {
                Logger.Write.Acknowledge("How many download threads?: ");
                if (!int.TryParse(Console.ReadLine().ToString(), out downloadthreads))
                {
                    Logger.Warn("Invalid number (integer) provided!");
                    continue;
                }
                break;
            }
            int videos = items.Count;
            Task<string>[] tasks = new Task<string>[downloadthreads];
            #endregion

            #region [Download Videos]
            // Download Commences! 
            while (items.Count > 0)
            {
                for (int i = 0; i < downloadthreads; i++)
                {
                    if (tasks[i] == null || tasks[i].IsCompleted)
                    {
                        if (!items.TryDequeue(out YTPlaylistItem item))
                            break;
                        string[] s = (string[])arguments.Clone();
                        s[^1] = item.Snippet.ResourceId.VideoId;
                        Console.WriteLine("Downloading: {0}: [{1}]", item.Snippet.ResourceId.VideoId, item.Snippet.Title);
                        tasks[i] = Execute(YTPath, s, false, false);
                    }
                }
                Logger.Title("{0}/{1} in process or completed.", videos - items.Count, videos);
                Task.WaitAny(tasks);
            }

            // after all tasks are dequed, ensure all tasks are complete.
            Task.WaitAll(tasks);
            #endregion
        }

        public static async Task<string> Execute(string filePath, string[] arguments, bool useShellExecute = false, bool createNoWindow = true)
        {
            return await Task.Run<string>(async () =>
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
            return match.Groups[1].Value;
        }

    }
}
