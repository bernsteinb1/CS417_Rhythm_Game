using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrianRhythm
{
    public enum GapPolicy
    {
        NearestLane,
        Ignore
    }

    [ExecuteAlways]
    public class HitTrack : MonoBehaviour
    {
        const string GeneratedName = "Lanes (generated)";

        public int laneCount = 5;
        public float laneWidth = 0.22f;
        public float laneGap = 0.04f;
        public float hitDepth = 0.07f;
        public float laneLength = 2.5f;

        public Vector3 tipOffset = new Vector3(0f, 0f, 0.06f);
        public List<Transform> strikers = new List<Transform>();
        public bool showStrikePoints = true;
        public float strikePointSize = 0.015f;

        public GapPolicy gapPolicy = GapPolicy.NearestLane;
        public float sidePadding = 0.02f;
        public float depthPadding = 0.04f;
        public float minHitSpeed = 0.4f;
        public float maxEntryAngle = 50f;
        public float rearmHeight = 0.02f;

        public float slideHalfHeight = 0.05f;
        public bool showSlidePresence = true;

        public Material padMaterial;
        public Color fillColor = new Color(0.94f, 0.92f, 0.87f, 0.22f);
        public Color edgeColor = new Color(0.95f, 0.93f, 0.89f, 0.8f);
        public Color stripColor = new Color(0.94f, 0.92f, 0.87f, 0.05f);
        public Color flashColor = new Color(1f, 0.97f, 0.91f, 0.85f);
        public Color presenceColor = new Color(0.70f, 0.75f, 0.84f, 0.75f);
        public Color strikePointColor = new Color(0.62f, 0.60f, 0.57f, 1f);
        public float edgeWidth = 0.005f;
        public float glow = 0.4f;
        public float flashTime = 0.25f;

        public ParticleSystem hitEffect;
        public Piano piano;
        public int[] laneKeys = { 12, 14, 16, 19, 21 };
        public bool logHits = true;
        public bool logRejected = true;

        public event Action<LaneHit> Hit;

        class LaneVisual
        {
            public Renderer fill;
            public Renderer[] edges;
            public float flash;
            public float presence;
        }

        class StrikerState
        {
            public Transform marker;
            public Vector3 prevPoint;
            public double prevTime;
            public bool hasPrev;
            public bool armed;
        }

        GameObject generated;
        Material runtimeMaterial;
        MaterialPropertyBlock block;
        readonly List<LaneVisual> lanes = new List<LaneVisual>();
        readonly Dictionary<Transform, StrikerState> states = new Dictionary<Transform, StrikerState>();
        bool autoStrikers;
        float nextStrikerSearch;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public int LaneCount => laneCount;
        float Pitch => laneWidth + laneGap;
        float TotalWidth => laneCount * laneWidth + (laneCount - 1) * laneGap;
        float LaneCenterX(int lane) => -TotalWidth * 0.5f + laneWidth * 0.5f + lane * Pitch;

        public Vector3 LaneCenterWorld(int lane)
        {
            return transform.TransformPoint(new Vector3(LaneCenterX(lane), 0f, 0f));
        }

        public Vector3 StrikePointOf(Transform striker)
        {
            return striker.TransformPoint(tipOffset);
        }

        public int SlideLaneOf(Vector3 worldPoint)
        {
            Vector3 p = transform.InverseTransformPoint(worldPoint);
            if (Mathf.Abs(p.y) > slideHalfHeight) return -1;
            return LaneAt(p.x, p.z, false, out _, out _);
        }

        public bool IsInSlideZone(int lane)
        {
            return IsInSlideZone(lane, out _);
        }

        public bool IsInSlideZone(int lane, out Transform striker)
        {
            foreach (Transform s in strikers)
            {
                if (s == null) continue;
                if (SlideLaneOf(StrikePointOf(s)) == lane)
                {
                    striker = s;
                    return true;
                }
            }
            striker = null;
            return false;
        }

        int LaneAt(float x, float z, bool ignoreGaps, out bool inArea, out float offset)
        {
            offset = 0f;
            float halfDepth = hitDepth * 0.5f + depthPadding;
            float halfWidth = TotalWidth * 0.5f + sidePadding;
            inArea = Mathf.Abs(z) <= halfDepth && Mathf.Abs(x) <= halfWidth;
            if (!inArea) return -1;

            int nearest = Mathf.Clamp(Mathf.RoundToInt((x - LaneCenterX(0)) / Pitch), 0, laneCount - 1);
            offset = x - LaneCenterX(nearest);

            if (Mathf.Abs(offset) <= laneWidth * 0.5f + sidePadding) return nearest;
            return ignoreGaps ? -1 : nearest;
        }

        void OnEnable()
        {
            Build();
        }

        void OnDisable()
        {
            Clear();
            if (autoStrikers)
            {
                strikers.Clear();
                autoStrikers = false;
            }
            nextStrikerSearch = 0f;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall -= Rebuild;
            UnityEditor.EditorApplication.delayCall += Rebuild;
        }

        void Rebuild()
        {
            if (this == null || !isActiveAndEnabled) return;
            Clear();
            Build();
        }
#endif

        void Update()
        {
            if (!Application.isPlaying) return;

            FindStrikers();

            double now = Time.timeAsDouble;
            foreach (Transform s in strikers)
            {
                if (s == null) continue;

                StrikerState state = StateOf(s);
                Vector3 point = StrikePointOf(s);

                state.marker.gameObject.SetActive(showStrikePoints);
                state.marker.localPosition = tipOffset;
                state.marker.localScale = Vector3.one * strikePointSize;

                if (state.hasPrev) CheckTap(s, state, point, now);

                state.prevPoint = point;
                state.prevTime = now;
                state.hasPrev = true;
            }

            UpdateLanes();
        }

        void CheckTap(Transform striker, StrikerState state, Vector3 point, double now)
        {
            Vector3 a = transform.InverseTransformPoint(state.prevPoint);
            Vector3 b = transform.InverseTransformPoint(point);

            if (!state.armed && b.y > rearmHeight) state.armed = true;
            if (!state.armed || a.y <= 0f || b.y > 0f) return;

            float t = a.y / (a.y - b.y);
            Vector3 crossing = Vector3.Lerp(a, b, t);
            int lane = LaneAt(crossing.x, crossing.z, gapPolicy == GapPolicy.Ignore, out bool inArea, out float offset);
            if (!inArea) return;

            state.armed = false;

            double dt = now - state.prevTime;
            if (dt <= 0.0) return;

            Vector3 velocity = (point - state.prevPoint) / (float)dt;
            float speed = velocity.magnitude;
            float angle = Vector3.Angle(velocity, -transform.up);

            if (lane < 0)
            {
                Reject(-1, "landed between lanes");
                return;
            }
            if (speed < minHitSpeed)
            {
                Reject(lane, $"too slow ({speed:0.00} m/s, need {minHitSpeed:0.00})");
                return;
            }
            if (angle > maxEntryAngle)
            {
                Reject(lane, $"too slanted ({angle:0}° from vertical, max {maxEntryAngle:0}°)");
                return;
            }

            double hitTime = state.prevTime + t * dt;
            var hit = new LaneHit
            {
                lane = lane,
                worldPoint = transform.TransformPoint(crossing),
                offsetFromCenter = offset,
                speed = speed,
                entryAngle = angle,
                time = hitTime,
                dspTime = AudioSettings.dspTime - (now - hitTime),
                striker = striker,
            };
            RegisterHit(hit);
        }

        void Reject(int lane, string reason)
        {
            if (!logRejected) return;
            string where = lane >= 0 ? $"Lane {lane + 1}" : "Strike";
            Debug.Log($"[HitTrack] {where} rejected: {reason}");
        }

        void RegisterHit(LaneHit hit)
        {
            if (hit.lane < lanes.Count) lanes[hit.lane].flash = 1f;

            PlayEffect(hitEffect, hit.worldPoint);
            PlayPianoKey(hit.lane);

            if (logHits)
                Debug.Log($"[HitTrack] Lane {hit.lane + 1} HIT at t={hit.time:0.000}s | {hit.offsetFromCenter * 100f:+0.0;-0.0;0.0} cm from centre | {hit.speed:0.00} m/s | {hit.entryAngle:0}° from vertical");

            Hit?.Invoke(hit);
        }

        void FindStrikers()
        {
            if (strikers.Count > 0 && !autoStrikers) return;
            if (autoStrikers || Time.unscaledTime < nextStrikerSearch) return;
            nextStrikerSearch = Time.unscaledTime + 1f;

            var found = new List<Transform>();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "Left Controller" || t.name == "Right Controller")
                    found.Add(t);

            if (found.Count == 0) return;
            strikers.Clear();
            strikers.AddRange(found);
            autoStrikers = true;
        }

        StrikerState StateOf(Transform striker)
        {
            if (states.TryGetValue(striker, out StrikerState state)) return state;

            state = new StrikerState();
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(marker.GetComponent<Collider>());
            marker.name = "Strike Point";
            marker.hideFlags = HideFlags.DontSave;
            marker.transform.SetParent(striker, false);
            var r = marker.GetComponent<Renderer>();
            r.sharedMaterial = runtimeMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            SetColors(r, strikePointColor, Color.black);
            state.marker = marker.transform;

            Vector3 local = transform.InverseTransformPoint(StrikePointOf(striker));
            state.armed = local.y > rearmHeight;
            states[striker] = state;
            return state;
        }

        void PlayEffect(ParticleSystem prefab, Vector3 worldPoint)
        {
            if (prefab == null) return;
            ParticleSystem fx = Instantiate(prefab, worldPoint, Quaternion.LookRotation(transform.up, transform.forward));
            fx.Play(true);
            var main = fx.main;
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.5f);
        }

        void PlayPianoKey(int lane)
        {
            if (piano == null) piano = FindFirstObjectByType<Piano>();
            if (piano == null || piano.keys == null || laneKeys == null || lane >= laneKeys.Length) return;

            int key = laneKeys[lane];
            if (key < 0 || key >= piano.keys.Length || piano.keys[key] == null) return;

            AudioSource src = piano.keys[key];
            if (src.clip != null) src.PlayOneShot(src.clip);
        }

        void UpdateLanes()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < lanes.Count; i++)
            {
                LaneVisual lane = lanes[i];
                float target = showSlidePresence && IsInSlideZone(i) ? 1f : 0f;
                float presence = Mathf.MoveTowards(lane.presence, target, dt / 0.08f);
                float flash = Mathf.Max(0f, lane.flash - dt / flashTime);
                if (presence == lane.presence && flash == lane.flash) continue;
                lane.presence = presence;
                lane.flash = flash;
                ApplyLaneLook(lane);
            }
        }

        void ApplyLaneLook(LaneVisual lane)
        {
            float f = lane.flash * lane.flash;
            float p = lane.presence;

            Color presenceFill = presenceColor;
            presenceFill.a = Mathf.Lerp(fillColor.a, presenceColor.a, 0.5f);

            Color fill = Color.Lerp(fillColor, presenceFill, p * 0.6f);
            fill = Color.Lerp(fill, flashColor, f);
            SetColors(lane.fill, fill, Rgb(fill) * (glow + 2.5f * f));

            Color edge = Color.Lerp(edgeColor, presenceColor, p);
            edge = Color.Lerp(edge, Rgb(flashColor), f);
            foreach (Renderer r in lane.edges)
                SetColors(r, edge, Rgb(edge) * (glow + 2.5f * f));
        }

        static Color Rgb(Color c)
        {
            return new Color(c.r, c.g, c.b, 1f);
        }

        void SetColors(Renderer r, Color baseColor, Color emission)
        {
            if (block == null) block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            emission.a = 1f;
            block.SetColor(BaseColorId, baseColor);
            block.SetColor(ColorId, baseColor);
            block.SetColor(EmissionColorId, emission);
            r.SetPropertyBlock(block);
        }

        void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                if (transform.GetChild(i).name == GeneratedName)
                    DestroyNow(transform.GetChild(i).gameObject);

            runtimeMaterial = MakeMaterial();

            generated = new GameObject(GeneratedName);
            generated.transform.SetParent(transform, false);

            const float sheet = 0.002f;
            float halfW = laneWidth * 0.5f;
            float halfD = hitDepth * 0.5f;

            for (int i = 0; i < laneCount; i++)
            {
                var lane = new LaneVisual();

                var root = new GameObject($"Lane {i + 1}").transform;
                root.SetParent(generated.transform, false);
                root.localPosition = new Vector3(LaneCenterX(i), 0f, 0f);

                lane.fill = MakeBlock("Hit Region", root, Vector3.zero, new Vector3(laneWidth, sheet, hitDepth));
                lane.edges = new[]
                {
                    MakeBlock("Edge Near", root, new Vector3(0f, 0f, -halfD), new Vector3(laneWidth + edgeWidth, sheet * 1.5f, edgeWidth)),
                    MakeBlock("Edge Far", root, new Vector3(0f, 0f, halfD), new Vector3(laneWidth + edgeWidth, sheet * 1.5f, edgeWidth)),
                    MakeBlock("Edge Left", root, new Vector3(-halfW, 0f, 0f), new Vector3(edgeWidth, sheet * 1.5f, hitDepth + edgeWidth)),
                    MakeBlock("Edge Right", root, new Vector3(halfW, 0f, 0f), new Vector3(edgeWidth, sheet * 1.5f, hitDepth + edgeWidth)),
                };

                if (laneLength > 0f)
                {
                    Renderer strip = MakeBlock("Lane", root,
                        new Vector3(0f, -sheet, halfD + edgeWidth * 0.5f + laneLength * 0.5f),
                        new Vector3(laneWidth, sheet * 0.5f, laneLength));
                    SetColors(strip, stripColor, Rgb(stripColor) * (glow * 0.5f));
                }

                lanes.Add(lane);
                ApplyLaneLook(lane);
            }

            HideFromSave(generated);
        }

        static void HideFromSave(GameObject go)
        {
            go.hideFlags = HideFlags.DontSave;
            foreach (Transform child in go.transform) HideFromSave(child.gameObject);
        }

        static void DestroyNow(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        Renderer MakeBlock(string name, Transform parent, Vector3 localPos, Vector3 size)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = runtimeMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        Material MakeMaterial()
        {
            Material m = padMaterial != null
                ? new Material(padMaterial)
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));

            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_BlendModePreserveSpecular", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_EMISSION");
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.renderQueue = (int)RenderQueue.Transparent;
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.hideFlags = HideFlags.DontSave;
            return m;
        }

        void Clear()
        {
            foreach (StrikerState state in states.Values)
                if (state.marker != null) DestroyNow(state.marker.gameObject);
            states.Clear();

            if (generated != null) DestroyNow(generated);
            generated = null;
            lanes.Clear();

            if (runtimeMaterial != null) DestroyNow(runtimeMaterial);
            runtimeMaterial = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 tapSize = new Vector3(laneWidth + sidePadding * 2f, 0.001f, hitDepth + depthPadding * 2f);
            Vector3 slideSize = new Vector3(laneWidth + sidePadding * 2f, slideHalfHeight * 2f, hitDepth + depthPadding * 2f);

            for (int i = 0; i < laneCount; i++)
            {
                Vector3 c = new Vector3(LaneCenterX(i), 0f, 0f);
                Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
                Gizmos.DrawWireCube(c, tapSize);
                Gizmos.color = new Color(0.5f, 0.75f, 1f, 0.5f);
                Gizmos.DrawWireCube(c, slideSize);
            }
        }
    }
}
