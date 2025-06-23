const express = require('express');
const router = express.Router();
const messageController = require('../Controllers/Message.controller');

// Gửi tin nhắn
router.post('/send', messageController.sendMessage);

// Lấy lịch sử chat với 1 người
router.get('/history/:receiverId', messageController.getChatHistory);

module.exports = router;
