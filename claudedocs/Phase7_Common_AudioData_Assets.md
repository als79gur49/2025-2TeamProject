# Phase 7: Common AudioData Assets Creation Guide

This document provides instructions for creating common AudioData assets for the gradual migration to the new audio system.

## Directory Structure

```
Assets/
└── Audio/
    ├── EventChannels/           # Event Channel SO assets
    │   └── DefaultSoundChannel.asset
    └── AudioData/               # AudioData SO assets
        ├── Buttons/             # Button-specific sounds
        │   ├── ButtonClick.asset
        │   ├── ButtonConfirm.asset
        │   ├── ButtonCancel.asset
        │   ├── ButtonWarning.asset
        │   ├── ButtonSuccess.asset
        │   └── ButtonNavigation.asset
        ├── UI/                  # General UI sounds
        │   ├── ButtonHover.asset
        │   ├── MenuOpen.asset
        │   ├── MenuClose.asset
        │   ├── TabSwitch.asset
        │   └── Notification.asset
        └── Gameplay/            # Common gameplay sounds
            ├── PickupItem.asset
            ├── DropItem.asset
            └── GenericHit.asset
```

## How to Create AudioData Assets

### Step 1: Create Directory Structure
1. In Unity Project window, navigate to `Assets/Audio/`
2. Create folders: `AudioData`, `AudioData/Buttons`, `AudioData/UI`, `AudioData/Gameplay`

### Step 2: Create Button AudioData Assets

For each button type, follow these steps:

#### ButtonClick (Default)
1. Right-click in `Assets/Audio/AudioData/Buttons/`
2. Select `Create > Audio > AudioData`
3. Name it `ButtonClick`
4. Configure in Inspector:
   ```
   Audio Clips: [Assign your click sound clip]
   Volume Min: 0.8
   Volume Max: 1.0
   Pitch Min: 0.95
   Pitch Max: 1.05
   Mixer Group: (Optional) SFX mixer group
   Loop: false
   Cooldown Time: 0.1
   ```

#### ButtonConfirm
```
Audio Clips: [Assign confirm sound]
Volume Min: 0.9
Volume Max: 1.0
Pitch Min: 1.0
Pitch Max: 1.1
Loop: false
Cooldown Time: 0.15
```

#### ButtonCancel
```
Audio Clips: [Assign cancel sound]
Volume Min: 0.7
Volume Max: 0.9
Pitch Min: 0.9
Pitch Max: 1.0
Loop: false
Cooldown Time: 0.15
```

#### ButtonWarning
```
Audio Clips: [Assign warning sound]
Volume Min: 1.0
Volume Max: 1.0
Pitch Min: 0.95
Pitch Max: 1.0
Loop: false
Cooldown Time: 0.2
```

#### ButtonSuccess
```
Audio Clips: [Assign success sound]
Volume Min: 0.85
Volume Max: 1.0
Pitch Min: 1.0
Pitch Max: 1.15
Loop: false
Cooldown Time: 0.2
```

#### ButtonNavigation
```
Audio Clips: [Assign navigation sound]
Volume Min: 0.6
Volume Max: 0.8
Pitch Min: 1.0
Pitch Max: 1.05
Loop: false
Cooldown Time: 0.05
```

### Step 3: Create UI AudioData Assets

#### ButtonHover
```
Audio Clips: [Assign hover sound]
Volume Min: 0.4
Volume Max: 0.6
Pitch Min: 1.0
Pitch Max: 1.1
Loop: false
Cooldown Time: 0.1
```

#### MenuOpen
```
Audio Clips: [Assign menu open sound]
Volume Min: 0.8
Volume Max: 1.0
Pitch Min: 1.0
Pitch Max: 1.0
Loop: false
Cooldown Time: 0.3
```

#### MenuClose
```
Audio Clips: [Assign menu close sound]
Volume Min: 0.7
Volume Max: 0.9
Pitch Min: 0.95
Pitch Max: 1.0
Loop: false
Cooldown Time: 0.3
```

#### TabSwitch
```
Audio Clips: [Assign tab switch sound]
Volume Min: 0.6
Volume Max: 0.8
Pitch Min: 1.0
Pitch Max: 1.05
Loop: false
Cooldown Time: 0.15
```

#### Notification
```
Audio Clips: [Assign notification sound]
Volume Min: 0.8
Volume Max: 1.0
Pitch Min: 1.0
Pitch Max: 1.0
Loop: false
Cooldown Time: 0.5
```

### Step 4: Create Event Channel Asset

1. Right-click in `Assets/Audio/EventChannels/`
2. Select `Create > Audio > Event Channel`
3. Name it `DefaultSoundChannel`
4. This will be the primary event channel for UI sounds

## Usage Examples

### Example 1: Updating a Button to Use Event Channel
```csharp
// In Unity Inspector:
// 1. Add ButtonSoundPlayer component to button GameObject
// 2. Assign:
//    - Sound Channel: DefaultSoundChannel
//    - Audio Data: ButtonClick
// 3. ButtonSoundPlayer will automatically register to Button.onClick
```

### Example 2: Direct AudioData Usage (No Event Channel)
```csharp
// In Unity Inspector:
// 1. Add ButtonSoundPlayer component
// 2. Assign ONLY:
//    - Audio Data: ButtonConfirm
//    - Leave Sound Channel empty
// 3. Will use direct service call
```

### Example 3: Legacy String Support
```csharp
// In Unity Inspector:
// 1. Add ButtonSoundPlayer component
// 2. Leave Sound Channel and Audio Data empty
// 3. Set:
//    - Click Sound Name: "ButtonClick"
//    - Volume: 1.0
//    - Pitch: 1.0
// 4. Legacy string-based system will be used
```

## Migration Priority Order

### P0 (Immediate) - All New Code
- Any new button created → Use Event Channel + AudioData
- Any new UI element → Use Event Channel + AudioData

### P1 (High Priority) - Core Systems
1. Main Menu buttons
2. Pause Menu buttons
3. Settings Menu buttons
4. Core combat UI
5. Player movement UI

### P2 (Medium Priority) - Secondary Systems
1. Inventory buttons
2. Shop UI
3. Dialogue UI
4. HUD elements

### P3 (Low Priority) - Legacy Compatibility
1. Old scenes (migrate when opened for other work)
2. Rarely used menus
3. Debug/test UI

## Testing Checklist

After creating assets, test:
- [ ] Button sounds play correctly
- [ ] Volume variation works as expected
- [ ] Pitch variation sounds natural
- [ ] Cooldown prevents spam-clicking issues
- [ ] Event Channel properly broadcasts to listeners
- [ ] Fallback to direct service works
- [ ] Legacy string-based still works for old code

## Performance Notes

- **AudioData assets**: Very lightweight, just references + parameters
- **Event Channel**: Singleton pattern, minimal overhead
- **Cooldown system**: Prevents performance issues from spam-clicking
- **Pooling**: Already handled by EffectAudioService

## Next Steps

1. ✅ Create directory structure
2. ✅ Create all button AudioData assets
3. ✅ Create all UI AudioData assets
4. ✅ Create DefaultSoundChannel asset
5. ✅ Test with one button in main menu
6. ✅ Roll out to high-priority buttons
7. ✅ Track migration progress in spreadsheet

---

**Created**: 2025-10-10
**Status**: Ready for implementation
**Owner**: Audio System Team
