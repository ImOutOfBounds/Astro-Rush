using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    // Fun��o chamada ao clicar no bot�o "Jogar"
    public void Jogar()
    {
        // Carrega a cena "SampleScene" diretamente
        SceneManager.LoadScene("SampleScene");
    }
}
