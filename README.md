# GeminiTranslateExplain

Windows向けの翻訳・Q&Aツール

## 概要
**GeminiTranslateExplain** は、WPF（.NET）で開発されたWindows用アプリケーションです。  
テキストを選択して `Ctrl` + `C` + `C`（ダブルコピー）するだけで即座に翻訳ウィンドウが表示され、結果を確認できます。  
Gemini（Google）や OpenAI のAPIを利用し、ユーザー自身でカスタマイズ可能なプロンプトを通じて柔軟な翻訳・質問応答が可能です。

![image](https://github.com/user-attachments/assets/27a5352b-cfb6-4bba-b100-6af3ebb775fa)

## 主な機能
- **ダブルコピーで即時翻訳**  
  選択テキストを `Ctrl+C, C` で翻訳ウィンドウがポップアップ  
- **Gemini / OpenAI のモデル切り替え**  
- **プロンプトカスタマイズ**  
  - 翻訳指示用テンプレートを自由に編集・保存可能  
- **追加質問**  
- **システムトレイ常駐・自動起動対応**

![image](https://github.com/user-attachments/assets/bea5e996-41f8-4b2d-a1dc-954f59117859)


## 必要環境
- Windows 10 / 11  
- .NET 8.0 以降  
- Gemini APIキー（Google Gemini を利用する場合）  
- OpenAI APIキー（OpenAI を利用する場合）

## インストール
1. [リリースページ](https://github.com/Rinqer0203/GeminiTranslateExplain/releases)から`GeminiTranslateExplain.exe` をダウンロード
2. GeminiTranslateExplain.exeを実行
3. 設定画面でAPIキーを登録  

![image](https://github.com/user-attachments/assets/80fcdcd9-e120-4ee8-8481-66141aa6df3a)

## 使い方
1. 翻訳したいテキストを選択  
2. `Ctrl` + `C` + `C` を押す  
3. ウィンドウに翻訳結果が表示される  
4. 必要に応じて追加質問を入力し、AIに質問する

## ライセンス
MIT License  
