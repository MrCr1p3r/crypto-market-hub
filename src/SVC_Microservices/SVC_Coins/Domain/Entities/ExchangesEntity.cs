using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SVC_Coins.Domain.Entities;

/// <summary>
/// Represents an exchange entity.
/// </summary>
[Index(nameof(Name), IsUnique = true, Name = "UQ_Exchanges_Name")]
public class ExchangesEntity
{
    /// <summary>
    /// Gets or sets unique identifier for the exchange.
    /// </summary>
    [Required]
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets name of the exchange.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }
}
