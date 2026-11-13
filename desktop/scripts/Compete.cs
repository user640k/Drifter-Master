// Competition: a judged solo qualifying run, and tandem battles against a second car driven by the game. Two runs,
// swapping the lead and the chase; the totals decide it, and a near tie is run again ("one more time"). Also the course
// marks on the floor, and the parking bay. The judging itself is in Judging.cs.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public partial class Main {
    Course _course; LineCar _line; int _pace = 1; static readonly (string name, double k)[] Paces = { ("EASY", 0.85), ("NORMAL", 1.0), ("FAST", 1.12) }; Track _t2; CarPhysics _rival; CarView _rivalView; RunJudge _jP, _jR; double[] _leadBeta; double _contactT, _count, _cardT;
    int _comp; /* 0 free practice, 1 qualifying, 2 tandem */ int _phase; /* 0 counting down, 1 running, 2 the score card is up */ int _runNo = 1, _omt; Label _card;
    RunJudge.Card _pLead, _pChase, _rLead, _rChase; string _lastResult = "-"; bool _parked; Vector3 _bay; float _bayHd; public string CompTest;
    bool PlayerLeads => _comp != 2 || _runNo == 1;

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
    void Park() { if (_comp != 0) StopComp(); _parked = true; _car.T.Idx = -1; _car.Place(_bay.X, _bay.Z, _bayHd); _car.Y = 0; _car.Air = true; /* (drawn at floor height, wherever the nearest piece of track is) */ _timing = false; _lap = 0; _camSnap = true; Say("PARKED   (R: back to the start line)"); }

    void EnsureRival() {
      if (_rival != null) return; var td = Tracks[TrackIdx % Tracks.Length];
      _t2 = Track.FromJson(FileAccess.GetFileAsString("res://assets/" + td.file + ".json"), td.z0, 4.5, 2.5); _t2.Name = td.name; _t2.Boards = td.boards;
      _rival = new CarPhysics { T = _t2 }; _line = new LineCar(_t2, _course);
      int rb = (_body + 3) % CarView.Bodies.Length; _rivalView = new CarView { Body = rb }; AddChild(_rivalView);
      int rp = _paintIdx == 1 ? 10 : 1; _rivalView.SetPaint(Paints[rp].col, true); _rivalView.SetRims(Rims[4].col, Rims[4].metal); _rivalView.SetCamber(-3, -3);
      var bd = CarView.Bodies[rb]; _rival.SetBody(bd.kg, bd.cgUp, _rivalView.HalfW, _rivalView.HalfL);
    }
    void Grid() {
      _parked = false; int n = _t.N, s0 = _t.NearestT(_t.Start[0], _t.Start[1], -1), back = ((s0 - (int)Math.Round(0.55 / _t.Ds)) % n + n) % n;
      _jP = new RunJudge(_course, _t); _jR = null; _car.Place(_t.Start[0], _t.Start[1], _t.Start[2]);
      if (_comp == 2) { EnsureRival(); _rivalView.Visible = true; _jR = new RunJudge(_course, _t2); _leadBeta = new double[n]; Array.Fill(_leadBeta, double.NaN); _contactT = 0;
        _line.Pace = Paces[_pace].k; if (PlayerLeads) _line.Place(_rival, back);
        else { _line.Place(_rival, s0); _car.Place(_t.X[back], _t.Z[back], Math.Atan2(_t.TX[back], _t.TZ[back])); } }
      _phase = 0; _count = 3.2; _timing = false; _lap = 0; _camSnap = true; _card.Visible = false;
    }
    void StartSolo() { _comp = 1; _runNo = 1; Grid(); }
    void StartTandem() { _comp = 2; _runNo = 1; _omt = 0; Grid(); }
    void StopComp() { _comp = 0; _phase = 0; if (_card != null) _card.Visible = false; if (_rivalView != null) _rivalView.Visible = false; }

    /// one physics step of everything on the track
    void StepWorld(in CarInput inp) {
      if (_parked) return; bool go = _comp == 0 || _phase == 1;
      _car.Step(CarPhysics.DT, inp, go);
      if (_comp == 0 || _phase != 1) { if (_comp == 2 && _rival != null) _line.Step(_rival, CarPhysics.DT, false); return; }   // (the rival rolls to a stop when the run is over)
      _jP.Step(CarPhysics.DT, _car, Math.Max(0, _t.Idx));
      if (_comp == 2) {
        _line.Step(_rival, CarPhysics.DT, true); /* its own line at its own pace, leading or chasing: it does not wait for the player or steer round them */ _jR.Step(CarPhysics.DT, _rival, Math.Max(0, _t2.Idx));
        if (PlayerLeads) { _leadBeta[Math.Max(0, _t.Idx)] = _car.Beta; _jR.Chase(CarPhysics.DT, _rival, _t2.Idx, _car, _t.Idx, _leadBeta, ref _contactT); }
        else { _leadBeta[Math.Max(0, _t2.Idx)] = _rival.Beta; _jP.Chase(CarPhysics.DT, _car, _t.Idx, _rival, _t2.Idx, _leadBeta, ref _contactT); }
      }
      var lead = PlayerLeads ? _jP : _jR; var chase = _comp == 2 ? (PlayerLeads ? _jR : _jP) : null;
      if (lead.Done || lead.Spun || (chase != null && chase.Spun && chase == _jP) || lead.Time > 90) EndRun();
    }
    static string Line(string who, RunJudge.Card c, bool chase) => chase
      ? $"{who,-14}  PROXIMITY {c.Line,4:0.0}/40   MIRROR {c.Angle,4:0.0}/30   TIMING {c.Style,4:0.0}/20   LINE {c.Speed,4:0.0}/10   DEDUCT -{c.Deduct:0}   =  {c.Total:0.0}"
      : $"{who,-14}  LINE {c.Line,4:0.0}/35   ANGLE {c.Angle,4:0.0}/30   STYLE {c.Style,4:0.0}/25   SPEED {c.Speed,4:0.0}/10   DEDUCT -{c.Deduct:0}   =  {c.Total:0.0}";
    void EndRun() {
      _phase = 2; _cardT = 0; string s;
      if (_comp == 1) { var c = _jP.Solo(); s = "QUALIFYING RUN\n\n" + Line("YOU", c, false) + (c.Notes != "" ? "\n" + c.Notes : "") + $"\n\naverage angle {_jP.AvgAngleDeg:0}°   clips {_jP.ClipsHit}/{_jP.Clip.Length}   {_jP.Time:0.0} s\n\nENTER  run again        ESC  menu";
        _lastResult = $"QUALIFYING {c.Total:0.0}"; }
      else if (_runNo == 1) { _pLead = _jP.Solo(); _rChase = _jR.ChaseCard();
        s = "TANDEM  RUN 1 OF 2: YOU LED\n\n" + Line("YOU (LEAD)", _pLead, false) + (_pLead.Notes != "" ? "\n" + _pLead.Notes : "") + "\n" + Line("RIVAL (CHASE)", _rChase, true) + (_rChase.Notes != "" ? "\n" + _rChase.Notes : "") + "\n\nENTER  run 2: you chase"; }
      else { _rLead = _jR.Solo(); _pChase = _jP.ChaseCard(); double you = _pLead.Total + _pChase.Total, riv = _rLead.Total + _rChase.Total; bool tie = Math.Abs(you - riv) < 3;
        s = "TANDEM  RUN 2 OF 2: YOU CHASED\n\n" + Line("RIVAL (LEAD)", _rLead, false) + (_rLead.Notes != "" ? "\n" + _rLead.Notes : "") + "\n" + Line("YOU (CHASE)", _pChase, true) + (_pChase.Notes != "" ? "\n" + _pChase.Notes : "")
          + $"\n\nYOU {you:0.0}     RIVAL {riv:0.0}\n\n" + (tie ? "THE JUDGES CANNOT SPLIT IT:  ONE MORE TIME!\n\nENTER  run it again" : (you > riv ? "YOU WIN THE BATTLE" : "THE RIVAL WINS THE BATTLE") + "\n\nENTER  new battle        ESC  menu");
        _lastResult = tie ? "TANDEM: ONE MORE TIME" : $"TANDEM {(you > riv ? "WON" : "LOST")} {you:0.0} : {riv:0.0}"; if (tie) _omt++; else _omt = -1; }
      _card.Text = s; _card.Visible = true; if (CompTest != null) GD.Print("CARD\n" + s);
    }
    void NextRun() {
      if (_comp == 1) { Grid(); return; }
      if (_runNo == 1) { _runNo = 2; Grid(); } else { _runNo = 1; if (_omt < 0) _omt = 0; Grid(); }
    }
    void StepCompetition(double delta) {
      if (_rivalView != null && _rivalView.Visible) _rivalView.Pose(_rival, _t2);
      if (_comp == 0) return;
      if (_phase == 0) { _count -= delta; if (_count <= 0) _phase = 1; }
      if (_phase == 2) { _cardT += delta; if (CompTest != null && _cardT > 0.5) { if (_comp == 2 && _runNo == 1) NextRun(); else if (_shot == null) GetTree().Quit(); } }
    }
    string CompHud() {
      if (_parked) return "\nPARKED";
      if (_comp == 0) return "";
      string name = _comp == 1 ? "QUALIFYING" : (_omt > 0 ? "ONE MORE TIME  " : "TANDEM  ") + (PlayerLeads ? "RUN 1: YOU LEAD" : "RUN 2: YOU CHASE");
      if (_phase == 0) return $"\n{name}\n{(_count > 0.4 ? Math.Ceiling(_count - 0.2).ToString() : "GO")}";
      return $"\n{name}\nCLIPS {_jP.ClipsHit}/{_jP.Clip.Length}   ANGLE {Math.Abs(_car.Beta) * 180 / Math.PI:0}°" + (_jP.Taps > 0 ? $"   TAPS {_jP.Taps}" : "");
    }
  }
}
