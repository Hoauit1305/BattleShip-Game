const express = require('express');
const router = express.Router();
const { display } = require('../Controllers/Display.controller');

// API Hiển thị dữ liệu
router.post('/user', display);

module.exports = router;
