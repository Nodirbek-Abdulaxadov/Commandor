namespace WebApplication1.Features;

public class Todo
{
    public int Id { get; set; } 
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}