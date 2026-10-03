using System.Collections.Generic;
using MemosIsland.Memos;
using MemosIsland.UI;
using UnityEngine;

namespace MemosIsland.Race
{
    /// <summary>
    /// Interfaz de la carrera (GDD §9), en pixels de interfaz (x ±120, y ±67.5):
    /// arriba el minimapa y la habilidad; abajo los 6 retratos con energía y flechas de efectividad
    /// para el tramo actual y el siguiente; en el medio, carteles y mensajes.
    /// </summary>
    public class RaceHud
    {
        const float Ppu = PixelFont.PixelsPerUnit;
        const int Order = 900; // debajo de los diálogos (1000+)
        const int SlotWidth = 38, SlotHeight = 34, SlotsLeft = -114, SlotsBottom = -66;
        const int MapLeft = -84, MapWidth = 196, MapY = 63;

        static readonly Color Dark = new Color32(0x33, 0x3c, 0x57, 0xff);
                static readonly Color Red = new Color32(0xb1, 0x3e, 0x53, 0xff);
        static readonly Color Green = new Color32(0x38, 0xb7, 0x64, 0xff);
        static readonly Color Gray = new Color32(0x56, 0x6c, 0x86, 0xff);
        static readonly Color Gold = new Color32(0xff, 0xcd, 0x75, 0xff);
        static readonly Color ActiveTint = new Color32(0xff, 0xf1, 0xc9, 0xff);
        static readonly Color[] MarkerColors =
        {
            new Color32(0xb1, 0x3e, 0x53, 0xff), new Color32(0x3b, 0x5d, 0xc9, 0xff),
            new Color32(0x38, 0xb7, 0x64, 0xff), new Color32(0x5d, 0x27, 0x5d, 0xff),
        };

        readonly RaceSimulation _sim;
        readonly PixelFont _font;
        readonly Sprite _box, _pixel;
        readonly Material _material;
        readonly GameObject _root;

        readonly List<SpriteRenderer> _markers = new();
        PixelText _position, _abilityName, _abilityReady, _switchInfo, _toast, _panelText;
        SpriteRenderer _chargeFill, _panel, _cursor;
        float _toastTime;

        class Slot
        {
            public GameObject root;
            public SpriteRenderer box, portrait, energy;
            public PixelText now, next;
        }
        readonly List<Slot> _slots = new();

        public RaceHud(Transform uiRoot, RaceSimulation sim, PixelFont font, Sprite box, Sprite pixel, Material material)
        {
            _sim = sim;
            _font = font;
            _box = box;
            _pixel = pixel;
            _material = material;
            _root = new GameObject("Race HUD");
            _root.transform.SetParent(uiRoot, false);
            Build();
        }

        public void Destroy() => Object.Destroy(_root);

        // ------------------------------------------------------------------ Armado

