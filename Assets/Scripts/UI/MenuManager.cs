using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class MenuManager : MonoBehaviour
{
    public GameObject overlay;
    public GameObject settings;

    // Closes the game
    public void QuitGame()
    {
        Application.Quit();
    }

    // Reloads current scene 
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1f;
    }

    // Loads scene 0
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        Time.timeScale = 1f;
    }

    // Loads the next scene
    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1f;
    }

    // Loads previous scene 
    public void GoAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        Time.timeScale = 1f;
    }

    //Opens the Setting menu
    public void Setting()
    {
        overlay.gameObject.SetActive(false);
        settings.gameObject.SetActive(true);
    }

    //Closes the Settings menu
    public void Back()
    {
        overlay.gameObject.SetActive(true);
        settings.gameObject.SetActive(false);
    }

}
