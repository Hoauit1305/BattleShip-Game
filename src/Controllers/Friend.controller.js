const jwt = require('jsonwebtoken');
const friendModel = require('../Models/Friend.model');
const db = require('../Config/db.config');

// Hàm lấy Player_Id từ token
const getUserIdFromToken = (req) => {
    const authHeader = req.headers.authorization;
    if (!authHeader || !authHeader.startsWith("Bearer ")) return null;
    try {
        const token = authHeader.split(" ")[1];
        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        return decoded.id; // hoặc decoded.username nếu bạn dùng username
    } catch {
        return null;
    }
};

// Gửi lời mời kết bạn
const sendFriendRequest = (req, res) => {
    const requesterId = getUserIdFromToken(req);
    const { addresseeId } = req.body;
    if (!requesterId) return res.status(403).json({ message: "Token không hợp lệ!" });
    if (!addresseeId) return res.status(400).json({ message: "Thiếu addresseeId" });

    friendModel.sendFriendRequest(requesterId, addresseeId, (err, result) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi gửi lời mời', detail: err });
        res.json(result);
    });
};

// Chấp nhận lời mời kết bạn
const acceptFriendRequest = (req, res) => {
    const addresseeId = getUserIdFromToken(req);
    const { requesterId } = req.body;
    if (!addresseeId) return res.status(403).json({ message: "Token không hợp lệ!" });
    if (!requesterId) return res.status(400).json({ message: "Thiếu requesterId" });

    friendModel.acceptFriendRequest(requesterId, addresseeId, (err, result) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi chấp nhận lời mời', detail: err });
        res.json(result);
    });
};

// Từ chối lời mời kết bạn
const rejectFriendRequest = (req, res) => {
    const addresseeId = getUserIdFromToken(req);
    const { requesterId } = req.body;
    if (!addresseeId) return res.status(403).json({ message: "Token không hợp lệ!" });
    if (!requesterId) return res.status(400).json({ message: "Thiếu requesterId" });

    friendModel.rejectFriendRequest(requesterId, addresseeId, (err, result) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi từ chối lời mời', detail: err });
        res.json(result);
    });
};

// Lấy danh sách bạn bè
const getFriends = (req, res) => {
    const userId = getUserIdFromToken(req);
    if (!userId) return res.status(403).json({ message: "Token không hợp lệ!" });
    friendModel.getFriends(userId, (err, result) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi lấy danh sách bạn bè', detail: err });
        res.json(result);
    });
};

// Lấy danh sách lời mời kết bạn đang chờ
const getPendingRequests = (req, res) => {
    const userId = getUserIdFromToken(req);
    if (!userId) return res.status(403).json({ message: "Token không hợp lệ!" });
    friendModel.getPendingRequests(userId, (err, result) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi lấy lời mời', detail: err });
        res.json(result);
    });
};

// Tìm player theo ID
const searchPlayer = (req, res) => {
    const userId = getUserIdFromToken(req);
    if (!userId) return res.status(403).json({ message: "Token không hợp lệ!" });

    const playerId = req.params.playerId;
    if (!playerId) return res.status(400).json({ message: "Thiếu playerId" });

    // Không cho tìm chính mình
    if (parseInt(playerId) === parseInt(userId)) {
        return res.status(400).json({ message: "Không thể tìm chính mình!" });
    }

    friendModel.findPlayerByIdWithFriendStatus(userId, playerId, (err, player) => {
        if (err) return res.status(500).json({ message: 'Lỗi khi tìm player', detail: err });
        if (!player) return res.status(404).json({ message: 'Không tìm thấy player' });

        res.json(player);
    });
};


module.exports = {
    sendFriendRequest,
    acceptFriendRequest,
    rejectFriendRequest,
    getFriends,
    getPendingRequests,
    searchPlayer
};