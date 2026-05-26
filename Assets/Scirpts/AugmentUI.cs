using System.Collections;
using TMPro;
using UnityEngine;

public class AugmentUI : MonoBehaviour
{
    public static AugmentUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject augmentPanel;

    [Header("Cards")]
    public AugmentCard[] cards = new AugmentCard[3];

    [Header("Active Augments")]
    public TextMeshProUGUI activeAugmentText;

    [Header("Flow Control")]
    [SerializeField] private bool autoResolveWhenOpened = true;
    [SerializeField, Min(0f)] private float autoResolveDelay = 0.5f;
    [SerializeField] private bool autoResolveRandomChoice = true;

    private AugmentData[] currentChoices;
    private bool selectionResolved;
    private Coroutine autoResolveCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (augmentPanel != null)
            augmentPanel.SetActive(false);

        UpdateActiveAugmentText();
    }

    public void ShowAugments()
    {
        if (AugmentManager.Instance == null)
        {
            GameManager.Instance?.OnAugmentSelected();
            return;
        }

        currentChoices = AugmentManager.Instance.GetRandomAugments();

        if (currentChoices == null || currentChoices.Length == 0)
        {
            GameManager.Instance?.OnAugmentSelected();
            return;
        }

        selectionResolved = false;

        if (autoResolveCoroutine != null)
        {
            StopCoroutine(autoResolveCoroutine);
            autoResolveCoroutine = null;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            if (i < currentChoices.Length)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(currentChoices[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        if (augmentPanel != null)
            augmentPanel.SetActive(true);

        Time.timeScale = 0f;

        if (autoResolveWhenOpened)
            autoResolveCoroutine = StartCoroutine(AutoResolveRoutine());
    }

    private IEnumerator AutoResolveRoutine()
    {
        yield return new WaitForSecondsRealtime(autoResolveDelay);

        if (selectionResolved)
            yield break;

        if (currentChoices == null || currentChoices.Length == 0)
        {
            FinishAugmentPhase();
            yield break;
        }

        int index = autoResolveRandomChoice ? Random.Range(0, currentChoices.Length) : 0;
        ResolveSelection(currentChoices[index]);
    }

    public void OnSelectAugment(AugmentData data)
    {
        ResolveSelection(data);
    }

    private void ResolveSelection(AugmentData data)
    {
        if (selectionResolved)
            return;

        if (data == null)
        {
            Debug.LogWarning("[AugmentUI] 선택할 증강 데이터가 없습니다.");
            FinishAugmentPhase();
            return;
        }

        selectionResolved = true;

        if (autoResolveCoroutine != null)
        {
            StopCoroutine(autoResolveCoroutine);
            autoResolveCoroutine = null;
        }

        AugmentManager.Instance?.ApplyAugment(data);

        if (augmentPanel != null)
            augmentPanel.SetActive(false);

        Time.timeScale = SpeedManager.Instance != null ? SpeedManager.Instance.CurrentSpeed : 1f;

        PassiveManager.Instance?.RecalculatePassives();
        GameManager.Instance?.OnAugmentSelected();

        UpdateActiveAugmentText();
        currentChoices = null;
    }

    private void FinishAugmentPhase()
    {
        if (autoResolveCoroutine != null)
        {
            StopCoroutine(autoResolveCoroutine);
            autoResolveCoroutine = null;
        }

        if (augmentPanel != null)
            augmentPanel.SetActive(false);

        Time.timeScale = SpeedManager.Instance != null ? SpeedManager.Instance.CurrentSpeed : 1f;
        GameManager.Instance?.OnAugmentSelected();
    }

    private void UpdateActiveAugmentText()
    {
        if (activeAugmentText == null)
            return;

        activeAugmentText.text = AugmentManager.Instance != null
            ? AugmentManager.Instance.GetActiveAugmentText()
            : string.Empty;
    }
}