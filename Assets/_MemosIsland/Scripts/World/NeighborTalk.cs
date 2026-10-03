using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Town;
using MemosIsland.UI;
using Random = UnityEngine.Random;

namespace MemosIsland.World
{
    /// <summary>
    /// Charlar con un vecino (Fase 7): saludo según corazones, clima, lo que está haciendo y tu compañero;
    /// después, regalar, entregar un pedido o comprar si está trabajando. Los eventos de amistad
    /// se disparan al hablarle con suficientes corazones.
    /// </summary>
    public static class NeighborTalk
    {
        const string Work = "trabajo";

        static DateTime Now => GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;

        public static void Start(NeighborNpc npc, Action done)
        {
            var root = GameRoot.Instance;
            var data = npc.Data;
            var friend = root.State.town.Get(data.id);

            if (friend.met)
            {
                var ev = NeighborFriendship.PendingEvent(data, friend, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                if (ev != null)
                {
                    PlayEvent(npc, ev, friend, done);
                    return;
                }
            }

            bool firstMeet = !friend.met;
            NeighborFriendship.Talk(friend, Now);
            var greeting = firstMeet && !string.IsNullOrEmpty(data.firstMeet) ? data.firstMeet : PickLine(npc, friend);
            Say(data, new[] { greeting }, () => Menu(npc, friend, done));
        }

        static void Say(NeighborData data, IEnumerable<string> pages, Action onClosed = null)
        {
            var dialogue = GameRoot.Instance.Dialogue;
            dialogue.SetSpeaker(data.displayName, data.portrait);
            dialogue.Show(pages, onClosed);
        }

        static string PickLine(NeighborNpc npc, NeighborState friend)
        {
            var data = npc.Data;
            var state = GameRoot.Instance.State;
            var companion = state.Companion;
            if (Weather.IsRainy(Now) && data.linesRain.Count > 0 && Random.value < 0.5f)
                return data.linesRain[Random.Range(0, data.linesRain.Count)];
            if (npc.Entry != null && !string.IsNullOrEmpty(npc.Entry.line) && Random.value < 0.4f)
                return npc.Entry.line;
            if (companion != null && data.linesCompanion.Count > 0 && Random.value < 0.3f)
                return data.linesCompanion[Random.Range(0, data.linesCompanion.Count)].Replace("{memo}", companion.DisplayName);
            var lines = NeighborFriendship.LinesFor(data, friend);
            return lines.Count > 0 ? lines[Random.Range(0, lines.Count)] : "…";
        }

        // ------------------------------------------------------------------ Menú

        static void Menu(NeighborNpc npc, NeighborState friend, Action done)
        {
            var root = GameRoot.Instance;
            var data = npc.Data;
            var options = new List<(string label, Action action)>();

            var request = RequestBoard.ActiveFor(root.State.town, data.id);
            if (request != null) options.Add(("Entregar pedido", () => DeliverRequest(npc, request, friend, done)));
            var shop = ShopFor(npc);
            if (shop != null) options.Add((shop.Value.label, () => { shop.Value.open(); done(); }));
            options.Add(("Regalar", () => Gift(npc, friend, done)));
            options.Add(("Chau", done));

            var dialogue = root.Dialogue;
            dialogue.SetSpeaker(data.displayName, data.portrait);
            dialogue.ShowChoice($"Amistad {Hearts(friend)}", options.Select(o => o.label).ToList(),
                i => options[i].action(), options.Count - 1);
        }

        /// <summary>Corazones como texto: ♥♥♥······· (los vacíos con puntos).</summary>
        public static string Hearts(NeighborState friend)
        {
            int h = NeighborFriendship.Hearts(friend);
            return new string('♥', h) + new string('·', NeighborFriendship.MaxHearts - h);
        }

        static (string label, Action open)? ShopFor(NeighborNpc npc)
        {
            if (npc.Entry == null || npc.Entry.activity != Work) return null;
            return npc.Data.id switch
            {
                "deny" => ("Comprar", IslandMenus.OpenDenyShop),
                "fer" => ("Herrería", IslandMenus.OpenFerForge),
                "anni" => ("Revisar Memos", () => Say(npc.Data, TownMenus.ClinicCheckup())),
                _ => null,
            };
        }

        // ------------------------------------------------------------------ Regalos y pedidos

        static void Gift(NeighborNpc npc, NeighborState friend, Action done)
        {
            var data = npc.Data;
            var state = GameRoot.Instance.State;
            if (friend.giftedOn == Farm.IslandState.DateKey(Now))
            {
                Say(data, new[] { "¡Ya me diste algo hoy! Guardalo para otro día." }, done);
                return;
            }
            TownMenus.OpenGift(data, itemId =>
            {
                if (string.IsNullOrEmpty(itemId) || state.CountOf(itemId) <= 0)
                {
                    done();
                    return;
                }
                var reaction = NeighborFriendship.Gift(data, friend, itemId, Now);
                state.RemoveItem(itemId);
                npc.Bubble?.Show(reaction switch
                {
                    GiftReaction.Loved => Emote.Love,
                    GiftReaction.Liked => Emote.Music,
                    GiftReaction.Disliked => Emote.Dots,
                    _ => Emote.Surprise,
                });
                var text = reaction switch
                {
                    GiftReaction.Loved => data.lovedText,
                    GiftReaction.Liked => data.likedText,
                    GiftReaction.Disliked => data.dislikedText,
                    _ => data.neutralText,
                };
                Say(data, new[] { text }, done);
            });
        }

        static void DeliverRequest(NeighborNpc npc, DailyRequest request, NeighborState friend, Action done)
        {
            var data = npc.Data;
            var state = GameRoot.Instance.State;
            var t = RequestBoard.TemplateOf(request, data);
            if (t == null || !RequestBoard.CanComplete(t, state))
            {
                string need = t == null ? "" : t.kind == RequestKind.Deliver
                    ? $"Necesito {t.count} {MemoDatabase.Instance.GetItem(t.targetId)?.displayName.ToLowerInvariant()} (tenés {state.CountOf(t.targetId)})."
                    : $"Quiero ver un {MemoDatabase.Instance.GetSpecies(t.targetId)?.displayName} de cerca. ¡Traelo de compañero!";
                Say(data, new[] { need }, done);
                return;
            }
            RequestBoard.Complete(request, t, state, friend);
            npc.Bubble?.Show(Emote.Love);
            Say(data, new[] { t.thanks, $"(Recibiste ${t.reward}.)" }, done);
        }

        // ------------------------------------------------------------------ Eventos de amistad

        static void PlayEvent(NeighborNpc npc, FriendshipEvent ev, NeighborState friend, Action done)
        {
            var data = npc.Data;
            friend.seenEvents.Add(ev.id);
            NeighborFriendship.Talk(friend, Now);
            npc.Bubble?.Show(Emote.Love, 1.5f);
            Say(data, ev.pages, () =>
            {
                if (string.IsNullOrEmpty(ev.question) || ev.answers.Count == 0)
                {
                    done();
                    return;
                }
                var dialogue = GameRoot.Instance.Dialogue;
                dialogue.SetSpeaker(data.displayName, data.portrait);
                dialogue.ShowChoice(ev.question, ev.answers, i =>
                {
                    if (i < ev.answerPoints.Count) NeighborFriendship.Add(friend, ev.answerPoints[i]);
                    var reply = i < ev.replies.Count ? ev.replies[i] : null;
                    if (string.IsNullOrEmpty(reply)) done();
                    else Say(data, new[] { reply }, done);
                });
            });
        }
    }
}
