# Spotify Playlist Manager

Spotify プレイリスト管理アプリケーション（WPF）

## 現在の実装状況

### ✅ 完了済み機能

**PBI-01: Spotify認証機能**
- Windows Credential Manager でのトークン管理
- ログイン/ログアウト機能
- 認証状態に応じた画面遷移
- ユーザー情報の表示
- エラーハンドリング

**基本アーキテクチャ**
- DLL分割アーキテクチャ（Core, Auth, Playlist, Theme, Wpf）
- MVVM パターン実装
- DI コンテナでのモジュール統合
- CommunityToolkit.Mvvm 使用

### 🏗️ アーキテクチャ

```
SpotifyManager.exe (Wpf)
├── SpotifyManager.Core.dll     # 共通インターフェース
├── SpotifyManager.Auth.dll     # 認証機能
├── SpotifyManager.Playlist.dll # プレイリスト機能
└── SpotifyManager.Theme.dll    # テーマ機能
```

### 🚀 実行方法

1. 実行ファイルの場所：
   ```
   src/SpotifyManager.Wpf/bin/Debug/net8.0-windows/win-x64/SpotifyManager.exe
   ```

2. ダブルクリックで起動
3. ログイン画面が表示される
4. 「Spotifyでログイン」ボタンをクリック
5. 認証成功後、メイン画面に遷移

### 📝 注記

- 現在はデモ認証実装（実際のSpotify OAuth は次の段階で実装予定）
- プレイリスト一覧機能は未実装（画面遷移のみ対応）
- テーマ切り替え機能は基本実装済み

### 🔧 開発コマンド

```bash
# ビルド
dotnet build -r win-x64

# 実行
dotnet run --project src/SpotifyManager.Wpf/SpotifyManager.Wpf.csproj
```