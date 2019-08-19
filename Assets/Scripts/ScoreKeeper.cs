using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreKeeper : MonoBehaviour
{
    private Text scoreDisplay;
    private int score;

    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<Projectiles>().onHitTarget += UpdateScore;
        score = 0;
        scoreDisplay = GetComponentInChildren<Text>();

       
        scoreDisplay.text = $"Score: {score}";
    }

    void UpdateScore()
    {
        score += 10;
        scoreDisplay.text = $"Score: {score}";
    }
}
