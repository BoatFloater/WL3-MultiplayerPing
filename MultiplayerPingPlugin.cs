using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Networking;

namespace MultiplayerPing;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MultiplayerPingPlugin : BasePlugin
{
    internal const short PingMessageType = 32255;

    internal static new ManualLogSource Log;

    private static ConfigEntry<string> cfgPingColor;
    private static ConfigEntry<string> cfgPingKeyCodeStr;

    private static KeyCode resultKeyCode;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        ClassInjector.RegisterTypeInIl2Cpp<PingManager>();
        ClassInjector.RegisterTypeInIl2Cpp<PingMessage>();

        cfgPingColor = Config.Bind("General", "PingColor", "#00ff00", "Color of the ping indicator.");
        cfgPingKeyCodeStr = Config.Bind("General", "PingKeyCodeStr", nameof(KeyCode.Mouse2), "Key code modifier for pinging.");

        resultKeyCode = Enum.TryParse(cfgPingKeyCodeStr.Value, out resultKeyCode) ? resultKeyCode : KeyCode.Mouse2;

        Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }

    private static Color ParseColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        else
        {
            Log.LogWarning($"Failed to parse color from hex string: {hex}. Using default color.");
            return Color.green;
        }
    }

    [HarmonyPatch(typeof(InputManager))]
    internal static class InputManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(InputManager.AcceptInputAtPoint))]
        internal static bool Prefix_AcceptInputAtPoint(InputManager __instance, Vector3 point,
            Transform hitTransform,
            bool acceptMovement = true)
        {
            if (!UnityEngine.Input.GetKey(resultKeyCode))
            {
                return true;
            }

            PingMessage msg = new PingMessage();
            msg.Position = point;
            msg.Color = ParseColor(cfgPingColor.Value);
            if (NetworkClient.allClients.Count > 0 && NetworkClient.allClients[0] != null)
            {
                Log.LogMessage($"Sending ping at: {msg.Position}");
                NetworkClient.allClients[0].Send(PingMessageType, msg);
            }
            else
            {
                Log.LogWarning($"Failed to send ping: No active network client.");
            }

            return false;
        }

        [HarmonyPatch(typeof(WastelandNetworkManager))]
        internal static class WastelandNetworkManagerPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(WastelandNetworkManager.OnStartClient))]
            internal static void Postfix_OnStartClient(WastelandNetworkManager __instance, NetworkClient client)
            {
                Log.LogMessage("Postfix_OnStartClient");
                client.RegisterHandler(PingMessageType, (NetworkMessageDelegate)OnClientPingReceived);
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(WastelandNetworkManager.OnStartHost))]
            internal static void Postfix_OnStartHost(WastelandNetworkManager __instance)
            {
                Log.LogMessage("Postfix_OnStartHost");
                NetworkServer.RegisterHandler(PingMessageType, (NetworkMessageDelegate)OnServerPingReceived);
            }

            internal static void OnServerPingReceived(NetworkMessage netMsg)
            {
                var msg = netMsg.ReadMessage<PingMessage>();
                Log.LogMessage($"[Server] Ping received at: {msg.Position}");
                NetworkServer.SendToAll(PingMessageType, msg);
            }

            internal static void OnClientPingReceived(NetworkMessage netMsg)
            {
                var msg = netMsg.ReadMessage<PingMessage>();
                Log.LogMessage($"[Client] Ping received at: {msg.Position}");
                PingManager test = PingManager.Instance;
                if (test == null)
                {
                    Debug.LogWarning("PingManager instance is null.");
                    return;
                }

                test.PingAt(msg.Position, msg.Color);
            }
        }
    }
}
