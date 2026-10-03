using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.Memos
{
    public enum FriendshipLevel { Strangers, Acquaintances, Friends, BestFriends }

    [Serializable]
    public class MemoRelationship
    {
        public string a, b; // uids ordenados (a < b)
        public float points;
    }

    /// <summary>Amistad entre dos Memos (GDD §12): sube jugando, durmiendo cerca y compartiendo comida.</summary>
    public static class Friendship
    {
        public const float AcquaintancesAt = 20f, FriendsAt = 60f, BestFriendsAt = 120f;

        public static FriendshipLevel LevelFor(float points) => points switch
        {
            >= BestFriendsAt => FriendshipLevel.BestFriends,
            >= FriendsAt => FriendshipLevel.Friends,
            >= AcquaintancesAt => FriendshipLevel.Acquaintances,
            _ => FriendshipLevel.Strangers,
        };

        public static string Name(FriendshipLevel level) => level switch
        {
            FriendshipLevel.BestFriends => "Mejores amigos",
            FriendshipLevel.Friends => "Amigos",
            FriendshipLevel.Acquaintances => "Conocidos",
            _ => "Desconocidos",
        };

        public static float Points(List<MemoRelationship> all, string uidA, string uidB)
        {
            var r = Find(all, uidA, uidB, false);
            return r?.points ?? 0f;
        }

        public static FriendshipLevel Level(List<MemoRelationship> all, string uidA, string uidB) =>
            LevelFor(Points(all, uidA, uidB));

        public static bool AreBestFriends(List<MemoRelationship> all, string uidA, string uidB) =>
            uidA != uidB && Level(all, uidA, uidB) == FriendshipLevel.BestFriends;

        /// <summary>Suma amistad (con la compatibilidad de personalidades). Devuelve true si subieron de nivel.</summary>
        public static bool Add(List<MemoRelationship> all, MemoInstance x, MemoInstance y, float amount)
        {
            if (x == null || y == null || x.uid == y.uid) return false;
            var r = Find(all, x.uid, y.uid, true);
            var before = LevelFor(r.points);
            r.points = Mathf.Max(0f, r.points + amount * Compatibility(x.temperamentId, y.temperamentId));
            return LevelFor(r.points) > before;
        }

        /// <summary>Personalidades que se llevan bien suben más rápido (GDD §12).</summary>
        public static float Compatibility(string t1, string t2)
        {
            bool Pair(string a, string b) => (t1 == a && t2 == b) || (t1 == b && t2 == a);
            if (Pair("jugueton", "mimoso") || Pair("jugueton", "remontador") || Pair("dormilon", "constante")) return 1.5f;
            if (Pair("orgulloso", "timido") || Pair("impulsivo", "dormilon")) return 0.5f;
            if (t1 == "orgulloso" || t2 == "orgulloso") return 0.8f;
            return 1f;
        }

        static MemoRelationship Find(List<MemoRelationship> all, string uidA, string uidB, bool create)
        {
            if (string.CompareOrdinal(uidA, uidB) > 0) (uidA, uidB) = (uidB, uidA);
            var r = all.Find(x => x.a == uidA && x.b == uidB);
            if (r == null && create)
            {
                r = new MemoRelationship { a = uidA, b = uidB };
                all.Add(r);
            }
            return r;
        }
    }
}
