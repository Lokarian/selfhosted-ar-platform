var signalRInterop ={
    GetServerUrl: function () {
        var returnStr = window.location.protocol + "//" + window.location.host;
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },

    GetArSessionId: function () {
        var urlParams = new URLSearchParams(window.location.search);
        var arSessionId = urlParams.get('arSessionId');
        var returnStr = arSessionId;
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },

    GetToken: function () {
        var returnStr = localStorage.getItem('access_token');
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },
    StartSignalRJs:function(){
        var url = window.location.protocol + "//" + window.location.host + "/api/unityBrokerHub";
        var connection = new signalR.HubConnectionBuilder().withUrl(url,{
            accessTokenFactory: async () => {
                return localStorage.getItem("access_token");
            }
        }).withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol()).build();
        connection.start().then(async () => {
            console.log('SignalR Connected!');
            var urlParams = new URLSearchParams(window.location.search);
            var arSessionId = urlParams.get('arSessionId');
            const myId = await connection.invoke("CreateArMember", arSessionId,1);//1 is for ArUserRole.Web

            const stream=connection.stream("ClientGetOwnStream", myId);
            stream.subscribe({
                next: (item) => {
                    console.log("received signalR message",item);
                    //send to wasm
                    const pointer = _malloc(item.length);
                    HEAPU8.set(item, pointer);
                    dynCall('viii', signalRInterop.callback, [pointer, item.length,0]);//0 is for NetworkEvent.Data
                },
                complete: () => {console.log("complete");dynCall('viii', signalRInterop.callback, [0, 0,2])},
                error: (err) => {console.log(err);dynCall('viii', signalRInterop.callback, [0, 0,2])}
            });
            const subject=new signalR.Subject();
            connection.send("ClientSendToServer",subject,arSessionId,myId);
            signalRInterop.subject=subject;
            connection.on("ConnectionEstablished",()=>{
                console.log("ConnectionEstablished");
                dynCall('viii', signalRInterop.callback, [0, 0,1]);//send connection event
            });
            await connection.invoke("NotifyServerOfClient",arSessionId,myId);

        }).catch(function (err) {
            return console.error(err.toString());
        });

    },
    SendByteArrayToSignalR:function(byteOffset, length){
        const myByteArray = new Uint8Array(buffer, byteOffset, length);
        signalRInterop.subject.next(myByteArray);
    },
    ProvideDataCallback:function(obj){
        console.log("ProvideDataCallback",obj);
        globalThis.signalRInterop={};
        signalRInterop.callback=obj;
    },

}
mergeInto(LibraryManager.library, signalRInterop);