namespace GeminiTranslateExplain.Services
{
    public static class WindowManager
    {
        // Viewの型をキーとして、ViewとViewModelのペアを格納
        private static readonly Dictionary<Type, (object View, object ViewModel)> _windows = new();

        /// <summary>
        /// ViewとViewModelを登録する
        /// </summary>
        public static void Register<TView, TViewModel>(TView view, TViewModel viewModel)
            where TView : class
            where TViewModel : class
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            _windows[typeof(TView)] = (view, viewModel);
        }

        /// <summary>
        /// Viewを型指定で取得する
        /// </summary>
        public static TView? GetView<TView>() where TView : class
        {
            if (_windows.TryGetValue(typeof(TView), out var pair))
            {
                return pair.View as TView;
            }
            return null;
        }

        /// <summary>
        /// ViewModelを型指定で取得する（Viewの型に基づく）
        /// </summary>
        public static TViewModel? GetViewModel<TView, TViewModel>()
            where TView : class
            where TViewModel : class
        {
            if (_windows.TryGetValue(typeof(TView), out var pair))
            {
                return pair.ViewModel as TViewModel;
            }
            return null;
        }

        /// <summary>
        /// ViewとViewModelの登録を解除する
        /// </summary>
        public static void Unregister<TView>() where TView : class
        {
            _windows.Remove(typeof(TView));
        }

        /// <summary>
        /// 全ての登録情報をクリアする
        /// </summary>
        public static void Clear()
        {
            _windows.Clear();
        }
    }
}
