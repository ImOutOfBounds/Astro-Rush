using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    // Função chamada ao clicar no botão "Jogar"
    public void Jogar()
    {
        // Carrega a cena "SampleScene" diretamente
        SceneManager.LoadScene("SampleScene");
    }
}
