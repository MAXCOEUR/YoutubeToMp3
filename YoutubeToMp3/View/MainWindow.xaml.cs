using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using YoutubeToMp3.repository;
using YoutubeToMp3.model;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using YoutubeToMp3.Model;

namespace YoutubeToMp3
{
    public partial class MainWindow : Window
    {
        private MusiqueRepository _musiqueRepository = new MusiqueRepository();
        private ObservableCollection<Musique> _musiqueList = new ObservableCollection<Musique>();
        private SettingsManager settingsManager = SettingsManager.Instance;

        public MainWindow()
        {
            InitializeComponent();
            MusicList.ItemsSource = _musiqueList;

            Title += " " + GetAppVersion();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RunAutoUpdate();
        }

        private async Task RunAutoUpdate()
        {
            try
            {
                // 1. Afficher l'overlay et bloquer l'interface
                UpdateOverlay.Visibility = Visibility.Visible;

                // 2. Lancer la mise à jour (sur un thread séparé pour ne pas figer la barre de chargement)
                await Task.Run(async () => {
                    await _musiqueRepository.UpdateYtDlp();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour : {ex.Message}");
            }
            finally
            {
                // 3. Cacher l'overlay et libérer l'utilisateur
                UpdateOverlay.Visibility = Visibility.Collapsed;
            }
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

        private string GetAppVersion()
        {
            var version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
            return version != null ? $"v{version}" : "v1.0";
        }
    }
}
