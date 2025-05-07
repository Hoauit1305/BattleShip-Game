const jwt = require('jsonwebtoken');
const db = require('../Config/db.config');
const util = require('util');
const query = util.promisify(db.query).bind(db);
const { generateBotShips, placeBotShips } = require('../Models/Bot.model');

/**
 * Controller xử lý đặt tàu cho bot
 * @param {Object} req Request object
 * @param {Object} res Response object
 */
const placeBotShipController = async (req, res) => {
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
  const { gameId } = req.body;

  if (!gameId) {
    return res.status(400).json({ message: "Thiếu gameId" });
  }

  try {
    // Kiểm tra xem game có tồn tại không và người chơi có quyền không
    const gameCheck = await query('SELECT * FROM Game WHERE Game_Id = ? AND (Player_Id_1 = ? OR Player_Id_2 = ?)', 
      [gameId, playerId, playerId]);
    
    if (gameCheck.length === 0) {
      return res.status(404).json({ message: "Không tìm thấy game hoặc bạn không có quyền truy cập" });
    }
    
    // Xác định Bot ID (người chơi còn lại)
    let botId;
    if (gameCheck[0].Player_Id_1 === playerId) {
      botId = gameCheck[0].Player_Id_2;
    } else {
      botId = gameCheck[0].Player_Id_1;
    }
    
    // Kiểm tra xem bot đã đặt tàu chưa
    const botShipsCheck = await query('SELECT * FROM Ship WHERE Game_Id = ? AND Player_Id = ? AND Owner_Type = "bot"', 
      [gameId, botId]);
    
    if (botShipsCheck.length > 0) {
      return res.status(400).json({ message: "Bot đã đặt tàu cho game này" });
    }

    // Tạo dữ liệu tàu ngẫu nhiên cho bot
    const ships = generateBotShips();
    
    // Lưu vào database
    await placeBotShips(gameId, botId, ships);

    return res.status(200).json({ 
      message: "Đặt tàu cho bot thành công", 
      data: {
        gameId,
        botId,
        ships
      }
    });
  } catch (err) {
    console.error("❌ Lỗi khi đặt tàu cho bot:", err);
    return res.status(500).json({ message: "Lỗi server khi đặt tàu cho bot" });
  }
};

module.exports = { 
  placeBotShipController 
};