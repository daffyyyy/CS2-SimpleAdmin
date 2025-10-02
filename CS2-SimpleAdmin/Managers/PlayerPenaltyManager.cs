using CS2_SimpleAdminApi;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin.Managers;

public static class PlayerPenaltyManager
{
    private static readonly ConcurrentDictionary<int, Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>>> Penalties =
        new();

    /// <summary>
    /// Adds a penalty for a specific player slot and penalty type.
    /// </summary>
    /// <param name="slot">The player slot where the penalty should be applied.</param>
    /// <param name="penaltyType">The type of penalty to apply (e.g. gag, mute, silence).</param>
    /// <param name="endDateTime">The validity expiration date/time of the penalty.</param>
    /// <param name="durationInMinutes">The duration of the penalty in minutes (0 for permanent).</param>
    public static void AddPenalty(int slot, PenaltyType penaltyType, DateTime endDateTime, int durationInMinutes)
    {
        Penalties.AddOrUpdate(slot,
            (_) =>
            {
                var dict = new Dictionary<PenaltyType, List<(DateTime, int, bool)>>
                {
                    [penaltyType] = [(endDateTime, durationInMinutes, false)]
                };
                return dict;
            },
            (_, existingDict) =>
            {
                if (!existingDict.TryGetValue(penaltyType, out var value))
                {
                    value = new List<(DateTime, int, bool)>();
                    existingDict[penaltyType] = value;
                }

                value.Add((endDateTime, durationInMinutes, false));
                return existingDict;
            });
    }

    /// <summary>
    /// Determines whether a player is currently penalized with the given penalty type.
    /// </summary>
    /// <param name="slot">The player slot to check.</param>
    /// <param name="penaltyType">The penalty type to check.</param>
    /// <param name="endDateTime">The out-parameter returning the end datetime of the penalty if active.</param>
    /// <returns>True if the player has an active penalty, false otherwise.</returns>
    public static bool IsPenalized(int slot, PenaltyType penaltyType, out DateTime? endDateTime)
    {
        endDateTime = null;

        if (!Penalties.TryGetValue(slot, out var penaltyDict) ||
            !penaltyDict.TryGetValue(penaltyType, out var penaltiesList)) return false;

        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode == 0)
        {
            if (penaltiesList.Count == 0) return false;
            
            endDateTime = penaltiesList.First().EndDateTime;
            return true;
        }

        var now = Time.ActualDateTime();

        // Check if any active penalties exist
        foreach (var penalty in penaltiesList.ToList())
        {
            // Check if the penalty is still active
            if (penalty.Duration > 0 && now >= penalty.EndDateTime)
            {
                penaltiesList.Remove(penalty); // Remove expired penalty
                if (penaltiesList.Count == 0)
                {
                    penaltyDict.Remove(penaltyType); // Remove penalty type if no more penalties exist
                }
            }
            else if (penalty.Duration == 0 || now < penalty.EndDateTime)
            {
                // Set endDateTime to the end time of this active penalty
                endDateTime = penalty.EndDateTime;
                return true;
            }
        }

