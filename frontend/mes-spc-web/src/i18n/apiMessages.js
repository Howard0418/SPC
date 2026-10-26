const plain=value=>value!==null&&typeof value==='object'&&!Array.isArray(value)&&[Object.prototype,null].includes(Object.getPrototypeOf(value));

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
 return descriptor?translate(descriptor.key,descriptor.args):null;
}
