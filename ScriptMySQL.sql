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


CREATE TABLE Setting (
    User_Id VARCHAR(5) PRIMARY KEY,
    Sound_Enabled BOOLEAN DEFAULT TRUE,
    Auto_Placed_Ships BOOLEAN DEFAULT FALSE,
    Turn_Timer INT DEFAULT 30,
    FOREIGN KEY (User_Id) REFERENCES Player(Player_Id)
);

CREATE TABLE MatchHistory (
    History_Id VARCHAR(5) PRIMARY KEY,
    Match_Id VARCHAR(5),
    User_Id VARCHAR(5),
    Game_Id VARCHAR(5),
    Result ENUM('win', 'lose', 'draw'),
    Total_Shots INT NOT NULL,
    Hits INT NOT NULL,
    Misses INT NOT NULL,
    Duration TIME NOT NULL,
    FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id),
    FOREIGN KEY (User_Id) REFERENCES Player(Player_Id)
);

CREATE TABLE Rounds (
    Round_Id VARCHAR(5) PRIMARY KEY,
    Match_Id VARCHAR(5),
    Round_Number INT NOT NULL,
    Player_Turns VARCHAR(5) NOT NULL,
    Shots INT NOT NULL,
    MoveHistory TEXT NOT NULL,
    FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id)
);

CREATE TABLE ChatHistory (
    Chat_Id VARCHAR(5) PRIMARY KEY,
    Match_Id VARCHAR(5),
    Message TEXT NOT NULL,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (Match_Id) REFERENCES Matches(Match_Id)
);