        void Build()
        {
            var t = _root.transform;

            // Minimapa: tramos de colores, marcas de corredores y meta.
            var mapBack = Box(t, -116, 68, 234, 32);
            mapBack.sortingOrder = Order - 1;
            _position = Text(t, -112, 64, Dark);
            foreach (var seg in _sim.Track.Segments)
            {
                float x0 = MapLeft + seg.start / _sim.Track.Length * MapWidth;
                float w = seg.length / _sim.Track.Length * MapWidth;
                var r = Pixel(t, x0, MapY - 2, Mathf.Max(1f, w - 1f), 4, seg.terrain.color);
                r.sortingOrder = Order + 1;
            }
            for (int i = 0; i < _sim.Racers.Count; i++)
                _markers.Add(Pixel(t, 0, MapY + 1, 2, 8, MarkerColors[i % MarkerColors.Length]));

            // Habilidad y cambio
            _abilityName = Text(t, -112, 54, Dark);
            Pixel(t, -112, 43, 50, 4, new Color32(0x94, 0xb0, 0xc2, 0xff)).sortingOrder = Order;
            _chargeFill = Pixel(t, -112, 43, 50, 4, Gold);
            _abilityReady = Text(t, -58, 47, Red);
            _switchInfo = Text(t, 36, 54, Dark);

            // Retratos
            var team = _sim.Player.memos;
            for (int i = 0; i < GameStateSlots; i++)
            {
                float x0 = SlotsLeft + i * SlotWidth;
                var slotRoot = new GameObject($"Slot{i}");
                slotRoot.transform.SetParent(t, false);
                var slot = new Slot { root = slotRoot };
                slot.box = Box(slotRoot.transform, x0, SlotsBottom + SlotHeight, SlotWidth - 2, SlotHeight);
                slot.portrait = Renderer(slotRoot.transform, null, Order + 2);
                slot.portrait.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                slot.portrait.transform.localPosition = P(x0 + 17, SlotsBottom + 6);
                Pixel(slotRoot.transform, x0 + 4, SlotsBottom + 6, 28, 3, Dark).sortingOrder = Order + 3;
                slot.energy = Pixel(slotRoot.transform, x0 + 4, SlotsBottom + 6, 28, 3, Green);
                slot.energy.sortingOrder = Order + 4;
                slot.now = Text(slotRoot.transform, x0 + 22, SlotsBottom + SlotHeight - 4, Dark, false);
                slot.next = Text(slotRoot.transform, x0 + 28, SlotsBottom + SlotHeight - 4, Dark, false);
                slotRoot.SetActive(i < team.Count);
                _slots.Add(slot);
            }
            _cursor = Renderer(t, null, Order + 6);
            var cursorText = Text(t, 0, 0, Red, false);
            cursorText.SetText("▼");
            cursorText.transform.SetParent(_cursor.transform, false);
            cursorText.transform.localPosition = Vector3.zero;

            // Mensajes y panel central
            _toast = Text(t, 0, 30, Dark, true, Order + 30);
            _panel = Box(t, -80, 36, 160, 70);
            _panel.sortingOrder = Order + 20;
            _panelText = Text(t, -72, 30, Dark, true, Order + 30);
            SetPanel(null);
        }

        const int GameStateSlots = 6;

        // ------------------------------------------------------------------ Actualización

        public void Refresh(int cursorIndex)
        {
            var player = _sim.Player;
            int rank = _sim.IsOver ? player.rank : 1 + CountAhead(player);
            _position.SetText($"{rank}º/{_sim.Racers.Count}");

            for (int i = 0; i < _sim.Racers.Count; i++)
            {
                var r = _sim.Racers[i];
                float x = MapLeft + Mathf.Clamp01(r.position / _sim.Track.Length) * MapWidth;
                Place(_markers[i], x - 1, MapY + 3, 2, 8);
                _markers[i].sortingOrder = Order + (r.isPlayer ? 4 : 3);
            }

            var active = player.Active;
            _abilityName.SetText(active.species.ability != null ? active.species.ability.displayName : "");
            Place(_chargeFill, -112, 43, Mathf.Max(0.01f, active.charge * 50f), 4);
            _abilityReady.SetText(active.AbilityReady && Time.time % 0.6f < 0.4f ? "¡A!" : "");

            if (player.memos.Count < 2) _switchInfo.SetText("");
            else if (player.pendingSwitch >= 0) _switchInfo.SetText("¡No quiere salir!");
            else if (player.switchCooldown > 0f) _switchInfo.SetText($"Cambio en {Mathf.CeilToInt(player.switchCooldown)}");
            else _switchInfo.SetText("B: cambiar");

            var terrainNow = _sim.Track.TerrainAt(player.position);
            var terrainNext = _sim.Track.NextTerrain(player.position);
            for (int i = 0; i < player.memos.Count && i < _slots.Count; i++)
            {
                var m = player.memos[i];
                var slot = _slots[i];
                var frames = m.instance.RaceFrames;
                slot.portrait.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
                slot.box.color = i == player.activeIndex ? ActiveTint : Color.white;
                float ratio = Mathf.Clamp01(m.EnergyRatio);
                Place(slot.energy, SlotsLeft + i * SlotWidth + 4, SlotsBottom + 6, Mathf.Max(0.01f, ratio * 28f), 3);
                slot.energy.color = ratio < 0.25f ? Red : ratio < 0.5f ? Gold : Green;
                SetArrow(slot.now, _sim.EffectivenessFor(m, terrainNow));
                SetArrow(slot.next, _sim.EffectivenessFor(m, terrainNext));
            }
            if (player.memos.Count > 1)
            {
                _cursor.gameObject.SetActive(true);
                _cursor.transform.localPosition = P(SlotsLeft + cursorIndex * SlotWidth + 15, SlotsBottom + SlotHeight + 8);
            }
            else
            {
                _cursor.gameObject.SetActive(false);
            }

            if (_toastTime > 0f)
            {
                _toastTime -= Time.deltaTime;
                if (_toastTime <= 0f) _toast.SetText("");
            }
        }

