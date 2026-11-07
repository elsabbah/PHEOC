import fs from 'node:fs';
const url='https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_110m_admin_0_countries.geojson';
const response=await fetch(url);if(!response.ok)throw new Error('Natural Earth download failed: '+response.status);
const data=await response.json();const egypt=data.features.find(f=>f.properties.ADM0_A3==='EGY');if(!egypt)throw new Error('Egypt feature missing');
fs.mkdirSync('src/PHEOC.OMS.Web/wwwroot/data',{recursive:true});
fs.writeFileSync('src/PHEOC.OMS.Web/wwwroot/data/egypt.geojson',JSON.stringify({type:'FeatureCollection',features:[egypt]}));
console.log('Saved local Egypt boundary from Natural Earth (public domain).');
