﻿var _____WB$wombat$assign$function_____ = function(name) {return (self._wb_wombat && self._wb_wombat.local_init && self._wb_wombat.local_init(name)) || self[name]; };
if (!self.__WB_pmw) { self.__WB_pmw = function(obj) { this.__WB_source = obj; return this; } }
{
    let window = _____WB$wombat$assign$function_____("window");
    let self = _____WB$wombat$assign$function_____("self");
    let document = _____WB$wombat$assign$function_____("document");
    let location = _____WB$wombat$assign$function_____("location");
    let top = _____WB$wombat$assign$function_____("top");
    let parent = _____WB$wombat$assign$function_____("parent");
    let frames = _____WB$wombat$assign$function_____("frames");
    let opener = _____WB$wombat$assign$function_____("opener");

    ;// bundle: Widgets___HierarchicalDropdown___7a314e37b080b485592363a9262b2aa4_m
    ;// files: modules/Widgets/HierarchicalDropdown.js

    ;// modules/Widgets/HierarchicalDropdown.js
    Roblox.define("Widgets.HierarchicalDropdown",[],function(){function n(n){var t=n.width();n.find("li").each(function(n,i){i=$(i),i.outerWidth()>t&&(t=i.outerWidth())}),n.find("li").each(function(n,i){i=$(i),i.width()<t&&i.width(t)})}function t(){var i=0,r=0,t=$(".roblox-hierarchicaldropdown"),f=t.find("li"),u=t.find("li ul"),e=t.find("li ul[hover=true]");u.mouseover(function(){$(this).attr("hover","true")}),u.mouseout(function(){$(this).attr("hover","false")}),f.mouseover(function(){var i=$(this).data("delay"),f;i!="ignore"&&e.length==0&&($(this).attr("hover","true"),i!="never"&&(r==1||i=="always")?window.setTimeout(function(){if(e.length==0){var i=t.find("li[hover=true] ul");u.hide(),i.length!=0&&(i.show(),n(i))}},1e3):(u.hide(),f=$(this).find("ul"),f.show(),n(f)))}),f.mouseout(function(){$(this).removeAttr("hover")}),t.mouseleave(function(){window.setTimeout(function(){u.hide()},100),i=0,r=0}),t.mousemove(function(n){var t=i;i=n.pageX,(t==i||t==0)&&(r=0),r=t<i?1:-1})}return{init:t}});


}