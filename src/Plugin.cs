using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AnimalFeedGuard;

[BepInPlugin(Guid, "Animal Feed Guard", "1.0.0")]
public sealed class Plugin : BaseUnityPlugin
{
	public const string Guid = "com.ziluck.valheim.animalfeedguard";
	private static ConfigEntry<bool> protectionEnabled = null!;
	private static ConfigEntry<float> radius = null!;
	private Harmony? harmony;
	private static int cachedFrame = -1;
	private static readonly List<FeedingAnimal> animals = new();

	private readonly struct FeedingAnimal
	{
		internal readonly Vector3 Position;
		internal readonly List<ItemDrop> Foods;
		internal FeedingAnimal(Vector3 position, List<ItemDrop> foods) { Position = position; Foods = foods; }
	}

	private void Awake()
	{
		protectionEnabled = Config.Bind("General", "Enabled", true, "Leave edible animal feed on the ground near tamed animals during automatic pickup. Manual pickup is unaffected.");
		radius = Config.Bind("General", "Protection Radius", 5f, new ConfigDescription("Maximum distance in metres between the dropped item and a living tamed animal that can eat it. 0 disables protection. Includes animals that are already fed. Uses full 3D distance, without a line-of-sight requirement.", new AcceptableValueRange<float>(0f, 50f)));
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
	}

	// Restrict only the automatic pickup path. Do not change ItemDrop flags,
	// ownership, feeding, manual interaction, or the inventory's AddItem method.
	internal static bool CanAutoPickup(ItemDrop item)
	{
		if (!item || !item.m_autoPickup) return false;
		if (!protectionEnabled.Value || radius.Value <= 0) return true;
		string? food = item.m_itemData?.m_shared?.m_name;
		if (string.IsNullOrEmpty(food)) return true;
		RefreshAnimals();
		Vector3 position = item.transform.position;
		foreach (FeedingAnimal animal in animals)
		{
			if (!FeedRules.WithinRadius((position - animal.Position).sqrMagnitude, radius.Value)) continue;
			foreach (ItemDrop candidate in animal.Foods)
			{
				// Matches MonsterAI.CanConsume's shared-name comparison, including
				// runtime changes to the animal's consume list made by other mods.
				if (candidate && FeedRules.SameFood(food, candidate.m_itemData?.m_shared?.m_name)) return false;
			}
		}
		return true;
	}

	private static void RefreshAnimals()
	{
		if (cachedFrame == Time.frameCount) return;
		cachedFrame = Time.frameCount;
		animals.Clear();
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!character || character.IsDead() || !character.IsTamed()) continue;
			MonsterAI ai = character.GetComponent<MonsterAI>();
			if (!ai || ai.m_consumeItems == null || ai.m_consumeItems.Count == 0) continue;
			animals.Add(new FeedingAnimal(character.transform.position, ai.m_consumeItems));
		}
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
