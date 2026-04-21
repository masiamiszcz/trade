# Manual Testing Guide - 2FA Implementation
**Version:** 1.0  
**Date:** 2026-04-21  
**Status:** Ready for Integration Testing

---

## 🚀 Quick Start

### Prerequisites
- Docker Desktop running
- Docker Compose installed
- Authenticator App (Google Authenticator, Microsoft Authenticator, or Authy)

### Startup Instructions

```bash
# Navigate to docker directory
cd trading_project/docker

# Clean up old containers and images
docker compose down
docker system prune -af --volumes

# Build and start fresh
docker compose up -d --build
```

**Wait 30-60 seconds for services to start:**
```bash
# Check if services are running
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

**Expected output:**
```
NAMES                 STATUS              PORTS
docker-backend-1      Up 20 seconds       80/tcp, 443/tcp
docker-frontend-1     Up 15 seconds       80/tcp
docker-postgres-1     Up 25 seconds       5432/tcp
docker-redis-1        Up 25 seconds       6379/tcp
```

### Access the Application

- **Frontend:** http://localhost
- **Backend API:** http://localhost/api
- **Health Check:** http://localhost/health

---

## 📋 Test Scenarios

### Phase 1: User Registration (2FA Mandatory)

#### Test Case 1.1: Happy Path Registration
**Goal:** Complete full registration with 2FA verification

**Steps:**
1. Go to http://localhost
2. Click "Zarejestruj się" (Register)
3. Fill in registration form:
   ```
   Username: testuser1
   Email: testuser1@example.com
   First Name: Jan
   Last Name: Kowalski
   Password: SecurePass123!
   Currency: PLN
   ```
4. Click "Przejdź do konfiguracji 2FA"

**Expected Result:**
- ✅ Form validation passes (no error messages)
- ✅ Page navigates to 2FA setup with QR code
- ✅ Manual key displayed (e.g., "JBSWY3DPEBLW64TMMQ======")

**Next Step:** Continue to 1.2 for 2FA verification

---

#### Test Case 1.2: 2FA Registration Verification
**Goal:** Complete 2FA verification during registration

**Prerequisites:** Completed 1.1

**Steps:**
1. On 2FA setup page, you see QR code
2. **Option A (Recommended):** Click "Skanuj kod QR" button and scan with phone authenticator
3. **Option B:** Click manual key link and manually enter the key in your authenticator app
4. App generates 6-digit code
5. Enter the code in the text input on the page
6. Click "Weryfikuj kod" or wait for auto-submit

**Expected Result:**
- ✅ Code is validated (no error message)
- ✅ Backup codes modal appears with 8 codes
- ✅ Each code is clickable and copyable

**Backup Codes Modal:**
1. Click "📥 Pobierz jako plik" to download codes
2. Click "📋 Skopiuj wszystkie" to copy to clipboard
3. Click "✅ Zrozumiałem - Przejdź dalej"

**Expected Result:**
- ✅ Modal closes
- ✅ Redirected to login page
- ✅ User account now exists in database with 2FA enabled

---

#### Test Case 1.3: Registration - Invalid 2FA Code (Rate Limiting)
**Goal:** Test rate limiting during registration

**Prerequisites:** Start new registration (don't verify 2FA code yet)

**Steps:**
1. On 2FA setup page after scanning QR
2. Enter **wrong** 6-digit code (e.g., "000000")
3. See error: "Invalid 2FA code. Attempts: 1/5"
4. Enter **wrong** code again (e.g., "111111")
5. See error: "Invalid 2FA code. Attempts: 2/5"
6. Repeat 3 more times (total 5 failed attempts)

**Expected Result (After 5 attempts):**
- ✅ Error message: "Maximum 2FA attempts (5) exceeded. Account locked for 5 minutes."
- ✅ Code input becomes disabled or shows error
- ✅ User must wait 5 minutes or restart registration

---

#### Test Case 1.4: Registration Form Validation
**Goal:** Test client-side validation

**Steps:**
1. Go to registration page
2. Leave fields blank
3. Click "Przejdź do konfiguracji 2FA"

**Expected Result - Per field:**
- ✅ Username: "Nazwa użytkownika jest wymagana"
- ✅ Email: "Email jest wymagany"
- ✅ First Name: "Imię jest wymagane"
- ✅ Last Name: "Nazwisko jest wymagane"
- ✅ Password: "Hasło jest wymagane"

**Steps (Invalid values):**
1. Username too short: "ab"
2. Email invalid: "notanemail"
3. Password weak: "123"

**Expected Result:**
- ✅ Appropriate error messages per field
- ✅ Submit button stays disabled

---

### Phase 2: User Login (2FA Enabled)

#### Test Case 2.1: Happy Path Login with 2FA
**Goal:** Login with user created in Phase 1

**Prerequisites:** 
- User `testuser1` created in Phase 1 with 2FA enabled

**Steps:**
1. Go to http://localhost
2. Click "Zaloguj się" (Login)
3. Enter credentials:
   ```
   Username/Email: testuser1 (or testuser1@example.com)
   Password: SecurePass123!
   ```
4. Click "Zaloguj się"

**Expected Result:**
- ✅ No error message
- ✅ Redirected to 2FA verification page
- ✅ Shows: "Zalogowany jako: testuser1"
- ✅ Prompts for 6-digit code

**Continue to 2FA verification:**
1. Open authenticator app
2. Get current 6-digit code for testuser1
3. Enter code in page
4. Wait or click submit

**Expected Result:**
- ✅ Code is validated
- ✅ Redirected to dashboard
- ✅ Top right shows user menu with "testuser1"
- ✅ Can access protected pages

---

#### Test Case 2.2: Login - Wrong Password
**Goal:** Test failed password validation

**Steps:**
1. Go to login page
2. Enter:
   ```
   Username: testuser1
   Password: WrongPassword123!
   ```
3. Click "Zaloguj się"

**Expected Result:**
- ✅ Error message: "Invalid credentials"
- ✅ Stay on login page
- ✅ Form cleared (password field empty)

---

#### Test Case 2.3: Login - Wrong 2FA Code (Rate Limiting)
**Goal:** Test 2FA rate limiting during login

**Prerequisites:** Logged in to login page, awaiting 2FA verification

**Steps:**
1. Enter wrong code (e.g., "000000")
2. See error: "Invalid 2FA code. (Attempt 1/3)"
3. Enter wrong code again (e.g., "111111")
4. See error: "Invalid 2FA code. (Attempt 2/3)"
5. Enter wrong code third time (e.g., "222222")

**Expected Result (After 3 attempts):**
- ✅ Error message: "Zbyt wiele nieudanych prób. Zaloguj się ponownie."
- ✅ Auto-redirect to login after 2 seconds
- ✅ Session cleared

---

#### Test Case 2.4: Login - Invalid Credentials (Non-existent User)
**Goal:** Test login with non-existent user

**Steps:**
1. Go to login page
2. Enter:
   ```
   Username: nonexistent
   Password: AnyPassword123!
   ```
3. Click "Zaloguj się"

**Expected Result:**
- ✅ Error message: "Invalid credentials"
- ✅ No indication whether username or password was wrong (good for security)
- ✅ Stay on login page

---

### Phase 3: User Registration (Without 2FA - Optional)

#### Test Case 3.1: Create Second User (for testing non-2FA scenarios)
**Goal:** Create user without 2FA to test login without 2FA

**Prerequisites:** One user already created with 2FA

**Steps:**
1. Register new user:
   ```
   Username: testuser2
   Email: testuser2@example.com
   First Name: Maria
   Last Name: Lewandowska
   Password: SecurePass456!
   Currency: EUR
   ```
2. Complete 2FA setup (scan QR code, enter valid code)
3. Download/copy backup codes
4. Confirm and redirect to login

**Expected Result:**
- ✅ User created with 2FA enabled
- ✅ Can login with 2FA

**Note:** Current implementation requires 2FA for all users. To test login without 2FA, we'd need an admin tool to disable it (future feature).

---

### Phase 4: Session & Token Testing

#### Test Case 4.1: Browser DevTools - Token Inspection
**Goal:** Verify token structure and claims

**Prerequisites:** Logged in user

**Steps:**
1. Open Browser DevTools (F12)
2. Go to Application → Storage → Local Storage → http://localhost
3. Look for keys:
   - `auth_token` (final token, 60 min)
   - `trading-platform-auth-token` (fallback)
   - `trading-platform-temp-token` (after 2FA, should be removed)

4. Copy `auth_token` value (looks like: `eyJhbGciOiJIUzI1NiIs...`)
5. Go to https://jwt.io
6. Paste token in "Encoded" section
7. Check "Payload" section

**Expected Payload:**
```json
{
  "userId": "xxx-xxx-xxx-xxx",
  "sub": "xxx-xxx-xxx-xxx",
  "name": "testuser1",
  "email": "testuser1@example.com",
  "role": "User",
  "iat": 1713696000,
  "exp": 1713699600  // 60 minutes from login
}
```

**CRITICAL - What Should NOT be present:**
- ❌ `totp_secret`
- ❌ `password`
- ❌ `backup_codes`
- ❌ Any sensitive data

**Expected Result:**
- ✅ Token is valid JWT
- ✅ Expiry is ~60 minutes
- ✅ No sensitive data in payload

---

#### Test Case 4.2: Session Cleanup After 2FA
**Goal:** Verify temp token is deleted after 2FA

**Prerequisites:** Just completed login with 2FA

**Steps:**
1. Open Browser DevTools → Local Storage
2. Check immediately after successful 2FA verification
3. Look for keys:
   - `trading-platform-temp-token` - should NOT exist
   - `trading-platform-session-id` - should NOT exist
   - `auth_token` - should exist (final token)

**Expected Result:**
- ✅ Temp token cleaned up
- ✅ Session ID cleaned up
- ✅ Final token present and valid

---

#### Test Case 4.3: Token Expiry Simulation
**Goal:** Verify 60-minute token expiry handling

**Steps (Requires patience or JWT manipulation):**
1. After login, get your final token from localStorage
2. Create expired token by modifying `exp` claim in https://jwt.io
   - Change: `"exp": 1713699600` → `"exp": 1713690000` (past timestamp)
3. Go back to browser and manually set localStorage:
   ```javascript
   localStorage.setItem('auth_token', 'YOUR_EXPIRED_TOKEN')
   ```
4. Refresh page or try to access protected endpoint

**Expected Result:**
- ✅ Application detects expired token
- ✅ Auto-redirect to login page
- ✅ Error message shown (if applicable)
- ✅ Token cleared from localStorage

---

### Phase 5: API Testing (via Browser Console)

#### Test Case 5.1: Registration API Call
**Goal:** Verify backend accepts registration request

**Steps:**
1. Open Browser Console (F12 → Console)
2. Run:
   ```javascript
   fetch('http://localhost/api/user/register', {
     method: 'POST',
     headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify({
       username: 'apitest1',
       email: 'apitest1@example.com',
       firstName: 'API',
       lastName: 'Tester',
       password: 'ApiPass123!',
       baseCurrency: 'USD'
     })
   })
   .then(r => r.json())
   .then(d => console.log(d))
   ```
3. Check response

**Expected Response:**
```json
{
  "token": "eyJhbGci...",           // Temp token (5 min)
  "sessionId": "uuid-here",
  "qrCodeDataUrl": "data:image/png;base64,iVBORw0KG...",
  "manualKey": "JBSWY3DPEBLW64TMMQ======",
  "backupCodes": ["code1", "code2", ...],
  "message": "Zeskanuj kod QR..."
}
```

**Expected Status:**
- ✅ HTTP 200 OK
- ✅ Token has 5-minute expiry
- ✅ SessionId is UUID format

---

#### Test Case 5.2: Login API Call
**Goal:** Verify backend accepts login request

**Steps:**
1. Open Browser Console
2. Run:
   ```javascript
   fetch('http://localhost/api/user/login', {
     method: 'POST',
     headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify({
       userNameOrEmail: 'testuser1',
       password: 'SecurePass123!'
     })
   })
   .then(r => r.json())
   .then(d => console.log(d))
   ```

**Expected Response:**
```json
{
  "token": "eyJhbGci...",           // Temp token if 2FA enabled
  "sessionId": "uuid-here",
  "requiresTwoFactor": true,        // true if user has 2FA
  "username": "testuser1"
}
```

**Expected Status:**
- ✅ HTTP 200 OK
- ✅ `requiresTwoFactor: true` (because user has 2FA)
- ✅ SessionId provided for 2FA verification

---

### Phase 6: Error Scenarios

#### Test Case 6.1: Network Error Handling
**Goal:** Verify app handles network failures gracefully

**Prerequisites:** Logged in as user

**Steps:**
1. Open DevTools → Network tab
2. Go to Dashboard (or any page that makes API calls)
3. In Network tab, right-click → "Throttling" → Select "Offline"
4. Try to perform action (e.g., go to different page)

**Expected Result:**
- ✅ Error message displayed: "Network error..."
- ✅ Retry logic kicks in (3 attempts)
- ✅ After retries exhausted: "Failed to fetch"
- ✅ Application doesn't crash

**Recovery:**
1. Right-click Network → "Throttling" → Select "No throttling"
2. Click retry or refresh page
3. Should work normally

---

#### Test Case 6.2: 401 Unauthorized Handling
**Goal:** Verify token invalidation triggers logout

**Steps:**
1. Logged in as user
2. Open Browser Console
3. Manually delete auth token:
   ```javascript
   localStorage.removeItem('auth_token')
   localStorage.removeItem('trading-platform-auth-token')
   ```
4. Try to navigate to protected page (e.g., dashboard)

**Expected Result:**
- ✅ Redirected to login page
- ✅ No error message visible to user (silent redirect)
- ✅ User can re-login

---

#### Test Case 6.3: CORS Error Handling
**Goal:** Verify CORS errors are handled

**Steps:**
1. This is automatic - if backend doesn't have CORS configured properly
2. Open DevTools → Network tab
3. Look for red XHR requests with "CORS" in error

**Expected Result (Correct Setup):**
- ✅ NO CORS errors in Network tab
- ✅ All XHR requests show green status codes

---

### Phase 7: UI/UX Testing

#### Test Case 7.1: Form Loading States
**Goal:** Verify UI feedback during API calls

**Steps:**
1. Go to login page
2. Enter credentials
3. **Immediately** click submit **multiple times**

**Expected Result:**
- ✅ Submit button shows "Logowanie..." or is disabled
- ✅ Form inputs are disabled during request
- ✅ Multiple rapid clicks don't create multiple requests
- ✅ Button re-enables after response

---

#### Test Case 7.2: Error Message Display
**Goal:** Verify error messages are user-friendly

**Steps:**
1. Go to registration page
2. Try to register with existing username (from earlier test)

**Expected Result:**
- ✅ Error message appears in red box
- ✅ Specific field error (not generic "error")
- ✅ Message is in Polish and user-friendly
- ✅ Form doesn't clear (user can edit)

---

#### Test Case 7.3: Validation Feedback (Real-time)
**Goal:** Verify real-time field validation

**Steps:**
1. Go to registration page
2. Click on Username field
3. Type 1 character
4. Click away (blur)
5. Return to field

**Expected Result:**
- ✅ After blur: error message if invalid
- ✅ While typing after blur: error updates in real-time
- ✅ When fixed: error message disappears

---

#### Test Case 7.4: Mobile Responsiveness
**Goal:** Test on different screen sizes

**Steps:**
1. Open DevTools (F12)
2. Click Device Toggle (phone icon)
3. Select "iPhone 12" or "Galaxy S21"
4. Test registration and login flows

**Expected Result:**
- ✅ All text readable (no tiny fonts)
- ✅ Form inputs full width
- ✅ Buttons easily clickable
- ✅ No horizontal scroll
- ✅ Modal/QR code scales properly

---

## 🔍 Advanced Testing (DevTools Console)

### Check Backend Connection
```javascript
// Test health check
fetch('http://localhost/health')
  .then(r => r.json())
  .then(d => console.log('Backend Status:', d))
