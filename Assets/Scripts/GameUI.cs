using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUI : MonoBehaviour
{
    private Label cropsLabel;
    private Label scoreLabel;
    private Label moneyLabel;
    private int totalCropsPlaced = 0;
    private int totalScore = 0;
    private int totalMoney = 0;

    private VisualElement uiRoot;
    private VisualElement energyBarMask;
    private Label promptLabel;

    void Awake()
    {
        UIDocument UIEnergy = GetComponent<UIDocument>();
        VisualElement root = UIEnergy.rootVisualElement;
        uiRoot = root;

        cropsLabel = root.Q<Label>("crop_text_label");
        scoreLabel = root.Q<Label>("Score_Text_Label");

        energyBarMask = root.Q<VisualElement>("energy-bar-mask");

        // Use a label named "interact-prompt" from the UXML if there is one,
        // otherwise build a simple one in code.
        promptLabel = root.Q<Label>("interact-prompt");
        if (promptLabel == null)
            promptLabel = CreatePromptLabel(root);
        promptLabel.style.display = DisplayStyle.None;

        // Same idea for the money text: use "money_text_label" from the UXML if present.
        moneyLabel = root.Q<Label>("money_text_label");
        if (moneyLabel == null)
            moneyLabel = CreateMoneyLabel(root);

        UpdateDisplay();
    }

    Label CreatePromptLabel(VisualElement root)
    {
        Label label = new Label();
        label.name = "interact-prompt";
        label.pickingMode = PickingMode.Ignore;
        label.style.position = Position.Absolute;
        label.style.left = 0;
        label.style.right = 0;
        label.style.bottom = 120;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.fontSize = 28;
        label.style.color = Color.white;
        root.Add(label);
        return label;
    }

    Label CreateMoneyLabel(VisualElement root)
    {
        Label label = new Label();
        label.name = "money_text_label";
        label.pickingMode = PickingMode.Ignore;
        label.style.position = Position.Absolute;
        label.style.top = 20;
        label.style.right = 20;
        label.style.unityTextAlign = TextAnchor.MiddleRight;
        label.style.fontSize = 28;
        label.style.color = Color.white;
        root.Add(label);
        return label;
    }

    // ---------- Speech bubbles (one per customer) ----------

    public Label CreateSpeechBubble()
    {
        if (uiRoot == null) return null;

        Label bubble = new Label();
        bubble.pickingMode = PickingMode.Ignore;
        bubble.style.position = Position.Absolute;
        bubble.style.maxWidth = 280;
        bubble.style.whiteSpace = WhiteSpace.Normal;
        bubble.style.unityTextAlign = TextAnchor.MiddleCenter;
        bubble.style.fontSize = 20;
        bubble.style.color = Color.white;
        bubble.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
        bubble.style.paddingTop = 6;
        bubble.style.paddingBottom = 6;
        bubble.style.paddingLeft = 12;
        bubble.style.paddingRight = 12;
        bubble.style.borderTopLeftRadius = 10;
        bubble.style.borderTopRightRadius = 10;
        bubble.style.borderBottomLeftRadius = 10;
        bubble.style.borderBottomRightRadius = 10;
        bubble.style.display = DisplayStyle.None;

        uiRoot.Add(bubble);
        return bubble;
    }

    // Shows the bubble with the given text above a point in the world, or hides it if the text is empty.
    public void UpdateSpeechBubble(Label bubble, Vector3 worldPosition, string text)
    {
        if (bubble == null || uiRoot == null || uiRoot.panel == null) return;

        Camera cam = Camera.main;
        if (string.IsNullOrEmpty(text) || cam == null)
        {
            bubble.style.display = DisplayStyle.None;
            return;
        }

        // Hide it when the point is behind the camera.
        Vector3 viewport = cam.WorldToViewportPoint(worldPosition);
        if (viewport.z <= 0f)
        {
            bubble.style.display = DisplayStyle.None;
            return;
        }

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(uiRoot.panel, worldPosition, cam);

        float width = bubble.resolvedStyle.width;
        float height = bubble.resolvedStyle.height;
        if (float.IsNaN(width)) width = 0f;
        if (float.IsNaN(height)) height = 0f;

        bubble.text = text;
        bubble.style.display = DisplayStyle.Flex;
        bubble.style.left = panelPosition.x - width * 0.5f;
        bubble.style.top = panelPosition.y - height;
    }

    public void RemoveSpeechBubble(Label bubble)
    {
        if (bubble != null)
            bubble.RemoveFromHierarchy();
    }

    // ---------- Prompt, bars and numbers ----------

    public void ShowPrompt(string text)
    {
        if (promptLabel == null) return;
        promptLabel.text = text;
        promptLabel.style.display = DisplayStyle.Flex;
    }

    public void HidePrompt()
    {
        if (promptLabel == null) return;
        promptLabel.style.display = DisplayStyle.None;
    }

    public void UpdateEnergyBar(int energy, int maxEnergy)
    {
        if (energyBarMask != null)
        {
            float energyPercent = (float)energy / maxEnergy * 100f;
            energyBarMask.style.width = Length.Percent(energyPercent);
            Debug.Log("Percentage: " + energyPercent);
        }
    }

    public void UpdateCropsPlaced(int cropsPlaced)
    {
        totalCropsPlaced = cropsPlaced;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (cropsLabel != null)
            cropsLabel.text = "Crops placed: " + totalCropsPlaced;

        if (scoreLabel != null)
            scoreLabel.text = "Score: " + totalScore;

        if (moneyLabel != null)
            moneyLabel.text = "Money: " + totalMoney;
    }

    public void AddMoney(int amount)
    {
        totalMoney += amount;
        UpdateDisplay();
    }

    public void AddScore(int points)
    {
        totalScore += points;
        UpdateDisplay();
    }

    public void RemoveScore(int points)
    {
        totalScore -= points;

        if (totalScore < 0)
        {
            totalScore = 0;
        }

        UpdateDisplay();
    }
}