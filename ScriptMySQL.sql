CREATE DATABASE BATTLESHIP

USE BATTLESHIP

CREATE TABLE Player (
    Player_Id VARCHAR(5) PRIMARY KEY,
    Name VARCHAR(100),
    Username VARCHAR(50) UNIQUE,
    Password VARCHAR(255),
    Score INT DEFAULT 0,
    Status ENUM('online', 'offline') DEFAULT 'offline'
);

CREATE TABLE Matches (
    Match_Id VARCHAR(5) PRIMARY KEY,
    Player1_Id VARCHAR(5),
    Player2_Id VARCHAR(5),
    Result ENUM('player1', 'player2', 'draw'),
    Round INT,
    Start_Time DATETIME,
    End_Time DATETIME,
    FOREIGN KEY (Player1_Id) REFERENCES Player(Player_Id),
    FOREIGN KEY (Player2_Id) REFERENCES Player(Player_Id)
);

CREATE TABLE Grids (
    Grid_Id VARCHAR(5) PRIMARY KEY,
    Match_Id VARCHAR(5),
    Player_Id VARCHAR(5),
    Ship_Positions TEXT,
    Grid_Status TEXT,
    FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id),
    FOREIGN KEY (Player_Id) REFERENCES Player(Player_Id)
);

CREATE TABLE Friend (
    User_Id VARCHAR(5),
    Friend_Id VARCHAR(5),
    Status ENUM('pending', 'accepted', 'blocked') DEFAULT 'pending',
    PRIMARY KEY (User_Id, Friend_Id),
    FOREIGN KEY (User_Id) REFERENCES Player(Player_Id),
    FOREIGN KEY (Friend_Id) REFERENCES Player(Player_Id)
);

