const express = require('express');
const router = express.Router();
const { placeShipController } = require('../Controllers/Gameplay.controller');

//API đặt tàu
router.post('/place-ship', placeShipController);

module.exports = router;