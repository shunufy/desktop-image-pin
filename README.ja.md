# Desktop Image Pin

[English README](README.md)

Desktop Image Pin は、通常のウィンドウ枠やタイトルバーを表示せず、好きな画像をWindowsデスクトップ上へ配置できるネタ／ユーティリティアプリです。

画像ごとに移動、縦横の拡大縮小、複製、変更、表示階層の切り替え、削除ができます。

## 主な機能

- 複数画像の同時表示
- 枠なし・背景透明の画像ウィンドウ
- 左ドラッグによる移動
- マウスホイールによる縦横比を維持した拡大縮小
- `Ctrl + マウスホイール` で横幅だけ変更
- `Alt + マウスホイール` で高さだけ変更
- 画像ごとに最前面・通常・最背面を設定
- 画像ごとのクリック透過ON/OFF
- 透明度を10～100%で調整
- Hubから左右90度回転・左右反転・上下反転
- 画像の変更・複製・削除
- Hubまたは画像ウィンドウへの複数ファイルのドラッグ＆ドロップ
- クリップボードの画像または画像ファイルから追加
- HTTP/HTTPSの画像URLから追加
- 画像パス、位置、縦横倍率、表示階層の保存と次回起動時の復元
- タスクトレイ常駐
- `Ctrl + Shift + H` でHubの表示・非表示を切り替え
- 大きな画像を初回表示時にデスクトップ作業領域の90%以内へ自動縮小
- Hubに現在表示中の画像枚数を表示

対応形式: PNG、JPEG、BMP、GIF、TIFF。GIFアニメーションは先頭フレームのみ表示します。

## 必要環境

- Windows 10 または Windows 11
- ソースからビルドする場合は .NET 8 SDK

## ソースから実行

```powershell
dotnet run
```

## ビルド

```powershell
dotnet build -c Release
```

Windows x64向け自己完結型単一EXE:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o publish/win-x64
```

## 操作

| 操作 | 入力 |
| --- | --- |
| 画像の移動 | 左ドラッグ |
| 縦横比を維持して拡大縮小 | マウスホイール |
| 横幅だけ変更 | `Ctrl + マウスホイール` |
| 高さだけ変更 | `Alt + マウスホイール` |
| 画像メニュー | 右クリック |
| Hubの表示切り替え | `Ctrl + Shift + H` |

## 保存データ

```text
%LocalAppData%\DesktopImagePin\images.json
```

画像パスと配置設定を保存します。クリップボード画像とURL画像は、次回復元できるよう `%LocalAppData%\DesktopImagePin\ImportedImages` に保存されます。URLからのダウンロード上限は25MBです。

## 注意点

- Hubの閉じるボタンはアプリ終了ではなく非表示です。
- 終了する場合はHubまたはタスクトレイの「Exit」を使用してください。
- 起動時に画像ファイルが見つからない場合、その画像は復元されません。
- クリック透過を有効にした画像は、Hubからクリック透過をOFFにすると再びマウス操作できます。

## ライセンス

MIT Licenseです。詳細は [LICENSE](LICENSE) を参照してください。
