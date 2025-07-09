const db = require('../Config/db.config');
const util = require('util');
const query = util.promisify(db.query).bind(db);

const placeShips = async (gameId, playerId, ownerType, ships) => {
    for (const ship of ships) {
        const result = await query('INSERT INTO Ship (Game_Id, Player_Id, Owner_Type, Type) VALUES (?, ?, ?, ?)',
            [gameId, playerId, ownerType, ship.type]);
        const shipId = result.insertId;

        for (const pos of ship.positions) {
            await query('INSERT INTO ShipPosition (Ship_Id, Position) VALUES (?, ?)', [shipId, pos]);
        }
    }
};
// Set ID đánh với bot
const setID = async (playerId) => {
    try {
        const result = await query(
            'INSERT INTO Game (Player_Id_1, Player_Id_2, Status, Current_Turn_Player_Id) VALUES (?, ?, ?, ?)',
            [playerId, -1, 'in_progress', playerId]
        );
        const gameId = result.insertId;
        return gameId;  // ✅ trả về gameId
    } catch (err) {
        throw new Error(err.message);
    }
}
// Set ID đánh với người
const setID1 = async (playerId1, playerId2) => {
    try {
        const result = await query(
            'INSERT INTO Game (Player_Id_1, Player_Id_2, Status, Current_Turn_Player_Id) VALUES (?, ?, ?, ?)',
            [playerId1, playerId2, 'in_progress', playerId1]
        );
        const gameId = result.insertId;
        return gameId;  // ✅ trả về gameId
    } catch (err) {
        throw new Error(err.message);
    }
}
// Hàm bắn vào một vị trí (gameid, -1, ô click)
const fireAtPosition = async (gameId, playerId, position) => {
    // Kiểm tra xem game có tồn tại và người chơi có quyền bắn không
    const gameExists = await query('SELECT * FROM Game WHERE Game_Id = ?', [gameId]);
    if (gameExists.length === 0) {
        throw new Error('Game không tồn tại');
    }

    // Kiểm tra xem có lượt của người chơi này không
    const game = gameExists[0];
    if (game.Current_Turn_Player_Id !== playerId) {
        throw new Error('Không phải lượt của bạn');
    }

    // Kiểm tra xem vị trí này đã bị bắn chưa
    const shotExists = await query(
        'SELECT * FROM Shot s JOIN Game g ON g.Current_Turn_Player_Id = Player_Id and g.Game_Id = s.Game_Id WHERE s.Game_Id = ? AND Position = ?',
        [gameId, position]
    );

    if (shotExists.length > 0) {
        throw new Error('Vị trí này đã bị bắn');
    }

    // Kiểm tra xem bắn trúng tàu không
    const hitShip = await query(`
        SELECT s.Ship_Id, s.Type
        FROM Ship s
        JOIN ShipPosition sp ON s.Ship_Id = sp.Ship_Id
        WHERE s.Game_Id = ? AND sp.Position = ? AND s.Player_Id != ?
    `, [gameId, position, playerId]);

    const isHit = hitShip.length > 0;
    const shotType = isHit ? 'hit' : 'miss';

    // Lưu thông tin bắn
    await query(
        'INSERT INTO Shot (Game_Id, Player_Id, Position, Type) VALUES (?, ?, ?, ?)',
        [gameId, playerId, position, shotType]
    );

    // Nếu trúng, cập nhật trạng thái Hit cho vị trí tàu đó
    if (isHit) {
        const shipId = hitShip[0].Ship_Id;
        await query(
        'UPDATE ShipPosition SET Hit = TRUE WHERE Ship_Id = ? AND Position = ?',
        [shipId, position]
        );
    }

    const result = await query(
        'SELECT Current_Turn_Player_Id FROM Game WHERE Game_Id = ?',
        [gameId]
    );

    // Nếu bắn hụt => đổi lượt
    if (shotType === 'miss') {
        // Xác định đối thủ
        result[0].Current_Turn_Player_Id = (playerId === game.Player_Id_1) ? game.Player_Id_2 : game.Player_Id_1;
        await query(
            'UPDATE Game SET Current_Turn_Player_Id = ? WHERE Game_Id = ?',
            [result[0].Current_Turn_Player_Id, gameId]
        );
    }
    let sunkShip = null;
    let gameResult = null;

    if (isHit) {
        const shipId = hitShip[0].Ship_Id;
        const shipType = hitShip[0].Type;
    // Kiểm tra xem tàu đã bị đánh chìm chưa
        const shipPositions = await query(
        'SELECT Position, Hit FROM ShipPosition WHERE Ship_Id = ?',
        [shipId]
        );
        
        const isSunk = shipPositions.every(position => position.Hit === 1);
        
        if (isSunk) {
            // Lấy danh sách tất cả các vị trí của tàu đã bị chìm
            const shipAllPositions = await query(
                'SELECT Position FROM ShipPosition WHERE Ship_Id = ? ORDER BY Position',
                [shipId]
            );
            
            const positions = shipAllPositions.map(pos => pos.Position);
            
            sunkShip = {
                shipId,
                shipType,
                positions  // Thêm mảng các vị trí của tàu
            };
            
            // Cập nhật trạng thái tàu đã bị đánh chìm
            await query('UPDATE Ship SET Is_Sunk = TRUE WHERE Ship_Id = ?', [shipId]);
            
            // Kiểm tra xem tất cả tàu đã bị đánh chìm chưa = kết thúc game
            const remainingShips = await query(
                `SELECT COUNT(*) as count 
                FROM Ship 
                WHERE Game_Id = ?  AND Is_Sunk = FALSE AND Player_Id != ?  `,
                [gameId, result[0].Current_Turn_Player_Id]
            );

            if (remainingShips[0].count === 0) {
                // Cập nhật trạng thái game đã kết thúc
                await query(
                `UPDATE Game 
                SET Status = ?, Winner_Id = ?
                WHERE Game_Id = ?`,
                ['completed', result[0].Current_Turn_Player_Id, gameId]
                );
                
                gameResult = {
                status: 'completed',
                winnerId: result[0].Current_Turn_Player_Id
                };
            }
        }
    }

    // Trả về kết quả bắn
    return {
        position,
        result: shotType,
        sunkShip,
        gameResult
    };
};
// gameID, playerid, playerposition
const fireWithBot = async (gameId, playerId, playerPosition) => {
    // Người chơi bắn
    const playerShot = await fireAtPosition(gameId, playerId, playerPosition);

    let game = await query('SELECT * FROM Game WHERE Game_Id = ?', [gameId]);
    let currentTurn = game[0].Current_Turn_Player_Id;

    const botShots = [];

    // Nếu đến lượt bot, bot bắn cho đến khi trượt
    while (currentTurn !== playerId) {
        const rows = "ABCDEFGHIJ".split('');
        const cols = Array.from({ length: 10 }, (_, i) => i);
        const allPositions = rows.flatMap(r => cols.map(c => `${r}${c}`));

        const fired = await query(
        'SELECT Position FROM Shot WHERE Game_Id = ? AND Player_Id = ?',
        [gameId, currentTurn]
        );
        const firedSet = new Set(fired.map(r => r.Position));
        const available = allPositions.filter(p => !firedSet.has(p));

        if (available.length === 0) {
        throw new Error("Bot không còn vị trí nào để bắn");
        }

        const botPosition = available[Math.floor(Math.random() * available.length)];

        const botShot = await fireAtPosition(gameId, currentTurn, botPosition);
        botShots.push(botShot);

        // Nếu bot bắn trượt thì dừng
        if (botShot.result === "miss") break;

        // Cập nhật lượt hiện tại
        game = await query('SELECT * FROM Game WHERE Game_Id = ?', [gameId]);
        currentTurn = game[0].Current_Turn_Player_Id;
    }

    // Trả về kết quả cuối cùng
    return {
        message: botShots.length > 0
        ? "Bạn đã bắn và bot đã phản công!"
        : "Bạn đã bắn thành công!",
        playerShot,
        botShots
    };
};

