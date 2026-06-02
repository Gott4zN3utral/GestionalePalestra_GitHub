namespace GestionalePalestra.Models;

public sealed class Member
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;
}
