// The RC drift car: a port of the browser version's model (stepCar), in double precision and with no engine types.
// A bicycle model with four wheel loads: a Pacejka-like tyre curve with combined slip, load sensitivity, a spool or
// gear diff, a brushless motor with ESC timing, a steering gyro, Ackermann and caster, and roll/pitch/heave on springs.
// It is stepped at 720 Hz: the tyre forces are stiff, and small steps keep them stable.
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public struct CarInput { public double Thr, Brk, Steer; public bool ThrDigital; }

  public sealed class CarSetup {
    public double Mu = 0.5;                       // hard plastic drift tyres on P-tile
    public double VFree = 10.5, FStall = 24;      // 13.5T drift motor, high final drive
    public double TireMul = 0.88;                 // HARD
    public double GyroGain = 0.9;
    public double KMul = 1, CMul = 1, FFront = 0.5; // springs, shock oil, roll-bar balance
    public double AckK = 0.5, CasterDeg = 9;
    public bool GearDiff = false, Assist = true;
    public double EscV = 1.12, EscF = 0.98;       // BOOST
    public double ToeF = 2, ToeR = 2;             // degrees: front toe-out, rear toe-in
    public double WeightRear = 0.60;              // share of the weight on the rear axle (40/60 to 35/65)
    public double CamberF = 0, CamberR = 0;       // degrees; 0 = wheels upright, negative = tops lean in
  }

  public sealed class CarPhysics {
    public const double DT = 1.0 / 720, GRAV = 9.81;
    // real 1/10 drift chassis: 257 mm wheelbase, 169 mm between tyre centres
    public const double L = 0.257, TW = 0.169, HRC = 0.011, HPC = 0.010;
    public const double IYY = 0.019, R = 0.032;
    // what the shell changes: the car's mass, how high its centre of gravity sits, and how hard it is to roll and to turn.
    // (the sedan's figures; SetBody gives the others)
    public double M = 1.45, H = 0.03, IXX = 0.0050, IZ = 0.021;
    /// the chassis alone is about 1.32 kg with its centre of gravity 26 mm up; a shell adds its own mass at its own height
    public void SetBody(double shellKg, double shellCgUp, double halfWidth, double halfLength) {
      const double mc = 1.32, hc = 0.0262;
      ShellHalfL = Math.Clamp(halfLength, 0.17, 0.24); ShellHalfW = Math.Clamp(halfWidth, 0.08, 0.105);
      M = mc + shellKg; H = (mc * hc + shellKg * shellCgUp) / M;
      IXX = 0.0044 + shellKg * (halfWidth * halfWidth / 3 + (shellCgUp - H) * (shellCgUp - H)) + mc * (hc - H) * (hc - H);
      IZ = 0.0186 + shellKg * (halfLength * halfLength + halfWidth * halfWidth) / 3;
    }
    public const double KROLL = 8.5, KPITCH = 16, ZETA = 0.5;
    public const double CDA = 0.0042, RR = 0.42, DRIVELINE = 0.035, DRAGBRAKE = 0.7, BRAKEF = 30, LOADSENS = 0.12;
    public const double TB = 12, TC = 1.1;        // hard plastic: builds early, then slides at nearly constant friction
    public const double GYROK = 1.6, RCMDMAX = 3.0, RTURN = 1.5, BETAMAX = 1.0, BETAK = 2.2, THRCUT = 3, KBSLIP = 0.8, KBSLIPK = 4, GYROMIX = 0.85;
    public const double MEQ = 0.25;               // wheel + rotor inertia as a mass at the tyre surface
    public const double WEIGHTREAR = 0.56, STEERMAX = 1.22, SERVO = 0.06;
    public const double RollStop = 0.078, PitchStop = 0.052;

    public double X, Z, Hd, U, V, Rr, W, Vx, Vz, Steer, Thr, GyroOut, Beta;
    public double ShellHalfL = 0.21, ShellHalfW = 0.095; public const double CornerR = 0.03;
    public double Roll, RollV, Pitch, PitchV, Ax, Ay, Y, Vy, Heave, HeaveV, RoadR, RoadP, RoadH;
    public double ShellZ, ShellV, ShellR, ShellRV; double _heaveV0, _rollV0, _roadHV0, _roadRV0; /* the shell's own shiver on its posts: up/down (m) and rocking (rad) */
    public bool Air, Reversing, Braking, Spinning;
    public bool TouchedWall;   /* set when the car has met a barrier or wall, however lightly; whoever reads it clears it */
    public double RevHold, Spin, SpinF, AlphaF, AlphaR, Slide, RearSlip, GripF, GripR;
    public readonly double[] Loads = new double[4];
    public CarSetup S = new CarSetup();
    public Track T;
    public Action<double> OnBump;                 // a knock, 0..1 (for sound later)
    readonly List<(double nx, double nz, double pen)> _hits = new();

    public void Place(double x, double z, double h) {
      X = x; Z = z; Hd = h; U = V = Rr = W = Vx = Vz = Steer = Thr = GyroOut = Beta = 0;
      Roll = RollV = Pitch = PitchV = Ax = Ay = Vy = Heave = HeaveV = RoadR = RoadP = RoadH = ShellZ = ShellV = ShellR = ShellRV = _heaveV0 = _rollV0 = _roadHV0 = _roadRV0 = 0; Air = Reversing = Braking = Spinning = false;
      RevHold = Spin = SpinF = AlphaF = AlphaR = Slide = RearSlip = 0;
      if (T != null) { T.Idx = T.NearestT(x, z, -1); Y = T.GroundAt(x, z); } else Y = 0;
    }

    static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
    static double TireCurve(double a) => Math.Sin(TC * Math.Atan(TB * a));
    /// What camber does to an axle's grip. A little negative camber lets the loaded outside tyre sit flat as the car leans,
    /// so grip rises slightly (best near -3 degrees, about +2%); more than that and the tyre rides on its inner edge and
    /// grip falls (-9: about 7% less). Positive camber only ever tips the outside tyre onto its outer edge (+6: about 15% less).
    public static double CamberGrip(double deg) => Math.Max(0.6, 1 - 0.012 * deg - 0.0022 * deg * deg);
    static double MuAt(double n, double mu0, double nref) => mu0 * Math.Max(0.5, 1 - LOADSENS * (n / nref - 1));
    static void Tire(double kap, double tn, double d, out double fx, out double fy) {
      double sg = Math.Sqrt(kap * kap + tn * tn); if (sg < 1e-6 || d < 1e-6) { fx = fy = 0; return; }
      double f = d * TireCurve(sg) / sg; fx = -f * kap; fy = -f * tn;
    }

    public void Step(double dt, in CarInput inp, bool canDrive, bool walls = true) {
      double mu0 = S.Mu * S.TireMul, kMul = S.KMul, cMul = S.CMul, fFront = S.FFront;
      double a = L * S.WeightRear, b = L - a, m = M, Wt = m * GRAV;
      double thrIn = canDrive ? inp.Thr : 0, brkIn = canDrive ? inp.Brk : 0, st = canDrive ? inp.Steer : 0;
      // drift assist on a key: an on/off key can't feather, so it becomes a wheelspin governor aiming for a drift
      // angle set by the steering; on a trigger it only eases the throttle past the angle the gyro starts fighting
      if (S.Assist && inp.ThrDigital && thrIn > 0) {
        double betaT = 0.87 * Math.Abs(st), betaNow = Math.Abs(Beta);
        double slipTarget = Clamp(0.12 + KBSLIP * Math.Abs(st) + KBSLIPK * (betaT - betaNow) * Math.Abs(st), 0.04, 1.6);
        double wTarget = Math.Max(U, 0) * (1 + slipTarget) + 0.5;
        thrIn = W < wTarget ? 1 : Math.Min(1, wTarget / S.VFree);
      } else if (S.Assist && thrIn > 0) thrIn *= Clamp(1 - (Math.Abs(Beta) - BETAMAX) * THRCUT, 0.3, 1);
      bool up = thrIn > 0.02, dn = brkIn > 0.02;
      double u = U, v = V, r = Rr;

      // steering: the stick asks for a yaw rate and the gyro steers to hold it, through a rate-limited servo
      double Vg = Math.Sqrt(u * u + v * v), fade = Math.Min(1, Vg / 1.2);
      double rCmd = st * Math.Min(RCMDMAX, Vg / Clamp(0.45 + 0.48 * Vg, 0.65, RTURN));
      double over = Math.Max(0, Math.Abs(Beta) - BETAMAX) * Math.Sign(Beta);
      double gyro = S.GyroGain * (GYROK * (rCmd - r) - BETAK * over) * fade;
      double target = Clamp(st * STEERMAX * (1 - S.GyroGain * fade * GYROMIX) + gyro, -STEERMAX, STEERMAX);
      double servoRate = (Math.PI / 3) / SERVO;
      Steer += Clamp(target - Steer, -servoRate * dt, servoRate * dt);
      GyroOut = gyro;
      double d = Steer, cd = Math.Cos(d), sd = Math.Sin(d);
      Thr += Clamp(thrIn - Thr, -dt * 12, dt * 8);

      // ESC: forward, brake, then reverse after a short hold at standstill
      double Fdrive = 0, Fbrake = 0;
      if (up) Reversing = false;
      if (dn) {
        if (Math.Max(u, W) > 0.15 && !Reversing) Fbrake = BRAKEF * brkIn;
        else { RevHold += dt; if (RevHold > 0.2) Reversing = true; }
      } else RevHold = 0;
      double vFr = S.VFree * S.EscV, fSt = S.FStall * S.EscF;
      if (up) Fdrive = fSt * Clamp(Thr - W / vFr, -0.3, 1);
      else if (dn && Reversing) Fdrive = -S.FStall * 0.45 * brkIn * Math.Max(0, 1 + W / 3.5);
      else if (!dn) Fbrake = DRAGBRAKE;
      Braking = Fbrake > 1;

      // wheel loads: static + longitudinal and lateral transfer
      double Kr = KROLL * kMul, Kp = KPITCH * kMul;
      double Nf0 = Wt * b / L, Nr0 = Wt * a / L, Nref = Wt / 4;
      double dLong = (m * Ax * HPC + Kp * Pitch) / L;
      double Nf = Math.Max(0, Nf0 - dLong), Nr = Math.Max(0, Nr0 + dLong);
      double latF = (fFront * Kr * Roll + m * Ay * HRC * b / L) / TW;
      double latR = ((1 - fFront) * Kr * Roll + m * Ay * HRC * a / L) / TW;
      double FL = Math.Max(0, Nf / 2 - latF), FR = Math.Max(0, Nf / 2 + latF), RL = Math.Max(0, Nr / 2 - latR), RRl = Math.Max(0, Nr / 2 + latR);
      double grip = Air ? 0 : 1;
      double lockF = Math.Min(1, Math.Abs(Steer) / 1.0);
      double geoF = 1 - 0.1 * S.AckK * lockF + 0.045 * (S.CasterDeg - 9) / 3 * lockF + 0.006 * Math.Max(0, -S.CamberF) * lockF;   // negative front camber sits the leading tyre flat at big steering angles
      double Df = grip * geoF * CamberGrip(S.CamberF) * (MuAt(FL, mu0, Nref) * FL + MuAt(FR, mu0, Nref) * FR);
      double Dr = grip * CamberGrip(S.CamberR) * (MuAt(RL, mu0, Nref) * RL + MuAt(RRl, mu0, Nref) * RRl);
      if (S.GearDiff) { double Din = grip * MuAt(Math.Min(RL, RRl), mu0, Nref) * Math.Min(RL, RRl), Dout = Dr - Din; Fdrive = Math.Min(Fdrive, 2 * Din + 0.6 * Math.Max(0, Dout - Din)); }   // a tight diff (thick oil): most of the outside wheel's grip still drives
      Loads[0] = FL; Loads[1] = FR; Loads[2] = RL; Loads[3] = RRl;

      // slip angles and slip ratio (the front wheels roll freely)
      double wfx = u, wfy = v + a * r;
      double uwf = wfx * cd + wfy * sd, vwf = -wfx * sd + wfy * cd;
      double Vf = Math.Max(Math.Abs(uwf), 0.8), Vr = Math.Max(Math.Abs(u), 0.8);
      // toe: front toe-out makes the front answer the steering sooner (sharper turn-in, easier to start a slide); rear toe-in
      // makes the rear build its side force sooner (steadier through transitions). Neither raises the grip there is to be had.
      double tanF = vwf / Vf * (1 + 0.05 * S.ToeF), tanR = (v - b * r) / Vr * (1 + 0.06 * S.ToeR);
      double kapR = (u - W) / Vr;
      Tire(0, tanF, Df, out double Fxwf, out double Fywf);
      Tire(S.GearDiff ? kapR * 0.8 : kapR, tanR, Dr, out double Fxr, out double Fyr);
      double FxF = Fxwf * cd - Fywf * sd, FyF = Fxwf * sd + Fywf * cd;
      // driven-wheel speed: motor/brake torque against the tyres' reaction (linearly implicit for stability)
      double kStiff = Dr / Vr * TB * TC;
      double Fw = Fdrive - Fbrake * Math.Tanh(W * 8);
      W += dt * (Fw - Fxr) / (MEQ + dt * kStiff);
      if (!up && !(dn && Reversing) && Math.Abs(u) < 0.05 && Math.Abs(W) < 0.1) W *= Math.Exp(-30 * dt);
      double alphaF = Math.Atan(tanF), alphaR = Math.Atan(tanR);

      double Fres = RR * (1 + 0.015 * (S.ToeF + S.ToeR)) * Math.Tanh(u * 6) + CDA * u * Math.Abs(u) + DRIVELINE * u;
      double Fx = FxF + Fxr - Fres, Fy = FyF + Fyr, Mz = a * FyF - b * Fyr;
      u += (Fx / m + v * r) * dt;
      v += (Fy / m - u * r) * dt;
      r += (Mz / IZ) * dt;
      if (!up && !(dn && Reversing) && Math.Abs(u) < 0.05) u *= Math.Exp(-30 * dt);
      if (Math.Abs(u) < 0.4 && Math.Abs(v) < 0.4) { double k = Math.Exp(-6 * dt); v *= k; r *= k; }
      Ax = Fx / m; Ay = Fy / m;

      // suspension: roll, pitch and heave as damped springs driven by the chassis accelerations
      double Cr = 2 * ZETA * Math.Sqrt(Kr * IXX) * cMul, Cp = 2 * ZETA * Math.Sqrt(Kp * IYY) * cMul;
      // the floor under each wheel (tile joints and steps, undulation, humps): the springs follow it
      double roadRV, roadPV, roadHV;
      { double sh0 = Math.Sin(Hd), ch0 = Math.Cos(Hd), hw = TW / 2;
        double At(double dd, double sdd) => T == null ? 0 : T.Bump(X + sh0 * dd + ch0 * sdd * hw, Z + ch0 * dd - sh0 * sdd * hw);
        double fl = At(a, 1), fr = At(a, -1), rl = At(-b, 1), rrr = At(-b, -1);
        double rR = ((fl + rl) - (fr + rrr)) / 2 / TW, rP = ((fl + fr) - (rl + rrr)) / 2 / L, rH = (fl + fr + rl + rrr) / 4;
        roadRV = (rR - RoadR) / dt; roadPV = (rP - RoadP) / dt; roadHV = (rH - RoadH) / dt; RoadR = rR; RoadP = rP; RoadH = rH;
        double Kh = 1600 * kMul, Ch = 2 * ZETA * Math.Sqrt(Kh * m) * cMul; HeaveV += ((-Kh * (Heave - rH) - Ch * (HeaveV - roadHV)) / m) * dt; Heave += HeaveV * dt; }
      RollV += ((m * Ay * (H - HRC) - Kr * (Roll - RoadR) - Cr * (RollV - roadRV)) / IXX) * dt; Roll += RollV * dt;
      PitchV += ((m * Ax * (H - HPC) - Kp * (Pitch - RoadP) - Cp * (PitchV - roadPV)) / IYY) * dt; Pitch += PitchV * dt;
      // bump stops: the dampers have about 6 mm of travel, so the body cannot lean more than about 4.5 degrees or pitch
      // more than 3 (a knock into a barrier used to throw it over to 12)
      if (Math.Abs(Roll) > RollStop) { Roll = Math.Sign(Roll) * RollStop; if (RollV * Roll > 0) RollV *= -0.2; }
      if (Math.Abs(Pitch) > PitchStop) { Pitch = Math.Sign(Pitch) * PitchStop; if (PitchV * Pitch > 0) PitchV *= -0.2; }
      if (Math.Abs(Heave) > 0.006) { Heave = Math.Sign(Heave) * 0.006; if (HeaveV * Heave > 0) HeaveV *= -0.2; }
      // the shell: a thin body held on four posts by clips. It is a light, springy lid on the chassis: it lags and
      // shivers at about 22 Hz when the chassis is jolted, up and down and side to side, and settles in a few cycles
      // (what jolts it: the chassis moving on its springs, and the sharp knocks from the floor itself, which reach the
      // chassis through the tyres and arms faster than the springs can soak them up)
      { double aH = (HeaveV - _heaveV0) / dt + (roadHV - _roadHV0) / dt, aR = (RollV - _rollV0) / dt + (roadRV - _roadRV0) / dt; _heaveV0 = HeaveV; _rollV0 = RollV; _roadHV0 = roadHV; _roadRV0 = roadRV;
        const double w = 2 * Math.PI * 22, zt = 0.11;
        ShellV += (-w * w * ShellZ - 2 * zt * w * ShellV - aH) * dt; ShellZ += ShellV * dt;
        ShellRV += (-w * w * ShellR - 2 * zt * w * ShellRV - aR) * dt; ShellR += ShellRV * dt;
        if (Math.Abs(ShellZ) > 0.002) { ShellZ = Math.Sign(ShellZ) * 0.002; ShellV *= 0.3; } if (Math.Abs(ShellR) > 0.03) { ShellR = Math.Sign(ShellR) * 0.03; ShellRV *= 0.3; } }

      // pose in the world
      Hd += r * dt;
      double sh = Math.Sin(Hd), ch = Math.Cos(Hd);
      Vx = sh * u + ch * v; Vz = ch * u - sh * v;
      X += Vx * dt; Z += Vz * dt;
      // follow the ground; leave it only at a real drop, land with a thump
      if (T != null) {
        double gNew = T.GroundAt(X, Z);
        if (!Air) {
          double vyCap = Math.Sqrt(Vx * Vx + Vz * Vz) * 0.4 + 0.05, vyG = Clamp((gNew - Y) / dt, -vyCap, vyCap);
          if (gNew < Y && vyG < Vy - GRAV * dt * 2 && Y - gNew > 0.004) Air = true;
          else { Vy = vyG * 0.5 + Vy * 0.5; Y = gNew; }
        }
        if (Air) {
          Vy -= GRAV * dt; Y += Vy * dt;
          if (Y <= gNew) { if (Vy < -0.5) { OnBump?.Invoke(Math.Min(0.8, -Vy / 3)); PitchV -= Vy * 2; } Y = gNew; Vy = 0; Air = false; }
        }
      }

      // barriers and walls: push each axle circle out, lose speed into the barrier, turn to run along it
      bool touched = false;
      if (walls && T != null) {
        // (the shape that meets a barrier: a circle on each axle, and a small one in each corner of the shell, so a corner
        // cannot poke through a board when the car meets it at an angle)
        for (int pass = 0; pass < 2; pass++) for (int c = 0; c < 6; c++) {
          double fx = Math.Sin(Hd), fz = Math.Cos(Hd), fore = c < 2 ? (c == 0 ? 0.1285 : -0.1285) : (c < 4 ? 1 : -1) * (ShellHalfL - CornerR), side = c < 2 ? 0 : (c % 2 == 0 ? 1 : -1) * (ShellHalfW - CornerR);
          double cx = X + fx * fore + fz * side, cz = Z + fz * fore - fx * side;
          _hits.Clear(); T.ContactsFor(cx, cz, _hits, c < 2 ? Track.CarR : CornerR);
          foreach (var (nx, nz, pen) in _hits) {
            touched = true; X += nx * pen; Z += nz * pen;
            double vn = -(Vx * nx + Vz * nz);
            if (vn > 0) {
              // a hard knock bounces the car back a little and scrubs speed: a PVC board is springy (30% back, 12% lost),
              // a foam block gives and soaks it up (8% back, 24% lost)
              bool hard = vn > 0.6, board = T.BoardAt(T.Idx); double kk = hard ? (board ? 1.3 : 1.08) : 1, keep = board ? 0.88 : 0.76;
              Vx += nx * vn * kk; Vz += nz * vn * kk;
              if (hard) { Vx *= keep; Vz *= keep; r *= 0.4; OnBump?.Invoke(Math.Min(1, vn / 6)); RollV += (nx * Math.Cos(Hd) - nz * Math.Sin(Hd)) * vn * 6; PitchV -= vn * 1.5; }
            }
            double k = Math.Exp(-1.2 * dt); Vx *= k; Vz *= k;
            double tx = nz, tz = -nx, fwdDot = Math.Sin(Hd) * tx + Math.Cos(Hd) * tz;
            double along = Math.Atan2(tx, tz) + (fwdDot < 0 ? Math.PI : 0), dh = along - Hd; dh = Math.Atan2(Math.Sin(dh), Math.Cos(dh));
            double alignRate = 3 * Clamp(1.6 - Math.Sqrt(Vx * Vx + Vz * Vz) / 2, 0.15, 1);
            Hd += dh * Math.Min(1, dt * alignRate);
          }
        }
      }
      if (touched) TouchedWall = true;
      if (touched) { double s2 = Math.Sin(Hd), c2 = Math.Cos(Hd); u = Vx * s2 + Vz * c2; v = Vx * c2 - Vz * s2; }
      U = u; V = v; Rr = r;
      Beta = Math.Sqrt(u * u + v * v) > 0.6 ? Math.Atan2(v, u) : 0;
      AlphaF = alphaF; AlphaR = alphaR; Spinning = Math.Abs(kapR) > 0.2; RearSlip = Math.Max(0, -kapR);
      GripF = Df > 0 ? Math.Sqrt(Fxwf * Fxwf + Fywf * Fywf) / Df : 0; GripR = Dr > 0 ? Math.Sqrt(Fxr * Fxr + Fyr * Fyr) / Dr : 0;
      Spin += W / R * dt; SpinF += uwf / R * dt;
      Slide = Math.Max(0, Math.Max(Math.Abs(alphaF) - 0.1, Math.Abs(alphaR) - 0.1)) * Math.Min(1, Math.Sqrt(u * u + v * v) / 2) * 2.5 + Math.Min(0.6, Math.Abs(kapR) * 0.4);
    }
  }
}
