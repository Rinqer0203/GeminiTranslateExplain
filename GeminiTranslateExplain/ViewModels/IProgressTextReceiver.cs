using GeminiTranslateExplain.Services;

namespace GeminiTranslateExplain.ViewModels
{
    /// <summary>
    /// <see cref="ApiManager"/>に登録するためのインターフェース
    /// </summary>
    public interface IProgressTextReceiver
    {
        /// <summary>
        /// 受信したコンテンツテキストが代入されるプロパティ
        /// </summary>
        string Text { set; }
    }
}
