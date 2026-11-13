// Solo training: free laps and judged runs against a ghost of your own best. The ghost is a see-through copy of the car
// replaying a recording: your fastest lap in free practice (from the line), your highest-scoring run in a judged run (from
// the start). Recordings are kept per track between sessions. Also the course marks on the floor, and the parking bay.
// The judging itself is in Judging.cs.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public partial class Main {
    Course _course; RunJudge _jP; double _count, _cardT;
    int _comp; /* 0 free practice, 1 judged run */ int _phase; /* 0 counting down, 1 running, 2 the score card is up */ Label _card;
    string _lastResult = "-"; bool _parked; Vector3 _bay; float _bayHd; public string CompTest;
    // ---- the ghost
    const int GF = 10; const double GDt = 1.0 / 60;   // each frame: x, z, y, heading, steer, roll, pitch, rear spin, front spin, lap index
    sealed class Rec { public readonly List<float> F = new(); public double Mark; public int Body; public int Count => F.Count / GF; }   // Mark: the lap time (s) or the run's score
    Rec _ghostLap, _ghostRun, _rec; bool _recOn, _ghostOn = true, _ghostPlay; double _recAcc, _ghostT; Track _t2; CarPhysics _ghostCar; CarView _ghostView; int _ghostBody = -1;
    string GhostFile(bool run) => $"user://ghost_{Tracks[TrackIdx % Tracks.Length].file}_{(run ? "run" : "lap")}.bin";
    void SaveGhost(Rec r, bool run) { if (_shot != null && CompTest == null) return; using var f = FileAccess.Open(GhostFile(run), FileAccess.ModeFlags.Write); if (f == null) return; f.Store32(1); f.StoreDouble(r.Mark); f.Store32((uint)r.Body); f.Store32((uint)r.F.Count); foreach (var v in r.F) f.StoreFloat(v); }
    Rec LoadGhost(bool run) { if (!FileAccess.FileExists(GhostFile(run))) return null; using var f = FileAccess.Open(GhostFile(run), FileAccess.ModeFlags.Read); if (f == null || f.Get32() != 1) return null;
      var r = new Rec { Mark = f.GetDouble(), Body = (int)f.Get32() }; uint n = f.Get32(); if (n == 0 || n % GF != 0 || n > 2000000) return null; for (uint i = 0; i < n; i++) r.F.Add(f.GetFloat()); return r; }
    void RecStart() { _rec = new Rec { Body = _body }; _recOn = true; _recAcc = GDt; }
    void RecStep(double dt) { if (!_recOn) return; _recAcc += dt; if (_recAcc < GDt) return; _recAcc -= GDt; if (_rec.Count > 60 * 180) { _recOn = false; return; }   // (three minutes: not a lap)
      _rec.F.AddRange(new[] { (float)_car.X, (float)_car.Z, (float)_car.Y, (float)_car.Hd, (float)_car.Steer, (float)_car.Roll, (float)_car.Pitch, (float)_car.Spin, (float)_car.SpinF, (float)Math.Max(0, _t.Idx) }); }
    void GhostStart() { _ghostT = 0; _ghostPlay = true; }
    void StepGhost(double delta) {
      var g = _comp == 1 ? _ghostRun : _ghostLap; bool show = _ghostOn && _ghostPlay && g != null && g.Count > 2 && !_parked && !(_comp == 1 && _phase == 0);
      if (show && _phase != 2 && !_pause.Visible) _ghostT += Math.Min(delta, 0.05); double ft = _ghostT / GDt; int i = (int)ft;
      if (show && i >= g.Count - 1) { show = false; _ghostPlay = false; }
      if (!show) { if (_ghostView != null) _ghostView.Visible = false; return; }
      if (_ghostView == null || _ghostBody != g.Body) { _ghostView?.QueueFree(); _ghostBody = g.Body; _ghostView = new CarView { Body = g.Body, Ghost = true }; AddChild(_ghostView); }
      if (_ghostCar == null) { var td = Tracks[TrackIdx % Tracks.Length]; _t2 = Track.FromJson(FileAccess.GetFileAsString("res://assets/" + td.file + ".json"), td.z0, 4.5, 2.5); _ghostCar = new CarPhysics { T = _t2 }; }
      float f = (float)(ft - i); int a = i * GF, b = a + GF; float L(int k) => g.F[a + k] + (g.F[b + k] - g.F[a + k]) * f; float A(int k) => g.F[a + k] + Mathf.AngleDifference(g.F[a + k], g.F[b + k]) * f;
      _ghostCar.X = L(0); _ghostCar.Z = L(1); _ghostCar.Y = L(2); _ghostCar.Hd = A(3); _ghostCar.Steer = L(4); _ghostCar.Roll = L(5); _ghostCar.Pitch = L(6); _ghostCar.Spin = L(7); _ghostCar.SpinF = L(8); _t2.Idx = (int)g.F[a + 9]; _ghostCar.Air = false;
      _ghostView.Visible = true; _ghostView.Pose(_ghostCar, _t2);
    }
    /// the car has crossed the line in free practice: a finished lap that beats the ghost becomes the ghost, and a new recording starts
    void LapCrossed(bool finished, double time) {
      if (_comp != 0) return;
      if (finished && _recOn && _rec != null && _rec.Count > 30 && (_ghostLap == null || time < _ghostLap.Mark)) { _rec.Mark = time; _ghostLap = _rec; SaveGhost(_rec, false); Say("NEW GHOST  " + T(time)); }
      RecStart(); GhostStart();
    }
    void ClearGhosts() { _ghostLap = _ghostRun = null; foreach (bool run in new[] { false, true }) if (FileAccess.FileExists(GhostFile(run))) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GhostFile(run))); Say("GHOSTS CLEARED"); }

    void BuildCompetition() {
      _course = Course.Build(_t);
      if (!_bare) {
        // course marks: a cone at each inner clipping point, a striped board along each outer zone
        var cone = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.38f, 0.02f), Roughness = 0.6f }; var white = new StandardMaterial3D { AlbedoColor = new Color(0.95f, 0.95f, 0.92f), Roughness = 0.6f };
        var zone = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.85f, 0.05f), Roughness = 0.5f, EmissionEnabled = true, Emission = new Color(1f, 0.85f, 0.05f), EmissionEnergyMultiplier = 0.25f };
        foreach (var m in _course.Marks) {
          if (!m.Outer) { var p = new Vector3((float)m.X, (float)m.Y, (float)m.Z);
            AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.004f, BottomRadius = 0.017f, Height = 0.05f, RadialSegments = 12 }, MaterialOverride = cone, Position = p + new Vector3(0, 0.027f, 0) });
            AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.0105f, BottomRadius = 0.0125f, Height = 0.009f, RadialSegments = 12 }, MaterialOverride = white, Position = p + new Vector3(0, 0.032f, 0) });
            AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.04f, 0.003f, 0.04f) }, MaterialOverride = cone, Position = p + new Vector3(0, 0.0015f, 0) }); }
          else for (int i = m.I0; i < m.I1; i += 2) { int a = ((i % _t.N) + _t.N) % _t.N; if ((i - m.I0) / 2 % 2 == 1) continue;   // dashes on the floor, a hand's width in from the outside barrier
              var p = new Vector3((float)(_t.X[a] - _t.NX[a] * m.Side * (_t.Hw - 0.07)), (float)_t.E[a] + 0.0012f, (float)(_t.Z[a] - _t.NZ[a] * m.Side * (_t.Hw - 0.07)));
              AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.05f, 0.0012f, (float)_t.Ds * 2.05f) }, MaterialOverride = zone, Position = p, Rotation = new Vector3(0, (float)Math.Atan2(_t.TX[a], _t.TZ[a]), 0), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }); }
        }
      }
      // the parking bay: the clear patch of floor nearest the driver's stand, off the lane and away from the walls
      double best = 1e9; _bay = new Vector3(0, 0, (float)_t.HallZ - 0.5f);
      for (double x = -_t.HallX + 0.45; x <= _t.HallX - 0.45; x += 0.1) for (double z = -_t.HallZ + 0.45; z <= _t.HallZ - 0.45; z += 0.1) {
          double dmin = 1e9; for (int i = 0; i < _t.N; i += 2) { double dx = x - _t.X[i], dz = z - _t.Z[i]; dmin = Math.Min(dmin, dx * dx + dz * dz); }
          if (Math.Sqrt(dmin) < _t.Hw + 0.42) continue; double cost = Math.Abs(x) * 0.6 + (_t.HallZ - z); if (cost < best) { best = cost; _bay = new Vector3((float)x, 0, (float)z); } }
      _bayHd = Mathf.Pi / 2;
      if (!_bare && best < 1e8) { var paint = new StandardMaterial3D { AlbedoColor = new Color(0.92f, 0.92f, 0.88f), Roughness = 0.7f }; float w = 0.56f, d = 0.3f;
        foreach (var (sz, off) in new[] { (new Vector3(w, 0.001f, 0.012f), new Vector3(0, 0, d / 2)), (new Vector3(w, 0.001f, 0.012f), new Vector3(0, 0, -d / 2)), (new Vector3(0.012f, 0.001f, d), new Vector3(-w / 2, 0, 0)) })
          AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = sz }, MaterialOverride = paint, Position = _bay + off + new Vector3(0, 0.001f, 0), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        var lb = new Label3D { Text = "P", FontSize = 96, PixelSize = 0.0016f, Modulate = new Color(0.92f, 0.92f, 0.88f), Position = _bay + new Vector3(0, 0.002f, 0), RotationDegrees = new Vector3(-90, 0, 0), Shaded = false }; AddChild(lb); }
    }
    /// take the car off the track into the bay (it stays there until it is reset to the start line)
    void Park() { if (_comp != 0) StopComp(); _recOn = false; _parked = true; _car.T.Idx = -1; _car.Place(_bay.X, _bay.Z, _bayHd); _car.Y = 0; _car.Air = true; /* (drawn at floor height, wherever the nearest piece of track is) */ _timing = false; _lap = 0; _camSnap = true; Say("PARKED   (R: back to the start line)"); }

    void Grid() {
      _parked = false; _jP = new RunJudge(_course, _t); _car.Place(_t.Start[0], _t.Start[1], _t.Start[2]);
      _phase = 0; _count = 3.2; _timing = false; _lap = 0; _camSnap = true; _card.Visible = false; _recOn = false; _ghostPlay = false;
    }
    void StartSolo() { _comp = 1; Grid(); }
    void StopComp() { _comp = 0; _phase = 0; _recOn = false; _ghostPlay = false; if (_card != null) _card.Visible = false; }

    /// one physics step of the car
    void StepWorld(in CarInput inp) {
      if (_parked) return; bool go = _comp == 0 || _phase == 1;
      _car.Step(CarPhysics.DT, inp, go); if (go) RecStep(CarPhysics.DT);
      if (_comp == 0 || _phase != 1) return;
      _jP.Step(CarPhysics.DT, _car, Math.Max(0, _t.Idx));
      if (_jP.Done || _jP.Spun || _jP.Time > 90) EndRun();
    }
    static string Line(string who, RunJudge.Card c) => $"{who,-6}  LINE {c.Line,4:0.0}/35   ANGLE {c.Angle,4:0.0}/30   STYLE {c.Style,4:0.0}/25   SPEED {c.Speed,4:0.0}/10   DEDUCT -{c.Deduct:0}   =  {c.Total:0.0}";
    void EndRun() {
      _phase = 2; _cardT = 0; _recOn = false; var c = _jP.Solo(); bool best = !_jP.Spun && _rec != null && _rec.Count > 30 && (_ghostRun == null || c.Total > _ghostRun.Mark + 0.05); double was = _ghostRun?.Mark ?? -1;
      if (best) { _rec.Mark = c.Total; _ghostRun = _rec; SaveGhost(_rec, true); }
      string s = "JUDGED RUN\n\n" + Line("YOU", c) + (c.Notes != "" ? "\n" + c.Notes : "") + $"\n\naverage angle {_jP.AvgAngleDeg:0}°   clips {_jP.ClipsHit}/{_jP.Clip.Length}   {_jP.Time:0.0} s\n\n"
        + (best ? (was >= 0 ? $"YOUR BEST RUN: IT IS THE NEW GHOST  (was {was:0.0})" : "THIS RUN IS NOW YOUR GHOST") : (_ghostRun != null ? $"ghost: your best run, {_ghostRun.Mark:0.0}" : "")) + "\n\nENTER  run again        ESC  menu";
      _lastResult = $"JUDGED RUN {c.Total:0.0}"; _card.Text = s; _card.Visible = true; if (CompTest != null) GD.Print("CARD\n" + s);
    }
    void NextRun() { Grid(); }
    void StepCompetition(double delta) {
      StepGhost(delta);
      if (_comp == 0) return;
      if (_phase == 0) { _count -= delta; if (_count <= 0) { _phase = 1; RecStart(); GhostStart(); } }
      if (_phase == 2) { _cardT += delta; if (CompTest == "solo2" && _cardT > 0.5) { CompTest = "solo"; NextRun(); } }
    }
    string CompHud() {
      if (_parked) return "\nPARKED";
      if (_comp == 0) return _ghostOn && _ghostLap != null ? $"\nGHOST  {T(_ghostLap.Mark)}" : "";
      if (_phase == 0) return $"\nJUDGED RUN\n{(_count > 0.4 ? Math.Ceiling(_count - 0.2).ToString() : "GO")}";
      return $"\nJUDGED RUN\nCLIPS {_jP.ClipsHit}/{_jP.Clip.Length}   ANGLE {Math.Abs(_car.Beta) * 180 / Math.PI:0}°" + (_jP.Taps > 0 ? $"   TAPS {_jP.Taps}" : "") + (_ghostOn && _ghostRun != null ? $"\nGHOST  {_ghostRun.Mark:0.0}" : "");
    }
  }
}