```

### Verify Redis Connection (from backend logs)
```bash
docker logs docker-backend-1 | grep -i redis
# Should show: "Redis connection successful" or similar
```

### Check Database Connection
```bash
docker logs docker-postgres-1 | grep -i "connection"
# Should show successful connections
```

### View Backend Logs
```bash
docker logs docker-backend-1 -f
# Watch for 2FA operations and errors
```

---

## ✅ Success Criteria Checklist

After completing all tests, verify:

### Authentication
- [ ] Can register new user with 2FA
- [ ] Can login with valid credentials
- [ ] Cannot login with invalid password
- [ ] 2FA verification works with correct code
- [ ] 2FA rate limiting works (5 attempts max)
- [ ] Session cleanup happens after 2FA

### Security
- [ ] JWT tokens don't contain TOTP secret
- [ ] JWT tokens don't contain password
- [ ] JWT tokens don't contain backup codes
- [ ] Temp tokens have 5-minute expiry
- [ ] Final tokens have 60-minute expiry
- [ ] Bearer token authentication works

### UI/UX
- [ ] Form validation shows helpful messages
- [ ] Loading states prevent double-submission
- [ ] Error messages are clear and actionable
- [ ] Responsive design works on mobile
- [ ] Backup codes can be downloaded/copied
- [ ] QR code displays correctly

### API Integration
- [ ] Registration endpoint works
- [ ] Login endpoint works
- [ ] 2FA verification endpoint works
- [ ] Correct HTTP status codes (200, 400, 401, 500)
- [ ] Error responses have meaningful messages

---

## 🐛 Troubleshooting

### Services Won't Start
```bash
# Check container logs
docker logs docker-backend-1
docker logs docker-frontend-1

