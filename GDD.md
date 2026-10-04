# Memos Island — *La Isla de los Recuerdos*
### Documento de Diseño del Juego (GDD) — Demo / Capítulo 1

> Versión 1.0 · Octubre 2026
> Motor: Unity 6 (URP 2D) · Plataforma inicial: PC · Idioma: Español (preparado para traducir)

---

## 0. Índice
1. Concepto
2. Pilares de diseño
3. Tono y estilo
4. Historia
5. Personajes (vecinos)
6. La isla (mundo y zonas)
7. Los Memos (20 especies)
8. Tipos, terrenos y efectividad
9. Carreras
10. Captura
11. Confianza y vínculo
12. El refugio vivo
13. Niveles, evolución y etapa rebelde
14. Equipamiento
15. Vida en la isla: granja, mina, crafteo, tienda, economía
16. Ciclo de día y tiempo
17. Creación de personaje
18. Arte (provisorio y final)
19. Audio
20. Alcance de la demo
21. Arquitectura técnica
22. Hoja de ruta (fases)
23. Contenido para futuras actualizaciones

---

## 1. Concepto

**Memos Island** es un juego de vida y amistad con criaturas, con estética inspirada en Pokémon de Game Boy Advance y carreras automáticas inspiradas en *Monster Race* (Koei, 1998).

El jugador hereda el refugio abandonado de su abuelo en una isla, rescata **Memos** atrapados por collares que les borran la memoria y la voluntad, y los ayuda a volver a confiar. Las carreras son la forma de liberarlos y de demostrar que **el vínculo es más fuerte que el control**.

**Frase de venta:** *"Cuidá, rescatá y corré junto a criaturas que recuerdan a quienes las quieren."*

### ¿Qué es un Memo?
Los Memos son criaturas que **guardan los recuerdos de quienes los quieren**. Cuanto más vínculo tienen con alguien, más recuerdos atesoran, y eso los hace más fuertes.
Los **collares de Ápice** les borran la memoria: olvidan su nombre, su hogar y a quienes amaban, y obedecen sin voluntad.

---

## 2. Pilares de diseño

1. **Vínculo y cuidado (el corazón).** El verdadero juego es hacerse amigo de los Memos, darles un hogar y ver crecer la confianza.
2. **Una isla viva.** Vecinos con rutinas, Memos con vida propia, granja, mina y tienda, al estilo Stardew Valley pero simple.
3. **Carreras simples y estratégicas.** Dos botones (Cambiar y Habilidad). La estrategia está en armar el equipo y elegir cuándo cambiar.
4. **Una historia que conmueve.** Para todo público, pero que duela ver sufrir a los Memos y den ganas de salvarlos a todos.

**Regla de oro:** todos los sistemas se conectan con los Memos. La granja produce su comida, la mina da material para su equipo, los vecinos cuentan su historia y las carreras los liberan.

---

## 3. Tono y estilo

- **Para todo público**, sin violencia explícita.
- **Acogedor** en el día a día (refugio, granja, vecinos) y **emotivo** en la historia.
- La tristeza nace de **mostrar lo que los Memos perdieron**:
  - Un Memo con collar no reacciona a su nombre.
  - Recién liberado, se encoge cuando intentás acariciarlo.
  - Un Memo con collar da un paso hacia vos, recuerda algo… y el collar brilla y lo obliga a darse vuelta.
- **Leitmotiv musical:** una melodía de cajita de música que suena cada vez que un Memo recupera un recuerdo.
- Sin castigos duros: descuidar a un Memo lo pone triste, pero nunca se muere ni se escapa para siempre.

---

## 4. Historia — "El Collar" (Capítulo 1 = Demo)

### Trasfondo
- La isla vivía en armonía con los Memos. Las carreras eran una tradición de amistad entre corredores y Memos.
- **El abuelo** del protagonista, cuidador de Memos, inventó con **Anni** (la veterinaria) un **collar de calma** para ayudar a Memos con recuerdos traumáticos: borraba solo el dolor.
- La empresa **Ápice**, del continente, se apropió del invento y lo convirtió en un collar que **borra todo**. Los Memos con collar son obedientes, rápidos y vacíos.
- Ápice llegó a la isla patrocinando la **Copa de la Isla** y trajo dinero y trabajo. Casi nadie sospecha.
- Hace un año, **el abuelo desapareció** la misma noche en que se llevaron a los tres Memos que cuidaba.

### Prólogo — pantalla negra
Solo se oye una tormenta, voces y cadenas. Se ven dos ojos brillando en la oscuridad (después se sabe que son de **Draken**).
> *"Este es fuerte. Pónganle el collar."*

Un grito y un destello violeta. Después, silencio. Aparece el título.

### Acto 1 — La herencia
1. **Creación de personaje** (ver sección 17).
2. El protagonista llega en barco a **Pueblo Puerto**. Lo recibe **Lalo**, su amigo de la infancia.
3. Una carta del abuelo le deja el **refugio**, abandonado y lleno de polvo.
4. En el refugio encuentra **el diario del abuelo**, con tres páginas arrancadas a medias. De cada Memo queda **la silueta borroneada (negra) y su historia** escrita a mano:
   - **Tostín:** *"Lo encontré dormido entre las brasas de un horno de pan abandonado. Se hace el duro, pero llora si lo dejás solo de noche."*
   - **Brotito:** *"Nació de una semilla que planté el día que nació mi nieto/a/e. Es tímido, mira todo con mucha atención. Crecieron juntos sin conocerse."*
   - **Charquito:** *"Perdió a su manada en una tormenta. Imita a todos para no sentirse solo. Todavía mira el mar esperando."*
   - Al pie: *"Cuidé a estos tres hasta la noche en que se los llevaron. Si alguna vez lees esto… encontrá al menos a uno."*
5. **El jugador elige qué página seguir**: esa es la elección del Memo inicial. Nunca ve cómo es el Memo hasta encontrarlo.

### Acto 2 — El encuentro
1. Siguiendo pistas del diario, el protagonista entra al **Bosque Susurro**.
2. Encuentra al Memo elegido **herido, con un collar violeta, atacando por miedo**.
3. **Minijuego de calma** (no hay combate): quedarse quieto, acercarse despacio y ofrecer comida. Si te movés rápido, retrocede.
4. El protagonista lleva puesta la **bufanda del abuelo**. El Memo la huele y **se detiene**. Suena la cajita de música. El collar se quiebra.
5. El Memo se derrumba en sus brazos. Lo lleva al refugio. **Anni** lo cura.
6. Empieza en confianza **Miedo**: se esconde bajo la cama. El primer objetivo del jugador es lograr que salga.

