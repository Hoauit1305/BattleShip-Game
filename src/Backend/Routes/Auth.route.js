const express = require('express');
const router = express.Router();
const { login, register, changePassword, logout } = require('../Controllers/Auth.controller');

//API Đăng nhập
router.post('/login', login);

//API Đăng ký
router.post('/register', register);

//API Quên mật khẩu
router.post('/forgot-password', forgotpw);

//API Đổi mật khẩu
router.post('/change-password', changePassword);

//API Logout
router.post('/logout', logout);

module.exports = router;
