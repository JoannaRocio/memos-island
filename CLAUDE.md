# Memos Island — contexto para Claude

Juego 2D en Unity 6 (URP 2D): vida en una isla + amistad con criaturas ("Memos") + carreras automáticas estilo *Monster Race*. Estética inspirada en Pokémon de GBA.

**El diseño completo está en [GDD.md](GDD.md). Leelo antes de implementar cualquier sistema y respetalo.** Si una decisión no está en el GDD, proponela y consultá antes de inventar.

## Cómo trabajamos
- El usuario habla español (rioplatense). Respondé en español.
- Se avanza **por fases** (GDD §22). Una fase a la vez; al terminar, se prueba en Unity y se marca en "Estado" abajo.
- Cada fase termina con algo **jugable**. Mostrá el resultado (escena, captura o pasos para probar) antes de pasar a la siguiente.
- Repositorio: https://github.com/JoannaRocio/memos-island (público, rama `main`). Al terminar y probar cada fase, hacer commit y push (preguntar antes de pushear).
- Usá MCP for Unity (package `com.coplaydev.unity-mcp`) para crear escenas, GameObjects y assets, y revisá la consola después de cada cambio de scripts.

## Convenciones técnicas
- Todo el contenido propio va en `Assets/_MemosIsland/` (estructura en GDD §21).
- Código en inglés (clases, métodos, variables); textos del juego en español; comentarios breves.
- Datos en **ScriptableObjects** (especies, tipos, terrenos, habilidades, objetos, recetas, NPCs). Agregar contenido no debe requerir tocar código.
- Estado del juego en clases serializables (JSON) desde el inicio, para el guardado.
- Input System (no el Input Manager viejo).
- Pixel art: PPU 16, Filter Point, sin compresión, Pixel Perfect Camera 480×270.
- Lógica pura (efectividad, fórmulas, confianza, experiencia) con tests EditMode.

## Arte provisorio
- Se genera por código desde **grillas de texto** (un carácter = un color de la paleta) con una herramienta de editor que exporta PNG a `Art/Generated/`.
- Paleta base Sweetie 16 (GDD §18). Contorno oscuro, sombreado de 2 tonos y nada de degradados. Tiene que verse **bonito y coherente**, no como cuadrados de prueba.
- **Cómo funciona (Fase 0):**
  - Fuentes en `Assets/_MemosIsland/Art/Source/*.txt`. El formato está documentado arriba de `Scripts/Editor/PixelArt/PixelArtGenerator.cs` (sprite, size, pivot, outline, frame, flip, shift, copyfrom, mirror, variant, tile). Los colores están en `PixelPalette.cs`: `0-f` = Sweetie 16, `g-p` = extras (piel, maderas, violeta del collar `k`/`l`).
  - Al guardar un `.txt` se regeneran sus PNG solos. Menú: **Memos Island ▸ Arte ▸ Regenerar todo el pixel art**.
  - Salida: `Art/Generated/<archivo>/<sprite>[_<cuadro>].png`. Los sprites con `tile` también crean un `Tile`/`AnimatedTile` en `Art/Tiles/`.
  - Cualquier imagen nueva dentro de `Art/` se importa como pixel art (`PixelArtImportSettings`).
  - `Tools/PixelArt/make_art.py` compone los sprites grandes con formas sombreadas (tiles, árbol, Tostín 64×64) y escribe Tiles.txt, Props.txt y Memos.txt. **Characters.txt se dibuja a mano.** Si editás a mano un archivo que genera el script, se pierde al volver a correrlo: elegí uno de los dos caminos.
  - `Tools/PixelArt/preview.py archivo.txt [escala] [salida.png]` genera una hoja ampliada para revisar sprites sin abrir Unity (Python puro, sin dependencias).
- Escena de prueba: menú **Memos Island ▸ Fase 0 ▸ Crear escena de prueba** → `Scenes/Fase0_Prueba.unity`.
- **Fuente pixel:** `Art/FontSource/MemosFont.txt` (un glifo por línea, con tildes, ñ, ¿ ¡) → `Art/Generated/Fonts/MemosFont.asset` (`PixelFont`). Se regenera al guardar o con **Memos Island ▸ Arte ▸ Regenerar fuente**.
- Los Memos tiernos son redondos con ojos grandes; los legendarios (Karman, Draken, Randy) son angulosos y oscuros con detalles brillantes.

## Arquitectura (Fase 1)
- Ensamblados: `MemosIsland.Runtime` (Scripts/), `MemosIsland.Editor` (Scripts/Editor/), `MemosIsland.Tests.EditMode` (Tests/EditMode/). Un componente por archivo (Unity lo exige para guardarlo en escenas).
- **GameRoot** (`Resources/GameRoot.prefab`, `DontDestroyOnLoad`): jugador, cámara pixel perfect, interfaz, `GameClock`, `MapManager`, luz global. Se crea solo al darle Play a cualquier escena que tenga un `MapInfo`. Las escenas de mapa **no** llevan cámara ni luz global.
- **Mapas** = escenas en `Scenes/Maps/` con: `MapInfo` (nombre y límites), tilemaps `Ground`/`Buildings` en la capa **Solid** con `TilemapCollider2D` (solo las tiles `solid` bloquean), props con `BoxCollider2D` en Solid, `SpawnPoint` (id + dirección) y `Warp` (escena + spawn de destino). Tienen que estar en Build Settings.
- **Movimiento:** `GridMover` (casillas; la posición es el borde inferior central de la casilla) + `PlayerController` (toque = girar, mantener = caminar, B = correr, A = interactuar con `IInteractable` de frente).
- **Orden de dibujo:** el Renderer 2D ordena por eje Y; actores y props usan `sortingOrder` 10, suelo 0, edificios 1, interfaz 1000+, fundido 5000.
- **Interfaz:** cuelga de la cámara con escala ×2; sus coordenadas son pixels de una pantalla virtual de 240×135 (escala GBA). Usa el material `Sprite-Unlit-Default` para que la noche no la oscurezca. Textos con `PixelText`; diálogos con `GameRoot.Instance.Dialogue.Show(páginas)`.
- **Reloj real:** `GameClock` (hora del sistema; F5/F6/F7 para probar). Luz global con `DayNightLighting`; faroles y ventanas con `NightLight`.
- Constructores: **Memos Island ▸ Fase 1 ▸ Construir mundo de prueba** regenera arte, fuente, GameRoot y los mapas `Map_PuebloPuerto` y `Map_RefugioExterior` (sobrescribe esas escenas).

## Estado
- [x] Fase 0 — Base (octubre 2026)
- [x] Fase 1 — Mundo (octubre 2026; reloj real en vez del ciclo acelerado del GDD original)
- [ ] Fase 2 — Datos de Memos
- [ ] Fase 3 — Carreras
- [ ] Fase 4 — Vínculo y refugio
- [ ] Fase 5 — Progreso
- [ ] Fase 6 — Vida en la isla
- [ ] Fase 7 — Vecinos
- [ ] Fase 8 — Historia
- [ ] Fase 9 — Pulido
