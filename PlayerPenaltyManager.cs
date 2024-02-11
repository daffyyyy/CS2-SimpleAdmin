using System.Collections.Concurrent;

namespace CS2_SimpleAdmin
{
	public enum PenaltyType
	{
		Mute,
		Gag,
		Silence
	}

	public class PlayerPenaltyManager
	{
		private static ConcurrentDictionary<int, Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration)>>> penalties =
			new ConcurrentDictionary<int, Dictionary<PenaltyType, List<(DateTime, int)>>>();

		// Add a penalty for a player
		public void AddPenalty(int slot, PenaltyType penaltyType, DateTime endDateTime, int durationSeconds)
		{
			if (!penalties.ContainsKey(slot))
			{
				penalties[slot] = new Dictionary<PenaltyType, List<(DateTime, int)>>();
			}

			if (!penalties[slot].ContainsKey(penaltyType))
			{
				penalties[slot][penaltyType] = new List<(DateTime, int)>();
			}

			penalties[slot][penaltyType].Add((endDateTime, durationSeconds));
		}

		public bool IsPenalized(int slot, PenaltyType penaltyType)
		{
			//Console.WriteLine($"Checking penalties for player with slot {slot} and penalty type {penaltyType}");

			if (penalties.TryGetValue(slot, out var penaltyDict) && penaltyDict.TryGetValue(penaltyType, out var penaltiesList))
			{
				//Console.WriteLine($"Found penalties for player with slot {slot} and penalty type {penaltyType}");

				DateTime now = DateTime.Now;

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
			}

			// Return false if no penalties of the specified type were found for the player
			//Console.WriteLine($"No penalties found for player with slot {slot} and penalty type {penaltyType}");
			return false;
		}

		// Get the end datetime and duration of penalties for a player and penalty type
		public List<(DateTime EndDateTime, int Duration)> GetPlayerPenalties(int slot, PenaltyType penaltyType)
		{
			if (penalties.TryGetValue(slot, out Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration)>>? penaltyDict) &&
				penaltyDict.TryGetValue(penaltyType, out List<(DateTime EndDateTime, int Duration)>? penaltiesList) && penaltiesList != null)
			{
				return penaltiesList;
			}
			return new List<(DateTime EndDateTime, int Duration)>();
		}

		public bool IsSlotInPenalties(int slot)
		{
			return penalties.ContainsKey(slot);
		}

		// Remove all penalties for a player slot
		public void RemoveAllPenalties(int slot)
		{
			if (penalties.ContainsKey(slot))
			{
				penalties.TryRemove(slot, out _);
			}
		}

		// Remove all penalties
		public void RemoveAllPenalties()
		{
			penalties.Clear();
		}

		// Remove all penalties of a selected type from a specific player
		public void RemovePenaltiesByType(int slot, PenaltyType penaltyType)
		{
			if (penalties.TryGetValue(slot, out Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration)>>? penaltyDict) &&
				penaltyDict.ContainsKey(penaltyType))
			{
				penaltyDict.Remove(penaltyType);
			}
		}

		// Remove all expired penalties for all players and penalty types
		public void RemoveExpiredPenalties()
		{
			DateTime now = DateTime.Now;
			foreach (var kvp in penalties.ToList()) // Use ToList to avoid modification while iterating
			{
				var playerSlot = kvp.Key;
				var penaltyDict = kvp.Value;

				// Remove expired penalties for the player
				foreach (var penaltiesList in penaltyDict.Values)
				{
					penaltiesList.RemoveAll(p => p.Duration > 0 && now >= p.EndDateTime.AddSeconds(p.Duration));
				}

				// Remove player slot if no penalties left
				if (penaltyDict.Count == 0)
				{
					penalties.TryRemove(playerSlot, out _);
				}
			}
		}
	}
}