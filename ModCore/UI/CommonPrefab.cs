using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModCore.UI;

public static class CommonPrefab
{
    public const string UidActionButton = "ModCore-ActionButton";

    internal static void MakePrefabsOnGame()
    {
        MakeActionButton();
    }

    private static void MakeActionButton()
    {
        var screen = GraphicsManager.Instance.BlueprintModelsPopup;
        if (!screen) return;

        var parent = screen.TabsParent?.parent;
        if (!parent) return;

        var target = parent.Find("CloseButton");
        if (!target) return;

        var go = Object.Instantiate(target.gameObject);
        go.name = "ActionButton";

        var btn = go.AddComponent<ActionButton>();
        btn.ButtonText = go.GetComponent<TextMeshProUGUI>("ButtonText");
        btn.Group = go.AddComponent<CanvasGroup>();
        btn.ButtonObject = go.GetComponent<Button>("ButtonObject");

        UIManager.RegisterPrefab(UidActionButton, btn);
    }
}