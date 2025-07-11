const express = require('express');
const cors = require('cors');
require('dotenv').config();
const db = require('./Config/db.config');
const http = require('http');
const WebSocket = require('ws');

const app = express();
const server = http.createServer(app);

// WebSocket server
const wss = new WebSocket.Server({ server });

const clients = new Map(); // Map: playerId => WebSocket
const readyPlayers = new Map(); // Map gameId → Set playerId đã sẵn sàng

wss.on('connection', (ws, req) => {
    console.log('🔌 Một client đã kết nối WebSocket');

    ws.once('message', (msg) => {
        try {
            const data = JSON.parse(msg);
            if (data.type === 'register') {
                const playerId = data.player_Id;
                clients.set(playerId, ws);
                console.log(`✅ Player ${playerId} đã đăng ký`);
                console.log("🧩 clients hiện tại:", Array.from(clients.keys()));

                ws.on('message', (msg) => {
                    try {
                        const parsed = JSON.parse(msg);

                        if (parsed.action === 'send_message') {
                            const { senderId, receiverId, content } = parsed;
                            console.log(`💬 ${senderId} → ${receiverId}: ${content}`);

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
                        }
                        else if (parsed.action === 'send_friend_request') {
                            const { fromId, toId, fromName } = parsed;

                            const payload = JSON.stringify({
                                type: 'friend_notify',
                                fromId,
                                fromName
                            });

                            const targetSocket = clients.get(toId);
                            if (targetSocket && targetSocket.readyState === WebSocket.OPEN) {
                                targetSocket.send(payload);
                                console.log(`🤝 Gửi lời mời kết bạn từ ${fromName} (id=${fromId}) đến ${toId}`);
                            }
                        }
                        // Khi một người tham gia phòng
                        else if (parsed.action === 'join_room') {
                            const { roomCode, playerId, playerName, role, targetId } = parsed;

                            const payload = JSON.stringify({
                                type: 'room_update',
                                action: 'join',
                                playerId,
                                playerName,
                                role
                            });

                            const targetSocket = clients.get(targetId);
                            if (targetSocket && targetSocket.readyState === WebSocket.OPEN) {
                                targetSocket.send(payload);
                                console.log(`🔔 ${playerName} đã vào phòng ${roomCode} → gửi đến ${targetId}`);
                            }
                        }
                        else if (parsed.action === 'leave_room') {
                            const { roomCode, playerId, playerName, role, targetId } = parsed;

                            const payload = JSON.stringify({
                                type: 'room_update',
                                action: 'leave',
                                playerId,
                                playerName,
                                role
                            });

                            const targetSocket = clients.get(targetId);
                            if (targetSocket && targetSocket.readyState === WebSocket.OPEN) {
                                targetSocket.send(payload);
                                console.log(`🚪 ${playerName} đã rời phòng ${roomCode} → gửi đến ${targetId}`);
                            }
                        }
                        else if (parsed.action === 'close_room') {
                            const { roomCode, ownerId, guestId } = parsed;

                            const payload = JSON.stringify({
                                type: 'room_update',
                                action: 'closed',
                                roomCode
                            });

                            [ownerId, guestId].forEach(id => {
                                const socket = clients.get(id);
                                if (socket && socket.readyState === WebSocket.OPEN) {
                                    socket.send(payload);
                                    console.log(`❌ Gửi tín hiệu đóng phòng tới player ${id}`);
                                }
                            });
                        }
                        else if (parsed.action === 'start_game') {
                            const { ownerId, guestId, roomCode, gameId } = parsed;

                            // Xóa trạng thái readyPlayers cũ khi bắt đầu game mới
                            if (readyPlayers.has(gameId)) {
                                readyPlayers.delete(gameId);
                                console.log(`🧹 Xóa trạng thái readyPlayers cho game ${gameId}`);
                            }

                            const payload = JSON.stringify({
                                type: 'goto_place_ship',
                                roomCode: roomCode,
                                gameId: gameId,
                                ownerId: ownerId,
                                guestId: guestId,
                                message: 'Cả hai đã sẵn sàng, chuyển đến scene đặt tàu!'
                            });

                            [ownerId, guestId].forEach(pid => {
                                const wsClient = clients.get(pid);
                                if (wsClient && wsClient.readyState === WebSocket.OPEN) {
                                    wsClient.send(payload);
                                    console.log(`🚀 Gửi goto_place_ship tới player ${pid} (gameId: ${gameId})`);
                                }
                            });
                        }
                        else if (parsed.action === 'ready_place_ship') {
                            const { gameId, playerId, opponentId } = parsed;
                            console.log(`📦 Player ${playerId} đã sẵn sàng đặt tàu (game ${gameId})`);

                            // Khởi tạo lại set cho gameId mới
                            if (!readyPlayers.has(gameId)) {
                                readyPlayers.set(gameId, new Set());
                            }

                            // Thêm playerId vào set ready
                            const currentReadySet = readyPlayers.get(gameId);
                            currentReadySet.add(playerId);
                            console.log(`🕒 readyPlayers cho game ${gameId}: ${Array.from(currentReadySet)}`);

                            // Kiểm tra nếu cả hai người chơi đều sẵn sàng trong cùng một game
                            const allPlayers = new Set([playerId, opponentId]);
                            if (currentReadySet.size === 2 && [...currentReadySet].every(id => allPlayers.has(id))) {
                                const payload = JSON.stringify({
                                    type: 'start_countdown'
                                });

                                const socketA = clients.get(playerId);
                                const socketB = clients.get(opponentId);

                                if (socketA && socketA.readyState === WebSocket.OPEN) socketA.send(payload);
                                if (socketB && socketB.readyState === WebSocket.OPEN) socketB.send(payload);

                                console.log(`🚀 Bắt đầu đếm ngược cho game ${gameId} với ${playerId} và ${opponentId}`);
                                readyPlayers.delete(gameId); // Xóa trạng thái sau khi hoàn tất
                            } else {
                                console.log(`⏳ Chờ ${opponentId} sẵn sàng cho game ${gameId}, hiện tại: ${currentReadySet.size}/2`);
                            }
                        }
                        // 👉 Xử lý chuyển lượt giữa người chơi
                        else if (data.type === 'switch_turn') {
                        const { fromPlayerId, toPlayerId } = data;
                        const targetSocket = clients.get(toPlayerId);
                        
                        if (targetSocket && targetSocket.readyState === WebSocket.OPEN) {
                            targetSocket.send(JSON.stringify({
                            type: 'switch_turn',
                            fromPlayerId,
                            toPlayerId
                            }));
                            console.log(`🔄 Gửi switch_turn từ ${fromPlayerId} → ${toPlayerId}`);
                        } else {
                            console.warn(`⚠️ Không tìm thấy socket hoặc socket đóng cho toPlayerId: ${toPlayerId}`);
                        }
                        }
                    } catch (e) {
                        console.error('❌ Lỗi xử lý message:', e.message);
                    }
                });

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

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
    console.log(`🚀 Server HTTP + WebSocket đang chạy tại http://localhost:${PORT}`);
});