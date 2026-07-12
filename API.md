# AjoVault API Reference

Base URL: `https://api.vault.kredar.xyz/api/v1`  
All requests require `Authorization: Bearer <token>` except auth endpoints.  
All responses wrap data in `{ isSuccess, message, data }`.

---

## Auth

### POST `/auth/register`
Create a new account. Triggers an OTP to the user's email.
```json
{ "fullName": "Gospel Mairo", "email": "user@email.com", "password": "...", "phoneNumber": "080..." }
```

### POST `/auth/verify-otp`
Verify signup OTP. Returns JWT token. Also triggers DVA creation in background.
```json
{ "email": "user@email.com", "otp": "123456" }
```
Response: `{ token, userId, fullName, email }`

### POST `/auth/login`
Step 1 of login. Sends OTP to email.
```json
{ "email": "user@email.com", "password": "..." }
```

### POST `/auth/verify-login-otp`
Step 2 of login. Returns JWT token.
```json
{ "email": "user@email.com", "otp": "123456" }
```
Response: `{ token, userId, fullName, email }`

### POST `/auth/resend-otp`
Resend signup OTP. `{ "email": "..." }`

### POST `/auth/resend-login-otp`
Resend login OTP. `{ "email": "..." }`

### POST `/auth/forgot-password`
Send password reset link. `{ "email": "..." }`

### POST `/auth/reset-password`
`{ "email": "...", "token": "...", "newPassword": "...", "confirmPassword": "..." }`

### POST `/auth/logout`
Stateless — just clears the token on the client. Returns 200.

### POST `/auth/provision-dva`
Manually trigger DVA creation for the logged-in user if it wasn't created during signup.  
No body needed. Call this if `wallet.dva.isSet` is `false`.

---

## Account

### GET `/account`
Get the current user's profile.  
Response: `{ userId, fullName, email, phoneNumber, ... }`

### PATCH `/account`
Update profile fields (fullName, phoneNumber etc.)

### PATCH `/account/password`
`{ "currentPassword": "...", "newPassword": "..." }`

### PATCH `/account/pin`
`{ "pin": "1234" }`

---

## Wallet

### GET `/wallet`
Full wallet summary. **Use this as the main wallet data source.**
```json
{
  "balance": 1000.0,
  "totalIn": 1050.0,
  "totalOut": 50.0,
  "currency": "NGN",
  "activeGroups": 1,
  "totalGroups": 1,
  "virtualAccount": {
    "accountNumber": "9703774999",
    "accountName": "GOSPEL MAIRO",
    "bank": "Nomba MFB",
    "bankCode": "...",
    "isSet": true
  }
}
```

### GET `/wallet/balance`
Lightweight balance only. `{ balance, totalIn, totalOut, currency }`

### GET `/wallet/dva`
The user's **Dedicated Virtual Account (DVA)** — the Nomba bank account people send money to.  
This is the "Deposit" account. Check `isSet` to know if DVA has been assigned.
```json
{ "accountNumber": "9703774999", "accountName": "GOSPEL MAIRO", "bank": "Nomba MFB", "isSet": true }
```

### POST `/wallet/payout-account/lookup`
Verify a bank account and get the account name before saving.  
Used in the withdrawal flow to confirm the user's bank details.
```json
{ "accountNumber": "7039653204", "bankCode": "999992" }
```
Response: `{ accountName, accountNumber, bankCode }`  
> ⚠️ Some fintech banks (Opay, PalmPay) may return 409 if the lookup isn't supported. Handle gracefully by letting user enter name manually.

### POST `/wallet/payout-account`
Save the bank account that withdrawals get sent to (Opay, GTBank, Zenith etc.).  
This is **not** the DVA — this is where the user's money goes when they withdraw.
```json
{ "accountNumber": "7039653204", "bankCode": "999992", "accountName": "GOSPEL MAIRO" }
```

### POST `/wallet/withdraw`
Withdraw from wallet balance to the saved payout account.
```json
{ "amount": 5000 }
```

---

## Transactions

### GET `/transactions`
All wallet transactions — deposits, withdrawals, payouts. Use this for the transactions page and recent activity.
```json
[{
  "id": "...",
  "description": "Deposit from bank",
  "direction": "In",       // "In" = credit, "Out" = debit
  "amount": 50.0,
  "occurredAt": "2026-07-12T...",
  "reference": "KRD-...",
  "groupName": null,
  "status": "Completed"
}]
```

### GET `/transactions/{id}`
Single transaction detail.

---

## Groups (Circles)

### GET `/groups/mine`
All circles the logged-in user belongs to.

### GET `/groups/{id}`
Single circle detail.

### POST `/groups`
Create a new circle.
```json
{ "name": "Tech Founders Ajo", "contributionAmount": 20000, "frequency": "Monthly", "maxMembers": 10 }
```

### POST `/groups/join/{inviteCode}`
Join a circle via invite link (no auth needed for this one).

### GET `/groups/{id}/invite`
Get invite link/code for a circle.

### POST `/groups/{id}/invite`
Generate a new invite link.

### GET `/groups/{id}/settings`
Get circle settings.

### PATCH `/groups/{id}/settings`
Update circle settings.

### GET `/groups/{id}/members`
List all members of a circle.

### DELETE `/groups/{id}/members/{memberId}`
Remove a member.

### PATCH `/groups/{id}/members/{memberId}/role`
Change member role. `{ "role": "Admin" }`

### POST `/groups/{id}/leave`
Leave a circle.

### DELETE `/groups/{id}`
Delete a circle (admin only).

---

## Contributions

### GET `/groups/{groupId}/contributions`
All contributions for a circle.

### GET `/groups/{groupId}/contributions/mine`
The logged-in user's contributions for a circle.

### POST `/groups/{groupId}/contributions`
Record a contribution. Deducts from wallet balance.
```json
{ "amount": 20000 }
```

---

## Payouts

### GET `/groups/{groupId}/payouts`
All payouts for a circle.

### GET `/groups/{groupId}/payouts/current`
The active payout (who is receiving this cycle).

### GET `/groups/{groupId}/payouts/upcoming`
Upcoming payouts schedule.

### POST `/groups/{groupId}/payouts/{payoutId}/disburse`
Trigger a payout disbursement (admin only).

---

## Dashboard

### GET `/dashboard/{groupId}`
Circle dashboard data — progress, next payout recipient, contribution stats.

---

## Notifications

### GET `/notifications`
User's notifications list.

### PATCH `/notifications/read`
Mark all notifications as read.

---

## Key Concepts

| Term | What it means |
|---|---|
| **DVA** | Dedicated Virtual Account — the Nomba bank account number users share so others can send them money. Created automatically on signup. |
| **Payout Account** | The user's personal bank account (Opay, GTBank etc.) where withdrawals land. Set manually by the user before they can withdraw. |
| **Circle / Group** | An ajo savings group. Members contribute on a schedule and take turns receiving the pool. |
| **direction: "In"** | Money came into the wallet (deposit/payout received) |
| **direction: "Out"** | Money left the wallet (contribution/withdrawal) |
