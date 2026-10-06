using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object? instance) => instance != null && !instance.Destroyed;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator -(Vector3 first, Vector3 second) => new Vector3(first.x - second.x, first.y - second.y, first.z - second.z);
        public static float Distance(Vector3 first, Vector3 second) => (float)Math.Sqrt((first - second).sqrMagnitude);
    }
    public class Transform { public Vector3 position; }
    public class GameObject { public string name = ""; public bool activeInHierarchy = true; }
    public class MonoBehaviour : Object
    {
        public readonly Dictionary<Type, Object> Components = new Dictionary<Type, Object>();
        public Transform transform = new Transform();
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : Object => Components.TryGetValue(typeof(T), out Object component) ? (T)component : null!;
    }
    public class Collider : MonoBehaviour
    {
        public bool enabled = true;
        public Vector3 Contact;
        public Vector3 ClosestPoint(Vector3 position) => Contact;
    }
    public static class Time { public static int frameCount; public static double timeAsDouble; }
}
namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; public ConfigEntry(T setting) => Value = setting; }
    public class AcceptableValueRange<T> { public AcceptableValueRange(T minimum, T maximum) { } }
    public class ConfigDescription { public ConfigDescription(string description, object range) { } }
    public class ConfigFile
    {
        public readonly Dictionary<string, object> Entries = new Dictionary<string, object>();
        public ConfigEntry<T> Bind<T>(string section, string key, T setting, object description)
        {
            var entry = new ConfigEntry<T>(setting);
            Entries[key] = entry;
            return entry;
        }
    }
}
namespace BepInEx.Logging
{
    public class ManualLogSource
    {
        public readonly List<string> Messages = new List<string>();
        public void LogInfo(object message) => Messages.Add(message.ToString());
        public void LogError(object message) => throw new Exception(message.ToString());
    }
}
namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BepInPlugin : Attribute { public BepInPlugin(string guid, string name, string version) { } }
    public class BaseUnityPlugin : UnityEngine.MonoBehaviour
    {
        public Configuration.ConfigFile Config = new Configuration.ConfigFile();
        public Logging.ManualLogSource Logger = new Logging.ManualLogSource();
    }
}
public class Character : UnityEngine.MonoBehaviour
{
    public static readonly List<Character> Characters = new List<Character>();
    public bool Dead, Tamed;
    public UnityEngine.Collider? Body;
    public bool IsDead() => Dead;
    public bool IsTamed() => Tamed;
    public UnityEngine.Collider GetCollider() => Body!;
    public static List<Character> GetAllCharacters() => Characters;
}
public class MonsterAI : UnityEngine.MonoBehaviour { public List<ItemDrop> m_consumeItems = new List<ItemDrop>(); }
public class ZDO
{
    public bool? Tamed;
    public bool GetBool(int hash, bool defaultValue) => Tamed ?? defaultValue;
}
public class ZNetView : UnityEngine.MonoBehaviour
{
    public bool Valid = true;
    public readonly ZDO Record = new ZDO();
    public bool IsValid() => Valid;
    public ZDO GetZDO() => Record;
}
public static class ZDOVars { public const int s_tamed = 1; }
public static class Utils { public static string GetPrefabName(UnityEngine.GameObject gameObject) => gameObject.name; }
public class ItemDrop : UnityEngine.MonoBehaviour
{
    public bool m_autoPickup = true;
    public bool Collected;
    public ItemData m_itemData = new ItemData();
    public class ItemData { public SharedData m_shared = new SharedData(); }
    public class SharedData { public string m_name = ""; }
}
public class Player : Character
{
    public readonly List<ItemDrop> Drops = new List<ItemDrop>();
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AutoPickup(float dt)
    {
        foreach (ItemDrop drop in Drops)
            if (drop.m_autoPickup) drop.Collected = true;
    }
}
