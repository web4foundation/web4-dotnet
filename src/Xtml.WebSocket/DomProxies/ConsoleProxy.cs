using Web4.Dom;

namespace Web4.WebSocket;

// Instead of a new-ing up another class, save an instantiation 
// and explicitly implement on Bridge.
public partial class Bridge : IConsole
{
    public IConsole Console => this;

    // TODO: Tests still needed for each one.
    void IConsole.Assert(bool assertion, string message)
        => JsonRpc.WriteNotification("console.assert", assertion ? "true" : "false", message);
    void IConsole.Assert(bool assertion, string message, params Span<string> substitutions)
        => JsonRpc.WriteNotification("console.assert", assertion ? "true" : "false", message);
    void IConsole.Assert(bool assertion, object value)
        => JsonRpc.WriteNotification("console.assert", assertion ? "true" : "false", value);
    void IConsole.Assert(bool assertion, params Span<object> values)
        => JsonRpc.WriteNotification("console.assert", [assertion ? "true" : "false", ..values]);
    void IConsole.Clear()
        => JsonRpc.WriteNotification("console.clear");
    void IConsole.Count()
        => JsonRpc.WriteNotification("console.count");
    void IConsole.Count(object label)
        => JsonRpc.WriteNotification("console.count", label);
    void IConsole.CountReset()
        => JsonRpc.WriteNotification("console.reset");
    void IConsole.CountReset(object label)
        => JsonRpc.WriteNotification("console.reset", label);
    void IConsole.Debug(string message)
        => JsonRpc.WriteNotification("console.debug", message);
    void IConsole.Debug(string message, params Span<string> substitutions)
        => JsonRpc.WriteNotification("console.debug", message, substitutions);
    void IConsole.Debug(object value)
        => JsonRpc.WriteNotification("console.debug", value);
    void IConsole.Debug(params Span<object> values)
        => JsonRpc.WriteNotification("console.debug", values);
    void IConsole.Dir(object obj)
        => JsonRpc.WriteNotification("console.dir", obj);
    void IConsole.Dir(object obj, IConsole.DirOptions options)
        => throw new NotImplementedException();
    void IConsole.Error(string message)
        => JsonRpc.WriteNotification("console.error", message);
    void IConsole.Error(string message, params Span<string> substitutions)
        => JsonRpc.WriteNotification("console.error", message, substitutions);
    void IConsole.Error(object value)
        => JsonRpc.WriteNotification("console.error", value);
    void IConsole.Error(object value, params Span<object> values)
        => JsonRpc.WriteNotification("console.error", [value, ..values]);
    void IConsole.Group()
        => JsonRpc.WriteNotification("console.group");
    void IConsole.Group(object label)
        => JsonRpc.WriteNotification("console.group", label);
    void IConsole.GroupCollapsed()
        => JsonRpc.WriteNotification("console.groupCollapsed");
    void IConsole.GroupCollapsed(object label)
        => JsonRpc.WriteNotification("console.groupCollapsed", label);
    void IConsole.GroupCollapsed(string message, params Span<string> substitutions)
        => JsonRpc.WriteNotification("console.groupCollapsed", message, substitutions);
    void IConsole.GroupEnd()
        => JsonRpc.WriteNotification("console.groupEnd");
    void IConsole.Info(string message)
        => JsonRpc.WriteNotification("console.info", message);
    void IConsole.Info(string message, params Span<string> substitutions)
        => JsonRpc.WriteNotification("console.info", message, substitutions);
    void IConsole.Info(object value)
        => JsonRpc.WriteNotification("console.info", value);
    void IConsole.Info(object value, params Span<object> values)
        => JsonRpc.WriteNotification("console.info", [value, ..values]);
    void IConsole.Log(string message)
        => JsonRpc.WriteNotification("console.log", message);
    void IConsole.Log(string message, params Span<string> substitutions)
        => JsonRpc.WriteNotification("console.log", message, substitutions);
    void IConsole.Log(object value)
        => JsonRpc.WriteNotification("console.log", value);
    void IConsole.Log(object value, params Span<object> values)
        => JsonRpc.WriteNotification("console.log", [value, ..values]);
    void IConsole.Table(object obj)
        => JsonRpc.WriteNotification("console.table", obj);
    void IConsole.Table(object obj, string[] columns)
        => JsonRpc.WriteNotification("console.table", obj, columns);
    void IConsole.Time()
        => JsonRpc.WriteNotification("console.time");
    void IConsole.Time(object label)
        => JsonRpc.WriteNotification("console.time", label);
    void IConsole.TimeEnd()
        => JsonRpc.WriteNotification("console.timeEnd");
    void IConsole.TimeEnd(object label)
        => JsonRpc.WriteNotification("console.timeEnd", label);
    void IConsole.TimeLog()
        => JsonRpc.WriteNotification("console.timeLog");
    void IConsole.TimeLog(object label)
        => JsonRpc.WriteNotification("console.timeLog", label);
    void IConsole.TimeLog(object label, params Span<object> values)
        => JsonRpc.WriteNotification("console.timeLog", [label, ..values]);
    void IConsole.Trace()
        => JsonRpc.WriteNotification("console.trace");
    void IConsole.Trace(params Span<object> objects)
        => JsonRpc.WriteNotification("console.trace", objects);
    void IConsole.Warn(string message)
        => JsonRpc.WriteNotification("console.warn", message);
    void IConsole.Warn(string message, params Span<string> substitutions)
        => JsonRpc.WriteNotification("console.warn", message, substitutions);
    void IConsole.Warn(object value)
        => JsonRpc.WriteNotification("console.warn", value);
    void IConsole.Warn(object value, params Span<object> values)
        => JsonRpc.WriteNotification("console.warn", [value, ..values]);
}