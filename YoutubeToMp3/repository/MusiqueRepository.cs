using YoutubeToMp3.DataSource;
using YoutubeToMp3.model;
using System;
namespace YoutubeToMp3.repository
{
    internal class MusiqueRepository
    {
        public async Task<bool> IsPlaylistYoutube(string url)
        {
            YoutubeDataSource dataSource = new YoutubeDataSource();
            return dataSource.IsPlaylist(url);
        }

        public async Task<List<Musique>> GetPlaylistVideosYoutube(string url)
        {
            YoutubeDataSource dataSource = new YoutubeDataSource();
            return await dataSource.GetPlaylistVideos(url);
        }

        public async Task<Musique> GetVideoInfoYoutube(string url)
        {
            YoutubeDataSource dataSource = new YoutubeDataSource();
            return await dataSource.GetVideoInfo(url);
        }
        async public Task<bool> DownloadMusiqueYoutube(Musique musiqueyt)
        {
            YoutubeDataSource dataSource = new YoutubeDataSource();
            return await dataSource.DownloadMusique(musiqueyt);
        }

        public string GetPathFolder()
        {
            YoutubeDataSource dataSource = new YoutubeDataSource();
            return dataSource.GetPathFolder();
        }
    }
}
