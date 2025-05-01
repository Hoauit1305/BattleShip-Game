const db = require('../Config/db.config');
const util = require('util');
const query = util.promisify(db.query).bind(db);

// Hàm tạo phòng
const createRoom = async (ownerId) => {
    let roomCode;
    let isUnique = false;

    // Thử random đến khi room_code không bị trùng
    while (!isUnique) {
        roomCode = Math.floor(100000 + Math.random() * 900000); // Random 6 chữ số
        const existingRoom = await query('SELECT * FROM ROOM WHERE Room_code = ?', [roomCode]);
        if (existingRoom.length === 0) {
            isUnique = true;
        }
    }

    const result = await query(
        'INSERT INTO ROOM (Owner_Id, Room_code) VALUES (?, ?)',
        [ownerId, roomCode]
    );

    return {
        roomId: result.insertId,
        roomCode: roomCode,
    };
};

module.exports = { createRoom };
