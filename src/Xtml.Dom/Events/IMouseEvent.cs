using static Xtml.Dom.Events.Aliases.Subsets;

namespace Xtml.Dom.Events;

public interface IMouseEvent
    : IUIEvent, Buttons, Coordinates, Modifiers, RelatedTarget
{
}
