const express = require('express');
const router = express.Router();
const { createRoomController, closeRoomController, findRoomController, leaveRoomController } = require('../Controllers/Room.controller');

// API Tạo phòng
router.post('/create-room', createRoomController);
//API Đóng phòng
router.post('/close-room', closeRoomController);
//API Tìm phòng
router.post('/find-room', findRoomController);
//API Rời phòng
router.post('/leave-room', leaveRoomController);
module.exports = router;
