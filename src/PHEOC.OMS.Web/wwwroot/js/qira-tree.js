'use strict';
(() => {
 const root=document.querySelector('[data-qira-tree]');if(!root)return;
 const viewport=root.querySelector('.tree-viewport'),size=root.querySelector('.tree-size'),world=root.querySelector('.tree-world'),svg=root.querySelector('.tree-lines');
 const nodes=[...root.querySelectorAll('[data-tree-id]')],edges=[...svg.querySelectorAll('.tree-edge')],bands=[...root.querySelectorAll('[data-band-column]')];
 const pathToggle=root.querySelector('[data-tree-path]');
 let vertical=matchMedia('(max-width:700px)').matches,onlyPath=vertical,manual=false,zoom=1,fit=true,pending=false;
 try{const direction=sessionStorage.getItem('pheoc.qira.tree.direction'),path=sessionStorage.getItem('pheoc.qira.tree.path');if(direction==='vertical'||direction==='horizontal'){vertical=direction==='vertical';onlyPath=path==='true';manual=true;}}catch{/* Presentation preferences are optional. */}
 function remember(){try{sessionStorage.setItem('pheoc.qira.tree.direction',vertical?'vertical':'horizontal');sessionStorage.setItem('pheoc.qira.tree.path',String(onlyPath));}catch{/* Keep working when storage is unavailable. */}}
 pathToggle.checked=onlyPath;
 function layout(){
  root.dataset.direction=vertical?'vertical':'horizontal';root.dataset.path=String(onlyPath);root.dataset.ready='true';
  root.querySelectorAll('[data-tree-direction]').forEach(b=>b.setAttribute('aria-pressed',String((b.dataset.treeDirection==='vertical')===vertical)));
  const shown=nodes.filter(n=>!onlyPath||n.dataset.onPath==='true');nodes.forEach(n=>n.hidden=!shown.includes(n));
  const nodeWidth=onlyPath&&vertical?Math.min(300,Math.max(230,viewport.clientWidth-36)):196;
  const step=236,gap=35,top=76;let width=8*step+20,height=0;
  shown.forEach(n=>{n.style.width=`${nodeWidth}px`;});
  const positions=new Map();
  if(onlyPath){
   // Recover the ordered visited path from selected edges, never from visual coordinates.
   const ordered=[];let id='Q1';const seen=new Set();
   while(!seen.has(id)){seen.add(id);const n=shown.find(n=>n.dataset.treeId===id);if(!n)break;ordered.push(n);const next=edges.find(e=>e.dataset.from===id&&e.classList.contains('is-selected'));if(!next)break;id=next.dataset.to;}
   let offset=24;const pathWidth=Math.max(nodeWidth+40,viewport.clientWidth-8);
   ordered.forEach(n=>{positions.set(n,vertical?{x:(pathWidth-nodeWidth)/2,y:offset}:{x:24+(ordered.length-ordered.indexOf(n)-1)*step,y:35});offset+=n.offsetHeight+70;});
   if(vertical){width=pathWidth;height=offset;}else{width=ordered.length*step+12;height=Math.max(...ordered.map(n=>n.offsetHeight),0)+90;}
  }else if(vertical){
   const columns=Array.from({length:8},(_,i)=>shown.filter(n=>Number(n.dataset.column)===i));let y=top;
   columns.forEach(group=>{group.forEach(n=>positions.set(n,{x:24+Number(n.dataset.row)*step,y}));y+=Math.max(...group.map(n=>n.offsetHeight),90)+95;});
   width=5*step+40;height=y;
  }else{
   for(let c=0;c<8;c++){let bottom=top;shown.filter(n=>Number(n.dataset.column)===c).sort((a,b)=>Number(a.dataset.row)-Number(b.dataset.row)).forEach(n=>{const y=Math.max(top+Number(n.dataset.row)*205,bottom);positions.set(n,{x:24+(7-c)*step,y});bottom=y+n.offsetHeight+gap;height=Math.max(height,bottom);});}
   height+=75;
  }
  shown.forEach(n=>{const p=positions.get(n);if(!p){n.hidden=true;return;}n.style.left=`${p.x}px`;n.style.top=`${p.y}px`;});
  bands.forEach(b=>{
   b.hidden=onlyPath;if(onlyPath)return;
   const c=Number(b.dataset.bandColumn),span=Number(b.dataset.bandSpan);
   if(vertical){const start=shown.filter(n=>Number(n.dataset.column)===c),end=shown.filter(n=>Number(n.dataset.column)===c+span-1);const y=Math.min(...start.map(n=>positions.get(n).y))-50;
    b.style.left='8px';b.style.top=`${y}px`;b.style.width=`${width-16}px`;b.style.height=`${Math.max(...end.map(n=>positions.get(n).y+n.offsetHeight))-y+45}px`;
   }else{b.style.left=`${12+(8-c-span)*step}px`;b.style.top='0px';b.style.width=`${span*step-4}px`;b.style.height=`${height}px`;}
  });
  world.style.width=`${width}px`;world.style.height=`${height}px`;svg.setAttribute('width',width);svg.setAttribute('height',height);svg.setAttribute('viewBox',`0 0 ${width} ${height}`);
  const ids=new Map(shown.filter(n=>!n.hidden).map(n=>[n.dataset.treeId,n]));
  edges.forEach((edge,index)=>{
   const from=ids.get(edge.dataset.from),to=ids.get(edge.dataset.to);const selected=edge.classList.contains('is-selected');
   edge.style.display=from&&to&&(!onlyPath||selected)?'':'none';if(!from||!to||(onlyPath&&!selected))return;
   const a=positions.get(from),b=positions.get(to);let x1,y1,x2,y2,d,labelX,labelY;
   if(vertical){x1=a.x+from.offsetWidth/2;y1=a.y+from.offsetHeight;x2=b.x+to.offsetWidth/2;y2=b.y;const bend=(y1+y2)/2;d=`M ${x1} ${y1} C ${x1} ${bend},${x2} ${bend},${x2} ${y2}`;labelX=(x1+x2)/2;labelY=bend-7;}
   else{
    x1=a.x;y1=a.y+from.offsetHeight/2;x2=b.x+to.offsetWidth;y2=b.y+to.offsetHeight/2;
    if(!onlyPath&&Number(to.dataset.column)-Number(from.dataset.column)>3){const lane=height-30;x1=a.x+from.offsetWidth/2;y1=a.y+from.offsetHeight;x2=b.x+to.offsetWidth/2;y2=b.y+to.offsetHeight;d=`M ${x1} ${y1} L ${x1} ${lane} L ${x2} ${lane} L ${x2} ${y2}`;labelX=(x1+x2)/2;labelY=lane-7;}
    else if(!onlyPath&&edge.dataset.from==='Q41'&&edge.dataset.to==='@VeryLow'){x1=a.x+from.offsetWidth/2;y1=a.y;x2=b.x+to.offsetWidth/2;y2=b.y;const lane=48;d=`M ${x1} ${y1} L ${x1} ${lane} L ${x2} ${lane} L ${x2} ${y2}`;labelX=(x1+x2)/2;labelY=lane-7;}
    else{const bend=(x1+x2)/2;d=`M ${x1} ${y1} C ${bend} ${y1},${bend} ${y2},${x2} ${y2}`;labelX=bend;labelY=(y1+y2)/2-7;}
   }
   const line=edge.querySelector('path');line.id=`qira-edge-${index}`;line.setAttribute('d',d);
   const label=edge.querySelector('text');label.textContent=edge.dataset.label;label.setAttribute('x',labelX);label.setAttribute('y',labelY);
  });
  edges.filter(e=>e.classList.contains('is-selected')).forEach(e=>svg.append(e));
  if(fit)zoom=Math.min(1,(viewport.clientWidth-8)/width);
  world.style.transform=`scale(${zoom})`;size.style.width=`${Math.ceil(width*zoom)}px`;size.style.height=`${Math.ceil(height*zoom)}px`;
 }
 function schedule(){if(pending)return;pending=true;requestAnimationFrame(()=>{pending=false;layout();});}
 function centerCurrent(){const n=root.querySelector('.is-current')??root.querySelector('.is-result');if(!n||n.hidden)return;viewport.scrollTop=Math.max(0,Number.parseFloat(n.style.top)*zoom-(viewport.clientHeight-n.offsetHeight*zoom)/2);viewport.scrollLeft=Math.max(0,Number.parseFloat(n.style.left)*zoom-(viewport.clientWidth-n.offsetWidth*zoom)/2);}
 root.querySelectorAll('[data-tree-direction]').forEach(b=>b.addEventListener('click',()=>{manual=true;vertical=b.dataset.treeDirection==='vertical';fit=true;remember();layout();}));
 pathToggle.addEventListener('change',()=>{manual=true;onlyPath=pathToggle.checked;fit=true;remember();layout();});
 root.querySelectorAll('[data-tree-zoom]').forEach(b=>b.addEventListener('click',()=>{fit=false;zoom=Math.max(.25,Math.min(1.7,zoom+(b.dataset.treeZoom==='in'?.15:-.15)));layout();}));
 root.querySelector('[data-tree-fit]').addEventListener('click',()=>{fit=true;layout();});
 root.querySelector('[data-tree-current]').addEventListener('click',()=>{const n=root.querySelector('.is-current')??root.querySelector('.is-result');if(!n)return;fit=false;zoom=1;layout();n.scrollIntoView({block:'nearest',inline:'center'});n.querySelector('button,a')?.focus({preventScroll:true});});
 window.addEventListener('resize',()=>{if(!manual){vertical=matchMedia('(max-width:700px)').matches;onlyPath=vertical;pathToggle.checked=onlyPath;}schedule();});
 const observer=new ResizeObserver(schedule);nodes.forEach(n=>observer.observe(n));
 root.querySelectorAll('details').forEach(d=>d.addEventListener('toggle',schedule));
 layout();requestAnimationFrame(centerCurrent);
})();
