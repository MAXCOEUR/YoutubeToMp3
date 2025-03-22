using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace YoutubeToMp3.model
{
    public class Musique : INotifyPropertyChanged
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ThumbnailUrl { get; set; }

        private int _downloadStatus = 0;

        public int DownloadStatus
        {
            get => _downloadStatus;
            set
            {
                _downloadStatus = value;
                OnPropertyChanged(nameof(DownloadStatus));
            }
        }

        public Musique(string url, string title, string author, string thumbnailUrl)
        {
            Url = url;
            Title = title;
            Author = author;
            ThumbnailUrl = thumbnailUrl;
        }

        public BitmapImage ThumbnailBitmap
        {
            get
            {
                if (string.IsNullOrEmpty(ThumbnailUrl))
                    return null;

                try
                {
                    return new BitmapImage(new Uri(ThumbnailUrl, UriKind.Absolute));
                }
                catch
                {
                    return null; // Évite les erreurs si l'URL est invalide
                }
            }
        }

        public override string ToString()
        {
            return $"Titre: {Title}\nAuteur: {Author}\nURL: {Url}\nMiniature: {ThumbnailUrl}";
        }

        // Surcharge de l'opérateur ==
        public static bool operator ==(Musique musique1, Musique musique2)
        {
            if (ReferenceEquals(musique1, musique2))
                return true;

            if (musique1 is null || musique2 is null)
                return false;

            return musique1.Title == musique2.Title &&
                   musique1.Author == musique2.Author;
        }

        // Surcharge de l'opérateur !=
        public static bool operator !=(Musique musique1, Musique musique2)
        {
            return !(musique1 == musique2);
        }

        public override int GetHashCode()
        {
            return (Url, Title, Author, ThumbnailUrl).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Musique otherMusique)
            {
                return this == otherMusique;
            }
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
