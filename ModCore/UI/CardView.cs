using UnityEngine;
using UnityEngine.UI;

namespace ModCore.UI;

public class CardView : MonoBehaviour
{
    public Image? Background;
    public Image? Image;

    public bool Active
    {
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    public bool ParentActive
    {
        get => transform.parent?.gameObject.activeSelf is true;
        set => transform.parent?.gameObject.SetActive(value);
    }
}