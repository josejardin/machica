const http = require('http');
const url = require('url');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const SecretSalt = "UniversalGym_Attendance_Secret_2026";
const DataFile = path.join(process.env.TMPDIR || '/tmp', 'universalgym_attendance.json');

// In-memory attendance cache
let attendanceRecords = [];

// Try to load cached records from disk
try {
  if (fs.existsSync(DataFile)) {
    attendanceRecords = JSON.parse(fs.readFileSync(DataFile, 'utf8')) || [];
  }
} catch (e) {
  attendanceRecords = [];
}

function saveRecords() {
  try {
    fs.writeFileSync(DataFile, JSON.stringify(attendanceRecords, null, 2), 'utf8');
  } catch (e) {}
}

function getManilaDate() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Manila' }).format(new Date());
}

function getManilaTimeString() {
  return new Intl.DateTimeFormat('en-US', { timeZone: 'Asia/Manila', hour: 'numeric', minute: '2-digit', hour12: true }).format(new Date());
}

function generateDailyToken(dateStr) {
  const raw = `${SecretSalt}_${dateStr}`;
  return crypto.createHash('sha256').update(raw, 'utf8').digest('hex').substring(0, 16).toUpperCase();
}

function validateDailyToken(dateStr, token) {
  if (!dateStr || !token) {
    return { valid: false, error: "Invalid QR code. Please scan the QR code at the counter." };
  }

  const today = getManilaDate();
  if (dateStr < today) {
    return { valid: false, error: "Expired QR code. Please scan today's active QR code at the counter." };
  }
  if (dateStr > today) {
    return { valid: false, error: "Invalid QR code." };
  }

  const expectedToken = generateDailyToken(today);
  if (token.trim().toUpperCase() !== expectedToken) {
    return { valid: false, error: "Invalid QR code. Please scan the QR code at the counter." };
  }

  return { valid: true };
}

