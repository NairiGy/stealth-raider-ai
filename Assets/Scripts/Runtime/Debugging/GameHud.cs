using StealthRaider.AI;
using StealthRaider.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace StealthRaider.Debugging
{
    /// <summary>Minimal test HUD: crosshair, health, controls, state legend; F1 debug, R restart.</summary>
    public class GameHud : MonoBehaviour
    {
        static readonly RaiderStateId[] LegendStates =
        {
            RaiderStateId.Patrol, RaiderStateId.Suspicious, RaiderStateId.Search,
            RaiderStateId.Combat, RaiderStateId.Return, RaiderStateId.Surrender,
        };

        [SerializeField] Health hero;
        [SerializeField] Font font;
        [SerializeField] Font fontBold;

        GUIStyle textStyle;
        GUIStyle bigStyle;

        void Awake()
        {
            DebugOverlay.Font = font;
            DebugOverlay.FontBold = fontBold != null ? fontBold : font;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) DebugOverlay.Visible = !DebugOverlay.Visible;
            if (keyboard.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnGUI()
        {
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label) { font = DebugOverlay.Font, fontSize = 13 };
                textStyle.normal.textColor = DebugOverlay.White;
                bigStyle = new GUIStyle(GUI.skin.label) { font = DebugOverlay.FontBold, fontSize = 28, alignment = TextAnchor.MiddleCenter };
                bigStyle.normal.textColor = DebugOverlay.Red;
            }

            DrawCrosshair();
            DrawHealth();
            DrawHelp();
            if (DebugOverlay.Visible) DrawLegend();

            if (hero != null && hero.IsDead)
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 40f), "YOU DIED — press R to restart", bigStyle);
        }

        static void DrawCrosshair()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.color = DebugOverlay.Amber;
            GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 6f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 2f, cy - 1f, 6f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 2f, 6f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy + 2f, 2f, 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void DrawHealth()
        {
            if (hero == null) return;
            var back = new Rect(20f, Screen.height - 40f, 220f, 14f);
            GUI.color = DebugOverlay.Panel;
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = hero.Fraction > 0.3f ? DebugOverlay.White : DebugOverlay.Red;
            GUI.DrawTexture(new Rect(back.x + 2f, back.y + 2f, (back.width - 4f) * hero.Fraction, back.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, Screen.height - 62f, 220f, 20f), $"SUIT INTEGRITY  {Mathf.CeilToInt(hero.Current)}", textStyle);
        }

        void DrawHelp()
        {
            const string help = "WASD move · Shift run · Mouse aim · LMB shoot\nF1 debug view · R restart · Esc free cursor";
            GUI.color = new Color(DebugOverlay.Panel.r, DebugOverlay.Panel.g, DebugOverlay.Panel.b, 0.75f);
            GUI.DrawTexture(new Rect(12f, 12f, 330f, 42f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, 14f, 330f, 40f), help, textStyle);
        }

        void DrawLegend()
        {
            float x = Screen.width - 150f;
            float y = 12f;
            GUI.color = new Color(DebugOverlay.Panel.r, DebugOverlay.Panel.g, DebugOverlay.Panel.b, 0.75f);
            GUI.DrawTexture(new Rect(x - 8f, y, 146f, LegendStates.Length * 18f + 8f), Texture2D.whiteTexture);
            for (int i = 0; i < LegendStates.Length; i++)
            {
                float rowY = y + 6f + i * 18f;
                GUI.color = DebugOverlay.StateColor(LegendStates[i]);
                GUI.DrawTexture(new Rect(x, rowY + 4f, 10f, 10f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 16f, rowY, 120f, 18f), LegendStates[i].ToString(), textStyle);
            }
        }
    }
}
