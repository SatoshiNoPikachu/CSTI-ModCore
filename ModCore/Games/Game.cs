using System;
using ModCore.Games.ExtraDataModule;
using ModCore.Utils;
using UnityEngine;

namespace ModCore.Games;

public class Game : MonoBehaviour
{
    public static event Action? DestroyEvent;

    public static GameManager Gm { get; private set; } = null!;

    public static GraphicsManager Grm { get; private set; } = null!;

    public static void Create(GameManager gm)
    {
        gm.gameObject.AddComponent<Game>();

        Gm = gm;
        Grm = GraphicsManager.Instance;
    }

    private void OnDestroy()
    {
        DestroyEvent.InvokeSafely();

        ExtraDataStorage.ClearStorage();

        Gm = null!;
        Grm = null!;
    }
}