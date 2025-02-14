const WebSocket = require('ws');
const express = require('express');
const app = express();
const path = require('path');
const ip = require('ip');

const webPort = 3000;
app.use(express.static(path.join(__dirname, 'public')));

// WebSocket-Server mit reduzierten Logs
const wss = new WebSocket.Server({ port: 8080 });
const clients = new Set();

wss.on('connection', function connection(ws) {
    clients.add(ws);
    
    ws.on('message', function incoming(message) {
        try {
            const data = JSON.parse(message);
            // Broadcast an alle anderen Clients
            for (let client of clients) {
                if (client !== ws && client.readyState === WebSocket.OPEN) {
                    client.send(JSON.stringify(data));
                }
            }
        } catch (error) {
            console.error('Nachrichtenverarbeitung fehlgeschlagen');
        }
    });
    
    ws.on('close', () => clients.delete(ws));
});

// Einmalige Serverstart-Information
app.listen(webPort, '0.0.0.0', () => {
    const localIP = ip.address();
    console.log(`
Server-Info:
- Web: http://${localIP}:${webPort}
- WebSocket: ws://${localIP}:8080`);
});