### Acto 3 — La isla y Ápice
- El jugador aprende la vida en la isla: granja, mina, tienda, vecinos.
- **Zorak** (ermitaño de la cueva) le enseña a correr "a la forma antigua".
- Aparecen **Memos salvajes con collar** en las rutas. Ganarles una carrera rompe el collar, y quedan en Miedo y necesitan cuidado.
- Los vecinos van revelando piezas:
  - **Fer** odia a Ápice y ayuda en secreto.
  - **Luca**, empleado de Ápice, empieza a dudar.
  - **Anni** esconde algo.
  - **La alcaldesa Marga** mira para otro lado.
- Momento duro: **un Memo con collar da un paso hacia el protagonista, recuerda… y el collar lo obliga a irse**.
- **Karman** aparece en las tormentas de los Acantilados (legendario conseguible en la demo).

### Clímax — La final de la Copa
1. El protagonista llega a la final de la **Copa de la Isla** contra la **Capitana Vera** (honesta) y, antes, contra **Lalo**.
2. La familia de Lalo necesita el premio. Desesperado, **Lalo le pone un collar a su propio Memo**, el que crió desde chico (**Pipo**, un Plumín).
3. Pipo gana el primer tramo, colapsa en plena pista y **ya no reconoce a Lalo**. Lalo le muestra el juguete que le regaló de cachorro, y Pipo lo ignora.
4. El protagonista salta a la pista y lo calma como a su inicial. El collar se rompe. Pipo recuerda.
5. Lalo, llorando: *"Solo quería ser suficiente."*
6. El protagonista gana la final contra Vera. Vera, que desconfiaba de Ápice, se vuelve aliada.

### Cierre de la demo
1. Anni examina el collar de Pipo. En el metal hay un grabado: **la firma del abuelo**.
2. Anni confiesa: *"Él lo inventó para calmar a los Memos heridos… no para esto. Lo hicimos juntos."*
3. Último plano: en una torre de Ápice en el continente, el **Director Sílex** mira una pantalla con la foto del protagonista. Detrás de él, en las sombras, una figura con **la misma bufanda que el abuelo**.
4. **"Continuará…"** y se muestra la MemoBox con Draken y Randy: *"Zona inaccesible: próximamente."*

### Para rejugar
- Los dos iniciales no elegidos aparecen **en el juego completo** como Memos con collar de los jefes de Ápice. Liberarlos va a pegar fuerte porque el jugador conoce su historia por el diario.
- En la demo: si el jugador termina la historia, ve a uno de los dos iniciales no elegidos en el plano final de la torre de Ápice, con collar.

---

## 5. Personajes

| Nombre | Rol | Personalidad | Arco | Dónde está |
|---|---|---|---|---|
| **Protagonista** | Nieto/a/e del abuelo | Lo define el jugador | Rescatar a los Memos y descubrir la verdad del abuelo | Refugio |
| **Lalo** | Amigo de la infancia y rival | Alegre, competitivo, inseguro | Le pone el collar a su Memo Pipo en la final. Redención | Casa familiar, puerto |
| **Anni** | Veterinaria | Dulce, maternal, culpable | Co-creadora del collar original. Lo confiesa al final | Clínica |
| **Fer** | Herrera | Directa, de pocas palabras, leal | Ápice le cerró el taller a su padre. Aliada secreta | Herrería |
| **Deny** | Dueño del almacén | Charlatán, simpático, distraído | Le vende materiales a Ápice sin saber para qué | Almacén |
| **Luca** | Empleado joven de Ápice | Idealista, cree en "el progreso" | Descubre la verdad y se vuelve informante | Oficina de Ápice, muelle |
| **Zorak** | Ermitaño de la cueva | Gruñón, cara de malo, corazón enorme | Excampeón de carreras. Perdió a Draken en el volcán | Cabaña junto a la cueva |
| **Jojo** | Nene del pueblo | Curioso pero con miedo a los Memos | Pierde el miedo con tu ayuda y adopta su primer Memo | Plaza, escuela |
| **Alcaldesa Marga** | Autoridad | Pragmática | ¿Progreso o principios? Moralmente gris | Municipio |
| **Capitana Vera** | Líder de la Copa (tipo Metal) | Dura, honesta, exigente | Desconfía de Ápice. Aliada al final | Estadio |
| **Director Sílex** | Jefe de Ápice | Frío, elegante | Villano principal (aparece al final) | Continente |

**Sistema de amistad con vecinos:** de 0 a 10 corazones. Sube con charlas diarias (+), regalos (los favoritos dan +++) y pedidos completados. Cada vecino tiene 2 o 3 **eventos de amistad** con escenas en la demo.

**Rutinas:** cada vecino tiene un horario por día y por clima. Ejemplo para Fer: 8:00 abre la herrería, 13:00 almuerza en la plaza, 18:00 cierra, 19:00 taberna, 22:00 casa.

**Decisiones de la Fase 7 (octubre 2026):** el almacén, la herrería, la clínica y la taberna tienen interior; el municipio, el estadio y la oficina de Ápice quedan cerrados hasta la Fase 8. Los negocios atienden solo cuando su dueño está trabajando. Cada vecino tiene 2 eventos de amistad (a los 3 y 6 corazones) **personales, sin trama**: las revelaciones de la historia se suman en la Fase 8. Un regalo y una charla que suma por día y por vecino.

---

## 6. La isla

### Zonas de la demo (mitad sur)
| Zona | Terrenos | Memos | Contenido |
|---|---|---|---|
| **Pueblo Puerto** | — | — | Tienda de Deny, herrería de Fer, clínica de Anni, taberna, plaza, municipio, estadio, muelle, oficina de Ápice, tablón |
| **Refugio** (casa del jugador) | — | Los tuyos | Casa, corrales, estanque, huerta, caja de envíos |
| **Ruta Pradera** | Pradera | Plumín, Zumbi | Primera ruta, recolección |
| **Bosque Susurro** | Bosque | Zumbi, Bostezo (noche) | Encuentro del inicial |
| **Pantano Pantufla** | Barro | Pantuflo | Hierbas raras |
| **Cueva de Zorak** | Cueva | Topín, Farolito (piso profundo) | Mina diaria, 5 pisos |
| **Colina Escarcha** | Nieve, Hielo | Copito | Pequeña zona fría |
| **Acantilados Tormenta** | Montaña, Corrientes de aire, Tormenta | Chispín, **Karman** (con tormenta) | Zona avanzada |

