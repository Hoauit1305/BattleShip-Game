const express = require('express');
const cors = require('cors');
require('dotenv').config();
const db = require('./Config/db.config');
const http = require('http');
const WebSocket = require('ws');

// Tạo app Express
const app = express();
const server = http.createServer(app); // <== thay vì app.listen, dùng http.createServer

// Khởi tạo WebSocket server
const wss = new WebSocket.Server({ server }); // <== WebSocket sẽ cùng chạy trên HTTP server

// WebSocket logic
wss.on('connection', (ws) => {
  console.log('Một client đã kết nối WebSocket');

  ws.on('message', (message) => {
    console.log('Nhận tin nhắn:', message);

    const data = JSON.parse(message);

    if (data.type === 'join_room') {
      // Gắn thông tin room và tên người dùng vào client
      ws.roomCode = data.roomCode;
      ws.username = data.guestName;

      // Broadcast đến tất cả client trong cùng phòng
      wss.clients.forEach((client) => {
        if (
          client.readyState === WebSocket.OPEN &&
          client.roomCode === data.roomCode
        ) {
          client.send(JSON.stringify({
            type: 'room_updated',
            roomCode: data.roomCode,
            message: `${data.guestName} đã tham gia phòng.`,
            guestName: data.guestName
          }));
        }
      });
    }
  });
});

// Import routes
const authRoutes = require('./Routes/Auth.route');
const GameplayRoutes = require('./Routes/Gameplay.route');
const botRoutes = require('./Routes/Bot.route'); // Thêm route mới cho Bot
const displayRoutes = require('./Routes/Display.route');
const roomRoutes = require('./Routes/Room.route');

// Middleware
app.use(cors());
app.use(express.json());

//Routes
app.use('/api/auth', authRoutes);
app.use('/api/gameplay', GameplayRoutes);
app.use('/api/bot', botRoutes); // Thêm route mới cho Bot
app.use('/api/room', roomRoutes);
app.use('/api/display', displayRoutes);

// Test route
app.get('/', (req, res) => {
  res.send('Server đang chạy 🚀');
});

// Port
const PORT = process.env.PORT || 3000;

// Start cả HTTP + WebSocket server
server.listen(PORT, () => {
  console.log(`Server HTTP + WebSocket đang chạy tại http://localhost:${PORT}`);
});