# Memos Island — contexto para Claude

Juego 2D en Unity 6 (URP 2D): vida en una isla + amistad con criaturas ("Memos") + carreras automáticas estilo *Monster Race*. Estética inspirada en Pokémon de GBA.

**El diseño completo está en [GDD.md](GDD.md). Leelo antes de implementar cualquier sistema y respetalo.** Si una decisión no está en el GDD, proponela y consultá antes de inventar.

## Cómo trabajamos
- El usuario habla español (rioplatense). Respondé en español.
- Se avanza **por fases** (GDD §22). Una fase a la vez; al terminar, se prueba en Unity y se marca en "Estado" abajo.
- Cada fase termina con algo **jugable**. Mostrá el resultado (escena, captura o pasos para probar) antes de pasar a la siguiente.
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
- Los Memos tiernos son redondos con ojos grandes; los legendarios (Karman, Draken, Randy) son angulosos y oscuros con detalles brillantes.

## Estado
- [x] Fase 0 — Base (octubre 2026)
- [ ] Fase 1 — Mundo
- [ ] Fase 2 — Datos de Memos
- [ ] Fase 3 — Carreras
- [ ] Fase 4 — Vínculo y refugio
- [ ] Fase 5 — Progreso
- [ ] Fase 6 — Vida en la isla
- [ ] Fase 7 — Vecinos
- [ ] Fase 8 — Historia
- [ ] Fase 9 — Pulido
