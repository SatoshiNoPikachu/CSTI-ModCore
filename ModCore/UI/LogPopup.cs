using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable InconsistentNaming

namespace ModCore.UI;

public class LogPopup<T> : MBSingleton<T> where T : LogPopup<T>
{
    public static T? Create(string name)
    {
        var prefab = UIManager.GetPrefabAsGameObject(CommonPrefab.UidLogPopup);
        if (prefab is null) return null;

        var go = Instantiate(prefab.gameObject, GraphicsManager.Instance.EncounterPopupWindow.transform.parent);
        go.name = name;
        var comp = go.AddComponent<T>();
        comp.Init();
        return comp;
    }

    protected readonly CardView[] CardViews = new CardView[4];

    protected readonly List<OptionButtonGroup> ButtonGroups = [];

    protected TextMeshProUGUI? UITitle;
    protected Transform? ButtonsParent;
    protected Transform? ActionButtonsParent;

    private static LocalizedString SeparatorInTextForm =>
        new() { LocalizationKey = "IGNOREKEY", DefaultText = "SEPARATOR" };

    protected virtual void Init()
    {
        var content = transform.Find("ShadowAndPopupWithTitle/Content");

        UITitle = content.GetComponent<TextMeshProUGUI>("Title/Text");

        ButtonsParent = content.Find("HorizontalScrollView/Viewport");
        ActionButtonsParent = content.Find("HorizontalScrollView/Viewport/Actions");
        ButtonGroups.Add(ActionButtonsParent.GetComponent<OptionButtonGroup>());

        CardViews[0] = content.GetComponent<CardView>("InspectionGroup/InspectionSlot/CardView1")!;
        CardViews[1] = content.GetComponent<CardView>("InspectionGroup/InspectionSlot/CardView2")!;
        CardViews[2] = content.GetComponent<CardView>("InspectionGroup/InspectionSlot2/CardView1")!;
        CardViews[3] = content.GetComponent<CardView>("InspectionGroup/InspectionSlot2/CardView2")!;

        var encounterPopup = EncounterPopup.Instance;
        GM = GameManager.Instance;
        EncounterLogSeparatorPrefab = encounterPopup.EncounterLogSeparatorPrefab;
        EncounterLogTextPrefab = encounterPopup.EncounterLogTextPrefab;
        EncounterLogParent = (RectTransform)content.Find("InspectionGroup/VerticalScrollView/Viewport/Content");
        LogScroll = content.GetComponent<ScrollRect>("InspectionGroup/VerticalScrollView");
    }

    public void SetTitle(string text)
    {
        UITitle?.text = text;
    }

    public OptionButtonGroup AddButtonGroup(string groupName)
    {
        var group = Instantiate(UIManager.GetPrefab<OptionButtonGroup>(CommonPrefab.UidOptionButtonGroup)!,
            ButtonsParent, false);
        group.name = groupName;
        ButtonGroups.Add(group);
        return group;
    }

    public void SetButtonGroupActive(int index)
    {
        if (ButtonGroups.Count < index) return;

        for (var i = 0; i < ButtonGroups.Count; i++)
        {
            ButtonGroups[i].Active = index == i;
        }
    }

    public void SetImage(Sprite sprite, int index = 0)
    {
        if (index >= CardViews.Length) return;

        CardViews[index].Image?.overrideSprite = sprite;
    }

    public void SetCardActive(int index, bool active)
    {
        CardViews[index].Active = active;
        CheckCardsActive();
    }

    private void CheckCardsActive()
    {
        CardViews[0].ParentActive = CardViews[0].Active || CardViews[1].Active;
        CardViews[2].ParentActive = CardViews[2].Active || CardViews[3].Active;
    }

    private void Update()
    {
        UpdateLog();
    }

