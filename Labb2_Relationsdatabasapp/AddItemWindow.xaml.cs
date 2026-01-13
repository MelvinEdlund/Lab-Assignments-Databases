using System;
using System.Linq;
using System.Windows;
using MusicLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace MusicLibrary
{
    public partial class AddItemWindow : Window
    {
        private readonly int? _editArtistId;

        public AddItemWindow()
        {
            InitializeComponent();
        }

        public AddItemWindow(Artist artistToEdit) : this()
        {
            _editArtistId = artistToEdit?.ArtistId;

            Title = "Edit Artist";
            ConfirmButton.Content = "Save";

            ArtistTextBox.Text = artistToEdit?.Name ?? string.Empty;

            // Edit mode: only allow editing artist name
            AlbumLabel.Visibility = Visibility.Collapsed;
            AlbumTextBox.Visibility = Visibility.Collapsed;
            TrackLabel.Visibility = Visibility.Collapsed;
            TrackTextBox.Visibility = Visibility.Collapsed;
            LengthLabel.Visibility = Visibility.Collapsed;
            LengthTextBox.Visibility = Visibility.Collapsed;
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (_editArtistId.HasValue)
            {
                var newName = ArtistTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("Artist name is required.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    await using var db = new MusicContext();

                    var artist = await db.Artists.FirstOrDefaultAsync(a => a.ArtistId == _editArtistId.Value);
                    if (artist == null)
                    {
                        MessageBox.Show("Artist not found.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    artist.Name = newName;
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while updating artist: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Owner is MainWindow ownerMain)
                {
                    ownerMain.ReloadArtists();
                }

                Close();
                return;
            }

            var artistName = ArtistTextBox.Text.Trim();
            var albumTitle = AlbumTextBox.Text.Trim();
            var trackName = TrackTextBox.Text.Trim();
            var lengthText = LengthTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(artistName) ||
                string.IsNullOrWhiteSpace(albumTitle) ||
                string.IsNullOrWhiteSpace(trackName))
            {
                MessageBox.Show("Artist, Album and Track name are required.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int milliseconds = 0;
            if (!string.IsNullOrEmpty(lengthText))
            {
                if (TimeSpan.TryParseExact(lengthText, @"m\:ss", null, out var ts) ||
                    TimeSpan.TryParse(lengthText, out ts))
                {
                    milliseconds = (int)ts.TotalMilliseconds;
                }
            }

            try
            {
                await using var db = new MusicContext();

                var nextArtistId = (await db.Artists.AnyAsync() ? await db.Artists.MaxAsync(a => a.ArtistId) : 0) + 1;
                var nextAlbumId = (await db.Albums.AnyAsync() ? await db.Albums.MaxAsync(a => a.AlbumId) : 0) + 1;
                var nextTrackId = (await db.Tracks.AnyAsync() ? await db.Tracks.MaxAsync(t => t.TrackId) : 0) + 1;

                var artist = await db.Artists.FirstOrDefaultAsync(a => a.Name == artistName);
                if (artist == null)
                {
                    artist = new Artist
                    {
                        ArtistId = nextArtistId,
                        Name = artistName
                    };
                    db.Artists.Add(artist);
                }

                var album = await db.Albums.FirstOrDefaultAsync(a => a.Title == albumTitle && a.ArtistId == artist.ArtistId);
                if (album == null)
                {
                    album = new Album
                    {
                        AlbumId = nextAlbumId,
                        Title = albumTitle,
                        ArtistId = artist.ArtistId
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
                MessageBox.Show($"Error while saving track: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Owner is MainWindow ownerMain2)
            {
                ownerMain2.ReloadArtists();
            }

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
