using System;
using System.Globalization;
using UnityEngine;

namespace GenJutsu.GenCore
{
    public struct HouseSnapshot
    {
        public string SessionId;
        public int Version;
        public string Mode;
        public string Dojo;
        public bool HasLights;
        public bool HasMain;
        public bool HasLanterns;
        public Color MainColor;
        public float MainIntensity;
        public int MainTransitionMs;
        public Color LanternColor;
        public float LanternIntensity;
        public int LanternTransitionMs;
        public bool HasAmbience;
        public string Preset;
        public float Volume;
        public int AmbienceTransitionMs;
        public HouseDeviceState[] Devices;
        public bool HasMedia;
        public HouseMediaState Media;
    }

    public struct HouseMediaState
    {
        public string Type;
        public string Url;
        public string Title;
        public string Artist;
        public string License;
        public bool Playing;
        public string Device;
    }

    public struct HouseDeviceState
    {
        public string Id;
        public bool On;
        public Color Color;
        public float Intensity;
        public int Temp;
        public string Mode;
        public string Fan;
    }

    public readonly struct HouseApplyPlan
    {
        public readonly bool HasLights;
        public readonly bool HasMain;
        public readonly bool HasLanterns;
        public readonly Color MainColor;
        public readonly float MainIntensity;
        public readonly int MainTransitionMs;
        public readonly Color LanternColor;
        public readonly float LanternIntensity;
        public readonly int LanternTransitionMs;
        public readonly bool HasAmbience;
        public readonly string Preset;
        public readonly float Volume;
        public readonly int AmbienceTransitionMs;
        public readonly bool SwitchDojo;
        public readonly string Dojo;
        public readonly HouseDeviceState[] Devices;
        public readonly bool HasMedia;
        public readonly HouseMediaState Media;

        public HouseApplyPlan(HouseSnapshot snap)
        {
            HasLights = snap.HasLights;
            HasMain = snap.HasMain;
            HasLanterns = snap.HasLanterns;
            MainColor = snap.MainColor;
            MainIntensity = snap.MainIntensity;
            MainTransitionMs = snap.MainTransitionMs;
            LanternColor = snap.LanternColor;
            LanternIntensity = snap.LanternIntensity;
            LanternTransitionMs = snap.LanternTransitionMs;
            HasAmbience = snap.HasAmbience;
            Preset = snap.Preset ?? "";
            Volume = snap.Volume;
            AmbienceTransitionMs = snap.AmbienceTransitionMs;
            SwitchDojo = !string.IsNullOrEmpty(snap.Dojo);
            Dojo = snap.Dojo ?? "";
            Devices = snap.Devices ?? System.Array.Empty<HouseDeviceState>();
            HasMedia = snap.HasMedia;
            Media = snap.Media;
        }

        /// <summary>House commands change the room. They never rewrite the visit profile.</summary>
        public bool ChangesSessionProfile => false;
    }

    public static class HouseStateLogic
    {
        public static readonly string[] DeviceIds =
        {
            "sala_luz", "quarto_luz", "abajur", "fita_led", "tv", "ambiente", "ar_condicionado"
        };

        public static bool IsNewer(int appliedVersion, int incomingVersion)
        {
            return incomingVersion > appliedVersion;
        }

        public static bool TryParse(string json, out HouseSnapshot snap, out bool stateNull)
        {
            snap = default;
            stateNull = false;
            if (!GenJson.TryParse(json, out var root) || !(root is JObj obj))
                return false;
            if (obj.TryGet("state", out var stateVal))
            {
                if (stateVal is JNull)
                {
                    stateNull = true;
                    return true;
                }

                var state = stateVal as JObj;
                if (state == null)
                    return false;
                snap = Read(state);
                return snap.Version != int.MinValue;
            }

            if (!obj.TryGet("version", out _))
                return false;
            snap = Read(obj);
            return snap.Version != int.MinValue;
        }

        public static HouseApplyPlan Map(HouseSnapshot snap)
        {
            return new HouseApplyPlan(snap);
        }

