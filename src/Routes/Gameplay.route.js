const express = require('express');
const router = express.Router();
const { placeShipController, fireController } = require('../Controllers/Gameplay.controller');

//API đặt tàu
router.post('/place-ship', placeShipController);

//API bắn tàu
router.post('/fire-ship', fireController);
module.exports = router;