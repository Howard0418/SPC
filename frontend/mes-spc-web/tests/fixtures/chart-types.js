// Isolated test entry only; never included in the application build or published.
import {createApp,h,provide} from 'vue';
import {createRouter,createWebHistory,RouterView} from 'vue-router';
import View from '../../src/views/ControlChartGroupsView.vue';
import LocalePicker from '../../src/components/LocalePicker.vue';
import {localeControlKey} from '../../src/i18n/context';
import {i18n} from '../../src/i18n';
import '../../src/style.css';
const router=createRouter({history:createWebHistory(),routes:[{path:'/tests/fixtures/chart-types.html',component:View,props:{groupsOnly:false}}]});
i18n.global.locale.value=localStorage.getItem('fixture-locale')||'zh-TW';
const app=createApp({setup(){provide(localeControlKey,event=>{i18n.global.locale.value=event.target.value;localStorage.setItem('fixture-locale',event.target.value)});return()=>h('main',{class:'p-4'},[h(LocalePicker),h(RouterView)])}});
app.use(router).use(i18n);router.isReady().then(()=>app.mount('#app'));
