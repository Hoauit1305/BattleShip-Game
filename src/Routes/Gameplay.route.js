const express = require('express');
const router = express.Router();
const { 
    placeShipController, 
    fireController, 
    fireWithBotController, 
    setidController, 
    setid1Controller, 
    placeBotShipController,
    getPositionController,
    fireWithPersonController
} = require('../Controllers/Gameplay.controller');

//API đặt tàu
router.post('/place-ship', placeShipController);

//API bắn tàu
router.post('/fire-ship', fireController);

//API bắn tàu với máy
router.post('/fire-ship/bot', fireWithBotController);

//API bắn tàu với người
router.post('/fire-ship/person', fireWithPersonController);

//API tạo id trận đấu
router.post('/create-gameid', setidController);

//API tạo id trận đấu
router.post('/create-gameid-fire-person', setid1Controller);

// API đặt tàu cho bot
router.post('/place-ship/bot', placeBotShipController);

// API hiện tàu trong panel của bot
router.post('/showship', getPositionController);
module.exports = router;