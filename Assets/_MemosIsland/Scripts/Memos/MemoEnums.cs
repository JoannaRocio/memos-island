namespace MemosIsland.Memos
{
    /// <summary>Qué tan bien corre un tipo sobre un terreno (GDD §8).</summary>
    public enum Effectiveness
    {
        Weak = -1,   // ▼ poco eficaz
        Normal = 0,  // · normal
        Strong = 1,  // ▲ muy eficaz
    }

    /// <summary>Corre / Nada / Vuela / Excava: define qué atajos puede tomar en las pistas.</summary>
    public enum Mobility { Runs, Swims, Flies, Digs }

    public enum MemoCategory { Starter, Legendary, Wild, Exclusive }

    /// <summary>Cómo se consigue un Memo en la versión actual del juego.</summary>
    public enum MemoAvailability
    {
        Available,     // se puede conseguir
        Locked,        // "Zona inaccesible" hasta una actualización (Draken, Randy)
        NotObtainable, // solo lo usa un personaje (exclusivos de la Capitana Vera)
    }

    /// <summary>Los tres arquetipos de habilidad del GDD, más las de beneficio propio.</summary>
    public enum AbilityArchetype { HinderBehind, HinderAhead, ChangeTerrain, Self }

    /// <summary>Efecto principal de una habilidad; la Fase 3 (carreras) lo interpreta.</summary>
    public enum AbilityEffect
    {
        SlowFollowers, // estela que frena a los de atrás
        Root,          // atrapa al de adelante
        ChangeTerrain, // cambia el terreno del tramo
        Stun,          // aturde
        Sprint,        // acelera mucho
        Teleport,      // desaparece y reaparece más adelante
        Zigzag,        // el rival corre en zigzag
        Sleep,         // el rival se duerme
        Blind,         // encandila
        Recover,       // recupera energía
        Copy,          // copia la habilidad de un rival
        Magnet,        // atrae y roba velocidad
        TeamBoost,     // el próximo del equipo entra a velocidad máxima
    }

    /// <summary>Rasgos de temperamento que no son un simple cambio de stats.</summary>
    public enum TemperamentTrait
    {
        None,
        SteadyPace, // Constante: no baja de ritmo con poca energía
        Comeback,   // Remontador: más rápido cuando va último
        Proud,      // Orgulloso: a veces ignora órdenes con confianza baja
    }
}
