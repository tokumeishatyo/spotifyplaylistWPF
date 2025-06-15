# Spotify Playlist Manager

Spotify プレイリスト管理アプリケーション（WPF）

## 現在の実装状況

### ✅ 完了済み機能

**PBI-01: Spotify認証機能**
- SpotifyAPI.Web を使用した本格的な OAuth 2.0 + PKCE 認証
- Windows Credential Manager でのトークン管理
- ログイン/ログアウト機能
- 認証状態に応じた画面遷移
- ユーザー情報の表示
- エラーハンドリングとリフレッシュトークン対応

**PBI-02 & PBI-03: プレイリスト・楽曲表示機能**
- プレイリスト一覧の階層表示（TreeView）
- 楽曲リストの展開・折りたたみ機能
- UI仮想化による大量データの高速表示
- ページング API による効率的なデータ取得
- マウスホイールスクロール対応
- プレイリスト画像・アルバム画像表示
- チェックボックスによる楽曲選択機能
- ローディングインジケーター表示

**PBI-04: アイテム選択機能**
- 3状態チェックボックス（プレイリスト：ON/OFF/中間状態）
- 親子チェックボックスの双方向連動機能
- Shift+クリックによる楽曲範囲選択
- Deleteボタンの選択状態による活性制御
- TreeView閉じた状態でのチェック状態保持
- ユーザビリティを考慮した2状態クリック動作

**PBI-05: アイテム削除機能**
- 確認ダイアログによる安全な削除操作
- プレイリスト全体削除 vs 楽曲個別削除の分岐ロジック
- 削除後のリアルタイムUI更新（トラック数・楽曲リスト）
- エラーハンドリングとユーザーフレンドリーなメッセージ
- 削除操作の不可逆性の明示

**PBI-06: テーマ切り替え機能**
- ライト/ダークテーマの切り替え機能
- ヘッダーのワンクリックテーマ切り替えボタン
- 現在のテーマを示すアイコン表示（☀️/🌙）
- アプリ全体の背景・文字色・UI要素の動的テーマ適用
- テーマ設定の永続化（次回起動時に復元）
- 選択状態に応じたDeleteボタンの表示/非表示制御

**基本アーキテクチャ**
- DLL分割アーキテクチャ（Core, Auth, Playlist, Theme, Wpf）
- MVVM パターン実装
- DI コンテナでのモジュール統合
- CommunityToolkit.Mvvm 使用
- 非同期処理とパフォーマンス最適化

### 🏗️ アーキテクチャ

```
SpotifyManager.exe (Wpf)
├── SpotifyManager.Core.dll     # 共通インターフェース
├── SpotifyManager.Auth.dll     # 認証機能
├── SpotifyManager.Playlist.dll # プレイリスト機能
└── SpotifyManager.Theme.dll    # テーマ機能
```

### 🚀 実行方法

**推奨方法（最新の実行ファイル）:**
1. 実行ファイルの場所：
   ```
   publish/SpotifyManager.exe
   ```
2. ダブルクリックで起動

**開発環境での実行:**
```bash
dotnet run --project src/SpotifyManager.Wpf/SpotifyManager.Wpf.csproj
```

**アプリケーションの使用方法:**
1. アプリケーション起動
2. ログイン画面で「Spotifyでログイン」ボタンをクリック
3. ブラウザでSpotify認証を完了
4. プレイリスト一覧が自動的に読み込まれる
5. プレイリストをクリックして楽曲リストを展開
6. チェックボックスで楽曲を選択（Shift+クリックで範囲選択）
7. プレイリストのチェックで配下全楽曲を一括選択
8. マウスホイールでスクロール可能
9. 選択した楽曲があるとDeleteボタンが表示される
10. ヘッダーのテーマボタンでライト/ダークテーマ切り替え

### 🎵 主な機能

**プレイリスト管理:**
- ユーザーのプレイリスト一覧表示
- プレイリスト画像とメタデータ表示
- 楽曲数カウント表示

**楽曲管理:**
- 楽曲一覧の遅延読み込み
- アルバム画像表示
- アーティスト名・再生時間表示
- 複数楽曲の選択機能（個別・範囲・一括選択）

**選択機能:**
- 3状態チェックボックス（チェック/未チェック/中間状態）
- 親子連動（プレイリスト⇔楽曲）
- Shift+クリックによる範囲選択
- TreeView未展開時の状態保持

**パフォーマンス:**
- 大量プレイリスト対応（UI仮想化）
- ページング API でメモリ効率化
- 非同期処理でUIブロッキング回避

**テーマ機能:**
- ライト/ダークテーマ対応
- ヘッダーボタンでワンクリック切り替え
- 現在のテーマを表すアイコン表示
- 全UI要素のテーマ対応
- テーマ設定の永続化

### 📝 注記

- Spotify Premium アカウントが必要
- 初回起動時にSpotify認証が必要
- 削除機能（選択したアイテムの削除）実装済み
- テーマ切り替え機能実装済み
- Client ID はBase64エンコードによる軽微な難読化を実装済み

### 🔧 開発コマンド

```bash
# ビルド
dotnet build

# 実行（開発環境）
dotnet run --project src/SpotifyManager.Wpf/SpotifyManager.Wpf.csproj

# 配布用実行ファイル作成
dotnet publish src/SpotifyManager.Wpf/SpotifyManager.Wpf.csproj -c Release -o publish --self-contained true -r win-x64
```

### 🛠️ 技術スタック

- **.NET 8.0** - アプリケーションフレームワーク
- **WPF** - UI フレームワーク
- **SpotifyAPI.Web** - Spotify Web API クライアント
- **CommunityToolkit.Mvvm** - MVVM フレームワーク
- **Microsoft.Extensions.DependencyInjection** - DI コンテナ
- **System.Security.Cryptography** - PKCE 認証

### 📦 次期実装予定機能

- **PBI-07:** 楽曲検索機能
- **PBI-08:** 重複楽曲検出・削除
- **PBI-09:** 設定画面
- **PBI-10:** プレイリスト作成・編集機能

### 🔒 セキュリティ対策

- **Client ID の難読化**
  - Base64エンコードによる軽微な暗号化
  - 変数名の意図隠蔽（AppConfig等）
  - 設定エラー時の適切な例外処理

- **認証セキュリティ**
  - PKCE (Proof Key for Code Exchange) 認証フロー
  - リフレッシュトークンの安全な保存（Windows Credential Manager）
  - Client Secretレス認証