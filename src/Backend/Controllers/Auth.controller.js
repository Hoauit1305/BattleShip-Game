const jwt = require('jsonwebtoken');
const MailService = require('../service/mail.service');
const { findUserByUsername, findUserByEmail, createUser, forgotPassword, updatePasswordByUsername, setUserOffline, setUserOnline, setName } = require('../Models/Auth.model');
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

        setUserOnline(username, (err) => {
            if (err) {
                console.error(err);
                return res.status(500).json({ message: 'Lỗi server khi cập nhật trạng thái online' });
            }

            // Tạo token
            const token = jwt.sign(
                { id: user.Player_Id, username: user.Username },
                process.env.JWT_SECRET, 
                { expiresIn: '1h' }
            );

            res.json({ message: 'Đăng nhập thành công!', token });
        });
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

        findUserByEmail(email, (err, existingEmailUser) => {
            if (err) {
                console.error(err);
                return res.status(500).json({ message: 'Lỗi server' });
            }

            if (existingEmailUser) {
                return res.status(400).json({ message: 'Email đã được sử dụng' });
            }

            // Nếu username và email đều chưa có, thì tạo user mới
            createUser(username, password, email, (err, newUserId) => {
                if (err) {
                    console.error(err);
                    return res.status(500).json({ message: 'Lỗi server khi tạo tài khoản' });
                }

                res.json({ message: 'Đăng ký thành công!', userId: newUserId });
            });
        });
    });
};

//Quên mật khẩu
const forgotpw = (req, res) => {
    const { username, email } = req.body;

    forgotPassword(username, email, (err, user) => {
        if (err) {
            console.error(err);
            return res.status(500).json({ message: 'Lỗi server' });
        }

        if (!user) {
            return res.status(404).json({ message: 'Thông tin không đúng' });
        }

        // Gửi email chứa mật khẩu cũ
        MailService.sendMail({
            to: email,
            subject: 'Khôi phục mật khẩu',
            html:   
            `<!DOCTYPE html>
            <html>
                <head>
                    <meta charset="UTF-8">
                    <title>Khôi phục mật khẩu</title>
                </head>
                <body style="font-family: Arial, sans-serif; background-color: #f7f7f7; padding: 20px; color: #000000;">
                    <div style="max-width: 500px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                        <h2 style="color: #d62828;">🔐 Yêu cầu khôi phục mật khẩu</h2>
                        <p>Xin chào <strong>${user.Username}</strong>,</p>
                        <p>Bạn đã yêu cầu lấy lại mật khẩu cho tài khoản của mình. Dưới đây là thông tin đăng nhập:</p>
                        <div style="background-color: #f1f1f1; padding: 15px; border-radius: 5px; margin-top: 10px; margin-bottom: 10px;">
                            <p><strong>Tài khoản:</strong> ${user.Username}</p>
                            <p><strong>Mật khẩu:</strong> ${user.Password}</p>
                        </div>
                        <p>Vui lòng đăng nhập lại và đổi mật khẩu ngay sau khi truy cập để đảm bảo an toàn cho tài khoản của bạn.</p>
                        <p>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email này hoặc liên hệ với bộ phận hỗ trợ.</p>
                        <p style="margin-top: 30px;">Trân trọng,<br>Team 15 hỗ trợ hệ thống</p>
                    </div>
                </body>
            </html>` 
        }).then(() => {
            return res.status(200).json({ message: 'Mật khẩu đã được gửi đến email!' });
        }).catch(err => {
            console.error(err);
            return res.status(500).json({ message: 'Lỗi gửi email' });
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

// Đăng xuất
const logout = (req, res) => {
    const authHeader = req.headers.authorization;

    if (!authHeader) {
        return res.status(401).json({ message: 'Không có token' });
    }

    const token = authHeader.split(' ')[1]; // Bearer <token>

    jwt.verify(token, process.env.JWT_SECRET, (err, decoded) => {
        if (err) {
            console.error(err);
            return res.status(401).json({ message: 'Token không hợp lệ' });
        }

        const username = decoded.username;

        setUserOffline(username, (err) => {
            if (err) {
                console.error(err);
                return res.status(500).json({ message: 'Lỗi server khi đăng xuất' });
            }

            res.json({ message: 'Đăng xuất thành công!' });
        });
    });
};
// Nhập tên
const chooseName = (req,res)=>{
    const authHeader = req.headers.authorization; //lấy token từ header
    console.log("📌 Token nhận được từ client:", authHeader);
                    
    if (!authHeader || !authHeader.startsWith("Bearer ")) { // kiểm tra token có hợp lệ không
        return res.status(403).json({ message: "Không có token hoặc token không hợp lệ!" });
    }
    const token = authHeader.split(" ")[1];
    const decoded = jwt.verify(token, process.env.JWT_SECRET);
    const id = decoded.Id;  // Id lấy từ token
    const {name} = req.body;
    setName(name,id, (err) => {
        if (err) {
            console.error(err);
            return res.status(500).json({ message: 'Lỗi server khi đặt tên' });
        }
        res.json({ message: 'Đặt tên thành công!' });
    });
}

module.exports = { login, register, forgotpw, changePassword, logout, chooseName };
