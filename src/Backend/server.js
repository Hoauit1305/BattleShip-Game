const express = require('express');
const cors = require('cors');
require('dotenv').config();
const db = require('./Config/db.config');
// Tạo app Express
const app = express();

const authRoutes = require('./Routes/Auth.route');


// Middleware
app.use(cors());
app.use(express.json());
// app.use(express.urlencoded({ extended: true }));
app.use('/api/auth', authRoutes);
// Test route
app.get('/', (req, res) => {
    res.send('Server đang chạy 🚀');
  });
  
// Port
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
console.log(`Server đang chạy tại http://localhost:${PORT}`);
});