const jwt = require('jsonwebtoken');
const messageModel = require('../Models/Message.model');

// Hàm lấy user id từ token
const getUserIdFromToken = (req) => {
    const authHeader = req.headers.authorization;
    if (!authHeader || !authHeader.startsWith("Bearer ")) return null;
    try {
        const token = authHeader.split(" ")[1];
        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        return decoded.id;
    } catch {
        return null;
    }
};

// API gửi tin nhắn
const sendMessage = (req, res) => {
    const senderId = getUserIdFromToken(req);
    const { receiverId, content } = req.body;

    if (!senderId) return res.status(403).json({ message: "Token không hợp lệ!" });
    if (!receiverId || !content) return res.status(400).json({ message: "Thiếu receiverId hoặc content!" });

    messageModel.sendMessage(senderId, receiverId, content, (err, result) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi gửi tin nhắn', detail: err });
        res.json(result);
    });
};

// API lấy lịch sử chat
const getChatHistory = (req, res) => {
    const userId1 = getUserIdFromToken(req);
    const userId2 = req.params.receiverId;

    if (!userId1) return res.status(403).json({ message: "Token không hợp lệ!" });
    if (!userId2) return res.status(400).json({ message: "Thiếu receiverId!" });

    messageModel.getChatHistory(userId1, userId2, (err, result) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi lấy lịch sử chat', detail: err });
        res.json(result);
    });
};

module.exports = {
    sendMessage,
    getChatHistory
};
