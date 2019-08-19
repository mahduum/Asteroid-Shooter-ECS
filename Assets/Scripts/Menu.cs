using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    void Start()
    {
        FindObjectOfType<Player>().playerDeathEvent += OnPlayerDeath;
        gameObject.SetActive(false);
    }

    void OnPlayerDeath()
    {
        gameObject.SetActive(true);
    }
}
