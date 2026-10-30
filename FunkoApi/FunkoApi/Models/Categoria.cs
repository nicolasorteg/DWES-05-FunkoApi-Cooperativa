using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FunkoApi.Validators.Attributes;

namespace FunkoApi.Models;

public class Categoria {

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // no es autoincremental
    [YouTubeId] // id estilo yt
    public string Id { get; set; } = null!;

    [Required] 
    [MaxLength(100)] 
    public string Nombre { get; set; } = string.Empty;

    // metadatos
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Funko> Funkos { get; set; } = new List<Funko>(); // lista para bidireccionalidad
}