const db = require('../Config/db.config');

//Fuction này dùng cho đăng nhập
const findUserByUsername = (username, callback) => {
    const query = 'SELECT * FROM Player WHERE Username = ?';
    db.query(query, [username], (err, results) => {
        if (err) return callback(err, null);
        callback(null, results[0]);
    });
};

//Fuction này dùng cho đăng ký
const createUser = (username, password, email, callback) => {
    const query = 'INSERT INTO Player (Username, Password, Email) VALUES (?, ?, ?)';
    db.query(query, [username, password, email], (err, result) => {
        if (err) return callback(err);
        callback(null, result.insertId);
    });
};

//Fuction này dùng cho quên mật khẩu
const forgotPassword = (username, email) => {
    const query = 'SELECT * FROM Player WHERE Username = ? AND Email = ?';
    db.query(query, [username, email], (err, results) => {
    if (err) return callback(err, null);
    if (results.length === 0) return callback(null, null); // Không tìm thấy user
    callback(null, results[0]);
  });
}
//Fuction này dùng cho đổi mật khẩu

module.exports = { findUserByUsername, createUser, forgotPassword};
