var _____WB$wombat$assign$function_____ = function(name) {return (self._wb_wombat && self._wb_wombat.local_init && self._wb_wombat.local_init(name)) || self[name]; };
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

;// bundle: Widgets___ItemImage___e0bf4632114522bd514dec91063b8cf2_m
;// files: modules/Widgets/ItemImage.js

;// modules/Widgets/ItemImage.js
Roblox.define("Widgets.ItemImage",[],function(){function i(n){var t=$(n);return{imageSize:t.attr("data-image-size")||"large",noClick:typeof t.attr("data-no-click")!="undefined",noOverlays:typeof t.attr("data-no-overlays")!="undefined",assetId:t.attr("data-item-id")||0}}function r(n,t){var o,i,r,u,f,e;t.bcOverlayUrl!=null&&(o=$("<img>").attr("src",t.bcOverlayUrl).attr("alt","Builders Club").css("position","absolute").css("left","0").css("bottom","5px").attr("border",0),n.after(o)),t.transparentBackground===!0&&n.addClass("checker-background-small"),t.limitedOverlayUrl!=null&&(i=$("<img>").attr("alt",t.limitedAltText).css("position","absolute").css("left","0").css("bottom","5px").attr("border",0),t.bcOverlayUrl!=null&&(t.imageSize=="small"?i.load(function(){i.css("left",34)}):i.load(function(){i.css("left",46)})),i.attr("src",t.limitedOverlayUrl),n.after(i)),t.deadlineOverlayUrl!=null&&(r=$("<img>").attr("alt","Deadline").attr("border",0),r.attr("src",t.deadlineOverlayUrl),n.after(r)),t.iosOverlayUrl!=null?(u=$("<img>").attr("alt","iOS Only").attr("border",0),u.attr("src",t.iosOverlayUrl),n.after(u)):t.saleOverlayUrl!=null?(f=$("<img>").attr("alt","Sale").attr("border",0),f.attr("src",t.saleOverlayUrl),n.after(f)):t.newOverlayUrl!=null&&(e=$("<img>").attr("alt","New").attr("border",0),e.attr("src",t.newOverlayUrl),n.after(e))}function t(u,f){for($.type(u)!=="array"&&(u=[u]);u.length>0;){for(var o=u.splice(0,10),s=[],e=0;e<o.length;e++)s.push(i(o[e]));$.getJSON(n.endpoint,{params:JSON.stringify(s)},function(n,i){return function(u){for(var v=[],e,a,h,o=0;o<u.length;o++)if(e=u[o],e!=null){var c=n[o],s=$(c),l=$("<div>").css("position","relative").css("overflow","hidden");s.html(l),s=l,i[o].noClick||(a=$("<a>").attr("href",e.url),s.append(a),s=a),h=$("<img>").attr("title",e.name).attr("alt",e.name).attr("border",0).addClass("original-image"),h.load(function(n,t,i,u){return function(){n.width(t.width),n.height(t.height),r(i,u)}}(l,c,h,e)),s.append(h),h.attr("src",e.thumbnailUrl),e.thumbnailFinal||v.push(c)}f=f||1,f<4&&window.setTimeout(function(){t(v,f+1)},f*2e3)}}(o,s))}}function u(){t($(n.selector+":empty").toArray())}var n={selector:".roblox-item-image",endpoint:"/item-thumbnails?jsoncallback=?"};return{config:n,load:t,populate:u}});


}
/*
     FILE ARCHIVED ON 17:53:27 Jan 30, 2016 AND RETRIEVED FROM THE
     INTERNET ARCHIVE ON 07:33:54 Jul 05, 2024.
     JAVASCRIPT APPENDED BY WAYBACK MACHINE, COPYRIGHT INTERNET ARCHIVE.

     ALL OTHER CONTENT MAY ALSO BE PROTECTED BY COPYRIGHT (17 U.S.C.
     SECTION 108(a)(3)).
*/
/*
playback timings (ms):
  captures_list: 0.798
  exclusion.robots: 0.084
  exclusion.robots.policy: 0.072
  esindex: 0.011
  cdx.remote: 12.245
  LoadShardBlock: 57.115 (3)
  PetaboxLoader3.datanode: 70.942 (5)
  load_resource: 821.545 (2)
  PetaboxLoader3.resolve: 798.127 (2)
*/