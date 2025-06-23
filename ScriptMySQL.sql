CREATE DATABASE BATTLESHIP_GAME;
select * from player;
select * from ship;
select * from shipposition;
select * from shot;
select * from game;
select * from friend;
insert into friend (Requester_Id, Addressee_Id, Status) VALUES (1, 4, "accepted");
INSERT INTO Player (Username, Password) VALUES ('bao123', '123456' );
INSERT INTO Player (Username, Password) VALUES ('hoang123', '123456' );

INSERT INTO Game (Player_Id_1, Player_Id_2, Status, Current_Turn_Player_Id) 
VALUES (1, 2, 'in_progress', 1);
USE BATTLESHIP_GAME;
drop table game;
DROP database BATTLESHIP_GAME;
CREATE TABLE Player (
	Player_Id INT AUTO_INCREMENT PRIMARY KEY,
	Name VARCHAR(100),
	Username VARCHAR(50) UNIQUE,
	Password VARCHAR(255),
	Score INT DEFAULT 0,
	Status ENUM('online', 'offline') DEFAULT 'offline',
	Email VARCHAR(100) UNIQUE
);

CREATE TABLE Ship (
	Ship_Id INT AUTO_INCREMENT PRIMARY KEY,
	Game_Id INT,
	Player_Id INT,
	Owner_Type ENUM('player', 'bot'),
	Type VARCHAR(20),
	Is_Sunk BOOLEAN DEFAULT FALSE
);

CREATE TABLE ShipPosition (
	ShipPosition_Id INT AUTO_INCREMENT PRIMARY KEY,
	Ship_Id INT,
	Position VARCHAR(3),
	Hit BOOLEAN DEFAULT FALSE,
	FOREIGN KEY (Ship_Id) REFERENCES Ship(Ship_Id) ON DELETE CASCADE
);
-- Thêm bảng Game để quản lý các trận đấu
CREATE TABLE Game (
    Game_Id INT AUTO_INCREMENT PRIMARY KEY,
    Player_Id_1 INT,
    Player_Id_2 INT,
    Status ENUM('waiting', 'in_progress', 'completed') DEFAULT 'waiting',
    Current_Turn_Player_Id INT,
    Winner_Id INT NULL,
    Created_At DATETIME DEFAULT CURRENT_TIMESTAMP,
    Updated_At DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (Player_Id_1) REFERENCES Player(Player_Id),
    FOREIGN KEY (Player_Id_2) REFERENCES Player(Player_Id),
    FOREIGN KEY (Current_Turn_Player_Id) REFERENCES Player(Player_Id),
    FOREIGN KEY (Winner_Id) REFERENCES Player(Player_Id)
);

-- Thêm bảng Shot để lưu thông tin các phát bắn
CREATE TABLE Shot (
    Shot_Id INT AUTO_INCREMENT PRIMARY KEY,
    Game_Id INT,
    Player_Id INT,
    Position VARCHAR(3),
    Type ENUM('hit', 'miss') NOT NULL,
    Created_At DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (Game_Id) REFERENCES Game(Game_Id),
    FOREIGN KEY (Player_Id) REFERENCES Player(Player_Id)
);

CREATE TABLE Friend (
    Requester_Id INT,
    Addressee_Id INT,
    Status ENUM('pending', 'accepted', 'rejected') DEFAULT 'pending',
    PRIMARY KEY (Requester_Id, Addressee_Id),
    FOREIGN KEY (Requester_Id) REFERENCES Player(Player_Id),
    FOREIGN KEY (Addressee_Id) REFERENCES Player(Player_Id)
);
-- CREATE TABLE Matches (
--     Match_Id VARCHAR(5) PRIMARY KEY,
--     Player1_Id INT,
--     Player2_Id INT,
--     Result ENUM('player1', 'player2', 'draw'),
--     Round INT,
--     Start_Time DATETIME,
--     End_Time DATETIME,
--     FOREIGN KEY (Player1_Id) REFERENCES Player(Player_Id),
--     FOREIGN KEY (Player2_Id) REFERENCES Player(Player_Id)
-- );

-- CREATE TABLE Grids (
--     Grid_Id VARCHAR(5) PRIMARY KEY,
--     Match_Id VARCHAR(5),
--     Player_Id INT,
--     Ship_Positions TEXT,
--     Grid_Status TEXT,
--     FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id),
--     FOREIGN KEY (Player_Id) REFERENCES Player(Player_Id)
-- );

-- CREATE TABLE Setting (
--     User_Id INT PRIMARY KEY,
--     Sound_Enabled BOOLEAN DEFAULT TRUE,
--     Auto_Placed_Ships BOOLEAN DEFAULT FALSE,
--     Turn_Timer INT DEFAULT 30,
--     FOREIGN KEY (User_Id) REFERENCES Player(Player_Id)
-- );

-- CREATE TABLE MatchHistory (
--     History_Id VARCHAR(5) PRIMARY KEY,
--     Match_Id VARCHAR(5),
--     User_Id INT,
--     Game_Id VARCHAR(5),
--     Result ENUM('win', 'lose', 'draw'),
--     Total_Shots INT NOT NULL,
--     Hits INT NOT NULL,
--     Misses INT NOT NULL,
--     Duration TIME NOT NULL,
--     FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id),
--     FOREIGN KEY (User_Id) REFERENCES Player(Player_Id)
-- );

-- CREATE TABLE Rounds (
--     Round_Id VARCHAR(5) PRIMARY KEY,
--     Match_Id VARCHAR(5),
--     Round_Number INT NOT NULL,
--     Player_Turns INT NOT NULL,
--     Shots INT NOT NULL,
--     MoveHistory TEXT NOT NULL,
--     FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id)
-- );

-- CREATE TABLE ChatHistory (
--     Chat_Id VARCHAR(5) PRIMARY KEY,
--     Match_Id VARCHAR(5),
--     Message TEXT NOT NULL,
--     Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
--     FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id)
-- );