'use strict';
(async () => {
 const element=document.querySelector('#event-map');if(!element||!window.L)return;
 const map=L.map(element,{attributionControl:true}).setView([27.0,30.5],5);
 map.attributionControl.addAttribution('Natural Earth · Public domain');
 try {const response=await fetch('/data/egypt.geojson');if(response.ok){const boundary=await response.json();L.geoJSON(boundary,{style:{color:'#73949c',weight:2,fillColor:'#dce9e8',fillOpacity:0.8}}).addTo(map);}}
 catch { /* The coordinate view remains usable if the optional boundary layer is unavailable. */ }
 try {
  const response=await fetch('/Map/Data');if(!response.ok)throw new Error('تعذر تحميل مواقع الأحداث');
  const events=await response.json();
  for(const event of events){
   const content=document.createElement('div');const title=document.createElement('strong');title.textContent=event.title;const link=document.createElement('a');link.href='/Events/Details/'+event.id;link.textContent='فتح الحدث';const para=document.createElement('p');para.textContent=event.governorate;content.append(title,para,link);
   L.circleMarker([event.latitude,event.longitude],{radius:8,color:'#fff',weight:2,fillColor:'#087f83',fillOpacity:1}).addTo(map).bindPopup(content);
  }
 }catch{element.insertAdjacentText('afterend','تعذر تحميل مواقع الأحداث. أعد تحميل الصفحة.');}
})();
