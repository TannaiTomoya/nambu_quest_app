using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class PrepLoopPrototypeController : MonoBehaviour
{
    [Serializable]
    public sealed class StepData
    {
        public string label;
        public string stageLabel;
    }

    [Serializable]
    public sealed class LocationData
    {
        public string id;
        public string name;
        public int requiredRank;
        public int depth;
        public string ticketTitle;
        public string ticketBody;
    }

    [Serializable]
    public sealed class ConfigData
    {
        public float prepSeconds = 8f;
        public float mashPoint = 0.08f;
        public float rank2Threshold = 0.34f;
        public float rank3Threshold = 0.66f;
        public string introTitle;
        public string introBody;
        public string prepTitle;
        public string prepGuide;
        public string mashGuide;
        public StepData[] steps;
        public LocationData[] locations;
        public string returnVideoFile = "nanbu_return_scene.mp4";
    }

    [Header("Panels")]
    public GameObject introPanel;
    public GameObject prepPanel;
    public GameObject rankPanel;
    public GameObject locationPanel;
    public GameObject divePanel;
    public GameObject chestPanel;
    public GameObject returnPanel;
    public GameObject resultPanel;

    [Header("Intro")]
    public TMP_Text introTitleText;
    public TMP_Text introBodyText;
    public Button startButton;

    [Header("Prep")]
    public TMP_Text prepTitleText;
    public TMP_Text timerText;
    public TMP_Text prepGuideText;
    public Button[] prepStepButtons;
    public TMP_Text[] prepStepLabels;
    public Button mashButton;
    public Slider prepGauge;
    public GameObject diverRoot;
    public GameObject[] diverStages;
    public TMP_Text[] diverStageLabels;

    [Header("Rank")]
    public TMP_Text rankText;
    public TMP_Text rankBodyText;

    [Header("Location")]
    public TMP_Text locationTitleText;
    public Button[] locationButtons;
    public TMP_Text[] locationButtonLabels;

    [Header("Dive")]
    public TMP_Text diveTitleText;
    public TMP_Text diveHudText;
    public TMP_Text diveLogText;
    public Slider airGauge;
    public Slider depthGauge;

    [Header("Chest")]
    public TMP_Text chestTitleText;
    public TMP_Text chestLocationText;
    public TMP_Text chestLogText;
    public Button inspectButton;
    public GameObject closedChest;
    public GameObject openChest;
    public GameObject rewardCard;
    public TMP_Text rewardTitleText;
    public TMP_Text rewardBodyText;

    [Header("Return Video")]
    public VideoPlayer returnVideoPlayer;
    public RawImage returnVideoImage;
    public Button skipReturnButton;

    [Header("Result")]
    public TMP_Text resultText;
    public Button restartButton;

    private ConfigData config;
    private int currentStep;
    private int rank;
    private float remainingTime;
    private float gaugeValue;
    private bool prepActive;
    private bool mashPhase;
    private bool listenersBound;
    private LocationData selectedLocation;

    private void Awake()
    {
        config = NormalizeConfig(LoadConfig());
    }

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("PrepLoopPrototypeController: UI参照が不足しています。Playを停止してから Nambu Quest > Build Prep Loop Prototype Scene を再実行してください。");
            return;
        }

        ApplyConfigText();
        BindListeners();
        ShowIntro();
    }

    private void Update()
    {
        if (!prepActive)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        timerText.text = remainingTime.ToString("0.0");

        if (remainingTime <= 0f)
        {
            FinishPrep();
        }
    }

    private void OnDestroy()
    {
        if (returnVideoPlayer != null)
        {
            returnVideoPlayer.loopPointReached -= OnReturnVideoFinished;
        }
    }

    private ConfigData LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "NambuQuestConfig.json");
        if (File.Exists(path))
        {
            return JsonUtility.FromJson<ConfigData>(File.ReadAllText(path));
        }

        Debug.LogWarning("NambuQuestConfig.json が見つからないため、標準設定で起動します: " + path);
        return CreateFallbackConfig();
    }

    private ConfigData NormalizeConfig(ConfigData source)
    {
        ConfigData fallback = CreateFallbackConfig();
        if (source == null)
        {
            return fallback;
        }

        source.prepSeconds = source.prepSeconds > 0f ? source.prepSeconds : fallback.prepSeconds;
        source.mashPoint = source.mashPoint > 0f ? source.mashPoint : fallback.mashPoint;
        source.rank2Threshold = source.rank2Threshold > 0f ? source.rank2Threshold : fallback.rank2Threshold;
        source.rank3Threshold = source.rank3Threshold > 0f ? source.rank3Threshold : fallback.rank3Threshold;
        source.introTitle = string.IsNullOrEmpty(source.introTitle) ? fallback.introTitle : source.introTitle;
        source.introBody = string.IsNullOrEmpty(source.introBody) ? fallback.introBody : source.introBody;
        source.prepTitle = string.IsNullOrEmpty(source.prepTitle) ? fallback.prepTitle : source.prepTitle;
        source.prepGuide = string.IsNullOrEmpty(source.prepGuide) ? fallback.prepGuide : source.prepGuide;
        source.mashGuide = string.IsNullOrEmpty(source.mashGuide) ? fallback.mashGuide : source.mashGuide;
        source.steps = source.steps != null && source.steps.Length > 0 ? source.steps : fallback.steps;
        source.locations = source.locations != null && source.locations.Length > 0 ? source.locations : fallback.locations;
        source.returnVideoFile = string.IsNullOrEmpty(source.returnVideoFile) ? fallback.returnVideoFile : source.returnVideoFile;
        return source;
    }

    private ConfigData CreateFallbackConfig()
    {
        return new ConfigData
        {
            prepSeconds = 8f,
            mashPoint = 0.08f,
            rank2Threshold = 0.34f,
            rank3Threshold = 0.66f,
            introTitle = "南部もぐり",
            introBody = "重い伝統装備を身につけて、海へ潜る。",
            prepTitle = "装備準備",
            prepGuide = "南部もぐりの装備を順番に準備しよう",
            mashGuide = "残り時間で準備を仕上げよう！",
            steps = new[]
            {
                new StepData { label = "1. 潜水服を着る", stageLabel = "潜水服" },
                new StepData { label = "2. 胸当てと錘を調整する", stageLabel = "胸当て・錘" },
                new StepData { label = "3. ヘルメットと送気を確認する", stageLabel = "ヘルメット・送気" }
            },
            locations = new[]
            {
                new LocationData { id = "near", name = "近距離の海底", requiredRank = 1, depth = 12, ticketTitle = "入館案内", ticketBody = "観光情報への誘導。" },
                new LocationData { id = "middle", name = "中距離の海底", requiredRank = 2, depth = 24, ticketTitle = "招待券", ticketBody = "体験情報への誘導。" },
                new LocationData { id = "far", name = "遠距離の海底", requiredRank = 3, depth = 36, ticketTitle = "特別リンク", ticketBody = "公式情報への誘導。" }
            },
            returnVideoFile = "nanbu_return_scene.mp4"
        };
    }

    private void ApplyConfigText()
    {
        SetText(introTitleText, config.introTitle);
        SetText(introBodyText, config.introBody);
        SetText(prepTitleText, config.prepTitle);
        SetText(prepGuideText, config.prepGuide);
        SetText(locationTitleText, "潜水地点を選ぶ");

        for (int i = 0; prepStepLabels != null && i < prepStepLabels.Length; i++)
        {
            SetText(prepStepLabels[i], i < config.steps.Length ? config.steps[i].label : string.Empty);
        }

        for (int i = 0; diverStageLabels != null && i < diverStageLabels.Length; i++)
        {
            if (i == 0)
            {
                SetText(diverStageLabels[i], "装備なし");
            }
            else
            {
                int stepIndex = i - 1;
                SetText(diverStageLabels[i], stepIndex < config.steps.Length ? config.steps[stepIndex].stageLabel : string.Empty);
            }
        }

        for (int i = 0; locationButtonLabels != null && i < locationButtonLabels.Length; i++)
        {
            SetText(locationButtonLabels[i], i < config.locations.Length ? config.locations[i].name : string.Empty);
            if (locationButtons != null && i < locationButtons.Length && locationButtons[i] != null)
            {
                locationButtons[i].gameObject.SetActive(i < config.locations.Length);
            }
        }
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }

    private void BindListeners()
    {
        if (listenersBound || !HasRequiredReferences())
        {
            return;
        }

        startButton.onClick.AddListener(StartPrep);
        restartButton.onClick.AddListener(ShowIntro);
        mashButton.onClick.AddListener(OnMash);
        inspectButton.onClick.AddListener(OnInspect);

        for (int i = 0; i < prepStepButtons.Length; i++)
        {
            int index = i;
            if (prepStepButtons[i] != null)
            {
                prepStepButtons[i].onClick.AddListener(() => OnPrepStep(index));
            }
        }

        for (int i = 0; i < locationButtons.Length; i++)
        {
            int index = i;
            if (locationButtons[i] != null)
            {
                locationButtons[i].onClick.AddListener(() => SelectLocation(index));
            }
        }

        if (skipReturnButton != null)
        {
            skipReturnButton.onClick.AddListener(ShowResult);
        }

        if (returnVideoPlayer != null)
        {
            returnVideoPlayer.loopPointReached += OnReturnVideoFinished;
        }

        listenersBound = true;
    }

    private bool HasRequiredReferences()
    {
        return introPanel != null &&
               prepPanel != null &&
               rankPanel != null &&
               locationPanel != null &&
               divePanel != null &&
               chestPanel != null &&
               returnPanel != null &&
               resultPanel != null &&
               introTitleText != null &&
               introBodyText != null &&
               prepTitleText != null &&
               timerText != null &&
               prepGuideText != null &&
               startButton != null &&
               restartButton != null &&
               mashButton != null &&
               inspectButton != null &&
               prepStepButtons != null &&
               prepStepButtons.Length > 0 &&
               prepStepLabels != null &&
               prepStepLabels.Length > 0 &&
               locationButtons != null &&
               locationButtons.Length > 0 &&
               locationButtonLabels != null &&
               locationButtonLabels.Length > 0 &&
               diverStages != null &&
               diverStages.Length > 0;
    }

    private void ShowIntro()
    {
        StopAllCoroutines();
        prepActive = false;
        mashPhase = false;
        selectedLocation = null;
        SetPanel(introPanel);
        ShowDiverStage(0);
    }

    private void StartPrep()
    {
        currentStep = 0;
        rank = 1;
        gaugeValue = 0f;
        remainingTime = config.prepSeconds;
        prepActive = true;
        mashPhase = false;
        prepGauge.value = 0f;
        timerText.text = remainingTime.ToString("0.0");
        prepGuideText.text = config.prepGuide;
        SetPanel(prepPanel);
        ShowDiverStage(0);
        UpdateStepButtons();
    }

    private void OnPrepStep(int index)
    {
        if (!prepActive || mashPhase || index != currentStep)
        {
            return;
        }

        currentStep++;
        ShowDiverStage(currentStep);
        StartCoroutine(Pulse(diverRoot.transform, 1.05f));

        if (currentStep >= config.steps.Length)
        {
            mashPhase = true;
            prepGuideText.text = config.mashGuide;
        }

        UpdateStepButtons();
    }

    private void OnMash()
    {
        if (!prepActive || !mashPhase)
        {
            return;
        }

        gaugeValue = Mathf.Clamp01(gaugeValue + config.mashPoint);
        prepGauge.value = gaugeValue;
        StartCoroutine(Pulse(mashButton.transform, 1.08f));
        StartCoroutine(Pulse(diverRoot.transform, 1.04f));
    }

    private void FinishPrep()
    {
        prepActive = false;
        mashPhase = false;
        rank = gaugeValue >= config.rank3Threshold ? 3 : gaugeValue >= config.rank2Threshold ? 2 : 1;
        rankText.text = "準備ランク " + rank;
        rankBodyText.text = rank >= 3 ? "遠距離の海底まで潜れる。" : rank == 2 ? "中距離の海底まで潜れる。" : "近距離の海底へ向かえる。";
        SetPanel(rankPanel);
        StartCoroutine(ShowLocationAfterDelay());
    }

    private IEnumerator ShowLocationAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        ShowLocationSelect();
    }

    private void ShowLocationSelect()
    {
        SetPanel(locationPanel);
        for (int i = 0; i < locationButtons.Length; i++)
        {
            bool hasData = i < config.locations.Length;
            locationButtons[i].gameObject.SetActive(hasData);
            locationButtons[i].interactable = hasData && rank >= config.locations[i].requiredRank;
        }
    }

    private void SelectLocation(int index)
    {
        if (index < 0 || index >= config.locations.Length)
        {
            return;
        }

        selectedLocation = config.locations[index];
        SetPanel(divePanel);
        StartCoroutine(DiveRoutine());
    }

    private IEnumerator DiveRoutine()
    {
        diveTitleText.text = "潜水中";
        diveLogText.text = "潜水士が海底へ向かっている。";

        for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f)
        {
            airGauge.value = Mathf.Lerp(1f, 0.76f, t);
            depthGauge.value = t;
            int depth = Mathf.RoundToInt(selectedLocation.depth * t);
            diveHudText.text = "AIR " + Mathf.RoundToInt(airGauge.value * 100f) + "% / DEPTH " + depth + "m / QUEST 探索中";
            yield return null;
        }

        diveTitleText.text = selectedLocation.name + "に到着";
        diveLogText.text = "海底で古い宝箱を見つけた。";
        yield return new WaitForSeconds(0.8f);
        ShowChest(false);
    }

    private void ShowChest(bool opened)
    {
        SetPanel(chestPanel);
        chestTitleText.text = opened ? "特別なものが眠っていた！" : "海底イベント";
        chestLocationText.text = selectedLocation.name;
        chestLogText.text = opened ? "宝箱が開いた！" : "海底に古い箱が沈んでいる。";
        inspectButton.gameObject.SetActive(!opened);
        closedChest.SetActive(!opened);
        openChest.SetActive(opened);
        rewardCard.SetActive(opened);

        if (opened)
        {
            rewardTitleText.text = selectedLocation.ticketTitle;
            rewardBodyText.text = selectedLocation.ticketBody;
        }
    }

    private void OnInspect()
    {
        StartCoroutine(InspectRoutine());
    }

    private IEnumerator InspectRoutine()
    {
        inspectButton.gameObject.SetActive(false);
        chestLogText.text = "宝箱を調査中……";
        yield return Pulse(closedChest.transform, 1.08f);
        yield return new WaitForSeconds(0.5f);
        ShowChest(true);
        yield return new WaitForSeconds(1.0f);
        ShowReturn();
    }

    private void ShowReturn()
    {
        SetPanel(returnPanel);

        if (returnVideoPlayer == null)
        {
            StartCoroutine(ResultAfterDelay());
            return;
        }

        string path = Path.Combine(Application.streamingAssetsPath, config.returnVideoFile);
        returnVideoPlayer.url = path;

        if (returnVideoImage != null && returnVideoPlayer.targetTexture == null)
        {
            RenderTexture texture = new RenderTexture(720, 1280, 0);
            returnVideoPlayer.targetTexture = texture;
            returnVideoImage.texture = texture;
        }

        returnVideoPlayer.Play();
    }

    private IEnumerator ResultAfterDelay()
    {
        yield return new WaitForSeconds(2.0f);
        ShowResult();
    }

    private void OnReturnVideoFinished(VideoPlayer player)
    {
        ShowResult();
    }

    private void ShowResult()
    {
        if (returnVideoPlayer != null)
        {
            returnVideoPlayer.Stop();
        }

        string locationName = selectedLocation != null ? selectedLocation.name : "未選択";
        resultText.text = "探索完了\n\n準備ランク " + rank + "\n到達地点 " + locationName;
        SetPanel(resultPanel);
    }

    private void UpdateStepButtons()
    {
        for (int i = 0; i < prepStepButtons.Length; i++)
        {
            prepStepButtons[i].interactable = !mashPhase && i == currentStep;
        }

        mashButton.gameObject.SetActive(mashPhase);
    }

    private void ShowDiverStage(int stage)
    {
        for (int i = 0; i < diverStages.Length; i++)
        {
            diverStages[i].SetActive(i == Mathf.Clamp(stage, 0, diverStages.Length - 1));
        }
    }

    private void SetPanel(GameObject activePanel)
    {
        introPanel.SetActive(activePanel == introPanel);
        prepPanel.SetActive(activePanel == prepPanel);
        rankPanel.SetActive(activePanel == rankPanel);
        locationPanel.SetActive(activePanel == locationPanel);
        divePanel.SetActive(activePanel == divePanel);
        chestPanel.SetActive(activePanel == chestPanel);
        returnPanel.SetActive(activePanel == returnPanel);
        resultPanel.SetActive(activePanel == resultPanel);

        if (diverRoot != null)
        {
            diverRoot.SetActive(activePanel == introPanel || activePanel == prepPanel);
            RectTransform diverRect = diverRoot.GetComponent<RectTransform>();
            if (diverRect != null)
            {
                diverRect.anchoredPosition = activePanel == introPanel ? new Vector2(0f, -300f) : new Vector2(0f, -90f);
            }
        }
    }

    private IEnumerator Pulse(Transform target, float scale)
    {
        Vector3 start = target.localScale;
        Vector3 big = start * scale;

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.12f)
        {
            target.localScale = Vector3.Lerp(start, big, t);
            yield return null;
        }

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.12f)
        {
            target.localScale = Vector3.Lerp(big, start, t);
            yield return null;
        }

        target.localScale = start;
    }
}
