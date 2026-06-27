namespace HtmlString;

public static partial class HtmlExtensions
{
    public static Html.Enumerable<T> Select<T>(
        this IList<T> source, 
        Func<T, Html> selector)
            => new(source, selector);

    // public static Html.IEnumerable<T> Select<T>(
    //     this IEnumerable<T> source, 
    //     Func<T, Html> selector)
    //         => new(source, selector);
}

public ref partial struct Html
{
    public struct Enumerable<T>(IList<T> source, Func<T, Html> selector)
    {
        public readonly int Count => source.Count;
        public readonly Enumerator GetEnumerator() => new(source, selector);

        public struct Enumerator(IList<T> list, Func<T, Html> selector)
        {
            int index = -1;
            public readonly Html Current => selector(list[index]);
            public readonly (Func<T, Html> selector, T) CurrentDeconstructed => (selector, list[index]);
            public bool MoveNext() => ++index < list.Count;
        }
    }

    // public struct IEnumerable<T>(System.Collections.Generic.IEnumerable<T> source, Func<T, Html> selector)
    // {
    //     public readonly int Count => source.Count();
    //     public readonly Enumerator GetEnumerator() => new(source, selector);

    //     public struct Enumerator(System.Collections.Generic.IEnumerable<T> enumerable, Func<T, Html> selector)
    //     {
    //         readonly IEnumerator<T> enumerator = enumerable.GetEnumerator();
    //         public readonly Html Current => selector(enumerator.Current);
    //         public readonly (Func<T, Html> selector, T) CurrentDeconstructed => (selector, enumerator.Current);
    //         public bool MoveNext() => enumerator.MoveNext();
    //     }
    // }
}