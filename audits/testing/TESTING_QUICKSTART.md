# 🚀 Quick Start - Docker & Testing

## Step 1: Start Docker Services (2 minutes)

```powershell
cd C:\Users\kubac\Desktop\Studia\p1\trading_project\docker

# Clean up old stuff
docker compose down
docker system prune -af --volumes

# Start fresh build
docker compose up -d --build
```

**Wait 30-60 seconds**, then verify services are running:

```powershell
docker ps --format "table {{.Names}}\t{{.Status}}"
```

You should see:
```
NAMES                 STATUS
docker-backend-1      Up 45 seconds
docker-frontend-1     Up 40 seconds  
docker-postgres-1     Up 50 seconds
docker-redis-1        Up 50 seconds
```

---

## Step 2: Access Application

**Frontend:** http://localhost  
**Backend API:** http://localhost/api  
**Health Check:** http://localhost/health

---

## Step 3: Manual Testing Flow (20-30 minutes)

### Phase 1: Registration with 2FA ✅
1. Click "Zarejestruj się"
2. Fill in form with test data
3. Scan QR code with phone authenticator (Google Authenticator, Microsoft Authenticator, or Authy)
4. Enter 6-digit code from app
5. Download/save backup codes
6. ✅ Account created!

### Phase 2: Login with 2FA ✅
1. Click "Zaloguj się"
2. Enter username + password
3. Enter 2FA code from authenticator
4. ✅ Logged in!

### Phase 3: Check Token Security ✅
1. Open DevTools (F12)
2. Go to: Application → Local Storage → http://localhost
3. Copy `auth_token` value
4. Go to https://jwt.io
5. Paste token
6. **Verify NO sensitive data:**
   - ❌ No `totp_secret`
   - ❌ No `password`
   - ❌ No `backup_codes`
   - ✅ Only: `userId`, `name`, `email`, `role`, `exp`

### Phase 4: Test Error Cases ✅
- [ ] Wrong password → error message
- [ ] Wrong 2FA code (3x) → redirect to login
- [ ] Invalid email format → form error
- [ ] Username already exists → error message

---

## 📋 Key Test Scenarios

| Scenario | Steps | Expected |
|----------|-------|----------|
| **Register** | Fill form → Scan QR → Enter code → Save codes | ✅ User created |
| **Login 2FA** | Username/pass → Enter 2FA code | ✅ Dashboard |
| **Invalid Code** | Enter wrong code 3x | ✅ Redirect to login |
| **Token Check** | DevTools → JWT.io | ✅ No sensitive data |
| **Session Cleanup** | After login, check localStorage | ✅ Temp token deleted |

---

## 🔍 Verification Checklist

After testing, verify in browser console:

```javascript
// Check final token
console.log(localStorage.getItem('auth_token'))

// Check temp token (should be empty after 2FA)
console.log(localStorage.getItem('trading-platform-temp-token'))

// Check temp session (should be empty)
console.log(localStorage.getItem('trading-platform-session-id'))
```

---

## 📊 Backend Logs (Verify 2FA Operations)

```powershell
# Watch backend logs in real-time
docker logs docker-backend-1 -f

# Should see messages like:
# "Registration STEP 1: Created Redis session..."
# "Registration STEP 2: 2FA code verified..."
# "Login STEP 1 - 2FA enabled..."
# "Login STEP 2: 2FA code verified..."
```

---

## 🐛 If Something Goes Wrong

**Services won't start?**
```powershell
docker logs docker-backend-1    # Check backend
docker logs docker-frontend-1   # Check frontend
docker logs docker-postgres-1   # Check database
```

**Redis not connecting?**
```powershell
docker exec docker-redis-1 redis-cli ping
# Should respond: PONG
```

**Can't access frontend?**
```powershell
curl http://localhost/health
# Should return: {"status":"healthy"}
```

---

## ✅ Success Indicators

You'll know everything is working when:

- ✅ Can register with 2FA QR code
- ✅ Can login and enter 2FA code
- ✅ JWT token contains NO secrets (check JWT.io)
- ✅ Rate limiting works (wrong code 3x = redirect)
- ✅ Backup codes display and are downloadable
- ✅ No console errors in DevTools
- ✅ Backend logs show successful 2FA operations

---

## 📁 Full Testing Guide

For detailed testing instructions, see:  
**[MANUAL_TESTING_GUIDE.md](MANUAL_TESTING_GUIDE.md)**

This has:
- 30+ detailed test cases
- Expected outputs for each
- API testing via console
- Troubleshooting tips
- Mobile responsiveness checks

---

## 🎯 Next: Commit & Document

After testing passes:

```powershell
cd C:\Users\kubac\Desktop\Studia\p1\trading_project

# Commit your work
git add -A
git commit -m "2fa_implementation_complete_and_tested"
git log --oneline -5  # Verify commit
```

---

**Ready? Start Docker and begin testing!** 🚀

Questions? Check the detailed [MANUAL_TESTING_GUIDE.md](MANUAL_TESTING_GUIDE.md)
