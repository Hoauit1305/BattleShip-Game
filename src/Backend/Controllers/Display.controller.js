const jwt = require('jsonwebtoken');
const { displayUser } = require('../Models/Display.model');

// Hiển thị dữ liệu user
const display = (req, res) => {
    const authHeader = req.headers.authorization; //lấy token từ header
    console.log("📌 Token nhận được từ client:", authHeader);

    if (!authHeader || !authHeader.startsWith("Bearer ")) { // kiểm tra token có hợp lệ không
        return res.status(403).json({ message: "Không có token hoặc token không hợp lệ!" });
    }

    const token = authHeader.split(" ")[1];
    try {
        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        const username = decoded.username;

        displayUser(username, (err, userData) => {
            if (err) {
                console.error(err);
                return res.status(500).json({ message: 'Lỗi server' });
            }

            if (!userData) {
                return res.status(404).json({ message: "User not found!" });
            }

            // Trả về dữ liệu người dùng
            res.json({
                name: userData.Name,
                id: userData.Player_Id,
                status: userData.Status
            });
        });
    } catch (error) {
        console.error(error);
        return res.status(403).json({ message: "Token không hợp lệ!" });
    }
};

module.exports = { display };
