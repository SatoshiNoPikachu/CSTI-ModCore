using UnityEngine;
using UnityEngine.UI;

namespace ModCore.Games.ScrollView;

public static class InspectionPopupScroll
{
    public static void Create()
    {
        var model = Game.Grm?.EncounterPopupWindow?.transform.Find(
            "ShadowAndPopupWithTitle/Content/HorizontalScrollView");
        if (model is null) return;

        var view = Object.Instantiate(model.gameObject);
        view.name = "HorizontalScrollView";

        var frame = view.transform.Find("Frame")?.gameObject;
        if (frame) Object.DestroyImmediate(frame);

        var viewport = view.transform.Find("Viewport")?.gameObject;
        viewport?.AddComponent<NonDrawingGraphic>();

        var actions = viewport?.transform.Find("Actions")?.gameObject;
        if (actions) Object.DestroyImmediate(actions);

        var bar = view.transform.Find("ScrollbarHorizontal");
        if (bar) bar.localScale = new Vector3(1, 0.7f, 1);

        var view2 = Object.Instantiate(view);
        view2.name = "HorizontalScrollView";

        CreateCardInspectionPopup(view.transform);
        CreateInventoryInspectionPopup(view2.transform);
    }

    private static void CreateCardInspectionPopup(Transform view)
    {
        var actions = Game.Grm?.CardInspectionPopup?.DismantleOptionsParent;
        if (actions is null) return;

        view.SetParent(actions.parent, false);
        view.SetSiblingIndex(2);

        var viewport = view.transform.Find("Viewport");
        actions.SetParent(viewport, false);

        var scrollRect = view.GetComponent<ScrollRect>();
        scrollRect.content = actions;

        actions.GetComponent<LayoutElement>().minHeight = 85;
    }

    private static void CreateInventoryInspectionPopup(Transform view)
    {
        var actions = Game.Grm?.InventoryInspectionPopup?.DismantleOptionsParent;
        if (actions is null) return;

        view.SetParent(actions.parent, false);
        view.SetSiblingIndex(1);
        view.GetComponent<LayoutElement>().minWidth = 1100;

        var viewport = view.transform.Find("Viewport");
        actions.SetParent(viewport, false);

        var scrollRect = view.GetComponent<ScrollRect>();
        scrollRect.content = actions;

        actions.GetComponent<LayoutElement>().minHeight = 68;
    }
}