        static HouseSnapshot Read(JObj state)
        {
            var snap = new HouseSnapshot
            {
                SessionId = state.Str("session_id") ?? state.Str("sessionId") ?? "",
                Mode = state.Str("mode") ?? "",
                Dojo = state.Str("dojo") ?? "",
                Version = int.MinValue,
                MainColor = Color.white,
                LanternColor = Color.white,
                Volume = 0.55f
            };
            if (!state.Num("version", out var version))
                return snap;
            snap.Version = (int)Math.Round(version);

            var lights = state.Obj("lights");
            if (lights != null)
            {
                var main = lights.Obj("main");
                var lanterns = lights.Obj("lanterns");
                snap.HasMain = main != null;
                snap.HasLanterns = lanterns != null;
                snap.HasLights = snap.HasMain || snap.HasLanterns;
                if (main != null)
                {
                    snap.MainIntensity = Intensity(main, 1f);
                    snap.MainColor = ColorOf(main, Color.white);
                    snap.MainTransitionMs = Milliseconds(main);
                }

                if (lanterns != null)
                {
                    snap.LanternIntensity = Intensity(lanterns, 0.6f);
                    snap.LanternColor = ColorOf(lanterns, new Color(1f, 0.72f, 0.42f));
                    snap.LanternTransitionMs = Milliseconds(lanterns);
                }
            }

            snap.Devices = ReadDevices(state, lights);
            if (snap.Devices != null && snap.Devices.Length > 0)
                snap.HasLights = true;

            var ambience = state.Obj("ambience");
            if (ambience != null)
            {
                snap.HasAmbience = true;
                snap.Preset = ambience.Str("preset") ?? "";
                snap.Volume = Intensity(ambience, snap.Volume);
                if (ambience.Num("volume", out var vol))
                    snap.Volume = (float)vol;
                snap.AmbienceTransitionMs = Milliseconds(ambience);
            }

            var media = state.Obj("media");
            if (media != null)
            {
                snap.HasMedia = true;
                var playing = true;
                if (media.TryGet("playing", out var p) && p is JBool pb)
                    playing = pb.Value;
                snap.Media = new HouseMediaState
                {
                    Type = media.Str("type") ?? "audio",
                    Url = media.Str("url") ?? "",
                    Title = media.Str("title") ?? "",
                    Artist = media.Str("artist") ?? "",
                    License = media.Str("license") ?? "",
                    Playing = playing,
                    Device = media.Str("device") ?? "tv"
                };
            }

            if (string.IsNullOrEmpty(snap.Dojo) && string.Equals(snap.Mode, "switch_dojo", StringComparison.Ordinal))
                snap.Dojo = state.Str("profile_id") ?? "";
            return snap;
        }

