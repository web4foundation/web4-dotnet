using Web4.Dom;

namespace Web4.WebSocket;

// Instead of a new-ing up another class, save an instantiation 
// and explicitly implement on Bridge.
public partial class Bridge : IWindow
{
    public IWindow Window => this;

    IConsole IWindow.Console => this;

    IDocument IWindow.Document => this;

    IWindow IWindow.Window => this;

    void IWindow.Alert(string message)
    {
        JsonRpc.WriteNotification("window.alert", message);
    }

    void IWindow.Focus()
    {
        JsonRpc.WriteNotification("window.focus");
    }

    Task<string> IWindow.Prompt(string? message, string? defaultValue)
    {
        throw new NotImplementedException();
    }
}