using System.Collections.Generic;
using UnityEngine;

namespace DeliveryBot.Delivery
{
    /// <summary>Pure selection logic for pickup/drop-off points (unit-testable, no scene access).</summary>
    public static class JobPicker
    {
        /// <summary>
        /// Picks a random index whose position is at least <paramref name="minDistance"/> from <paramref name="from"/>
        /// and is not <paramref name="exclude"/>. Relaxes the distance rule if nothing qualifies. Returns -1 if empty.
        /// </summary>
        public static int Pick(IReadOnlyList<Vector3> positions, Vector3 from, float minDistance, int exclude, System.Random rng)
        {
            if (positions == null || positions.Count == 0) return -1;

            var far = new List<int>();
            var any = new List<int>();
            for (var i = 0; i < positions.Count; i++)
            {
                if (i == exclude) continue;
                any.Add(i);
                if (Vector3.Distance(positions[i], from) >= minDistance) far.Add(i);
            }

            if (far.Count > 0) return far[rng.Next(far.Count)];
            if (any.Count > 0) return any[rng.Next(any.Count)];
            return exclude >= 0 && exclude < positions.Count ? exclude : 0;
        }
    }
}
