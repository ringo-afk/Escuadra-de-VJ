mergeInto(LibraryManager.library, {
  BS_AvisarPortal: function (tipo, datos) {
    var mensaje = { fuente: 'kiosko-juego', tipo: UTF8ToString(tipo), datos: UTF8ToString(datos) };
    if (window.parent && window.parent !== window) window.parent.postMessage(mensaje, '*');
  }
});
