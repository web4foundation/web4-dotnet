namespace Web4.Dom;

public interface IDocument
{
    IWindow DefaultView { get; }
    string? Title { get; set; }
}
