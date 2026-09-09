using System;

namespace AnimalFeedGuard;

internal static class FeedRules
{
	internal static bool WithinRadius(float squaredDistance, float radius) => radius > 0 && squaredDistance >= 0 && squaredDistance <= radius * radius;
	internal static bool SameFood(string? item, string? animalFood) => !string.IsNullOrEmpty(item) && string.Equals(item, animalFood, StringComparison.Ordinal);
}
