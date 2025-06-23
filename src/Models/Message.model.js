const db = require('../Config/db.config');

// Gửi tin nhắn (insert vào DB)
const sendMessage = (senderId, receiverId, content, callback) => {
    const query = 'INSERT INTO Message (Sender_Id, Receiver_Id, Content) VALUES (?, ?, ?)';
    db.query(query, [senderId, receiverId, content], (err, result) => {
        if (err) return callback(err, null);
        callback(null, { message: 'Message sent successfully.', insertId: result.insertId });
    });
};

// Lấy lịch sử chat giữa 2 người
const getChatHistory = (userId1, userId2, callback) => {
    const query = `
        SELECT * FROM Message
        WHERE 
            (Sender_Id = ? AND Receiver_Id = ?)
            OR
            (Sender_Id = ? AND Receiver_Id = ?)
        ORDER BY Created_At ASC
    `;
    db.query(query, [userId1, userId2, userId2, userId1], (err, result) => {
        if (err) return callback(err, null);
        callback(null, result);
    });
};

module.exports = {
    sendMessage,
    getChatHistory
};
