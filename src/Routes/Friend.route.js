const express = require('express');
const router = express.Router();
const friendController = require('../Controllers/Friend.controller');

// Gửi lời mời kết bạn
router.post('/request', friendController.sendFriendRequest);

// Chấp nhận lời mời kết bạn
router.post('/accept', friendController.acceptFriendRequest);

// Từ chối lời mời kết bạn
router.post('/reject', friendController.rejectFriendRequest);

// Lấy danh sách bạn bè
router.post('/list', friendController.getFriends);

// Lấy các lời mời kết bạn đang chờ
router.post('/pending', friendController.getPendingRequests);

// Tìm bạn theo Id
router.post('/search/:playerId', friendController.searchPlayer);


module.exports = router;