using CommunityToolkit.Mvvm.ComponentModel;
using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Wpf.ViewModels;

public partial class SearchResultViewModel : ObservableObject
{
    public SearchResult SearchResult { get; }

    public SearchResultViewModel(SearchResult searchResult)
    {
        SearchResult = searchResult;
    }

    public string ArtistsText => string.Join(", ", SearchResult.TrackInfo.Artists);
}