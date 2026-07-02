using UnityEngine;

public class SubMenuOpener : MonoBehaviour
{
    [SerializeField] private GameObject _subMenu;

    public void SwitchSubMenu()
    {
        _subMenu.SetActive(!_subMenu.activeSelf);
    }

    public void OpenSubMenu()
    {
        _subMenu.SetActive(true);
    }

    public void CloseSubMenu()
    {
        _subMenu.SetActive(false);
    }
}
