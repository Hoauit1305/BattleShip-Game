const db = require('../Config/db.config');
const util = require('util');
const query = util.promisify(db.query).bind(db);

/**
 * Hàm tạo vị trí ngẫu nhiên cho tàu bot
 * @returns {string} Vị trí ngẫu nhiên (ví dụ: 'A1', 'B5', ...)
 */
const generateRandomPosition = () => {
  const rows = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];
  const cols = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
  
  const randomRow = rows[Math.floor(Math.random() * rows.length)];
  const randomCol = cols[Math.floor(Math.random() * cols.length)];
  
  return `${randomRow}${randomCol}`;
};

/**
 * Kiểm tra xem vị trí có hợp lệ không (không trùng lặp và nằm trong bảng)
 * @param {Array} usedPositions Mảng các vị trí đã sử dụng
 * @param {Array} shipPositions Mảng các vị trí của tàu hiện tại
 * @returns {boolean} True nếu vị trí hợp lệ, False nếu không
 */
const isValidPosition = (usedPositions, shipPositions) => {
  // Kiểm tra xem vị trí có nằm trong bảng không
  const rows = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];
  const cols = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
  
  for (const pos of shipPositions) {
    if (pos.length < 2 || pos.length > 3) return false;
    
    const row = pos.charAt(0);
    const col = pos.substring(1);
    
    if (!rows.includes(row) || !cols.includes(col)) return false;
    
    // Kiểm tra xem vị trí có trùng với vị trí đã sử dụng không
    if (usedPositions.includes(pos)) return false;
  }
  
  return true;
};

/**
 * Tạo vị trí ngẫu nhiên cho tàu theo hướng ngang hoặc dọc
 * @param {number} shipLength Độ dài của tàu
 * @param {Array} usedPositions Mảng các vị trí đã sử dụng
 * @returns {Array} Mảng các vị trí của tàu
 */
const generateShipPositions = (shipLength, usedPositions) => {
  const rows = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];
  const cols = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
  let shipPositions = [];
  let isValid = false;
  
  while (!isValid) {
    shipPositions = [];
    const direction = Math.random() < 0.5 ? 'horizontal' : 'vertical';
    
    if (direction === 'horizontal') {
      const row = rows[Math.floor(Math.random() * rows.length)];
      const startCol = Math.floor(Math.random() * (11 - shipLength));
      
      for (let i = 0; i < shipLength; i++) {
        shipPositions.push(`${row}${cols[startCol + i]}`);
      }
    } else {
      const col = cols[Math.floor(Math.random() * cols.length)];
      const startRow = Math.floor(Math.random() * (11 - shipLength));
      
      for (let i = 0; i < shipLength; i++) {
        shipPositions.push(`${rows[startRow + i]}${col}`);
      }
    }
    
    isValid = isValidPosition(usedPositions, shipPositions);
  }
  
  return shipPositions;
};

/**
 * Tạo dữ liệu ngẫu nhiên cho tàu bot
 * @returns {Array} Mảng chứa thông tin các tàu
 */
const generateBotShips = () => {
  const ships = [];
  const usedPositions = [];
  
  // Tàu 5 ô
  const ship5Positions = generateShipPositions(5, usedPositions);
  ships.push({
    type: "Ship5",
    positions: ship5Positions
  });
  usedPositions.push(...ship5Positions);
  
  // Tàu 4 ô
  const ship4Positions = generateShipPositions(4, usedPositions);
  ships.push({
    type: "Ship4",
    positions: ship4Positions
  });
  usedPositions.push(...ship4Positions);
  
  // Tàu 3 ô (thứ nhất)
  const ship31Positions = generateShipPositions(3, usedPositions);
  ships.push({
    type: "Ship3.1",
    positions: ship31Positions
  });
  usedPositions.push(...ship31Positions);
  
  // Tàu 3 ô (thứ hai)
  const ship32Positions = generateShipPositions(3, usedPositions);
  ships.push({
    type: "Ship3.2",
    positions: ship32Positions
  });
  usedPositions.push(...ship32Positions);
  
  // Tàu 2 ô
  const ship2Positions = generateShipPositions(2, usedPositions);
  ships.push({
    type: "Ship2",
    positions: ship2Positions
  });
  
  return ships;
};

/**
 * Lưu thông tin tàu của bot vào database
 * @param {number} gameId ID của game
 * @param {number} botId ID của bot (hoặc player đóng vai trò là bot)
 * @param {Array} ships Mảng chứa thông tin các tàu
 */
const placeBotShips = async (gameId, botId, ships) => {
  for (const ship of ships) {
    const result = await query('INSERT INTO Ship (Game_Id, Player_Id, Owner_Type, Type) VALUES (?, ?, ?, ?)', 
      [gameId, botId, 'bot', ship.type]);
    const shipId = result.insertId;
    
    for (const pos of ship.positions) {
      await query('INSERT INTO ShipPosition (Ship_Id, Position) VALUES (?, ?)', [shipId, pos]);
    }
  }
  
  return true;
};

module.exports = { 
  generateBotShips,
  placeBotShips
};