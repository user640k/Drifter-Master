// The menu: a plain list of options in three pages (CAR, SETUP, GAME), opened with Esc, which also pauses the car.
// Up / Down pick a row, Left / Right change it, Q / E (or Tab) change page, Esc closes. A gamepad's d-pad, shoulder
// buttons and Start do the same. The game supplies the rows; this only shows them and passes the changes back.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public partial class Menu : Control {
    public class Row { public string Label; public Func<string> Value; public Action<int> Change; public string Note = ""; }
    public readonly List<(string title, List<Row> rows)> Pages = new();
    int _page, _row; ColorRect _shade; Label _tabs, _note; VBoxContainer _list; readonly List<(Label name, Label val)> _lines = new();
    static readonly Color Dim = new Color(0.72f, 0.74f, 0.78f), Hi = new Color(1f, 0.83f, 0.25f);

    public override void _Ready() {
      SetAnchorsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore;
      _shade = new ColorRect { Color = new Color(0.02f, 0.02f, 0.03f, 0.8f) }; AddChild(_shade);
      var box = new VBoxContainer { Position = new Vector2(70, 56), CustomMinimumSize = new Vector2(540, 0) }; box.AddThemeConstantOverride("separation", 4); AddChild(box);
      Label L(string t, int size, Color c) { var l = new Label { Text = t }; l.AddThemeFontSizeOverride("font_size", size); l.AddThemeColorOverride("font_color", c); return l; }
      box.AddChild(L("DRIFT MASTER", 40, Colors.White));
      _tabs = L("", 20, Dim); box.AddChild(_tabs); box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 12) });
      _list = new VBoxContainer(); _list.AddThemeConstantOverride("separation", 1); box.AddChild(_list);
      box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 12) });
      _note = L("", 16, Dim); _note.AutowrapMode = TextServer.AutowrapMode.WordSmart; _note.CustomMinimumSize = new Vector2(540, 48); box.AddChild(_note);
      box.AddChild(L("UP / DOWN  choose     LEFT / RIGHT  change     Q / E  page     ESC  back to the track", 14, Dim));
      Refresh();
    }

    // it sits on a canvas layer, which has no size of its own: cover the window, whatever its size
    public override void _Process(double delta) { if (Visible) { Size = GetViewportRect().Size; _shade.Size = new Vector2(Size.X * (Preview ? 0.5f : 1f), Size.Y); } }
    /// the page that is open, and whether the right half of the screen is left clear for a look at the car
    public string Page => Pages.Count > 0 ? Pages[_page].title : ""; public bool Preview => Page == "CAR";

    public void Open(bool on) { Visible = on; if (on) Refresh(); }
    public void Goto(int page, int row) { _page = page % Pages.Count; _row = row; Refresh(); } // (test shots)

    public void Refresh() {
      if (_list == null || Pages.Count == 0) return;
      var rows = Pages[_page].rows; _row = Math.Clamp(_row, 0, rows.Count - 1);
      string tabs = ""; for (int i = 0; i < Pages.Count; i++) tabs += (i == _page ? "[ " + Pages[i].title + " ]" : "  " + Pages[i].title + "  ") + "   "; _tabs.Text = tabs;
      while (_lines.Count < rows.Count) {
        var h = new HBoxContainer(); var n = new Label { CustomMinimumSize = new Vector2(280, 0) }; var v = new Label();
        foreach (var l in new[] { n, v }) l.AddThemeFontSizeOverride("font_size", 21); h.AddChild(n); h.AddChild(v); _list.AddChild(h); _lines.Add((n, v));
      }
      for (int i = 0; i < _lines.Count; i++) {
        var (n, v) = _lines[i]; bool used = i < rows.Count; ((Control)n.GetParent()).Visible = used; if (!used) continue;
        bool sel = i == _row; n.Text = (sel ? "> " : "   ") + rows[i].Label; v.Text = rows[i].Value == null ? "" : (sel && rows[i].Change != null ? "<  " + rows[i].Value() + "  >" : rows[i].Value()); // (rows with nothing to change, as on the KEYS page, show no arrows)
        n.AddThemeColorOverride("font_color", sel ? Hi : Colors.White); v.AddThemeColorOverride("font_color", sel ? Hi : Dim);
      }
      _note.Text = rows[_row].Note;
    }

    /// a key or button while the menu is open; true if it was one of ours
    public bool Handle(InputEvent e) {
      bool key(Key k) => e is InputEventKey ek && ek.Pressed && ek.Keycode == k;
      bool pad(JoyButton b) => e is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == b;
      var rows = Pages[_page].rows; int n = rows.Count;
      if (key(Key.Up) || key(Key.W) || pad(JoyButton.DpadUp)) _row = (_row + n - 1) % n;
      else if (key(Key.Down) || key(Key.S) || pad(JoyButton.DpadDown)) _row = (_row + 1) % n;
      else if (key(Key.Left) || key(Key.A) || pad(JoyButton.DpadLeft)) rows[_row].Change?.Invoke(-1);
      else if (key(Key.Right) || key(Key.D) || key(Key.Enter) || key(Key.Space) || pad(JoyButton.DpadRight) || pad(JoyButton.A)) rows[_row].Change?.Invoke(1);
      else if (key(Key.E) || key(Key.Tab) || pad(JoyButton.RightShoulder)) { _page = (_page + 1) % Pages.Count; _row = 0; }
      else if (key(Key.Q) || pad(JoyButton.LeftShoulder)) { _page = (_page + Pages.Count - 1) % Pages.Count; _row = 0; }
      else return false;
      Refresh(); return true;
    }
  }
}
