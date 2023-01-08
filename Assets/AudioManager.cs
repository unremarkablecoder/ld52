using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public AudioSource kill;
    public AudioSource grab;
    public AudioSource drop;
    public AudioSource huh;
    public AudioSource disappeared;
    public AudioSource ohWell;
    public AudioSource body;
    public AudioSource die;
    public AudioSource powerToggle;
    public AudioSource door;
    public AudioSource overThere;
    public AudioSource alertSound;

    public AudioSource musicNormal;
    public AudioSource musicAlert;

    private AudioSource musicNormalSound;
    private AudioSource musicAlertSound;
    private bool alert = false;

    private float alertFade = 0;

    public void Play(AudioSource source) {
        var sound = Instantiate(source);
        Destroy(sound.gameObject, sound.clip.length);
    }

    void Start() {
        musicNormalSound = Instantiate(musicNormal);
        musicAlertSound = Instantiate(musicAlert);
        musicNormalSound.Play();
        musicAlertSound.volume = 0;
        musicAlertSound.Play();
    }

    public void SetAlert(bool alert) {
        this.alert = alert;
    }

    private void Update() {
        const float fadeSpeed = 2;
        if (alert) {
            alertFade = Mathf.Min(1.0f, alertFade + fadeSpeed * Time.deltaTime);
        }
        else {
            alertFade = Mathf.Max(0.0f, alertFade - fadeSpeed * Time.deltaTime);
        }

        musicNormalSound.volume = 1.0f - alertFade;
        musicAlertSound.volume = alertFade;
    }
}

