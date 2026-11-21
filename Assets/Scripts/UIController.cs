using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    private Button startButton, creditsButton, quitButton, mainmenuButton;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        startButton = root.Q<Button>("start-button");
        creditsButton = root.Q<Button>("credits-button");
        quitButton = root.Q<Button>("quit-button");

        startButton.clicked += StartButtonPressed;
        creditsButton.clicked += CreditsButtonPressed;
        quitButton.clicked += QuitButtonPressed;
    }

    void StartButtonPressed()
    {
        SceneManager.LoadScene("MainScene");
        Debug.Log("Start game pressed!");
    }

    void CreditsButtonPressed()
    {
        Debug.Log(SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Credits.unity"));
        SceneManager.LoadScene("CreditScene");

        Debug.Log("Credits pressed!");
    }

    void QuitButtonPressed()
    {
        Debug.Log("Quit game pressed!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}

