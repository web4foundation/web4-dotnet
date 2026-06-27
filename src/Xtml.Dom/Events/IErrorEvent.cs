using static Xtml.Dom.Events.Aliases.Subsets;

namespace Xtml.Dom.Events;

public interface IErrorEvent
    : IEvent, Error
{
}
