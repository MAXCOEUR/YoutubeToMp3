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
        string appPath = Directory.GetCurrentDirectory();
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

            string lienMusique = Path.Combine(appPath, "musiquesDownload", $"{musiqueyt.Title} ({musiqueyt.Author}).mp3");

            string arguments = $"-x --audio-format mp3 -o \"{lienMusique}\" ";
            if (File.Exists(FFmpegGestion.ffmpegPath))
            {
                arguments += " --ffmpeg-location \"" + FFmpegGestion.ffmpegPath + "\"";
            }

            if (SettingsManager.Instance.browsers[SettingsManager.Instance.browserIndice].ToLower().Contains("chrome"))
            {
                arguments += " --cookies-from-browser chrome";
            }
            else if (SettingsManager.Instance.browsers[SettingsManager.Instance.browserIndice].ToLower().Contains("edge"))
            {
                arguments += " --cookies-from-browser edge";
            }
            else if (SettingsManager.Instance.browsers[SettingsManager.Instance.browserIndice].ToLower().Contains("firefox"))
            {
                arguments += " --cookies-from-browser firefox";
            }


            arguments += " " + musiqueyt.Url;

            using (var process = new Process())
            {
                process.StartInfo.FileName = ".\\outilsExtern\\yt-dlp.exe";
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = ".\\outilsExtern";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.EnableRaisingEvents = true;

                // Événement pour la sortie standard
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                };

                // Événement pour la sortie d'erreur
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"Error: {e.Data}");
                        // Traiter la sortie d'erreur ici
                    }
                };

                // Démarrer le processus
                process.Start();

                // Commencer la redirection de la sortie standard et d'erreur de manière asynchrone
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Attendre que le processus se termine
                await Task.Run(() => process.WaitForExit());

                var file = TagLib.File.Create(lienMusique);
                file.Tag.Title = musiqueyt.Title;
                file.Tag.Performers = new[] { musiqueyt.Author };
                file.Save();

                return true;
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
