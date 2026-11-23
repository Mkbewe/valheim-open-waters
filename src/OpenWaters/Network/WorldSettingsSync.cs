using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace OpenWaters
{
    internal static class WorldSettingsSync
    {
        private const string Marker = "OWS1:";
        private const string ClientMarker = "OWC1";
        private const string ErrorRpc = "OpenWatersError";
        private static readonly byte[] ClientMarkerBytes = Encoding.UTF8.GetBytes(ClientMarker);
        private static readonly ConditionalWeakTable<ZRpc, object> _moddedClients =
            new ConditionalWeakTable<ZRpc, object>();
        private static readonly object _moddedMarker = new object();

        internal static bool ClientMarkerHookActive { get; private set; }
        internal static bool SettingsWriteHookActive { get; private set; }
        internal static bool SettingsReadHookActive { get; private set; }

        private static int FindSequence(List<CodeInstruction> code, params Func<CodeInstruction, bool>[] pattern)
        {
            for (int i = 0; i <= code.Count - pattern.Length; i++)
            {
                bool matched = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (!pattern[j](code[i + j]))
                    {
                        matched = false;
                        break;
                    }
                }
                if (matched)
                {
                    return i;
                }
            }
            return -1;
        }

        private static LocalVariableInfo[] GetLocals(MethodBase method)
        {
            MethodBody body = method != null ? method.GetMethodBody() : null;
            if (body == null)
            {
                return Array.Empty<LocalVariableInfo>();
            }
            IList<LocalVariableInfo> source = body.LocalVariables;
            LocalVariableInfo[] locals = new LocalVariableInfo[source.Count];
            source.CopyTo(locals, 0);
            return locals;
        }

        private static int? LocalIndex(CodeInstruction instruction)
        {
            OpCode opcode = instruction.opcode;
            if (opcode == OpCodes.Ldloc_0)
            {
                return 0;
            }
            if (opcode == OpCodes.Ldloc_1)
            {
                return 1;
            }
            if (opcode == OpCodes.Ldloc_2)
            {
                return 2;
            }
            if (opcode == OpCodes.Ldloc_3)
            {
                return 3;
            }
            if (opcode == OpCodes.Ldloc || opcode == OpCodes.Ldloc_S)
            {
                if (instruction.operand is LocalBuilder local)
                {
                    return local.LocalIndex;
                }
                if (instruction.operand is int index)
                {
                    return index;
                }
            }
            return null;
        }

        private static bool IsPackageLoad(CodeInstruction instruction, LocalVariableInfo[] locals)
        {
            int? index = LocalIndex(instruction);
            return index.HasValue && index.Value >= 0 && index.Value < locals.Length &&
                   locals[index.Value].LocalType == typeof(ZPackage);
        }

        [HarmonyPatch(typeof(ZNet), "SendPeerInfo")]
        internal static class SendPeerInfoPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                MethodInfo writeInt = AccessTools.Method(
                    typeof(ZPackage), nameof(ZPackage.Write), new[] { typeof(int) });
                MethodInfo writeDouble = AccessTools.Method(
                    typeof(ZPackage), nameof(ZPackage.Write), new[] { typeof(double) });
                MethodInfo appendSettings = AccessTools.Method(
                    typeof(WorldSettingsSync), nameof(AppendPeerInfo));
                MethodInfo appendClientMarker = AccessTools.Method(
                    typeof(WorldSettingsSync), nameof(AppendClientMarker));
                FieldInfo netTime = AccessTools.Field(typeof(ZNet), "m_netTime");
                List<CodeInstruction> code = new List<CodeInstruction>(instructions);
                LocalVariableInfo[] locals = GetLocals(method);

                int netTimeStart = FindSequence(code,
                    i => i.opcode == OpCodes.Callvirt && Equals(i.operand, writeInt),
                    i => IsPackageLoad(i, locals),
                    i => i.opcode == OpCodes.Ldarg_0,
                    i => i.opcode == OpCodes.Ldfld && Equals(i.operand, netTime),
                    i => i.opcode == OpCodes.Callvirt && Equals(i.operand, writeDouble));

                int markerStart = FindSequence(code,
                    i => i.opcode == OpCodes.Ldarg_1,
                    i => i.opcode == OpCodes.Ldstr &&
                         "PeerInfo".Equals(i.operand as string, StringComparison.Ordinal));

                if (netTimeStart < 0)
                {
                    OpenWatersPlugin.Log.LogError(
                        "[Network] SendPeerInfo settings pattern not found; no hooks installed.");
                }
                if (markerStart < 0)
                {
                    OpenWatersPlugin.Log.LogError(
                        "[Network] SendPeerInfo client marker pattern not found; no hooks installed.");
                }
                if (netTimeStart < 0 || markerStart < 0)
                {
                    SettingsWriteHookActive = false;
                    ClientMarkerHookActive = false;
                    return code;
                }

                CodeInstruction packageLoad = code[netTimeStart + 1].Clone();
                code.InsertRange(netTimeStart + 1, new[]
                {
                    packageLoad.Clone(),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, appendSettings)
                });

                int markerInsert = markerStart > netTimeStart ? markerStart + 3 : markerStart;
                code.InsertRange(markerInsert, new[]
                {
                    packageLoad.Clone(),
                    new CodeInstruction(OpCodes.Call, appendClientMarker)
                });

                SettingsWriteHookActive = true;
                ClientMarkerHookActive = true;
                OpenWatersPlugin.Log.LogInfo("[Network] SendPeerInfo hooks installed.");
                return code;
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        internal static class PeerInfoMarkerPatch
        {
            private static void Prefix(ZRpc rpc, ZPackage pkg)
            {
                try
                {
                    ZNet net = ZNet.instance;
                    if (net == null || rpc == null || pkg == null)
                    {
                        return;
                    }
                    if (!OpenWatersSettings.Enabled)
                    {
                        return;
                    }
                    if (!net.IsServer())
                    {
                        WorldSettingsStore.MarkPeerInfoReceived();
                        return;
                    }
                    if (!HasClientMarker(pkg))
                    {
                        return;
                    }
                    _moddedClients.GetValue(rpc, _ => _moddedMarker);
                    OpenWatersPlugin.Log.LogInfo("[Network] Client advertises Open Waters support.");
                }
                catch (Exception exception)
                {
                    OpenWatersPlugin.Log.LogWarning(
                        $"[Network] Could not read the client marker: {exception.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        internal static class PeerInfoPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo initialize = AccessTools.Method(
                    typeof(WorldGenerator), nameof(WorldGenerator.Initialize));
                MethodInfo read = AccessTools.Method(
                    typeof(WorldSettingsSync), nameof(ReadPeerInfo));
                List<CodeInstruction> code = new List<CodeInstruction>(instructions);
                int insertAt = FindSequence(code,
                    i => i.opcode == OpCodes.Call && Equals(i.operand, initialize));

                if (insertAt < 0)
                {
                    SettingsReadHookActive = false;
                    OpenWatersPlugin.Log.LogError(
                        "[Network] RPC_PeerInfo settings pattern not found; server settings will be ignored.");
                    return code;
                }

                code.InsertRange(insertAt, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Call, read)
                });
                SettingsReadHookActive = true;
                OpenWatersPlugin.Log.LogInfo("[Network] RPC_PeerInfo settings hook installed.");
                return code;
            }
        }

        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        internal static class RegisterErrorRpcPatch
        {
            private static void Postfix(ZNetPeer peer)
            {
                if (peer == null || peer.m_rpc == null)
                {
                    return;
                }
                try
                {
                    peer.m_rpc.Register(ErrorRpc, new Action<ZRpc, string>(OnRemoteError));
                }
                catch (Exception exception)
                {
                    OpenWatersPlugin.Log.LogWarning(
                        $"[Network] Could not register the error RPC: {exception.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ZNet), "OnDestroy")]
        internal static class OnDestroyPatch
        {
            private static void Postfix()
            {
                WorldSettingsStore.ClearRemoteSnapshot();
            }
        }

        private static void OnRemoteError(ZRpc rpc, string message)
        {
            ZNet net = ZNet.instance;
            if (net != null && net.IsServer())
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Network] A client rejected the world settings: {message}");
                return;
            }
            ShowError(message);
        }

        private static void RejectWithMessage(ZRpc rpc, string message)
        {
            if (rpc != null)
            {
                try
                {
                    rpc.Invoke(ErrorRpc, message);
                }
                catch (Exception exception)
                {
                    OpenWatersPlugin.Log.LogWarning(
                        $"[Network] Could not report the rejection to the server: {exception.Message}");
                }
            }
            ShowError(message);
        }

        private static void ShowError(string message)
        {
            try
            {
                UnifiedPopup.Push(new WarningPopup(
                    "Open Waters: incompatible world settings",
                    message,
                    UnifiedPopup.Pop,
                    localizeText: false));
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Network] Could not show the world settings error: {exception.Message}");
            }
            try
            {
                ZNet net = ZNet.instance;
                if (net != null)
                {
                    MethodInfo sendDisconnect = AccessTools.Method(
                        typeof(ZNet), "SendDisconnect", Type.EmptyTypes);
                    if (sendDisconnect != null)
                    {
                        sendDisconnect.Invoke(net, null);
                    }
                    else
                    {
                        OpenWatersPlugin.Log.LogError(
                            "[Network] SendDisconnect() not found; could not leave an incompatible world.");
                    }
                }
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Network] Could not disconnect after a world settings error: {exception.Message}");
            }
        }

        public static void AppendClientMarker(ZPackage pkg)
        {
            ZNet net = ZNet.instance;
            if (net == null || net.IsServer() || pkg == null)
            {
                return;
            }
            if (!OpenWatersSettings.Enabled)
            {
                return;
            }
            if (!SettingsReadHookActive)
            {
                OpenWatersPlugin.Log.LogError(
                    "[Network] Settings read hook is not active; not advertising Open Waters.");
                return;
            }
            pkg.Write(ClientMarker);
            OpenWatersPlugin.Log.LogInfo("[Network] Sent client marker.");
        }

        public static void AppendPeerInfo(ZPackage pkg, ZRpc rpc)
        {
            ZNet net = ZNet.instance;
            if (net == null || !net.IsServer() || pkg == null)
            {
                return;
            }
            if (!OpenWatersSettings.Enabled)
            {
                OpenWatersPlugin.Log.LogInfo("[Network] Open Waters is disabled; no settings payload is sent.");
                return;
            }
            if (!SettingsWriteHookActive)
            {
                OpenWatersPlugin.Log.LogError(
                    "[Network] SendPeerInfo hook is not active; no settings payload is sent.");
                return;
            }

            GenerationSession session = GenerationSession.Current;
            World world = session != null ? session.World : null;
            if (world == null || world.m_menu || world.m_worldName == OpenWatersSettings.PreviewWorldName)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Network] AppendPeerInfo skipped: world='{(world == null ? "none" : world.m_worldName)}', " +
                    $"menu={world != null && world.m_menu}.");
                return;
            }

            WorldSettings settings = session.Settings;
            if (!settings.AffectsGeneration)
            {
                OpenWatersPlugin.Log.LogInfo("[Network] Vanilla world; no settings payload is sent.");
                return;
            }

            if (!ClientMarkerHookActive)
            {
                OpenWatersPlugin.Log.LogError(
                    "[Network] Client marker hook is not active; skipping settings and client enforcement.");
                return;
            }

            if (!IsClientModded(rpc))
            {
                OpenWatersPlugin.Log.LogWarning(
                    "[Network] Rejecting a client without Open Waters; the server requires the mod.");
                RejectClient(rpc);
                return;
            }

            if (string.IsNullOrEmpty(world.m_seedName))
            {
                OpenWatersPlugin.Log.LogError(
                    "[Network] Server world has no seed; skipping the settings payload.");
                return;
            }

            string payload = WorldSettingsCodec.Serialize(settings, world.m_seedName);
            pkg.Write(Marker + payload);
            OpenWatersPlugin.Log.LogInfo(
                $"[Network] Sending world settings: world='{world.m_worldName}', seed='{world.m_seedName}', " +
                $"preset={settings.IslandAmount}, radius={settings.WorldRadius}, " +
                $"biomes={settings.StartBiomeLimit}, mountains={settings.ExcludeMountains}, " +
                $"generatorVersion={settings.GeneratorVersionValue}, modVersion={settings.ModVersion}, " +
                $"bytes={payload.Length}.");
        }

        public static void ReadPeerInfo(ZRpc rpc, ZPackage pkg)
        {
            ZNet net = ZNet.instance;
            if (net == null || net.IsServer() || pkg == null)
            {
                OpenWatersPlugin.Log.LogInfo("[Network] ReadPeerInfo skipped: not a client.");
                return;
            }
            if (!OpenWatersSettings.Enabled)
            {
                WorldSettingsStore.ClearRemoteSnapshot();
                return;
            }

            WorldSettingsStore.MarkPeerInfoReceived();
            try
            {
                int position = pkg.GetPos();
                int size = pkg.Size();
                PeerInfoPayloadKind kind = PeerInfoPayload.Read(
                    pkg.GetArray(), position, size, Marker, out string text, out int consumed);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Network] ReadPeerInfo: position={position}, remaining={size - position}, kind={kind}.");
                if (consumed > 0)
                {
                    pkg.SetPos(position + consumed);
                }

                switch (kind)
                {
                    case PeerInfoPayloadKind.NoExtension:
                        OpenWatersPlugin.Log.LogInfo(
                            "[Network] Server does not use Open Waters; playing a vanilla world.");
                        return;
                    case PeerInfoPayloadKind.EmptyExtension:
                        OpenWatersPlugin.Log.LogWarning("[Network] PeerInfo extension is empty.");
                        return;
                    case PeerInfoPayloadKind.OtherMod:
                        OpenWatersPlugin.Log.LogInfo(
                            "[Network] PeerInfo extension belongs to another mod; using vanilla generation.");
                        return;
                    case PeerInfoPayloadKind.Malformed:
                        OpenWatersPlugin.Log.LogWarning("[Network] PeerInfo extension is malformed.");
                        return;
                    case PeerInfoPayloadKind.UnsupportedVersion:
                        OpenWatersPlugin.Log.LogError(
                            "[Network] Server sent an unsupported Open Waters payload version.");
                        WorldSettingsStore.MarkRemotePayloadInvalid();
                        RejectWithMessage(
                            rpc,
                            "The server uses an Open Waters payload version this client does not support.");
                        return;
                }

                string payload = text.Substring(Marker.Length);
                FieldInfo worldField = AccessTools.Field(typeof(ZNet), "m_world");
                World world = worldField != null ? worldField.GetValue(null) as World : null;
                string seed = world != null ? world.m_seedName : string.Empty;
                if (!WorldSettingsCodec.TryParse(payload, seed, out WorldSettings settings))
                {
                    OpenWatersPlugin.Log.LogError("[Network] Server sent invalid Open Waters settings.");
                    WorldSettingsStore.MarkRemotePayloadInvalid();
                    RejectWithMessage(rpc, "The server sent invalid Open Waters world settings.");
                    return;
                }

                if (!settings.IsGeneratorCompatible)
                {
                    OpenWatersPlugin.Log.LogError(
                        $"[Network] Server generator version {settings.GeneratorVersionValue} is not supported " +
                        $"({WorldSettings.SupportedVersionsText}).");
                    WorldSettingsStore.MarkRemotePayloadInvalid();
                    RejectWithMessage(
                        rpc,
                        $"The server uses generator version {settings.GeneratorVersionValue}; " +
                        $"this client supports {WorldSettings.SupportedVersionsText}.");
                    return;
                }

                WorldSettingsStore.SetRemoteSnapshot(settings, seed);
                OpenWatersPlugin.Log.LogInfo(
                    $"[Network] Received world settings from server for seed '{seed}': " +
                    $"preset={settings.IslandAmount}, generatorVersion={settings.GeneratorVersionValue}.");
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Network] Could not read world settings: {exception.Message}");
            }
        }

        private static bool HasClientMarker(ZPackage pkg)
        {
            byte[] bytes = pkg.GetArray();
            return PeerInfoProtocol.EndsWith(bytes, pkg.Size(), ClientMarkerBytes);
        }

        private static bool IsClientModded(ZRpc rpc)
        {
            return rpc != null && _moddedClients.TryGetValue(rpc, out _);
        }

        private static void RejectClient(ZRpc rpc)
        {
            if (rpc == null)
            {
                return;
            }
            try
            {
                rpc.Invoke("Error", 3);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Network] Could not reject the client: {exception.Message}");
            }

            try
            {
                ZNet net = ZNet.instance;
                MethodInfo getPeer = AccessTools.Method(
                    typeof(ZNet), "GetPeer", new[] { typeof(ZRpc) });
                ZNetPeer peer = net != null && getPeer != null
                    ? getPeer.Invoke(net, new object[] { rpc }) as ZNetPeer
                    : null;
                if (peer != null)
                {
                    net.Disconnect(peer);
                    OpenWatersPlugin.Log.LogWarning("[Network] Kicked a client without Open Waters.");
                }
                else
                {
                    OpenWatersPlugin.Log.LogWarning(
                        "[Network] Could not find the peer to kick; the client may stay connected.");
                }
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning(
                    $"[Network] Could not disconnect the client: {exception.Message}");
            }
        }
    }
}
