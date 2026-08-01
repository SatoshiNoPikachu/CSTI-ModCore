using ModCore.UI;
using UnityEngine;

namespace ModCore.Games.Equip;

public class CharacterScreenEx : MBSingleton<CharacterScreenEx>
{
    private static readonly List<EquipTab> Tabs = EquipTab.Tabs;

    private readonly IndexButton[] _tabButtons = new IndexButton[Tabs.Count + 3];

    private CharacterScreen _screen = null!;

    private int _curTabPage = -1;

    private const int PageSize = 6;

    public static void Create(CharacterScreen screen)
    {
        if (!screen) return;

        var title = screen.transform.Find("DarkBG/ShadowAndPopupWithTitle/Content/Title");
        if (title is null) return;

        title.Find("Tabs").LocalX = 310;

        var titleText = (RectTransform)screen.TitleText.transform;
        titleText.sizeDelta = new Vector2(-1110, 0);
        titleText.LocalX = 10;

        var nextBtnPrefab = UIManager.GetPrefab<ActionButton>(CommonPrefab.UidActionButton);
        if (nextBtnPrefab is null) return;

        var nextBtn = Instantiate(nextBtnPrefab, title, false);
        nextBtn.name = "NextTabButton";
        nextBtn.Text = "→";

        var nextBtnRt = (RectTransform)nextBtn.transform;
        nextBtnRt.anchorMin = nextBtnRt.anchorMax = nextBtnRt.pivot = Vector2.one;
        nextBtnRt.localPosition = new Vector3(1310, -10);

        var tabPrefab = UIManager.GetPrefab<IndexButton>("ModCore:EquipTabButton");
        if (tabPrefab is null)
        {
            tabPrefab = Instantiate(screen.EquipmentTabButton);
            tabPrefab.name = "EquipExTabButton";
            UIManager.RegisterPrefab("ModCore:EquipTabButton", tabPrefab);
        }

        var tabsParent = screen.EquipmentTabButton.transform.parent;

        var ex = screen.gameObject.AddComponent<CharacterScreenEx>();
        ex._screen = screen;
        nextBtn.OnClick = ex.NextTabPage;

        var tabButtons = ex._tabButtons;
        tabButtons[0] = screen.EquipmentTabButton;
        tabButtons[1] = screen.WoundsTabButton;
        tabButtons[2] = screen.CharacterTabButton;
        for (var i = 3; i < tabButtons.Length; i++)
        {
            var tab = Tabs[i - 3];
            var tabBtn = Instantiate(tabPrefab, tabsParent, false);
            tabBtn.Setup(i, "", tab.TabName, false);
            tabBtn.Selected = false;
            tabBtn.OnClicked = ex.ChangeTab;
            if (tab.TabIcon) tabBtn.Sprite = tab.TabIcon;
            tabButtons[i] = tabBtn;
        }

        ex.ChangeTabPageToCur();
    }

    public void ChangeTab(int index)
    {
        if (index >= Tabs.Count + 3) return;

        _screen.CurrentTab = index;
        foreach (var button in _tabButtons)
        {
            button.Selected = button.Index == index;
        }

        var tab = index > 2 ? Tabs[index - 3] : null;

        _screen.TitleText.text = index switch
        {
            0 => LocalizedString.Equipment,
            1 => LocalizedString.Wounds,
            2 => LocalizedString.Character,
            _ => tab!.TabName
        };

        if (index == 2)
        {
            _screen.EquipmentAndWoundsGroup.SetActive(false);
            _screen.CharacterSheetGroup.SetActive(true);
            return;
        }

        var line = _screen.EquipmentSlotsLine;
        foreach (var slot in line.Slots)
        {
            if (!slot.AssignedCard) continue;

            var card = slot.AssignedCard.CardModel;
            if (_screen.WoundsList.IsWound(card))
            {
                slot.IsActive = index == 1;
            }
            else if (tab?.IsThis(card) is true)
            {
                slot.IsActive = true;
            }
            else
            {
                slot.IsActive = index == 0 && EquipTab.IsEquipment(card);
            }
        }

        if (index > 0) line.MoveToPos(0);

        _screen.EquipmentAndWoundsGroup.SetActive(true);
        _screen.CharacterSheetGroup.SetActive(false);
    }

    public void Open()
    {
        Game.Grm!.CloseAllPopups();
        gameObject.SetActive(true);

        if (!_screen.EquipmentSlotsLine) return;

        var curTab = _screen.CurrentTab;
        var slot = _screen.HasNewCards();
        var card = slot?.AssignedCard.CardModel;
        if (card is not null)
        {
            if (_screen.WoundsList.IsWound(card))
            {
                ChangeTab(1);
            }
            else if (EquipTab.IsEquipment(card))
            {
                ChangeTab(0);
            }
            else
            {
                for (var i = 0; i < Tabs.Count; i++)
                {
                    var tab = Tabs[i];
                    if (!tab.IsThis(card)) continue;
                    ChangeTab(i + 3);
                    break;
                }
            }

            ChangeTabPageToCur();
            _screen.EquipmentSlotsLine.MoveViewTo(slot, true, true);
            return;
        }

        if (curTab == 2 && _screen.DontStayOnCharacterSheet)
        {
            ChangeTab(0);
            ChangeTabPageToCur();
            return;
        }

        ChangeTab(curTab);
        ChangeTabPageToCur();
    }

    private void ChangeTabPage()
    {
        var min = _curTabPage * PageSize;
        var max = min + PageSize;

        for (var i = 0; i < _tabButtons.Length; i++)
        {
            _tabButtons[i].gameObject.SetActive(i >= min && i < max);
        }
    }

    private void NextTabPage()
    {
        _curTabPage++;
        if (_curTabPage * PageSize >= _tabButtons.Length) _curTabPage = 0;

        ChangeTabPage();
    }

    private void ChangeTabPageToCur()
    {
        var page = _screen.CurrentTab / PageSize;
        if (page == _curTabPage) return;

        _curTabPage = page;
        ChangeTabPage();
    }
}