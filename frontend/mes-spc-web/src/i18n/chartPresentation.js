// Capture only labels. Never retain or patch series data, zoom, legend selection or axis ranges.
export function captureChartPresentation(option) {
 const result={};
 for(const kind of ['title','xAxis','yAxis'])if(Array.isArray(option[kind]))result[kind]=option[kind].map(row=>({[kind==='title'?'text':'name']:row[kind==='title'?'text':'name']}));
 if(Array.isArray(option.legend))result.legend=option.legend.map(row=>({formatter:row.formatter}));
 if(Array.isArray(option.graphic))result.graphic=option.graphic;
 if(Array.isArray(option.toolbox))result.toolbox=option.toolbox.map(box=>({feature:Object.fromEntries(Object.keys(box.feature||{}).filter(key=>key==='saveAsImage'||key==='restore').map(key=>[key,{title:key==='saveAsImage'?'儲存為圖片':'還原'}]))}));
 return result;
}
export function translateChartPresentation(base,translate) {
 const result={};
 for(const kind of ['title','xAxis','yAxis'])if(base[kind])result[kind]=base[kind].map(row=>{const key=kind==='title'?'text':'name';return {[key]:translate(row[key])}});
 if(base.legend)result.legend=base.legend.map(row=>({formatter:name=>translate(typeof row.formatter==='function'?row.formatter(name):typeof row.formatter==='string'?row.formatter.replace('{name}',name):name)}));
 if(base.graphic)result.graphic=base.graphic.map(row=>translateGraphic(row,translate));
 if(base.toolbox)result.toolbox=base.toolbox.map(box=>({feature:Object.fromEntries(Object.entries(box.feature).map(([key,value])=>[key,{title:translate(value.title)}]))}));
 return result;
}
function translateGraphic(row,translate){
 return {...row,...(row.style?{style:{...row.style,...(typeof row.style.text==='string'?{text:translate(row.style.text)}:{})}}:{}),...(row.elements?{elements:row.elements.map(n=>translateGraphic(n,translate))}:{}),...(row.children?{children:row.children.map(n=>translateGraphic(n,translate))}:{})};
}
export function escapeChartText(value) {
 return String(value??'').replace(/[&<>"']/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
}
