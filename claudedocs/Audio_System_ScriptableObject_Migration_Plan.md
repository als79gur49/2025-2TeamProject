# Audio System Migration Plan: ScriptableObject Architecture

## 📋 Executive Summary

**Current State (Method 2 - Repository Pattern):**
- AudioClipData: Serializable struct with metadata
- AudioClipRepository: MonoBehaviour with AudioClipData array
- Services use string-based lookup
- Data tightly coupled to scene GameObject

**Target State (Method 3 - ScriptableObject Revolution + Event Channels):**
- AudioData: ScriptableObject with enhanced features
- SoundEventChannelSO: Event-driven decoupling layer
- Each sound event = independent asset file
- Services accept AudioData directly or via Event Channel
- Complete data-logic separation + broadcaster/listener pattern

**Migration Strategy:** Zero-downtime, backward-compatible, phased approach

---

## 🎯 Goals & Benefits

### Workflow Revolution
**Before:**
1. Designer creates audio file
2. Designer asks programmer to add to Repository
3. Programmer opens scene, modifies Repository GameObject
4. Designer wants tweaks → repeat steps 2-3

**After:**
1. Programmer creates AudioData asset (one-time)
2. Designer opens .asset file in Inspector
3. Designer drags multiple sound variations
4. Designer adjusts pitch/volume ranges, mixer routing
5. Designer tests in Play mode, tweaks instantly
6. **Zero programmer involvement after initial setup!**

### Technical Benefits
1. **Sound Variations:** Multiple clips per event, random selection
2. **Parametric Randomization:** Volume/pitch ranges for natural variety
3. **Mixer Routing:** Per-sound AudioMixer group assignment
4. **Anti-Spam:** Built-in cooldown system
5. **Scene Independence:** Data survives scene changes
6. **Designer Empowerment:** Full control without code changes
7. **Complete Decoupling:** Event Channels eliminate direct dependencies
8. **Testability:** Systems testable in isolation via event mocking
9. **Extensibility:** Add new listeners without modifying broadcasters

---

## 🏗️ Architecture Design

### AudioData ScriptableObject Structure

```csharp
[CreateAssetMenu(fileName = "NewAudioData", menuName = "Audio/AudioData")]
public class AudioData : ScriptableObject
{
    [Header("🎵 Audio Clips")]
    [Tooltip("Multiple clips for variation - plays randomly")]
    [SerializeField] private AudioClip[] clips;

    [Header("🔊 Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMin = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float volumeMax = 1.0f;

    [Header("🎼 Pitch Settings")]
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMin = 1.0f;
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMax = 1.0f;

    [Header("🎚️ Audio Routing")]
    [Tooltip("AudioMixer group for this sound (optional)")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Header("⚙️ Playback Settings")]
    [SerializeField] private bool loop = false;
    [SerializeField] private float fadeInTime = 0f;
    [SerializeField] private float fadeOutTime = 0f;
    [Range(0, 256)]
    [SerializeField] private int priority = 128;

    [Header("⏱️ Anti-Spam Protection")]
    [Tooltip("Minimum time between plays (0 = no limit)")]
    [SerializeField] private float cooldownTime = 0f;

    // Runtime state
    private float lastPlayTime = -Mathf.Infinity;

    // Public Properties
    public AudioMixerGroup MixerGroup => mixerGroup;
    public bool Loop => loop;
    public float FadeInTime => fadeInTime;
    public float FadeOutTime => fadeOutTime;
    public int Priority => priority;

    /// <summary>
    /// Get a random clip from the array
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    /// <summary>
    /// Get a random volume within the specified range
    /// </summary>
    public float GetRandomVolume()
    {
        return Random.Range(volumeMin, volumeMax);
    }

    /// <summary>
    /// Get a random pitch within the specified range
    /// </summary>
    public float GetRandomPitch()
    {
        return Random.Range(pitchMin, pitchMax);
    }

    /// <summary>
    /// Check if this sound can be played (cooldown check)
    /// </summary>
    public bool CanPlay()
    {
        if (cooldownTime <= 0)
            return true;

        float timeSinceLastPlay = Time.time - lastPlayTime;
        if (timeSinceLastPlay < cooldownTime)
            return false;

        lastPlayTime = Time.time;
        return true;
    }

    /// <summary>
    /// Validation in editor
    /// </summary>
    private void OnValidate()
    {
        // Ensure min <= max
        if (volumeMin > volumeMax)
            volumeMax = volumeMin;
        if (pitchMin > pitchMax)
            pitchMax = pitchMin;
    }
}
```

