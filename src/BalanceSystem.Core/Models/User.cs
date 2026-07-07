using FreeSql.DataAnnotations;

namespace BalanceSystem.Core.Models;

[Table(Name = "Users")]
public class User
{
    [Column(IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    [Column(StringLength = 50, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    [Column(StringLength = 256, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column(StringLength = 20, IsNullable = false)]
    public string Role { get; set; } = "Operator";

    public DateTime CreateTime { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
