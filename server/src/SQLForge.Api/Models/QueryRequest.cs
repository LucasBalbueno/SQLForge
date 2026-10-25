using System.ComponentModel.DataAnnotations;

namespace SQLForge.Api.Models;

public sealed class QueryRequest
{
    [Required, MinLength(1), MaxLength(5)]
    public List<string> Columns { get; set; } = [];
    
    [Required, MaxLength(20)]
    public List<QueryFilter> Filters { get; set; } = [];
    
    [Required, MaxLength(5)]
    public List<QueryOrder> OrderBy { get; set; } = [];
    
    // Paginação de consultas
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [AllowedValues(5, 10, 20, 30, ErrorMessage = "Escolha 5, 10, 20 ou 30 itens por página.")]
    public int PageSize { get; set; } = 10;
}