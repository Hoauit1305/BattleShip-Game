const jwt = require('jsonwebtoken');
const { placeShips } = require('../Models/Gameplay.model');

const placeShipController = async (req, res) => {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith("Bearer ")) {
        return res.status(403).json({ message: "Không có token hoặc token không hợp lệ!" });
    }

    const token = authHeader.split(" ")[1];
    let decoded;

    try {
        decoded = jwt.verify(token, process.env.JWT_SECRET);
    } catch (err) {
        return res.status(401).json({ message: "Token không hợp lệ" });
    }

    const playerId = decoded.id;
    const { gameId, ships } = req.body;

    if (!gameId || !ships || !Array.isArray(ships) || ships.length === 0) {
        return res.status(400).json({ message: "Dữ liệu gameId hoặc danh sách tàu không hợp lệ" });
    }

    try {
        await placeShips(gameId, playerId, 'player', ships);
        return res.json({ message: "Đặt tàu thành công!" });
    } catch (err) {
        console.error("❌ Lỗi khi đặt tàu:", err);
        return res.status(500).json({ message: "Lỗi server khi đặt tàu" });
    }
};

module.exports = { placeShipController };
