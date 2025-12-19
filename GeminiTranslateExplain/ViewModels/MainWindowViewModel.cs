using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using GeminiTranslateExplain.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GeminiTranslateExplain
{
    internal partial class MainWindowViewModel : ObservableObject, IProgressTextReceiver
    {
        public AiModel[] AiModels { get; } = AppConfig.Instance.AIModels;
        public ObservableCollection<PromptProfile> PromptProfiles { get; } = AppConfig.Instance.PromptProfiles;
        public ObservableCollection<ChatMessage> ChatMessages { get; } = new();

        private ChatMessage? _streamingMessage;

        string IProgressTextReceiver.Text
        {
            set
            {
                if (_streamingMessage == null)
                    return;

                _streamingMessage.Text = value;
            }
        }

        [ObservableProperty]
        private AiModel _selectedAiModel = AppConfig.Instance.SelectedAiModel;

        [ObservableProperty]
        private PromptProfile? _selectedPromptProfile = AppConfig.Instance.GetSelectedPromptProfile();

        [ObservableProperty]
        private string _questionText = string.Empty;

        public MainWindowViewModel()
        {
            ApiRequestManager.Instance.RegisterProgressReceiver(this);
        }

        [RelayCommand]
        private async Task SendQuestion()
        {
            if (string.IsNullOrWhiteSpace(QuestionText))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var text = QuestionText;
            QuestionText = string.Empty;
            await SubmitMessageAsync(text, ChatMessages.Count == 0);
        }

        [RelayCommand]
        private static void OpenSettingWindow()
        {
            var settingWindow = new SettingWindow();
            settingWindow.Owner = System.Windows.Application.Current.MainWindow;  // 所有者を明示
            settingWindow.ShowDialog();  // モーダル表示
        }

        [RelayCommand]
        private static void OpenPromptEditorWindow()
        {
            var promptEditorWindow = new Views.PromptEditorWindow();
            promptEditorWindow.Owner = System.Windows.Application.Current.MainWindow;
            promptEditorWindow.ShowDialog();
        }

        public async Task<string> SubmitMessageAsync(string text, bool resetConversation)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var instance = ApiRequestManager.Instance;
            if (resetConversation)
            {
                instance.ClearMessages();
                ChatMessages.Clear();
            }

            ChatMessages.Add(new ChatMessage("user", "あなた", text));
            instance.AddUserMessage(text);

            _streamingMessage = new ChatMessage("assistant", "AI", string.Empty);
            ChatMessages.Add(_streamingMessage);

            var result = await instance.RequestTranslation();
            if (_streamingMessage != null)
            {
                _streamingMessage.Text = result;
                _streamingMessage = null;
            }

            return result;
        }

        public async Task<string> SubmitImageMessageAsync(byte[] imageBytes, bool resetConversation)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return string.Empty;

            var instance = ApiRequestManager.Instance;
            if (resetConversation)
            {
                instance.ClearMessages();
                ChatMessages.Clear();
            }

            const string displayMessage = "";
            var imageSource = CreateImageSource(imageBytes);
            ChatMessages.Add(new ChatMessage("user", "あなた", displayMessage, imageSource));

            _streamingMessage = new ChatMessage("assistant", "AI", string.Empty);
            ChatMessages.Add(_streamingMessage);

            var result = await instance.RequestImageQuestion(imageBytes, displayMessage);
            if (_streamingMessage != null)
            {
                _streamingMessage.Text = result;
                _streamingMessage = null;
            }

            return result;
        }

        private static ImageSource? CreateImageSource(byte[] imageBytes)
        {
            try
            {
                using var stream = new MemoryStream(imageBytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        partial void OnSelectedAiModelChanged(AiModel value)
        {
            AppConfig.Instance.SelectedAiModel = value;
        }

        partial void OnSelectedPromptProfileChanged(PromptProfile? value)
        {
            if (value == null)
                return;

            AppConfig.Instance.SelectedPromptId = value.Id;
        }
    }
}
