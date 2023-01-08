using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoop : MonoBehaviour {
    [SerializeField] private Player player;
    [SerializeField] private Enemies enemies;
    [SerializeField] private Blood blood;
    [SerializeField] private Corpses corpses;

    private bool levelWasLoaded = false;
    private int loadedLevelIndex = 0;

    private void Update() {
        if (Input.GetKeyUp(KeyCode.Return)) {
           LoadLevel(1); 
        }

        if (levelWasLoaded) {
            var spawn = GameObject.Find("PlayerSpawn");
            if (!spawn) {
                return;
            }

            var goal = GameObject.Find("Goal");

            levelWasLoaded = false;
            Debug.Assert(spawn);
            player.OnLevelLoaded(spawn.transform.position, goal);
            enemies.OnLevelLoaded();
        }
    }

    public void LoadLevel(int level) {
        loadedLevelIndex = level;
        SceneManager.LoadScene(level, LoadSceneMode.Additive);

        levelWasLoaded = true;
    }
    
    public void Die() {
        SceneManager.UnloadSceneAsync(loadedLevelIndex);
        loadedLevelIndex = 0;
        blood.Clear();
        corpses.Clear();
    }

    public void Win() {
    }
}
