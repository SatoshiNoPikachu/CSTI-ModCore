using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
public static class Extensions
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }

    public static T SafeAccess<T>(this T obj) where T : Object
    {
        return obj ? obj : null;
    }

    public static T GetComponent<T>(this Component m, string path) where T : Component
    {
        return m.transform.Find(path)?.GetComponent<T>();
    }

    public static T GetComponent<T>(this GameObject go, string path) where T : Component
    {
        return go.transform.Find(path)?.GetComponent<T>();
    }
}