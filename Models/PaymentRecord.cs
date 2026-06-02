namespace GestionalePalestra.Models;

public sealed class PaymentRecord
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string Method { get; set; } = "Contanti";
    public string Note { get; set; } = string.Empty;
}