### Zonas bloqueadas (próxima actualización)
| Zona | Bloqueo dentro de la historia | Pistas en el pueblo |
|---|---|---|
| **Volcán Dormido** (Draken) | Un derrumbe tapa la entrada. Fer: *"Hace falta una herramienta que todavía no existe en la isla."* | Foto de Zorak joven con Draken. Recorte de diario: *"Avistan dragón negro sobre el Volcán Dormido"* |
| **Playa Helada** (Randy) | Muro de hielo. El barco rompehielos está *"en reparación, vuelve pronto"* | Historias de pescadores en la taberna. Cartel de recompensa con dibujo de Randy |

En la MemoBox, Draken y Randy aparecen con foto borrosa, descripción y la etiqueta **"Zona inaccesible"**.

---

## 7. Los Memos — 20 especies

**Stats base** (escala 1–10): **VEL** Velocidad · **ACE** Aceleración · **RES** Resistencia (energía) · **CAR** Carga (velocidad de la barra de habilidad)
**Movilidad:** Corre / Nada / Vuela / Excava. Permite tomar **atajos** en pistas que los tienen.

### Iniciales
| # | Memo | Tipo | Mov. | VEL | ACE | RES | CAR | Habilidad única | Evoluciona |
|---|---|---|---|---|---|---|---|---|---|
| 01 | **Tostín** | Fuego | Corre | 6 | 7 | 5 | 6 | **Estela de brasas:** deja fuego atrás; los rivales que lo siguen se frenan 2 s | Nv. 16 → Brasón |
| 02 | **Brasón** | Fuego/Roca | Corre | 8 | 7 | 7 | 7 | **Estela ardiente:** igual, más largo, y derrite hielo/nieve del tramo | — |
| 03 | **Brotito** | Planta | Corre | 5 | 5 | 7 | 7 | **Enredadera:** atrapa 2 s al rival que va adelante | Nv. 16 → Ramazón |
| 04 | **Ramazón** | Planta/Tierra | Corre | 7 | 5 | 9 | 8 | **Raíces profundas:** atrapa al rival de adelante 3 s y recupera energía | — |
| 05 | **Charquito** | Agua | Nada | 6 | 6 | 5 | 7 | **Salpicón:** convierte el tramo actual en Río por 6 s | Nv. 16 → Chapuzón |
| 06 | **Chapuzón** | Agua/Viento | Nada | 8 | 7 | 6 | 8 | **Ola:** convierte el tramo en Río y empuja hacia atrás a los rivales cercanos | — |

### Legendarios rebeldes
| # | Memo | Tipo | Mov. | VEL | ACE | RES | CAR | Habilidad única | Disponible |
|---|---|---|---|---|---|---|---|---|---|
| 07 | **Karman** | Eléctrico/Viento | Corre | 9 | 10 | 6 | 8 | **Trueno:** sprint instantáneo y aturde 1,5 s a todos los rivales cercanos | ✅ Demo |
| 08 | **Draken** | Fuego/Sombra | Vuela | 10 | 8 | 8 | 7 | **Llamarada negra:** convierte el tramo en Ceniza y frena a todos los que vienen atrás | 🔒 Actualización |
| 09 | **Randy** | Agua/Hielo | Nada | 9 | 7 | 9 | 6 | **Mar helado:** congela todo el tramo; él va más rápido sobre hielo | 🔒 Actualización |

**Cómo se doma cada uno**
- **Karman** ama la libertad. Solo aparece con tormenta en los Acantilados. Hay que ganarle, **dejarlo ir** y esperar: vuelve solo al refugio a los pocos días si lo trataste bien.
- **Draken** solo respeta la fuerza: hay que ganarle varias veces, en días distintos.
- **Randy** es orgulloso y burlón: hay que ganarle **en su terreno**, el mar congelado.

### Salvajes
| # | Memo | Tipo | Mov. | VEL | ACE | RES | CAR | Habilidad única | Dónde |
|---|---|---|---|---|---|---|---|---|---|
| 10 | **Plumín** | Viento | Vuela | 6 | 8 | 4 | 7 | **Ráfaga:** el próximo Memo de tu equipo que entre arranca a velocidad máxima | Ruta Pradera |
| 11 | **Topín** | Tierra | Excava | 5 | 4 | 8 | 6 | **Túnel:** desaparece 2 s y reaparece más adelante | Cueva |
| 12 | **Zumbi** | Bicho | Vuela | 6 | 7 | 4 | 8 | **Enjambre:** el rival más cercano corre en zigzag 3 s | Ruta Pradera, Bosque |
| 13 | **Chispín** | Eléctrico | Corre | 7 | 9 | 3 | 6 | **Descarga:** sprint enorme 3 s, después queda agotado | Acantilados |
| 14 | **Copito** | Hielo | Corre | 5 | 6 | 6 | 7 | **Escarcha:** convierte el tramo actual en Hielo por 6 s | Colina Escarcha |
| 15 | **Pantuflo** | Agua/Tierra | Nada | 4 | 5 | 9 | 6 | **Pantano:** convierte el tramo actual en Barro por 6 s | Pantano Pantufla |
| 16 | **Bostezo** | Sombra | Corre | 5 | 5 | 6 | 9 | **Siesta contagiosa:** el rival de adelante se duerme 1,5 s | Bosque (solo de noche) |
| 17 | **Farolito** | Luz | Vuela | 6 | 6 | 5 | 8 | **Destello:** encandila 1 s a todos los rivales | Cueva, piso 5 (raro) |

### Exclusivos de la Capitana Vera (no se pueden conseguir)
| # | Memo | Tipo | Mov. | VEL | ACE | RES | CAR | Habilidad única |
|---|---|---|---|---|---|---|---|---|
| 18 | **Tuerquita** | Metal | Corre | 6 | 6 | 7 | 7 | **Remiendo:** recupera 40% de energía al instante |
| 19 | **Ferrolobo** | Metal/Sombra | Corre | 8 | 7 | 7 | 6 | **Acecho:** copia la última habilidad que usó un rival |
| 20 | **Imanta** | Metal/Eléctrico | Corre | 7 | 7 | 7 | 7 | **Magnetismo:** atrae a los rivales cercanos y les roba velocidad 3 s |

