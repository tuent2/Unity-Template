using System;

namespace Tuent.Core.UI
{
    [Serializable]
    public sealed class UIScreenReference : ComponentReference<UIScreen>
    {
        public UIScreenReference(string guid) : base(guid) { }
    }
}