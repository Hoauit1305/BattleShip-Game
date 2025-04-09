const express = require('express');
const router = express.Router();
const { login, register, forgotpw } = require('../Controllers/Auth.controller');

//API Đăng nhập
router.post('/login', login);

//API Đăng ký
router.post('/register', register);

//API Quên mật khẩu
router.post('/forgot-password', forgotpw);
//API Đổi mật khẩu
module.exports = router;
