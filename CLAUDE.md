# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Spotify Playlist Management WPF Application - a Windows desktop app for managing Spotify playlists with bulk deletion capabilities. The project is currently in the documentation phase with comprehensive specifications in Japanese.

## Architecture Requirements

- **MVVM Pattern**: Strictly follow Model-View-ViewModel pattern with clear separation of concerns
- **Dependency Injection**: Use DI container (Microsoft.Extensions.DependencyInjection recommended)
- **No code-behind**: ViewModels handle all logic, Views only contain XAML bindings

## Key Technical Specifications

### Authentication
- Spotify OAuth with Authorization Code Flow + PKCE
- Store refresh tokens in Windows Credential Manager
- No Client Secret (desktop app security requirement)

### MVP Features (PBI-01 to PBI-06)
1. Spotify authentication (login/logout)
2. Display all user playlists
3. Display tracks within playlists
4. Multiple selection with checkboxes
5. Bulk deletion of selected items
6. Light/Dark theme switching

### Recommended Libraries
- SpotifyAPI-NET - Spotify API integration
- Microsoft.Extensions.DependencyInjection - DI container
- CommunityToolkit.Mvvm - MVVM helpers and base classes

## Development Commands

```bash
# Build the project
dotnet build

# Run the application  
dotnet run --project src/SpotifyManager.Wpf/SpotifyManager.Wpf.csproj

# Run tests
dotnet test

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

## Important Documentation

The `doc/` directory contains two critical Japanese documents:
- `要件定義書.md` - Requirements Definition with use cases and functional requirements
- `外部仕様書.md` - External Design Specification with UI mockups and technical details

Always refer to these documents when implementing features to ensure compliance with the defined specifications.

## Gradual editing
- "Make small changes and see results" → "Make next changes"
- This case is Scrum development. Implement only the items that have been instructed. When you have finished implementing it, report the finished product, wait for the next instruction, and do not move forward of your own free will.
- Dialogue will be conducted in Japanese