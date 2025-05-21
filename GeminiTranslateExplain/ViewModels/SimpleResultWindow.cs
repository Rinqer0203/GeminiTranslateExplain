using CommunityToolkit.Mvvm.ComponentModel;

namespace GeminiTranslateExplain
{
    public partial class SimpleResultWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _translatedText = string.Empty;
    }
}
