using UnityEngine;
using UnityEngine.UI;

namespace Tuent.Core.UI
{
    public class LockButton : MonoBehaviour
    {
        [SerializeField] private Button myBtn;

        private void Start()
        {
            myBtn.onClick.AddListener(OnMyBtnClick);
        }

        private void OnMyBtnClick()
        {
            Toast.Show("Coming soon!!!");
        }
    }
}