const express = require('express');
const cors = require('cors');
require('dotenv').config();
const db = require('./Config/db.config');
const http = require('http');
const WebSocket = require('ws');

const app = express();
const server = http.createServer(app); // Server HTTP chung

// WebSocket server
const wss = new WebSocket.Server({ server });

const clients = new Map(); // Map: playerId => WebSocket

// Khi có client kết nối WebSocket
wss.on('connection', (ws, req) => {
    console.log('🔌 Một client đã kết nối WebSocket');

    // Lắng nghe tin đầu tiên: đăng ký playerId
    ws.once('message', (msg) => {
        try {
            const data = JSON.parse(msg);
            if (data.type === 'register') {
                const playerId = data.player_Id;
                clients.set(playerId, ws);
                console.log(`✅ Player ${playerId} đã đăng ký`);
                console.log("🧩 clients hiện tại:", Array.from(clients.keys()));

                // Tiếp tục lắng nghe các message khác
                ws.on('message', (msg) => {
                    try {
                        const parsed = JSON.parse(msg);

                        if (parsed.action === 'send_message') {
                            const { senderId, receiverId, content } = parsed;
                            console.log(`💬 ${senderId} → ${receiverId}: ${content}`);

                            const messageModel = require('./Models/Message.model');
                            messageModel.sendMessage(senderId, receiverId, content, (err, result) => {
                                if (err) {
                                    console.error('❌ Lỗi lưu message:', err);
                                    return;
                                }

                                console.log('✅ Message đã lưu vào DB:', result);

                                const payload = JSON.stringify({
                                    type: 'new_message',
                                    data: {
                                        senderId,
                                        content,
                                        timestamp: new Date().toISOString()
                                    }
                                });

                                const receiverSocket = clients.get(receiverId);
                                if (receiverSocket && receiverSocket.readyState === WebSocket.OPEN) {
                                    receiverSocket.send(payload);
                                    console.log(`📨 Gửi realtime tới receiver ${receiverId}`);
                                }

                                const senderSocket = clients.get(senderId);
                                if (senderSocket && senderSocket.readyState === WebSocket.OPEN) {
                                    senderSocket.send(payload);
                                    console.log(`🔁 Gửi realtime lại cho sender ${senderId}`);
                                }
                            });
                        }
                    } catch (e) {
                        console.error('❌ Lỗi xử lý message:', e.message);
                    }
                });

                // Xử lý ngắt kết nối
                ws.on('close', () => {
                    clients.delete(playerId);
                    console.log(`❌ Player ${playerId} ngắt kết nối`);
                });
            }
        } catch (e) {
            console.error('❌ Lỗi đăng ký playerId:', e.message);
        }
    });
});

// Express middleware & routes
// app.use(cors());
app.use(cors({
    origin: '*'
}));

app.use(express.json());

const authRoutes = require('./Routes/Auth.route');
const GameplayRoutes = require('./Routes/Gameplay.route');
const displayRoutes = require('./Routes/Display.route');
const roomRoutes = require('./Routes/Room.route');
const friendRoutes = require('./Routes/Friend.route');
const messageRoutes = require('./Routes/Message.route');

app.use('/api/auth', authRoutes);
app.use('/api/gameplay', GameplayRoutes);
app.use('/api/room', roomRoutes);
app.use('/api/display', displayRoutes);
app.use('/api/friend', friendRoutes);
app.use('/api/message', messageRoutes);

app.get('/', (req, res) => {
    res.send('🌐 Server HTTP + WebSocket đang chạy 🚀');
});

// Start HTTP server + WebSocket server
const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
    console.log(`🚀 Server HTTP + WebSocket đang chạy tại http://localhost:${PORT}`);
});
