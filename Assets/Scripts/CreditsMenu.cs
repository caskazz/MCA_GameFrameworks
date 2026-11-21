using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class CreditsMenu : MonoBehaviour
{
    private Button mainmenuButton;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        mainmenuButton = root.Q<Button>("main-menu-button");

        mainmenuButton.clicked += MainMenu;

    }
    void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");

        Debug.Log("Main Menu pressed!");
    }
}