### Service API Evolution

```csharp
// ==================== NEW PRIMARY API ====================
public interface IBGMAudioService
{
    // New: Accept AudioData directly
    bool PlayBGM(AudioData audioData);
    void FadeInBGM(AudioData audioData, float fadeTime = 1.0f);
    void CrossFadeBGM(AudioData newAudioData, float crossFadeTime = 2.0f);

    // Existing: Backward compatibility maintained
    bool PlayBGM(string clipName, float startRate = 0.0f, bool loop = true);
    // ... other existing methods unchanged
}

public interface IEffectAudioService
{
    // New: Accept AudioData directly
    bool PlayEffect(AudioData audioData);
    int PlayEffectLoop(AudioData audioData);

    // Existing: Backward compatibility maintained
    bool PlayEffect(string clipName, float volume = 1.0f, float pitch = 1.0f);
    // ... other existing methods unchanged
}
```

### Repository Evolution

```csharp
public class AudioClipRepository : MonoBehaviour, IAudioClipRepository
{
    // NEW: ScriptableObject references
    [Header("🎯 AudioData Assets (Primary)")]
    [SerializeField] private AudioData[] audioDataAssets = new AudioData[0];

    // DEPRECATED: Legacy struct array (kept for backward compatibility)
    [Header("⚠️ Legacy Data (Deprecated)")]
    [SerializeField] private AudioClipData[] legacyClipDataArray = new AudioClipData[0];

    // Caches
    private Dictionary<string, AudioData> audioDataCache = new Dictionary<string, AudioData>();
    private Dictionary<string, AudioClipData> legacyClipDataCache = new Dictionary<string, AudioClipData>();

    // NEW: Get AudioData by name
    public AudioData GetAudioData(string name)
    {
        if (audioDataCache.TryGetValue(name, out AudioData data))
            return data;
        return null;
    }

    // MODIFIED: Fallback to legacy if AudioData not found
    public AudioClipData GetClipData(string clipName)
    {
        // First check if we have AudioData
        var audioData = GetAudioData(clipName);
        if (audioData != null)
        {
            // Convert AudioData to AudioClipData for backward compatibility
            return ConvertToLegacyFormat(audioData);
        }

        // Fall back to legacy data
        if (legacyClipDataCache.TryGetValue(clipName, out AudioClipData clipData))
            return clipData;

        return default(AudioClipData);
    }

    private AudioClipData ConvertToLegacyFormat(AudioData audioData)
    {
        var clip = audioData.GetRandomClip();
        return new AudioClipData(
            audioData.name,
            clip,
            audioData.GetRandomVolume(),
            audioData.GetRandomPitch(),
            audioData.Loop
        );
    }
}
```

### Event Channel Architecture: Decoupling Sound Requests

**Philosophy:** If data-logic separation through ScriptableObjects is the first revolution, then decoupling 'requests' from 'handlers' is the second. Game systems should never know AudioManager exists.

#### The Concept: Broadcaster/Listener Pattern

