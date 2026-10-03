using System.Collections.Generic;

namespace MemosIsland.Memos
{
    /// <summary>
    /// Diario de vínculo (GDD §11): cada nivel de confianza alcanzado le devuelve un recuerdo al Memo.
    /// Los rescatados de un collar recuperan de a poco su vida anterior.
    /// En la Fase 8 se suman los recuerdos especiales de los iniciales con el abuelo.
    /// </summary>
    public static class MemoryBook
    {
        static readonly Dictionary<TrustLevel, string[]> Generic = new()
        {
            [TrustLevel.Fear] = new[]
            {
                "Un ruido fuerte, una mano que lo agarraba… y después, nada. {name} no quiere recordar más.",
                "{name} se acuerda del frío. Mucho frío, y nadie que lo abrigara.",
            },
            [TrustLevel.Distrust] = new[]
            {
                "{name} te miró comer desde lejos. Pensó que olías a pan tostado.",
                "La primera noche en el refugio, {name} no durmió. Te escuchó roncar y se quedó tranquilo.",
            },
            [TrustLevel.Neutral] = new[]
            {
                "{name} se acordó de {habitat}: el olor del lugar donde nació.",
                "{name} recordó un juego que jugaba de chiquito: perseguir su propia sombra.",
            },
            [TrustLevel.Trusting] = new[]
            {
                "{name} ya no escucha aquella voz que le daba órdenes. Ahora elige quedarse con vos.",
                "{name} recordó la primera vez que corrió con vos. El viento en la cara. Se sintió libre.",
                "{name} se acuerda de que le diste de comer cuando tenía miedo. No se lo olvida.",
            },
            [TrustLevel.Friend] = new[]
            {
                "{name} juntó una piedrita brillante para vos. En su memoria, sos parte de su manada.",
                "{name} recordó una canción que alguien le cantaba. Ahora la tararea cuando estás cerca.",
            },
            [TrustLevel.Soulmate] = new[]
            {
                "{name} ya no tiene miedo de olvidar: guarda cada día con vos como un tesoro.",
                "Si cierra los ojos, {name} puede verte llegando al refugio. Es su recuerdo favorito.",
            },
        };

        static readonly string RescuedFirst =
            "Un collar frío, una luz violeta, una voz que daba órdenes… {name} tiembla cuando lo recuerda.";

        /// <summary>El recuerdo que recupera al alcanzar ese nivel.</summary>
        public static string MemoryFor(MemoInstance m, TrustLevel level)
        {
            if (!Generic.TryGetValue(level, out var options)) return null;
            string text;
            if (m.rescued && level == TrustLevel.Fear) text = RescuedFirst;
            else if (m.rescued && level == TrustLevel.Trusting) text = options[0];
            else
            {
                // El primer recuerdo de "Confía" es exclusivo de los rescatados.
                int first = level == TrustLevel.Trusting ? 1 : 0;
                text = options[first + StableHash(m.uid) % (options.Length - first)];
            }
            var species = m.Species;
            return text.Replace("{name}", m.DisplayName)
                .Replace("{habitat}", species != null ? species.habitat.ToLowerInvariant() : "su hogar");
        }

        /// <summary>Todos los recuerdos que ya recuperó, del más viejo al más nuevo.</summary>
        public static List<string> Unlocked(MemoInstance m)
        {
            var list = new List<string>();
            // El recuerdo del miedo solo lo tienen los que vivieron algo feo (rescatados de un collar).
            var first = m.rescued ? TrustLevel.Fear : TrustLevel.Distrust;
            for (var level = first; level <= m.highestTrust; level++)
            {
                var text = MemoryFor(m, level);
                if (text != null) list.Add(text);
            }
            if (m.grewTogether) list.Add(GrewTogether(m));
            return list;
        }

        /// <summary>Recuerdo especial al superar la etapa rebelde después de evolucionar (GDD §13).</summary>
        public static string GrewTogether(MemoInstance m) =>
            $"Después de evolucionar, {m.DisplayName} estaba raro, gruñón, como si no se reconociera. " +
            "Pero un día volvió a buscarte. Crecieron juntos.";

        static int StableHash(string s)
        {
            int h = 0;
            foreach (var c in s ?? "") h = (h * 31 + c) & 0x7fffffff;
            return h;
        }
    }
}
