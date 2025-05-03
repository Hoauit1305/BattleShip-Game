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
 
 // Hàm bắn vào một vị trí
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
       sunkShip = {
         shipId,
         shipType
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
 
 const fireWithBot = async (gameId, playerId, playerPosition) => {
  // Người chơi bắn
  const playerShot = await fireAtPosition(gameId, playerId, playerPosition);

  let game = await query('SELECT * FROM Game WHERE Game_Id = ?', [gameId]);
  let currentTurn = game[0].Current_Turn_Player_Id;

  const botShots = [];

  // Nếu đến lượt bot, bot bắn cho đến khi trượt
  while (currentTurn !== playerId) {
    const rows = "ABCDEFGHIJ".split('');
    const cols = Array.from({ length: 10 }, (_, i) => i + 1);
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

 module.exports = { placeShips, fireAtPosition, fireWithBot };