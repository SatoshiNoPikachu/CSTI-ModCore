using UnityEngine;

// ReSharper disable once CheckNamespace
public static class UnityExtensions
{
    public static T? GetComponent<T>(this Component m, string path) where T : Component
    {
        return m.transform.Find(path)?.GetComponent<T>();
    }

    public static T? GetComponent<T>(this GameObject go, string path) where T : Component
    {
        return go.transform.Find(path)?.GetComponent<T>();
    }

    extension<T>(T obj) where T : Object
    {
        public T? Val => obj ? obj : null;
    }
}