    private StringBuilder? CurrentEncounterLog;
    private float CurrentLogDuration;
    private TextMeshProUGUI? CurrentLogText;
    private int CurrentLogTextTargetLength;
    private float CurrentLogTextVisibleLength;
    private RectTransform? EncounterLogParent;
    private GameObject? EncounterLogSeparatorPrefab;
    private readonly List<GameObject> EncounterLogSeparators = [];
    private TextMeshProUGUI? EncounterLogTextPrefab;
    private readonly List<TextMeshProUGUI> EncounterLogTexts = [];
    private GameManager? GM;
    private readonly List<EncounterLogMessage> LogQueue = [];
    private float LogQueueTimer;
    private ScrollRect? LogScroll;
    private int LogSeparatorCount;
    private int LogTextCount;
    private List<LocalizedString> LogTextHistory = [];
    private int PrevLogTextTargetLength;
    private bool SkipLog;

    public bool LogIsUpdating => LogQueueTimer > 0f || LogQueue.Count > 0;

    public void AddLogSeparator(float _Duration = 0f)
    {
        LogQueue.Add(new EncounterLogMessage(null, _Duration));
    }

    public void AddToLog(EncounterLogMessage _Message)
    {
        if (string.IsNullOrEmpty(_Message))
        {
            return;
        }

        LogQueue.Add(_Message);
    }

    public void ClearLog()
    {
        LogTextHistory.Clear();
        if (CurrentEncounterLog == null)
        {
            CurrentEncounterLog = new StringBuilder();
        }
        else
        {
            CurrentEncounterLog.Clear();
        }

        LogTextCount = 1;
        LogSeparatorCount = 0;
        switch (EncounterLogTexts.Count)
        {
            case > 1:
            {
                for (var i = 1; i < EncounterLogTexts.Count; i++)
                {
                    EncounterLogTexts[i].gameObject.SetActive(false);
                }

                break;
            }
            case 0:
                LogTextCount = 0;
                CreateLogTextObject();
                break;
        }

        CurrentLogText = EncounterLogTexts[0];
        CurrentLogText.text = "";
        foreach (var sep in EncounterLogSeparators)
        {
            sep.gameObject.SetActive(false);
        }
    }

    private void CreateLogSeparator()
    {
        LogTextHistory.Add(SeparatorInTextForm);
        LogSeparatorCount++;
        if (EncounterLogSeparators.Count < LogSeparatorCount)
        {
            EncounterLogSeparators.Add(Instantiate(EncounterLogSeparatorPrefab, EncounterLogParent)!);
        }
        else
        {
            EncounterLogSeparators[LogSeparatorCount - 1].SetActive(true);
        }

        CreateLogTextObject();
        if (!LogScroll) return;
        LogScroll.DOKill();
        LogScroll.DOVerticalNormalizedPos(0f, 0.3f).SetEase(Ease.OutSine);
    }

    private void CreateLogTextObject()
    {
        if (CurrentLogText)
        {
            CurrentLogText!.maxVisibleCharacters = 99999;
        }

        LogTextCount++;
        if (EncounterLogTexts.Count < LogTextCount)
        {
            EncounterLogTexts.Add(Instantiate(EncounterLogTextPrefab, EncounterLogParent)!);
        }
        else
        {
            EncounterLogTexts[LogTextCount - 1].text = "";
            EncounterLogTexts[LogTextCount - 1].gameObject.SetActive(true);
        }

        if (CurrentEncounterLog == null)
        {
            CurrentEncounterLog = new StringBuilder();
        }
        else
        {
            CurrentEncounterLog.Clear();
        }

        CurrentLogText = EncounterLogTexts[LogTextCount - 1];
        PrevLogTextTargetLength = 0;
        CurrentLogTextTargetLength = 0;
        CurrentLogTextVisibleLength = CurrentLogTextTargetLength;
    }

    private IEnumerator WaitBeforeClosingWindow()
    {
        gameObject.SetActive(false);
        // ContinueButton.interactable = false;
        // while (ActionPlaying)
        // {
        //     yield return null;
        // }
        LogQueue.Clear();
        LogQueueTimer = 0f;
        // OngoingEncounter = false;
        yield break;
    }

