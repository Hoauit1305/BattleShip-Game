const db = require('../Config/db.config');

// Function này dùng để hiển thị các dữ liệu cần thiết.
const displayUser = (username, callback) => {
    const query = 'SELECT Name, Player_Id, Status FROM Player WHERE Username = ?';
    db.query(query, [username], (err, result) => {
        if (err) return callback(err, null);
        callback(null, result[0]); // Trả về kết quả người dùng
    });
};

module.exports = { displayUser };
