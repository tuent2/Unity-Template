using UnityEngine;

namespace Tuent.Core.UI
{
    public abstract class BaseSelectView : MonoBehaviour, ISelectView
    {
        public bool IsSelected { get; set; }
        public abstract void ChangeSelect(bool isSelected);
    }
}