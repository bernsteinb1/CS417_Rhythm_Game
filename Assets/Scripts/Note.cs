using UnityEngine;

namespace PianoMasters.Notes
{
    /// <summary>
    /// Root component on every note prefab: holds the note's type and stats.
    /// Falling, stat gain, death and collisions are handled by separate scripts.
    /// </summary>
    [DisallowMultipleComponent]
    public class Note : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP / HDRP
        static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in

        [SerializeField] NoteType type;
        [SerializeField] NoteStats stats = new NoteStats();

        [Tooltip("Renderers tinted by SetColor (e.g. by the note colour sliders).")]
        [SerializeField] Renderer[] visuals = new Renderer[0];

        MaterialPropertyBlock block;

        public NoteType Type => type;
        public NoteStats Stats => stats;

        /// <summary>Called by the spawner with the stats for this chart entry.</summary>
        public void Initialize(NoteStats chartStats)
        {
            stats = chartStats.Clone();
            if (type == NoteType.Chord)
                stats.keysRemaining = 1 + stats.otherKeys.Length;
        }

        /// <summary>Tints this note without creating new materials.</summary>
        public void SetColor(Color color)
        {
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var r in visuals)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                r.SetPropertyBlock(block);
            }
        }

#if UNITY_EDITOR
        /// <summary>Used by NotePrefabBuilder to set up prefabs.</summary>
        public void EditorSetup(NoteType noteType, NoteStats defaults, Renderer[] renderers)
        {
            type = noteType;
            stats = defaults.Clone();
            visuals = renderers;
        }
#endif
    }
}
