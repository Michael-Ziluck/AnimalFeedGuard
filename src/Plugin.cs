using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AnimalFeedGuard;

[BepInPlugin(Guid, "AnimalFeedGuard", "2.0.1")]
public sealed class Plugin : BaseUnityPlugin
{
	public const string Guid = "com.ziluck.valheim.animalfeedguard";
	private static ConfigEntry<bool> protectionEnabled = null!;
	private static ConfigEntry<float> radius = null!;
	private static ConfigEntry<bool> diagnosticLogging = null!;
	private static ManualLogSource log = null!;
	private static readonly Dictionary<(string Food, bool Blocked), double> decisions = new();
	private Harmony? harmony;
	private static int cachedFrame = -1;
	private static readonly List<FeedingAnimal> animals = new();

	private readonly struct FeedingAnimal
	{
		internal readonly Character Character;
		internal readonly MonsterAI AI;
		internal FeedingAnimal(Character character, MonsterAI ai) { Character = character; AI = ai; }
	}

	private void Awake()
	{
		log = Logger;
		protectionEnabled = Config.Bind("General", "Enabled", true, "Leave edible animal feed on the ground near tamed animals during automatic pickup. Manual pickup is unaffected.");
		radius = Config.Bind("General", "Protection Radius", 5f, new ConfigDescription("Maximum distance in metres from the dropped item to the nearest point on a living tamed animal's body collider (or its origin if no active collider is available). 0 disables protection. Includes animals that are already fed. Uses full 3D distance, without a line-of-sight requirement.", new AcceptableValueRange<float>(0f, 50f)));
		diagnosticLogging = Config.Bind("Diagnostics", "Log Pickup Decisions", false, "Log automatic pickup decisions and the nearest diet-matching creature's distance and tame state. Useful for testing pickup conflicts; messages for the same food and outcome are limited to once every five seconds.");
		try
		{
			harmony = new Harmony(Guid);
			harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"Animal Feed Guard ready. Protection radius: {radius.Value}m.");
		}
		catch (Exception error)
		{
			Logger.LogError($"Animal Feed Guard could not patch automatic pickup. Feed protection is NOT active: {error}");
			harmony?.UnpatchSelf();
		}
	}

	private void OnDestroy()
	{
		harmony?.UnpatchSelf();
		animals.Clear();
		cachedFrame = -1;
		decisions.Clear();
	}

	// Restrict only the automatic pickup path. Do not change ItemDrop flags,
	// ownership, feeding, manual interaction, or the inventory's AddItem method.
	internal static bool CanAutoPickup(ItemDrop item)
	{
		if (!item || !item.m_autoPickup) return false;
		if (!protectionEnabled.Value || radius.Value <= 0) return true;
		string food = item.m_itemData?.m_shared?.m_name ?? string.Empty;
		if (food.Length == 0) return true;
		RefreshAnimals();
		Vector3 position = item.transform.position;
		string? nearest = null;
		float nearestDistance = float.PositiveInfinity;
		bool diagnostics = diagnosticLogging.Value;
		foreach (FeedingAnimal animal in animals)
		{
			Character character = animal.Character;
			MonsterAI ai = animal.AI;
			if (!character || !ai || ai.m_consumeItems == null) continue;
			bool alive = !character.IsDead();
			bool tamed = IsTamed(character);
			if (!diagnostics && (!alive || !tamed)) continue;
			float squaredDistance = DistanceSquared(character, position);
			if (FeedRules.Protects(alive, tamed, squaredDistance, radius.Value, food, FoodNames(ai.m_consumeItems)))
			{
				if (diagnostics) Report(food, true, Describe(character, squaredDistance, position, alive, tamed));
				return false;
			}
			if (diagnostics && squaredDistance < nearestDistance && FoodNames(ai.m_consumeItems).Any(candidate => FeedRules.SameFood(food, candidate)))
			{
				nearestDistance = squaredDistance;
				nearest = Describe(character, squaredDistance, position, alive, tamed);
			}
		}
		if (diagnostics) Report(food, false, nearest ?? "no loaded creature has this food in its diet");
		return true;
	}

	internal static IEnumerable<string?> FoodNames(List<ItemDrop> foods)
	{
		// Use the live consume list, including replacements made by other mods.
		// The shared-name comparison is the one used by MonsterAI.CanConsume.
		foreach (ItemDrop candidate in foods)
			if (candidate) yield return candidate.m_itemData?.m_shared?.m_name;
	}

	internal static bool IsTamed(Character character)
	{
		// Character.IsTamed caches remote state for a second and stops refreshing
		// on becoming the owner. Read the synchronized flag for ownership handoffs.
		ZNetView view = character.GetComponent<ZNetView>();
		return view && view.IsValid()
			? view.GetZDO().GetBool(ZDOVars.s_tamed, character.IsTamed())
			: character.IsTamed();
	}

	internal static float DistanceSquared(Character character, Vector3 itemPosition)
	{
		Collider body = character.GetCollider();
		Vector3 closest = body && body.enabled && body.gameObject.activeInHierarchy
			? body.ClosestPoint(itemPosition) : character.transform.position;
		return (itemPosition - closest).sqrMagnitude;
	}

	private static string Describe(Character character, float squaredDistance, Vector3 itemPosition, bool alive, bool tamed) =>
		$"{Utils.GetPrefabName(character.gameObject)}: body distance {Math.Sqrt(squaredDistance):F2}m, origin distance {Vector3.Distance(itemPosition, character.transform.position):F2}m, alive={alive}, tamed={tamed}";

	private static void Report(string food, bool blocked, string detail)
	{
		double now = Time.timeAsDouble;
		var key = (food, blocked);
		if (decisions.TryGetValue(key, out double previous) && now - previous < 5) return;
		decisions[key] = now;
		log.LogInfo($"Automatic pickup {(blocked ? "blocked" : "allowed")} for {food}; protection radius {radius.Value:F2}m; {detail}.");
	}

	private static void RefreshAnimals()
	{
		if (cachedFrame == Time.frameCount) return;
		cachedFrame = Time.frameCount;
		animals.Clear();
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!character) continue;
			MonsterAI ai = character.GetComponent<MonsterAI>();
			if (ai) animals.Add(new FeedingAnimal(character, ai));
		}
		// Only component discovery is cached. Tameness, life, position, and diet
		// are rechecked for each item, so a stale snapshot cannot allow pickup.
	}

	[HarmonyPatch(typeof(Player), "AutoPickup")]
	private static class AutomaticPickupPatch
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var field = AccessTools.Field(typeof(ItemDrop), nameof(ItemDrop.m_autoPickup));
			var filter = AccessTools.Method(typeof(Plugin), nameof(CanAutoPickup));
			List<CodeInstruction> code = instructions.ToList();
			if (code.Count(i => i.LoadsField(field)) != 1)
				throw new InvalidOperationException("Expected one ItemDrop.m_autoPickup check. Game version or another pickup mod changed this method.");
			foreach (CodeInstruction instruction in code)
			{
				if (instruction.LoadsField(field))
				{
					// Same input/output stack as ldfld bool; retain branch labels and
					// exception blocks on this instruction for other Harmony patches.
					instruction.opcode = OpCodes.Call;
					instruction.operand = filter;
				}
				yield return instruction;
			}
		}
	}
}