        int CountAhead(Racer of)
        {
            int n = 0;
            foreach (var r in _sim.Racers)
                if (r != of && (r.finished && !r.gaveUp && !of.finished || r.position > of.position)) n++;
            return n;
        }

        static void SetArrow(PixelText t, Effectiveness e)
        {
            switch (e)
            {
                case Effectiveness.Strong: t.SetColor(Green); t.SetText("▲"); break;
                case Effectiveness.Weak: t.SetColor(Red); t.SetText("▼"); break;
                default: t.SetColor(Gray); t.SetText("·"); break;
            }
        }

        public void ShowToast(string text)
        {
            _toast.SetText(text);
            int w = _font.MeasureWidth(text);
            _toast.transform.localPosition = P(-w / 2f, 30);
            _toastTime = 2.2f;
        }

        /// <summary>Cartel central (texto con varios renglones); null lo oculta.</summary>
        public void SetPanel(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (show)
            {
                _toast.SetText("");
                _toastTime = 0f;
            }
            _panel.gameObject.SetActive(show);
            _panelText.gameObject.SetActive(show);
            if (!show) return;
            var lines = _font.Wrap(text, 196);
            int width = 0;
            foreach (var l in lines) width = Mathf.Max(width, _font.MeasureWidth(l));
            width += 20;
            int height = lines.Count * _font.lineHeight + 12;
            float left = -width / 2f, top = 6 + height / 2f;
            _panel.size = new Vector2(width / Ppu, height / Ppu);
            _panel.transform.localPosition = P(0, top - height / 2f);
            _panelText.transform.localPosition = P(left + 10, top - 5);
            _panelText.SetText(string.Join("\n", lines));
        }

        // ------------------------------------------------------------------ Ayudas

        static Vector3 P(float x, float y) => new(x / Ppu, y / Ppu, 0f);

        SpriteRenderer Renderer(Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject("r");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            if (_material != null) r.sharedMaterial = _material;
            return r;
        }

        SpriteRenderer Box(Transform parent, float left, float top, float w, float h)
        {
            var r = Renderer(parent, _box, Order);
            r.drawMode = SpriteDrawMode.Sliced;
            r.size = new Vector2(w / Ppu, h / Ppu);
            r.transform.localPosition = P(left + w / 2f, top - h / 2f);
            return r;
        }

        SpriteRenderer Pixel(Transform parent, float left, float top, float w, float h, Color color)
        {
            var r = Renderer(parent, _pixel, Order + 1);
            r.color = color;
            Place(r, left, top, w, h);
            return r;
        }

        void Place(SpriteRenderer r, float left, float top, float w, float h)
        {
            float px = _pixel.rect.width;
            r.transform.localScale = new Vector3(w / px, h / px, 1f);
            r.transform.localPosition = P(left + w / 2f, top - h / 2f);
        }

        PixelText Text(Transform parent, float x, float y, Color color, bool shadow = true, int order = Order + 10)
        {
            var go = new GameObject("t");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = P(x, y);
            var t = go.AddComponent<PixelText>();
            t.Setup(_font, _material, order, color, shadow);
            return t;
        }
    }
}
