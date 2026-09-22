using System.ComponentModel.DataAnnotations;

namespace EcShop.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string Provider { get; init; } = "Sqlite";

    [Required]
    public string ConnectionString { get; init; } = "Data Source=ecshop.db";
}
