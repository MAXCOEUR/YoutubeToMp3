using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using YoutubeToMp3.repository;
using YoutubeToMp3.model;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

namespace YoutubeToMp3
{
    public partial class MainWindow : Window
    {
        private MusiqueRepository _musiqueRepository = new MusiqueRepository();
        private ObservableCollection<Musique> _musiqueList = new ObservableCollection<Musique>();

        public MainWindow()
        {
            InitializeComponent();
            MusicList.ItemsSource = _musiqueList;
        }

        private async void PasteFromClipboard(object sender, RoutedEventArgs e)
        {
            string url = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                bool isPlaylist = await Task.Run(() => _musiqueRepository.IsPlaylistYoutube(url));

                if (isPlaylist)
                {
                    MessageBoxResult result = MessageBox.Show("C'est une playlist ! Télécharger toutes les vidéos ?",
                        "Playlist détectée", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var videos = await Task.Run(() => _musiqueRepository.GetPlaylistVideosYoutube(url));
                        foreach (var video in videos)
                        {
                            addIn_musiqueList(video);
                        }
                    }
                    else
                    {
                        var video = await Task.Run(() => _musiqueRepository.GetVideoInfoYoutube(url));
                        addIn_musiqueList(video);
                    }
                }
                else
                {
                    var video = await Task.Run(() => _musiqueRepository.GetVideoInfoYoutube(url));
                    addIn_musiqueList(video);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void addIn_musiqueList(Musique musique)
        {
            if (!_musiqueList.Contains(musique))
            {
                _musiqueList.Add(musique);
                Console.WriteLine(musique);
            }
            else
            {
                MessageBox.Show($"La video : {musique} est deja dans la liste ", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DownloadMusic(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Musique musique)
            {
                try
                {
                    musique.DownloadStatus = 2;
                    await Task.Run(() => _musiqueRepository.DownloadMusiqueYoutube(musique));
                    musique.DownloadStatus = 3;
                    MessageBox.Show($"Téléchargement terminé : {musique.Title}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du téléchargement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    musique.DownloadStatus = 4;
                }
            }
        }

        private async void DownloadAll(object sender, RoutedEventArgs e)
        {
            foreach (var musique in _musiqueList)
            {
                musique.DownloadStatus = 1;
            }
            foreach (var musique in _musiqueList)
            {
                try
                {
                    musique.DownloadStatus = 2;
                    await Task.Run(() => _musiqueRepository.DownloadMusiqueYoutube(musique));
                    
                    musique.DownloadStatus = 3;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du téléchargement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    musique.DownloadStatus = 4;
                }
            }
            MessageBox.Show("Tous les téléchargements sont terminés !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveMusic(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Musique musique)
            {
                _musiqueList.Remove(musique);
            }
        }

        private void OpenDownloadFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                string folderPath = _musiqueRepository.GetPathFolder();
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dossier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
