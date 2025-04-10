const jwt = require('jsonwebtoken');
const { findUserByUsername, createUser,  findUserByEmail, updatePasswordByUsername } = require('../Models/Auth.model');

//Đăng nhập
const login = (req, res) => {
    const { username, password } = req.body;

    findUserByUsername(username, (err, user) => {
        if (err) {
            console.error(err);
            return res.status(500).json({ message: 'Lỗi server' });
        }

        if (!user) {
            return res.status(401).json({ message: 'Sai tài khoản hoặc mật khẩu' });
        }

        // So sánh mật khẩu trực tiếp
        if (password !== user.Password) {
            return res.status(401).json({ message: 'Sai tài khoản hoặc mật khẩu' });
        }

        // Tạo token
        const token = jwt.sign(
            { id: user.Player_Id, username: user.Username },
            'your_secret_key', // thay bằng secret key thật sự
            { expiresIn: '1h' }
        );

        res.json({ message: 'Đăng nhập thành công!', token });
    });
};


//Đăng ký
const register = (req, res) => {
    const { username, password, email } = req.body;

    // Kiểm tra trống
    if (!username || !password || !email) {
        return res.status(400).json({ message: 'Vui lòng nhập đầy đủ tài khoản và mật khẩu' });
    }

    // Kiểm tra username đã tồn tại chưa
    findUserByUsername(username, (err, existingUser) => {
        if (err) {
            console.error(err);
            return res.status(500).json({ message: 'Lỗi server' });
        }

        if (existingUser) {
            return res.status(400).json({ message: 'Tên tài khoản đã tồn tại' });
        }

        // Thêm người dùng mới
        createUser(username, password, email, (err, newUserId) => {
            if (err) {
                console.error(err);
                return res.status(500).json({ message: 'Lỗi server khi tạo tài khoản' });
            }

            res.json({ message: 'Đăng ký thành công!', userId: newUserId });
        });
    });
};

// Quên mật khẩu
const forgotPassword = (req, res) => {
    const { email } = req.body;

    findUserByEmail(email, (err, user) => {
        if (err) return res.status(500).json({ message: 'Lỗi server' });
        if (!user) return res.status(404).json({ message: 'Không tìm thấy người dùng' });

        const tempPassword = Math.random().toString(36).slice(-8); // mật khẩu tạm thời

        updatePasswordByEmail(email, tempPassword, (err) => {
            if (err) return res.status(500).json({ message: 'Lỗi khi cập nhật mật khẩu' });

            res.json({
                message: 'Mật khẩu tạm thời đã được tạo',
                tempPassword, // Bạn có thể thay bằng gửi mail thật nếu muốn
            });
        });
    });
};

// Đổi mật khẩu
const changePassword = (req, res) => {
    const { username, oldPassword, newPassword } = req.body;

    findUserByUsername(username, (err, user) => {
        if (err) return res.status(500).json({ message: 'Lỗi server' });
        if (!user) return res.status(404).json({ message: 'Không tìm thấy người dùng' });

        if (oldPassword !== user.Password) {
            return res.status(400).json({ message: 'Mật khẩu cũ không đúng' });
        }

        updatePasswordByUsername(username, newPassword, (err) => {
            if (err) return res.status(500).json({ message: 'Lỗi khi cập nhật mật khẩu' });

            res.json({ message: 'Đổi mật khẩu thành công' });
        });
    });
};

// Logout
const { deleteToken } = require('../Models/Auth.model');

const logout = (req, res) => {
    const token = req.headers.authorization?.split(' ')[1];

    if (!token) {
        return res.status(400).json({ message: 'Không tìm thấy token!' });
    }

    deleteToken(token, (err) => {
        if (err) {
            console.error(err);
            return res.status(500).json({ message: 'Lỗi server khi xoá token' });
        }

        return res.status(200).json({ message: 'Đăng xuất thành công!' });
    });
};


module.exports = { login, register, forgotPassword,
    changePassword,
    logout, };
