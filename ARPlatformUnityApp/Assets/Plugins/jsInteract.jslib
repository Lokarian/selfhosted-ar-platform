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
    var url = window.location.protocol + "//" + window.location.host + "/api/hub";
    var connection = new signalR.HubConnectionBuilder().withUrl(url,{
        accessTokenFactory: async () => {
          return localStorage.getItem("access_token");
        }
      }).withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol()).withAutomaticReconnect().build();
    connection.start().then(async () => {
      const connectionId = await connection.invoke("InitializeConnection", []);
      console.log('SignalR Connected!');
      var urlParams = new URLSearchParams(window.location.search);
      var arSessionId = urlParams.get('arSessionId');
      const response = await fetch("/api/Ar/JoinArSession",{method:"POST",headers:{"Content-Type": "application/json","userconnectionid":connectionId,"Authorization":"Bearer "+localStorage.getItem('access_token')},body:JSON.stringify({role:0,arSessionId:arSessionId})});
      if(!response.ok){
        console.log("Error joining AR session",response);
      }
      const member = await response.json();
      const myId = member.id;
      signalRInterop.myId=myId;
      
      const stream=connection.stream("SubscribeToTopic", myId);
      stream.subscribe({
        next: (item) => {
          console.log("received signalR message",item);
          //send to wasm
          const pointer = _malloc(item.payload.length);
          HEAPU8.set(item.payload, pointer);
          dynCall('viii', signalRInterop.callback, [pointer, item.payload.length,0]);//0 is for NetworkEvent.Data
        },
        complete: () => console.log("complete"),
        error: (err) => console.log(err)
      });
      const subject=new signalR.Subject();
      connection.send("PublishStreamWithContextUserId",subject,arSessionId,myId);
      signalRInterop.subject=subject;
      dynCall('viii', signalRInterop.callback, [0, 0,1]);
      
    }).catch(function (err) {
      return console.error(err.toString());
    });
    
  },
  SendByteArrayToSignalR:function(byteOffset, length){
    const myByteArray = new Uint8Array(buffer, byteOffset, length);
    const data={networkEvent:0,clientId:0,payload:myByteArray,senderArMemberId:signalRInterop.myId};
    signalRInterop.subject.next(data);
  },
  ProvideDataCallback:function(obj){
    console.log("ProvideDataCallback",obj);
    globalThis.signalRInterop={};
    signalRInterop.callback=obj;
  },
   
}
mergeInto(LibraryManager.library, signalRInterop);