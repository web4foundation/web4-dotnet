using static Web4.Dom.Events.Aliases.Subsets;

namespace Web4.Dom.Events;

public interface IPointerEvent
    : IMouseEvent, Pointer, Angles, WidthHeight, Pressures, Tilts
{
}
