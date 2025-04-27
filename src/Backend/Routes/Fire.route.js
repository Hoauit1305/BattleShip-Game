const express = require('express');
const router = express.Router();
const { fireController } = require('../Controllers/Fire.controller');

// API bắn tàu
router.post('/fire', fireController);

module.exports = router;