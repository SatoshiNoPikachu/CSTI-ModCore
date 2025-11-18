using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModCore.Games.ExtraDataModule;

internal class ExtraDataStorageController : MonoBehaviour
{
    private static readonly List<IDictionary> StorageTables = [];

    public static void Creat(GameManager gm)
    {
        gm.gameObject.AddComponent<ExtraDataStorageController>();
    }

    public static void RegisterStorageTable(IDictionary table)
    {
        StorageTables.Add(table);
    }

    private void OnDestroy()
    {
        foreach (var table in StorageTables)
        {
            table.Clear();
        }
    }
}