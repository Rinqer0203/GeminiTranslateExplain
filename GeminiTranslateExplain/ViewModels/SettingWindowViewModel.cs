using CommunityToolkit.Mvvm.ComponentModel;

namespace GeminiTranslateExplain
{
    public partial class SettingWindowViewModel : ObservableObject
    {
        public List<WindowType> WindowTypeItems { get; }

        [ObservableProperty]
        private string _apiKey = AppConfig.Instance.ApiKey;

        [ObservableProperty]
        private WindowType _selectedResultWindowType;

        [ObservableProperty]
        private string _systemInstruction = AppConfig.Instance.SystemInstruction;

        [ObservableProperty]
        private string _customSystemInstruction = AppConfig.Instance.CustomSystemInstruction;

        public SettingWindowViewModel()
        {
            WindowTypeItems = Enum.GetValues(typeof(WindowType)).Cast<WindowType>().ToList();
            SelectedResultWindowType = AppConfig.Instance.SelectedResultWindowType;
        }

        public void OnClosed()
        {
            AppConfig.Instance.SaveConfigJson();
        }

        partial void OnApiKeyChanged(string value)
        {
            AppConfig.Instance.ApiKey = value;
        }

        partial void OnSelectedResultWindowTypeChanged(WindowType value)
        {
            AppConfig.Instance.SelectedResultWindowType = value;
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
