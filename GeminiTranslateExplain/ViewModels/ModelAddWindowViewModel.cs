using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using System.Collections.ObjectModel;

namespace GeminiTranslateExplain.ViewModels
{
    public partial class ModelAddWindowViewModel : ObservableObject
    {
        private readonly HashSet<AiModel> _existingModels;
        private readonly List<AiModel> _allModels = new();
        private int _loadVersion;

        public ObservableCollection<AiModel> FilteredModels { get; } = new();

        public AiModel? AddedModel { get; private set; }
        public event Action<bool?>? CloseRequested;

        [ObservableProperty]
        private string _modelSearchText = string.Empty;

        [ObservableProperty]
        private AiModel? _selectedModel;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public ModelAddWindowViewModel(IEnumerable<AiModel> existingModels)
        {
            _existingModels = existingModels.ToHashSet();
        }

        [RelayCommand]
        private async Task RefreshModelsAsync()
        {
            var loadVersion = ++_loadVersion;
            IsLoading = true;
            StatusMessage = "モデルを取得しています...";
            _allModels.Clear();
            FilteredModels.Clear();
            SelectedModel = null;
            ModelSearchText = string.Empty;

            try
            {
                var result = await ModelCatalogService.GetAllModelsAsync();
                if (loadVersion != _loadVersion)
                    return;

                _allModels.AddRange(result.Models);
                RefreshFilteredModels();

                if (_allModels.Count == 0)
                {
                    StatusMessage = result.Errors.Count == 0
                        ? "追加できるモデルが見つかりませんでした。"
                        : string.Join(Environment.NewLine, result.Errors);
                }
                else if (result.Errors.Count == 0)
                {
                    StatusMessage = $"{_allModels.Count} 件のモデルを取得しました。";
                }
                else
                {
                    StatusMessage = $"{_allModels.Count} 件のモデルを取得しました。一部の取得に失敗しました: {string.Join(" / ", result.Errors)}";
                }
            }
            catch (Exception ex)
            {
                if (loadVersion != _loadVersion)
                    return;

                StatusMessage = ex.Message;
            }
            finally
            {
                if (loadVersion == _loadVersion)
                {
                    IsLoading = false;
                    AddModelCommand.NotifyCanExecuteChanged();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddModel))]
        private void AddModel()
        {
            if (SelectedModel == null)
                return;

            AddedModel = SelectedModel.Value;
            CloseRequested?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(false);
        }

        private bool CanAddModel()
        {
            if (SelectedModel == null)
                return false;

            return _allModels.Contains(SelectedModel.Value)
                && !_existingModels.Contains(SelectedModel.Value);
        }

        partial void OnModelSearchTextChanged(string value)
        {
            if (SelectedModel != null && !string.Equals(value, SelectedModel.Value.Name, StringComparison.Ordinal))
                SelectedModel = null;

            RefreshFilteredModels();
            AddModelCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedModelChanged(AiModel? value)
        {
            if (value != null && !string.Equals(ModelSearchText, value.Value.Name, StringComparison.Ordinal))
                ModelSearchText = value.Value.Name;

            AddModelCommand.NotifyCanExecuteChanged();
        }

        private void RefreshFilteredModels()
        {
            var filter = NormalizeModelName(ModelSearchText);
            var models = _allModels
                .Where(model => string.IsNullOrWhiteSpace(filter) || model.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .Take(80)
                .ToArray();

            FilteredModels.Clear();
            foreach (var model in models)
            {
                FilteredModels.Add(model);
            }
        }

        private static string NormalizeModelName(string? name)
        {
            return name?.Trim() ?? string.Empty;
        }
    }
}
