using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using UFMT.Core;
using UFMT.FnAssets;
using UFMT.FnAssetsLogic;
using UFMT.MaterialTextureAssignment;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UFMT.UI
{
    public sealed partial class EmotesPage : Page, INotifyPropertyChanged
    {
        private CancellationTokenSource _currentEmotePathDebounce;
        public event PropertyChangedEventHandler PropertyChanged;
        private EmoteData _currentEmote;
        public EmoteData CurrentEmote
        {
            get => _currentEmote;
            set
            {
                if (_currentEmote != value)
                {
                    _currentEmote = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEmote)));
                }
            }
        }
        public EmotesPage()
        {
            InitializeComponent();
            CurrentEmote = new EmoteData();

            EmotesPathTextBox.Text = AppSettings.GetValue("EmotesPath", string.Empty);

            seriesComboBox.Items.Clear();
            var seriesOptions = AppSettings.GetValue<ObservableCollection<string>>("AvailableSeries", null);
            if (seriesOptions != null)
            {
                foreach (string series in seriesOptions)
                {
                    seriesComboBox.Items.Add(series);
                }
            }
            else
            {
                seriesComboBox.Items.Add("None");
                foreach (string series in SkinAssetCreator.SeriesCodenames.Keys)
                {
                    seriesComboBox.Items.Add(series);
                }
                seriesComboBox.Items.Add("+Add");
            }
            seriesComboBox.SelectedIndex = 0;
            seriesComboBox.Items.VectorChanged += SaveSeries;

            CurrentEmotePathTextBox.Text = AppSettings.GetValue("CurrentEmotePath", string.Empty);
            CurrentEmotePathTextBox_TextChanged(CurrentEmotePathTextBox, null);

            ((FrameworkElement)this.Content).Loaded += (s, e) =>
            {
            };
        }

        private void EmotesPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SetValue("EmotesPath", (sender as TextBox)?.Text);
        }

        private async void CurrentEmotePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentEmotePathDebounce?.Cancel();
            _currentEmotePathDebounce = new CancellationTokenSource();
            var token = _currentEmotePathDebounce.Token;
            try
            {
                await Task.Delay(250, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            CurrentEmote = new EmoteData();

            AppSettings.SetValue("CurrentEmotePath", (sender as TextBox).Text);
            if (!EmoteValidator.ValidateAfterPathChange((sender as TextBox)?.Text, CurrentEmote)) return;
            Log.Test($"Current emote's icons folder path is {CurrentEmote.IconsPath}");

            CurrentEmote.Codename = Path.GetFileName(CurrentEmote.Path);

            EmoteData loadedJson = LoadEmoteConfig(Path.Combine(CurrentEmote.Path, $"{CurrentEmote.Codename}_Settings.json"));

            if (loadedJson != null)
            {
                CurrentEmote = loadedJson;
                CurrentEmote.Path = CurrentEmotePathTextBox.Text;
                if (!EmoteValidator.ValidateAfterPathChange(CurrentEmote.Path, CurrentEmote)) return;
                Log.Test("Current emote was not null!");
                Log.Test($"Current emote male animation length: {CurrentEmote.MaleAnimationLength}, current emote name: {CurrentEmote.Name}");
            }
            else
            {
                (bool success, string maleAnim, string femaleAnim) = EmoteFolderScanner.GetAnimationsData(CurrentEmote.AnimationsPath);
                if (!success) return;
                CurrentEmote.MaleAnimationPsa = maleAnim;
                CurrentEmote.FemaleAnimationPsa = femaleAnim;

                Log.Test($"Current emote sound path is {CurrentEmote.SoundPath}");
                (success, string wav) = EmoteFolderScanner.GetSoundData(CurrentEmote.SoundPath);
                if (!success) return;
                CurrentEmote.SoundWavPath = wav;

                (string largeIcon, string smallIcon) = TextureCategorizer.GetIconTextures(CurrentEmote.IconsPath, "emote");
                if (largeIcon == null || smallIcon == null) return;
                CurrentEmote.LargeIcon = largeIcon;
                CurrentEmote.SmallIcon = smallIcon;

                Log.Test($"Large icon: {CurrentEmote.LargeIcon}, Small icon: {CurrentEmote.SmallIcon}");

                CurrentEmote.MaleAnimationLength = PsaReader.GetAnimationLength(Path.Combine(CurrentEmote.AnimationsPath, "Male", CurrentEmote.MaleAnimationPsa));
                CurrentEmote.FemaleAnimationLength = PsaReader.GetAnimationLength(Path.Combine(CurrentEmote.AnimationsPath, "Female", CurrentEmote.FemaleAnimationPsa));

                CurrentEmote.MaleAnimationLength = Math.Round(CurrentEmote.MaleAnimationLength / 30.0, 6);
                CurrentEmote.FemaleAnimationLength = Math.Round(CurrentEmote.FemaleAnimationLength / 30.0, 6);

                CurrentEmote.EID = $"EID_{CurrentEmote.Codename}";
            }

            CurrentEmote.PropertyChanged += (s, e) => SaveEmoteConfig();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e) { }
        private void CreateEmoteFolder_Click(object sender, RoutedEventArgs e) { }
        private void Reimport_Click(object sender, RoutedEventArgs e) { }
        private void SaveSeries(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            AppSettings.SetValue("AvailableSeries", seriesComboBox.Items);
        }
        public void SaveEmoteConfig()
        {
            Log.Test("Save Emote config called!");
            if (CurrentEmote == null || string.IsNullOrEmpty(CurrentEmote.Path)) return;

            string jsonPath = Path.Combine(CurrentEmote.Path, $"{CurrentEmote.Codename}_Settings.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(CurrentEmote, options);

            File.WriteAllText(jsonPath, jsonString);
        }
        private EmoteData LoadEmoteConfig(string jsonPath)
        {
            string filePath = jsonPath;
            if (!File.Exists(filePath)) return null;
            string jsonString = File.ReadAllText(filePath);
            EmoteData loadedEmote = System.Text.Json.JsonSerializer.Deserialize<EmoteData>(jsonString);

            return loadedEmote;
        }
    }

    public class EmoteData : INotifyPropertyChanged
    {
        public string Codename { get; set; } = string.Empty;
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _rarity = "Common";
        public string Rarity
        {
            get => _rarity;
            set
            {
                if (_rarity != value)
                {
                    _rarity = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _series = "None";
        public string Series
        {
            get => _series;
            set
            {
                if (_series != value)
                {
                    _series = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _smallIcon = string.Empty;
        public string SmallIcon
        {
            get => _smallIcon;
            set
            {
                if (_smallIcon != value)
                {
                    _smallIcon = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _largeIcon = string.Empty;
        public string LargeIcon
        {
            get => _largeIcon;
            set
            {
                if (_largeIcon != value)
                {
                    _largeIcon = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _eid = string.Empty;
        public string EID
        {
            get => _eid;
            set
            {
                if (_eid != value)
                {
                    _eid = value;
                    OnPropertyChanged();
                }
            }

        }
        public string MaleAnimationPsa { get; set; } = string.Empty;
        public string MaleAnimationFbx { get; set; } = string.Empty;
        public string MaleAnimationJson { get; set; } = string.Empty;
        private double _maleAnimationLength = 0;
        public double MaleAnimationLength
        {
            get => _maleAnimationLength;
            set
            {
                if (value != _maleAnimationLength)
                {
                    _maleAnimationLength = value;
                    OnPropertyChanged();
                }
            }
        }
        public string FemaleAnimationPsa { get; set; } = string.Empty;
        public string FemaleAnimationFbx { get; set; } = string.Empty;
        public string FemaleAnimationJson { get; set; } = string.Empty;
        private double _femaleAnimationLength = 0;
        public double FemaleAnimationLength
        {
            get => _femaleAnimationLength;
            set
            {
                if (value != _femaleAnimationLength)
                {
                    _femaleAnimationLength = value;
                    OnPropertyChanged();
                }
            }
        }
        public string SoundWavPath { get; set; } = string.Empty;
        public string OutputContentPath { get; set; } = string.Empty;

        public string Path = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string SourcePath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string AnimationsPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string IconsPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string SoundPath { get; set; } = string.Empty;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public EmoteData Clone()
        {
            EmoteData clone = (EmoteData)this.MemberwiseClone();
            return clone;
        }
    }
}
