# 📝 Test Verification Checklist

**Date:** ________________  
**Tester:** ________________  
**Environment:** Docker (Local)  

---

## PART 1: Setup & Access ✅

### Docker Services
- [ ] `docker compose up -d --build` completed successfully
- [ ] All 4 services running (backend, frontend, postgres, redis)
- [ ] No container errors in logs
- [ ] Services accessible:
  - [ ] Frontend: http://localhost
  - [ ] API: http://localhost/api
  - [ ] Health: http://localhost/health

---

## PART 2: Registration Flow ✅

### 2.1 Registration Form
- [ ] Can navigate to registration page
- [ ] All form fields visible and editable:
  - [ ] Username
  - [ ] Email
  - [ ] First Name
  - [ ] Last Name
  - [ ] Password
  - [ ] Currency dropdown
- [ ] Submit button shows "Przejdź do konfiguracji 2FA"

### 2.2 Form Validation (Test with Invalid Data)
- [ ] Empty username → error message
- [ ] Empty email → error message
- [ ] Invalid email (e.g., "test") → error message
- [ ] Short password (e.g., "123") → error message
- [ ] Form validation prevents submission

### 2.3 Valid Registration Submission
**Test Data:**
```
Username: testuser1
Email: test1@example.com
First Name: Jan
Last Name: Kowalski
Password: TestPass123!
Currency: PLN
```

- [ ] Form accepts all data
- [ ] Submit button shows loading state ("Przetwarzanie...")
- [ ] No console errors

### 2.4 2FA Setup Page
- [ ] Redirected to 2FA setup page after registration
- [ ] QR code displays (large square image)
- [ ] Manual key shows (Base32 format, ~26 characters)
- [ ] "Skanuj kod QR" link visible
- [ ] Manual key copy/reveal options work

---

## PART 3: 2FA Verification ✅

### 3.1 Authenticator App Integration
- [ ] Can scan QR code with phone authenticator:
  - [ ] Google Authenticator
  - [ ] Microsoft Authenticator
  - [ ] Authy
  - [ ] Or use manual key
- [ ] App generates 6-digit code
- [ ] Code changes every 30 seconds

### 3.2 2FA Code Entry
- [ ] Input field appears for 6-digit code
- [ ] Can enter 6 digits
- [ ] Auto-submit after 6 digits (or manual submit button)
- [ ] Loading state during validation

### 3.3 Valid 2FA Code
- [ ] Enter correct code from authenticator
- [ ] Code accepted (no error message)
- [ ] Backup codes modal appears

### 3.4 Invalid 2FA Code - Rate Limiting
- [ ] Enter wrong code 1st time → Error: "Attempts: 1/5"
- [ ] Enter wrong code 2nd time → Error: "Attempts: 2/5"
- [ ] Enter wrong code 3rd time → Error: "Attempts: 3/5"
- [ ] Enter wrong code 4th time → Error: "Attempts: 4/5"
- [ ] Enter wrong code 5th time → Error: "Maximum attempts exceeded. Locked for 5 minutes"
- [ ] Cannot submit more codes for 5 minutes

### 3.5 Backup Codes Modal
- [ ] Modal shows 8 backup codes
- [ ] Codes are readable and copyable
- [ ] "📥 Pobierz jako plik" button works (downloads .txt file)
- [ ] "📋 Skopiuj wszystkie" button works (copies to clipboard)
- [ ] "✅ Zrozumiałem - Przejdź dalej" button present
- [ ] Modal closes on confirmation button
- [ ] Redirected to login page

---

## PART 4: Login Without 2FA (Skip if not testing) ⭕

### 4.1 Create Non-2FA User (Requires Admin Tool)
- [ ] Skip this for now (2FA is mandatory in current implementation)

---

## PART 5: Login With 2FA ✅

### 5.1 Login Form
- [ ] Can access login page (click "Zaloguj się")
- [ ] Two form fields:
  - [ ] Username or Email
  - [ ] Password
- [ ] "Zaloguj się" button visible

### 5.2 Login Form Validation
- [ ] Empty username → error message
- [ ] Empty password → error message
- [ ] Submit disabled until both fields filled

### 5.3 Valid Credentials
**Test Data:**
```
Username: testuser1
Email: test1@example.com
Password: TestPass123!
```

- [ ] Submit button shows loading state ("Logowanie...")
- [ ] No error message
- [ ] Redirected to 2FA verification page

### 5.4 2FA Verification During Login
- [ ] Page shows: "Zalogowany jako: testuser1"
- [ ] 2FA code input field visible
- [ ] Enter valid code from authenticator
- [ ] Code accepted

