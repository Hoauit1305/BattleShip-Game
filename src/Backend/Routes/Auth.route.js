const express = require('express');
const router = express.Router();
const { login, register } = require('../Controllers/Auth.controller');

//API Đăng nhập
router.post('/login', login);

//API Đăng ký
router.post('/register', register);

//API Quên mật khẩu
//API Đổi mật khẩu
module.exports = router;
