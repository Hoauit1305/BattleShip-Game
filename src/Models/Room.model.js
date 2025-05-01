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
        roomCode: roomCode,
        ownerId: ownerId
    };
};

const closeRoom = async (ownerId) => {
    // Tìm phòng đang mở của chủ phòng
    const existingRoom = await query(
        'SELECT * FROM ROOM WHERE Owner_Id = ? AND Status IN ("waiting", "playing")',
        [ownerId]
    );

    if (existingRoom.length === 0) {
        throw new Error('Không tìm thấy phòng để đóng');
    }

    // Update status thành close
    await query(
        'UPDATE ROOM SET Status = ? WHERE Room_Id = ?',
        ['close', existingRoom[0].Room_Id]
    );

    return {
        roomId: existingRoom[0].Room_Id,
        message: 'Đóng phòng thành công'
    };
};

const findRoom = async (roomCode, guestId) => {
    const existingRoom = await query('SELECT * FROM ROOM WHERE Room_code = ? AND Status = ?', 
        [roomCode, 'waiting']);

    if (existingRoom.length === 0) {
        throw new Error('Không tìm thấy phòng hoặc phòng đã đóng/bắt đầu');
    }

    const room = existingRoom[0];

    if (room.Guest_Id) {
        throw new Error('Phòng đã có khách');
    }

    // Update Guest_Id dựa trên Room_code
    await query('UPDATE ROOM SET Guest_Id = ? WHERE Room_code = ?', [guestId, roomCode]);

    // Lấy tên owner
    const [owner] = await query('SELECT Name FROM PLAYER WHERE Player_Id = ?', [room.Owner_Id]);

    // Lấy tên guest
    const [guest] = await query('SELECT Name FROM PLAYER WHERE Player_Id = ?', [guestId]);

    return {
        roomCode: room.Room_code,
        ownerId: room.Owner_Id,
        guestId: guestId,
        ownerName: owner?.Name || 'Unknown',
        guestName: guest?.Name || 'Unknown'
    };
};

const leaveRoom = async (userId) => {
    // Tìm phòng mà người dùng đang là khách
    const existingRoom = await query(
        'SELECT * FROM ROOM WHERE Guest_Id = ? AND Status IN ("waiting", "playing")',
        [userId]
    );

    if (existingRoom.length === 0) {
        throw new Error('Bạn không phải là khách trong phòng nào');
    }

    // Cập nhật Guest_Id thành null để rời phòng
    await query(
        'UPDATE ROOM SET Guest_Id = NULL WHERE Room_Id = ?',
        [existingRoom[0].Room_Id]
    );

    return {
        success: true,
        roomCode: existingRoom[0].Room_code
    };
};

module.exports = { createRoom, closeRoom, findRoom, leaveRoom };

