using static Xtml.Dom.Events.Aliases.Subsets;

namespace Xtml.Dom.Events.Subsets;

public interface ICoordinatesSubset : ISubset, XY, ClientXY, MovementXY, OffsetXY, PageXY, ScreenXY, IViewSubset
{
    new const string TRIM = $"{XY.TRIM},{ClientXY.TRIM},{MovementXY.TRIM},{OffsetXY.TRIM},{PageXY.TRIM},{ScreenXY.TRIM}";
}