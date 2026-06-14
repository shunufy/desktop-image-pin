# Codex for Open Source Application Draft

Each response below is under 500 characters per language. Claims are limited to repository-visible facts.

## 1. Why This Repository Qualifies

**日本語**

Desktop Image Pinは、画像を透明・枠なしの独立ウィンドウとしてWindowsデスクトップへ配置するMITライセンスの公開OSSです。通常の画像ビューアと異なり、画像ごとの移動、縦横別拡縮、前後関係、クリック透過、変形、複製、状態復元を提供します。.NET 8/WPFで構築し、英日README、テスト、CI、セキュリティ方針、ロードマップを公開しています。

**English**

Desktop Image Pin is a public MIT-licensed Windows utility that places images on the desktop as independent transparent, borderless windows. Unlike a conventional viewer, it supports per-image movement, independent X/Y scaling, layering, click-through, transforms, duplication, and restoration. The .NET 8/WPF repository includes English/Japanese docs, tests, CI, a security policy, and a roadmap.

## 2. Planned Use Of API Credits

**日本語**

APIクレジットは、IssueやPRの要約、変更影響の確認、テストケース案、リリースノートと英日ドキュメントの整合確認、Windows固有コードのレビュー補助に使用する予定です。ユーザー画像やローカル保存パスをAPIへ送る機能は追加せず、開発・保守作業の品質向上に限定して活用します。

**English**

API credits would support issue and PR summaries, change-impact review, test-case generation, release notes, English/Japanese documentation consistency, and review assistance for Windows-specific code. We do not plan to add a feature that sends user images or local saved paths to an API; credits would be used for repository development and maintenance quality.

## 3. Anything Else

**日本語**

実行時の外部NuGet依存はなく、同梱する第三者画像・カスタムフォント・アイコンもありません。状態はローカルJSONへ保存し、テレメトリやアカウント機能は持ちません。小規模ツールとして依存を抑え、挙動の分かりやすさ、Windows上での実動作確認、公開Issueに基づく継続保守を優先します。

**English**

The app has no runtime third-party NuGet dependencies and bundles no third-party images, custom fonts, or icon files. State remains in local JSON, with no telemetry or account system. As a focused utility, the project prioritizes a small dependency surface, understandable behavior, real Windows verification, and continued maintenance through public issues.

## 4. Progress Since The Previous Application

**日本語**

前回応募後の公開履歴として、画像ごとのクリック透過、透明度、回転・反転、クリップボード/URL取り込み、表示枚数、名前付きURLキャッシュを追加しました。さらに永続化・変換・取り込み判定の自動テスト、GitHub Actions、英日README、開発ログ、ロードマップ、Issue/PRテンプレート、依存ライセンス記録を整備しました。利用者数は推測していません。

**English**

Since the previous application, the public history added per-image click-through, opacity, rotation/flips, clipboard and URL import, image count, and named URL caching. The repository now also includes automated persistence/transform/import tests, GitHub Actions, English/Japanese READMEs, a development log, roadmap, issue/PR templates, and dependency-license records. No user-count claims are made.
