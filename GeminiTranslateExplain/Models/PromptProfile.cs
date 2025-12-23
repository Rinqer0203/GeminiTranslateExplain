using CommunityToolkit.Mvvm.ComponentModel;

namespace GeminiTranslateExplain.Models
{
    public partial class PromptProfile : ObservableObject
    {
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString("N");

        [ObservableProperty]
        private string _name = "デフォルト";

        [ObservableProperty]
        private string _instruction = string.Empty;
    }
}
