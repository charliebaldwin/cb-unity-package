using UnityEngine;
using UnityEngine.Audio;

public class AudioTest : MonoBehaviour
{
    public AudioMixer mixer;
    public AudioSource source;
    [Range(0.1f, 3f)]
    public float speed = 1.0f;

    // Update is called once per frame
    void Update()
    {
        mixer.SetFloat("PitchGroup", speed);
        mixer.SetFloat("PitchEffect", 1f / speed);
        //source.pitch = 1f / speed;
    }
}