### Tres arquetipos de habilidad
1. **Molestar al de atrás:** Estela de brasas, Llamarada negra
2. **Molestar al de adelante o a todos:** Enredadera, Enjambre, Siesta, Destello, Trueno, Magnetismo
3. **Cambiar el terreno:** Salpicón, Escarcha, Pantano, Ola, Mar helado

Además hay habilidades de **beneficio propio o del equipo**: Ráfaga, Túnel, Descarga, Remiendo, Acecho.

### Personalidades (temperamento)
Cada Memo capturado tiene una al azar. Modifica un poco las stats y su comportamiento en el refugio.

| Temperamento | En carrera | En el refugio |
|---|---|---|
| Impulsivo | +ACE, −RES | Corre por todos lados |
| Constante | Sin bonus ni penalidad, no baja de ritmo con poca energía | Rutinas ordenadas |
| Remontador | +VEL cuando va último | Juega a perseguir |
| Tímido | −ACE, +CAR | Se esconde, le cuesta hacer amigos |
| Mimoso | +bonus de confianza | Busca caricias, sigue al jugador |
| Dormilón | +RES, −ACE | Duerme siestas largas |
| Juguetón | +CAR | Inicia juegos con otros Memos |
| Orgulloso | +VEL, a veces ignora órdenes con confianza baja | Solitario |

### Memos brillantes ✨
Con probabilidad de 1 en 200, un Memo salvaje aparece con otra paleta de colores. Es solo estético y muy coleccionable.

---

## 8. Tipos, terrenos y efectividad

### 12 tipos
🌿 Planta · 🪨 Tierra · 💧 Agua · ❄️ Hielo · 🔥 Fuego · ⚡ Eléctrico · 🌪️ Viento · ⛰️ Roca · 🐛 Bicho · 🌑 Sombra · ✨ Luz · ⚙️ Metal

### 12 terrenos
Pradera · Bosque · Arena · Barro · Río · Hielo · Nieve · Ceniza · Montaña · Cueva · Tormenta · Corrientes de aire

### Multiplicadores
- ▲ **Muy eficaz:** ×1,30 de velocidad
- · **Normal:** ×1,00
- ▼ **Poco eficaz:** ×0,75

### Tabla de efectividad (tipo ↓ / terreno →)
| Tipo | Prad. | Bosq. | Arena | Barro | Río | Hielo | Nieve | Ceniza | Mont. | Cueva | Torm. | Aire |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Planta | ▲ | ▲ | ▼ | · | · | ▼ | ▼ | ▼ | · | · | · | · |
| Tierra | · | · | ▲ | ▲ | ▼ | ▼ | · | · | ▲ | · | · | ▼ |
| Agua | · | · | ▼ | ▲ | ▲ | · | · | ▲ | ▼ | · | · | · |
| Hielo | · | · | ▼ | · | ▲ | ▲ | ▲ | ▼ | · | · | · | · |
| Fuego | · | · | · | ▼ | ▼ | ▲ | ▲ | ▲ | · | · | ▼ | · |
| Eléctrico | · | · | ▼ | ▼ | · | · | · | · | · | ▲ | ▲ | · |
| Viento | ▲ | ▼ | · | · | · | · | · | · | ▲ | ▼ | · | ▲ |
| Roca | · | · | · | ▼ | ▼ | · | · | ▲ | ▲ | ▲ | · | ▼ |
| Bicho | ▲ | ▲ | · | ▲ | · | ▼ | ▼ | ▼ | · | · | · | · |
| Sombra | ▼ | ▲ | ▼ | · | · | · | · | · | · | ▲ | ▲ | · |
| Luz | ▲ | · | ▲ | ▼ | · | · | · | · | · | ▲ | ▼ | · |
| Metal | · | · | ▲ | ▼ | ▼ | ▲ | · | · | ▲ | · | ▼ | · |

### Regla para tipo doble
- Si **algún** tipo es ▲ y **ninguno** es ▼ → ▲
- Si **algún** tipo es ▼ y **ninguno** es ▲ → ▼
- Si hay uno ▲ y otro ▼, o los dos son normales → ·

### Modificadores extra
- **Noche:** los de tipo Sombra tienen +10% de velocidad. **Lluvia:** los de tipo Agua +10%.
- **Movilidad:** los que nadan, vuelan o excavan pueden tomar **atajos** marcados en algunas pistas (más cortos).

---

## 9. Carreras

### Controles (simples, como Monster Race)
- **Cambiar:** con **← →** elegís cualquiera de los 6 retratos y con **B** entra. Hay que esperar **8 s** entre cambio y cambio. La transición dura 0,5 s.
- **Habilidad:** la barra se carga sola según CAR mientras el Memo corre. Cuando está llena, **A** activa la habilidad del Memo que está corriendo. Si no tiene sentido usarla (por ejemplo, Enredadera sin nadie adelante), no se gasta.
- Todo lo demás es automático.

### Formatos
| Tipo de carrera | Memos | Tramos | Rivales |
|---|---|---|---|
| **Captura (salvaje)** | 1 vs 1 | 1 tramo largo, **un solo terreno: donde lo encontraste** | El salvaje |
| **Amistosa** (vecinos) | Hasta 3 | 2 o 3 | 1 |
| **Entrenador de ruta** | Hasta 6 | 3 o 4 | 1 |
| **Copa oficial** | **6 (equipo completo)** | **6** | 3 |

Las carreras de entrenadores y la Copa usan el **equipo de 6**. El jugador puede cambiar entre ellos en cualquier momento, respetando la espera.

### Pista
- Vista **lateral con scroll** (como Monster Race). Cada corredor va en su carril.
- La pista es una secuencia de **tramos**, cada uno con su terreno. Antes de largar se ve el **mapa completo** de tramos.
- Algunas pistas tienen **atajos** (agua, aire o túnel) que solo puede tomar un Memo con la movilidad adecuada.

### Energía
- Cada Memo tiene energía = RES × 10.
- Mientras corre gasta 1 por segundo (más con algunas habilidades).
- Con menos del 25% de energía, la velocidad baja a ×0,8. Con 0, a ×0,6.
- En el banco recupera 2 por segundo.
- Así rotar sale de forma natural, sin obligar.

