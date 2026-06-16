using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    private List<TutorialPageData> pages;
    private Dictionary<string, Sprite> iconMap;
    private TMP_FontAsset koreanFont;
    private Action onClosed;
    private int pageIndex;

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI contentText;
    private TextMeshProUGUI pageIndicatorText;
    private Image iconImage;
    private GameObject iconObj;
    private Button prevButton;
    private Button nextButton;
    private TextMeshProUGUI nextButtonLabel;

    public static TutorialUI Show(List<TutorialPageData> pages, Dictionary<string, Sprite> iconMap, TMP_FontAsset koreanFont, Action onClosed)
    {
        GameObject canvasObj = new GameObject("TutorialCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        TutorialUI ui = canvasObj.AddComponent<TutorialUI>();
        ui.pages = pages;
        ui.iconMap = iconMap;
        ui.koreanFont = koreanFont;
        ui.onClosed = onClosed;
        ui.Build();
        return ui;
    }

    private void Build()
    {
        RectTransform dimRect = CreateUIObject("Dim", transform, out Image dimImage);
        dimImage.color = new Color(0f, 0f, 0f, 0.65f);
        Stretch(dimRect);

        RectTransform card = CreateUIObject("Card", transform, out Image cardImage);
        cardImage.color = new Color(0.10f, 0.13f, 0.20f, 0.97f);
        card.anchorMin = new Vector2(0.08f, 0.18f);
        card.anchorMax = new Vector2(0.92f, 0.82f);
        card.offsetMin = Vector2.zero;
        card.offsetMax = Vector2.zero;

        RectTransform titleRect = CreateUIObject("Title", card, out _, addImage: false);
        titleText = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        SetupText(titleText, 48, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.4f));
        titleRect.anchorMin = new Vector2(0f, 0.82f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        RectTransform iconRect = CreateUIObject("Icon", card, out iconImage);
        iconImage.preserveAspect = true;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(220, 220);
        iconRect.anchoredPosition = new Vector2(0f, 120f);
        iconObj = iconRect.gameObject;

        RectTransform contentRect = CreateUIObject("Content", card, out _, addImage: false);
        contentText = contentRect.gameObject.AddComponent<TextMeshProUGUI>();
        SetupText(contentText, 34, FontStyles.Normal, TextAlignmentOptions.Top, Color.white);
        contentRect.anchorMin = new Vector2(0.06f, 0.16f);
        contentRect.anchorMax = new Vector2(0.94f, 0.55f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        RectTransform pageRect = CreateUIObject("PageIndicator", card, out _, addImage: false);
        pageIndicatorText = pageRect.gameObject.AddComponent<TextMeshProUGUI>();
        SetupText(pageIndicatorText, 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.75f));
        pageRect.anchorMin = new Vector2(0f, 0.08f);
        pageRect.anchorMax = new Vector2(1f, 0.16f);
        pageRect.offsetMin = Vector2.zero;
        pageRect.offsetMax = Vector2.zero;

        prevButton = CreateButton("이전", card, new Vector2(0.06f, 0.0f), new Vector2(0.30f, 0.10f), out _);
        prevButton.onClick.AddListener(OnPrev);

        nextButton = CreateButton("다음", card, new Vector2(0.70f, 0.0f), new Vector2(0.94f, 0.10f), out nextButtonLabel);
        nextButton.onClick.AddListener(OnNext);

        pageIndex = 0;
        RefreshPage();
    }

    private RectTransform CreateUIObject(string name, Transform parent, out Image image, bool addImage = true)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        image = addImage ? obj.AddComponent<Image>() : null;
        return obj.GetComponent<RectTransform>();
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetupText(TextMeshProUGUI text, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        if (koreanFont != null) text.font = koreanFont;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
    }

    private Button CreateButton(string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI labelText)
    {
        RectTransform rect = CreateUIObject(label + "Button", parent, out Image image);
        image.color = new Color(0.2f, 0.45f, 0.6f, 1f);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Button button = rect.gameObject.AddComponent<Button>();

        RectTransform textRect = CreateUIObject("Label", rect, out _, addImage: false);
        labelText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        SetupText(labelText, 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        labelText.text = label;
        Stretch(textRect);

        return button;
    }

    private void RefreshPage()
    {
        TutorialPageData page = pages[pageIndex];

        titleText.text = page.title;
        contentText.text = page.content;
        pageIndicatorText.text = (pageIndex + 1) + " / " + pages.Count;

        if (!string.IsNullOrEmpty(page.iconKey) && iconMap.TryGetValue(page.iconKey, out Sprite sprite) && sprite != null)
        {
            iconImage.sprite = sprite;
            iconObj.SetActive(true);
        }
        else
        {
            iconObj.SetActive(false);
        }

        prevButton.gameObject.SetActive(pageIndex > 0);

        bool isLast = pageIndex >= pages.Count - 1;
        nextButtonLabel.text = isLast ? "확인" : "다음";
    }

    private void OnPrev()
    {
        if (pageIndex > 0)
        {
            pageIndex--;
            RefreshPage();
        }
    }

    private void OnNext()
    {
        if (pageIndex < pages.Count - 1)
        {
            pageIndex++;
            RefreshPage();
        }
        else
        {
            Close();
        }
    }

    private void Close()
    {
        onClosed?.Invoke();
        Destroy(gameObject);
    }
}
