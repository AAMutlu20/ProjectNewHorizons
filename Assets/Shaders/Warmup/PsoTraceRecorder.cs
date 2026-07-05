using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking.PlayerConnection;

namespace Core
{
    /// <summary>
    /// TEMPORARY diagnostic tool -- not meant to ship. Records which PSOs
    /// (shader + GPU render-state combinations) your game actually uses
    /// during a real play session, then sends that recording back to the
    /// Editor as a .graphicsstate asset via PlayerConnection -- over the
    /// same USB connection you already use for Development Build debugging.
    ///
    /// Setup:
    /// 1. Add this to a GameObject in the game scene (e.g. next to GameManager).
    /// 2. In Build Profiles, enable BOTH "Development Build" and
    ///    "Autoconnect Profiler" for Android.
    /// 3. Build and install on your Xiaomi device via USB, with the Unity
    ///    Editor open on this project the whole time (SendToEditor writes
    ///    directly into the Editor's Assets folder over the connection).
    /// 4. Play through a session that touches every enemy type (Zombie,
    ///    Spider, SpiderMinion, EyeWinged, TornadoGhost) and any VFX/hit
    ///    effects at least once -- the trace only knows about what actually
    ///    rendered during it.
    /// 5. Exit the app normally (Android back button until it closes). That
    ///    triggers OnApplicationQuit, which ends the trace and sends it.
    /// 6. Check the Unity Console for the "sent to Editor" log, then look in
    ///    your Project window for the new .graphicsstate asset.
    ///
    /// Remove this component once you have a good trace -- it has no place
    /// in a shipping build.
    /// </summary>
    public class PsoTraceRecorder : MonoBehaviour
    {
        [SerializeField] private string fileName = "AndroidVulkan.graphicsstate";

        private GraphicsStateCollection _collection;
        private bool _ended;

        private void Start()
        {
            _collection = new GraphicsStateCollection();
            _collection.BeginTrace();
            Debug.Log("[PsoTraceRecorder] Trace started.");
        }

        private void OnApplicationQuit()
        {
            EndAndSend();
        }

        private void OnDestroy()
        {
            EndAndSend();
        }

        private void EndAndSend()
        {
            if (_ended) return;
            _ended = true;

            _collection.EndTrace();

            if (PlayerConnection.instance.isConnected)
            {
                _collection.SendToEditor(fileName);
                Debug.Log($"[PsoTraceRecorder] Trace ended, sent '{fileName}' to Editor.");
            }
            else
            {
                Debug.LogWarning("[PsoTraceRecorder] No PlayerConnection -- trace was NOT sent. " +
                                  "Make sure this is a Development Build with Autoconnect Profiler " +
                                  "enabled, and that the Editor was open and connected the whole session.");
            }
        }
    }
}
