---
description: Complete iDate Recreation Plan
---

# iDate Revival 2025: Complete Recreation Plan

## Project Overview
Recreating iDate (Audition Online) from scratch in Unity3D with full cross-platform support (Windows, macOS, Linux, Android, iOS).

**Current Status**: ✅ Foundation established - Networking, UI pipeline, and asset loaders implemented.

---

## Phase 1: Asset Pipeline (COMPLETED ✅)

### 1.1 IDO Container Extraction
- ✅ Tool: `tools/ido_decompiler.py`
- ✅ Extracts textures (DDS), configs (XML), models (GB3)
- ✅ Unity loader: `Assets/Scripts/Assets/IDOLoader.cs`

### 1.2 UI Texture System
- ✅ Bulk extraction: `tools/bulk_extract_ui.py`
- ✅ DDS loading: `Assets/Scripts/Assets/DDSLoader.cs`
- ✅ Auto-prefab generation: `Assets/Scripts/Editor/UiImporter.cs`

### 1.3 3D Model Pipeline (IN PROGRESS 🟡)
- ⚠️ GB3 format partially reversed
- 🔧 Need: GB3 → FBX converter (Windows tool using G-Blender.DLL)
- 📋 Alternative: Manual reverse engineering of GB3 format

---

## Phase 2: Core Systems (IN PROGRESS 🟡)

### 2.1 Networking ✅
**Files**: `Assets/Scripts/Network/`
- ✅ `NetworkManager.cs` - TCP connection manager
- ✅ `Packet.cs` - Packet serialization/deserialization
- ✅ `OpCodes.cs` - Protocol opcodes
- ✅ `Blowfish.cs` - Encryption implementation
- ✅ `CryptoHelper.cs` - Key generation

**Implemented Features**:
- TCP connection to server (127.0.0.1:8132)
- Handshake protocol
- Blowfish encryption/decryption
- Login authentication flow

**Next Steps**:
1. Implement remaining packet handlers (game start, room management, gameplay)
2. Add packet queue system for reliable delivery
3. Implement reconnection logic
4. Add latency compensation

### 2.2 UI System ✅
**Files**: `Assets/Scripts/UI/`
- ✅ `UIManager.cs` - Screen management
- ✅ `LoginLogic.cs` - Login screen controller
- ✅ `LobbyLogic.cs` - Lobby screen controller
- ✅ `ServerSelectLogic.cs` - Server selection

**Next Steps**:
1. Create remaining UI screens (character selection, shop, game room)
2. Implement UI animations and transitions
3. Add localization support
4. Create responsive layouts for mobile

### 2.3 Data Management (PARTIAL 🟡)
**Files**: `Assets/Scripts/Data/`
- ✅ `ItemDatabase.cs` - Shop item parser
- ⚠️ Need: Character data management
- ⚠️ Need: Map/stage configuration loader
- ⚠️ Need: Animation configuration loader

---

## Phase 3: Gameplay Systems (TODO 🔴)

### 3.1 Character System
**Requirements**:
1. Character model loading (GB3 → Unity)
2. Equipment system (head, body, shoes, accessories)
3. Character customization (colors, styles)
4. Animation controller (idle, dance, emotes)

**Implementation Plan**:
```csharp
// Assets/Scripts/Character/CharacterController.cs
public class CharacterController : MonoBehaviour
{
    public CharacterData data;
    public Dictionary<EquipSlot, GameObject> equipment;
    
    public void LoadModel(string modelId) { }
    public void EquipItem(int itemId, EquipSlot slot) { }
    public void PlayAnimation(string animName) { }
}
```

### 3.2 Room System
**Requirements**:
1. Room creation/joining
2. Player list management
3. Room settings (mode, difficulty, song)
4. Ready/start system

**Implementation Plan**:
```csharp
// Assets/Scripts/Game/RoomManager.cs
public class RoomManager : MonoBehaviour
{
    public Room currentRoom;
    public List<Player> players;
    
    public void CreateRoom(RoomSettings settings) { }
    public void JoinRoom(int roomId) { }
    public void SetReady(bool ready) { }
    public void StartGame() { }
}
```

### 3.3 Rhythm Game Core
**Requirements**:
1. Note chart loading and parsing
2. Input timing system (Perfect/Great/Good/Miss)
3. Combo system
4. Score calculation
5. Visual feedback (note highway, judgement display)

**Implementation Plan**:
```csharp
// Assets/Scripts/Game/RhythmEngine.cs
public class RhythmEngine : MonoBehaviour
{
    public NoteChart currentChart;
    public float songTime;
    
    public void LoadChart(string chartId) { }
    public void ProcessInput(KeyCode key, float timing) { }
    public JudgementType CalculateJudgement(float timing) { }
    public void UpdateScore(JudgementType judgement) { }
}
```

### 3.4 Social Features
**Requirements**:
1. Friend list management
2. Private messaging
3. Guild/clan system
4. Leaderboards

---

## Phase 4: Advanced Features (TODO 🔴)

### 4.1 Shop System
- Item browsing and filtering
- Purchase flow (virtual currency)
- Inventory management
- Item preview (try before buy)

