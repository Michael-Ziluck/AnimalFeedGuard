using System;
using System.Collections.Generic;

namespace AnimalFeedGuard;

internal static class FeedRules
{
	internal static bool WithinRadius(float squaredDistance, float radius) => radius > 0 && squaredDistance >= 0 && squaredDistance <= radius * radius;
	internal static bool SameFood(string? item, string? animalFood) => !string.IsNullOrEmpty(item) && string.Equals(item, animalFood, StringComparison.Ordinal);

	internal static bool Protects(bool alive, bool tamed, float squaredDistance, float radius, string? food, IEnumerable<string?> diet)
	{
		if (!alive || !tamed || !WithinRadius(squaredDistance, radius)) return false;
		foreach (string? candidate in diet)
			if (SameFood(food, candidate)) return true;
		return false;
	}
}