function renderHtmlPage() {
  return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Universal Gym - Member Attendance</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }
        body { background: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; min-height: 100dvh; padding: 16px; }
        .card { background: #1e293b; width: 100%; max-width: 440px; border-radius: 20px; box-shadow: 0 20px 40px rgba(0,0,0,0.4); overflow: hidden; border: 1px solid #334155; }
        .header { background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); color: #ffffff; padding: 28px 20px; text-align: center; border-bottom: 4px solid #ff8423; }
        .header h1 { font-size: 26px; font-weight: 900; letter-spacing: 1px; color: #ffffff; }
        .header p { color: #94a3b8; font-size: 13px; margin-top: 4px; }
        .body { padding: 24px 20px; }
        .form-group { margin-bottom: 16px; }
        label { display: block; font-size: 12px; font-weight: 700; color: #94a3b8; margin-bottom: 6px; letter-spacing: 0.5px; }
        input[type="text"] { width: 100%; padding: 13px 14px; background: #0f172a; border: 1.5px solid #334155; border-radius: 10px; font-size: 16px; color: #ffffff; outline: none; transition: all 0.2s; }
        input[type="text"]:focus { border-color: #ff8423; box-shadow: 0 0 0 3px rgba(255, 132, 35, 0.2); }
        .btn-submit { width: 100%; background: #ff8423; color: #ffffff; border: none; padding: 15px; font-size: 16px; font-weight: 800; border-radius: 10px; cursor: pointer; transition: all 0.2s; margin-top: 8px; box-shadow: 0 4px 14px rgba(255, 132, 35, 0.3); }
        .btn-submit:hover { background: #e07018; transform: translateY(-1px); }
        .btn-submit:active { transform: scale(0.99); }
        .btn-submit:disabled { background: #475569; cursor: not-allowed; box-shadow: none; transform: none; }
        .result-box { display: none; padding: 16px; border-radius: 10px; margin-top: 18px; font-size: 14px; line-height: 1.6; }
        .result-box.success { display: block; background: rgba(16, 185, 129, 0.15); border: 1.5px solid #10b981; color: #6ee7b7; }
        .result-box.error { display: block; background: rgba(239, 68, 68, 0.15); border: 1.5px solid #ef4444; color: #fca5a5; }
        .footer { text-align: center; font-size: 11px; color: #64748b; margin-top: 24px; }
    </style>
</head>
<body>
    <div class="card">
        <div class="header">
            <h1>UNIVERSAL GYM</h1>
            <p>Member Attendance Check-In</p>
        </div>
        <div class="body">
            <form id="attendanceForm">
                <div class="form-group">
                    <label for="memberId">MEMBER ID</label>
                    <input type="text" id="memberId" placeholder="e.g. GM100001" required autocomplete="off" style="text-transform: uppercase;">
                </div>
                <div class="form-group">
                    <label for="firstName">FIRST NAME</label>
                    <input type="text" id="firstName" placeholder="e.g. Juan" required>
                </div>
                <div class="form-group">
                    <label for="lastName">LAST NAME</label>
                    <input type="text" id="lastName" placeholder="e.g. Dela Cruz" required>
                </div>
                <button type="submit" class="btn-submit" id="btnSubmit">Submit Attendance</button>
            </form>
            <div id="resultBox" class="result-box"></div>
            <div class="footer">Universal Gym Attendance</div>
        </div>
    </div>

    <script>
        const urlParams = new URLSearchParams(window.location.search);
        const qrDate = urlParams.get('date') || '';
        const qrToken = urlParams.get('token') || '';

        const form = document.getElementById('attendanceForm');
        const resultBox = document.getElementById('resultBox');
        const btn = document.getElementById('btnSubmit');

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            btn.disabled = true;
            btn.innerText = 'Submitting...';
            resultBox.className = 'result-box';
            resultBox.style.display = 'none';

            const payload = {
                memberId: document.getElementById('memberId').value.trim(),
                firstName: document.getElementById('firstName').value.trim(),
                lastName: document.getElementById('lastName').value.trim(),
                qrDate: qrDate,
                qrToken: qrToken
            };

            try {
                const res = await fetch('/api/attendance', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const data = await res.json();

                if (data.success) {
                    resultBox.className = 'result-box success';
                    resultBox.innerHTML = '<strong>Check-in Recorded!</strong><br>' +
                                          data.fullName + ' (' + data.memberId + ')<br>' +
                                          'Time: ' + (data.checkInTime || 'Just now');
                    form.reset();
                } else {
                    resultBox.className = 'result-box error';
                    resultBox.innerHTML = '<strong>Check-in Failed</strong><br>' + (data.message || 'Error occurred.');
                }
            } catch (err) {
                resultBox.className = 'result-box error';
                resultBox.innerHTML = '<strong>Connection Error</strong><br>Could not submit attendance. Please try again.';
            } finally {
                btn.disabled = false;
                btn.innerText = 'Submit Attendance';
            }
        });
    </script>
</body>
</html>`;
}

// Request dispatcher
async function handler(req, res) {
  // CORS
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');

  if (req.method === 'OPTIONS') {
    res.statusCode = 200;
    res.end();
    return;
  }

  const parsedUrl = url.parse(req.url, true);
  const pathname = parsedUrl.pathname;

  // 1. Health check
  if (pathname === '/api/health') {
    res.setHeader('Content-Type', 'application/json');
    res.end(JSON.stringify({ status: 'ok', serverTime: new Date().toISOString(), manilaDate: getManilaDate() }));
    return;
  }

  // 2. Attendance Page
  if (req.method === 'GET' && (pathname === '/' || pathname === '/attendance' || pathname === '/index.html')) {
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    res.end(renderHtmlPage());
    return;
  }

  // 3. Attendance Submission API
  if (req.method === 'POST' && (pathname === '/api/attendance' || pathname === '/attendance')) {
    let rawBody = '';
    req.on('data', chunk => { rawBody += chunk; });
    req.on('end', () => {
      try {
        let payload = {};
        if (rawBody.trim().startsWith('{')) {
          payload = JSON.parse(rawBody);
        } else {
          payload = require('querystring').parse(rawBody);
        }

        const memberId = (payload.memberId || payload.member_id || '').trim().toUpperCase();
        const firstName = (payload.firstName || payload.first_name || '').trim();
        const lastName = (payload.lastName || payload.last_name || '').trim();
        const qrDate = (payload.qrDate || payload.date || '').trim();
        const qrToken = (payload.qrToken || payload.token || '').trim();

        if (!memberId) {
          res.statusCode = 400;
          res.setHeader('Content-Type', 'application/json');
          res.end(JSON.stringify({ success: false, message: "Member ID is required." }));
          return;
        }

        // Daily token verification
        if (qrDate || qrToken) {
          const val = validateDailyToken(qrDate, qrToken);
          if (!val.valid) {
            res.statusCode = 400;
            res.setHeader('Content-Type', 'application/json');
            res.end(JSON.stringify({ success: false, message: val.error }));
            return;
          }
        }

        const today = getManilaDate();
        const timeStr = getManilaTimeString();
        const fullName = `${firstName} ${lastName}`.trim() || memberId;

        // Duplicate check
        const alreadyCheckedIn = attendanceRecords.some(r => r.memberId === memberId && r.attendanceDate === today);
        if (alreadyCheckedIn) {
          res.statusCode = 200;
          res.setHeader('Content-Type', 'application/json');
          res.end(JSON.stringify({
            success: false,
            message: `Member ${memberId} has already checked in today!`,
            memberId: memberId,
            fullName: fullName
          }));
          return;
        }

        const newRecord = {
          id: `ATT-${Date.now()}-${Math.floor(Math.random() * 900 + 100)}`,
          memberId: memberId,
          fullName: fullName,
          firstName: firstName,
          lastName: lastName,
          attendanceDate: today,
          checkInTime: timeStr,
          recordedAt: new Date().toISOString(),
          recordedBy: 'QR-Mobile',
          synced: false
        };

        attendanceRecords.push(newRecord);
        saveRecords();

        res.statusCode = 200;
        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify({
          success: true,
          message: "Attendance recorded successfully!",
          memberId: memberId,
          fullName: fullName,
          checkInTime: timeStr
        }));
      } catch (err) {
        res.statusCode = 500;
        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify({ success: false, message: `Server error: ${err.message}` }));
      }
    });
    return;
  }

  // 4. Sync API for Desktop App
  if (req.method === 'GET' && pathname === '/api/sync') {
    res.setHeader('Content-Type', 'application/json');
    const unsynced = attendanceRecords.filter(r => !r.synced);
    res.end(JSON.stringify({
      success: true,
      count: unsynced.length,
      records: unsynced
    }));
    return;
  }

  // 5. Sync Acknowledgement API
  if (req.method === 'POST' && pathname === '/api/sync/ack') {
    let rawBody = '';
    req.on('data', chunk => { rawBody += chunk; });
    req.on('end', () => {
      try {
        const body = JSON.parse(rawBody || '{}');
        const ids = body.syncedIds || [];
        for (const record of attendanceRecords) {
          if (ids.includes(record.id)) {
            record.synced = true;
          }
        }
        saveRecords();
        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify({ success: true, acknowledged: ids.length }));
      } catch (e) {
        res.statusCode = 400;
        res.end(JSON.stringify({ success: false, error: e.message }));
      }
    });
    return;
  }

  // 404
  res.statusCode = 404;
  res.setHeader('Content-Type', 'text/plain');
  res.end('Not Found');
}

// Support running locally with "node api/index.js"
if (require.main === module) {
  const PORT = process.env.PORT || 3000;
  const server = http.createServer(handler);
  server.listen(PORT, () => {
    console.log(`Universal Gym Attendance Web running at http://localhost:${PORT}`);
  });
}

// Export for Vercel Serverless
module.exports = handler;
