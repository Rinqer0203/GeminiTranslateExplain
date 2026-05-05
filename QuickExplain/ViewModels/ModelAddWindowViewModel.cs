using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickExplain.Models;
using QuickExplain.Services;
using System.Collections.ObjectModel;

namespace QuickExplain.ViewModels
{
    public partial class ModelAddWindowViewModel : ObservableObject
    {
        private readonly ObservableCollection<AiModel> _configuredModels;
        private readonly List<AiModel> _allModels = new();
        private int _loadVersion;

        public ObservableCollection<ModelEditItemViewModel> FilteredModels { get; } = new();

        public event Action? ModelsChanged;

        [ObservableProperty]
        private string _modelSearchText = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public ModelAddWindowViewModel(ObservableCollection<AiModel> configuredModels)
        {
            _configuredModels = configuredModels;
        }

        [RelayCommand]
        private async Task RefreshModelsAsync()
        {
            var loadVersion = ++_loadVersion;
            IsLoading = true;
            StatusMessage = "モデルを取得しています...";
            _allModels.Clear();
            FilteredModels.Clear();
            ModelSearchText = string.Empty;

            try
            {
                var result = await ModelCatalogService.GetAllModelsAsync();
                if (loadVersion != _loadVersion)
                    return;

                foreach (var model in _configuredModels.Concat(result.Models).Distinct())
                {
                    _allModels.Add(model);
                }

                RefreshFilteredModels();

                var statusLines = new List<string>
                {
                    $"{result.Models.Count} 件のモデルを取得しました。",
                    $"登録済み: {_configuredModels.Count} 件"
                };

                statusLines.AddRange(result.Statuses);
                if (_allModels.Count == 0)
                {
                    statusLines[0] = "利用可能なモデルが見つかりませんでした。";
                }

                foreach (var error in result.Errors)
                {
                    if (error.StartsWith("Ollama ", StringComparison.Ordinal))
                        continue;

                    statusLines.Add(error);
                }

                StatusMessage = string.Join(Environment.NewLine, statusLines);
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
                }
            }
        }

        private void AddModel(AiModel model)
        {
            if (_configuredModels.Contains(model))
                return;

            _configuredModels.Add(model);
            SaveModels();
            RefreshFilteredModels();
        }

        private void RemoveModel(AiModel model)
        {
            if (!_configuredModels.Contains(model))
                return;

            _configuredModels.Remove(model);
            SaveModels();
            RefreshFilteredModels();
        }

        private void SaveModels()
        {
            AppConfig.Instance.AIModels = _configuredModels.ToArray();
            AppConfig.Instance.SaveConfigJson();
            ModelsChanged?.Invoke();
        }

        partial void OnModelSearchTextChanged(string value)
        {
            RefreshFilteredModels();
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
                FilteredModels.Add(new ModelEditItemViewModel(
                    model,
                    _configuredModels.Contains(model),
                    AddModel,
                    RemoveModel));
            }
        }

        private static string NormalizeModelName(string? name)
        {
            return name?.Trim() ?? string.Empty;
        }
    }

    public partial class ModelEditItemViewModel : ObservableObject
    {
        private readonly Action<AiModel> _addModel;
        private readonly Action<AiModel> _removeModel;

        public ModelEditItemViewModel(
            AiModel model,
            bool isConfigured,
            Action<AiModel> addModel,
            Action<AiModel> removeModel)
        {
            Model = model;
            IsConfigured = isConfigured;
            _addModel = addModel;
            _removeModel = removeModel;
        }

        public AiModel Model { get; }

        public string Name => Model.Name;

        public AiType Type => Model.Type;

        public string ProviderName => Model.ProviderName;

        [ObservableProperty]
        private bool _isConfigured;

        [RelayCommand]
        private void Add()
        {
            _addModel(Model);
        }

        [RelayCommand]
        private void Remove()
        {
            _removeModel(Model);
        }
    }
}
