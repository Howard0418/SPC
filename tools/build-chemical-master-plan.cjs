const x = require('../frontend/mes-spc-web/node_modules/xlsx');
const fs = require('fs');
const wb = x.readFile('sample-data/批次轉換結果_藥液_計量型匯入.xlsx');
const rows = x.utils.sheet_to_json(wb.Sheets[wb.SheetNames[0]], { defval: '' });
const combos = new Map();
const processes = new Map();
const machines = new Map();
const chars = new Map();
for (const r of rows) {
  const process = String(r.ProcessCode || '').trim();
  const machine = String(r.MachineCode || '').trim();
  const tank = String(r.TankCode || '').trim();
  const ch = String(r.CharacteristicCode || '').trim();
  if (!process || !machine || !tank || !ch) continue;
  const key = `${process}\t${machine}\t${tank}\t${ch}`;
  combos.set(key, (combos.get(key) || 0) + 1);
  processes.set(process, true);
  const machineKey = `${process}\t${machine}`;
  if (!machines.has(machineKey)) machines.set(machineKey, new Map());
  machines.get(machineKey).set(tank, true);
  chars.set(ch, String(r.CharacteristicName || ch).trim());
}
const out = {
  rowCount: rows.length,
  comboCount: combos.size,
  processes: [...processes.keys()].sort(),
  machines: [...machines.entries()].map(([key, tanks]) => { const [process, machine] = key.split('\t'); return { process, machine, tanks: [...tanks.keys()].sort() }; }).sort((a,b)=>a.process.localeCompare(b.process)||a.machine.localeCompare(b.machine)),
  characteristics: [...chars.entries()].map(([code,name]) => ({ code, name })).sort((a,b)=>a.code.localeCompare(b.code)),
  combos: [...combos.entries()].map(([key,count]) => { const [process,machine,tank,characteristic] = key.split('\t'); return { process, machine, tank, characteristic, count }; }).sort((a,b)=>a.process.localeCompare(b.process)||a.machine.localeCompare(b.machine)||a.tank.localeCompare(b.tank)||a.characteristic.localeCompare(b.characteristic))
};
fs.writeFileSync('sample-data/批次轉換結果_藥液_master_plan.json', JSON.stringify(out, null, 2), 'utf8');
console.log(JSON.stringify({ rows: out.rowCount, combos: out.comboCount, processes: out.processes.length, machines: out.machines.length, characteristics: out.characteristics.length }, null, 2));
