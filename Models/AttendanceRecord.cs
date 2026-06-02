namespace GestionalePalestra.Models;

public sealed class AttendanceRecord
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; } = DateTime.Now;
}
