using System;
using UnityEngine;

namespace Tuent.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReadOnlyInInspectorAttribute : PropertyAttribute
    {
    }
}
