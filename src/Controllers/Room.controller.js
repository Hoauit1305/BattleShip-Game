const jwt = require('jsonwebtoken');
const { 
    createRoom, 
    closeRoom, 
    findRoom, 
    leaveRoom 
} = require('../Models/Room.model');

const createRoomController = async (req, res) => {
    try {
        const authHeader = req.headers['authorization'];
        const token = authHeader && authHeader.split(' ')[1];

        if (!token) {
            return res.status(401).json({ message: 'Token không tồn tại' });
        }

        const decoded = jwt.verify(token, process.env.JWT_SECRET);

        const ownerId = decoded.id; // lấy Player_Id từ token
        if (!ownerId) {
            return res.status(400).json({ message: 'Không tìm thấy ownerId trong token' });
        }

        const room = await createRoom(ownerId);

        res.status(201).json({
            message: 'Tạo phòng thành công',
            room: room
        });
    } catch (error) {
        console.error('Lỗi tạo phòng:', error);

        if (error.name === 'JsonWebTokenError' || error.name === 'TokenExpiredError') {
            return res.status(403).json({ message: 'Token không hợp lệ hoặc đã hết hạn' });
        }

        res.status(500).json({ message: 'Lỗi server' });
    }
};

// Đóng phòng
const closeRoomController = async (req, res) => {
    try {
        const authHeader = req.headers['authorization'];
        const token = authHeader && authHeader.split(' ')[1];

        if (!token) {
            return res.status(401).json({ message: 'Token không tồn tại' });
        }

        const decoded = jwt.verify(token, process.env.JWT_SECRET);

        const ownerId = decoded.id;
        if (!ownerId) {
            return res.status(400).json({ message: 'Không tìm thấy ownerId trong token' });
        }

        const result = await closeRoom(ownerId);

        res.status(200).json(result);
    } catch (error) {
        console.error('Lỗi đóng phòng:', error);

        if (error.name === 'JsonWebTokenError' || error.name === 'TokenExpiredError') {
            return res.status(403).json({ message: 'Token không hợp lệ hoặc đã hết hạn' });
        }

        res.status(500).json({ message: 'Lỗi server: ' + error.message });
    }
};

const findRoomController = async (req, res) => {
    try {
        const authHeader = req.headers['authorization'];
        const token = authHeader && authHeader.split(' ')[1];

        if (!token) {
            return res.status(401).json({ message: 'Token không tồn tại' });
        }

        const decoded = jwt.verify(token, process.env.JWT_SECRET);

        const userId = decoded.id;
        if (!userId) {
            return res.status(400).json({ message: 'Không tìm thấy userId trong token' });
        }

        const { roomCode } = req.body;
        if (!roomCode) {
            return res.status(400).json({ message: 'Thiếu mã phòng' });
        }

        const roomInfo = await findRoom(roomCode, userId);

        res.status(200).json({
            message: 'Tham gia phòng thành công',
            room: roomInfo
        });
    } catch (error) {
        console.error('Lỗi tìm phòng:', error);

        if (error.name === 'JsonWebTokenError' || error.name === 'TokenExpiredError') {
            return res.status(403).json({ message: 'Token không hợp lệ hoặc đã hết hạn' });
        }

        res.status(400).json({ message: error.message });
    }
};

const leaveRoomController = async (req, res) => {
    try {
        const authHeader = req.headers['authorization'];
        const token = authHeader && authHeader.split(' ')[1];

        if (!token) {
            return res.status(401).json({ message: 'Token không tồn tại' });
        }

        const decoded = jwt.verify(token, process.env.JWT_SECRET);

        const userId = decoded.id;
        if (!userId) {
            return res.status(400).json({ message: 'Không tìm thấy userId trong token' });
        }

        const result = await leaveRoom(userId);

        res.status(200).json({
            message: 'Rời phòng thành công',
            result: result
        });
    } catch (error) {
        console.error('Lỗi rời phòng:', error);

        if (error.name === 'JsonWebTokenError' || error.name === 'TokenExpiredError') {
            return res.status(403).json({ message: 'Token không hợp lệ hoặc đã hết hạn' });
        }

        res.status(500).json({ message: 'Lỗi server: ' + error.message });
    }
};

module.exports = { 
    createRoomController, 
    closeRoomController, 
    findRoomController, 
    leaveRoomController 
};