// gameID, playerid, playerposition
const fireWithPerson = async (gameId, playerId, playerPosition) => {
    // Người chơi bắn
    const playerShot = await fireAtPosition(gameId, playerId, playerPosition);

    // Trả về kết quả cuối cùng
    return {
        message: "Bạn đã bắn thành công!",  
        playerShot
    };
};

/**
 * Hàm tạo vị trí ngẫu nhiên cho tàu bot
 * @returns {string} Vị trí ngẫu nhiên (ví dụ: 'A1', 'B5', ...)
 */
const generateRandomPosition = () => {
    const rows = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];
    const cols = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];

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
    const cols = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];

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
    const cols = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
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

//Hiển thị các position mà player đặt
const showPositionShips = async (playerId, gameId) => {
    try {
        // Lấy tất cả các loại ship distinct
        const shipTypes = await query(
            `SELECT DISTINCT S.Type
             FROM Ship S 
             JOIN ShipPosition SP ON S.Ship_Id = SP.Ship_Id 
             WHERE S.Player_Id = ? AND S.Game_Id = ?`,
            [playerId, gameId]
        );

        // Tạo mảng kết quả
        let result = [];

        // Lặp qua từng loại ship để lấy positions
        for (const typeRow of shipTypes) {
            const shipType = typeRow.Type;
            
            // Lấy tất cả positions của loại ship này
            const shipAllPositions = await query(
                `SELECT SP.Position 
                 FROM Ship S 
                 JOIN ShipPosition SP ON S.Ship_Id = SP.Ship_Id 
                 WHERE S.Type = ? AND S.Player_Id = ? AND S.Game_Id = ?`,
                [shipType, playerId, gameId]
            );
            
            // Chuyển đổi thành mảng string positions
            const positions = shipAllPositions.map(pos => pos.Position);
            // Thêm vào kết quả
            result.push({
                shipType,
                positions
            });
        }
        return result;
    } catch (error) {
        console.error('Error in showPositionShips:', error);
        throw error;
    }
}

module.exports = { 
    placeShips, 
    fireAtPosition, 
    fireWithBot, 
    setID, 
    setID1,
    generateBotShips,
    placeBotShips,
    showPositionShips,
    fireWithPerson
};