### 4.2 Progression System
- Experience points and leveling
- Achievement system
- Daily missions/quests
- Season pass/battle pass

### 4.3 Customization
- Avatar customization
- Room decoration
- Profile customization
- Emote/gesture system

---

## Phase 5: Polish & Optimization (TODO 🔴)

### 5.1 Performance
- Asset bundling and streaming
- Memory optimization
- Network optimization
- Mobile performance tuning

### 5.2 Audio
- Music playback system
- Sound effects
- Voice chat (optional)
- Audio mixing and effects

### 5.3 Visual Effects
- Particle effects (combos, perfect hits)
- Screen effects (flash, shake)
- UI animations
- Character effects

---

## Technical Architecture

### Directory Structure
```
Assets/
├── Scenes/
│   ├── Login.unity
│   ├── Lobby.unity
│   ├── Game.unity
│   └── Shop.unity
├── Scripts/
│   ├── Network/          # Networking layer
│   ├── UI/               # UI controllers
│   ├── Character/        # Character system
│   ├── Game/             # Gameplay logic
│   ├── Data/             # Data management
│   ├── Assets/           # Asset loaders
│   └── Systems/          # Core systems
├── Prefabs/
│   ├── UI/               # Auto-generated UI prefabs
│   ├── Characters/       # Character models
│   └── Effects/          # VFX prefabs
├── Resources/
│   ├── Textures/         # Extracted textures
│   ├── Models/           # Converted 3D models
│   └── Audio/            # Music and SFX
└── StreamingAssets/
    └── Data/             # Game data files
```

### Key Design Patterns
1. **Singleton**: NetworkManager, UIManager, GameManager
2. **Observer**: Event-driven networking and UI updates
3. **Factory**: Character/item instantiation
4. **State Machine**: Game state management
5. **Object Pool**: Note objects, effects

---

## Development Priorities

### Immediate (Week 1-2)
1. ✅ Complete GB3 model extraction
2. 🔧 Implement character loading system
3. 🔧 Create basic game room UI
4. 🔧 Implement room management packets

### Short-term (Week 3-4)
1. Build rhythm game core
2. Implement note chart system
3. Add input handling and judgement
4. Create game UI (note highway, score display)

### Medium-term (Month 2)
1. Implement shop system
2. Add inventory management
3. Create character customization
4. Build social features

### Long-term (Month 3+)
1. Add progression systems
2. Implement advanced features
3. Mobile optimization
4. Beta testing and polish

---

## Testing Strategy

### Unit Tests
- Packet serialization/deserialization
- Encryption/decryption
- Timing calculations
- Score calculations

### Integration Tests
- Network flow (login → lobby → game)
- Asset loading pipeline
- UI navigation

### Performance Tests
- Network latency handling
- Frame rate stability
- Memory usage
- Asset loading times

---

## Deployment Plan

### Development Build
- Local server (iDateEmulator)
- Debug UI and logging
- Hot reload support

### Staging Build
- Test server deployment
- Beta tester access
- Analytics integration

### Production Build
- Production server
- Anti-cheat measures
- Crash reporting
- Update system

---

## Resources & Documentation

### Reverse Engineering Docs
- `RE/Status.md` - Current progress
- `RE/Unity_Master_Plan.md` - Overall strategy
- `RE/Packet_Protocol.md` - Network protocol
- `RE/IDO_Format.md` - Asset format
- `RE/GB3_Format.md` - 3D model format
- `RE/Structs.md` - Data structures

### Tools
- `tools/ido_decompiler.py` - IDO extraction
- `tools/bulk_extract_ui.py` - UI batch extraction
- `iDateEmulator(Server)` - Development server

### External Dependencies
- Unity 2021.3+ LTS
- .NET Standard 2.1
- Input System Package
- TextMeshPro
- DOTween (animations)

---

## Risk Assessment

### High Risk
- ❌ GB3 model extraction complexity
- ⚠️ Timing synchronization in multiplayer
- ⚠️ Mobile performance on older devices

### Medium Risk
- Network stability and reconnection
- Asset loading performance
- Cross-platform compatibility

### Low Risk
- UI implementation
- Single-player features
- Basic networking

---

## Success Metrics

### MVP (Minimum Viable Product)
- ✅ Login and authentication
- ✅ Lobby system
- 🔧 Character display
- 🔧 Room creation/joining
- 🔧 Basic rhythm gameplay
- 🔧 Score tracking

### Full Release
- All original game features
- Mobile support (iOS/Android)
- Stable multiplayer
- Shop and progression
- Social features
- 60 FPS on target devices

---

## Next Immediate Actions

1. **Resolve GB3 Model Loading**
   - Option A: Build Windows tool using G-Blender.DLL
   - Option B: Reverse engineer GB3 format completely
   - Option C: Use placeholder models temporarily

2. **Implement Character System**
   - Create CharacterController script
   - Implement equipment system
   - Add animation controller

3. **Build Game Room**
   - Create room UI prefabs
   - Implement room management logic
   - Add player list display

4. **Start Rhythm Engine**
   - Design note chart format
   - Implement timing system
   - Create input handler

---

**Last Updated**: 2025-12-09
**Status**: Foundation Complete, Moving to Gameplay Implementation
