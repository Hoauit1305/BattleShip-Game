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

module.exports = { placeShips };