        public static bool TryDevice(HouseDeviceState[] devices, string id, out HouseDeviceState device)
        {
            device = default;
            if (devices == null)
                return false;
            for (var i = 0; i < devices.Length; i++)
            {
                if (string.Equals(devices[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    device = devices[i];
                    return true;
                }
            }

            return false;
        }

        static HouseDeviceState[] ReadDevices(JObj state, JObj lights)
        {
            var list = new System.Collections.Generic.List<HouseDeviceState>(6);
            CollectBag(state.Obj("devices"), list);
            if (lights != null)
            {
                CollectBag(lights.Obj("devices"), list);
                CollectKnown(lights, list);
            }

            CollectKnown(state, list);
            return list.Count == 0 ? System.Array.Empty<HouseDeviceState>() : list.ToArray();
        }

        static void CollectBag(JObj bag, System.Collections.Generic.List<HouseDeviceState> list)
        {
            if (bag == null)
                return;
            for (var i = 0; i < bag.Items.Count; i++)
            {
                var pair = bag.Items[i];
                AddDevice(pair.Key, pair.Value as JObj, list);
            }
        }

        static void CollectKnown(JObj bag, System.Collections.Generic.List<HouseDeviceState> list)
        {
            if (bag == null)
                return;
            for (var i = 0; i < DeviceIds.Length; i++)
                AddDevice(DeviceIds[i], bag.Obj(DeviceIds[i]), list);
        }

        static void AddDevice(string id, JObj obj, System.Collections.Generic.List<HouseDeviceState> list)
        {
            if (string.IsNullOrEmpty(id) || obj == null)
                return;
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            var on = true;
            if (obj.TryGet("on", out var flag) && flag is JBool b)
                on = b.Value;
            var intensity = Intensity(obj, 1f);
            var temp = 24;
            if (obj.Num("temp", out var t) || obj.Num("temperature", out t))
                temp = Mathf.Clamp((int)Math.Round(t), 16, 30);
            var mode = NormalizeMode(obj.Str("mode"));
            var fan = NormalizeFan(obj);
            list.Add(new HouseDeviceState
            {
                Id = id,
                On = on,
                Color = ColorOf(obj, Color.white),
                Intensity = intensity,
                Temp = temp,
                Mode = mode,
                Fan = fan
            });
        }

        public static string NormalizeMode(string mode)
        {
            var m = (mode ?? "").Trim().ToLowerInvariant();
            if (m == "ventilar" || m == "fan" || m == "ventilacao" || m == "ventilação")
                return "ventilar";
            if (m == "seco" || m == "dry")
                return "seco";
            return "frio";
        }

        public static string NormalizeFan(JObj obj)
        {
            if (obj == null)
                return "auto";
            var s = obj.Str("fan");
            if (!string.IsNullOrEmpty(s))
            {
                var f = s.Trim().ToLowerInvariant();
                if (f == "1" || f == "2" || f == "3" || f == "auto")
                    return f;
            }

            if (obj.Num("fan", out var n))
            {
                var v = (int)Math.Round(n);
                if (v >= 1 && v <= 3)
                    return v.ToString(CultureInfo.InvariantCulture);
            }

            return "auto";
        }

        public static float FanScale(string fan)
        {
            switch ((fan ?? "auto").Trim().ToLowerInvariant())
            {
                case "1": return 0.55f;
                case "2": return 0.85f;
                case "3": return 1.2f;
                default: return 0.7f;
            }
        }

        static float Intensity(JObj obj, float fallback)
        {
            if (obj != null && obj.Num("intensity", out var n))
                return (float)n;
            return fallback;
        }

        static int Milliseconds(JObj obj)
        {
            if (obj != null && obj.Num("transition_ms", out var n) && n > 0)
                return (int)Math.Round(n);
            return 0;
        }

        /// <summary>
        /// API intensity is 0–1. Unity lights in this dojo need a wider range or a color change stays subtle.
        /// Values already above 1 are left alone so older tests keep their raw intensity.
        /// </summary>
        public static float DriveIntensity(float api, bool pointLight)
        {
            if (api > 1.01f)
                return api;
            var u = api < 0f ? 0f : (api > 1f ? 1f : api);
            return pointLight ? 0.15f + u * 2.8f : 0.35f + u * 2.2f;
        }

        public static bool TryColor(JVal value, out Color color)
        {
            color = Color.white;
            if (value is JStr s)
                return TryHex(s.Value, out color);
            var obj = value as JObj;
            if (obj == null)
                return false;
            if (!obj.Num("r", out var r) || !obj.Num("g", out var g) || !obj.Num("b", out var b))
                return false;
            if (r > 1.0 || g > 1.0 || b > 1.0)
            {
                r /= 255.0;
                g /= 255.0;
                b /= 255.0;
            }

            color = new Color(Mathf.Clamp01((float)r), Mathf.Clamp01((float)g), Mathf.Clamp01((float)b), 1f);
            return true;
        }

        public static bool TryHex(string text, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            var hex = text.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex.Substring(1);
            if (hex.Length == 3)
            {
                hex = string.Concat(
                    hex[0], hex[0],
                    hex[1], hex[1],
                    hex[2], hex[2]);
            }

            if (hex.Length != 6 && hex.Length != 8)
                return false;
            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r))
                return false;
            if (!byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g))
                return false;
            if (!byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                return false;
            color = new Color(r / 255f, g / 255f, b / 255f, 1f);
            return true;
        }

