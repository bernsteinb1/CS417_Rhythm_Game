using Unity.XR.CoreUtils;
using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game Instance { get; private set; }
    public XROrigin rig;
    private double latency;

    public double GetLatency()
    {
        return latency;
    }

    public void SetLatency(double newLatency)
    {
        latency = newLatency;
    }
    
    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy the duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(rig);
        DontDestroyOnLoad(gameObject);
    }

}
