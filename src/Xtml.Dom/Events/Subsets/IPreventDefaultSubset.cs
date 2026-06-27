namespace Web4.Dom.Events.Subsets;

public interface IPreventDefaultSubset : ISubset
{
    const string TRIM = "preventDefault";

    /// <summary>
    /// Cancels the event (if it is cancelable).
    /// Important difference in Web4: preventDefault() is called first thing
    /// on the developer's behalf before the rest of the event handler is executed.
    /// This is necessary since it's a synchronous API tring to execute remotely.
    /// </summary>
    void PreventDefault()
    {
    }
}