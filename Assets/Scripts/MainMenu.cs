using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; 

public class MainMenu : MonoBehaviour
{
    public TMP_Text titleText;  

    // This method will be linked to the Start button
    public void StartGame()
    {
        SceneManager.LoadScene("Game");  // Load the game scene
    }

    // This method will be linked to the Quit button
    public void QuitGame()
    {
        Debug.Log("Quit Game");  // Logs in the editor console
        Application.Quit();  // Quits the application (doesn't work in editor)
    }
}
