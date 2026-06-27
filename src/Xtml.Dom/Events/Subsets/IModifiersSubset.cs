using static Xtml.Dom.Events.Aliases.Subsets;

namespace Xtml.Dom.Events.Subsets;

public interface IModifiersSubset : ISubset, ModifierAlt, ModifierCtrl, ModifierMeta, ModifierShift, IViewSubset
{
    new const string TRIM = $"{ModifierAlt.TRIM},{ModifierCtrl.TRIM},{ModifierMeta.TRIM},{ModifierShift.TRIM}";
}