using UnityEngine;
using TMPro;
public class GameUI : MonoBehaviour
{
    public static GameUI Instance;
    public GameObject interactionPanel;
    public TextMeshProUGUI infoText;

    private object currentOwner;

    void Awake()
    {
        Instance = this;
        if (interactionPanel != null) interactionPanel.SetActive(false);
    }

    public void ShowText(string message, object owner)
    {
        currentOwner = owner;
        if (infoText != null) infoText.text = message;
        if (interactionPanel != null) interactionPanel.SetActive(true);
    }

    public void HideText(object owner)
    {
        if (currentOwner == owner && interactionPanel != null)
        {
            interactionPanel.SetActive(false);
            currentOwner = null;
        }
    }
}