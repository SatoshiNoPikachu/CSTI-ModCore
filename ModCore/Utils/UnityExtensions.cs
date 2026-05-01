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

    extension(Transform t)
    {
        public float LocalX
        {
            set
            {
                var p = t.localPosition;
                p.x = value;
                t.localPosition = p;
            }
        }

        public float LocalY
        {
            set
            {
                var p = t.localPosition;
                p.y = value;
                t.localPosition = p;
            }
        }
    }

    extension(RectTransform rt)
    {
        public float Width
        {
            set
            {
                var size = rt.sizeDelta;
                size.x = value;
                rt.sizeDelta = size;
            }
        }

        public float Height
        {
            set
            {
                var size = rt.sizeDelta;
                size.y = value;
                rt.sizeDelta = size;
            }
        }
    }
}