        static Color ColorOf(JObj obj, Color fallback)
        {
            if (obj != null && obj.TryGet("color", out var c) && TryColor(c, out var parsed))
                return parsed;
            return fallback;
        }
    }

    /// <summary>Applies only a strictly newer house version, and only for the visit that is open.</summary>
    public sealed class HouseStateCursor
    {
        public int Version { get; private set; } = -1;
        public HouseApplyPlan Plan { get; private set; }
        public bool HasPlan { get; private set; }

        public void Reset()
        {
            Version = -1;
            HasPlan = false;
            Plan = default;
        }

        public bool Offer(string json, string expectedSessionId)
        {
            if (!HouseStateLogic.TryParse(json, out var snap, out var stateNull) || stateNull)
                return false;
            if (!string.IsNullOrEmpty(snap.SessionId) && !string.IsNullOrEmpty(expectedSessionId)
                && !string.Equals(snap.SessionId, expectedSessionId, StringComparison.Ordinal))
                return false;
            if (!HouseStateLogic.IsNewer(Version, snap.Version))
                return false;
            Version = snap.Version;
            Plan = HouseStateLogic.Map(snap);
            HasPlan = true;
            return true;
        }
    }

    public sealed class ChatAction
    {
        public string Tool = "";
        public string ArgsJson = "{}";
        public string Dojo = "";
    }

    public sealed class ChatReply
    {
        public string Transcript = "";
        public string ReplyText = "";
        public string StateRaw = "";
        public ChatAction[] Actions = Array.Empty<ChatAction>();
        public bool IsChat;
    }

    public static class ChatReplyParser
    {
        public static bool TryParse(string json, out ChatReply reply)
        {
            reply = null;
            if (!GenJson.TryParse(json, out var root) || !(root is JObj obj))
                return false;
            if (obj.TryGet("toolCall", out _) || obj.TryGet("serverContent", out _) || obj.TryGet("setupComplete", out _))
                return false;
            var hasReply = obj.TryGet("reply_text", out _) || obj.TryGet("replyText", out _) || obj.TryGet("transcript", out _);
            var hasActions = obj.TryGet("actions", out _);
            var hasState = obj.TryGet("state", out _);
            if (!hasReply && !hasActions && !hasState)
                return false;
            reply = new ChatReply
            {
                Transcript = obj.Str("transcript") ?? "",
                ReplyText = obj.Str("reply_text") ?? obj.Str("replyText") ?? "",
                IsChat = true
            };
            if (obj.TryGet("state", out var state))
                reply.StateRaw = state is JNull ? "" : state.Raw ?? "";
            if (obj.TryGet("actions", out var actions) && actions is JArr arr)
            {
                var list = new ChatAction[arr.Items.Count];
                var n = 0;
                for (var i = 0; i < arr.Items.Count; i++)
                {
                    var item = arr.Items[i] as JObj;
                    if (item == null)
                        continue;
                    var tool = item.Str("tool") ?? item.Str("name") ?? "";
                    var args = "{}";
                    string dojo = "";
                    if (item.TryGet("args", out var argsVal))
                    {
                        args = string.IsNullOrEmpty(argsVal.Raw) ? "{}" : argsVal.Raw;
                        var argsObj = argsVal as JObj;
                        if (argsObj != null)
                            dojo = argsObj.Str("dojo") ?? argsObj.Str("profile_id") ?? argsObj.Str("target") ?? "";
                    }

                    list[n++] = new ChatAction { Tool = tool, ArgsJson = args, Dojo = dojo };
                }

                if (n != list.Length)
                {
                    var trimmed = new ChatAction[n];
                    Array.Copy(list, trimmed, n);
                    list = trimmed;
                }

                reply.Actions = list;
            }

            return true;
        }
    }
}
