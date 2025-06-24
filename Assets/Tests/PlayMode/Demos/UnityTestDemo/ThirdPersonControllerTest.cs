using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ThirdPersonControllerTest
{
    private GameObject player;
    private ThirdPersonController controller;
    private CharacterController charController;
    private GameObject cameraObj;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("Player");
        charController = player.AddComponent<CharacterController>();
        controller = player.AddComponent<ThirdPersonController>();

        cameraObj = new GameObject("Camera");
        cameraObj.transform.position = Vector3.zero;
        controller.cameraTransform = cameraObj.transform;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player);
        Object.DestroyImmediate(cameraObj);
    }

    [UnityTest]
    public IEnumerator CameraControl_ClampsCameraPitch()
    {
        SetPrivateField(controller, "cameraPitch", 100f);

        // Simulate mouse Y input
        InputSimulator.SetAxis("Mouse Y", -100f);
        controller.cameraSensitivity = 1f;
        InvokePrivateMethod(controller, "CameraControl");

        float cameraPitch = GetPrivateField<float>(controller, "cameraPitch");
        Assert.LessOrEqual(cameraPitch, 60f);
        yield return null;
    }

    // --- Helpers for reflection and input simulation ---

    private T GetPrivateField<T>(object obj, string field)
    {
        var f = obj.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)f.GetValue(obj);
    }

    private void SetPrivateField<T>(object obj, string field, T value)
    {
        var f = obj.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f.SetValue(obj, value);
    }

    private void InvokePrivateMethod(object obj, string method)
    {
        var m = obj.GetType().GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        m.Invoke(obj, null);
    }

    // Simple input simulation for tests
    private static class InputSimulator
    {
        private static readonly System.Collections.Generic.Dictionary<string, float> axes = new();
        private static readonly System.Collections.Generic.Dictionary<string, bool> buttons = new();

        public static void SetAxis(string axis, float value) => axes[axis] = value;
        public static void SetButtonDown(string button, bool down) => buttons[button] = down;
    }
}