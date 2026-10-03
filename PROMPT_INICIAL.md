# Prompts para construir Memos Island con Claude

Abrí una sesión nueva de Claude Code **en la carpeta `D:\Proyectos Unity\Memos Island`**, con el proyecto abierto en Unity y MCP for Unity conectado.
Pegá el **Prompt inicial**. Cuando termine cada fase y la pruebes, pegá el prompt de la fase siguiente.

---

## Prompt inicial (Fase 0)

```
Hola Claude. Vamos a crear mi videojuego "Memos Island — La Isla de los Recuerdos" en este proyecto de Unity 6 (URP 2D).

Todo el diseño está en GDD.md y las reglas de trabajo en CLAUDE.md. Leé los dos completos antes de empezar: ahí están la historia, los 20 Memos, las carreras, el sistema de confianza, el refugio, la granja, los vecinos, el estilo de arte, la arquitectura técnica y la hoja de ruta por fases.

Vamos a construir la demo (Capítulo 1) fase por fase, siguiendo el GDD §22. Hoy hacemos la FASE 0 — Base:

1. Crear la estructura de carpetas de Assets/_MemosIsland/ del GDD §21.
2. Configurar el proyecto para pixel art: Pixel Perfect Camera a 480x270 y PPU 16, import con Filter Point y sin compresión (con un AssetPostprocessor para la carpeta de arte), y Light 2D global.
3. Crear la herramienta de editor "Pixel Art Generator": sprites definidos como grillas de texto (un carácter por color de la paleta Sweetie 16 del GDD §18), exportados a PNG en Art/Generated/ con la configuración de importación correcta. Tiene que soportar sprites de varios cuadros (animaciones) y paletas alternativas (para Memos brillantes y con collar).
4. Usar esa herramienta para crear arte provisorio BONITO, en estilo GBA:
   - Tiles de 16x16: pasto, pasto alto, camino, agua, árbol, flores, cerca, pared y techo de casa.
   - El protagonista de 16x32, caminando en 4 direcciones.
   - Un Memo de prueba (Tostín, de 16x16 para el mundo y de 64x64 para carreras) con contorno oscuro, ojos grandes y sombreado de 2 tonos.
5. Una escena de prueba que muestre todo eso nítido en pantalla.

Antes de escribir código, contame en pocas líneas tu plan para esta fase. Al terminar, verificá que la consola de Unity no tenga errores, marcá la fase en CLAUDE.md y decime cómo probarla.
```

---

## Prompts de las fases siguientes

**Fase 1 — Mundo**
```
Seguimos con la FASE 1 — Mundo del GDD §22: movimiento por casillas en 4 direcciones (estilo Pokémon GBA, con correr), tilemaps con colisiones, interacción con el botón A, transiciones entre mapas con fundido, cámara que sigue al jugador, y reloj de día con iluminación (GDD §16). Armá un mapa de prueba chiquito de Pueblo Puerto y el exterior del Refugio con el arte provisorio. Contame el plan antes de empezar.
```

**Fase 2 — Datos de Memos**
```
Seguimos con la FASE 2 — Datos de Memos: ScriptableObjects para tipos, terrenos, la tabla de efectividad (GDD §8, con la regla de tipo doble), las 20 especies con sus stats y habilidades (GDD §7), y las personalidades. Generá los 20 assets de especies, el sprite provisorio de cada Memo (respetando el estilo de cada uno: los tiernos redondos y Karman, Draken y Randy cool) y una Memodex básica. Agregá tests EditMode de efectividad. Contame el plan antes de empezar.
```

**Fase 3 — Carreras**
```
Seguimos con la FASE 3 — Carreras, el corazón mecánico del juego (GDD §9 y §10): pista lateral por tramos de terreno, corredores, energía, cambio libre con 8 segundos de espera, habilidades únicas como efectos componibles, IA rival, interfaz con los 6 retratos y las flechas de efectividad, los formatos (captura 1v1 de un solo terreno, amistosa, entrenador y Copa de 6 tramos) y el flujo de captura con comida. Primero hacé que sea divertida con arte simple; después la embellecemos. Contame el plan antes de empezar.
```

**Fase 4 — Vínculo y refugio**
```
Seguimos con la FASE 4 — Vínculo y refugio (GDD §11 y §12): niveles de confianza y cómo suben, comportamiento visible según la confianza, necesidades, IA de vida de los Memos (entran y salen de la casa, rutinas por personalidad), burbujas de emoción, relaciones entre Memos con mejores amigos y padrinos, el impulso de amistad en carrera, el compañero que te sigue y el diario de vínculo con recuerdos. El refugio tiene que sentirse vivo. Contame el plan antes de empezar.
```

**Fase 5 — Progreso**
```
Seguimos con la FASE 5 — Progreso (GDD §13 y §14): experiencia, niveles, crecimiento de stats, evolución con escena, etapa rebelde, comportamiento Hostil de los legendarios y el sistema de equipamiento con sus 2 espacios y los accesorios estéticos visibles. Contame el plan antes de empezar.
```

**Fase 6 — Vida en la isla**
```
Seguimos con la FASE 6 — Vida en la isla (GDD §15): inventario, dinero, herramientas, energía del jugador, granja con 5 cultivos, mina de 5 pisos que se regenera cada día, fundición, crafteo, la tienda de Deny, la herrería de Fer, la caja de envíos y los Memos trabajando según su tipo. Contame el plan antes de empezar.
```

**Fase 7 — Vecinos**
```
Seguimos con la FASE 7 — Vecinos (GDD §5): los personajes con rutinas por hora y clima, sistema de diálogos con etiquetas de pronombre, amistad por corazones, regalos favoritos, tablón de pedidos y eventos de amistad. Contame el plan antes de empezar.
```

**Fase 8 — Historia**
```
Seguimos con la FASE 8 — Historia (GDD §4 y §17): prólogo en pantalla negra, creación de personaje, llegada en barco, el diario con las 3 siluetas y la elección, el encuentro con el minijuego de calma, los Memos con collar en las rutas, Karman, las pistas de Draken y Randy en el pueblo, la Copa con la escena de Lalo y Pipo, y el final con el gancho. Tiene que emocionar. Contame el plan antes de empezar.
```

**Fase 9 — Pulido**
```
Seguimos con la FASE 9 — Pulido: guardado y carga completos, menú de título y de pausa, audio, mejoras de arte, partículas, balance de economía y carreras, y una pasada general para que la demo se pueda mostrar. Contame el plan antes de empezar.
```

---

## Consejos
- **Una fase por sesión** (o varias sesiones si es grande). No pidas todo junto: sale mejor por partes.
- Probá cada fase en Unity antes de seguir. Si algo no te gusta, decilo en el momento.
- Si cambiás una idea de diseño, pedile a Claude que **actualice también el GDD.md**, así siempre está al día.
