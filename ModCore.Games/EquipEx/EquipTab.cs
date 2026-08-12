using System;
using System.Linq;
using ModCore.Data;
using UnityEngine;

namespace ModCore.Games.EquipEx;

[Serializable]
[DataInfo("MC-EquipTab")]
public class EquipTab : ScriptableObject
{
    public static readonly List<EquipTab> Tabs = [];

    private static readonly List<EquipmentTag> WoundTags = [];

    private static readonly HashSet<EquipmentTag> ExTags = [];

    /// <summary>
    /// 选项卡名称
    /// </summary>
    public LocalizedString TabName;

    /// <summary>
    /// 选项卡图标
    /// </summary>
    public Sprite? TabIcon;

    /// <summary>
    /// 标签列表
    /// </summary>
    public EquipmentTag?[]? Tags;

    internal static void OnLoadComplete()
    {
        var data = Database.GetData<EquipTab>();
        if (data is null) return;

        var wound = data.GetValueOrDefault("ModCore:Wound");
        RegisterTags(wound, WoundTags);

        foreach (var tab in data.Values)
        {
            if (tab == wound) continue;

            Tabs.Add(tab);

            var tags = tab.Tags;
            if (tags is null) continue;

            var list = new List<EquipmentTag>();
            foreach (var tag in tags)
            {
                if (tag is null) continue;
                ExTags.Add(tag);
                list.Add(tag);
            }

            tab.Tags = [.. list];
        }
    }

    private static void RegisterTags(EquipTab? tab, List<EquipmentTag> target)
    {
        if (tab?.Tags is null) return;

        var set = new HashSet<EquipmentTag>();
        foreach (var tag in tab.Tags)
        {
            if (tag is not null && set.Add(tag)) target.Add(tag);
        }
    }

    internal static void AddTags(CharacterScreen screen)
    {
        AddTags(ref screen.WoundsList.WoundTags, WoundTags);
    }

    private static void AddTags(ref EquipmentTag[]? target, List<EquipmentTag> tags)
    {
        if (target is null || tags.Count is 0) return;

        target = [.. target, .. tags];
    }

    public static bool IsEquipment(CardData? card)
    {
        if (card?.EquipmentTags is null) return true;

        foreach (var tag in card.EquipmentTags)
        {
            if (ExTags.Contains(tag)) return false;
        }

        return true;
    }

    public bool IsThis(CardData? card)
    {
        if (card is null) return false;

        return Tags?.Any(card.HasEquipmentTag) is true;
    }
}