# Check if ports are already in use
netstat -ano | findstr :80
netstat -ano | findstr :5432
```

### Database Connection Fails
```bash
# Verify PostgreSQL is running
docker exec docker-postgres-1 psql -U postgres -l

# Check connection string in appsettings
cat backend/appsettings.json | grep ConnectionStrings
```

### Redis Connection Fails
```bash
# Verify Redis is running and accessible
docker exec docker-redis-1 redis-cli ping
# Should respond: PONG
```

### Frontend Doesn't Load
```bash
# Check frontend build logs
docker logs docker-frontend-1

# Verify nginx is running
docker exec docker-frontend-1 ps aux | grep nginx
```

### CORS Errors
```javascript
// In browser console, check:
console.log('CORS Headers:', fetch('http://localhost/health')
  .then(r => r.headers))
```

---

## 📝 Notes for Tester

1. **Test accounts persist** - Once created, users stay in database until you run `docker compose down`
2. **Authenticator app codes change every 30 seconds** - You have 30 seconds to enter code
3. **Backup codes are one-time use** - Can't reuse same code twice (backend enforces)
4. **2FA is mandatory** - All new users get 2FA enabled during registration
5. **Rate limiting is per session** - Different browser tabs = different sessions
6. **Temp tokens are 5 minutes** - You must complete 2FA within this window

---

## 🎯 Next Steps After Testing

If all tests pass ✅:
1. Commit changes to git
2. Prepare deployment documentation
3. Plan production rollout
4. Set up monitoring/logging

If issues found ❌:
1. Document issue in ISSUE_REPORT.md
2. Note which test case failed
3. Include browser console errors
4. Include backend logs: `docker logs docker-backend-1`

---

**Happy Testing! 🚀**
