using static Web4.Dom.Events.Aliases.Subsets;

namespace Web4.Dom.Events;

public interface IMouseEvent
    : IUIEvent, Buttons, Coordinates, Modifiers, RelatedTarget
{
}
