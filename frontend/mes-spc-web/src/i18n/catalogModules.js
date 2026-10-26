const validKey=key=>/^[a-z][a-zA-Z0-9]*$/.test(key)&&!['__proto__','constructor','prototype'].includes(key);
const object=value=>value!==null&&typeof value==='object'&&!Array.isArray(value)&&[Object.prototype,null].includes(Object.getPrototypeOf(value));

// Catalog modules never overwrite a prior leaf or change its shape.
export function composeCatalogs(...modules){
 if(!modules.length)throw Error('Missing catalog modules');
 const result={};
 function merge(target,source,path=''){
  if(!object(source)||!Object.keys(source).length)throw Error(`Invalid catalog object: ${path}`);
  for(const [key,value] of Object.entries(source)){
   if(!validKey(key))throw Error(`Invalid catalog key: ${key}`);
   const next=path?path+'.'+key:key,exists=Object.hasOwn(target,key);
   if(typeof value==='string'&&value.trim()){
    if(exists)throw Error(`Catalog collision: ${next}`);
    target[key]=value;
   }else if(object(value)){
    if(exists&&!object(target[key]))throw Error(`Catalog collision: ${next}`);
    if(!exists)target[key]={};
    merge(target[key],value,next);
   }else throw Error(`Invalid catalog leaf: ${next}`);
  }
 }
 for(const module of modules)merge(result,module);
 return result;
}
