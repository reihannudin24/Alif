using UnityEditor;
using UnityEngine;
namespace Alif.EditorTools
{
    public static class AlifTooling
    {
        [MenuItem("Alif/Tools/Connect Existing MCP")]
        public static async void Connect()
        {
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            bool connected = await MCPForUnity.Editor.Services.MCPServiceLocator.Bridge.StartAsync();
            Debug.Log("[Alif] MCP connected: " + connected);
        }
    }
}
