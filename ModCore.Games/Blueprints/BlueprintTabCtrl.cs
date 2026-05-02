using ModCore.UI;

namespace ModCore.Games.Blueprints;

public class BlueprintTabCtrl : MBSingleton<BlueprintTabCtrl>
{
    private const int PageSize = 10;

    private BlueprintModelsScreen? _screen;

    private readonly List<int> _showTabs = [];

    private int _curPage;

    public static void OnBlueprintModelsScreenAwake(BlueprintModelsScreen screen)
    {
        var prefab = UIManager.GetPrefab<ActionButton>(CommonPrefab.UidActionButton);
        if (prefab is null) return;

        screen.TabsParent.Width = -185;

        var ctrl = screen.gameObject.AddComponent<BlueprintTabCtrl>();
        ctrl._screen = screen;

        var btn = Instantiate(prefab, screen.TabsParent.parent, false);
        btn.name = "NextTabButton";
        btn.Text = "→";
        btn.OnClick = ctrl.NextTabPage;
        btn.transform.LocalX = 1385;
    }

    public void OnShow()
    {
        if (_screen is null) return;

        UpdateShowTabs();

        if (_showTabs.Count == 0)
        {
            _screen.ShowWithoutTabs();
            return;
        }

        var i = _showTabs.IndexOf(_screen.CurrentTab);
        if (i == -1)
        {
            _curPage = 0;
            _screen.ShowTab(_showTabs[0]);
        }
        else _curPage = i / PageSize;

        ChangeTabPage();
    }

    private void UpdateShowTabs()
    {
        if (_screen!.TabButtons is not { } tabBtnList) return;
        if (_screen.BlueprintTabs is not { } tabs) return;

        _showTabs.Clear();

        var tabLen = tabs.Length;
        for (var i = 0; i < tabLen; i++)
        {
            var tab = tabs[i];
            if (tab is null) continue;
            if (BlueprintTab.CanShowTab(tab)) _showTabs.Add(i);
            else tabBtnList[i]?.gameObject.SetActive(false);
        }
    }

    private void ChangeTabPage()
    {
        if (_screen!.TabButtons is not { } tabBtnList) return;

        var min = _curPage * PageSize;
        var max = min + PageSize;

        var showed = _showTabs.Count;
        for (var i = 0; i < showed; i++)
        {
            tabBtnList[_showTabs[i]]?.gameObject.SetActive(i >= min && i < max);
        }
    }

    private void NextTabPage()
    {
        var tabBtnList = _screen?.TabButtons;
        if (tabBtnList is null) return;

        _curPage++;
        if (_curPage * PageSize >= _showTabs.Count) _curPage = 0;

        ChangeTabPage();
    }
}