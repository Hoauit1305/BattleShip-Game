const express = require('express');
const router = express.Router();
const { placeShipController, fireController, fireWithBotController, setidController } = require('../Controllers/Gameplay.controller');

//API đặt tàu
router.post('/place-ship', placeShipController);

//API bắn tàu
router.post('/fire-ship', fireController);
//API bắn tàu với máy
router.post('/fire-ship/bot', fireWithBotController);
//API tạo id trận đấu
router.post('/set-id', setidController);
module.exports = router;