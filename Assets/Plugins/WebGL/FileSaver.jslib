var FileSaver = {
    SaveTextFile: function (filename, text) {
        var a = document.createElement('a');
        var file = new Blob([text], { type: 'application/json' });

        a.href = URL.createObjectURL(file);
        a.download = UTF8ToString(filename);  // Convert C# string
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }
};

mergeInto(LibraryManager.library, FileSaver);

var FileLoader = {
    OpenFile: function () {
        var input = document.createElement('input');
        input.type = 'file';
        input.accept = '.mp3';

        input.onchange = function (event) {
            var file = event.target.files[0];
            if (!file) return;

            var reader = new FileReader();
            reader.readAsArrayBuffer(file);

            reader.onloadend = function () {
                var byteArray = new Uint8Array(reader.result);
                SendMessage('MappingModeController', 'OnAudioFileSelected', byteArray);
            };
        };

        input.click();
    }
};

mergeInto(LibraryManager.library, FileLoader);