Event Channels are ScriptableObject assets that act as communication mediators between systems. Like a radio station:
- **Broadcasters** transmit events to a channel (no knowledge of who's listening)
- **Listeners** subscribe to a channel and respond to events (no knowledge of who's broadcasting)
- **The Channel** mediates all communication, achieving zero coupling

#### Core Components

**1. SoundEventChannelSO (The Mediator)**

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Event Channel for audio playback requests.
/// Decouples sound requesters from AudioManager.
/// </summary>
[CreateAssetMenu(fileName = "SoundEventChannel", menuName = "Audio/Event Channel")]
public class SoundEventChannelSO : ScriptableObject
{
    /// <summary>
    /// Event raised when any system requests sound playback
    /// Parameters: (AudioData soundData, Vector3 position)
    /// </summary>
    public UnityAction<AudioData, Vector3> OnSoundRequested;

    /// <summary>
    /// Broadcast a sound request to all listeners
    /// </summary>
    public void RaiseSoundEvent(AudioData soundData, Vector3 position)
    {
        if (OnSoundRequested != null)
        {
            OnSoundRequested.Invoke(soundData, position);
        }
        else
        {
            Debug.LogWarning($"[SoundEventChannel] Sound '{soundData.name}' requested but no listeners subscribed.");
        }
    }

    /// <summary>
    /// Convenience method for 2D sounds (position irrelevant)
    /// </summary>
    public void RaiseSoundEvent(AudioData soundData)
    {
        RaiseSoundEvent(soundData, Vector3.zero);
    }
}
```

**2. Broadcaster Example (PlayerHealth)**

```csharp
public class PlayerHealth : MonoBehaviour
{
    [Header("Audio Configuration")]
    [SerializeField] private SoundEventChannelSO soundEventChannel;
    [SerializeField] private AudioData playerHurtSound;
    [SerializeField] private AudioData playerDeathSound;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // Broadcast hurt sound event - no knowledge of AudioManager
        if (soundEventChannel != null && playerHurtSound != null)
        {
            soundEventChannel.RaiseSoundEvent(playerHurtSound, transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Broadcast death sound event
        if (soundEventChannel != null && playerDeathSound != null)
        {
            soundEventChannel.RaiseSoundEvent(playerDeathSound, transform.position);
        }

        // ... death logic ...
    }
}
```

**Key Observation:** PlayerHealth has ZERO references to AudioManager, audio services, or any audio system component. It only knows:
- Which channel to broadcast to (`SoundEventChannelSO`)
- Which sounds to play (`AudioData` assets)

**3. Listener Example (AudioManager)**

```csharp
public class AudioManager : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private SoundEventChannelSO soundEventChannel;

    [Header("Services")]
    [SerializeField] private IEffectAudioService effectAudioService;

    // ... object pooling and other audio logic ...

    private void OnEnable()
    {
        // Subscribe to sound events when enabled
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested += HandleSoundRequest;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled to prevent memory leaks
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested -= HandleSoundRequest;
        }
    }

    /// <summary>
    /// Handle incoming sound requests from any broadcaster
    /// </summary>
    private void HandleSoundRequest(AudioData soundData, Vector3 position)
    {
        if (soundData == null)
        {
            Debug.LogWarning("[AudioManager] Received null AudioData in sound request.");
            return;
        }

        // Cooldown check
        if (!soundData.CanPlay())
        {
            Debug.Log($"[AudioManager] Sound '{soundData.name}' on cooldown.");
            return;
        }

        // Play the sound using the effect service
        effectAudioService.PlayEffect(soundData);

        // Position can be used for future features (e.g., UI directional indicators)
        // but not for 3D spatial audio in this 2D game
    }
}
```

**Key Observation:** AudioManager only knows about the event channel. It doesn't know which game systems will broadcast events. This is true inversion of dependencies.

#### Revolutionary Benefits

**1. Complete Decoupling (Total System Independence)**
- PlayerHealth can be tested without AudioManager existing in the scene
- AudioManager can be tested by programmatically raising events
- Systems can be added/removed without code changes
- Unit testing becomes trivial - mock the event channel

**2. Open-Closed Principle in Action**

Want to add screen shake when player takes damage?

```csharp
public class ScreenEffectManager : MonoBehaviour
{
    [SerializeField] private SoundEventChannelSO soundEventChannel;

    private void OnEnable()
    {
        // Subscribe to the SAME channel
        soundEventChannel.OnSoundRequested += HandleScreenShake;
    }

    private void OnDisable()
    {
        soundEventChannel.OnSoundRequested -= HandleScreenShake;
    }

    private void HandleScreenShake(AudioData soundData, Vector3 position)
    {
        // Add screen shake logic based on sound type
        if (soundData.name.Contains("Hurt"))
        {
            ShakeScreen(0.2f, 0.1f);
        }
    }
}
```

**Zero lines changed in PlayerHealth or AudioManager.** This is extension without modification.

**3. Replaces Singleton Pattern**

Traditional approach requires `AudioManager.Instance.PlaySound()` everywhere, creating tight coupling and testing nightmares. Event Channels eliminate this anti-pattern entirely.

**4. Multi-System Coordination**

One event can trigger multiple systems:
- AudioManager plays sound
- ScreenEffectManager adds visual feedback
- AnalyticsManager logs the event
- SubtitleManager displays audio caption

All from a single broadcast, zero coupling between systems.

#### Debug Listener Pattern

For development and debugging, add a debug listener that logs all sound events:

```csharp
public class AudioDebugLogger : MonoBehaviour
{
    [SerializeField] private SoundEventChannelSO soundEventChannel;
    [SerializeField] private bool enableLogging = true;

    private void OnEnable()
    {
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested += LogSoundEvent;
        }
    }

    private void OnDisable()
    {
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested -= LogSoundEvent;
        }
    }

    private void LogSoundEvent(AudioData soundData, Vector3 position)
    {
        if (!enableLogging) return;

        Debug.Log($"[AudioDebug] Sound Requested: '{soundData.name}' at position {position} (Time: {Time.time:F2}s)");
    }
}
```

Attach this to any scene to monitor all audio events in real-time. Enable/disable via Inspector checkbox without code changes.

#### Event Channel Best Practices

**Channel Granularity:**
- **Single Global Channel:** Simple, works for most games
- **Categorized Channels:** UI sounds, gameplay sounds, narrative sounds
- **System-Specific Channels:** Combat channel, menu channel, ambient channel

**Asset Organization:**
```
Assets/
  Audio/
    EventChannels/
      GlobalSoundChannel.asset      # Main channel for all sounds
      UISoundChannel.asset           # UI-specific sounds
      CombatSoundChannel.asset       # Combat events
