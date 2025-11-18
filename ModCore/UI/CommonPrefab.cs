using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModCore.UI;

public static class CommonPrefab
{
    public const string UidActionButton = "ModCore:ActionButton";

    public const string UidLogPopup = "ModCore:LogPopup";

    public const string UidOptionButton = "ModCore:OptionButton";

    public const string UidOptionButtonGroup = "ModCore:OptionButtonGroup";

    internal static void MakePrefabsOnGame()
    {
        MakeActionButton();
        MakeOptionButton();
        MakeLogPopup();
    }

    private static void MakeActionButton()
    {
        if (UIManager.GetPrefab(UidActionButton)) return;

        var screen = GraphicsManager.Instance.BlueprintModelsPopup;
        if (!screen) return;

        var parent = screen.TabsParent?.parent;
        if (!parent) return;

        var target = parent!.Find("CloseButton");
        if (!target) return;

        var go = Object.Instantiate(target.gameObject);
        go.name = "ActionButton";

        var btn = go.AddComponent<ActionButton>();
        btn.ButtonText = go.GetComponent<TextMeshProUGUI>("ButtonText");
        btn.Group = go.AddComponent<CanvasGroup>();
        btn.ButtonObject = go.GetComponent<Button>("ButtonObject")!;
        btn.ButtonObject.onClick = new Button.ButtonClickedEvent();

        UIManager.RegisterPrefab(UidActionButton, btn);
    }

    private static void MakeOptionButton()
    {
        if (UIManager.GetPrefab(UidOptionButton)) return;

        var tmp = Object.Instantiate(GraphicsManager.Instance.EncounterPopupWindow.ActionButtonPrefab);
        var prefab = tmp.gameObject;
        Object.DestroyImmediate(tmp);

        var btn = prefab.AddComponent<OptionButton>();
        btn.name = "OptionButton";
        btn.ButtonObject = btn.transform.GetComponent<Button>("ButtonObject");
        btn.ButtonText = btn.transform.GetComponent<TextMeshProUGUI>("ButtonText");
        btn.Group = btn.GetComponent<CanvasGroup>();
        btn.ButtonObject?.onClick = new Button.ButtonClickedEvent();

        UIManager.RegisterPrefab(UidOptionButton, btn);
    }

    private static void MakeLogPopup()
    {
        if (UIManager.GetPrefab(UidLogPopup)) return;

        var encounterPopup = GraphicsManager.Instance.EncounterPopupWindow;
        encounterPopup.SetAsInstance(false);

        var prefab = Object.Instantiate(encounterPopup.gameObject);
        Object.DestroyImmediate(prefab.GetComponent<EncounterPopup>());
        prefab.name = "LogPopup";

        var inspectionSlot = prefab.transform.Find("ShadowAndPopupWithTitle/Content/InspectionGroup/InspectionSlot");
        var bg = inspectionSlot.Find("EncounterBG");
        var image = inspectionSlot.Find("EncounterImage");
        bg.name = "BG";
        image.name = "Image";

        var content = prefab.transform.Find("ShadowAndPopupWithTitle/Content");
        var slot = content.Find("InspectionGroup/InspectionSlot");
        var fill = slot.Find("Fill");
        var border = slot.Find("Border");

        border.localScale = new Vector3(1, 0.98f, 1);

        var view1 = Object.Instantiate(slot.gameObject, slot);
        view1.name = "CardView1";
        view1.transform.localPosition = new Vector3(0, 10, 0);

        var view1Comp = view1.AddComponent<CardView>();
        view1Comp.Background = view1Comp.transform.GetComponent<Image>("BG");
        view1Comp.Image = view1Comp.transform.GetComponent<Image>("Image");

        Object.DestroyImmediate(fill.gameObject);
        Object.DestroyImmediate(border.gameObject);
        Object.DestroyImmediate(bg.gameObject);
        Object.DestroyImmediate(image.gameObject);

        var view2 = Object.Instantiate(view1, slot);
        view2.name = "CardView2";
        view2.transform.localPosition = new Vector3(0, -338, 0);

        prefab.SetActive(true);
        prefab.SetActive(false);

        var slot2 = Object.Instantiate(slot.gameObject, slot.parent);
        slot2.name = "InspectionSlot2";

        var buttonGroup = content.Find("HorizontalScrollView/Viewport/Actions");
        Object.DestroyImmediate(buttonGroup.Find("ContinueButton").gameObject);
        var buttonGroupComp = buttonGroup.gameObject.AddComponent<OptionButtonGroup>();

        UIManager.RegisterPrefab(UidLogPopup, prefab);

        if (UIManager.GetPrefab(UidOptionButtonGroup)) return;

        var group = Object.Instantiate(buttonGroupComp);
        group.gameObject.SetActive(false);
        UIManager.RegisterPrefab(UidOptionButtonGroup, group);
    }
}