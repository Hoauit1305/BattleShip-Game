const express = require('express');
const cors = require('cors');
require('dotenv').config();
const db = require('./Config/db.config');
const http = require('http');
const { Server } = require('socket.io'); // dùng socket.io thay vì ws

// Tạo app Express
const app = express();
const server = http.createServer(app); // vẫn giữ createServer

// Khởi tạo Socket.IO server
const io = new Server(server, {
    cors: {
        origin: '*', // cho phép tất cả, khi deploy thì chỉnh lại domain cụ thể
    }
});

// Map lưu userId => socketId
const onlineUsers = new Map();

// Socket.IO logic
io.on('connection', (socket) => {
    console.log('Một client đã kết nối Socket.IO:', socket.id);

    // Lắng nghe user đăng ký userId
    socket.on('register', (userId) => {
        onlineUsers.set(userId, socket.id);
        console.log(`User ${userId} online với socketId ${socket.id}`);
    });

    // Lắng nghe gửi message
    socket.on('send_message', (data) => {
        const { senderId, receiverId, content } = data;
        console.log(`Tin nhắn từ ${senderId} tới ${receiverId}: ${content}`);

        // Gửi cho receiver nếu online
        const receiverSocketId = onlineUsers.get(receiverId);
        if (receiverSocketId) {
            io.to(receiverSocketId).emit('receive_message', {
                senderId,
                content,
                timestamp: new Date().toISOString(),
            });
            console.log(`Đã gửi realtime tới ${receiverId}`);
        } else {
            console.log(`User ${receiverId} offline, không gửi realtime`);
        }

        // Lưu DB (gọi model)
        const messageModel = require('./Models/Message.model');
        messageModel.sendMessage(senderId, receiverId, content, (err, result) => {
            if (err) {
                console.error('Lỗi lưu message:', err);
            } else {
                console.log('Message đã lưu:', result);
            }
        });
    });

    // Khi user disconnect
    socket.on('disconnect', () => {
        console.log('Client disconnect:', socket.id);

        // Xóa user ra khỏi onlineUsers
        for (const [userId, socketId] of onlineUsers.entries()) {
            if (socketId === socket.id) {
                onlineUsers.delete(userId);
                console.log(`User ${userId} offline`);
                break;
            }
        }
    });
});

// Import routes
const authRoutes = require('./Routes/Auth.route');
const GameplayRoutes = require('./Routes/Gameplay.route');
const displayRoutes = require('./Routes/Display.route');
const roomRoutes = require('./Routes/Room.route');
const friendRoutes = require('./Routes/Friend.route');
const messageRoutes = require('./Routes/Message.route');

// Middleware
app.use(cors());
app.use(express.json());

// Routes
app.use('/api/auth', authRoutes);
app.use('/api/gameplay', GameplayRoutes);
app.use('/api/room', roomRoutes);
app.use('/api/display', displayRoutes);
app.use('/api/friend', friendRoutes);
app.use('/api/message', messageRoutes);

// Test route
app.get('/', (req, res) => {
    res.send('Server HTTP + Socket.IO đang chạy 🚀');
});

// Port
const PORT = process.env.PORT || 3000;

// Start HTTP + Socket.IO server
server.listen(PORT, () => {
    console.log(`Server HTTP + Socket.IO đang chạy tại http://localhost:${PORT}`);
});
