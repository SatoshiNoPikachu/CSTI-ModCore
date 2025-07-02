using System;
using HarmonyLib;

namespace ModCore;

[AttributeUsage(AttributeTargets.Class)]
public class ModNamespaceAttribute(string ns) : Attribute
{
    public string Namespace => ns;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ModCoreAfterAttribute() : HarmonyAfter(Plugin.PluginGuid);