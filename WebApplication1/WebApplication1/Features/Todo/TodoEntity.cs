namespace WebApplication1.Features;

public class TodoEntity
{
    public long Id { get; set; }
    public string Task { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}