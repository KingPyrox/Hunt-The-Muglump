mergeInto(LibraryManager.library, {
    SaveToIndexedDB: function(key, data) {
        var keyStr = UTF8ToString(key);
        var dataStr = UTF8ToString(data);
        
        try {
            localStorage.setItem(keyStr, dataStr);
            
            // Also try IndexedDB for larger data
            if (window.indexedDB) {
                var request = window.indexedDB.open("HuntTheMuglump", 1);
                
                request.onsuccess = function(event) {
                    var db = event.target.result;
                    var transaction = db.transaction(["saves"], "readwrite");
                    var objectStore = transaction.objectStore("saves");
                    objectStore.put({key: keyStr, data: dataStr});
                };
                
                request.onupgradeneeded = function(event) {
                    var db = event.target.result;
                    if (!db.objectStoreNames.contains("saves")) {
                        db.createObjectStore("saves", { keyPath: "key" });
                    }
                };
            }
        } catch (e) {
            console.error("Failed to save data:", e);
        }
    },
    
    LoadFromIndexedDB: function(key) {
        var keyStr = UTF8ToString(key);
        
        try {
            // Try localStorage first
            var data = localStorage.getItem(keyStr);
            if (data) {
                var buffer = _malloc(lengthBytesUTF8(data) + 1);
                writeStringToMemory(data, buffer);
                return buffer;
            }
        } catch (e) {
            console.error("Failed to load data:", e);
        }
        
        return null;
    },
    
    DeleteFromIndexedDB: function(key) {
        var keyStr = UTF8ToString(key);
        
        try {
            localStorage.removeItem(keyStr);
            
            if (window.indexedDB) {
                var request = window.indexedDB.open("HuntTheMuglump", 1);
                request.onsuccess = function(event) {
                    var db = event.target.result;
                    var transaction = db.transaction(["saves"], "readwrite");
                    var objectStore = transaction.objectStore("saves");
                    objectStore.delete(keyStr);
                };
            }
        } catch (e) {
            console.error("Failed to delete data:", e);
        }
    },
    
    IsMobile: function() {
        var isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);
        return isMobile ? 1 : 0;
    },
    
    RequestFullscreen: function() {
        var canvas = document.querySelector("#unity-canvas");
        if (canvas) {
            if (canvas.requestFullscreen) {
                canvas.requestFullscreen();
            } else if (canvas.mozRequestFullScreen) {
                canvas.mozRequestFullScreen();
            } else if (canvas.webkitRequestFullscreen) {
                canvas.webkitRequestFullscreen();
            } else if (canvas.msRequestFullscreen) {
                canvas.msRequestFullscreen();
            }
        }
    },
    
    ExitFullscreen: function() {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.mozCancelFullScreen) {
            document.mozCancelFullScreen();
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
    },
    
    IsFullscreen: function() {
        var isFullscreen = document.fullscreenElement || 
                          document.mozFullScreenElement || 
                          document.webkitFullscreenElement || 
                          document.msFullscreenElement;
        return isFullscreen ? 1 : 0;
    }
});