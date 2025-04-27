const jwt = require('jsonwebtoken');
const { fireAtPosition } = require('../Models/Fire.model');

const fireController = async (req, res) => {
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
  const { gameId, position } = req.body;

  if (!gameId || !position) {
    return res.status(400).json({ message: "Thiếu gameId hoặc vị trí bắn" });
  }

  try {
    const result = await fireAtPosition(gameId, playerId, position);
    return res.json({
      message: result.result === 'hit' ? "Bắn trúng!" : "Bắn trượt!",
      data: result
    });
  } catch (err) {
    console.error("❌ Lỗi khi bắn:", err);
    return res.status(400).json({ message: err.message || "Lỗi server khi bắn" });
  }
};

module.exports = { fireController };