using UnityEngine.UI;

namespace ModCore.UI;

public class OptionButton : IndexButton
{
    public Button? ButtonObject;

    private void Awake()
    {
        ButtonObject?.onClick.AddListener(Click);
    }
}