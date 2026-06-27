using static Web4.Dom.Events.Aliases.Subsets;

namespace Web4.Dom.Events;

public interface IUIEvent
    : IEvent, Detail, View
{
}
