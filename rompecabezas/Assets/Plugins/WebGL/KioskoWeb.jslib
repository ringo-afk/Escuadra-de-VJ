mergeInto(LibraryManager.library, {
  KioskoAvisarPagina: function (tipoPtr, datosPtr) {
    var tipo = UTF8ToString(tipoPtr);
    var datos = UTF8ToString(datosPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ fuente: 'kiosko-juego', tipo: tipo, datos: datos }, '*');
      }
    } catch (e) {  }
  }
});