### Fórmula de velocidad
```
velocidad = VEL_base(nivel)
          × efectividad(tipo, terreno)
          × factor_energía
          × factor_confianza   (0,90 Miedo … 1,10 Alma gemela)
          × factor_ánimo        (0,95 triste … 1,05 feliz)
          × bonus_equipo
          × efectos_activos     (habilidades propias y rivales)
```
La aceleración (ACE) define cuánto tarda en llegar a esa velocidad al entrar o tras un frenazo.

### Interfaz de carrera
- Abajo: **6 retratos** con barra de energía y una flecha según el terreno actual y el próximo: 🟢 ▲ · 🟡 · · 🔴 ▼.
- Arriba: minimapa de tramos con la posición de cada corredor.
- Barra de habilidad del Memo actual.
- Indicador de espera para cambiar.

### Bonus
- **Equipo completo:** si los 6 Memos corrieron al menos una vez → +50% de premio y experiencia.
- **Impulso de amistad:** si cambiás entre dos Memos que son **mejores amigos**, el que entra arranca a velocidad máxima (ver sección 12).

### Desobediencia (confianza baja, etapa rebelde o legendario Hostil)
Con cierta probabilidad, el Memo:
- **ignora el cambio** (sigue corriendo 2 s más),
- **usa su habilidad cuando quiere**,
- o **se frena a propósito** un momento.

La probabilidad baja a medida que sube la confianza.

### IA de los rivales
- Cambia de Memo cuando el próximo tramo le conviene a otro o cuando la energía baja del 30%.
- Usa la habilidad apenas la tiene lista, o (IA de jefe) cuando le conviene: por ejemplo, Enredadera solo si tiene un rival adelante.

### Recompensas
Experiencia para todos los Memos que corrieron, dinero, objetos, y en la Copa una medalla y el avance de la historia.

---

## 10. Captura

1. Caminando por zonas con terreno (pasto alto, barro, nieve…) hay **encuentros aleatorios**, como en Pokémon. Algunos Memos raros se ven en el mapa.
2. Empieza una **carrera 1 vs 1** en una pista de **un solo terreno**: el del lugar del encuentro. El jugador elige qué Memo de su equipo corre.
3. **Ganás o lo cansás** (si su energía llega a 0 antes del final, se rinde).
4. **Momento de comida:** elegís qué darle. Favorita: 90% de que acepte · comida básica: 60% · nada: se va. (Hasta la Fase 6 la comida no se gasta del inventario.)
5. El Memo **te sigue al refugio** con la confianza inicial según cómo lo hayas conseguido:
   - Salvaje normal: **Desconfianza**
   - Rescatado de un collar: **Miedo**
   - Legendario: **Hostil**
6. Si perdés, el Memo se va y puede volver a aparecer.

**Memos con collar:** aparecen en las rutas con brillo violeta y ojos sin vida. Corren más rápido de lo normal (+10%). Ganarles **rompe el collar**, y lo que sigue es una escena corta y triste en la que el Memo tiembla, desorientado.

**Capacidad:** 6 en el equipo y el resto en el refugio (en la demo, sin límite práctico).

---

## 11. Confianza y vínculo

### Niveles
| Nivel | Puntos | Qué hace el Memo | En carrera |
|---|---|---|---|
| 😠 **Hostil** (solo legendarios) | <0 | Gruñe, no se deja tocar, se va del refugio de día | ×0,90 y desobediencia alta |
| 😨 **Miedo** | 0–99 | Se esconde bajo la cama o en un rincón. No come si lo mirás. Tiembla | ×0,90 y desobediencia media |
| 😟 **Desconfianza** | 100–249 | Te observa de lejos. Come si te alejás | ×0,95 y desobediencia baja |
| 😐 **Neutral** | 250–449 | Anda tranquilo por la casa. A veces se deja acariciar | ×1,00 |
| 🙂 **Confía** | 450–699 | Se te acerca solo y te sigue por el refugio | ×1,03 |
| 😊 **Amigo** | 700–999 | Te espera en la puerta cuando volvés. Te trae "regalos" (piedritas, flores) | ×1,06 |
| 💞 **Alma gemela** | 1000+ | Duerme al lado de tu cama y te despierta a la mañana | ×1,10 y desbloquea evolución por confianza |

### Cómo sube la confianza (por día)
| Acción | Puntos | Límite |
|---|---|---|
| Darle de comer | +5 (favorita +15) | 2 por día |
| Acariciar | +5 | 1 por día |
| Bañar | +8 | 1 cada 3 días |
| Jugar (pelota, etc.) | +6 | 1 por día |
| Ponerle un accesorio que le gusta | +10 | 1 vez por accesorio |
| Correr juntos una carrera | +3 (ganando +6) | — |
| Que te siga en el mundo | +2 por hora de juego | — |
| Tiempo en el refugio | +1 por día | — |
| Tener un "padrino" (ver sección 12) | +3 por día | — |

- Los Memos con **Miedo** solo aceptan comida y "estar cerca" (quedarse quieto a su lado). Las caricias fallan al principio: retroceden.
- **Descuido:** si pasan varios días sin atenderlo, baja el ánimo (no la confianza). Nunca se pierde un Memo.

### Comidas favoritas
Cada especie tiene 1 o 2 favoritas, que se descubren probando. Quedan anotadas en la MemoBox.

### Diario de vínculo (recuerdos)
- Cada Memo tiene una página en la **MemoBox** con **recuerdos** que se desbloquean al subir de nivel de confianza.
- Los rescatados **recuperan recuerdos de su vida anterior**.
- Tu inicial recupera recuerdos del abuelo. El primero: el abuelo cantándole para dormir (suena la cajita de música).

### Primer momento especial
El primer día que un Memo rescatado **sale de abajo de la cama por su cuenta** se dispara una escena breve: animación, la música de la cajita y un recuerdo nuevo.

---

## 12. El refugio vivo

### Los Memos tienen vida propia
- **Todos tus Memos viven en el refugio** (no solo los que no están en el equipo). El equipo son los 6 que corren carreras; el **compañero** es el que te sigue por la isla.
- **Entran y salen de la casa** cuando quieren (por la puerta y con animación).
- **Rutinas según personalidad y especie:** Bostezo duerme de día y sale de noche, Plumín se sube al techo, Charquito y Pantuflo se meten en el estanque, Topín cava pozos en la huerta (¡y a veces encuentra objetos!).
- **Necesidades** (de 0 a 100, bajan con el tiempo): Hambre · Sueño · Juego · Compañía.
- Las cumplen solos (comedero, camas, juguetes, otros Memos) o con tu ayuda.
- **Ánimo** = promedio de necesidades + eventos del día. Se ve con las burbujas de emoción.