        // Return false if no active penalties are found
        return false;
    }

    /// <summary>
    /// Retrieves all penalties for a player of a specific penalty type.
    /// </summary>
    /// <param name="slot">The player slot.</param>
    /// <param name="penaltyType">The penalty type to retrieve.</param>
    /// <returns>A list of penalties if found, otherwise an empty list.</returns>
    public static List<(DateTime EndDateTime, int Duration, bool Passed)> GetPlayerPenalties(int slot, PenaltyType penaltyType)
    {
        if (Penalties.TryGetValue(slot, out var penaltyDict) &&
            penaltyDict.TryGetValue(penaltyType, out var penaltiesList))
        {
            return penaltiesList;
        }
        return [];
    }
    
    /// <summary>
    /// Retrieves all penalties for a player across multiple penalty types.
    /// </summary>
    /// <param name="slot">The player slot.</param>
    /// <param name="penaltyType">A list of penalty types to retrieve.</param>
    /// <returns>A combined list of penalties of all requested types.</returns>
    public static List<(DateTime EndDateTime, int Duration, bool Passed)> GetPlayerPenalties(int slot, List<PenaltyType> penaltyType)
    {
        List<(DateTime EndDateTime, int Duration, bool Passed)> result = [];

        if (Penalties.TryGetValue(slot, out var penaltyDict))
        {
            foreach (var type in penaltyType)
            {
                if (penaltyDict.TryGetValue(type, out var penaltiesList))
                {
                    result.AddRange(penaltiesList);
                }
            }
        }

        return result;
    }
    
    /// <summary>
    /// Retrieves all penalties for a player across all penalty types.
    /// </summary>
    /// <param name="slot">The player slot.</param>
    /// <returns>A dictionary with penalty types as keys and lists of penalties as values.</returns>
    public static Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>> GetAllPlayerPenalties(int slot)
    {
        // Check if the player has any penalties in the dictionary
        return Penalties.TryGetValue(slot, out var penaltyDict) ?
            // Return all penalty types and their respective penalties for the player
            penaltyDict :
            // If the player has no penalties, return an empty dictionary
            new Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>>();
    }

    /// <summary>
    /// Checks if a given slot has any penalties assigned.
    /// </summary>
    /// <param name="slot">The player slot.</param>
    /// <returns>True if the player has any penalties, false otherwise.</returns>
    public static bool IsSlotInPenalties(int slot)
    {
        return Penalties.ContainsKey(slot);
    }

    /// <summary>
    /// Removes all penalties assigned to a specific player slot.
    /// </summary>
    /// <param name="slot">The player slot.</param>
    public static void RemoveAllPenalties(int slot)
    {
        if (Penalties.ContainsKey(slot))
        {
            Penalties.TryRemove(slot, out _);
        }
    }

    /// <summary>
    /// Removes all penalties for all players.
    /// </summary>
    public static void RemoveAllPenalties()
    {
        Penalties.Clear();
    }

    /// <summary>
    /// Removes all penalties of a specific type from a player.
    /// </summary>
    /// <param name="slot">The player slot.</param>
    /// <param name="penaltyType">The penalty type to remove.</param>
    public static void RemovePenaltiesByType(int slot, PenaltyType penaltyType)
    {
        if (Penalties.TryGetValue(slot, out var penaltyDict) &&
            penaltyDict.ContainsKey(penaltyType))
        {
            penaltyDict.Remove(penaltyType);
        }
    }

    /// <summary>
    /// Marks penalties with a specific end datetime as "passed" for a player.
    /// </summary>
    /// <param name="slot">The player slot.</param>
    /// <param name="dateTime">The end datetime of penalties to mark as passed.</param>
    public static void RemovePenaltiesByDateTime(int slot, DateTime dateTime)
    {
        if (!Penalties.TryGetValue(slot, out var penaltyDict)) return;

        foreach (var penaltiesList in penaltyDict.Values)
        {
            for (var i = 0; i < penaltiesList.Count; i++)
            {
                if (penaltiesList[i].EndDateTime != dateTime) continue;
                // Create a copy of the penalty
                var penalty = penaltiesList[i];

                // Update the end datetime of the copied penalty to the current datetime
                penalty.Passed = true;

                // Replace the original penalty with the modified one
                penaltiesList[i] = penalty;
            }
        }
    }

    /// <summary>
    /// Removes or expires penalties automatically across all players based on their duration or "passed" flag.
    /// </summary>
    /// <remarks>
    /// If <c>TimeMode == 0</c>, penalties are considered passed manually and are removed if flagged as such.  
    /// Otherwise, expired penalties are removed based on the current datetime compared with their end time.
    /// </remarks>
    public static void RemoveExpiredPenalties()
    {
        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode == 0)
        {
            foreach (var (playerSlot, penaltyDict) in Penalties.ToList()) // Use ToList to avoid modification while iterating
            {
                // Remove expired penalties for the player
                foreach (var penaltiesList in penaltyDict.Values)
                {
                    penaltiesList.RemoveAll(p => p is { Duration: > 0, Passed: true });
                }

                // Remove player slot if no penalties left
                if (penaltyDict.Count == 0)
                {
                    Penalties.TryRemove(playerSlot, out _);
                }
            }

            return;
        }

        var now = Time.ActualDateTime();
        foreach (var (playerSlot, penaltyDict) in Penalties.ToList()) // Use ToList to avoid modification while iterating
        {
            foreach (var penaltiesList in penaltyDict.Values)
            {
                penaltiesList.RemoveAll(p => p.Duration > 0 && now >= p.EndDateTime);
            }

            if (penaltyDict.Count == 0)
            {
                Penalties.TryRemove(playerSlot, out _);
            }
        }
    }
}