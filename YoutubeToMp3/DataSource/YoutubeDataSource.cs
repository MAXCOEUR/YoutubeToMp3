using YoutubeToMp3.model;
using NAudio.Wave;
using System.IO;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using System.Diagnostics;
using YoutubeToMp3.outils;
using YoutubeToMp3.Model;

namespace YoutubeToMp3.DataSource
{
    internal class YoutubeDataSource
    {
        private SettingsManager settingsManager = SettingsManager.Instance;
        string appPath = AppDomain.CurrentDomain.BaseDirectory;
        YoutubeClient _youtube = new YoutubeClient();
        public bool IsPlaylist(string url)
        {
            if (PlaylistId.TryParse(url) != null)
                return true;
            if (VideoId.TryParse(url) != null)
                return false;
            throw new ArgumentException("URL YouTube invalide.");
        }


        public async Task<List<Musique>> GetPlaylistVideos(string playlistUrl)
        {
            var playlistId = PlaylistId.TryParse(playlistUrl);
            if (playlistId == null)
                throw new ArgumentException("URL de playlist invalide.");

            var musiqueList = new List<Musique>();
            await foreach (var video in _youtube.Playlists.GetVideosAsync(playlistId.Value))
            {
                musiqueList.Add(new Musique(video.Url, CleanFileName(video.Title), CleanFileName(video.Author.ChannelTitle), video.Thumbnails.GetWithHighestResolution().Url));
            }

            return musiqueList;
        }


        public async Task<Musique> GetVideoInfo(string videoUrl)
        {
            var videoId = VideoId.TryParse(videoUrl);
            if (videoId == null)
            {
                throw new ArgumentException("URL de vidéo invalide.");
            }

            var video = await _youtube.Videos.GetAsync(videoId.Value);

            return new Musique(video.Url, CleanFileName(video.Title), CleanFileName(video.Author.ChannelTitle), video.Thumbnails.GetWithHighestResolution().Url);
        }

        public string GetPathFolder()
        {
            return Path.Combine(appPath, "musiquesDownload");
        }
        public async Task<bool> DownloadMusique(Musique musiqueyt)
        {
            Console.WriteLine(appPath);
            Directory.CreateDirectory(Path.Combine(appPath, "musiquesDownload"));

            string lienMusique = Path.Combine(appPath, "musiquesDownload", $"{musiqueyt.Title} ({musiqueyt.Author}).mp3");
            string lienMusiqueTmp = "";

            try
            {
                

                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(musiqueyt.Url);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                if (streamInfo == null)
                    throw new Exception("Aucun flux audio disponible pour cette vidéo.");

                lienMusiqueTmp = Path.Combine(appPath, $"{musiqueyt.Title} ({musiqueyt.Author}).{streamInfo.Container}");

                Console.WriteLine($"Début du téléchargement : {musiqueyt.Title}");
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, lienMusiqueTmp);
                Console.WriteLine("Téléchargement terminé.");

                await FFmpegGestion.ConvertWebmToMp3(lienMusiqueTmp, lienMusique);

                var file = TagLib.File.Create(lienMusique);
                file.Tag.Title = musiqueyt.Title;
                file.Tag.Performers = new[] { musiqueyt.Author };
                file.Save();
                Console.WriteLine("Conversion MP3 terminée.");

                System.IO.File.Delete(lienMusiqueTmp);
                Console.WriteLine("Fichier temporaire supprimé.");

                return true;
            }
            catch (Exception e)
            {
                if (lienMusiqueTmp != "")
                {
                    File.Delete(lienMusiqueTmp);
                    File.Delete(lienMusique);
                }

                return await otherdl(musiqueyt);
            }


        }

        private async Task<bool> otherdl(Musique musiqueyt)
        {
            string directory = Path.Combine(appPath, "musiquesDownload");
            Directory.CreateDirectory(directory);

            string outputTemplate = Path.Combine(directory, $"{musiqueyt.Title} ({musiqueyt.Author}).%(ext)s");
            string finalMp3Path = Path.Combine(directory, $"{musiqueyt.Title} ({musiqueyt.Author}).mp3");

            string qjsPath = Path.Combine(appPath, "outilsExtern", "qjs.exe");

            string arguments = $"-x --audio-format mp3 --no-check-certificate " +
                               $"--js-runtimes \"quickjs:{qjsPath}\" " +
                               $"--extractor-args \"youtube:player-client=android,web;po_token=web+generated\" " +
                               $"-o \"{outputTemplate}\" ";

            if (File.Exists(FFmpegGestion.ffmpegPath))
            {
                arguments += $" --ffmpeg-location \"{FFmpegGestion.ffmpegPath}\"";
            }

            arguments += $" \"{musiqueyt.Url}\"";

            using (var process = new Process())
            {
                process.StartInfo.FileName = Path.Combine(appPath, "outilsExtern", "yt-dlp.exe");
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = Path.Combine(appPath, "outilsExtern");
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine($"[yt-dlp]: {e.Data}"); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine($"[Error]: {e.Data}"); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Attente asynchrone de la fin du processus
                await Task.Run(() => process.WaitForExit());

                // Application des Tags ID3 sur le fichier MP3 généré
                if (File.Exists(finalMp3Path))
                {
                    try
                    {
                        var file = TagLib.File.Create(finalMp3Path);
                        file.Tag.Title = musiqueyt.Title;
                        file.Tag.Performers = new[] { musiqueyt.Author };
                        file.Save();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'écriture des tags: {ex.Message}");
                        return true; // On retourne quand même true car le fichier est téléchargé
                    }
                }

                return false;
            }
        }
        public async Task UpdateYtDlp()
        {
            Console.WriteLine("Vérification des mises à jour pour yt-dlp...");
            string toolsPath = Path.Combine(appPath, "outilsExtern");
            string ytDlpPath = Path.Combine(toolsPath, "yt-dlp.exe");

            if (!File.Exists(ytDlpPath))
            {
                Console.WriteLine("Erreur : yt-dlp.exe introuvable.");
                return;
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = ytDlpPath;
                process.StartInfo.Arguments = "-U";
                process.StartInfo.WorkingDirectory = toolsPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) Console.WriteLine($"[yt-dlp Update]: {e.Data}");
                };

                process.Start();

                process.BeginOutputReadLine();
                await Task.Run(() => process.WaitForExit());

                Console.WriteLine("Processus de mise à jour terminé.");
            }
        }

        private string CleanFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '-');
            }
            return fileName;
        }

    }
}
