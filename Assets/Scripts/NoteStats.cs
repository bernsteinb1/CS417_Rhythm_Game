using System;
using UnityEngine;

namespace PianoMasters.Notes
{
    /// <summary>
    /// Per-note stats. Values on the prefab are defaults; the spawner overwrites them
    /// from the chart when it creates a note. (Spawn Timer, the 8th stat, lives on the spawner.)
    /// </summary>
    [Serializable]
    public class NoteStats
    {
        [Tooltip("Metres above the target key. Drops as the note falls; the note dies at 0.")]
        [Min(0f)] public float height = 2f;

        [Tooltip("Fall speed in metres per second.")]
        [Min(0f)] public float fallSpeed = 1f;

        [Tooltip("Hold notes only: seconds the key must still be held.")]
        [Min(0f)] public float holdRemaining;

        [Tooltip("Seconds left to hit the note once it reaches its key.")]
        [Min(0f)] public float hitWindow = 0.15f;

        [Tooltip("Points awarded for a hit (before judgment multipliers).")]
        [Min(0)] public int pointValue = 100;

        [Tooltip("Index of the key this note lands on (0 = lowest key on the piano).")]
        [Min(0)] public int targetKey;

        [Tooltip("Chord: the other keys in the chord. Slide: the end key. Trill: the alternate key.")]
        public int[] otherKeys = Array.Empty<int>();

        [Tooltip("Chord notes only: keys still to be struck.")]
        [Min(0)] public int keysRemaining = 1;

        public NoteStats Clone()
        {
            var copy = (NoteStats)MemberwiseClone();
            copy.otherKeys = otherKeys != null ? (int[])otherKeys.Clone() : Array.Empty<int>();
            return copy;
        }
    }
}
