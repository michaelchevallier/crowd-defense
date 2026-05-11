#nullable enable
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using CrowdDefense.Visual;

namespace CrowdDefense.Tests.Runtime
{
    public class JuiceFXTests
    {
        private GameObject? _host;
        private JuiceFX? _juice;
        private Camera? _cam;
        private GameObject? _camHost;
        private GameObject? _uiHost;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 1. Camera : JuiceFX caches Camera.main in OnAwakeSingleton.
            _camHost = new GameObject("Test_MainCamera");
            _camHost.tag = "MainCamera";
            _cam = _camHost.AddComponent<Camera>();
            _cam.transform.position = new Vector3(1f, 2f, 3f);

            // 2. UIDocument : Flash requires a UIDocument in scene to mount overlay.
            _uiHost = new GameObject("Test_UIDocument");
            var panelAsset = ScriptableObject.CreateInstance<PanelSettings>();
            var uiDoc = _uiHost.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelAsset;
            // Provide a root VisualElement (UIDocument auto-creates one with PanelSettings).
            yield return null;

            // 3. JuiceFX host.
            _host = new GameObject("Test_JuiceFX");
            _juice = _host.AddComponent<JuiceFX>();

            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            if (_camHost != null) Object.DestroyImmediate(_camHost);
            if (_uiHost != null) Object.DestroyImmediate(_uiHost);
            Time.timeScale = 1f;
        }

        [Test]
        public void Instance_IsSet_AfterAwake()
        {
            Assert.IsNotNull(JuiceFX.Instance, "MonoSingleton must wire Instance during Awake.");
        }

        [UnityTest]
        public IEnumerator Shake_ChangesCameraPosition_ThenRestoresBase()
        {
            Assert.IsNotNull(_juice);
            Assert.IsNotNull(_cam);

            var basePos = _cam!.transform.position;
            _juice!.Shake(0.5f, 200);

            // Wait one frame so LateUpdate runs.
            yield return null;
            yield return null;

            // During shake, position should diverge from base (random offset added).
            // We can't assert exact value (randomized), but we can assert "not equal" with margin.
            var afterStartShake = _cam.transform.position;
            // Note : random might land on 0 exactly, so we use a loose tolerance.
            bool diverged = Vector3.Distance(afterStartShake, basePos) > 0.0001f
                            || Mathf.Abs(_juice.GetType()
                                .GetField("_shakeIntensity", BindingFlags.Instance | BindingFlags.NonPublic)!
                                .GetValue(_juice) is float f ? f : 0f) > 0f;
            Assert.IsTrue(diverged, "Shake should produce camera offset within first frames.");

            // Wait until duration expires + a margin.
            yield return new WaitForSecondsRealtime(0.3f);

            // After shake expires, camera must snap back to base.
            Assert.AreEqual(basePos, _cam.transform.position,
                "Camera must restore to base position after shake expires.");
        }

        [UnityTest]
        public IEnumerator Flash_CreatesOverlayVisualElement()
        {
            Assert.IsNotNull(_juice);

            _juice!.Flash(Color.red, 200);

            yield return null;
            yield return null;

            // Reflect the private _flashOverlay field to verify it was created.
            var field = typeof(JuiceFX).GetField("_flashOverlay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "JuiceFX._flashOverlay private field must exist.");

            var overlay = field!.GetValue(_juice) as VisualElement;
            Assert.IsNotNull(overlay, "Flash() must create the overlay VisualElement.");
            Assert.AreEqual("juice-flash-overlay", overlay!.name);
        }

        [UnityTest]
        public IEnumerator SlowMo_SetsAndRestoresTimeScale()
        {
            Assert.IsNotNull(_juice);
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "Time.timeScale baseline must be 1f.");

            _juice!.SlowMo(0.3f, 200);

            // Wait a frame for coroutine to set timeScale.
            yield return null;
            Assert.AreEqual(0.3f, Time.timeScale, 0.001f, "SlowMo must apply timeScale.");

            // Wait for duration + margin (unscaled time).
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.AreEqual(1f, Time.timeScale, 0.001f,
                "SlowMo must restore Time.timeScale = 1f after duration.");
        }

        [Test]
        public void SetBaseCamPos_UpdatesInternalBase()
        {
            Assert.IsNotNull(_juice);
            var newBase = new Vector3(10f, 20f, 30f);
            _juice!.SetBaseCamPos(newBase);

            var field = typeof(JuiceFX).GetField("_baseCamPos",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var current = (Vector3)field!.GetValue(_juice);
            Assert.AreEqual(newBase, current, "SetBaseCamPos must update internal _baseCamPos.");
        }
    }
}