```

**Memory Management:**
- Event channels are lightweight (just event delegates)
- Always unsubscribe in OnDisable to prevent memory leaks
- Use OnEnable/OnDisable pattern for automatic cleanup

**Testing Strategy:**
```csharp
// Unit test example
[Test]
public void PlayerHealth_TakeDamage_BroadcastsCorrectSound()
{
    // Arrange
    var mockChannel = ScriptableObject.CreateInstance<SoundEventChannelSO>();
    var player = CreatePlayerWithChannel(mockChannel);
    AudioData capturedSound = null;

    mockChannel.OnSoundRequested += (sound, pos) => capturedSound = sound;

    // Act
    player.TakeDamage(10);

    // Assert
    Assert.IsNotNull(capturedSound);
    Assert.AreEqual("PlayerHurt", capturedSound.name);
}
```

---

## 📅 Migration Phases

### Phase 1: Foundation (Week 1, Days 1-2)
**Goal:** Create AudioData ScriptableObject without breaking existing code

**Tasks:**
1. ✅ Create AudioData.cs ScriptableObject class
2. ✅ Add CreateAssetMenu attribute
3. ✅ Implement behavior methods (GetRandomClip, CanPlay, etc.)
4. ✅ Test: Manually create sample AudioData assets
5. ✅ Validate: All existing scenes work unchanged

**Success Criteria:**
- AudioData asset can be created via right-click menu
- Inspector shows all fields correctly
- No compilation errors
- Existing game functionality unaffected

---

### Phase 2: Service Integration (Week 1, Days 3-5)
**Goal:** Services support AudioData while maintaining backward compatibility

**Tasks:**
1. ✅ Add new methods to IBGMAudioService interface
   - `bool PlayBGM(AudioData audioData)`
   - `void FadeInBGM(AudioData audioData, float fadeTime)`
   - `void CrossFadeBGM(AudioData newAudioData, float crossFadeTime)`

2. ✅ Add new methods to IEffectAudioService interface
   - `bool PlayEffect(AudioData audioData)`
   - `int PlayEffectLoop(AudioData audioData)`

3. ✅ Implement in BGMAudioService
   ```csharp
   public bool PlayBGM(AudioData audioData)
   {
       if (!isInitialized || audioData == null)
           return false;

       // Cooldown check
       if (!audioData.CanPlay())
       {
           Debug.Log($"BGM on cooldown: {audioData.name}");
           return false;
       }

       // Get random clip
       AudioClip clip = audioData.GetRandomClip();
       if (clip == null)
           return false;

       // Apply settings from AudioData
       audioSource.clip = clip;
       audioSource.volume = audioData.GetRandomVolume();
       audioSource.pitch = audioData.GetRandomPitch();
       audioSource.loop = audioData.Loop;

       // Apply mixer group if specified
       if (audioData.MixerGroup != null)
           audioSource.outputAudioMixerGroup = audioData.MixerGroup;

       audioSource.Play();

       CurrentBGMName = audioData.name;
       return true;
   }
   ```

4. ✅ Implement in EffectAudioService (similar pattern)

5. ✅ Keep all existing string-based methods working
   - They continue to use Repository lookup
   - No changes to existing call sites required

**Success Criteria:**
- New AudioData-based methods work correctly
- Metadata from AudioData is applied (pitch, volume, mixer)
- All existing string-based calls still work
- Unit tests pass for new methods

---

### Phase 3: Repository Evolution (Week 2, Days 1-2)
**Goal:** Repository supports both AudioData and legacy data

**Tasks:**
1. ✅ Add `AudioData[] audioDataAssets` field to AudioClipRepository
2. ✅ Keep `AudioClipData[] legacyClipDataArray` field (mark as deprecated)
3. ✅ Add `GetAudioData(string name)` method
4. ✅ Modify `GetClipData()` to check AudioData first, fall back to legacy
5. ✅ Update caching logic to handle both types
6. ✅ Add conversion method: AudioData → AudioClipData (for backward compat)

**Success Criteria:**
- Repository can hold both AudioData references and legacy data
- String lookup works for both types
- Priority: AudioData > Legacy
- No breaking changes to existing code

---

### Phase 4: Event Channel Integration (Week 2, Days 3-4)
**Goal:** Implement Event Channel architecture for complete decoupling

**Tasks:**
1. ✅ Create SoundEventChannelSO.cs ScriptableObject class
   ```csharp
   [CreateAssetMenu(fileName = "SoundEventChannel", menuName = "Audio/Event Channel")]
   public class SoundEventChannelSO : ScriptableObject
   {
       public UnityAction<AudioData, Vector3> OnSoundRequested;
       public void RaiseSoundEvent(AudioData soundData, Vector3 position) { ... }
       public void RaiseSoundEvent(AudioData soundData) { ... }
   }
   ```

2. ✅ Create GlobalSoundChannel.asset
   - Place in `Assets/Audio/EventChannels/`
   - This becomes the primary sound event bus

3. ✅ Update AudioManager to listen to event channel
   - Add `[SerializeField] private SoundEventChannelSO soundEventChannel`
   - Subscribe in OnEnable: `soundEventChannel.OnSoundRequested += HandleSoundRequest`
   - Unsubscribe in OnDisable
   - Implement HandleSoundRequest method

4. ✅ Create AudioDebugLogger.cs for debugging
   - Logs all sound events in development builds
   - Attach to AudioManager GameObject
   - Toggle-able via Inspector

5. ✅ Update one system as proof-of-concept
   - Choose PlayerHealth or similar gameplay system
   - Replace direct AudioManager calls with event broadcasts
   - Test that sound plays correctly
   - Verify complete decoupling

**Success Criteria:**
- SoundEventChannelSO can be created and works correctly
- AudioManager responds to channel events
- Debug logger shows all events in real-time
- At least one system successfully uses event channel
- No direct references to AudioManager in broadcaster systems
- Memory leaks prevented (proper OnDisable unsubscribe)

---

### Phase 5: Migration Tooling (Week 2, Day 5 - Week 3, Day 1)
**Goal:** Automated tools to convert legacy data to ScriptableObjects

**Tasks:**
1. ✅ Create Editor Window: "AudioSystem/Convert to ScriptableObjects"

2. ✅ Implement conversion logic:
   ```csharp
   [MenuItem("AudioSystem/Convert to ScriptableObjects")]
   public static void ConvertRepositoryToScriptableObjects()
   {
       // Find AudioClipRepository in scene
       var repository = FindObjectOfType<AudioClipRepository>();

       // Get legacy data array
       var legacyData = GetLegacyDataArray(repository);

       // For each AudioClipData:
       foreach (var clipData in legacyData)
       {
           // Create AudioData asset
           var audioData = ScriptableObject.CreateInstance<AudioData>();

           // Copy data from struct to SO
           CopyDataToScriptableObject(clipData, audioData);

           // Save as asset file
           string path = $"Assets/Audio/Data/{clipData.audioType}/{clipData.clipName}.asset";
           AssetDatabase.CreateAsset(audioData, path);

           // Add to Repository's audioDataAssets array
           AddToRepositoryArray(repository, audioData);
       }

       AssetDatabase.SaveAssets();
       EditorUtility.DisplayDialog("Success", "Conversion complete!", "OK");
   }
   ```

3. ✅ Add preview/validation before conversion
4. ✅ Create backup of scene before modification
5. ✅ Organize assets by AudioType (BGM, Effect, UI, etc.)

**Success Criteria:**
- Tool successfully converts all legacy data
- Assets organized in logical folder structure
- Repository references updated automatically
- Original data preserved as backup
- Conversion can be undone if needed

---

### Phase 6: Documentation & Training (Week 3)
**Goal:** Team understands and adopts new workflow

**Tasks:**
1. ✅ Write Designer Guide: "Creating Audio Events"
   - How to create AudioData asset
   - How to add sound variations
   - How to set pitch/volume ranges
   - How to assign mixer groups
   - How to test in Play mode
   - Understanding Event Channels (for advanced users)

2. ✅ Write Programmer Guide: "Using AudioData in Code"
   ```csharp
   // OLD WAY (still works)
   effectAudioService.PlayEffect("ButtonClick", 1.0f, 1.0f);

   // NEW WAY - Direct (preferred for simple cases)
   [SerializeField] private AudioData buttonClickSound;
   effectAudioService.PlayEffect(buttonClickSound);

   // BEST WAY - Event Channel (preferred for decoupled systems)
   [SerializeField] private SoundEventChannelSO soundChannel;
   [SerializeField] private AudioData buttonClickSound;
   soundChannel.RaiseSoundEvent(buttonClickSound);
   ```

3. ✅ Create example AudioData assets
   - ButtonClick.asset
   - ButtonHover.asset
   - GoblinHit.asset (with 3 variations)
   - SwordSlash.asset (with pitch range 0.9-1.1)

4. ✅ Record video walkthrough for team

**Success Criteria:**
- Designers can create/modify audio without programmer help
- Programmers understand when to use new vs old API
- Example assets demonstrate best practices
- Team agrees new workflow is better

---

### Phase 7: Gradual Adoption (Week 3+)
**Goal:** New code uses AudioData, legacy code migrated gradually

**Priority Levels:**
- **P0 (Immediate):** All new audio events use AudioData + Event Channels
- **P1 (High Priority):** Core gameplay systems (combat, movement) → Event Channel
- **P2 (Medium Priority):** UI sounds (buttons, menus) → Direct AudioData or Event Channel
- **P3 (Low Priority):** Legacy scenes/prefabs → Keep string-based until refactor needed

**Tasks:**
1. ✅ **COMPLETED** - Update ButtonSoundPlayer to support Event Channel
   - **Implementation**: [ButtonSoundPlayer.cs](Assets/Script/Audio/ButtonSoundPlayer.cs)
   - **Features**:
     - Three-tier fallback system (Event Channel > AudioData > Legacy String)
     - Full backward compatibility with existing code
     - Clear priority system with logging
   ```csharp
   [Header("Sound Configuration (우선순위별)")]
   [SerializeField] private SoundEventChannelSO soundChannel; // BEST
   [SerializeField] private AudioData audioData;              // NEW
   [SerializeField] private string soundName;                 // LEGACY

   public void PlayClickSound()
   {
       // Priority 1: Event Channel + AudioData (BEST)
       if (soundChannel != null && audioData != null)
       {
           soundChannel.RaiseSoundEvent(audioData);
           return;
       }

       // Priority 2: AudioData only (NEW)
       if (audioData != null)
       {
           effectAudioService.PlayEffect(audioData);
           return;
       }

       // Priority 3: Legacy string-based (LEGACY)
       if (!string.IsNullOrEmpty(soundName))
       {
           effectAudioService.PlayEffect(soundName, volume, pitch);
           return;
       }
   }
   ```

2. ✅ **COMPLETED** - Create common AudioData assets
   - **Documentation**: [Phase7_Common_AudioData_Assets.md](claudedocs/Phase7_Common_AudioData_Assets.md)
   - **Asset structure defined**:
     - Button sounds (6 types: Default, Confirm, Cancel, Warning, Success, Navigation)
     - UI feedback sounds (5 types: Hover, MenuOpen, MenuClose, TabSwitch, Notification)
     - Common gameplay sounds (3 types: PickupItem, DropItem, GenericHit)
   - **Includes** detailed configuration parameters for each asset
   - **Status**: Ready for Unity asset creation

3. ⏳ **PENDING** - Migrate critical paths first
   - **Tracking**: [Phase7_Migration_Progress_Tracking.md](claudedocs/Phase7_Migration_Progress_Tracking.md)
   - **P1 High Priority** (10 components):
     - Main menu (3 buttons)
     - Pause menu (3 buttons)
     - Combat system (3 buttons)
     - Player movement UI (1 component)
   - **P2 Medium Priority** (10 components):
     - Inventory UI (4 buttons)
     - Shop UI (2 buttons)
     - Dialogue UI (2 buttons)
     - HUD elements (2 components)

4. ✅ **COMPLETED** - Track migration progress
   - **Spreadsheet**: [Phase7_Migration_Progress_Tracking.md](claudedocs/Phase7_Migration_Progress_Tracking.md)
   - **Coverage**: 23 total components across P1, P2, P3 priorities
   - **Current status**: 0/23 migrated (0%)
   - **Target**: 80% migrated by end of month (18.4/23 components)
   - **Includes**: Weekly goals with testing checklist per component

**Success Criteria:**
- All new features use AudioData
- High-traffic areas migrated
- Performance equal or better than before
- Designer satisfaction improved

---

## 🎨 Asset Organization

### Recommended Folder Structure

```
Assets/
  Audio/
    EventChannels/                 # Event Channel ScriptableObjects
      GlobalSoundChannel.asset     # Main channel for all sounds
      UISoundChannel.asset         # Optional: UI-specific sounds
      CombatSoundChannel.asset     # Optional: Combat-specific sounds

    Data/                          # AudioData ScriptableObjects
      BGM/
        MainMenu.asset
        Battle.asset
        Victory.asset
        Defeat.asset
      Effects/
        UI/
          ButtonClick.asset        # Single clip
          ButtonHover.asset
          ButtonConfirm.asset
          WindowOpen.asset
          WindowClose.asset
        Combat/
          SwordSlash.asset         # Multiple clips for variation
          GoblinHit.asset          # Pitch range 0.9-1.1
          PlayerDamage.asset
          Block.asset
        Movement/
          Footstep.asset           # 4 variations, cooldown 0.1s
          Jump.asset
          Land.asset
      Ambient/
        Forest.asset
        Cave.asset

    Clips/                         # Raw AudioClip files
      bgm/
        main_menu.ogg
        battle.ogg
      effects/
        ui/
          click.wav
          hover.wav
        combat/
          sword_slash_01.wav
          sword_slash_02.wav
          sword_slash_03.wav
          goblin_hit.wav
