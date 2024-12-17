const WebSocket = require('ws');

// Constants and State
const SERVER_CONFIG = {
    port: 8080,
    timeSync: {
        interval: 1000,
        enabled: false
    }
};

const STATE = {
    usedClientIds: new Set(),
    clients: new Map(),
    clientConnections: new Map()
};

// Main Server Setup and Event Handling
const server = new WebSocket.Server({ port: SERVER_CONFIG.port });

server.on('connection', handleNewConnection);

// if (SERVER_CONFIG.timeSync.enabled) {
//     setInterval(sendTimeSync, SERVER_CONFIG.timeSync.interval);
// }

// Connection Handler Functions
function handleNewConnection(socket) {
    const clientId = getNextAvailableClientId();
    setupNewClient(clientId, socket);
    setupClientEventListeners(clientId, socket);
}

function setupNewClient(clientId, socket) {
    console.log(`New connection attempt. Current clients: ${STATE.clients.size}`);
    console.log('Active client IDs:', Array.from(STATE.clients.keys()));

    STATE.clients.set(clientId, socket);
    STATE.clientConnections.set(clientId, createClientConnectionInfo());

    console.log(`Client connected. Assigned ID: ${clientId}`);
    sendClientIdAssignment(socket, clientId);
}

function setupClientEventListeners(clientId, socket) {
    socket.on('message', (message) => handleClientMessage(clientId, message));
    socket.on('open', () => handleClientConnection(clientId));
    socket.on('close', (code) => handleClientDisconnection(clientId));
}

function handleClientMessage(clientId, message) {
    try {
        const data = JSON.parse(message);
        logMessageReceived(clientId, data);
        updateClientStats(clientId, data);
        broadcastToClients(clientId, data);
    } catch (error) {
        handleMessageError(clientId, message, error);
    }
}

// Message Handling Functions
function logMessageReceived(clientId, data) {
    console.log('Received data structure:', data);
    if (data.Type === 0) {
        console.log(`Client ${clientId}: ${data.Text}`);
    } else {
        console.log(`Received non-chat message from Client ${clientId}:`, data);
    }
}

function updateClientStats(clientId, data) {
    const clientInfo = STATE.clientConnections.get(clientId);
    if (clientInfo) {
        clientInfo.lastMessageTime = Date.now();
        clientInfo.messageCount++;
        if (data.Type === 0) {
            clientInfo.chatCount++;
        }
    }
}

function broadcastToClients(senderId, data) {
    const broadcastData = {
        ...data,
        SenderId: senderId
    };
    console.log('Broadcasting structure:', broadcastData);
    broadcastMessage(broadcastData);
}

// Utility Functions
function createClientConnectionInfo() {
    return {
        connectTime: Date.now(),
        lastMessageTime: null,
        messageCount: 0,
        chatCount: 0
    };
}

function getNextAvailableClientId() {
    let id = 1;
    while (STATE.usedClientIds.has(id)) {
        id++;
    }
    STATE.usedClientIds.add(id);
    return id;
}

function releaseClientId(id) {
    STATE.usedClientIds.delete(id);
}

function sendClientIdAssignment(socket, clientId) {
    socket.send(JSON.stringify({
        type: 'id_assign',
        senderId: 0,
        sequence: 0,
        clientId: clientId
    }));
}

function broadcastMessage(message) {
    STATE.clients.forEach((client, id) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(Buffer.from(JSON.stringify(message)));
        } else if (client.readyState === WebSocket.CLOSED) {
            cleanupClosedClient(id);
        }
    });
}

function cleanupClosedClient(id) {
    STATE.clients.delete(id);
    STATE.clientConnections.delete(id);
    releaseClientId(id);
}

function handleClientConnection(clientId) {
    const socket = STATE.clientConnections.get(clientId);
    socket.send(JSON.stringify({
        type: "init",
        clientId: clientId
    }));
}
function handleClientDisconnection(clientId) {
    const clientInfo = STATE.clientConnections.get(clientId);
    logClientDisconnection(clientId, clientInfo);
    cleanupClosedClient(clientId);
}

function logClientDisconnection(clientId, clientInfo) {
    console.log(`Client ${clientId} disconnected:`, {
        connectTime: clientInfo?.connectTime,
        disconnectTime: Date.now(),
        totalMessages: clientInfo?.messageCount || 0,
        totalChatMessages: clientInfo?.chatCount || 0,
        connectionDuration: clientInfo ? Date.now() - clientInfo.connectTime : 0
    });
}

function handleMessageError(clientId, message, error) {
    console.error(`Error processing message from Client ${clientId}:`, error);
    console.error('Raw message:', message);
}

function sendTimeSync() {
    const timeSync = {
        type: "timeSync",
        serverTime: Date.now()
    };
    broadcastMessage(timeSync);
}
