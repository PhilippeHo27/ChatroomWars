const WebSocket = require('ws');
const server = new WebSocket.Server({ port: 8080 });

console.log('WebSocket server is running on port 8080');

server.on('connection', (socket) => {
    console.log('Client connected');

    socket.on('message', (message) => {
        try {
            const parsedMessage = JSON.parse(message);
            console.log('Received:', parsedMessage);

            switch (parsedMessage.type) {
                case 'chat':
                    broadcastMessage(parsedMessage);
                    break;
                case 'position':
                    broadcastMessage(parsedMessage);
                    break;
                default:
                    console.log('Unknown message type:', parsedMessage.type);
            }
        } catch (error) {
            console.error('Error processing message:', error);
        }
    });

    socket.on('close', () => {
        console.log('Client disconnected');
    });
});

function broadcastMessage(message) {
    server.clients.forEach((client) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(JSON.stringify(message));
        }
    });
}
