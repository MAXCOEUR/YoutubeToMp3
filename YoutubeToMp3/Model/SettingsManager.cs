using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeToMp3.Model
{
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private static readonly object _lockObject = new object();

        // Déclare ici les propriétés de tes paramètres

        public string APP_NAME { get; }

        // Constructeur privé pour empêcher l'instanciation directe
        private SettingsManager()
        {
            APP_NAME = "YoutubeToMp3";
        }        

        // Méthode pour obtenir l'instance unique de la classe
        public static SettingsManager Instance
        {
            get
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new SettingsManager();
                    }
                    return _instance;
                }
            }
        }
    }
}
