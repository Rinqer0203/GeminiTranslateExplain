using CommunityToolkit.Mvvm.ComponentModel;

namespace GeminiTranslateExplain.Models
{
    public partial class ChatMessage : ObservableObject
    {
        public string Role { get; }
        public string DisplayName { get; }

        [ObservableProperty]
        private string _text;

        public ChatMessage(string role, string displayName, string text)
        {
            Role = role;
            DisplayName = displayName;
            _text = text;
        }
    }
}
