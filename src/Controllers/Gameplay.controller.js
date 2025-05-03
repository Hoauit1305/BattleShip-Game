const jwt = require('jsonwebtoken');
const { placeShips, fireAtPosition, fireWithBot, setID } = require('../Models/Gameplay.model');

const setidController = async (req, res) => {
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

  try {
    const gameId = await setID(playerId);
    return res.json({ message: "Tạo ID trận đấu thành công!", gameId });
  } catch (err) {
    console.error("❌ Lỗi khi tạo ID:", err);
    return res.status(500).json({ message: "Lỗi server khi tạo ID trận đấu" });
  }
};

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

const fireWithBotController = async (req, res) => {
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
    const result = await fireWithBot(gameId, playerId, position);
    return res.json(result);
  } catch (err) {
    console.error("❌ Lỗi khi bắn với bot:", err);
    return res.status(400).json({ message: err.message || "Lỗi server khi bắn" });
  }
};
module.exports = { placeShipController, fireController, fireWithBotController, setidController };
