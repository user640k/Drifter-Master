// The drift counter: a small, quiet read-out in the top right corner, and a call-out when a drift gets big.
//   points build while the car is sideways (angle x speed); a change of direction without straightening up is a LINK,
//   and each link raises the multiplier; the run ends a moment after the car straightens, or at once on a hard knock.
// The read-out is plain lettering in milky white with a soft shadow: no box, no frame. A call-out (NICE, GREAT, INSANE,
// MONSTER DRIFT) is vibrant pink and a little see-through, stays one second, then breaks up into dust.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public sealed partial class DriftCounter : Control {
    public static readonly (double at, string name)[] Tiers = { (350, "NICE DRIFT"), (900, "GREAT DRIFT"), (1600, "INSANE DRIFT"), (2400, "MONSTER DRIFT") };
    const double Rate = 2.0, MinAngle = 10, MinSpeed = 0.8, EndAfter = 1.2;
    public double Score, Mult = 1, Best; public int Links, Tier = -1;
    /// the best run on this track and the points banked in all (kept by the game between sessions); a run that ends is handed to OnBank, a call-out to OnTier
    public double Record, Total; public Action<double> OnBank; public Action<int> OnTier;
    double _angle, _off, _shown, _fade, _linkT = 9, _callT = 9; int _lastSign, _callTier = -1; bool _on;
    Font _font; readonly List<(Vector2 p, Vector2 v, float life, float size)> _dust = new(); readonly Random _rnd = new(7);
    static readonly Color Milk = new(0.96f, 0.95f, 0.91f), Pink = new(1f, 0.16f, 0.62f), Shade = new(0, 0, 0, 0.55f);

    public override void _Ready() { _font = GD.Load<Font>("res://assets/fonts/Saira-ExtraBoldItalic.ttf"); SetAnchorsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore; }

    /// a hard knock: the run is lost
    public void Knock() { if (Score > 0) { OnBank?.Invoke(0); Score = 0; Mult = 1; Links = 0; Tier = -1; _lastSign = 0; _on = false; } }

    /// once a frame: the car's drift angle (degrees, signed), its speed (m/s), and whether it is being driven (not parked, in the air or paused)
    public void Tick(double dt, double angleDeg, double speed, bool live) {
      _angle = Math.Abs(angleDeg); _linkT += dt; _callT += dt;
      bool on = live && _angle > MinAngle && speed > MinSpeed;
      if (on) { int sg = angleDeg > 0 ? 1 : -1; if (_lastSign != 0 && sg != _lastSign) { Links++; Mult = 1 + 0.5 * Links; _linkT = 0; } _lastSign = sg; _off = 0; Score += _angle * speed * Rate * Mult * dt; if (Score > Best) Best = Score; }
      else if (live) { _off += dt; if (_off > EndAfter && Score > 0) { Total += Score; if (Score > Record) Record = Score; OnBank?.Invoke(Score); Score = 0; Mult = 1; Links = 0; Tier = -1; _lastSign = 0; } }
      _on = on;
      for (int k = Tiers.Length - 1; k > Tier; k--) if (Score >= Tiers[k].at) { Tier = k; _callTier = k; _callT = 0; _dust.Clear(); OnTier?.Invoke(k); break; }
      if (Score > 0) { _shown = Score; _fade = Math.Min(1, _fade + dt * 6); } else _fade = Math.Max(0, _fade - dt * 1.8);
      for (int i = _dust.Count - 1; i >= 0; i--) { var d = _dust[i]; d.life -= (float)dt; if (d.life <= 0) { _dust.RemoveAt(i); continue; } d.v += new Vector2(0, 260) * (float)dt; d.v *= 1 - 1.6f * (float)dt; d.p += d.v * (float)dt; _dust[i] = d; }
      QueueRedraw();
    }

    void Text(Vector2 at, string s, int size, Color col, HorizontalAlignment al = HorizontalAlignment.Right, float width = 400) {
      float w = _font.GetStringSize(s, HorizontalAlignment.Left, -1, size).X; var p = al == HorizontalAlignment.Right ? at - new Vector2(w, 0) : al == HorizontalAlignment.Center ? at - new Vector2(w / 2, 0) : at;   // (placed by its own measured width)
      var sh = new Color(0, 0, 0, Shade.A * col.A); DrawStringOutline(_font, p + new Vector2(1, 2), s, HorizontalAlignment.Left, -1, size, 5, new Color(0, 0, 0, 0.16f * col.A)); DrawString(_font, p + new Vector2(2, 3), s, HorizontalAlignment.Left, -1, size, sh); DrawString(_font, p, s, HorizontalAlignment.Left, -1, size, col);
    }

    public override void _Draw() {
      if (_font == null) return; var sz = GetViewportRect().Size; float a = (float)_fade;
      if (a > 0.01f) { // the read-out, top right: small lettering, nothing round it
        float x = sz.X - 30, y = 44; var milk = new Color(Milk, a * (_on ? 1f : 0.6f));
        Text(new Vector2(x, y), "DRIFT", 15, new Color(Milk, a * 0.75f)); Text(new Vector2(x, y + 36), $"{_shown:#,0}", 34, milk);
        Text(new Vector2(x, y + 58), $"{_angle:0}°", 15, new Color(Milk, a * 0.85f));
        if (Record > 0) Text(new Vector2(x, y + (Links > 0 ? 124 : 80)), $"BEST {Math.Max(Record, Score):#,0}", 13, new Color(Milk, a * 0.6f));
        if (Links > 0) { float pop = 1 + 0.35f * Mathf.Max(0, 1 - (float)_linkT / 0.3f); Text(new Vector2(x, y + 84), $"LINK {Links}   x{Mult:0.0}", (int)(17 * pop), new Color(Milk, a * 0.95f));
          for (int k = 0; k < Math.Min(Links, 8); k++) { float lx = x - 20 - k * 17, ly = y + 96; var c = new Color(k % 2 == 0 ? Milk : Pink, a * 0.9f); DrawArc(new Vector2(lx, ly + 5), 5.5f, 0, Mathf.Tau, 14, new Color(0, 0, 0, 0.4f * a), 4.5f, true); DrawArc(new Vector2(lx, ly + 4), 5.5f, 0, Mathf.Tau, 14, c, 2.4f, true); } } }
      if (_callTier >= 0 && _callT < 1.0) { // the call-out: one second, vibrant pink, a little see-through
        bool big = _callTier == Tiers.Length - 1; int fs = big ? 58 : 38; float inT = Mathf.Min(1, (float)_callT / 0.12f), sc = 0.8f + 0.2f * inT; var at = new Vector2(sz.X / 2, sz.Y * 0.27f);
        Text(at, Tiers[_callTier].name, (int)(fs * sc), new Color(Pink, 0.78f * inT), HorizontalAlignment.Center, 1000); }
      else if (_callTier >= 0 && _callT >= 1.0 && _dust.Count == 0 && _callT < 1.1) { // then it breaks up: dust thrown from where the lettering was
        bool big = _callTier == Tiers.Length - 1; int fs = big ? 58 : 38; float w = _font.GetStringSize(Tiers[_callTier].name, HorizontalAlignment.Left, -1, fs).X, h = fs * 0.72f; var c0 = new Vector2(sz.X / 2, sz.Y * 0.27f - h * 0.45f);
        int count = big ? 240 : 150; for (int i = 0; i < count; i++) { var p = c0 + new Vector2(((float)_rnd.NextDouble() - 0.5f) * w, ((float)_rnd.NextDouble() - 0.5f) * h);
          var v = new Vector2(((float)_rnd.NextDouble() - 0.5f) * 260 + (p.X - c0.X) * 0.5f, -40 - (float)_rnd.NextDouble() * 150); _dust.Add((p, v, 0.45f + (float)_rnd.NextDouble() * 0.5f, 1.5f + (float)_rnd.NextDouble() * 2.8f)); }
        _callTier = -1; }
      foreach (var d in _dust) { float k = Mathf.Min(1, d.life / 0.5f); DrawRect(new Rect2(d.p, new Vector2(d.size, d.size)), new Color(Pink, 0.75f * k)); }
    }
  }
}