### 5.5 Successful Login
- [ ] Redirected to dashboard
- [ ] User menu shows username (top right)
- [ ] Can access protected pages
- [ ] No console errors

### 5.6 Invalid Credentials
- [ ] Wrong username → Error: "Invalid credentials"
- [ ] Wrong password → Error: "Invalid credentials"
- [ ] Non-existent user → Error: "Invalid credentials"
- [ ] Form clears (password field empty)
- [ ] Stay on login page

### 5.7 Invalid 2FA Code During Login
- [ ] Enter wrong code 1st time → Error: "Attempt 1/3"
- [ ] Enter wrong code 2nd time → Error: "Attempt 2/3"
- [ ] Enter wrong code 3rd time → Error: "Zbyt wiele nieudanych prób..."
- [ ] Auto-redirect to login after 3 attempts
- [ ] Temp session cleared

---

## PART 6: Token Security ✅

### 6.1 JWT Token Inspection
- [ ] Open DevTools (F12)
- [ ] Go to: Application → Local Storage → http://localhost
- [ ] Copy `auth_token` value

- [ ] Go to https://jwt.io
- [ ] Paste token in "Encoded" section
- [ ] View "Payload" JSON

### 6.2 Token Claims - Check Present
- [ ] `userId` present
- [ ] `sub` present (should equal userId)
- [ ] `name` present (username)
- [ ] `email` present
- [ ] `role` present (should be "User")
- [ ] `exp` present (expiration timestamp)
- [ ] `iat` present (issued at timestamp)

### 6.3 Token Claims - Check ABSENT ⚠️ CRITICAL
- [ ] ❌ NO `totp_secret` claim
- [ ] ❌ NO `password` claim
- [ ] ❌ NO `backup_codes` claim
- [ ] ❌ NO sensitive data
- [ ] ✅ ONLY safe user info + timestamps

### 6.4 Token Expiry
- [ ] Expiry time in token:
  ```
  Current Time: __________ (copy from browser console: Math.floor(Date.now()/1000))
  Token exp:   __________ (from JWT.io)
  Difference:  ~3600 seconds (60 minutes)
  ```
- [ ] Token set to expire in ~60 minutes

### 6.5 Temp Token Cleanup
- [ ] Immediately after 2FA verification, check localStorage:
  - [ ] `trading-platform-temp-token` - SHOULD NOT exist
  - [ ] `trading-platform-session-id` - SHOULD NOT exist
  - [ ] `auth_token` - SHOULD exist (final token)

---

## PART 7: Session Management ✅

### 7.1 localStorage Keys
- [ ] After login, localStorage contains:
  - [ ] `auth_token` (final JWT)
  - [ ] NO `trading-platform-temp-token`
  - [ ] NO `trading-platform-session-id`

### 7.2 Logout & Cleanup
- [ ] Click user menu → Logout
- [ ] All tokens removed from localStorage:
  - [ ] `auth_token` - deleted
  - [ ] `trading-platform-temp-token` - deleted (if exists)
- [ ] Redirected to login page

### 7.3 Session Timeout
- [ ] After 60 minutes (token expiry), accessing protected endpoint:
  - [ ] Auto-logout triggered
  - [ ] Redirect to login page
  - [ ] Token cleared from localStorage

---

## PART 8: UI/UX Testing ✅

### 8.1 Form Loading States
- [ ] During API call, submit button shows loading:
  - [ ] Text changes to "Przetwarzanie..." or "Logowanie..."
  - [ ] Button is disabled
  - [ ] Form inputs are disabled
- [ ] After response, button re-enables

### 8.2 Error Message Display
- [ ] All error messages appear in red box
- [ ] Error messages are in Polish (or configured language)
- [ ] Error messages are user-friendly (not technical)
- [ ] Field-level errors show next to specific field

### 8.3 Real-time Validation
- [ ] Fill username field → No error
- [ ] Leave username field (blur) → Error if invalid
- [ ] Fix username → Error disappears in real-time
- [ ] Similar for all fields

### 8.4 Mobile Responsiveness
- [ ] Open DevTools → Device Emulation (F12)
- [ ] Select iPhone 12 or Galaxy S21
- [ ] Test registration form:
  - [ ] All fields readable (no tiny text)
  - [ ] Form inputs are full-width
  - [ ] Buttons easily tappable
  - [ ] No horizontal scrolling
- [ ] Test login form (same checks)
- [ ] Test 2FA page:
  - [ ] 2FA code input readable
  - [ ] QR code visible (not cut off)

---

## PART 9: API Integration ✅

