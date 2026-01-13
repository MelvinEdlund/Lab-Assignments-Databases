using Microsoft.EntityFrameworkCore;
using MusicLibrary.Models;
using System;
using System.Globalization;
using System.Windows;

namespace MusicLibrary;

public partial class EditTrackWindow : Window
{
    private readonly int _trackId;

    public EditTrackWindow(Track track)
    {
        InitializeComponent();

        ArgumentNullException.ThrowIfNull(track);
        _trackId = track.TrackId;

        Title = $"Edit Track ({track.Name})";

        TrackNameTextBox.Text = track.Name;
        LengthTextBox.Text = track.Duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);

        ArtistTextBlock.Text = track.ArtistName;
        AlbumTextBlock.Text = track.Album?.Title ?? string.Empty;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = string.Empty;

        var newName = TrackNameTextBox.Text.Trim();
        var lengthText = LengthTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            StatusTextBlock.Text = "Track name is required.";
            return;
        }

        if (!TimeSpan.TryParseExact(lengthText, @"m\:ss", CultureInfo.InvariantCulture, out var ts) &&
            !TimeSpan.TryParse(lengthText, CultureInfo.InvariantCulture, out ts))
        {
            StatusTextBlock.Text = "Length must be in format m:ss (e.g. 3:45).";
            return;
        }

        var ms = (int)ts.TotalMilliseconds;

        try
        {
            SaveButton.IsEnabled = false;

            await using var db = new MusicContext();

            var track = await db.Tracks.FirstOrDefaultAsync(t => t.TrackId == _trackId);
            if (track == null)
            {
                StatusTextBlock.Text = "Track not found.";
                return;
            }

            track.Name = newName;
            track.Milliseconds = ms;

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
            return;
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }

        if (Owner is MainWindow main)
        {
            main.ReloadArtists();
        }

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
