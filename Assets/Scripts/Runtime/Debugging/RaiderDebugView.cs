using StealthRaider.AI;
using UnityEngine;

namespace StealthRaider.Debugging
{
    /// <summary>
    /// Draws, in the game view: the vision cone (clipped by walls, coloured by state, brighter as the
    /// meter fills), the last known position with a line to it, and a label with state, meter and
    /// the last transition's reason.
    /// </summary>
    [RequireComponent(typeof(RaiderBrain))]
    public class RaiderDebugView : MonoBehaviour
    {
        const int ConeRays = 48;
        const int MarkerSegments = 24;

        [SerializeField] Material coneMaterial;
        [SerializeField] Material lineMaterial;

        RaiderBrain brain;
        GameObject coneObject;
        Mesh coneMesh;
        Vector3[] vertices;
        Color[] colors;
        LineRenderer lkpLine;
        LineRenderer lkpMarker;
        GUIStyle labelStyle;
        GUIStyle smallStyle;

        void Awake()
        {
            brain = GetComponent<RaiderBrain>();

            coneObject = new GameObject($"{name} Vision Cone");
            coneMesh = new Mesh { name = "VisionCone" };
            coneMesh.MarkDynamic();
            coneObject.AddComponent<MeshFilter>().sharedMesh = coneMesh;
            var coneRenderer = coneObject.AddComponent<MeshRenderer>();
            coneRenderer.sharedMaterial = coneMaterial;
            coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coneRenderer.receiveShadows = false;

            vertices = new Vector3[ConeRays + 1];
            colors = new Color[ConeRays + 1];
            var triangles = new int[(ConeRays - 1) * 3];
            for (int i = 0; i < ConeRays - 1; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            coneMesh.vertices = vertices;
            coneMesh.triangles = triangles;

            lkpLine = CreateLine($"{name} LKP Line", 2, false, 0.05f);
            lkpMarker = CreateLine($"{name} LKP Marker", MarkerSegments, true, 0.06f);
        }

        void OnDestroy()
        {
            if (coneObject != null) Destroy(coneObject);
            if (lkpLine != null) Destroy(lkpLine.gameObject);
            if (lkpMarker != null) Destroy(lkpMarker.gameObject);
            if (coneMesh != null) Destroy(coneMesh);
        }

        LineRenderer CreateLine(string objectName, int points, bool loop, float width)
        {
            var line = new GameObject(objectName).AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.positionCount = points;
            line.loop = loop;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void LateUpdate()
        {
            RaiderStateId state = brain.State;
            bool alive = state != RaiderStateId.Dead;
            bool showCone = DebugOverlay.Visible && alive && state != RaiderStateId.Surrender;
            coneObject.SetActive(showCone);
            if (showCone) UpdateCone(state);

            bool searching = state == RaiderStateId.Suspicious || state == RaiderStateId.Search ||
                             state == RaiderStateId.Combat;
            bool showLkp = DebugOverlay.Visible && alive && searching && brain.HasLastKnownPosition;
            lkpLine.enabled = showLkp;
            lkpMarker.enabled = showLkp;
            if (showLkp) UpdateLastKnownPosition(state);
        }

        void UpdateCone(RaiderStateId state)
        {
            RaiderConfig config = brain.Config;
            Vector3 ground = transform.position + Vector3.up * 0.06f;
            Vector3 castOrigin = transform.position + Vector3.up * 1.0f;
            float half = config.viewAngle * 0.5f;

            Color baseColor = DebugOverlay.StateColor(state);
            float alpha = Mathf.Lerp(0.12f, 0.45f, brain.Detection.Value);
            Color edge = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            Color centre = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 1.4f);

            vertices[0] = ground;
            colors[0] = centre;
            for (int i = 0; i < ConeRays; i++)
            {
                float angle = Mathf.Lerp(-half, half, i / (float)(ConeRays - 1));
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                float length = config.viewDistance;
                if (Physics.Raycast(castOrigin, direction, out RaycastHit hit, length, config.obstacleMask,
                        QueryTriggerInteraction.Ignore))
                    length = hit.distance;
                vertices[i + 1] = ground + direction * length;
                colors[i + 1] = edge;
            }
            coneMesh.vertices = vertices;
            coneMesh.colors = colors;
            coneMesh.RecalculateBounds();
        }

        void UpdateLastKnownPosition(RaiderStateId state)
        {
            Color color = DebugOverlay.StateColor(state);
            Vector3 lkp = brain.LastKnownPosition + Vector3.up * 0.08f;

            lkpLine.startColor = new Color(color.r, color.g, color.b, 0.2f);
            lkpLine.endColor = color;
            lkpLine.SetPosition(0, transform.position + Vector3.up * 0.08f);
            lkpLine.SetPosition(1, lkp);

            lkpMarker.startColor = color;
            lkpMarker.endColor = color;
            for (int i = 0; i < MarkerSegments; i++)
            {
                float a = i / (float)MarkerSegments * Mathf.PI * 2f;
                lkpMarker.SetPosition(i, lkp + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.5f);
            }
        }

        void OnGUI()
        {
            if (!DebugOverlay.Visible) return;
            Camera view = Camera.main;
            if (view == null) return;

            Vector3 screen = view.WorldToScreenPoint(transform.position + Vector3.up * 2.4f);
            if (screen.z < 0f || screen.z > 45f) return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { font = DebugOverlay.FontBold, fontSize = 13, alignment = TextAnchor.MiddleCenter };
                smallStyle = new GUIStyle(GUI.skin.label) { font = DebugOverlay.Font, fontSize = 11, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            }

            const float width = 220f;
            float x = screen.x - width * 0.5f;
            float y = Screen.height - screen.y - 44f;
            RaiderStateId state = brain.State;
            Color stateColor = DebugOverlay.StateColor(state);

            GUI.color = new Color(DebugOverlay.Panel.r, DebugOverlay.Panel.g, DebugOverlay.Panel.b, 0.8f);
            GUI.DrawTexture(new Rect(x, y, width, 52f), Texture2D.whiteTexture);

            labelStyle.normal.textColor = stateColor;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + 1f, width, 18f), $"{name}: {state.ToString().ToUpperInvariant()}", labelStyle);

            if (state != RaiderStateId.Dead)
            {
                var bar = new Rect(x + 10f, y + 21f, width - 20f, 6f);
                GUI.color = DebugOverlay.Background;
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = brain.Detection.IsFull ? DebugOverlay.Red : DebugOverlay.Amber;
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * brain.Detection.Value, bar.height), Texture2D.whiteTexture);
                GUI.color = DebugOverlay.White;
                float mark = bar.x + bar.width * brain.Config.suspicionThreshold;
                GUI.DrawTexture(new Rect(mark, bar.y - 2f, 1f, bar.height + 4f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            smallStyle.normal.textColor = DebugOverlay.Muted;
            GUI.Label(new Rect(x + 4f, y + 28f, width - 8f, 24f), brain.LastTransition, smallStyle);
        }
    }
}
