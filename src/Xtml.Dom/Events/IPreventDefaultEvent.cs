using static Xtml.Dom.Events.Aliases.Subsets;

namespace Xtml.Dom.Events;

public interface IPreventDefaultEvent : IEvent, PreventDefault,
    IDragEvent,
    IPointerEvent,
    IWheelEvent,
    IMouseEvent,
    ICompositionEvent,
    IFocusEvent,
    IKeyboardEvent,
    ITouchEvent,
    IAnimationEvent,
    IBeforeUnloadEvent,
    IContentVisibilityAutoStateChangeEvent,
    IDeviceMotionEvent,
    IDeviceOrientationEvent,
    IErrorEvent,
    IHashChangeEvent,
    IPageTransitionEvent,
    IProgressEvent,
    IStorageEvent,
    ISubmitEvent,
    IToggleEvent,
    ITransitionEvent,
    IClipboardEvent,
    IFormDataEvent,
    IGamepadEvent,
    IMessageEvent,
    IPopStateEvent,
    IPromiseRejectionEvent
{
}