### Burbujas de emoción (estilo GBA)
❤️ feliz · 💢 enojado · 💧 triste · ❗ sorprendido · ❓ curioso · 💤 dormido · 🎵 contento · 😰 asustado

### IA del Memo (máquina de estados con utilidad)
Estados: Deambular · Comer · Dormir · Jugar solo · Jugar con otro · Seguir al jugador · Esconderse · Saludar en la puerta · Trabajar · Bañarse en el estanque.
Cada cierto tiempo, el Memo elige la acción con mayor puntaje según sus necesidades, personalidad, confianza, hora del día y Memos cercanos.

### Vínculos entre Memos
- Cada par tiene una relación: **Desconocidos → Conocidos → Amigos → Mejores amigos**.
- Sube cuando juegan juntos, duermen cerca o comen juntos. Las personalidades compatibles suben más rápido (Juguetón + Mimoso sí; Orgulloso + Tímido cuesta).
- **Mejores amigos:**
  - Duermen acurrucados, se persiguen y se bañan juntos.
  - Escena especial cuando se vuelven mejores amigos.
  - **En carrera:** si cambiás de uno al otro, el que entra recibe el **impulso de amistad** y arranca a velocidad máxima.
- **Padrino:** podés asignar un Memo con confianza Amigo o más a un Memo recién rescatado. El nuevo gana +3 de confianza por día y le pierde el miedo más rápido.
  → Escena: tu inicial se acerca al Memo asustado escondido bajo la cama y se acurruca a su lado.

### Los Memos trabajan (si están contentos)
| Tipo | Trabajo |
|---|---|
| 🌿 Planta | Los cultivos crecen más rápido |
| 💧 Agua | Riega la huerta |
| 🪨 Tierra | Ara la tierra |
| 🔥 Fuego | Enciende la fundición (funde más rápido) |
| ⚡ Eléctrico | Activa máquinas (procesadora de frutas) |
| Excavadores | Encuentran objetos cavando |

Un Memo trabaja si su ánimo es feliz y su confianza es Neutral o más. Trabajar le da un poco de experiencia.

### El refugio se mejora
Al principio es una casa con polvo, sin camas ni comedero. Se mejora con dinero y materiales: camas, comederos, juguetes, estanque, corral, casitas individuales y decoración. La mejora del refugio **también es progreso visible**.

### Tu Memo te sigue
El Memo que elijas como "compañero" camina detrás tuyo por la isla, reacciona a los lugares (burbujas), saluda a los vecinos y a veces encuentra objetos.

---

## 13. Niveles, evolución y etapa rebelde

- **Experiencia** por carreras (todos los que corrieron), por trabajo en el refugio y por capturas.
- **Nivel máximo en la demo:** 30.
- **Las stats crecen con el nivel:** `stat = base × (1 + nivel × 0,04)`.
- **Evolución:**
  - **Por nivel:** los iniciales en el nivel 16.
  - **Por confianza:** algunas especies del juego completo evolucionan en Alma gemela.
  - Escena de evolución con destello, a la Pokémon. El jugador puede cancelarla.
- **Etapa rebelde:** al evolucionar, el Memo pierde **un nivel de confianza** (sin bajar de Neutral) y tiene más probabilidad de desobedecer por 7 días. Si lo cuidás, vuelve más fuerte el vínculo: recupera el nivel perdido con un recuerdo nuevo ("crecimos juntos").

---

## 14. Equipamiento

Cada Memo tiene **2 espacios**: **Equipo** y **Amuleto**. Se craftean en la herrería de Fer o se compran.

### Equipo (afecta terrenos o stats)
| Objeto | Efecto | Receta |
|---|---|---|
| Herraduras | Barro y Montaña cuentan como uno más favorable | 2 hierro |
| Aletas | Río ▲ para cualquier tipo | 2 cobre + 1 alga |
| Botas de clavos | Hielo y Nieve sin penalidad | 2 hierro + 1 cuero |
| Alas de planeo | Puede usar atajos de aire | 3 pluma + 1 tela |
| Pesas livianas | +1 VEL, −1 RES | 2 hierro |
| Mochila de agua | +2 RES | 1 cuero + 1 calabaza vacía |

### Amuletos (efectos especiales)
| Objeto | Efecto |
|---|---|
| Amuleto de relevo | Entra a velocidad máxima al cambiar |
| Piedra de carga | La barra de habilidad empieza llena |
| Cascabel de calma | Inmune a habilidades que molestan (1 vez por carrera) |
| Moño de amistad | +50% de confianza ganada en carreras |
| Gema de terreno (×12) | El terreno elegido cuenta como ▲ |

### Accesorios estéticos (aparte de los 2 espacios)
Gorritos, bufandas, moños y anteojos. Se ven en el sprite del mundo y del refugio y suben la confianza la primera vez (si le gusta).

---

## 15. Vida en la isla

### 🌱 Granja (simple)
- Arar con la azada → plantar → regar → cosechar. Sin estaciones en la demo (siempre primavera).

| Cultivo | Días | Venta | Uso |
|---|---|---|---|
| Nabo | 3 | 30 | Comida básica |
| Zanahoria | 4 | 45 | Comida básica, favorita de Topín |
| Frutilla | 6 | 80 | Postre, favorita de Brotito y Plumín |
| Zapallo | 8 | 120 | Comida grande, favorita de Pantuflo |
| **Bayamemo** | 5 | 60 | **Comida favorita de casi todos los Memos.** Clave para capturar |

### ⛏️ Cueva de Zorak (mina diaria)
- **5 pisos** en la demo. Las rocas **se regeneran cada día**.
- Se pica con el pico (sin energía: cada roca se pica una vez por día; el pico mejorado abre minerales más duros).
- Materiales: Piedra, Cobre, Hierro, Cuarzo; gemas raras: Amatista, Topacio, Esmeralda.
- Escaleras para bajar de piso. En el piso 5 está **Farolito** (raro) y unas ruinas.
- Un Memo excavador en el equipo encuentra más materiales.

### 🔨 Fundición y crafteo
- **Fundición** (en el refugio, se construye): mineral → lingote. Funciona más rápido con un Memo de fuego.
- **Mesa de trabajo:** unas 15 recetas en la demo (comidas para Memos, equipo, amuletos, muebles, juguetes, mejoras del refugio).

