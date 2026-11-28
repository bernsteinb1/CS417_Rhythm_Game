using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Calibrator : MonoBehaviour
{
    const double tempo = 60;
    public AudioSource click;
    public InputActionReference calibrateButton;
    double nextTime;
    double currentTime;
    double expectedTime;
    int leadIn = 4;
    int calibrationBeats = 12;
    int beats;
    double dspAtFrame, realAtFrame;

    double offsetSum = 0.0;

    void Awake()
    {
        beats = leadIn + calibrationBeats;
        Calibrate();
        calibrateButton.action.Enable();
        calibrateButton.action.performed += UserInput;
    }

    private void UserInput(InputAction.CallbackContext context)
    {
        double tapTime = dspAtFrame + (context.time - realAtFrame);
        double offset = tapTime - expectedTime;
        offsetSum += offset;
        Debug.Log(offset);
        expectedTime += 60.0 / tempo;
    }

    void Calibrate()
    {
        currentTime = AudioSettings.dspTime + 0.1;
        nextTime = currentTime + 60.0 / tempo;
        expectedTime = nextTime + 60.0 / tempo * leadIn; // 4 beats before calibration starts
    }

    void OnDestroy()
    {
        Game.Instance.SetLatency(offsetSum / calibrationBeats);
        calibrateButton.action.performed -= UserInput;
    }

    void Update()
    {
        if (beats < 0) return;
        realAtFrame = Time.realtimeSinceStartupAsDouble;
        dspAtFrame = AudioSettings.dspTime;
        if (AudioSettings.dspTime > currentTime + click.clip.length)
        {
            beats -= 1;
            click.PlayScheduled(nextTime);
            currentTime = nextTime;
            nextTime += 60.0 / tempo; // next time gets incremented to next beat.
        }

        if (beats < 0) 
        {
            Destroy(gameObject, 2);
        }
    }
}
