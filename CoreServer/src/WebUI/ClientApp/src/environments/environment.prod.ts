export const environment = {
  production: true,
  backendPort: undefined,
  iceConfiguration: {
    iceServers: [
      {
        urls: 'stun:stun.l.google.com:19302'
      },
      {
        urls: 'turn:93.195.134.173:3478',
        username: 'test',
        credential: 'test123'
      }
    ]
  }

};
