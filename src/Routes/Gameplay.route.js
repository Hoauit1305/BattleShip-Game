const express = require('express');
const router = express.Router();
const { 
    placeShipController, 
    fireController, 
    fireWithBotController, 
    setidController, 
    placeBotShipController,
    getPositionController
} = require('../Controllers/Gameplay.controller');

//API đặt tàu
router.post('/place-ship', placeShipController);

//API bắn tàu
router.post('/fire-ship', fireController);

//API bắn tàu với máy
router.post('/fire-ship/bot', fireWithBotController);

//API tạo id trận đấu
router.post('/create-gameid', setidController);

// API đặt tàu cho bot
router.post('/place-ship/bot', placeBotShipController);

// API hiện tàu trong panel của bot
router.post('/showship', getPositionController);
module.exports = router;