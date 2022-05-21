### POST /auth/signin
#### Request
```json
{
    "credential":"MyUsername OR MyEmail",
    "password": "MyPassword"
}
```
#### Response - 200
```json
{
    "user": {
        "id": 7,
        "username": "MyUsername",
        "isEmailVerified": true
    },
    "jwt": {
        "jwt": "jwt access token or null if SendAsCookie=true",
        "expiration": 1653154867474
    },
    "refreshJwt": "jwt refresh token or null if SendAsCookie=true"
}
```
### POST /auth/signup
#### Request
```json
{
    "username":"MyUsername",
    "email":"MyEmail",
    "password": "MyPassword",
    "passwprdConf":"MyPassword"
}
```
#### Response - 201
```json
{
    "user": {
        "id": 7,
        "username": "MyUsername",
        "isEmailVerified": true
    },
    "jwt": {
        "jwt": "jwt access token or null if SendAsCookie=true",
        "expiration": 1653154867474
    },
    "refreshJwt": "jwt refresh token or null if SendAsCookie=true"
}
```
### POST /auth/signout (Only for SendAsCookie mode)
#### Request - Empty
#### Response - 200
### POST /auth/email/vrf
#### Request
```json
{
    "token":"email verification token"
}
```
#### Response
```json
{
    "jwt": "jwt access token or null if SendAsCookie=true",
    "expiration": 1653154867474
}
```
### [Authorize] GET /auth/email/revrf
#### Request - Empty
#### Response - 302 redirect to "/"
### [Authorize("IsEmailVerified")] POST /auth/email/change (For verified users only)
#### Request
```json
{
    "newEmail":"MyNewEmail",
    "password":"MyPassword"
}
```
#### Response - 200
### POST /auth/pwd/reset1
#### Request
```json
{
    "email":"MyEmail"
}
```
#### Response - 200
### POST /auth/pwd/reset2 
#### Request
```json
{
    "email":"MyEmail",
    "newPassword":"MyNewPassword",
    "newPasswordConf":"MyNewPassword",
    "token":"password reset token"
}
```
#### Response - 200
###  POST /auth/jwt/refresh (In SendAsCookie mode all jwt content comes from cookies)
#### Request 
```json
{
    "accessToken":"active OR expired access jwt",
    "refreshToken":"active refresh token"
}
```
#### Response
```json
{
    "jwt": "jwt access token or null if SendAsCookie=true",
    "expiration": 1653154867474
}
```
