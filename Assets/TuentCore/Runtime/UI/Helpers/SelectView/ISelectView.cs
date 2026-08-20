namespace Tuent.Core.UI
{
    public interface ISelectView
    {
        bool IsSelected { get; set; }
        void ChangeSelect(bool isSelected);
    }
}