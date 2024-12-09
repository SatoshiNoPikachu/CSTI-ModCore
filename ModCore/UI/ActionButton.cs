using System;
using UnityEngine.UI;

namespace ModCore.UI;

public class ActionButton : TooltipButton
{
    public Action OnClick;

    public Button ButtonObject;

    private void Awake()
    {
        ButtonObject?.onClick.AddListener(Click);
    }

    public void Click()
    {
        OnClick?.Invoke();
    }
}