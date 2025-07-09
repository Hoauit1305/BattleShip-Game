require("dotenv").config();
const mysql = require("mysql2");

// Khởi tạo connection pool
const pool = mysql.createPool({
    host: process.env.DB_HOST,
    user: process.env.DB_USER,
    password: process.env.DB_PASS,
    database: process.env.DB_NAME,
    port: process.env.DB_PORT,
    waitForConnections: true,
    connectionLimit: 10,
    queueLimit: 0
});

// ✅ Kiểm tra kết nối ban đầu
pool.getConnection((err, connection) => {
    if (err) {
        console.error("❌ Lỗi kết nối MySQL:", err);
    } else {
        console.log("✅ Kết nối MySQL pool thành công!");
        connection.release(); // Trả kết nối lại pool
    }
});

module.exports = pool;