```

### Naming Conventions

**AudioData Assets:**
- PascalCase: `ButtonClick.asset`
- Descriptive: `SwordSlash.asset` not `Sound1.asset`
- Category prefix for large projects: `UI_ButtonClick.asset`

**AudioClip Files:**
- snake_case: `button_click.wav`
- Variations numbered: `sword_slash_01.wav`, `sword_slash_02.wav`
- Format suffix optional: `footstep_grass.wav`, `footstep_metal.wav`

---

## ⚠️ Risk Management

### Risk Matrix

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Breaking existing scenes | LOW | HIGH | Maintain backward compatibility, thorough testing |
| Performance regression | LOW | MEDIUM | Profile before/after, optimize if needed |
| Team adoption resistance | MEDIUM | MEDIUM | Good documentation, clear benefits, training |
| Asset management overhead | MEDIUM | LOW | Clear folder structure, naming conventions |
| Migration tool bugs | MEDIUM | MEDIUM | Preview/validate before conversion, backup |

### Mitigation Strategies

1. **Backward Compatibility**
   - All existing APIs remain functional
   - Services support both AudioData and string lookups
   - Repository handles both types
   - Zero breaking changes

2. **Testing Plan**
   - Unit tests for AudioData methods
   - Integration tests for service calls
   - Manual testing of all existing scenes
   - Performance profiling (100+ concurrent sounds)
   - Designer workflow validation

3. **Rollback Plan**
   - Keep legacy data in Repository
   - Can toggle between systems via flag
   - Git branches for each phase
   - Scene backups before conversion

4. **Performance Monitoring**
   - Profile audio system before migration
   - Profile after each phase
   - Monitor: CPU usage, memory, GC allocations
   - Target: ≤5% overhead acceptable

---

## ✅ Validation Checklist

### Phase 1: Foundation
- [ ] AudioData.cs compiles without errors
- [ ] Can create AudioData via right-click menu
- [ ] Inspector displays all fields correctly
- [ ] Behavior methods work (GetRandomClip, CanPlay)
- [ ] No existing scenes broken

### Phase 2: Service Integration
- [ ] IBGMAudioService updated with new methods
- [ ] IEffectAudioService updated with new methods
- [ ] BGMAudioService implements new methods
- [ ] EffectAudioService implements new methods
- [ ] AudioData metadata applied correctly (pitch, volume, mixer)
- [ ] All existing string-based calls still work
- [ ] Unit tests pass

### Phase 3: Repository Evolution
- [ ] Repository holds AudioData references
- [ ] GetAudioData() method works
- [ ] Legacy data still accessible
- [ ] Priority: AudioData > Legacy
- [ ] Caching works for both types
- [ ] No breaking changes

### Phase 4: Event Channel Integration
- [ ] SoundEventChannelSO.cs created and compiles
- [ ] GlobalSoundChannel.asset created
- [ ] AudioManager subscribes/unsubscribes correctly
- [ ] HandleSoundRequest method works
- [ ] AudioDebugLogger logs events properly
- [ ] At least one system migrated to Event Channel
- [ ] No memory leaks (OnDisable unsubscribe verified)
- [ ] Complete decoupling achieved (no AudioManager references in broadcasters)

### Phase 5: Migration Tooling
- [ ] Conversion tool accessible via menu
- [ ] Preview shows what will be created
- [ ] Conversion creates all assets correctly
- [ ] Assets organized properly
- [ ] Repository references updated
- [ ] Original data preserved
- [ ] Can undo conversion

### Phase 6: Documentation
- [ ] Designer guide complete
- [ ] Programmer guide complete
- [ ] Example assets created
- [ ] Video walkthrough recorded
- [ ] Team trained

### Phase 7: Adoption
- [ ] New code uses AudioData + Event Channels
- [ ] ButtonSoundPlayer updated with Event Channel support
- [ ] Common AudioData assets created
- [ ] Common Event Channel assets created
- [ ] Critical paths migrated to Event Channel pattern
- [ ] Performance validated
- [ ] Designer satisfaction improved
- [ ] System decoupling verified (no direct AudioManager dependencies)

---

## 📊 Success Metrics

### Quantitative Metrics
- **Migration Coverage:** 80% of audio calls use AudioData by end of month
- **Designer Productivity:** 50% reduction in programmer requests for audio tweaks
- **Sound Variation:** Average 2.5 clips per AudioData (vs 1 currently)
- **Performance:** ≤5% CPU/memory overhead
- **Asset Count:** ~50 AudioData assets created in first month

### Qualitative Metrics
- **Designer Satisfaction:** Survey before/after (target: +2 points on 5-point scale)
- **Code Quality:** Less hard-coded strings, more type-safe references, zero direct AudioManager coupling
- **Maintainability:** Easier to add new sounds (target: <5 minutes)
- **System Decoupling:** Game systems testable independently of AudioManager
- **Team Confidence:** Comfortable using new system independently

---

## 🎓 Key Learnings from Article

### Workflow Revolution Principle
> "ScriptableObject는 프로그래머와 사운드 디자이너 간의 '설계 계약' 역할을 한다."

**Applied:**
- Programmer: Creates AudioData asset, uses it in code
- Designer: Defines HOW it sounds (clips, pitch, volume, routing)
- Clear separation of responsibilities
- No code changes needed for audio tweaks

### Data-Logic Separation
> "로직과 데이터를 완벽하게 분리하여 매우 강력하고 유연한 오디오 시스템을 구축"

**Applied:**
- AudioData: Pure data container (ScriptableObject)
- Services: Pure logic (MonoBehaviour)
- Repository: Thin lookup layer
- No mixed concerns

### Designer Empowerment
> "게임이 실행 중인 상태에서도 AudioData 애셋을 수정하며 실시간으로 사운드를 조율"

**Applied:**
- Play mode editing of AudioData
- Immediate feedback on changes
- No scene reloading required
- Independent iteration

### Event-Driven Decoupling
> "시스템 간의 통신을 중개하는 ScriptableObject로 방송자와 청취자가 서로를 알 필요 없게 만든다"

**Applied:**
- SoundEventChannelSO mediates all audio requests
- Game systems broadcast events without knowing AudioManager
- AudioManager listens without knowing requesters
- Perfect adherence to Dependency Inversion Principle
- Open-Closed Principle: extend without modifying existing code
- Complete elimination of singleton anti-pattern

---

## 🚀 Next Steps

### Immediate (This Week)
1. Create AudioData.cs ScriptableObject class
2. Add to version control
3. Test with sample assets
4. Present to team for feedback

### Short-term (This Month)
1. Implement service integration
2. Implement Event Channel architecture
3. Build migration tools
4. Create documentation
5. Train team on both AudioData and Event Channels

### Long-term (Next Quarter)
1. Migrate all audio to AudioData + Event Channels
2. Remove legacy code and direct AudioManager references
3. Add advanced features (ducking, audio mix snapshots)
4. Extend Event Channel pattern to other systems (VFX, UI feedback)
5. Share learnings with wider team

---

## 📚 References

- Original Article: "ScriptableObject 혁명" section
- Unity Documentation: ScriptableObject
- Current Codebase: AudioClipRepository.cs, BGMAudioService.cs, EffectAudioService.cs

---

## 💬 Questions & Discussion

**For Team Review:**
1. Is the phased approach acceptable? (Zero downtime vs faster but risky)
2. Asset organization preferences? (Folder structure feedback)
3. Priority for migration? (Which systems first?)
4. Additional features needed? (Custom behavior per AudioData?)

**For Technical Review:**
1. Performance concerns with ScriptableObject lookup?
2. Cooldown system design (global vs per-instance)?
3. Thread safety considerations?
4. Memory management strategies?

---

**Document Version:** 1.0
**Last Updated:** 2025-10-09
**Author:** Audio System Refactoring Team
**Status:** ✅ Ready for Implementation
