using Microsoft.EntityFrameworkCore;
using MusicLibrary.Models;
using System;
using System.Windows;

namespace MusicLibrary;

public partial class AddAlbumWindow : Window
{
    private readonly int _artistId;

    public AddAlbumWindow(Artist selectedArtist)
    {
        InitializeComponent();

        ArgumentNullException.ThrowIfNull(selectedArtist);

        _artistId = selectedArtist.ArtistId;
        Title = $"Add Album / Track ({selectedArtist.Name})";
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var albumTitle = AlbumTitleTextBox.Text.Trim();
        var trackName = TrackNameTextBox.Text.Trim();
        var lengthText = LengthTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(albumTitle) || string.IsNullOrWhiteSpace(trackName))
        {
            MessageBox.Show("Album title and track name are required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var milliseconds = 0;
        if (!string.IsNullOrWhiteSpace(lengthText) &&
            (TimeSpan.TryParseExact(lengthText, @"m\:ss", null, out var ts) || TimeSpan.TryParse(lengthText, out ts)))
        {
            milliseconds = (int)ts.TotalMilliseconds;
        }

        try
        {
            await using var db = new MusicContext();

            var nextAlbumId = (await db.Albums.AnyAsync() ? await db.Albums.MaxAsync(a => a.AlbumId) : 0) + 1;
            var nextTrackId = (await db.Tracks.AnyAsync() ? await db.Tracks.MaxAsync(t => t.TrackId) : 0) + 1;

            var album = await db.Albums.FirstOrDefaultAsync(a => a.Title == albumTitle && a.ArtistId == _artistId);
            if (album == null)
            {
                album = new Album
                {
                    AlbumId = nextAlbumId,
                    Title = albumTitle,
                    ArtistId = _artistId
                };
                db.Albums.Add(album);
            }

            db.Tracks.Add(new Track
            {
                TrackId = nextTrackId,
                Name = trackName,
                AlbumId = album.AlbumId,
                MediaTypeId = 1,
                Milliseconds = milliseconds,
                UnitPrice = 0.0
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error while saving album/track: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (Owner is MainWindow ownerMain)
            ownerMain.ReloadArtists();

        AlbumTitleTextBox.Clear();
        TrackNameTextBox.Clear();
        LengthTextBox.Clear();
        AlbumTitleTextBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
