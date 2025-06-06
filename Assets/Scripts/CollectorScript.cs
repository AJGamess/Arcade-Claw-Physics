using TMPro;
using UnityEngine;

public class CollectorScript : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public int scorePerBlock = 100;

    private int totalScore = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Points"))
        {

            totalScore += scorePerBlock;
            UpdateScoreUI();

            Destroy(other.gameObject);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + totalScore;
        }
    }
}
