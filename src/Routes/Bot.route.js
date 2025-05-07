const express = require('express');
const router = express.Router();
const { placeBotShipController } = require('../Controllers/Bot.controller');

// API đặt tàu cho bot
router.post('/place-ship', placeBotShipController);

module.exports = router;