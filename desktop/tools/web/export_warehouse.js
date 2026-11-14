const { chromium } = require('playwright'); const fs = require('fs');
(async () => {
  const b = await chromium.launch({ args: ['--use-gl=swiftshader', '--enable-unsafe-swiftshader'] });
  const p = await b.newPage({ viewport: { width: 1280, height: 720 } });
  p.on('pageerror', e => console.log('ERR', e.message)); p.on('console', m => { if (m.text().startsWith('X:')) console.log(m.text()); });
  await p.route('**/three.min.js', r => r.fulfill({ path: '/home/user/carpet-circuit/tools/vendor/three.r128.min.js', contentType: 'application/javascript' }));
  await p.goto('file://' + __dirname + '/dkw.html'); await p.waitForTimeout(4000);
  await p.evaluate(() => { window.THREE = window.__carpetCircuit.THREE; });
  await p.addScriptTag({ path: __dirname + '/npm3/package/examples/js/exporters/GLTFExporter.js' });
  const info = await p.evaluate(() => { const m = window.__MAPS.arena; const g = m.group; let meshes = 0; g.traverse(o => { if (o.isMesh) meshes++; });
    return { hx: m.hx, hz: m.hz, h: m.h, bounds: m.bounds, pov: m.pov, pov2: m.pov2, scale: g.scale.toArray(), pos: g.position.toArray(), meshes, obst: m.obst, kids: g.children.length }; });
  console.log(JSON.stringify(info).slice(0, 1500));
  fs.writeFileSync('export/warehouse.json', JSON.stringify(info));
  const glb = await p.evaluate(async () => {
    const g = window.__MAPS.arena.group; g.visible = true; g.updateMatrixWorld(true);
    const lights = []; g.traverse(o => { if (o.isLight) lights.push(o); }); lights.forEach(l => l.parent.remove(l));
    // the deck's own surface and the wear drawn over it are left out: the desktop game lays its P-tile there
    const gone = []; g.traverse(o => { if (!o.isMesh) return; const bb = new THREE.Box3().setFromObject(o), sz = bb.getSize(new THREE.Vector3()); if (sz.x > 6.5 && sz.x < 8.6 && sz.z > 8.5 && sz.z < 10.9 && bb.max.y < 0.06 && sz.y < 0.08) gone.push(o); });
    gone.forEach(o => o.parent.remove(o)); console.log('X: removed ' + gone.length);
    return await new Promise((res, rej) => new THREE.GLTFExporter().parse(g, r => { const u = new Uint8Array(r); let s = ''; for (let i = 0; i < u.length; i += 32768) s += String.fromCharCode.apply(null, u.subarray(i, i + 32768)); res(btoa(s)); }, { binary: true, onlyVisible: false, maxTextureSize: 1024 }));
  });
  fs.writeFileSync('export/warehouse.glb', Buffer.from(glb, 'base64')); console.log('glb', fs.statSync('export/warehouse.glb').size);
  await b.close();
})();
