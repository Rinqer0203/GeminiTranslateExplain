# Project Overview

GeminiTranslateExplain は、Windows 向けの WPF デスクトップアプリです。クリップボード、グローバルショートカット、スクリーンショットをトリガーにして、Gemini / OpenAI へ問い合わせ、翻訳・説明・任意プロンプトの結果を表示します。

## 構成

- `GeminiTranslateExplain.sln`: ソリューション。
- `GeminiTranslateExplain/GeminiTranslateExplain.csproj`: WPF アプリ本体。`net8.0-windows`、`UseWPF`、`UseWindowsForms`、COM 参照を使用します。
- `GeminiTranslateExplain/Views`: WPF の `Window` / XAML。`MainWindow`、`SettingWindow`、`SimpleResultWindow`、`PromptEditorWindow` など。
- `GeminiTranslateExplain/ViewModels`: CommunityToolkit.Mvvm ベースの ViewModel。`[ObservableProperty]`、`[RelayCommand]` を使います。
- `GeminiTranslateExplain/Models`: `AppConfig`、AIモデル定義、プロンプト、チャットメッセージなど。
- `GeminiTranslateExplain/Services`: API呼び出し、クリップボード監視、ホットキー、ウィンドウ制御、画像ログ保存などのアプリロジック。
- `GeminiTranslateExplain/Services/ApiClients`: Gemini / OpenAI のストリーミングAPIクライアント。
- `README.md`: ユーザー向け概要。
- `AGENTS.md`: AIエージェント向け作業ルール。

## 主要な設計メモ

- 設定は `AppConfig.Instance` で管理し、`appconfig.json` に保存します。`appconfig.json` は exe と同じディレクトリへ配置される想定です。
- AIモデルは `AiModel(Name, Type)` で表現します。Gemini のモデル名は API URL 側で `models/{modelName}` として使うため、設定には `models/` なしで保存します。
- MainWindow のモデル一覧は UI から追加できるため、ViewModel 側では更新可能なコレクションとして扱います。
- 画像送信時は `ImageLogService` で exe と同じディレクトリ配下の `log` フォルダへ送信画像を保存します。
- SimpleResultWindow モードでは MainWindow と同時に表へ出ないよう、MainWindow を隠す挙動があります。ウィンドウ表示まわりを変更する時は `WindowManager` と `WindowUtilities` を確認してください。

## ビルド・検証の注意

- このプロジェクトは COM 参照を含むため、通常の `dotnet build GeminiTranslateExplain.sln` では `ResolveComReference` が失敗することがあります。
- 検証ビルドは Visual Studio の MSBuild を使ってください。

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' GeminiTranslateExplain.sln /p:OutputPath=bin\Verify\ /p:UseSharedCompilation=false
```

- `dotnet build` を実行すると、環境によってリポジトリ直下に `.dotnet/` が作られることがあります。これは不要な生成物なのでコミットしないでください。
- ネットワーク制限下では NuGet 脆弱性データ取得の `NU1900` 警告が出ることがあります。ビルド成功可否はエラー有無で判断してください。
- WPF の XAML で `Window` を追加すると、生成される partial class は public になります。コンストラクタ引数に渡す ViewModel も public にしないと `CS0051` になる場合があります。
- MaterialDesign のアイコンボタンは既存スタイル `MaterialDesignIconForegroundButton` と `materialDesign:PackIcon` を使うと既存UIに馴染みます。

---

# Implementation & Quality Policy
実装に関しては、以下を **必ず遵守** してください。

## 実装・検証
- 新規実装または修正したコードは **必ず実行し、動作確認を行う**
- 想定した入力・出力・挙動と一致することを確認する
- エラーや想定外の挙動が発生した場合は、原因を調査し修正する

---

# General Policy
- **Thinking:** English
- **Code identifiers:** English
- **User explanation:** Japanese
- **Code comments:** Japanese
- **Documentation:** Japanese
- **File reading** Use UTF-8 for all text file operations unless another encoding is explicitly required.

# GitHub Operation Policy
- **Commit messages:** Japanese
- **Pull Requests:** Japanese
- **Code Reviews:** Japanese
- **Branch names:** English

Always ensure consistency across all outputs according to these policies.
