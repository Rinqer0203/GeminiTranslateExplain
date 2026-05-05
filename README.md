# QuickExplain

Windows向けの翻訳・Q&Aツール

## 概要
**QuickExplain** は、WPF/.NET 8 で開発されたWindows用アプリケーションです。<br>
選択テキストを `Ctrl + C + C` で即時に翻訳し、結果を確認できます。<br>
Google / OpenAI のAPIを利用し、プロンプトをカスタマイズしながら翻訳や質問応答が可能です。

![image](https://github.com/user-attachments/assets/27a5352b-cfb6-4bba-b100-6af3ebb775fa)

## 主な機能
- **ダブルコピーで即時翻訳**  
  選択テキストを `Ctrl + C + C` で翻訳ウィンドウを表示
- **グローバルショートカット**  
  任意のショートカットで選択テキストを翻訳（設定で変更可能）
- **スクリーンショットで質問**  
  画面範囲を選択して画像をAIに送信（Google / OpenAI 対応）
- **モデル切り替え**  
  Google / OpenAI のモデルを選択
- **プロンプト管理**  
  複数のプロンプトを作成・編集・切り替え
- **チャットUI**  
  これまでの質問と回答を一覧で確認
- **テーマ切り替え**  
  ダーク / ライト / システムに対応
- **タスクトレイ常駐**  
  処理中・完了・失敗の状態をアイコンで表示
- **起動オプション**  
  システム起動時の自動起動、最小化起動に対応

![image](https://github.com/user-attachments/assets/bea5e996-41f8-4b2d-a1dc-954f59117859)

## 動作環境
- Windows 10 / 11
- .NET 8.0
- Google APIキー（Googleモデル利用時）
- OpenAI APIキー（OpenAIモデル利用時）

## インストール
1. [リリースページ](https://github.com/Rinqer0203/QuickExplain/releases)から `QuickExplain.exe` をダウンロード
2. `QuickExplain.exe` を実行
3. 設定画面でAPIキーを登録

![image](https://github.com/user-attachments/assets/80fcdcd9-e120-4ee8-8481-66141aa6df3a)

## 使い方
1. 翻訳したいテキストを選択
2. `Ctrl + C + C` を押す
3. ウィンドウに翻訳結果が表示される
4. 必要に応じて追加質問を入力してAIに質問

## デフォルトショートカット
- 即時翻訳（選択テキスト）: `Ctrl + J`
- スクリーンショット質問: `Ctrl + Alt + S`

※ ショートカットは設定画面から変更できます。

## ライセンス
MIT License