    private void WriteTextToLog(LocalizedString _Text, bool _Loading, OptionalTextSettings _Settings,
        OptionalColorValue _Color)
    {
        if (string.IsNullOrEmpty(_Text))
        {
            return;
        }

        var text = StringUtils.WithUpperCase(_Text);
        if (!_Loading)
        {
            if (_Settings)
            {
                if (_Settings.Bold)
                {
                    text = $"<b>{text}</b>";
                }

                if (_Settings.Italics)
                {
                    text = $"<i>{text}</i>";
                }

                if (_Settings.Underlined)
                {
                    text = $"<u>{text}</u>";
                }
            }

            if (_Color)
            {
                text = $"<color=#{ColorUtility.ToHtmlStringRGBA(_Color.ColorValue)}>{text}</color>";
            }
        }

        LogTextHistory.Add(_Text);
        if (!CurrentLogText)
        {
            CreateLogTextObject();
        }

        CurrentLogTextVisibleLength = CurrentLogTextTargetLength;
        CurrentLogText!.maxVisibleCharacters = CurrentLogTextTargetLength;
        CurrentEncounterLog ??= new StringBuilder();

        CurrentEncounterLog.Append(CurrentEncounterLog.Length > 0 ? $"\n{text}" : text);

        CurrentLogText.text = CurrentEncounterLog.ToString();
        GM!.StartCoroutine(UpdateLogTextMaxCharacterLength());
        if (!LogScroll) return;
        LogScroll.DOKill();
        LogScroll.DOVerticalNormalizedPos(0f, 0.3f).SetEase(Ease.OutSine);
    }

    private IEnumerator UpdateLogTextMaxCharacterLength()
    {
        yield return null;
        PrevLogTextTargetLength = CurrentLogTextTargetLength;
        CurrentLogTextTargetLength = CurrentLogText!.textInfo.characterCount;
    }

    private void UpdateLog()
    {
        if (LogQueue.Count > 0)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SkipLog = true;
            }

            if (LogQueueTimer <= 0f || SkipLog)
            {
                PrevLogTextTargetLength = CurrentLogTextTargetLength;
                if (string.IsNullOrEmpty(LogQueue[0].GetLogText()))
                {
                    CreateLogSeparator();
                }
                else
                {
                    WriteTextToLog(LogQueue[0].GetLogText(), false, LogQueue[0].TextSettings, LogQueue[0].TextColor);
                }

                if (LogQueue[0].ScreenShake && MBSingleton<AmbienceImageEffect>.Instance)
                {
                    MBSingleton<AmbienceImageEffect>.Instance.ShakeScreen();
                }

                LogQueueTimer = LogQueue[0].GetDuration;
                CurrentLogDuration = LogQueue[0].GetDuration;
                LogQueue.RemoveAt(0);
            }
        }
        else
        {
            if ((Input.GetMouseButtonDown(0) || SkipLog) && LogQueueTimer > 0f)
            {
                LogQueueTimer = 0f;
                PrevLogTextTargetLength = CurrentLogTextTargetLength;
                if (CurrentLogText)
                {
                    CurrentLogTextVisibleLength = CurrentLogTextTargetLength;
                }
            }

            SkipLog = false;
        }

        LogQueueTimer -= Time.deltaTime;
        if (!CurrentLogText) return;
        if (CurrentLogDuration > 0f)
        {
            CurrentLogTextVisibleLength = Mathf.Lerp(PrevLogTextTargetLength, CurrentLogTextTargetLength,
                (CurrentLogDuration - LogQueueTimer) / CurrentLogDuration);
        }
        else
        {
            CurrentLogTextVisibleLength = CurrentLogTextTargetLength;
        }

        CurrentLogText!.maxVisibleCharacters = Mathf.FloorToInt(CurrentLogTextVisibleLength);
    }
}