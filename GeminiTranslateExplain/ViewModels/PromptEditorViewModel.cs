using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;
using System.Collections.ObjectModel;

namespace GeminiTranslateExplain.ViewModels
{
    public partial class PromptEditorViewModel : ObservableObject
    {
        public ObservableCollection<PromptProfile> PromptProfiles { get; } = PromptStore.Instance.PromptProfiles;

        [ObservableProperty]
        private PromptProfile? _selectedPromptProfile;

        [ObservableProperty]
        private string _promptName = string.Empty;

        [ObservableProperty]
        private string _promptInstruction = string.Empty;

        public PromptEditorViewModel()
        {
            SelectedPromptProfile = PromptStore.Instance.GetSelectedPromptProfile();
            if (SelectedPromptProfile != null)
            {
                PromptName = SelectedPromptProfile.Name;
                PromptInstruction = SelectedPromptProfile.Instruction;
            }
        }

        public void OnClosed()
        {
            PromptStore.Instance.SaveAllProfiles();
        }

        partial void OnSelectedPromptProfileChanged(PromptProfile? value)
        {
            if (value == null)
                return;

            PromptStore.Instance.SetSelectedPromptProfile(value);
            PromptName = value.Name;
            PromptInstruction = value.Instruction;
        }

        partial void OnPromptNameChanged(string value)
        {
            if (SelectedPromptProfile == null)
                return;

            SelectedPromptProfile.Name = value;
        }

        partial void OnPromptInstructionChanged(string value)
        {
            if (SelectedPromptProfile == null)
                return;

            SelectedPromptProfile.Instruction = value;
        }

        [RelayCommand]
        private void AddPromptProfile()
        {
            var profile = PromptStore.Instance.AddPromptProfile("新しいプロンプト");
            SelectedPromptProfile = profile;
        }

        [RelayCommand]
        private void RemovePromptProfile()
        {
            if (SelectedPromptProfile == null || PromptProfiles.Count <= 1)
                return;

            var index = PromptProfiles.IndexOf(SelectedPromptProfile);
            PromptStore.Instance.RemovePromptProfile(SelectedPromptProfile);

            if (PromptProfiles.Count == 0)
                return;

            if (index >= PromptProfiles.Count)
                index = PromptProfiles.Count - 1;

            SelectedPromptProfile = PromptProfiles[index];
        }
    }
}
