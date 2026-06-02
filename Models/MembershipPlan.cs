namespace GestionalePalestra.Models;

public sealed class MembershipPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Months { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}
