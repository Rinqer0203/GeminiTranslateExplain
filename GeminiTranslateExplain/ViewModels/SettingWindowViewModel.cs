using CommunityToolkit.Mvvm.ComponentModel;

namespace GeminiTranslateExplain
{
    public partial class SettingWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _apiKey = AppConfig.Instance.ApiKey;

        [ObservableProperty]
        private string _systemInstruction = AppConfig.Instance.SystemInstruction;

        [ObservableProperty]
        private string _customSystemInstruction = AppConfig.Instance.CustomSystemInstruction;

        partial void OnApiKeyChanged(string value)
        {
            AppConfig.Instance.ApiKey = value;
        }

        partial void OnSystemInstructionChanged(string value)
        {
            AppConfig.Instance.SystemInstruction = value;
        }

        partial void OnCustomSystemInstructionChanged(string value)
        {
            AppConfig.Instance.CustomSystemInstruction = value;
        }
    }
}
