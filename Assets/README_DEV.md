# iDate Revival Unity Client

This project is the Unity implementation of the iDate/Audition Online client, built to connect to the custom `iDateEmulator(Server)`.

## Current Status
- **Networking Core**: Implemented TCP connection to `127.0.0.1:8132`.
- **Protocol**: 
  - Packet structure (Header: Size + OpCode).
  - Handshake flow (Recv Handshake -> Send Handshake).
  - Login flow (Recv EncryptInit -> Send Login(Encrypted) -> Recv LoginResp).
- **UI**: Basic connection status screen.

## How to Test
1. Start the **Emulator Server**:
   - Go to `iDateEmulator(Server)` directory.
   - Run `npm install` (if not done).
   - Run `npm start` (or `node server/server.js` if running in headless mode, though `npm start` runs the Electron app).
   - Ensure the server logs say "Server started on port 8132".
2. Open this project in **Unity**.
3. Open the **Console** window.
4. Press **Play**.
5. You should see:
   - "Connecting to 127.0.0.1:8132..."
   - "Handshake received..."
   - "Encryption Init received..."
   - "Login Successful!"
   - The Game View should show "Status: Logged In!".

## Next Steps
1. **Asset Loading**:
   - The legacy assets (`.gb3`) need a custom parser or conversion to Unity formats (FBX/Prefab).
   - `Assets/Scripts/Network/Packet.cs` is ready for more packets.
2. **Item Database**:
   - `ShopItem.ido` parser can be ported from `server/itemDatabase.js` to C#.
3. **Gameplay**:
   - Implement `GAME_START` and `GAME_NOTE` packet handlers.

## Directory Structure
- `Assets/Scripts/Network`: Networking logic (`NetworkManager`, `Packet`, `OpCodes`).
- `Assets/Scripts/UI`: UI logic (`LoginController`).
- `GameClient`: Contains original game binaries and assets (Reference).
