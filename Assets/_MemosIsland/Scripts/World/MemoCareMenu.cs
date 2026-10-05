using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;

namespace MemosIsland.World
{
    /// <summary>
    /// Menú que aparece al apretar A frente a un Memo (en el refugio o el compañero):
    /// acariciar, dar de comer, jugar, bañar, ver su diario, llevarlo de compañero y elegirle padrino.
    /// </summary>
    public static class MemoCareMenu
    {
        /// <param name="react">Muestra una emoción sobre el Memo.</param>
        /// <param name="companionChanged">Avisa que cambió el compañero (para crear o quitar actores).</param>
        public static void Open(MemoInstance memo, Action<Emote> react, Action companionChanged)
        {
            AudioManager.Cry(memo.Species);
            var root = GameRoot.Instance;
            var state = root.State;
            bool isCompanion = state.companionUid == memo.uid;
            var level = memo.TrustLevel;

            var options = new List<(string label, Action action)>
            {
                ("Acariciar", () => Care(memo, CareAction.Pet, react)),
                ("Dar de comer", () => ChooseFood(memo, react)),
                ("Jugar", () => Care(memo, CareAction.Play, react)),
                ("Bañar", () => Care(memo, CareAction.Bath, react)),
                ("Diario", () => root.MyMemos.Open(memo)),
            };
            if (isCompanion)
                options.Add(("Dejar en el refugio", () => SetCompanion(null, memo, companionChanged)));
            else if (level >= TrustLevel.Neutral)
                options.Add(("Que me acompañe", () => SetCompanion(memo, memo, companionChanged)));
            if (level <= TrustLevel.Distrust && GodparentCandidates(state, memo).Any())
                options.Add(("Elegir padrino", () => ChooseGodparent(memo)));
            options.Add(("Nada", () => { }));

            root.Dialogue.ShowChoice($"{memo.DisplayName} · {TrustRules.Name(level)}",
                options.Select(o => o.label).ToList(), i => options[i].action(), options.Count - 1);
        }

        /// <summary>Comidas que tenés, con las favoritas del Memo primero (marcadas con ♥).</summary>
        public static List<ItemData> FoodsFor(MemoInstance memo, GameState state) =>
            state.inventory.Select(s => MemoDatabase.Instance.GetItem(s.id))
                .Where(i => i != null && i.kind == ItemKind.Food)
                .OrderByDescending(i => MemoCare.IsFavorite(memo, i)).ToList();

        static void ChooseFood(MemoInstance memo, Action<Emote> react)
        {
            var root = GameRoot.Instance;
            var foods = FoodsFor(memo, root.State);
            if (foods.Count == 0)
            {
                root.Dialogue.Show(new[] { "No tenés comida. Cosechá en la huerta, fabricala en la mesa de trabajo o comprala en lo de Deny." });
                return;
            }
            var labels = foods.Select(f => $"{(MemoCare.IsFavorite(memo, f) ? "♥ " : "")}{f.displayName} ×{root.State.CountOf(f.id)}").ToList();
            labels.Add("Nada");
            root.Dialogue.ShowChoice($"¿Qué le das a {memo.DisplayName}?", labels, i =>
            {
                if (i >= foods.Count) return;
                Feed(memo, foods[i], react);
            }, labels.Count - 1);
        }

        static void Feed(MemoInstance memo, ItemData food, Action<Emote> react)
        {
            var root = GameRoot.Instance;
            var result = MemoCare.ApplyFood(memo, food, GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now);
            if (result.success) root.State.RemoveItem(food.id);
            react?.Invoke(result.success ? Emote.Music : Emote.Dots);
            var pages = new List<string>();
            if (!string.IsNullOrEmpty(result.message)) pages.Add(result.message);
            if (result.newMemory) AddMemoryPages(memo, result.level, pages);
            if (pages.Count > 0) root.Dialogue.Show(pages);
        }

        public static void Care(MemoInstance memo, CareAction action, Action<Emote> react, bool favorite = false)
        {
            var result = MemoCare.Apply(memo, action, GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now, favorite);
            react?.Invoke(result.success
                ? action == CareAction.Feed ? Emote.Music : Emote.Love
                : memo.TrustLevel <= TrustLevel.Fear ? Emote.Scared : Emote.Dots);
            var pages = new List<string>();
            if (!string.IsNullOrEmpty(result.message)) pages.Add(result.message);
            if (result.newMemory) AddMemoryPages(memo, result.level, pages);
            if (pages.Count > 0) GameRoot.Instance.Dialogue.Show(pages);
        }

        /// <summary>Páginas de diálogo de "recordó algo" cuando sube a un nivel nuevo de confianza.</summary>
        public static void AddMemoryPages(MemoInstance memo, TrustLevel level, List<string> pages)
        {
            pages.Add($"♪ {memo.DisplayName} ahora te tiene: {TrustRules.Name(level)}. Recordó algo…");
            var text = MemoryBook.MemoryFor(memo, level);
            if (text != null) pages.Add(text);
        }

        static void SetCompanion(MemoInstance newCompanion, MemoInstance memo, Action companionChanged)
        {
            var root = GameRoot.Instance;
            root.State.companionUid = newCompanion?.uid;
            if (newCompanion == null) memo.inside = false;
            companionChanged?.Invoke();
            root.Dialogue.Show(new[]
            {
                newCompanion != null
                    ? $"¡{memo.DisplayName} va a acompañarte por la isla! ♪"
                    : $"{memo.DisplayName} se queda en el refugio.",
            });
        }

        static IEnumerable<MemoInstance> GodparentCandidates(GameState state, MemoInstance memo) =>
            state.AllMemos.Where(m => m != memo && m.TrustLevel >= TrustLevel.Friend);

        static void ChooseGodparent(MemoInstance memo)
        {
            var root = GameRoot.Instance;
            var candidates = GodparentCandidates(root.State, memo).ToList();
            var labels = candidates.Select(c => c.DisplayName).ToList();
            labels.Add("Nadie");
            root.Dialogue.ShowChoice($"¿Quién acompaña a {memo.DisplayName} para que pierda el miedo?", labels, i =>
            {
                if (i >= candidates.Count) return;
                memo.godparentUid = candidates[i].uid;
                root.Dialogue.Show(new[]
                {
                    $"{candidates[i].DisplayName} va a quedarse cerca de {memo.DisplayName}. " +
                    "Al ver que con vos está bien, va a confiar más rápido.",
                });
            }, labels.Count - 1);
        }
    }
}
