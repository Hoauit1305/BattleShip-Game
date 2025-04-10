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
const forgotPassword = (username, email, callback) => {
    const query = 'SELECT * FROM Player WHERE Username = ? AND Email = ?';
    db.query(query, [username, email], (err, results) => {
        if (err) return callback(err, null);
        callback(null, results[0]);
  });
}

//Fuction này dùng cho đổi mật khẩu
const updatePasswordByUsername = (username, newPassword, callback) => {
    const query = 'UPDATE Player SET Password = ? WHERE Username = ?';
    db.query(query, [newPassword, username], (err, result) => {
        if (err) return callback(err);
        callback(null);
    });
};

// Function này dùng cho logout: cập nhật trạng thái Offline
const setUserOffline = (username, callback) => {
    const query = 'UPDATE Player SET Status = "offline" WHERE Username = ?';
    db.query(query, [username], (err, result) => {
        if (err) return callback(err);
        callback(null);
    });
};
// Function này dùng cho login: cập nhật trạng thái Online
const setUserOnline = (username, callback) => {
    const query = 'UPDATE Player SET Status = "online" WHERE Username = ?';
    db.query(query, [username], (err, result) => {
        if (err) return callback(err);
        callback(null);
    });
};

module.exports = {
    findUserByUsername,
    createUser,
    forgotPassword,
    updatePasswordByUsername,
    setUserOffline,
    setUserOnline
};