### 9.1 Network Requests
- [ ] Open DevTools → Network tab
- [ ] During registration:
  - [ ] `POST /user/register` returns 200 OK
  - [ ] `POST /user/register/verify-2fa` returns 200 OK
- [ ] During login:
  - [ ] `POST /user/login` returns 200 OK
  - [ ] `POST /user/verify-2fa` returns 200 OK
- [ ] No 4xx or 5xx errors

### 9.2 Response Headers
- [ ] Responses include:
  - [ ] `Content-Type: application/json`
  - [ ] `Authorization: Bearer` (in requests to protected endpoints)

### 9.3 API Error Handling
- [ ] Invalid request body → 400 Bad Request
- [ ] Unauthorized request → 401 Unauthorized
- [ ] Server error → 500 Internal Server Error
- [ ] Error responses include meaningful message

---

## PART 10: Backend Integration ✅

### 10.1 Backend Logs
- [ ] Run: `docker logs docker-backend-1 -f`
- [ ] During registration, see logs:
  - [ ] `"Registration STEP 1: Created Redis session..."`
  - [ ] `"Registration STEP 2: 2FA code verified..."`
  - [ ] `"User 'testuser1' created with 2FA enabled"`
- [ ] During login with 2FA:
  - [ ] `"Login STEP 1 - 2FA enabled..."`
  - [ ] `"Login STEP 2: 2FA code verified..."`

### 10.2 Database Records
- [ ] User record created in PostgreSQL:
  - [ ] `username`, `email` match
  - [ ] `password_hash` set (not plaintext)
  - [ ] `two_factor_enabled = true`
  - [ ] `two_factor_secret` encrypted (not plaintext)

### 10.3 Redis Session
- [ ] During 2FA, Redis stores temp session:
  - [ ] `sessionId` as key
  - [ ] `totp_secret` as value (encrypted/stored in Redis)
  - [ ] TTL set to 600 seconds (10 minutes)
- [ ] After 2FA verification, session deleted

### 10.4 Rate Limiting
- [ ] After 5 failed 2FA attempts:
  - [ ] Backend sets lockout in Redis
  - [ ] Session locked for 5 minutes (300 seconds)
  - [ ] Subsequent attempts return 429 or 401

---

## PART 11: Error Scenarios ✅

### 11.1 Network Errors
- [ ] Close backend container: `docker stop docker-backend-1`
- [ ] Try to login
- [ ] Error message displays
- [ ] Auto-retry logic activates (3 attempts)
- [ ] Restart backend: `docker start docker-backend-1`
- [ ] Can login again successfully
- [ ] [ ] Cleanup: `docker restart docker-backend-1`

### 11.2 Database Errors (Simulate)
- [ ] Not typically testable without intentionally breaking DB
- [ ] Skip unless necessary

### 11.3 Redis Errors (Simulate)
- [ ] Stop Redis: `docker stop docker-redis-1`
- [ ] Try to login (which uses Redis for 2FA):
  - [ ] Backend should error or timeout
  - [ ] Error displayed to user
- [ ] Restart Redis: `docker start docker-redis-1`
- [ ] Restart backend: `docker restart docker-backend-1`
- [ ] Can login again

---

## FINAL CHECKLIST ✅

### All Core Features Working
- [ ] Registration with 2FA mandatory
- [ ] Login with 2FA verification
- [ ] Invalid code rate limiting (3-5 attempts)
- [ ] Session cleanup after authentication
- [ ] JWT tokens secure (no sensitive data)
- [ ] Error messages user-friendly
- [ ] Form validation working
- [ ] Loading states prevent double-submission

### All Security Requirements Met
- [ ] ✅ No TOTP secret in JWT
- [ ] ✅ No password in JWT
- [ ] ✅ No backup codes in JWT
- [ ] ✅ Temp tokens 5 min, final tokens 60 min
- [ ] ✅ Rate limiting enforced
- [ ] ✅ Session IDs used for Redis lookups
- [ ] ✅ Bearer token authentication working

### All Integrations Working
- [ ] Frontend ↔ Backend communication working
- [ ] Backend ↔ PostgreSQL working
- [ ] Backend ↔ Redis working
- [ ] Error handling end-to-end
- [ ] Logging informative for debugging

---

## 🎯 Final Sign-Off

**All tests passed?** ✅ YES / ❌ NO

**If NO, list issues:**
```
1. _______________________________
2. _______________________________
3. _______________________________
```

**Tested by:** ________________  
**Date:** ________________  
**Time Spent:** ________________  

**Ready for production?** ✅ YES / ❌ NO - Fix issues first

---

**Great testing! Save this checklist for your records.** 📋
