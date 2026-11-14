// Fewer draw calls: everything under a node that never moves is gathered into one mesh per material. The renderer
// draws each mesh instance on its own, so a flag made of six boxes costs six draws (and six more in the shadow pass);
// merged, the whole room's worth of one material costs one.
using Godot;
using System.Collections.Generic;

namespace DriftMaster {
  public static class Merge {
    /// merge the visible mesh instances under root (not those in keep, nor anything under them); noShadow: the merged meshes cast none
    public static void Static(Node3D root, bool noShadow = false, params Node[] keep) {
      var groups = new Dictionary<(Material mat, int shadow, uint layers, ulong fmt), SurfaceTool>(); var done = new List<MeshInstance3D>(); var skip = new HashSet<Node>(keep);
      void Walk(Node n, Transform3D xf) {
        if (skip.Contains(n) || n == null) return; if (n is Node3D n3) { if (!n3.Visible) return; if (n != root) xf = xf * n3.Transform; }
        if (n is MeshInstance3D mi && mi.Mesh != null && mi.Skin == null && xf.Basis.Determinant() > 0) { bool ok = true;
          for (int s = 0; s < mi.Mesh.GetSurfaceCount(); s++) if (mi.Mesh is ArrayMesh am && am.SurfaceGetPrimitiveType(s) != Mesh.PrimitiveType.Triangles) ok = false;
          if (ok) { for (int s = 0; s < mi.Mesh.GetSurfaceCount(); s++) {
              var mat = mi.MaterialOverride ?? mi.GetSurfaceOverrideMaterial(s) ?? mi.Mesh.SurfaceGetMaterial(s);
              ulong fmt = mi.Mesh is ArrayMesh a2 ? (ulong)a2.SurfaceGetFormat(s) & 0xFF : 0xFFFF;   // (meshes with different vertex data are kept apart)
              var key = (mat, noShadow ? 0 : (int)mi.CastShadow, mi.Layers, fmt);
              if (!groups.TryGetValue(key, out var st)) { st = new SurfaceTool(); groups[key] = st; }
              st.AppendFrom(mi.Mesh, s, xf); }
            done.Add(mi); } }
        foreach (var ch in n.GetChildren()) Walk(ch, xf);
      }
      Walk(root, Transform3D.Identity);
      if (done.Count < 2) return;
      foreach (var mi in done) { mi.Mesh = null; if (mi.GetChildCount() == 0) { mi.GetParent().RemoveChild(mi); mi.QueueFree(); } }
      foreach (var (key, st) in groups) root.AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = key.mat, CastShadow = (GeometryInstance3D.ShadowCastingSetting)key.shadow, Layers = key.layers, Name = "Merged" });
    }
  }
}
