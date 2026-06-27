using System;

namespace Xtml
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class LiveAttribute : Attribute
    {
        public LiveAttribute()
        {
        }

        public string PropertyName { get; set; } = "";
    }
}