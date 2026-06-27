namespace Web4.Dom.Events.Subsets;

public interface ITargetSubset<T> : ISubset, IViewSubset
{
    new const string TRIM = "target";

    /// <summary>
    /// A reference to the object to which the event was originally dispatched.
    /// </summary>
    EventTarget<T> Target { get; }
}