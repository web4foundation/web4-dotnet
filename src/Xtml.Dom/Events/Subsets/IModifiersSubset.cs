using static Web4.Dom.Events.Aliases.Subsets;

namespace Web4.Dom.Events.Subsets;

public interface IModifiersSubset : ISubset, ModifierAlt, ModifierCtrl, ModifierMeta, ModifierShift, IViewSubset
{
    new const string TRIM = $"{ModifierAlt.TRIM},{ModifierCtrl.TRIM},{ModifierMeta.TRIM},{ModifierShift.TRIM}";
}