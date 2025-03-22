using YoutubeToMp3.model;
using NAudio.Wave;
using System.IO;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMp3.DataSource
{
    internal class YoutubeDataSource
    {
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

            string lienMusique = Path.Combine(appPath,"musiquesDownload", $"{musiqueyt.Title} ({musiqueyt.Author}).mp3");

            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(musiqueyt.Url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            if (streamInfo == null)
                throw new Exception("Aucun flux audio disponible pour cette vidéo.");

            string lienMusiqueTmp = Path.Combine(appPath, $"{musiqueyt.Title} ({musiqueyt.Author}).{streamInfo.Container}");

            Console.WriteLine($"Début du téléchargement : {musiqueyt.Title}");
            await _youtube.Videos.Streams.DownloadAsync(streamInfo, lienMusiqueTmp);
            Console.WriteLine("Téléchargement terminé.");

            using (var reader = new MediaFoundationReader(lienMusiqueTmp))
            {
                MediaFoundationEncoder.EncodeToMp3(reader, lienMusique);
            }

            var file = TagLib.File.Create(lienMusique);
            file.Tag.Title = musiqueyt.Title;
            file.Tag.Performers = new[] { musiqueyt.Author };
            file.Save();
            Console.WriteLine("Conversion MP3 terminée.");

            System.IO.File.Delete(lienMusiqueTmp);
            Console.WriteLine("Fichier temporaire supprimé.");

            return true;
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
