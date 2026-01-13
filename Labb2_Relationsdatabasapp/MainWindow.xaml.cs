using Microsoft.EntityFrameworkCore;
using MusicLibrary.Models;
using MusicLibrary.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MusicLibrary;

public partial class MainWindow : Window
{
    public static readonly RoutedUICommand AddArtistRoutedCommand = new("Add Artist", nameof(AddArtistRoutedCommand), typeof(MainWindow));
    public static readonly RoutedUICommand EditArtistRoutedCommand = new("Edit Artist", nameof(EditArtistRoutedCommand), typeof(MainWindow));
    public static readonly RoutedUICommand RemoveArtistRoutedCommand = new("Remove Artist", nameof(RemoveArtistRoutedCommand), typeof(MainWindow));
    public static readonly RoutedUICommand AddAlbumRoutedCommand = new("Add Album", nameof(AddAlbumRoutedCommand), typeof(MainWindow));
    public static readonly RoutedUICommand EditTrackRoutedCommand = new("Edit Track", nameof(EditTrackRoutedCommand), typeof(MainWindow));
    public static readonly RoutedUICommand ToggleFullscreenRoutedCommand = new("Toggle Fullscreen", nameof(ToggleFullscreenRoutedCommand), typeof(MainWindow));
    public static readonly RoutedUICommand ExitRoutedCommand = new("Exit", nameof(ExitRoutedCommand), typeof(MainWindow));

    private readonly MusicViewModel _vm = new();

    private bool _isFullscreen;
    private WindowState _prevWindowState;
    private WindowStyle _prevWindowStyle;
    private ResizeMode _prevResizeMode;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        CommandBindings.Add(new CommandBinding(AddArtistRoutedCommand, (_, _) => OpenAddItemWindow()));
        CommandBindings.Add(new CommandBinding(EditArtistRoutedCommand, EditArtist_Executed, EditArtist_CanExecute));
        CommandBindings.Add(new CommandBinding(RemoveArtistRoutedCommand, RemoveArtist_Executed, RemoveArtist_CanExecute));
        CommandBindings.Add(new CommandBinding(AddAlbumRoutedCommand, AddAlbum_Executed, AddAlbum_CanExecute));
        CommandBindings.Add(new CommandBinding(EditTrackRoutedCommand, EditTrack_Executed, EditTrack_CanExecute));
        CommandBindings.Add(new CommandBinding(ToggleFullscreenRoutedCommand, (_, _) => ToggleFullscreen()));
        CommandBindings.Add(new CommandBinding(ExitRoutedCommand, (_, _) => Close()));

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
        await LoadArtistsAsync();
    }

    public void ReloadArtists() => _ = LoadArtistsAsync();

    private async Task LoadArtistsAsync()
    {
        await using var db = new MusicContext();

        var artists = await db.Artists
            .Include(artist => artist.Albums)
            .ThenInclude(album => album.Tracks)
            .ToListAsync();

        myTreeView.ItemsSource = new ObservableCollection<Artist>(artists);
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MusicViewModel vm && e.NewValue is MusicLibrary.Models.Track track)
            vm.SelectedLibraryTrack = track;

        CommandManager.InvalidateRequerySuggested();
    }

    private async void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 20 && DataContext is MusicViewModel vm)
            await vm.LoadMoreTracksAsync();
    }

    private void RowHeader_DeleteClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRowHeader header || header.DataContext is not MusicLibrary.Models.Track track || DataContext is not MusicViewModel vm)
            return;

        vm.SelectedPlaylistTrack = track;

        if (vm.RemoveTrackFromPlaylistCommand.CanExecute(null))
            vm.RemoveTrackFromPlaylistCommand.Execute(null);
    }

    private void OpenAddItemWindow_Click(object sender, RoutedEventArgs e) => OpenAddItemWindow();

    private void ToggleFullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void AddArtist_Click(object sender, RoutedEventArgs e) => OpenAddItemWindow();

    private void OpenAddItemWindow()
    {
        new AddItemWindow { Owner = this }.Show();
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _prevWindowState = WindowState;
            _prevWindowStyle = WindowStyle;
            _prevResizeMode = ResizeMode;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
        }
        else
        {
            WindowStyle = _prevWindowStyle;
            ResizeMode = _prevResizeMode;
            WindowState = _prevWindowState;
            _isFullscreen = false;
        }
    }

    private void EditArtist_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = myTreeView?.SelectedItem is Artist;

    private void EditArtist_Executed(object? sender, ExecutedRoutedEventArgs? e)
    {
        if (myTreeView.SelectedItem is not Artist artist)
            return;

        new AddItemWindow(artist) { Owner = this }.Show();
    }

    private void RemoveArtist_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = myTreeView?.SelectedItem is Artist;

    private async void RemoveArtist_Executed(object? sender, ExecutedRoutedEventArgs? e)
    {
        if (myTreeView.SelectedItem is not Artist selected)
        {
            MessageBox.Show("Select an artist in the tree first.", "Music Library", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"This will permanently delete artist '{selected.Name}' and ALL related albums and tracks.\n\nContinue?",
            "Remove Artist",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            using var db = new MusicContext();
            using var tx = await db.Database.BeginTransactionAsync();

            var artist = await db.Artists
                .Include(a => a.Albums)
                .ThenInclude(al => al.Tracks)
                .FirstOrDefaultAsync(a => a.ArtistId == selected.ArtistId);

            if (artist == null)
                return;

            var trackIds = artist.Albums.SelectMany(a => a.Tracks).Select(t => t.TrackId).ToList();

            if (trackIds.Count > 0)
            {
                await db.PlaylistTracks
                    .Where(pt => trackIds.Contains(pt.TrackId))
                    .ExecuteDeleteAsync();

                var tracks = await db.Tracks
                    .Where(t => trackIds.Contains(t.TrackId))
                    .ToListAsync();

                db.Tracks.RemoveRange(tracks);
            }

            if (artist.Albums.Count > 0)
                db.Albums.RemoveRange(artist.Albums);

            db.Artists.Remove(artist);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            await LoadArtistsAsync();
        }
        catch (Exception ex)
        {
            var details = ex.InnerException?.Message is { Length: > 0 }
                ? $"{ex.Message}\n\nInner: {ex.InnerException.Message}"
                : ex.Message;

            MessageBox.Show($"Failed to remove artist: {details}", "Music Library", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddAlbum_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = myTreeView?.SelectedItem is Artist;

    private void AddAlbum_Executed(object? sender, ExecutedRoutedEventArgs? e)
    {
        if (myTreeView.SelectedItem is not Artist artist)
            return;

        new AddAlbumWindow(artist) { Owner = this }.Show();
    }

    private void EditTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = myTreeView?.SelectedItem is MusicLibrary.Models.Track;

    private void EditTrack_Executed(object? sender, ExecutedRoutedEventArgs? e)
    {
        if (myTreeView.SelectedItem is not MusicLibrary.Models.Track track)
            return;

        new EditTrackWindow(track) { Owner = this }.Show();
    }
}
