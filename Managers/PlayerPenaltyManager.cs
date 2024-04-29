using System.Collections.Concurrent;

namespace CS2_SimpleAdmin;

public enum PenaltyType
{
	Mute,
	Gag,
	Silence
}

public class PlayerPenaltyManager
{
	private static readonly ConcurrentDictionary<int, Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration)>>> Penalties =
		new();

	// Add a penalty for a player
	public static void AddPenalty(int slot, PenaltyType penaltyType, DateTime endDateTime, int durationSeconds)
	{
		Penalties.AddOrUpdate(slot,
			(_) =>
			{
				var dict = new Dictionary<PenaltyType, List<(DateTime, int)>>
				{
					[penaltyType] = [(endDateTime, durationSeconds)]
				};
				return dict;
			},
			(_, existingDict) =>
			{
				if (!existingDict.TryGetValue(penaltyType, out var value))
				{
                    value = new List<(DateTime, int)>();
                    existingDict[penaltyType] = value;
				}

                value.Add((endDateTime, durationSeconds));
				return existingDict;
			});
	}

	public static bool IsPenalized(int slot, PenaltyType penaltyType)
	{
		//Console.WriteLine($"Checking penalties for player with slot {slot} and penalty type {penaltyType}");

		if (!Penalties.TryGetValue(slot, out var penaltyDict) ||
		    !penaltyDict.TryGetValue(penaltyType, out var penaltiesList)) return false;
		//Console.WriteLine($"Found penalties for player with slot {slot} and penalty type {penaltyType}");

		var now = DateTime.UtcNow.ToLocalTime();

		// Check if any active penalties exist
		foreach (var penalty in penaltiesList.ToList())
		{
			// Check if the penalty is still active
			if (penalty.Duration > 0 && now >= penalty.EndDateTime.AddSeconds(penalty.Duration))
			{
				//Console.WriteLine($"Removing expired penalty for player with slot {slot} and penalty type {penaltyType}");
				penaltiesList.Remove(penalty); // Remove expired penalty
				if (penaltiesList.Count == 0)
				{
					//Console.WriteLine($"No more penalties of type {penaltyType} for player with slot {slot}. Removing penalty type.");
					penaltyDict.Remove(penaltyType); // Remove penalty type if no more penalties exist
				}
			}
			else if (penalty.Duration == 0 || now < penalty.EndDateTime)
			{
				//Console.WriteLine($"Player with slot {slot} is penalized for type {penaltyType}");
				// Return true if there's an active penalty
				return true;
			}
		}

		// Return false if no active penalties are found
		//Console.WriteLine($"Player with slot {slot} is not penalized for type {penaltyType}");
		return false;

		// Return false if no penalties of the specified type were found for the player
		//Console.WriteLine($"No penalties found for player with slot {slot} and penalty type {penaltyType}");
	}

	// Get the end datetime and duration of penalties for a player and penalty type
	public static List<(DateTime EndDateTime, int Duration)> GetPlayerPenalties(int slot, PenaltyType penaltyType)
	{
		if (Penalties.TryGetValue(slot, out var penaltyDict) &&
		    penaltyDict.TryGetValue(penaltyType, out var penaltiesList))
		{
			return penaltiesList;
		}
		return [];
	}

	public static bool IsSlotInPenalties(int slot)
	{
		return Penalties.ContainsKey(slot);
	}

	// Remove all penalties for a player slot
	public static void RemoveAllPenalties(int slot)
	{
		if (Penalties.ContainsKey(slot))
		{
			Penalties.TryRemove(slot, out _);
		}
	}

	// Remove all penalties
	public static void RemoveAllPenalties()
	{
		Penalties.Clear();
	}

	// Remove all penalties of a selected type from a specific player
	public static void RemovePenaltiesByType(int slot, PenaltyType penaltyType)
	{
		if (Penalties.TryGetValue(slot, out Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration)>>? penaltyDict) &&
			penaltyDict.ContainsKey(penaltyType))
		{
			penaltyDict.Remove(penaltyType);
		}
	}

	// Remove all expired penalties for all players and penalty types
	public static void RemoveExpiredPenalties()
	{
		var now = DateTime.UtcNow.ToLocalTime();
		foreach (var kvp in Penalties.ToList()) // Use ToList to avoid modification while iterating
		{
			var playerSlot = kvp.Key;
			var penaltyDict = kvp.Value;

			// Remove expired penalties for the player
			foreach (var penaltiesList in penaltyDict.Values)
			{
				penaltiesList.RemoveAll(p => p.Duration > 0 && now >= p.EndDateTime.AddSeconds(p.Duration).ToLocalTime());
			}

			// Remove player slot if no penalties left
			if (penaltyDict.Count == 0)
			{
				Penalties.TryRemove(playerSlot, out _);
			}
		}
	}
}