### 🧺 Recolección
Hierbas, frutos, plumas, algas, conchas y flores. Aparecen en el mapa y se renuevan cada pocos días.

### 🛒 Almacén de Deny
- **Semillas** (algunas cambian según el día de la semana)
- **Comida para Memos** (básica y premium)
- **Accesorios estéticos y muebles**
- **Juguetes:** pelota, peluche, rascador
- Compra tus productos: cosechas, minerales y crafteos

### ⚒️ Herrería de Fer
Mejoras de herramientas (azada, regadera, pico: cobre → hierro), equipo para carreras y fundición de minerales por un costo.

### 🏥 Clínica de Anni
Curar Memos heridos (después de rescates), consejos de cuidado y venta de medicinas y jabón para el baño.

### 📋 Tablón de pedidos
Pedidos diarios de vecinos ("Deny necesita 5 nabos", "Jojo quiere ver un Zumbi de cerca"). Dan dinero y amistad.

### 📦 Caja de envíos
Lo que se deja en la caja se vende al final del día.

### Economía (balance inicial orientativo)
- Día 1: 500 monedas.
- Ingreso esperado: día 5 entre 300 y 600 por día; día 20 entre 1500 y 2500 por día.
- Premio de la Copa: 5000.
- Carrera de entrenador de ruta: 200 a 600.

---

## 16. Ciclo de día y tiempo

- **El reloj del juego es el reloj real del jugador**, como en Pokémon Oro/Plata: si en tu casa son las 4:00, en la isla también es de madrugada. El día de la semana también es el real.
- **Fases del día:** Noche (20:00–5:00) · Amanecer (5:00–7:00) · Día (7:00–17:00) · Atardecer (17:00–20:00). Cambian la luz, y más adelante los Memos que aparecen (Bostezo solo de noche), las rutinas de los vecinos y los horarios de las tiendas.
- **Modo de prueba** (solo en el editor o en builds de desarrollo): F5 / F6 atrasan o adelantan una hora y F7 vuelve a la hora real.
- **Los días del juego son los días reales**, como en Animal Crossing: los cultivos crecen por días reales regados (si un día no se riega, se pausa; nunca se muere), la mina y la recolección se renuevan cada día real, la caja de envíos paga al día siguiente y los cuidados tienen límites por día real.
- **Sin energía del jugador ni desmayos:** podés hacer todo lo que quieras; lo que pone el ritmo es el día real (decisión de octubre 2026).
- **Clima:** uno por día real, igual todo el día. En la Fase 8B se suma la **tormenta** (≈1 de cada 12 días, llueve igual, relámpagos; es cuando aparece Karman). En la Fase 7 hay **sol y lluvia** (≈1 de cada 4 días; la lluvia riega sola la huerta y los vecinos cambian su rutina). Nublado y **tormenta** (aparece Karman) se suman más adelante. F8 fuerza el clima en modo de prueba.
- **Días de la semana:** la Copa y las carreras oficiales son los **sábados**.
- **Iluminación:** amanecer, día, atardecer y noche, con luces 2D en ventanas y faroles.

---

## 17. Creación de personaje

- **Nombre** del personaje.
- **Pronombre:** él / ella / elle. Es **independiente de la apariencia**: cualquier combinación es válida.
- **Apariencia:** tono de piel, peinado y color de pelo, ojos, anteojos (varios), barba o bigote (opcional), ropa superior, ropa inferior y colores.
- **Nombre del refugio** (opcional, ej. "Refugio Lunita").
- Los diálogos usan el pronombre elegido mediante etiquetas en los textos: `{nombre}`, `{refugio}` y opciones por pronombre como `{o/a/e}` o `{nieto/nieta/niete}` (él / ella / elle).
- **Fase 8A (octubre 2026):** versión básica de la apariencia (piel, peinado, color de pelo, remera y pantalón); anteojos y barba quedan para el pulido. La bufanda roja del abuelo es fija.

---

## 18. Arte

### Estilo: inspirado en GBA, pero moderno
| Elemento | Especificación |
|---|---|
| Resolución de referencia | 480×270 escalado entero (Pixel Perfect Camera) |
| Pixels por unidad (PPU) | 16 |
| Tiles del mundo | 16×16 |
| Personajes en el mundo | 16×32, caminar en 4 direcciones, 4 cuadros |
| Memos en el mundo/refugio | 16×16 o 24×24, caminar e idle |
| Memos en carreras y menús | **64×64**, correr (4–6 cuadros), idle y habilidad |
| Retratos de vecinos | 48×48 para el diálogo |
| Iluminación | Luces 2D de URP para el ciclo de día y noche |

### Diseño de los Memos
- **Tiernos:** formas redondas, cabeza grande, ojos enormes, 2 o 3 colores principales y un rasgo único (la cola de Tostín es una llamita, Pantuflo tiene forma de pantufla, Bostezo tiene ojos medio cerrados).
- **Cool (Karman, Draken, Randy):** siluetas angulosas, colores oscuros con **detalles que brillan**, poses imponentes.
- **Con collar:** colores desaturados, ojos sin brillo y un collar violeta con brillo pulsante.

### Arte provisorio (fase de desarrollo)
- Pixel art **generado por código** a partir de **sprites definidos como grillas de texto** (un carácter por color de la paleta). Así es fácil de editar, versionar y reemplazar.
- Una **herramienta de editor** convierte las grillas en PNG con la configuración de importación correcta (Point filter, sin compresión, PPU 16).
- Tiene que **verse bonito**: paleta coherente, contornos oscuros, sombreado simple de 2 tonos, sin degradados.

**Paleta base: Sweetie 16** (ampliable a 32 colores con tonos intermedios)
```
#1a1c2c  #5d275d  #b13e53  #ef7d57  #ffcd75  #a7f070  #38b764  #257179
#29366f  #3b5dc9  #41a6f6  #73eff7  #f4f4f4  #94b0c2  #566c86  #333c57
```
Violeta de los collares (color reservado): `#9b3cff` con brillo `#d8a6ff`.

### Arte final (más adelante)
Packs de pixel art estilo GBA (itch.io) para el mundo y encargo o dibujo propio de los Memos. El código **no depende** del arte: los sprites se reemplazan sin tocar la lógica.

---

## 19. Audio

- **Música** estilo chiptune moderno o GBA:
  - Tema del pueblo
  - Tema del refugio (tranquilo)
  - Tema de las rutas
  - Tema de carrera
  - Tema de la Copa
  - Tema de Ápice (inquietante)
  - **Cajita de música** (leitmotiv emotivo)
