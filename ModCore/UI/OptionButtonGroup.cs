using System.Collections.Generic;
using UnityEngine;

namespace ModCore.UI;

public class OptionButtonGroup : MonoBehaviour
{
    public List<OptionButton> Buttons { get; set; } = [];

    public bool Active
    {
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    public OptionButton AddButton()
    {
        var btn = Instantiate(UIManager.GetPrefab<OptionButton>(CommonPrefab.UidOptionButton)!, transform);
        Buttons.Add(btn);
        return btn;
    }
}