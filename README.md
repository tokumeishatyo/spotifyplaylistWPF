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
6. チェックボックスで楽曲を選択
7. マウスホイールでスクロール可能

### 🎵 主な機能

**プレイリスト管理:**
- ユーザーのプレイリスト一覧表示
- プレイリスト画像とメタデータ表示
- 楽曲数カウント表示

**楽曲管理:**
- 楽曲一覧の遅延読み込み
- アルバム画像表示
- アーティスト名・再生時間表示
- 複数楽曲の選択機能

**パフォーマンス:**
- 大量プレイリスト対応（UI仮想化）
- ページング API でメモリ効率化
- 非同期処理でUIブロッキング回避

### 📝 注記

- Spotify Premium アカウントが必要
- 初回起動時にSpotify認証が必要
- プレイリストの削除・楽曲削除機能は次の段階で実装予定

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

- **PBI-04:** アイテム選択機能の拡張
- **PBI-05:** プレイリスト・楽曲削除機能
- **PBI-06:** ダーク/ライトテーマ切り替え機能