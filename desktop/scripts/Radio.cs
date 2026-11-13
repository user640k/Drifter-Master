// A real RC transmitter as the controller. A pistol-grip radio plugged into the PC (by its own USB port, or through a
// simulator dongle on its receiver) shows up as a joystick with plain numbered axes: the steering wheel on one, the
// throttle trigger on another (pull: forward; push: brake, then reverse). This picks the two axes out by watching which
// one moves, learns their centre and end points, and turns them into the car's steering, throttle and brake, one to one:
// no dead zone to speak of and no curve, as on the real car.
using Godot;
using System;

namespace DriftMaster {
  public class Radio {
    public bool On;                       // false: keyboard and gamepad
    public int Device, SteerAxis = 0, ThrAxis = 1; public bool SteerRev, ThrRev;
    public float SMin = -1, SCen = 0, SMax = 1, TMin = -1, TCen = 0, TMax = 1;
    public const int Axes = 10; public const float Dead = 0.015f;

    // what is being learned just now: 0 nothing, 1 the steering axis, 2 the throttle axis, 3 centre and end points
    public int Learning; double _learnT; readonly float[] _base = new float[Axes], _swing = new float[Axes];
    public string Status = "";

    /// a raw axis value against its centre and end points: -1 .. 1, 0 at rest
    public static double Norm(double raw, double min, double cen, double max, bool rev) {
      double lo = Math.Min(min, max), hi = Math.Max(min, max); cen = Math.Clamp(cen, lo, hi);
      double v = raw >= cen ? (hi - cen > 1e-4 ? (raw - cen) / (hi - cen) : 0) : (cen - lo > 1e-4 ? (raw - cen) / (cen - lo) : 0);
      v = Math.Clamp(v, -1, 1); if (Math.Abs(v) < Dead) v = 0; else v = Math.Sign(v) * (Math.Abs(v) - Dead) / (1 - Dead);
      return rev ? -v : v;
    }
    public bool Connected => Input.GetConnectedJoypads().Contains(Device);
    public string DeviceName => Connected ? Input.GetJoyName(Device) : (Input.GetConnectedJoypads().Count == 0 ? "NOTHING PLUGGED IN" : "NOT CONNECTED");
    public void NextDevice(int d) { var pads = Input.GetConnectedJoypads(); if (pads.Count == 0) return; int i = pads.IndexOf(Device); Device = pads[((i < 0 ? 0 : i + d) % pads.Count + pads.Count) % pads.Count]; }
    float Raw(int axis) => Fake != null ? Fake[axis] : Input.GetJoyAxis(Device, (JoyAxis)axis);
    public float[] Fake;                  // test: axis values from the command line instead of a device

    /// wheel to the right and trigger pulled are positive on most radios; the car's steering is positive to the left
    public double Steer => -Norm(Raw(SteerAxis), SMin, SCen, SMax, SteerRev);
    public double Trigger => Norm(Raw(ThrAxis), TMin, TCen, TMax, ThrRev);
    public void Read(ref CarInput inp) { if (!On || (!Connected && Fake == null)) return; double t = Trigger; inp.Steer = Steer; inp.Thr = Math.Max(0, t); inp.Brk = Math.Max(0, -t); inp.ThrDigital = false; }

    public void Learn(int what) {
      if (!Connected && Fake == null) { Status = "PLUG THE RADIO IN FIRST"; return; }
      Learning = what; _learnT = what == 3 ? 6 : 4; for (int a = 0; a < Axes; a++) { _base[a] = Raw(a); _swing[a] = 0; }
      if (what == 3) { SCen = _base[SteerAxis]; TCen = _base[ThrAxis]; SMin = SMax = SCen; TMin = TMax = TCen; }
    }
    /// call every frame; true while something is being learned (the menu shows Status)
    public bool Step(double dt) {
      if (Learning == 0) return false; _learnT -= dt;
      if (Learning == 3) {
        float s = Raw(SteerAxis), t = Raw(ThrAxis); SMin = Math.Min(SMin, s); SMax = Math.Max(SMax, s); TMin = Math.Min(TMin, t); TMax = Math.Max(TMax, t);
        Status = $"WHEEL FULLY LEFT AND RIGHT, TRIGGER FULLY IN AND OUT   {Math.Ceiling(_learnT)}";
        if (_learnT <= 0) { Learning = 0; bool ok = SMax - SMin > 0.2f && TMax - TMin > 0.2f; if (!ok) { SMin = TMin = -1; SMax = TMax = 1; SCen = TCen = 0; } Status = ok ? "CALIBRATED" : "NOTHING MOVED: LEFT AS IT WAS"; }
        return true;
      }
      int best = -1; for (int a = 0; a < Axes; a++) { _swing[a] = Math.Max(_swing[a], Math.Abs(Raw(a) - _base[a])); if (Learning == 2 && a == SteerAxis) continue; if (best < 0 || _swing[a] > _swing[best]) best = a; }
      Status = (Learning == 1 ? "TURN THE WHEEL" : "PULL THE TRIGGER") + $"   {Math.Ceiling(_learnT)}";
      if (best >= 0 && _swing[best] > 0.5f || _learnT <= 0) {
        if (best >= 0 && _swing[best] > 0.25f) { if (Learning == 1) SteerAxis = best; else ThrAxis = best; Status = (Learning == 1 ? "STEERING" : "THROTTLE") + $" IS CHANNEL {best + 1}"; } else Status = "NOTHING MOVED: LEFT AS IT WAS";
        Learning = 0; }
      return true;
    }
    public void Save(ConfigFile cf) { cf.SetValue("radio", "on", On); cf.SetValue("radio", "steer_axis", SteerAxis); cf.SetValue("radio", "thr_axis", ThrAxis); cf.SetValue("radio", "steer_rev", SteerRev); cf.SetValue("radio", "thr_rev", ThrRev);
      cf.SetValue("radio", "cal", new float[] { SMin, SCen, SMax, TMin, TCen, TMax }); }
    public void Load(ConfigFile cf) { On = (bool)cf.GetValue("radio", "on", false); SteerAxis = Math.Clamp((int)cf.GetValue("radio", "steer_axis", 0), 0, Axes - 1); ThrAxis = Math.Clamp((int)cf.GetValue("radio", "thr_axis", 1), 0, Axes - 1);
      SteerRev = (bool)cf.GetValue("radio", "steer_rev", false); ThrRev = (bool)cf.GetValue("radio", "thr_rev", false);
      var c = (float[])cf.GetValue("radio", "cal", new float[] { -1, 0, 1, -1, 0, 1 }); if (c.Length == 6) { SMin = c[0]; SCen = c[1]; SMax = c[2]; TMin = c[3]; TCen = c[4]; TMax = c[5]; } }
  }
}
