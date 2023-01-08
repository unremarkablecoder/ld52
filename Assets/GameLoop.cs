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
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject dieScreen;
    [SerializeField] private GameObject levelSelectScreen;
    private AudioManager audioManager;

    private bool levelWasLoaded = false;
    private int loadedLevelIndex = 0;
    private bool inLevel = false;

    void Awake() {
        audioManager = FindObjectOfType<AudioManager>();
    }
    
    private void Update() {
        if (!inLevel) {
            if (Input.GetKeyUp(KeyCode.Alpha1)) {
                LoadLevel(1);
            }

            if (Input.GetKeyUp(KeyCode.Alpha2)) {
                LoadLevel(2);
            }

            if (Input.GetKeyUp(KeyCode.Alpha3)) {
                LoadLevel(3);
            }

            if (Input.GetKeyUp(KeyCode.Alpha4)) {
                LoadLevel(4);
            }
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
        inLevel = true;
        winScreen.SetActive(false);
        dieScreen.SetActive(false);
        levelSelectScreen.SetActive(false);
        
        loadedLevelIndex = level;
        SceneManager.LoadScene(level, LoadSceneMode.Additive);

        levelWasLoaded = true;
    }

    void UnloadLevel() {
        SceneManager.UnloadSceneAsync(loadedLevelIndex);
        loadedLevelIndex = 0;
        blood.Clear();
        corpses.Clear();
        inLevel = false;
    }
    
    public void Die() {
        UnloadLevel();
        dieScreen.SetActive(true);
        levelSelectScreen.SetActive(true);
        audioManager.SetAlert(false);
        enemies.Unload();
        
    }

    public void Win() {
        UnloadLevel();
        winScreen.SetActive(true);
        levelSelectScreen.SetActive(true);
        audioManager.SetAlert(false);
        enemies.Unload();
    }
}
