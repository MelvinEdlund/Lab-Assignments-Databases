using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MusicLibrary.Models;

[Table("genres", Schema = "music")]
public partial class Genre
{
    [Key]
    public int GenreId { get; set; }

    [StringLength(120)]
    public string? Name { get; set; }

    [InverseProperty("Genre")]
    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
}
