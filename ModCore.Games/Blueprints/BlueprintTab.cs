using System;
using ModCore.Data;
using ModCore.Services;
using UnityEngine;

namespace ModCore.Games.Blueprints;

[Serializable]
[DataInfo("MC-BlueprintTab")]
public class BlueprintTab : ScriptableObject
{
    private static Dictionary<CardTabGroup, BlueprintTab> _tabs = [];

    private static List<CardTabGroup> _mainTabs = [];

    public CardTabGroup? BindTabGroup;

    public CharacterPerk?[]? RequiredPerks;

    public CharacterPerk?[]? ExcludedPerks;

    public bool IsMainTab => BindTabGroup?.HasSubGroups is true;

    public static bool CanShowTab(CardTabGroup ctg)
    {
        if (!_tabs.TryGetValue(ctg, out var tab)) return true;

        return tab.HasRequiredPerks() && !tab.HasExcludedPerks();
    }

    public bool HasRequiredPerks()
    {
        if (RequiredPerks is null or { Length: 0 }) return true;

        var flag = true;

        foreach (var perk in RequiredPerks)
        {
            if (perk is null) continue;

            flag = false;

            if (GameManager.CurrentPlayerCharacter.CharacterPerks.Contains(perk)) return true;
        }

        return flag;
    }

    public bool HasExcludedPerks()
    {
        if (ExcludedPerks is null or { Length: 0 }) return false;

        foreach (var perk in ExcludedPerks)
        {
            if (perk is null) continue;
            if (GameManager.CurrentPlayerCharacter.CharacterPerks.Contains(perk)) return true;
        }

        return false;
    }

    internal static void OnLoadComplete()
    {
        var modMc = Plugin.ModData!;
        var tabs = modMc.GetData<BlueprintTab>();
        if (tabs is not null)
        {
            foreach (var tab in tabs.Values)
            {
                tab.RegisterTab();
            }
        }

        List<BlueprintTab> wait = [];

        foreach (var mod in ModService.Mods)
        {
            if (mod == modMc) continue;

            tabs = mod.GetData<BlueprintTab>();
            if (tabs is null) continue;

            foreach (var tab in tabs.Values)
            {
                if (tab.BindTabGroup is null) continue;

                var ns = ModData.GetNamespace(tab.BindTabGroup.name);
                if (ns is null || ModService.GetMod(ns) != mod)
                {
                    wait.Add(tab);
                    continue;
                }

                tab.RegisterTab(true);
            }
        }

        foreach (var tab in wait)
        {
            tab.RegisterTab();
        }
    }

    internal static void AddMainTabs()
    {
        var tabs = Game.Grm!.BlueprintModelsPopup.BlueprintTabs;
        var newTabs = new CardTabGroup[tabs.Length + _mainTabs.Count];
        Array.Copy(tabs, newTabs, tabs.Length);
        _mainTabs.CopyTo(newTabs, tabs.Length);
    }

    private bool RegisterTab(bool registerMain = false)
    {
        if (BindTabGroup is null) return false;
        if (_tabs.TryAdd(BindTabGroup, this))
        {
            if (registerMain && IsMainTab) _mainTabs.Add(BindTabGroup);
            return true;
        }

        Plugin.Log.LogWarning($"BlueprintTab not registered: {name}.");
        return false;
    }
}