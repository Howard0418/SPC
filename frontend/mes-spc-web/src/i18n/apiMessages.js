import { snapshotApiDisplay } from './uiMessage.js';
const plain=value=>value!==null&&typeof value==='object'&&!Array.isArray(value)&&[Object.prototype,null].includes(Object.getPrototypeOf(value));

// A validated independent snapshot can be rendered again after a locale change.
export function captureApiErrorDisplay(data,source){
 const descriptor=resolveApiErrorDescriptor(data,source);
 if(!descriptor)return null;
 const tree=resolveApiErrorTree(data,source);
 if(tree)return snapshotApiDisplay(tree);
 return typeof data.message==='string'&&data.message?data.message:snapshotApiDisplay(descriptor);
}

// Server keys are accepted only from the packaged API namespace; parameters are data.
export function resolveApiErrorDescriptor(data,source){
 if(!plain(data)||typeof data.messageKey!=='string'||!/^spc\.api\.[a-z][a-zA-Z0-9]*$/.test(data.messageKey))return null;
 const template=data.messageKey.split('.').reduce((node,key)=>plain(node)&&Object.hasOwn(node,key)?node[key]:undefined,source);
 if(typeof template!=='string')return null;
 const names=new Set([...template.matchAll(/\{([a-z][a-zA-Z0-9]*)\}/g)].map(match=>match[1]));
 const args=data.args??{};
 if(!plain(args)||Object.keys(args).length!==names.size)return null;
 const result={};
 for(const name of names){
  if(!Object.hasOwn(args,name))return null;
  const value=args[name];
  if(typeof value!=='string'&&!(typeof value==='number'&&Number.isFinite(value)))return null;
  result[name]=String(value);
 }
 return {key:data.messageKey,args:result};
}

export function formatApiErrorDescriptor(data,source,translate){
 const descriptor=resolveApiErrorDescriptor(data,source);
 if(!descriptor)return null;
 const tree=resolveApiErrorTree(data,source);
 if(!tree)return typeof data.message==='string'&&data.message?data.message:translate(descriptor.key,descriptor.args);
 const render=node=>[translate(node.key,node.args),...node.details.map(render)].join('\n');
 return render(tree);
}

// Normalize only known explicit descriptors; never compile raw text or nested arguments.
export function resolveApiErrorTree(data,source){
 const path=new Set();let nodes=0;
 function visit(value,depth){
  if(depth>3||++nodes>64||path.has(value))throw Error('Invalid display tree');
  const item=resolveApiErrorDescriptor(value,source);if(!item)throw Error('Unknown display descriptor');
  const details=value.details??[];if(!Array.isArray(details)||details.length>64)throw Error('Invalid display details');
  path.add(value);const children=details.map(child=>visit(child,depth+1));path.delete(value);
  return {...item,details:children};
 }
 try{return visit(data,0)}catch{return null}
}

// Additive metadata for old API endpoints whose response body must remain a string.
export function resolveLegacyApiDescriptor(data,headers,source){
 if(typeof data!=='string')return null;
 const header=name=>typeof headers?.get==='function'?headers.get(name):headers?.[name];
 const key=header('x-spc-message-key'),encoded=header('x-spc-message-args');
 let args={};
 if(encoded!=null){
  if(typeof encoded!=='string'||encoded.length>4096||!encoded.length||!/^[A-Za-z0-9_-]+$/.test(encoded)||encoded.length%4===1)return null;
  try{const bytes=Uint8Array.from(atob(encoded.replace(/-/g,'+').replace(/_/g,'/')+'='.repeat((4-encoded.length%4)%4)),c=>c.charCodeAt(0));args=JSON.parse(new TextDecoder('utf-8',{fatal:true}).decode(bytes));}catch{return null;}
 }
 return resolveApiErrorDescriptor({messageKey:key,args},source);
}
