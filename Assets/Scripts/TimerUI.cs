using UnityEngine;
using UnityEngine.UIElements;

public class TimerUI : MonoBehaviour
{
    [Header("Real & Shift Durations")]
    [SerializeField] private float realDurationInSeconds = 300f; 
    [SerializeField] private float shiftDurationInHours = 8f;    

    [Header("Display")]
    [SerializeField] private string timerLabelName = "TimerLabel";
    [SerializeField] private bool showWholeHoursOnly = true;     

    private Label timerLabel;
    private float elapsedRealTime;
    private bool isRunning = true;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        timerLabel = root.Q<Label>(timerLabelName);

        if (timerLabel == null)
        {
            Debug.LogError($"Could not find a Label named '{timerLabelName}' in the UI Document.");
            return;
        }

        elapsedRealTime = 0f;
        UpdateTimerDisplay(shiftDurationInHours);
    }

    private void Update()
    {
        if (!isRunning || timerLabel == null) return;

        elapsedRealTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedRealTime / realDurationInSeconds);
        float remainingShiftHours = shiftDurationInHours * (1f - progress);

        UpdateTimerDisplay(remainingShiftHours);

        if (progress >= 1f)
        {
            isRunning = false;
            OnTimerFinished();
        }
    }

    private void UpdateTimerDisplay(float remainingHours)
    {
        if (showWholeHoursOnly)
        {
            int hoursLeft = Mathf.CeilToInt(remainingHours);
            timerLabel.text = $"{hoursLeft}h";
        }
        else
        {
            float clampedHours = Mathf.Max(0f, remainingHours);
            int hours = Mathf.FloorToInt(clampedHours);
            int minutes = Mathf.FloorToInt((clampedHours - hours) * 60f);
            timerLabel.text = $"{hours:00}:{minutes:00}";
        }
    }

    private void OnTimerFinished()
    {
        Debug.Log("Timer finished!");
    }

    // Optional helper methods you can call from other scripts
    public void PauseTimer() => isRunning = false;
    public void ResumeTimer() => isRunning = true;
    public void ResetTimer()
    {
        elapsedRealTime = 0f;
        isRunning = true;
        UpdateTimerDisplay(shiftDurationInHours);
    }
}