using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private bool isPaused = false;

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        Cronometro.Instance.Retomar();
        Time.timeScale = 1f;   // retoma o tempo do jogo
        isPaused = false;
    }

    public void Pause()
    {
        Cronometro.Instance.Pausar();
        Time.timeScale = 0f;   // congela o tempo do jogo
        isPaused = true;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;   // reseta o tempo antes de trocar de cena
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}