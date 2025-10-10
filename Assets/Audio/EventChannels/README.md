# Event Channels Directory

This directory contains ScriptableObject assets for the Audio Event Channel system.

## How to Create GlobalSoundChannel.asset

1. In Unity Editor, right-click in this folder
2. Select `Create > Audio > Event Channel`
3. Name it `GlobalSoundChannel`
4. Assign this asset to:
   - AudioServiceContainer's `soundEventChannel` field
   - AudioDebugLogger's `soundEventChannel` field
   - Any EventChannelSoundPlayer components that need to broadcast sounds

## Usage Pattern

```
Game System (e.g., PlayerHealth)
   ↓ broadcasts to
GlobalSoundChannel (ScriptableObject)
   ↓ notifies
AudioServiceContainer
   ↓ routes to
BGMAudioService or EffectAudioService
```

## Benefits

- **Complete Decoupling**: Game systems don't reference AudioServiceContainer
- **Easy Testing**: Can swap event channels or mock them in tests
- **Designer Friendly**: Artists/designers can configure sounds via Inspector
- **Debugging**: AudioDebugLogger can monitor all events in real-time
- **Scalability**: Can create multiple channels for different sound categories

## Example Systems to Migrate

- PlayerHealth → death sounds
- EnemyAI → attack/damage sounds
- UI Systems → button clicks
- Weapons → firing/reload sounds
- Environment → ambient/trigger sounds
