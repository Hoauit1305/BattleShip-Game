const nodemailer = require('nodemailer');
require('dotenv').config();
console.log('EMAIL_USER:', process.env.EMAIL_USER);
console.log('EMAIL_PASS:', process.env.EMAIL_PASS);
const transporter = nodemailer.createTransport({
    service: 'gmail',
    auth: {
      user: process.env.EMAIL_USER,
      pass: process.env.EMAIL_PASS,
    },
});
  
const sendMail = ({ to, subject, text }) => {
    return transporter.sendMail({
        from: process.env.EMAIL_USER,
        to,
        subject,
        text,
    });
};
  
module.exports = {sendMail};
  