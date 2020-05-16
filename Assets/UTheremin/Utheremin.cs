
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Utheremin : UdonSharpBehaviour
{
    private AudioSource source;
    private AudioClip clip;

    public float freq = 440f;

    // kludgy phase matching; might be a better way to do this
    float prevFreq = 440f;
    float phase;

    // very low headroom for computation
    private const int SAMPLE_RATE = 6000;

    // there's a click artifact when the sample loops around that's
    // hard to avoid, so make it pretty long.
    private const int SAMPLEN = SAMPLE_RATE * 30;

    // need to write ahead of the read head since audio is playing as we update
    // the frame; otherwise would get more clicks/artifacts. this affects how
    // responsive the theremin is too if it's too long.
    public int writeAheadSamples = 256;

    // how many samples we can write every frame; udon is
    // very slow so not many;
    // in theory we only need to write as many samples as would
    // need to fill a frame (at 90hz and 8192 sample rate, 90 samples).
    // suffer some overwrite in case of framedrops.
    private const int SAMPLES_PER_FRAME = 256;
    private float[] buffer = new float[SAMPLES_PER_FRAME];

    private int writeHead = 0;

    public Transform pitchCenter;
    public Transform volumeCenter;

    public Transform leftHand, rightHand;

    public Material boxMaterial;

    Vector2 pitchCenterXZ;

    public Slider lowFreqSlider, highFreqSlider;
    public Text lowText, highText;

    [UdonSynced]
    public float lowFreq = 20f;
    
    [UdonSynced]
    public float highFreq = 2048f;

    [UdonSynced]
    public bool playing = false;

    const int SINE = 0, SQUARE = 1, SAW = 2, TRIANGLE = 3;

    [UdonSynced]
    public int waveform = SINE;

    public Toggle sineToggle, squareToggle, sawToggle, triangleToggle;

    // UI events

    public void SetWaveform()
    {
        if (!Networking.IsOwner(gameObject)) return;
        waveform = sineToggle.isOn ? SINE :
            squareToggle.isOn ? SQUARE :
            sawToggle.isOn ? SAW :
            triangleToggle.isOn ? TRIANGLE : SINE;
        UpdateVisuals();
    }

    public void TakeOwnership()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Networking.SetOwner(Networking.LocalPlayer, leftHand.gameObject);
        Networking.SetOwner(Networking.LocalPlayer, rightHand.gameObject);
    }

    public void TogglePower()
    {
        if (Networking.IsOwner(gameObject))
        {
            playing = !playing;
            UpdateVisuals();
        }
    }

    public override void OnDeserialization()
    {
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        boxMaterial.color = playing ? Color.white : Color.grey;
        if (playing)
        {
            source.Play();
        } else
        {
            source.Stop();
        }
        switch (waveform)
        {
            case SINE: sineToggle.isOn = true; break;
            case SQUARE: squareToggle.isOn = true; break;
            case SAW: sawToggle.isOn = true; break;
            case TRIANGLE: triangleToggle.isOn = true; break;
        }
    }

    void Start()
    {
        source = GetComponent<AudioSource>();
        clip = AudioClip.Create(gameObject.name, SAMPLEN, 1, SAMPLE_RATE, false);

        source.clip = clip;
        source.loop = true;

        pitchCenterXZ = new Vector2(pitchCenter.position.x, pitchCenter.position.z);
    }

    int lastTimeSamples;

    int absoluteReadHead;

    const float TWOPI = 2 * Mathf.PI;

    void Update()
    {
        if (!playing) return;

        var r = rightHand.transform.position;

        if (Networking.IsOwner(gameObject))
        {
            lowFreq = lowFreqSlider.value;
            highFreq = highFreqSlider.value;
        } else
        {
            lowFreqSlider.value = lowFreq;
            highFreqSlider.value = highFreq;
        }
        lowText.text = $"low freq: {lowFreq}Hz";
        highText.text = $"high freq: {highFreq}Hz";

        freq =
            Mathf.Lerp(
                lowFreq, highFreq,
                Mathf.Pow(
                    1 - Mathf.InverseLerp(0.0f, 0.4f, Vector2.Distance(pitchCenterXZ, new Vector2(r.x, r.z))),
                    2f)
                );
        source.volume = Mathf.Pow(
            Mathf.InverseLerp(
                0.05f, 0.2f, Vector3.Distance(volumeCenter.position, leftHand.transform.position)),
            2);

        var readHead = source.timeSamples;
        // how far the read head advanced in the last frame
        var deltaTimeSamples = (SAMPLEN + readHead - lastTimeSamples) % SAMPLEN;

        absoluteReadHead += deltaTimeSamples;
        // write slightly ahead; seems to solve some artifacts when changing pitch; but still not great.
        var absoluteWriteHead = absoluteReadHead + writeAheadSamples;

        writeHead = (absoluteReadHead) % SAMPLEN;

        // phase match for (new) freq
        // awhTime * prevFreq * TWOPI + phase = awhTime * freq * TWOPI + newPhase % TWOPI
        // newPhase = awhTime * TWOPI * (prevFreq - freq) + phase;
        if (prevFreq != freq)
        {
            float awhTime = absoluteWriteHead / (float)SAMPLE_RATE;
            phase = awhTime * TWOPI * (prevFreq - freq) + phase;
            prevFreq = freq;
        }

        // could try to adaptively write less samples if the framerate is
        // faster to save CPU, but not really worth the effort.
        switch (waveform)
        {
            case SINE:
                for (int i = 0; i < SAMPLES_PER_FRAME; ++i)
                {
                    float t = absoluteWriteHead++ / (float)SAMPLE_RATE;
                    buffer[i] = Mathf.Sin(t * TWOPI * freq + phase);
                }
                break;
            case SQUARE:
                for (int i = 0; i < SAMPLES_PER_FRAME; ++i)
                {
                    float t = absoluteWriteHead++ / (float)SAMPLE_RATE;
                    buffer[i] = Mathf.Sign(Mathf.Sin(t * TWOPI * freq + phase));
                }
                break;
            case SAW:
                for (int i = 0; i < SAMPLES_PER_FRAME; ++i)
                {
                    float t = absoluteWriteHead++ / (float)SAMPLE_RATE;
                    buffer[i] = Mathf.Repeat(t * freq + phase / TWOPI, 1) * 2 -1;               }
                break;
            case TRIANGLE:
                for (int i = 0; i < SAMPLES_PER_FRAME; ++i)
                {
                    float t = absoluteWriteHead++ / (float)SAMPLE_RATE;
                    buffer[i] = Mathf.PingPong(2 * (t * freq + phase / TWOPI), 1) * 2 - 1;
                }
                break;
        }

        // write the segment into the clip, hopefully enough ahead of real time playback.
        clip.SetData(buffer, writeHead);

        lastTimeSamples = readHead;
    }
}