- **Efectos:** pasos por superficie, burbujas de emoción, sonidos únicos de cada Memo (un "grito" corto como en Pokémon), herramientas, caja de envíos, largada y llegada de carrera.
- En la demo, sonidos provisorios o gratuitos.

---

## 20. Alcance de la demo (checklist)

- [ ] Prólogo, creación de personaje y elección de inicial (diario con siluetas)
- [ ] Pueblo Puerto, Refugio y 6 zonas jugables
- [ ] 20 Memos en datos: 17 conseguibles en la demo (3 iniciales + 3 evoluciones + 8 salvajes + Karman), 3 exclusivos de Vera, y Draken y Randy bloqueados con pistas
- [ ] Carreras: captura 1v1, amistosas, de entrenador y Copa de 6 tramos
- [ ] Confianza, refugio vivo, vínculos entre Memos y padrinos
- [ ] Niveles, evolución de iniciales y etapa rebelde
- [ ] Equipamiento: 6 equipos, 5 amuletos y 10 accesorios
- [ ] Granja (5 cultivos), mina (5 pisos), fundición y unas 15 recetas
- [ ] Tienda, herrería, clínica, tablón y caja de envíos
- [ ] 11 personajes con rutinas, 9 vecinos con amistad y 2 o 3 eventos cada uno
- [ ] Historia del Capítulo 1 completa, con el final y el gancho
- [ ] MemoBox con diario de vínculo
- [ ] Guardado y carga
- [ ] Ciclo de día y noche, clima básico

**Duración estimada de la demo:** 4 a 6 horas.

---

## 21. Arquitectura técnica

### Proyecto
- Unity 6 · URP 2D (Renderer 2D + Light 2D) · Input System · Tilemap + Tilemap Extras · 2D Animation · Pixel Perfect Camera.

### Estructura de carpetas
```
Assets/_MemosIsland/
  Art/            (Generated/ arte provisorio, Final/ arte definitivo)
  Audio/
  Data/           (ScriptableObjects: Memos, Tipos, Terrenos, Objetos, Recetas, NPCs, Diálogos)
  Prefabs/
  Scenes/         (Boot, Title, CharacterCreation, Island, Race, …)
  Scripts/
    Core/         (GameManager, SaveSystem, TimeManager, EventBus)
    World/        (GridMovement, Interactables, Transitions, Weather)
    Memos/        (MemoSpecies, MemoInstance, TrustSystem, Needs, MemoAI, Relationships)
    Race/         (RaceManager, Track, Segment, Runner, AbilitySystem, RaceAI, RaceUI)
    Farm/         (Crops, Tools, Mine, Smelter, Crafting, Shop, Shipping)
    NPC/          (Schedules, Dialogue, Friendship, Quests)
    Story/        (StoryFlags, Cutscenes)
    UI/
    Editor/       (PixelArtGenerator, herramientas de datos)
  Tests/
```

### Principios
- **Datos en ScriptableObjects**: especies, tipos, terrenos, tabla de efectividad, habilidades, objetos, recetas, cultivos, NPCs y diálogos. Agregar un Memo no requiere tocar código.
- **Estado del juego serializable** (clases planas → JSON) desde el principio, para el guardado.
- **Habilidades** como datos + efectos componibles (frenar, aturdir, cambiar terreno, sprint, recuperar energía…).
- **Carreras deterministas** dada una semilla, para poder testearlas.
- **Eventos** (EventBus) para desacoplar sistemas: "Memo subió de confianza", "Día terminó", etc.
- **Textos** en tablas (Unity Localization o una tabla propia), con etiquetas de pronombre.
- **Código en inglés**, comentarios breves, textos del juego en español.
- **Tests** (EditMode) para la lógica pura: efectividad, fórmula de velocidad, confianza, experiencia y economía.

---

## 22. Hoja de ruta (fases)

Cada fase termina con algo **jugable y probado**. El arte es provisorio hasta la fase 9.

| Fase | Objetivo | Listo cuando… |
|---|---|---|
| **0. Base** | Estructura de carpetas, Pixel Perfect Camera, Input System, **generador de pixel art desde grillas**, paleta, escena de prueba | Se ve un tile y un personaje generados en pantalla, nítidos |
| **1. Mundo** | Movimiento por casillas en 4 direcciones, tilemaps, colisiones, interacción (botón A), transiciones entre mapas, cámara, reloj de día y noche | Se camina por un mapa de prueba con hora y luz cambiando |
| **2. Datos de Memos** | ScriptableObjects de tipos, terrenos, efectividad, 20 especies, habilidades y personalidades; MemoBox básica; tests de efectividad | La MemoBox muestra los 20 con sus datos |
| **3. Carreras** | Pista por tramos, corredores, energía, cambio con espera, habilidades, IA rival, interfaz, formatos (1v1 y 6 tramos), captura | Se puede correr y capturar un Memo, y es divertido |
| **4. Vínculo y refugio** | Confianza, necesidades, IA de vida, emociones, entrar y salir de la casa, relaciones entre Memos, padrino, compañero que te sigue | El refugio "se siente vivo" |
| **5. Progreso** | Experiencia, niveles, evolución con escena, etapa rebelde, legendario Hostil, equipamiento | Un inicial evoluciona y se le pone equipo |
| **6. Vida en la isla** | Inventario, dinero, granja, mina, fundición, crafteo, tienda, herrería, caja de envíos | Un ciclo de días completo genera dinero |
| **7. Vecinos** | Rutinas, diálogos, amistad, regalos, tablón de pedidos, eventos | Los vecinos se mueven y responden según la hora |
| **8. Historia** | Prólogo, creación de personaje, diario, elección, escenas, Karman, Copa, final, pistas de Draken y Randy | La demo se juega de principio a fin |
| **9. Pulido** | Guardado completo, menús, audio, arte mejorado, balance, partículas | Demo lista para que la prueben otras personas |

---

## 23. Contenido para futuras actualizaciones
- **Volcán Dormido** (Draken) y **Playa Helada** (Randy)
- Mitad norte de la isla y Capítulo 2: rescatar a los otros dos iniciales de manos de Ápice
- Evoluciones de los Memos salvajes y nuevas especies
- Estaciones, festivales y eventos de temporada
- Romance o amistades profundas con vecinos (a definir)
- Misterio: ¿el abuelo está vivo? ¿Qué es la figura con la bufanda?
