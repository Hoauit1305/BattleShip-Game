const db = require('../Config/db.config');

// Gửi lời mời kết bạn
const sendFriendRequest = (requesterId, addresseeId, callback) => {
    const query = 'INSERT INTO Friend (Requester_Id, Addressee_Id) VALUES (?, ?)';
    db.query(query, [requesterId, addresseeId], (err, result) => {
        if (err) return callback(err, null);
            callback(null, { message: 'Friend request sent.' });
    });
};

// Chấp nhận lời mời
const acceptFriendRequest = (requesterId, addresseeId, callback) => {
    const query = 'UPDATE Friend SET Status = "accepted" WHERE Requester_Id = ? AND Addressee_Id = ?';
    db.query(query, [requesterId, addresseeId], (err, result) => {
        if (err) return callback(err, null);
            callback(null, { message: 'Friend request accepted.' });
    });
};

// Từ chối lời mời
const rejectFriendRequest = (requesterId, addresseeId, callback) => {
    const query = 'UPDATE Friend SET Status = "rejected" WHERE Requester_Id = ? AND Addressee_Id = ?';
    db.query(query, [requesterId, addresseeId], (err, result) => {
        if (err) return callback(err, null);
            callback(null, { message: 'Friend request rejected.' });
    });
};

// Lấy danh sách bạn bè
const getFriends = (userId, callback) => {
    const query = `
        SELECT 
            u.Player_Id, 
            u.Name, 
            u.Status 
        FROM Friend f 
        JOIN Player u 
            ON (u.Player_Id = f.Addressee_Id AND f.Requester_Id = ?) 
            OR (u.Player_Id = f.Requester_Id AND f.Addressee_Id = ?) 
        WHERE f.Status = 'accepted' 
            AND u.Player_Id != ?;
    `;
    db.query(query, [userId, userId, userId], (err, result) => {
        if (err) return callback(err, null);
        callback(null, result);
    });
};

// Lấy danh sách lời mời đang chờ (inbox)
const getPendingRequests = (userId, callback) => {
    const query = ` SELECT f.Requester_Id, p.Name 
                    FROM Friend f 
                    JOIN Player p 
                    ON p.Player_Id = f.Requester_Id 
                    WHERE f.Addressee_Id = ? AND f.Status = 'pending' 
                `;
    db.query(query, [userId], (err, result) => {
        if (err) return callback(err, null);
            callback(null, result);
    });
};

// Tìm player theo ID + kiểm tra đã là bạn bè hay chưa
const findPlayerByIdWithFriendStatus = (userId, playerId, callback) => {
    const query = `
        SELECT 
            p.Player_Id, 
            p.Name, 
            p.Status AS PlayerStatus,
            (SELECT 
                CASE 
                    WHEN f.Status = 'accepted' THEN 'friend'
                    WHEN f.Status = 'pending' THEN 'pending'
                    ELSE 'none'
                END
            FROM Friend f
            WHERE 
                (f.Requester_Id = ? AND f.Addressee_Id = ?)
                OR
                (f.Requester_Id = ? AND f.Addressee_Id = ?)
            LIMIT 1) AS FriendStatus
        FROM Player p
        WHERE p.Player_Id = ?;
    `;
    db.query(query, [userId, playerId, playerId, userId, playerId], (err, results) => {
        if (err) return callback(err, null);
        if (results.length === 0) return callback(null, null);

        const player = results[0];
        callback(null, {
            Player_Id: player.Player_Id,
            Name: player.Name,
            PlayerStatus: player.PlayerStatus,
            FriendStatus: player.FriendStatus || 'none' // nếu null → none
        });
    });
};


module.exports = {
    sendFriendRequest,
    acceptFriendRequest,
    rejectFriendRequest,
    getFriends,
    getPendingRequests,
    findPlayerByIdWithFriendStatus
};