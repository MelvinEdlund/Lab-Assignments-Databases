using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MusicLibrary.Models;

[Keyless]
[Table("playlist_track", Schema = "music")]
public partial class PlaylistTrack
{
    public int PlaylistId { get; set; }

    public int TrackId { get; set; }

    [ForeignKey("PlaylistId")]
    public virtual Playlist Playlist { get; set; } = null!;

    [ForeignKey("TrackId")]
    public virtual Track Track { get; set; } = null!;
}
