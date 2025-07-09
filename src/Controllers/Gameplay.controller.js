const jwt = require('jsonwebtoken');
const db = require('../Config/db.config');
const util = require('util');
const query = util.promisify(db.query).bind(db);
const { 
    placeShips, 
    fireAtPosition, 
    fireWithBot, 
    setID,  
    setID1,
    generateBotShips, 
    placeBotShips,
    showPositionShips
} = require('../Models/Gameplay.model');

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

const setid1Controller = async (req, res) => {
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

    const { playerId1, playerId2 } = req.body;

    if (!playerId1 || !playerId2 || playerId1 === playerId2) {
        return res.status(400).json({ message: "Thiếu hoặc trùng playerId1 và playerId2" });
    }

    try {
        const gameId = await setID1(playerId1, playerId2);
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

const fireWithPersonController = async (req, res) => {
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
        const result = await fireWithPerson(gameId, playerId, position);
        return res.json(result);
    } catch (err) {
        console.error("❌ Lỗi khi bắn với người chơi:", err);
        return res.status(400).json({ message: err.message || "Lỗi server khi bắn" });
    }
};
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
        let botId=-1;
        
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
//Controller để hiển thị tàu player bên BotPanel
const getPositionController = async (req, res) => {
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
        const result = await showPositionShips(playerId, gameId);
        return res.json(result);
    } catch (err) {
        console.error("❌ Lỗi khi hiện tàu người chơi:", err);
        return res.status(400).json({ message: err.message || "Lỗi server khi hiện tàu" });
    }
}
module.exports = { 
    placeShipController, 
    fireController, 
    fireWithBotController, 
    setidController, 
    setid1Controller,
    placeBotShipController,
    getPositionController,
    fireWithPersonController
};
