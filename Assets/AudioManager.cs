using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public AudioSource kill;

    public void Play(AudioSource source) {
        var sound = Instantiate(source);
        Destroy(sound.gameObject, sound.clip.length);
    }
}

