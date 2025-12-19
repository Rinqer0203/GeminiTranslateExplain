using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace GeminiTranslateExplain.Models
{
    public partial class ChatMessage : ObservableObject
    {
        public string Role { get; }
        public string DisplayName { get; }

        [ObservableProperty]
        private string _text;

        [ObservableProperty]
        private ImageSource? _imageSource;

        public ChatMessage(string role, string displayName, string text, ImageSource? imageSource = null)
        {
            Role = role;
            DisplayName = displayName;
            _text = text;
            _imageSource = imageSource;
        }
    }
}
